using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Sun;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;
using ProviderCall = eInvoice.Tests.Api.MisaApiTestFactory.ProviderCall;

namespace eInvoice.Tests.Api;

public class SunOperationTests
{
    private const string Id = "33333333-3333-3333-3333-333333333333";
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-10T18:00:00Z", CultureInfo.InvariantCulture); }
    private static MisaApiTestFactory Factory(Func<ProviderCall, HttpResponseMessage>? responder = null) => new()
    {
        Clock = new Clock(),
        Resolve = (_, _) => new ResolvedSite(new SiteConfiguration
        {
            Id = MisaTestData.SiteId, Partner = 3, PartnerUrl = "https://sun-cancel.example.test/",
            PartnerUrl2 = "https://sun.example.test/", TaxNumber = "TEST-TAX",
            Username = "synthetic-v3-user", Password = "synthetic-v3-secret",
        }, []),
        Respond = call => responder?.Invoke(call) ?? Default(call),
    };
    private static HttpResponseMessage Json(string value, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Default(ProviderCall call) => Json(call.Url.EndsWith("signin", StringComparison.Ordinal)
        ? "{\"token\":\"synthetic-sun-token\"}"
        : call.Url.EndsWith("searchpdf", StringComparison.Ordinal)
            ? "{\"pdf\":\"data:application/pdf;base64, c3ludGhldGljLXBkZg==\"}"
            : "{\"summary\":{\"error\":0},\"success\":[{\"seq\":\"000001\",\"sec\":\"SEARCH-001\"}]}");
    private static AdjustOrReplaceInvoiceRequest Input()
    {
        var request = MisaTestData.Invoice(id: Id); request.Invoice!.Sid = "old-sid"; request.Invoice.SearchCode = "SEARCH-001"; return request;
    }

    [Theory]
    [InlineData("publish", "publish", "savsigninv")]
    [InlineData("draft", "draft", "invtmp")]
    [InlineData("replace", "replace", "apiReplace")]
    [InlineData("adjust-increase", "adjust/increase", "adjniss")]
    [InlineData("adjust-decrease", "adjust/decrease", "adjniss")]
    [InlineData("preview", "preview", "searchpdf")]
    [InlineData("document", "document", "searchpdf")]
    [InlineData("draft-document", "document/draft", "searchpdf")]
    [InlineData("void", "void", "cancelHD")]
    public async Task Api_matches_frozen_wire_fixtures_with_invariant_numbers_and_utc7_date(string fixture, string route, string endpoint)
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-fixture-001");
        object body = route switch
        {
            "void" => new VoidInvoiceRequest { InvoiceType = 0, Sid = "old-sid", Reason = "Synthetic reason" },
            "document" or "document/draft" => new DownloadPdfRequest { InvoiceType = 0, SearchCode = "SEARCH-001" },
            _ => Input(),
        };
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            using var response = await client.PostAsync("/api/eInvoice/" + route, MisaApiTestFactory.Body(body, route is "draft" or "publish" ? typeof(CreateInvoiceRequest) : null));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Sun", "Payloads", fixture + ".json")));
            var call = factory.Calls.Last();
            var actual = route == "void" ? JObject.FromObject(call.Body!.Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1]))) : JToken.Parse(call.Body!);
            if (route == "void") expected = JObject.FromObject(((JObject)expected).Properties().ToDictionary(p => p.Name, p => p.Value.ToString()));
            Assert.True(JToken.DeepEquals(expected, actual), actual.ToString());
            Assert.EndsWith(endpoint, call.Url); Assert.StartsWith(route == "void" ? "https://sun-cancel.example.test/" : "https://sun.example.test/", call.Url);
            Assert.Equal("Bearer synthetic-sun-token", call.Headers["Authorization"]); Assert.Equal(2, factory.Calls.Count);
            var text = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("synthetic-sun-token", text);
            if (route == "publish") { Assert.Contains("33333333333333333333333333333333", text); Assert.Contains("000001", text); }
            if (route == "draft") Assert.DoesNotContain("000001", text);
            if (endpoint == "searchpdf") { Assert.Contains("c3ludGhldGljLXBkZg==", text); Assert.DoesNotContain("data:application", text); }
            Assert.Equal("fr-FR", CultureInfo.CurrentCulture.Name);
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Fact]
    public async Task Health_uses_one_fresh_login_and_catalogue_has_all_ten_baseline_actions()
    {
        using var factory = Factory(); using var client = factory.Client();
        using var health = await client.PostAsync("/api/eInvoice/health-check", MisaApiTestFactory.Body(new { }));
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Sun", "Payloads", "health-check.json")));
        Assert.True(JToken.DeepEquals(expected, JToken.Parse(factory.Calls.Single().Body!)));
        Assert.DoesNotContain("synthetic-sun-token", await health.Content.ReadAsStringAsync());
        using var caps = await client.GetAsync("/api/eInvoice/capabilities");
        using var json = JsonDocument.Parse(await caps.Content.ReadAsStringAsync());
        Assert.Equal(ProviderCapabilityCatalog.GetBaselineActions(ProviderId.Sun).Select(a => (int)a).Order(),
            json.RootElement.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("action").GetInt32()).Order());
        Assert.Single(factory.Calls);
    }

    [Fact]
    public async Task Duplicate_sid_cannot_be_bypassed_using_another_pms_id_or_key()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-publish-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode); Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var changed = Input(); changed.Invoice!.InvoiceId = Id + "OTHER";
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-publish-002");
        using var blocked = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(changed, typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode); Assert.Equal(2, factory.Calls.Count);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/sun-publish-001");
        Assert.Contains("completed", await lookup.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{\"summary\":{\"error\":1,\"des_err\":\"synthetic-v3-secret synthetic-sun-token invalid amount\"},\"success\":[]}",422)]
    [InlineData("{\"summary\":{\"error\":0},\"success\":[]}",502)]
    [InlineData("{\"summary\":{\"error\":0},\"success\":[{\"sid\":\"OTHER\",\"seq\":\"1\",\"sec\":\"SEARCH\"}]}",502)]
    [InlineData("{\"summary\":{\"error\":0},\"success\":[{}]}",502)]
    [InlineData("not-json",502)]
    [InlineData("",502)]
    public async Task Write_errors_remain_pending_with_safe_diagnostics_and_no_retry(string supplierBody, int status)
    {
        using var factory = Factory(call => call.Url.EndsWith("signin", StringComparison.Ordinal) ? Default(call) : Json(supplierBody));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-error-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(status, (int)first.StatusCode); var text = await first.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-v3-secret", text); Assert.DoesNotContain("synthetic-sun-token", text);
        using var lookup = await client.GetAsync("/api/eInvoice/operations/publish/sun-error-001");
        using var json = JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());
        Assert.Equal("pending", json.RootElement.GetProperty("state").GetString()); Assert.Equal("SUN", json.RootElement.GetProperty("provider").GetString());
        Assert.False(json.RootElement.GetProperty("canRetry").GetBoolean());
        using var again = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
        Assert.DoesNotContain("synthetic-v3-secret", string.Join('\n', factory.Logs));
    }

    [Fact]
    public async Task Invalid_short_id_is_rejected_before_login()
    {
        using var factory = Factory(); using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-invalid-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Timeout_after_dispatch_is_never_retried()
    {
        using var factory = Factory(call => call.Url.EndsWith("signin", StringComparison.Ordinal) ? Default(call) : throw new TaskCanceledException("synthetic secret"));
        using var client = factory.Client(); client.DefaultRequestHeaders.Add("Idempotency-Key", "sun-timeout-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.GatewayTimeout, first.StatusCode); Assert.DoesNotContain("synthetic secret", await first.Content.ReadAsStringAsync());
        using var again = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(Input(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode); Assert.Equal(2, factory.Calls.Count);
    }

    [Theory]
    [InlineData("%%%")]
    [InlineData("")]
    public async Task Invalid_pdf_cannot_be_returned_as_success(string content)
    {
        using var factory = Factory(call => call.Url.EndsWith("signin", StringComparison.Ordinal) ? Default(call) : Json(JsonSerializer.Serialize(new { pdf = content })));
        using var client = factory.Client(); using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceType = 0, SearchCode = "SEARCH-001" }));
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
