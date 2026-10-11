using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;
using ProviderCall = eInvoice.Tests.Api.MisaApiTestFactory.ProviderCall;

namespace eInvoice.Tests.Api;

public class CyberbillOperationTests
{
    private static MisaApiTestFactory Factory(Func<ProviderCall, HttpResponseMessage>? responder = null) => new()
    {
        Resolve = (_, _) => new ResolvedSite(new SiteConfiguration
        {
            Id = MisaTestData.SiteId, Partner = 7, PartnerUrl = "https://cyberbill.example.test/",
            TaxNumber = "TEST-TAX", Username = "synthetic-user", Password = "synthetic-password",
        }, []),
        Respond = call => responder?.Invoke(call) ?? Json(call.Url.EndsWith("GetToken", StringComparison.Ordinal)
            ? "{\"result\":{\"access_token\":\"synthetic-token\"}}"
            : "{\"success\":true,\"result\":{\"maketqua\":\"01\",\"base64pdf\":\"c3ludGhldGljLXBkZg==\"}}"),
    };
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
    { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private static VoidInvoiceRequest Void() => new()
    { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0, FormNo = "1", Serial = "C26TAA", InvoiceDate = "08/10/2026" };

    [Fact]
    public async Task Health_and_catalogue_only_advertise_finished_operations_without_token()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var health = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var text = await health.Content.ReadAsStringAsync();
        Assert.Contains("CYBER_BILL", text); Assert.DoesNotContain("synthetic-token", text);
        using var caps = await client.GetAsync("/api/eInvoice/capabilities");
        using var json = JsonDocument.Parse(await caps.Content.ReadAsStringAsync());
        Assert.Equal(4, json.RootElement.GetProperty("actions").GetArrayLength());
        Assert.DoesNotContain("publish", json.RootElement.GetRawText());
        Assert.Single(factory.Calls);
    }

    [Theory]
    [InlineData("document")]
    [InlineData("void")]
    public async Task Existing_routes_preserve_wire_fixtures_and_authentication(string route)
    {
        using var factory = Factory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "cyberbill-fixture-001");
        object body = route == "void" ? Void() : new DownloadPdfRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 };
        using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cyberbill", "Payloads", route + ".json")));
        var call = factory.Calls.Last();
        Assert.True(JToken.DeepEquals(expected, JToken.Parse(call.Body!)), call.Body);
        Assert.Equal("Bearer synthetic-token", call.Headers["Authorization"]);
        Assert.Equal("utf-8", call.Encoding); Assert.Equal(2, factory.Calls.Count);
        Assert.EndsWith(route == "void" ? "/GuiHoadonHuyBo" : "/TaiHoaDonPDF", call.Url);
    }

    [Fact]
    public async Task Void_replays_completed_result_and_blocks_new_key_without_supplier_call()
    {
        using var factory = Factory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "cyberbill-void-001");
        using var first = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(Void()));
        using var replay = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(Void()));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var status = await client.GetAsync("/api/eInvoice/operations/void/cyberbill-void-001");
        var text = await status.Content.ReadAsStringAsync();
        Assert.Contains("completed", text); Assert.Contains("CYBER_BILL", text);
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyberbill-void-002");
        using var blocked = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(Void()));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode); Assert.Equal(2, factory.Calls.Count);
    }

    [Fact]
    public async Task Supplier_rejection_is_sanitized_saved_and_not_retried()
    {
        using var factory = Factory(call => Json(call.Url.EndsWith("GetToken", StringComparison.Ordinal)
            ? "{\"result\":{\"access_token\":\"synthetic-token\"}}"
            : "{\"success\":true,\"result\":{\"maketqua\":\"02\",\"motaketqua\":\"synthetic-password synthetic-token invalid invoice\"}}"));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "cyberbill-error-001");
        using var first = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(Void()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, first.StatusCode);
        var text = await first.Content.ReadAsStringAsync();
        Assert.Contains("invalid invoice", text); Assert.DoesNotContain("synthetic-password", text); Assert.DoesNotContain("synthetic-token", text);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/void/cyberbill-error-001");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("pending", json.RootElement.GetProperty("state").GetString());
        Assert.Equal(200, json.RootElement.GetProperty("lastError").GetProperty("providerHttpStatus").GetInt32());
        using var again = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(Void()));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
        Assert.DoesNotContain("synthetic-password", string.Join('\n', factory.Logs));
    }

    [Theory]
    [InlineData("", 502)]
    [InlineData("%%%", 502)]
    public async Task Document_does_not_return_missing_or_invalid_base64(string content, int status)
    {
        using var factory = Factory(call => Json(call.Url.EndsWith("GetToken", StringComparison.Ordinal)
            ? "{\"result\":{\"access_token\":\"synthetic-token\"}}"
            : JsonSerializer.Serialize(new { success = true, result = new { maketqua = "01", base64pdf = content } })));
        using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 }));
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task Unsupported_publish_fails_before_login_or_dispatch()
    {
        using var factory = Factory(); using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "cyberbill-publish-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode); Assert.Empty(factory.Calls);
    }
}
