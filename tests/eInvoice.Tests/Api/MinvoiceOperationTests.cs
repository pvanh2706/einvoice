using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;
using ProviderCall = eInvoice.Tests.Api.MisaApiTestFactory.ProviderCall;

namespace eInvoice.Tests.Api;

public class MinvoiceOperationTests
{
    private const string Id = "33333333-3333-3333-3333-333333333333";
    private const string Original = "44444444-4444-4444-4444-444444444444";
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-10T18:00:00Z", CultureInfo.InvariantCulture); }
    private static MisaApiTestFactory Factory(Func<ProviderCall, HttpResponseMessage>? responder = null) => new()
    {
        Clock = new Clock(),
        Resolve = (_, _) => new ResolvedSite(new SiteConfiguration
        { Id = MisaTestData.SiteId, Partner = 2, PartnerUrl = "https://minvoice.example.test/", TaxNumber = "TEST-TAX", Username = "synthetic-user", Password = "synthetic-secret", Username2 = "synthetic-unit" }, []),
        Respond = call => responder?.Invoke(call) ?? Default(call),
    };
    private static HttpResponseMessage Json(string text, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Default(ProviderCall call)
    {
        if (call.Url.EndsWith("Login", StringComparison.Ordinal)) return Json("{\"token\":\"synthetic-token\"}");
        if (call.Url.EndsWith("GetTypeInvoiceSeries", StringComparison.Ordinal)) return Json("{\"code\":\"00\",\"data\":[{\"invoiceForm\":\"1\",\"khhdon\":\"C26TAA\"}]}");
        if (call.Url.EndsWith("GetInvoices", StringComparison.Ordinal)) return Json("{\"code\":\"00\",\"data\":[{\"hoadon68_id\":\"" + Id + "\",\"is_success\":1,\"nky\":\"2026-10-10T18:00:00Z\"}]}");
        if (call.Url.Contains("SearchTaxCode?", StringComparison.Ordinal)) return Json("{\"ma_so_thue\":\"TEST-TAX\",\"ten_cty\":\"Synthetic company\",\"dia_chi\":\"Synthetic address\"}");
        if (call.Url.Contains("PrintInvoice?", StringComparison.Ordinal)) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("%PDF-1.7\nSynthetic offline PDF\n%%EOF"u8.ToArray()) };
        var input = JToken.Parse(call.Body!);
        var code = input["data"]?.FirstOrDefault()?["hoadon68_id"]?.Value<string>() ?? Id;
        return Json("{\"code\":\"00\",\"data\":{\"hoadon68_id\":\"" + code + "\",\"shdon\":\"0000123\",\"sobaomat\":\"SEARCH-001\",\"macqt\":\"TAX-001\"},\"sobaomat\":\"TOP-SEARCH\",\"macqt\":\"TOP-TAX\"}");
    }
    private static AdjustOrReplaceInvoiceRequest Input()
    {
        var input = MisaTestData.Invoice(id: Id); input.Invoice!.SearchCode = "SEARCH-001";
        input.OriginalInvoiceId = "PMS-ORIGINAL-ID"; input.OriginalProviderInvoiceCode = Original; return input;
    }

