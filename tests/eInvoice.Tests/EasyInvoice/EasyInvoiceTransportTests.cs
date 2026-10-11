using System.Collections.Concurrent;
using System.Net;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.EasyInvoice;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.EasyInvoice;

public class EasyInvoiceTransportTests
{
    private static SiteConfiguration Site(string account = "one") => new() { PartnerUrl = "https://easyinvoice.example.test/", Username = account, Password = "synthetic-secret-" + account };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request); }
    private static EasyInvoiceOptions Options => new() { AllowedHosts = ["easyinvoice.example.test"] };
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Concurrent_sites_have_separate_authentication_and_nonce_without_default_headers()
    {
        var calls = new ConcurrentBag<(string User, string Secret, string Nonce)>();
        using var http = new HttpClient(new Handler(async request =>
        {
            var data = JObject.Parse(await request.Content!.ReadAsStringAsync()); var auth = request.Headers.GetValues("Authentication").Single().Split(':');
            Assert.Equal(data.Value<string>("user"), auth[3]); calls.Add((auth[3], auth[4], auth[1])); await Task.Yield(); return Json("{\"Status\":2}");
        }));
        var transport = new EasyInvoiceTransport(http, Options, TimeProvider.System);
        await Task.WhenAll(Enumerable.Range(0,20).Select(index => transport.ExecuteAsync(Site("user" + index), "api/publish/importInvoice", "{\"user\":\"user" + index + "\"}", false, default)));
        Assert.Equal(20, calls.Count); Assert.All(calls, call => Assert.Equal("synthetic-secret-" + call.User, call.Secret)); Assert.Equal(20, calls.Select(c => c.Nonce).Distinct().Count()); Assert.False(http.DefaultRequestHeaders.Contains("Authentication"));
    }

    [Theory]
    [InlineData("http://easyinvoice.example.test/")]
    [InlineData("https://other.example.test/")]
    [InlineData("https://easyinvoice.example.test/no-slash")]
    [InlineData("https://username@easyinvoice.example.test/")]
    [InlineData("https://easyinvoice.example.test/?password=secret")]
    public async Task Invalid_endpoint_fails_before_any_dispatch(string url)
    {
        var calls = 0; using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Json("{}")); })); var transport = new EasyInvoiceTransport(http, Options, TimeProvider.System);
        var site = Site(); site.PartnerUrl = url; var error = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(site, "api/publish/importInvoice", "{}", true, default)); Assert.Equal("EASY_INVOICE_ENDPOINT_NOT_ALLOWED", error.Code); Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Supplier_cannot_echo_authentication_into_error_message()
    {
        using var http = new HttpClient(new Handler(request => Task.FromResult(Json(new JObject { ["Status"] = 1, ["Message"] = request.Headers.GetValues("Authentication").Single() + " invalid account" }.ToString()))));
        var transport = new EasyInvoiceTransport(http, Options, TimeProvider.System); var error = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(Site(), "api/publish/importInvoice", "{}", true, default));
        Assert.Equal(ProviderFailure.Rejected, error.Failure); Assert.DoesNotContain("synthetic-secret", error.SupplierMessage!); Assert.DoesNotContain(":one:", error.SupplierMessage!); Assert.Contains("invalid account", error.SupplierMessage!);
    }

    [Fact]
    public async Task Network_failure_does_not_echo_exception_details()
    {
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("synthetic-secret"))); var transport = new EasyInvoiceTransport(http, Options, TimeProvider.System);
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => transport.ExecuteAsync(Site(), "api/publish/importInvoice", "{}", true, default)); Assert.Equal(ProviderFailure.Connection, error.Failure); Assert.DoesNotContain("synthetic-secret", error.Message);
    }
}
