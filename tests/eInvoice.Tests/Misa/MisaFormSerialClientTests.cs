using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Misa;

public class MisaFormSerialClientTests
{
    private static SiteConfiguration Site => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Partner = 6,
        PartnerUrl = "https://misa.example.test/api/v3/", TaxNumber = "TEST-TAX",
        Username = "synthetic-user", Password = "synthetic-secret",
    };

    [Theory]
    [InlineData("normal")]
    [InlineData("nested")]
    [InlineData("escaped")]
    [InlineData("quoted")]
    public async Task Provider_variants_match_static_legacy_mapping_fixture(string encoding)
    {
        var data = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Misa", "form-serial-provider-data.json")).Trim();
        data = encoding switch
        {
            "nested" => JsonSerializer.Serialize(data),
            "escaped" => data.Replace("\"", "\\\"", StringComparison.Ordinal),
            "quoted" => "\"" + data + "\"",
            _ => data,
        };
        var client = Client((_, _) => Task.FromResult(Json(HttpStatusCode.BadRequest, Envelope(data))));
        var forms = await client.GetAsync(Site);
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Misa", "form-serial-expected.json")));
        var actual = JToken.Parse(JsonSerializer.Serialize(forms, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.True(JToken.DeepEquals(expected, actual), actual.ToString());
    }

    [Theory]
    [InlineData("2026-12-31T16:59:59Z", 2026)]
    [InlineData("2026-12-31T17:00:00Z", 2027)]
    public async Task Get_preserves_url_header_casing_no_body_and_vietnamese_year(string utc, int expectedYear)
    {
        var login = new FakeLogin();
        var client = Client((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"https://misa.example.test/api/v3/code/itg/InvoicePublishing/templates?invyear={expectedYear}",
                request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("synthetic-provider-token", request.Headers.Authorization.Parameter);
            Assert.Equal("CompanyTaxCode", Assert.Single(request.Headers,
                header => header.Key.Equals("CompanyTaxCode", StringComparison.OrdinalIgnoreCase)).Key);
            Assert.Equal("TEST-TAX", Assert.Single(request.Headers.GetValues("CompanyTaxCode")));
            Assert.Null(request.Content);
            return Task.FromResult(Json(HttpStatusCode.OK, Envelope("[]")));
        }, login, new FixedTime(DateTimeOffset.Parse(utc)));
        Assert.Empty(await client.GetAsync(Site));
        Assert.Equal(1, login.Calls);
        Assert.Equal(Site.Id, login.LastSite!.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Valid_json_escaped_quotes_are_not_corrupted(bool nested)
    {
        const string name = "Mẫu \"VAT\" tại C:\\MISA";
        var data = JsonSerializer.Serialize(new[] { new { TemplateName = name } });
        if (nested) data = JsonSerializer.Serialize(data);
        var client = Client((_, _) => Task.FromResult(Json(HttpStatusCode.OK, Envelope(data))));
        Assert.Equal(name, Assert.Single(await client.GetAsync(Site)).InvoiceName);
    }

    [Theory]
    [InlineData("", ProviderFailure.EmptyResponse)]
    [InlineData("<html>synthetic-secret</html>", ProviderFailure.InvalidResponse)]
    [InlineData("null", ProviderFailure.InvalidResponse)]
    [InlineData("{}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":\"null\"}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":\"{}\"}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":\"[null]\"}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":\"[{}]\"}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":\"[]\",\"Errors\":null}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":true,\"Data\":[]}", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":false,\"Errors\":[\"synthetic-secret\"]}", ProviderFailure.Rejected)]
    [InlineData("{\"Success\":true,\"Data\":\"[]\",\"ErrorCode\":\"synthetic-secret\"}", ProviderFailure.Rejected)]
    [InlineData("{\"Success\":true,\"Data\":\"[]\",\"Errors\":[\"synthetic-secret\"]}", ProviderFailure.Rejected)]
    public async Task Invalid_responses_are_classified_even_on_http_500_without_leaking_body(string body, ProviderFailure failure)
    {
        var client = Client((_, _) => Task.FromResult(Json(HttpStatusCode.InternalServerError, body)));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.GetAsync(Site));
        Assert.Equal(failure, error.Failure);
        Assert.DoesNotContain("synthetic-secret", error.Message);
        Assert.StartsWith("MISA_FORM_SERIAL_", error.Code);
    }

    [Theory]
    [InlineData("http://misa.example.test/")]
    [InlineData("https://unexpected.example.test/")]
    [InlineData("https://misa.example.test")]
    public async Task Disallowed_endpoint_is_rejected_before_login_or_get(string url)
    {
        var login = new FakeLogin();
        var client = Client((_, _) => throw new InvalidOperationException("Network must not be called"), login);
        var site = Site;
        site.PartnerUrl = url;
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.GetAsync(site));
        Assert.Equal("MISA_ENDPOINT_NOT_ALLOWED", error.Code);
        Assert.Equal(0, login.Calls);
    }

    [Fact]
    public async Task Login_failure_prevents_get_and_keeps_login_classification()
    {
        var login = new FakeLogin(new ProviderCallException(ProviderFailure.Rejected, "MISA_INVALID_CREDENTIALS", "Login rejected"));
        var client = Client((_, _) => throw new InvalidOperationException("GET must not be called"), login);
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.GetAsync(Site));
        Assert.Equal("MISA_INVALID_CREDENTIALS", error.Code);
    }

    [Fact]
    public async Task Http_timeout_and_caller_cancellation_are_distinct_and_do_not_retry()
    {
        var calls = 0;
        using var http = new HttpClient(new FakeHandler(async (_, token) =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("Delay should be cancelled");
        })) { Timeout = TimeSpan.FromMilliseconds(30) };
        var client = new MisaFormSerialClient(http, new FakeLogin(), Options(), TimeProvider.System);
        var timeout = await Assert.ThrowsAsync<ProviderCallException>(() => client.GetAsync(Site));
        Assert.Equal("MISA_FORM_SERIAL_TIMEOUT", timeout.Code);
        Assert.Equal(1, calls);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(Site, cancelled.Token));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Network_failure_does_not_retry_or_expose_transport_message()
    {
        var calls = 0;
        var client = Client((_, _) =>
        {
            calls++;
            throw new HttpRequestException("synthetic-secret");
        });
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.GetAsync(Site));
        Assert.Equal("MISA_FORM_SERIAL_CONNECTION_ERROR", error.Code);
        Assert.DoesNotContain("synthetic-secret", error.Message);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Concurrent_sites_keep_each_requests_token_and_tax_header_together()
    {
        var calls = 0;
        var client = Client(async (request, _) =>
        {
            await Task.Yield();
            var tax = Assert.Single(request.Headers.GetValues("CompanyTaxCode"));
            Assert.Equal(tax, request.Headers.Authorization!.Parameter);
            Interlocked.Increment(ref calls);
            return Json(HttpStatusCode.OK, Envelope("[]"));
        }, new FakeLogin(useTaxAsToken: true));
        var siteA = Site;
        var siteB = Site;
        siteA.TaxNumber = "TEST-A";
        siteB.Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        siteB.TaxNumber = "TEST-B";
        await Task.WhenAll(client.GetAsync(siteA), client.GetAsync(siteB));
        Assert.Equal(2, calls);
    }

    private static MisaLoginOptions Options() => new() { AllowedHosts = ["misa.example.test"] };
    private static MisaFormSerialClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
        FakeLogin? login = null, TimeProvider? clock = null) =>
        new(new HttpClient(new FakeHandler(respond)), login ?? new FakeLogin(), Options(), clock ?? TimeProvider.System);
    private static string Envelope(string data) => JsonSerializer.Serialize(new { Success = true, Data = data, Errors = Array.Empty<string>() });
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };
    private sealed class FixedTime(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    private sealed class FakeLogin(ProviderCallException? failure = null, bool useTaxAsToken = false) : IMisaLoginClient
    {
        public int Calls { get; private set; }
        public SiteConfiguration? LastSite { get; private set; }
        public Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastSite = site;
            if (failure is not null) throw failure;
            return Task.FromResult(useTaxAsToken ? site.TaxNumber! : "synthetic-provider-token");
        }
    }
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
