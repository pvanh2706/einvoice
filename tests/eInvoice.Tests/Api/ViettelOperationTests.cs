using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Viettel;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;
using ProviderCall = eInvoice.Tests.Api.MisaApiTestFactory.ProviderCall;

namespace eInvoice.Tests.Api;

public class ViettelOperationTests
{
    private static readonly ResolvedSite Site = new(new SiteConfiguration
    {
        Id = MisaTestData.SiteId, Partner = 4, PartnerUrl = "https://viettel.example.test/", TaxNumber = "TEST-TAX",
        Username = "synthetic-viettel-user", Password = "synthetic-viettel-secret",
    }, []);

    private static MisaApiTestFactory Factory(Func<ProviderCall, HttpResponseMessage>? responder = null) => new()
    {
        Resolve = (_, _) => Site,
        Respond = call => responder?.Invoke(call) ?? Default(call),
    };
    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Default(ProviderCall call) => Json(call.Url.EndsWith("auth/login", StringComparison.Ordinal)
        ? "{\"access_token\":\"synthetic-viettel-token\"}"
        : call.Url.Contains("createBatchInvoice", StringComparison.Ordinal)
            ? "{\"createInvoiceOutputs\":[{\"transactionUuid\":\"INV-001\",\"errorCode\":\"200\",\"result\":{\"invoiceNo\":\"000001\"}}]}"
            : call.Url.Contains("searchInvoice", StringComparison.Ordinal)
                ? "{\"errorCode\":\"200\",\"result\":[{\"invoiceNo\":\"000001\",\"codeOfTax\":\"TAX-001\"}]}"
                : "{\"errorCode\":\"200\",\"result\":{\"invoiceNo\":\"000001\",\"reservationCode\":\"SEARCH-001\",\"codeOfTax\":\"TAX-001\"},\"fileToBytes\":\"c3ludGhldGljLXBkZg==\"}");

