using System.Net;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Minvoice;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Minvoice;

public class MinvoiceTransportTests
{
    private static SiteConfiguration Site(string account = "one") => new()
    { PartnerUrl = "https://minvoice.example.test/", Username = account, Password = "synthetic-secret-" + account, Username2 = "unit-" + account };
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request); }
    private static MinvoiceOptions Options => new() { AllowedHosts = ["minvoice.example.test"] };

    [Fact]
    public async Task Concurrent_business_requests_never_share_token_or_credentials()
    {
        var calls = new System.Collections.Concurrent.ConcurrentBag<(string Token, string User)>();
        using var http = new HttpClient(new Handler(async request =>
        {
            var body = JObject.Parse(await request.Content!.ReadAsStringAsync());
            if (request.RequestUri!.AbsolutePath.EndsWith("Login", StringComparison.Ordinal))
            {
                await Task.Yield(); Assert.Equal("VP", body.Value<string>("ma_dvcs"));
                return Json("{\"token\":\"token-" + body.Value<string>("username") + "\"}");
            }
            Assert.Equal("Bear", request.Headers.Authorization!.Scheme);
            calls.Add((request.Headers.Authorization.Parameter!, body.Value<string>("user")!));
            return Json("{\"code\":\"00\"}");
        }));
        var transport = new MinvoiceTransport(http, Options);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(index => transport.ExecuteAsync(Site("user" + index), "api/InvoiceApi78/Save", new { user = "user" + index }, false, default)));
        Assert.Equal(20, calls.Count); Assert.All(calls, call => Assert.Equal("token-" + call.User, call.Token)); Assert.Null(http.DefaultRequestHeaders.Authorization);
    }

    [Theory]
    [InlineData("http://minvoice.example.test/")]
    [InlineData("https://other.example.test/")]
    [InlineData("https://minvoice.example.test/no-slash")]
    [InlineData("https://username@minvoice.example.test/")]
    public async Task Endpoint_guard_prevents_login_or_business_dispatch(string url)
    {
        var calls = 0; using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json("{}")); }));
        var site = Site(); site.PartnerUrl = url; var transport = new MinvoiceTransport(http, Options);
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(site, "api/InvoiceApi78/Save", new { }, true, default));
        Assert.Equal("MINVOICE_ENDPOINT_NOT_ALLOWED", error.Code); Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"token\":\"\"}")]
    [InlineData("{\"token\":{\"value\":\"synthetic-secret\"}}")]
    public async Task Invalid_login_never_reaches_business_endpoint(string response)
    {
        var calls = 0; using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json(response)); }));
        var transport = new MinvoiceTransport(http, Options);
        _ = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(Site(), "api/InvoiceApi78/Save", new { }, true, default)); Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Registry_requires_allowlist_before_even_logging_in()
    {
        var calls = 0; using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json("{}")); }));
        var transport = new MinvoiceTransport(http, Options);
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => transport.TaxCodeAsync(Site(), "synthetic-tax", default));
        Assert.Equal("MINVOICE_ENDPOINT_NOT_ALLOWED", error.Code); Assert.Equal(0, calls);
    }
}
