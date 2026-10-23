using System.Net;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Sun;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Sun;

public class SunTransportTests
{
    private static SiteConfiguration Site(string account = "one") => new()
    { PartnerUrl = "https://cancel.example.test/", PartnerUrl2 = "https://sun.example.test/", Username = account, Password = "synthetic-secret-" + account, TaxNumber = "synthetic-tax-" + account };
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request); }
    private static SunOptions Options => new() { AllowedHosts = ["sun.example.test", "cancel.example.test"] };

    [Fact]
    public async Task Concurrent_sites_never_share_token_or_default_headers()
    {
        var calls = new System.Collections.Concurrent.ConcurrentBag<(string Token, string User)>();
        using var http = new HttpClient(new Handler(async request =>
        {
            var body = JObject.Parse(await request.Content!.ReadAsStringAsync());
            if (request.RequestUri!.AbsolutePath.EndsWith("signin", StringComparison.Ordinal))
            {
                await Task.Yield();
                return Json("{\"token\":\"token-" + body.Value<string>("username") + "\"}");
            }
            calls.Add((request.Headers.Authorization!.Parameter!, body.Value<string>("user")!));
            return Json("{\"summary\":{\"error\":0},\"success\":[]}");
        }));
        var transport = new SunTransport(http, Options);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(index => transport.ExecuteAsync(Site("user" + index), "api/savsigninv", new { user = "user" + index }, false, default)));
        Assert.Equal(20, calls.Count); Assert.All(calls, call => Assert.Equal("token-" + call.User, call.Token));
        Assert.Null(http.DefaultRequestHeaders.Authorization);
    }

    [Theory]
    [InlineData("http://sun.example.test/")]
    [InlineData("https://other.example.test/")]
    [InlineData("https://sun.example.test/no-slash")]
    [InlineData("https://username@sun.example.test/")]
    public async Task Invalid_business_endpoint_is_rejected_before_any_login(string url)
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json("{}")); }));
        var transport = new SunTransport(http, Options); var site = Site(); site.PartnerUrl = url;
        var exception = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(site, "api/cancelHD", new { }, true, default));
        Assert.Equal("SUN_ENDPOINT_NOT_ALLOWED", exception.Code); Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"token\":\"\"}")]
    [InlineData("{\"token\":{\"value\":\"synthetic-secret\"}}")]
    public async Task Missing_or_non_string_login_token_never_dispatches_business_operation(string response)
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json(response)); }));
        var transport = new SunTransport(http, Options);
        _ = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(Site(), "api/savsigninv", new { }, true, default));
        Assert.Equal(1, calls);
    }
}
