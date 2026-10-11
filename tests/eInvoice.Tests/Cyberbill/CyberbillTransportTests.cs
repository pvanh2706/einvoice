using System.Collections.Concurrent;
using System.Net;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Cyberbill;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Cyberbill;

public class CyberbillTransportTests
{
    private static SiteConfiguration Site(string user = "synthetic-user") => new()
    {
        Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Partner = 7,
        PartnerUrl = "https://cyberbill.example.test/", TaxNumber = "TEST-TAX", Username = user, Password = "synthetic-secret",
    };
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static CyberbillTransport Transport(Handler handler) => new(new HttpClient(handler), new CyberbillOptions { AllowedHosts = ["cyberbill.example.test"] });

    [Fact]
    public async Task Login_preserves_tax_key_and_token_is_sent_as_bearer_only_to_business_request()
    {
        using var handler = new Handler(call => call.Url.Contains("GetToken", StringComparison.Ordinal)
            ? Json("{\"result\":{\"access_token\":\"synthetic-token\"}}")
            : Json("{\"success\":true,\"result\":{\"maketqua\":\"01\",\"sohoadon\":\"000001\"}}"));
        var result = await Transport(handler).ExecuteAsync(Site(), "api/services/hddtws/GuiHoadon/GuiHoadonGoc", new { ma_hoadon = "INV-001" }, true, default);
        Assert.Equal("000001", result.Value<string>("sohoadon"));
        var calls = handler.Calls.ToArray(); Assert.Equal(2, calls.Length);
        var login = JObject.Parse(calls[0].Body);
        Assert.Equal(new[] { "username", "password", "doanhnghiep_mst" }, login.Properties().Select(p => p.Name));
        Assert.Null(calls[0].Authorization); Assert.Equal("Bearer synthetic-token", calls[1].Authorization);
        Assert.Equal("utf-8", calls[1].Encoding);
    }

    [Theory]
    [InlineData("{\"success\":false,\"error\":{\"details\":\"synthetic-secret invalid credentials\"}}")]
    [InlineData("{\"result\":{\"access_token\":123}}")]
    [InlineData("{\"result\":{\"access_token\":\"bad;token\"}}")]
    public async Task Invalid_login_does_not_dispatch_business_request(string response)
    {
        using var handler = new Handler(_ => Json(response));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => Transport(handler).ExecuteAsync(Site(), "api/create", new { }, true, default));
        Assert.DoesNotContain("synthetic-secret", error.SupplierMessage ?? ""); Assert.Single(handler.Calls);
    }

    [Theory]
    [InlineData("{\"success\":false,\"error\":{\"details\":\"synthetic-secret synthetic-token rejected\"}}", ProviderFailure.Rejected)]
    [InlineData("{\"success\":true,\"result\":{\"maketqua\":\"02\",\"motaketqua\":\"synthetic-secret bad amount\"}}", ProviderFailure.Rejected)]
    [InlineData("{\"success\":true,\"result\":{}}", ProviderFailure.InvalidResponse)]
    [InlineData("not-json", ProviderFailure.InvalidResponse)]
    [InlineData("", ProviderFailure.InvalidResponse)]
    public async Task Business_errors_never_echo_raw_response_or_credentials(string body, ProviderFailure failure)
    {
        using var handler = new Handler(call => call.Url.Contains("GetToken", StringComparison.Ordinal) ? Json("{\"result\":{\"access_token\":\"synthetic-token\"}}") : Json(body));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => Transport(handler).ExecuteAsync(Site(), "api/create", new { ma_hoadon = "INV-001" }, true, default));
        Assert.Equal(failure, error.Failure);
        Assert.DoesNotContain("synthetic-secret", error.SupplierMessage ?? ""); Assert.DoesNotContain("synthetic-token", error.SupplierMessage ?? "");
        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public async Task Parallel_tenants_never_share_token_or_mutable_default_headers()
    {
        using var handler = new Handler(call => call.Url.Contains("GetToken", StringComparison.Ordinal)
            ? Json("{\"result\":{\"access_token\":\"token-" + JObject.Parse(call.Body).Value<string>("username") + "\"}}")
            : Json("{\"success\":true,\"result\":{\"maketqua\":\"01\"}}"));
        var transport = Transport(handler);
        await Task.WhenAll(transport.ExecuteAsync(Site("tenant-A"), "api/create", new { ma_hoadon = "A" }, true, default),
            transport.ExecuteAsync(Site("tenant-B"), "api/create", new { ma_hoadon = "B" }, true, default));
        var calls = handler.Calls.Where(c => !c.Url.Contains("GetToken", StringComparison.Ordinal)).ToArray();
        Assert.Equal("Bearer token-tenant-A", calls.Single(c => JObject.Parse(c.Body).Value<string>("ma_hoadon") == "A").Authorization);
        Assert.Equal("Bearer token-tenant-B", calls.Single(c => JObject.Parse(c.Body).Value<string>("ma_hoadon") == "B").Authorization);
    }

    [Fact]
    public async Task Empty_unused_create_and_publish_endpoint_is_not_sent_to_base_url()
    {
        using var handler = new Handler(_ => throw new InvalidOperationException("Must not call provider"));
        await Assert.ThrowsAsync<UnsupportedProviderOperationException>(() => Transport(handler).ExecuteAsync(Site(), "", new { }, true, default));
        Assert.Empty(handler.Calls);
    }

    private sealed record Call(string Url, string Body, string? Authorization, string? Encoding);
    private sealed class Handler(Func<Call, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public ConcurrentQueue<Call> Calls { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = new Call(request.RequestUri!.AbsoluteUri, await request.Content!.ReadAsStringAsync(cancellationToken), request.Headers.Authorization?.ToString(), request.Content.Headers.ContentType?.CharSet);
            Calls.Enqueue(call); return respond(call);
        }
    }
}