    [Theory]
    [InlineData("publish", "publish")]
    [InlineData("publish-batch", "publish-batch")]
    [InlineData("draft", "draft")]
    [InlineData("preview", "preview")]
    [InlineData("replace", "replace")]
    [InlineData("adjust-increase", "adjust/increase")]
    [InlineData("adjust-decrease", "adjust/decrease")]
    [InlineData("adjust-information", "adjust/information")]
    [InlineData("document", "document")]
    [InlineData("search", "search")]
    [InlineData("void", "void")]
    public async Task Api_preserves_independent_baseline_wire_fixture(string fixture, string route)
    {
        using var factory = Factory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-fixture-001");
        object request = route switch
        {
            "publish-batch" => new InvoiceBatchRequest { Invoices = [MisaTestData.Invoice()] },
            "document" => new DownloadPdfRequest { InvoiceId = "INV-001", InvoiceType = 0, ProviderInvoiceNumber = "000001", FormNo = "1" },
            "search" => new SearchInvoiceRequest { InvoiceId = "INV-001" },
            "void" => new VoidInvoiceRequest { InvoiceType = 0, ProviderInvoiceNumber = "000001", FormNo = "1", InvoiceDate = "08/10/2026", AdditionalReferenceDate = "10/10/2026", Reason = "Synthetic reason" },
            _ => MisaTestData.Invoice(),
        };
        using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(request,
            route is "publish" or "draft" ? typeof(CreateInvoiceRequest) : null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = factory.Calls.Last();
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Viettel", "Payloads", fixture + ".json")));
        var actual = route is "search" or "void"
            ? JObject.FromObject(call.Body!.Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1])))
            : JToken.Parse(call.Body!);
        Assert.True(JToken.DeepEquals(expected, actual), actual.ToString());
        Assert.Equal("access_token=synthetic-viettel-token", call.Headers["Cookie"]);
        Assert.Equal(2, factory.Calls.Count);
        Assert.DoesNotContain("synthetic-viettel-token", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_and_capabilities_expose_no_token_and_match_baseline_actions()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("connected").GetBoolean());
        Assert.Equal("VIETTEL", json.RootElement.GetProperty("provider").GetString());
        Assert.DoesNotContain("synthetic-viettel-token", json.RootElement.GetRawText());
        using var capabilities = await client.GetAsync("/api/eInvoice/capabilities");
        using var caps = JsonDocument.Parse(await capabilities.Content.ReadAsStringAsync());
        Assert.Equal(ProviderCapabilityCatalog.GetBaselineActions(ProviderId.Viettel).Select(a => (int)a).Order(),
            caps.RootElement.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("action").GetInt32()).Order());
        Assert.Single(factory.Calls);
    }

    [Fact]
    public async Task Publish_replays_after_success_and_blocks_a_new_key_without_another_provider_call()
    {
        using var factory = Factory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-publish-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal(2, factory.Calls.Count);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/viettel-publish-001");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("completed", json.RootElement.GetProperty("state").GetString()); Assert.Equal("VIETTEL", json.RootElement.GetProperty("provider").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-publish-002");
        using var blocked = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode); Assert.Contains("VIETTEL_INVOICE_ALREADY_ATTEMPTED", await blocked.Content.ReadAsStringAsync());
        Assert.Equal(2, factory.Calls.Count);
    }

    [Theory]
    [InlineData("{\"errorCode\":\"400\",\"description\":\"synthetic-viettel-secret synthetic-viettel-token invalid amount\"}", 422)]
    [InlineData("{\"errorCode\":\"200\",\"result\":{}}", 502)]
    [InlineData("{\"errorCode\":\"200\",\"result\":{\"invoiceNo\":\"000001\",\"transactionUuid\":\"OTHER-ID\"}}", 502)]
    [InlineData("not-json", 502)]
    [InlineData("", 502)]
    [InlineData("{\"result\":{\"invoiceNo\":\"000001\"}}", 502)]
    public async Task Post_dispatch_error_is_sanitized_persisted_and_never_retried(string providerResponse, int status)
    {
        using var factory = Factory(call => call.Url.EndsWith("auth/login", StringComparison.Ordinal) ? Default(call) : Json(providerResponse));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-error-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-viettel-secret", text); Assert.DoesNotContain("synthetic-viettel-token", text);
        using var lookup = await client.GetAsync("/api/eInvoice/invoice-attempts?invoiceId=INV-001&phase=publish");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("pending", json.RootElement.GetProperty("state").GetString()); Assert.False(json.RootElement.GetProperty("canRetry").GetBoolean());
        Assert.Equal("VIETTEL", json.RootElement.GetProperty("provider").GetString());
        using var again = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
        Assert.DoesNotContain("synthetic-viettel-secret", string.Join('\n', factory.Logs));
    }

    [Theory]
    [InlineData("{\"createInvoiceOutputs\":[{\"transactionUuid\":\"OTHER\",\"result\":{\"invoiceNo\":\"1\"}}]}")]
    [InlineData("{\"createInvoiceOutputs\":[]}")]
    public async Task Batch_rejects_missing_or_foreign_invoice_results(string supplierBody)
    {
        using var factory = Factory(call => call.Url.EndsWith("auth/login", StringComparison.Ordinal) ? Default(call) : Json(supplierBody));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-batch-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest { Invoices = [MisaTestData.Invoice()] }));
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Batch_preserves_partial_failures_and_correlates_results_by_id()
    {
        using var factory = Factory(call => call.Url.EndsWith("auth/login", StringComparison.Ordinal) ? Default(call) :
            Json("{\"createInvoiceOutputs\":[{\"transactionUuid\":\"INV-002\",\"errorCode\":\"400\",\"description\":\"Invalid amount\"},{\"transactionUuid\":\"INV-001\",\"result\":{\"invoiceNo\":\"000001\"}}]}"));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-batch-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest { Invoices = [MisaTestData.Invoice(), MisaTestData.Invoice(id: "INV-002")] }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("data")[0].GetProperty("isSuccess").GetBoolean());
        Assert.True(json.RootElement.GetProperty("data")[1].GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task Invalid_input_or_unsupported_operation_does_not_contact_provider()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-invalid-001");
        var input = MisaTestData.Invoice(); input.Invoice!.InvoiceDate = "invalid";
        using var invalid = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(input, typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var status = await client.PostAsync("/api/eInvoice/status", MisaApiTestFactory.Body(new SendToTaxAuthorityRequest { InvoiceIds = ["INV-001"] }));
        Assert.Equal(HttpStatusCode.NotImplemented, status.StatusCode); Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Buyer_bank_fields_use_current_bank_information_and_not_payment_bank_fields()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-bank-001");
        var input = MisaTestData.Invoice(); input.Invoice!.BankAccount = "BUYER-ACCOUNT"; input.Invoice.BankName = "BUYER-BANK";
        input.Invoice.PaymentBankAccount = "PAYMENT-ACCOUNT"; input.Invoice.PaymentBankName = "PAYMENT-BANK";
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(input, typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var buyer = JToken.Parse(factory.Calls.Last().Body!)["buyerInfo"]!;
        Assert.Equal("BUYER-ACCOUNT", buyer.Value<string>("buyerBankAccount")); Assert.Equal("BUYER-BANK", buyer.Value<string>("buyerBankName"));
    }

    [Theory]
    [InlineData("01/01/1970", -25200000L)]
    [InlineData("01/01/2026", 1767200400000L)]
    public void Provider_date_is_midnight_UTC7_independent_of_server_timezone(string date, long expected) =>
        Assert.Equal(expected, ProviderDate.ToUnixMilliseconds(date));

    [Fact]
    public async Task Login_rejection_releases_unsubmitted_reservation_then_corrected_credentials_can_succeed()
    {
        var reject = true;
        using var factory = Factory(call => reject ? Json("{\"code\":\"401\",\"description\":\"synthetic-viettel-secret invalid login\"}", HttpStatusCode.Unauthorized) : Default(call));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-login-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, first.StatusCode); Assert.Single(factory.Calls);
        Assert.DoesNotContain("synthetic-viettel-secret", await first.Content.ReadAsStringAsync());
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/viettel-login-001");
        Assert.Contains("not-found", await lookup.Content.ReadAsStringAsync());
        reject = false;
        using var retry = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal(3, factory.Calls.Count);
    }

    [Fact]
    public async Task Business_timeout_stays_pending_and_lookup_cannot_trigger_another_write()
    {
        using var factory = Factory(call => call.Url.EndsWith("auth/login", StringComparison.Ordinal) ? Default(call) : throw new TaskCanceledException());
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-timeout-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        using var lookup = await client.GetAsync("/api/eInvoice/invoice-attempts?invoiceId=INV-001");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("VIETTEL_TIMEOUT", json.RootElement.GetProperty("lastError").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("lastError").GetProperty("providerHttpStatus").ValueKind);
        Assert.True(json.RootElement.GetProperty("requiresReconciliation").GetBoolean()); Assert.Equal(2, factory.Calls.Count);
    }

    [Theory]
    [InlineData("http://viettel.example.test/")]
    [InlineData("https://other.example.test/")]
    [InlineData("https://viettel.example.test/?password=secret")]
    public async Task Endpoint_validation_fails_before_any_http_request(string endpoint)
    {
        using var factory = Factory();
        factory.Resolve = (_, _) => new ResolvedSite(new SiteConfiguration { Id = Site.Site.Id, Partner = 4, PartnerUrl = endpoint, TaxNumber = "TEST-TAX", Username = "user", Password = "secret" }, []);
        using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Missing_authentication_never_contacts_viettel()
    {
        using var factory = Factory(); using var client = factory.Client(authenticated: false);
        using var response = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Site_metadata_mapping_preserves_customername_buyer_rule_and_supplier_key_case()
    {
        using var factory = Factory();
        factory.Resolve = (_, _) => new ResolvedSite(Site.Site,
            [new ExtraFieldMapping { Code = "CUSTOMER", ThirdPartyCode = "Customername", ThirdPartyDataType = "text", ThirdPartyLabel = "Customer", FormNo = "1", Serial = "C26TAA" }]);
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "viettel-metadata-001");
        var input = MisaTestData.Invoice(); input.ExtraData = [new ExtraField { Code = "CUSTOMER", StringValue = "Synthetic customer" }];
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(input, typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JToken.Parse(factory.Calls.Last().Body!);
        Assert.Equal("", body["buyerInfo"]!.Value<string>("buyerName"));
        Assert.Equal("Customername", body["metadata"]![0]!.Value<string>("keyTag"));
        Assert.Equal("Synthetic customer", body["metadata"]![0]!.Value<string>("stringValue"));
    }
}