    [Theory]
    [InlineData("publish", "publish", "SaveSign")]
    [InlineData("create", "create", "Save")]
    [InlineData("change", "change", "Save")]
    [InlineData("replace", "replace", "ThayThe")]
    [InlineData("adjust", "adjust", "DieuChinh")]
    [InlineData("delete", "delete", "Save")]
    [InlineData("void", "void", "HuyHoaDon")]
    [InlineData("sign", "publish-existing", "Sign")]
    [InlineData("invoices", "invoices", "GetInvoices")]
    public async Task Full_api_preserves_frozen_wire_culture_dates_and_provider_reference(string fixture, string route, string endpoint)
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-fixture-001");
        object body = route switch
        {
            "delete" => new DeleteInvoiceRequest { InvoiceId = "PMS-REFERENCE", InvoiceType = 0, ProviderInvoiceNumber = "0000123", Serial = "C26TAA" },
            "void" => new VoidInvoiceRequest { InvoiceId = "PMS-REFERENCE", InvoiceType = 0, ProviderInvoiceCode = Id, ProviderInvoiceNumber = "0000123" },
            "publish-existing" => new PublishExistingInvoiceRequest { InvoiceId = "PMS-REFERENCE", InvoiceType = 0, ProviderInvoiceCode = Id, ProviderInvoiceNumber = "0000123" },
            "invoices" => new GetInvoicesRequest { FromDate = new DateTime(2026, 10, 1), ToDate = new DateTime(2026, 10, 11), Serial = "C26TAA" },
            _ => Input(),
        };
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(body, route is "create" or "publish" ? typeof(CreateInvoiceRequest) : null));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Minvoice", "Payloads", fixture + ".json")));
            var call = factory.Calls.Last(); Assert.True(JToken.DeepEquals(expected, JToken.Parse(call.Body!)), call.Body);
            Assert.EndsWith(endpoint, call.Url); Assert.Equal("Bear synthetic-token", call.Headers["Authorization"]); Assert.Equal("utf-8", call.Encoding);
            Assert.Equal("VP", JObject.Parse(factory.Calls.First().Body!).Value<string>("ma_dvcs"));
            Assert.Equal(2, factory.Calls.Count); Assert.Equal("fr-FR", CultureInfo.CurrentCulture.Name);
            var result = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-token", result);
            if (route == "change") { Assert.Contains(Id, result); Assert.Contains(Original, result); }
            if (route is "replace" or "adjust") Assert.Contains("TOP-SEARCH", result);
            if (route == "publish") Assert.Contains("SEARCH-001", result);
            if (route == "invoices") Assert.Contains("2026-10-11T01:00:00+07:00", result);
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Fact]
    public async Task Health_and_all_fifteen_capabilities_are_site_scoped_and_hide_token()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var health = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var call = factory.Calls.Single(); Assert.Equal("synthetic-unit", JObject.Parse(call.Body!).Value<string>("ma_dvcs")); Assert.False(call.Headers.ContainsKey("Authorization"));
        Assert.DoesNotContain("synthetic-token", await health.Content.ReadAsStringAsync());
        using var caps = await client.GetAsync("/api/eInvoice/capabilities"); using var json = JsonDocument.Parse(await caps.Content.ReadAsStringAsync());
        Assert.Equal(ProviderCapabilityCatalog.GetBaselineActions(ProviderId.Minvoice).Select(a => (int)a).Order(),
            json.RootElement.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("action").GetInt32()).Order());
        Assert.Single(factory.Calls);
    }

    [Fact]
    public async Task Forms_get_keeps_provider_keys_and_canonical_defaults()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/form-serials", MisaApiTestFactory.Body(new { invoiceType = 0 }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(HttpMethod.Get, factory.Calls.Last().Method);
        var text = await response.Content.ReadAsStringAsync(); Assert.Contains("C26TAA", text); Assert.Contains("\"fromNo\":1", text);
    }

    [Fact]
    public async Task Document_get_returns_only_actual_pdf_bytes_and_uses_provider_guid()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceId = "PMS-ID", ProviderInvoiceCode = Id, InvoiceType = 0 }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.EndsWith("?id=" + Id, factory.Calls.Last().Url); Assert.Equal(HttpMethod.Get, factory.Calls.Last().Method);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.StartsWith("%PDF-", Encoding.UTF8.GetString(Convert.FromBase64String(json.RootElement.GetProperty("data").GetProperty("content").GetString()!)));
    }

    [Theory]
    [InlineData("{\"code\":\"22\",\"message\":\"synthetic-secret synthetic-token missing invoice\"}",422)]
    [InlineData("There is no row at position 0.",404)]
    [InlineData("{\"code\":\"00\",\"data\":\"not a PDF\"}",502)]
    [InlineData("<html>error</html>",502)]
    public async Task Supplier_errors_are_never_base64_encoded_as_pdf_success(string body, int expected)
    {
        using var factory = Factory(call => call.Url.EndsWith("Login", StringComparison.Ordinal) ? Default(call) : Json(body));
        using var client = factory.Client(); using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { ProviderInvoiceCode = Id, InvoiceType = 0 }));
        Assert.Equal(expected, (int)response.StatusCode); var text = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-secret", text); Assert.DoesNotContain("synthetic-token", text);
    }

    [Fact]
    public async Task Email_wire_uses_explicit_provider_code_not_pms_id_and_replays_without_resending()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-email-001");
        var request = new SendEmailRequest { InvoiceId = Guid.Parse(Original), ProviderInvoiceCode = Guid.Parse(Id), Email = "synthetic@example.test" };
        using var first = await client.PostAsync("/api/eInvoice/email", MisaApiTestFactory.Body(request));
        using var replay = await client.PostAsync("/api/eInvoice/email", MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal(2, factory.Calls.Count);
        var body = JObject.Parse(factory.Calls.Last().Body!); Assert.Equal(Id, body["id"]![0]!.Value<string>("id")); Assert.Equal("Gửi hóa đơn", body.Value<string>("type"));
    }

    [Fact]
    public async Task Tax_code_uses_fixed_registry_after_login_without_forwarding_token_and_maps_real_data()
    {
        using var factory = Factory(); using var client = factory.Client(); using var response = await client.PostAsync("/api/eInvoice/tax-code", MisaApiTestFactory.Body(new GetTaxCodeInfoRequest { TaxCode = "TEST-TAX&A=B" }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = factory.Calls.Last(); Assert.Equal("https://mst.minvoice.com.vn/api/System/SearchTaxCode?tax=TEST-TAX%26A%3DB", call.Url);
        Assert.False(call.Headers.ContainsKey("Authorization")); Assert.Equal(2, factory.Calls.Count); Assert.Contains("Synthetic company", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Completed_publish_replays_and_cannot_be_signed_again_under_another_pms_id()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-publish-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-sign-001");
        using var again = await client.PostAsync("/api/eInvoice/publish-existing", MisaApiTestFactory.Body(new PublishExistingInvoiceRequest { InvoiceId = "ANOTHER-PMS-ID", ProviderInvoiceCode = Id, ProviderInvoiceNumber = "123" }));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
    }

    [Theory]
    [InlineData("{\"code\":\"23\",\"message\":\"synthetic-secret synthetic-token already signed\"}",422)]
    [InlineData("{\"code\":\"00\",\"data\":{\"hoadon68_id\":\"44444444-4444-4444-4444-444444444444\",\"shdon\":\"123\"}}",502)]
    [InlineData("{\"code\":\"00\",\"data\":{}}",502)]
    [InlineData("not-json",502)]
    [InlineData("",502)]
    public async Task Post_dispatch_failure_remains_pending_and_cannot_retry(string body, int status)
    {
        using var factory = Factory(call => call.Url.EndsWith("Login", StringComparison.Ordinal) ? Default(call) : Json(body)); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-error-001"); using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)first.StatusCode); var text = await first.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-secret", text); Assert.DoesNotContain("synthetic-token", text);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/minvoice-error-001"); using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("pending", json.RootElement.GetProperty("state").GetString()); Assert.Equal("MINVOICE", json.RootElement.GetProperty("provider").GetString()); Assert.False(json.RootElement.GetProperty("canRetry").GetBoolean());
        using var again = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count); Assert.DoesNotContain("synthetic-secret", string.Join('\n', factory.Logs));
    }

    [Fact]
    public async Task Timeout_after_dispatch_is_saved_and_never_retried()
    {
        using var factory = Factory(call => call.Url.EndsWith("Login", StringComparison.Ordinal) ? Default(call) : throw new TaskCanceledException("synthetic-secret")); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "minvoice-timeout-001"); using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.GatewayTimeout, first.StatusCode);
        using var again = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
    }

    [Fact]
    public async Task Invalid_query_and_provider_reference_fail_before_login()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var invalid = await client.PostAsync("/api/eInvoice/invoices", MisaApiTestFactory.Body(new GetInvoicesRequest { Serial = "C26TAA" }));
        using var document = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceType = 0, InvoiceId = Id }));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); Assert.Equal(HttpStatusCode.BadRequest, document.StatusCode); Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Query_dates_with_explicit_instants_are_normalized_to_vietnam_business_day()
    {
        using var factory = Factory(); using var client = factory.Client(); using var response = await client.PostAsync("/api/eInvoice/invoices", MisaApiTestFactory.Body(new GetInvoicesRequest
        { Serial = "C26TAA", FromDate = DateTime.Parse("2026-10-10T18:00:00Z", null, DateTimeStyles.RoundtripKind), ToDate = DateTime.Parse("2026-10-11T18:00:00Z", null, DateTimeStyles.RoundtripKind) }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var wire = JObject.Parse(factory.Calls.Last().Body!);
        Assert.Equal("2026-10-11", wire.Value<string>("tuNgay")); Assert.Equal("2026-10-12", wire.Value<string>("denNgay"));
    }
}
