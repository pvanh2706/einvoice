using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;
using ProviderCall = eInvoice.Tests.Api.MisaApiTestFactory.ProviderCall;

namespace eInvoice.Tests.Api;

public class EasyInvoiceOperationTests
{
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-10T18:00:00Z", CultureInfo.InvariantCulture); }
    private static MisaApiTestFactory Factory(Func<ProviderCall, HttpResponseMessage>? responder = null) => new()
    {
        Clock = new Clock(), Resolve = (_, _) => new ResolvedSite(new SiteConfiguration
        { Id = MisaTestData.SiteId, Partner = 9, PartnerUrl = "https://easyinvoice.example.test/", Username = "synthetic-user", Password = "synthetic-secret" }, []),
        Respond = call => responder?.Invoke(call) ?? Default(call),
    };
    private static HttpResponseMessage Json(string text, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Default(ProviderCall call) => call.Url.EndsWith("getInvoicePdf", StringComparison.Ordinal) || call.Url.EndsWith("previewInvoice", StringComparison.Ordinal)
        ? new(HttpStatusCode.OK) { Content = new ByteArrayContent("%PDF-1.7\nSynthetic PDF\n%%EOF"u8.ToArray()) }
        : Json("{\"Status\":2,\"Data\":{\"Invoices\":\"[{\\\"Ikey\\\":\\\"INV-001\\\",\\\"No\\\":\\\"000123\\\",\\\"LookupCode\\\":\\\"SEARCH\\\",\\\"TaxAuthorityCode\\\":\\\"TAX\\\"}]\"}}");
    private static AdjustOrReplaceInvoiceRequest Input(int? previewType = null)
    {
        var input = MisaTestData.Invoice(); input.OriginalInvoiceId = "OLD-001"; input.OriginalInvoice = new Invoice { InvoiceId = "OLD-001", MaCQT = "OLD-TAX" };
        input.Invoice!.PreviewType = previewType; return input;
    }

    [Theory]
    [InlineData("publish","publish","importAndPublishInvoice",null)]
    [InlineData("create","create","importInvoice",null)]
    [InlineData("draft","draft","importInvoice",null)]
    [InlineData("replace","replace","replaceInvoice",null)]
    [InlineData("adjust-increase","adjust/increase","adjustInvoice",null)]
    [InlineData("adjust-decrease","adjust/decrease","adjustInvoice",null)]
    [InlineData("adjust-information","adjust/information","adjustInvoice",null)]
    [InlineData("preview","preview","previewInvoice",1)]
    [InlineData("preview-replace","preview","previewInvoice",3)]
    [InlineData("preview-increase","preview","previewInvoice",4)]
    [InlineData("preview-decrease","preview","previewInvoice",5)]
    [InlineData("preview-information","preview","previewInvoice",6)]
    [InlineData("document","document","getInvoicePdf",null)]
    [InlineData("void","void","cancelInvoice",null)]
    [InlineData("delete","delete","removeUnsignedInvoice",null)]
    [InlineData("publish-existing","publish-existing","issueInvoices",null)]
    public async Task Api_preserves_wire_xml_reference_and_authentication(string fixture, string route, string endpoint, int? previewType)
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-fixture-001");
        object request = route switch
        {
            "void" => new VoidInvoiceRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" },
            "delete" => new DeleteInvoiceRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" },
            "document" => new DownloadPdfRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" },
            "publish-existing" => new PublishExistingInvoiceRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" }, _ => Input(previewType),
        };
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(request, route is "publish" or "create" or "draft" ? typeof(CreateInvoiceRequest) : null));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var call = factory.Calls.Single(); Assert.EndsWith(endpoint, call.Url); Assert.Equal("utf-8", call.Encoding); Assert.Equal(HttpMethod.Post, call.Method);
            var expected = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "EasyInvoice", "Payloads", fixture + ".json")));
            // Frozen synthetic input had no OriginalInvoiceId. The API requires it; XML stays byte-identical.
            if (route == "replace" || route.StartsWith("adjust/", StringComparison.Ordinal)) expected["Ikey"] = "OLD-001";
            Assert.True(JToken.DeepEquals(expected, JToken.Parse(call.Body!)), call.Body);
            var parts = call.Headers["Authentication"].Split(':'); Assert.Equal(5, parts.Length); Assert.Equal("synthetic-user", parts[3]); Assert.Equal("synthetic-secret", parts[4]);
            Assert.Equal(new Clock().GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), parts[2]);
            Assert.True(Guid.TryParseExact(parts[1], "N", out _));
            Assert.Equal(Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("POST" + parts[2] + parts[1]))), parts[0]);
            Assert.Equal("fr-FR", CultureInfo.CurrentCulture.Name);
            var text = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-secret", text);
            using var json = JsonDocument.Parse(text);
            if (route is "create" or "draft") Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data")[0].GetProperty("providerInvoiceNumber").ValueKind);
            if (route is "document" or "preview") Assert.StartsWith("%PDF-", Encoding.UTF8.GetString(Convert.FromBase64String(json.RootElement.GetProperty("data").GetProperty("content").GetString()!)));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData("{\"Status\":1,\"ErrorCode\":\"ERROR\",\"Message\":\"synthetic-user synthetic-secret invalid tax\"}",422)]
    [InlineData("{\"Status\":2,\"Data\":{\"Invoices\":[]}}",502)]
    [InlineData("{\"Status\":2,\"Data\":\"wrong shape\"}",502)]
    [InlineData("{\"Status\":2,\"Data\":{\"Invoices\":[{\"Ikey\":\"OTHER\",\"No\":\"1\"}]}}",502)]
    [InlineData("{\"Status\":2,\"Data\":{\"Invoices\":[{\"Ikey\":\"INV-001\",\"No\":\"0\"}]}}",502)]
    [InlineData("not JSON",502)]
    [InlineData("",502)]
    public async Task Write_error_persists_safe_pending_diagnostics_and_blocks_retry(string supplierBody, int status)
    {
        using var factory = Factory(_ => Json(supplierBody)); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-error-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)first.StatusCode); var text = await first.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-secret", text); Assert.DoesNotContain("synthetic-user", text);
        if (status == 422) Assert.Contains("invalid tax", text);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/easy-error-001");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync()); Assert.Equal("pending", json.RootElement.GetProperty("state").GetString()); Assert.Equal("EASY_INVOICE", json.RootElement.GetProperty("provider").GetString()); Assert.False(json.RootElement.GetProperty("canRetry").GetBoolean());
        using var retry = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode); Assert.Single(factory.Calls);
        Assert.DoesNotContain("synthetic-secret", string.Join('\n', factory.Logs));
    }

    [Fact]
    public async Task Completed_publish_replays_and_cannot_be_signed_again_with_new_key()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-success-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-sign-002");
        using var sign = await client.PostAsync("/api/eInvoice/publish-existing", MisaApiTestFactory.Body(new PublishExistingInvoiceRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" }));
        Assert.Equal(HttpStatusCode.Conflict, sign.StatusCode); Assert.Single(factory.Calls);
    }

    [Theory]
    [InlineData("{\"Status\":1,\"Message\":\"synthetic-secret missing\"}",422)]
    [InlineData("{\"Status\":2}",502)]
    [InlineData("<html>error</html>",502)]
    public async Task Error_body_is_never_returned_as_pdf(string body, int status)
    {
        using var factory = Factory(_ => Json(body)); using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceType = 0, InvoiceId = "INV-001", Serial = "C26TAA" }));
        Assert.Equal(status, (int)response.StatusCode); Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Timeout_keeps_claim_and_never_echoes_exception_or_retries()
    {
        using var factory = Factory(_ => throw new TaskCanceledException("synthetic-secret")); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-timeout-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.GatewayTimeout, first.StatusCode); Assert.DoesNotContain("synthetic-secret", await first.Content.ReadAsStringAsync());
        using var retry = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode); Assert.Single(factory.Calls);
    }

    [Fact]
    public async Task Unknown_health_is_not_claimed_as_connected_and_unsupported_actions_do_not_call_supplier()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var health = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.NotImplemented, health.StatusCode);
        using var caps = await client.GetAsync("/api/eInvoice/capabilities"); using var json = JsonDocument.Parse(await caps.Content.ReadAsStringAsync()); Assert.Equal(12, json.RootElement.GetProperty("actions").GetArrayLength());
        client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-unsupported-001");
        using var batch = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest { Invoices = [Input()] })); Assert.Equal(HttpStatusCode.NotImplemented, batch.StatusCode);
        Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Invalid_original_reference_fails_before_dispatch_and_key_can_be_corrected()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "easy-validation-001");
        var data = Input(); data.OriginalInvoiceId = "wrong";
        using var invalid = await client.PostAsync("/api/eInvoice/replace", MisaApiTestFactory.Body(data)); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); Assert.Empty(factory.Calls);
        using var valid = await client.PostAsync("/api/eInvoice/replace", MisaApiTestFactory.Body(Input())); Assert.Equal(HttpStatusCode.OK, valid.StatusCode); Assert.Single(factory.Calls);
    }
}
