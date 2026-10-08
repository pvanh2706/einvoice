using System.Net;
using eInvoice.Providers.Misa;

namespace eInvoice.Tests.Misa;

public class MisaReceiptLoginClientTests
{
    [Fact]
    public async Task Receipt_login_preserves_external_keys_guid_header_and_escaped_primary_credentials()
    {
        var site = MisaTestData.Site.Site;
        site.Password = "synthetic+secret&unicode=Việt#x";
        var client = Client((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            var expected = "https://misa.example.test/receipt/user/token?appid=" + site.Id!.Value.ToString("D") +
                "&taxcode=TEST-TAX&username=synthetic-v3-user&password=" + Uri.EscapeDataString(site.Password);
            Assert.Equal(expected, request.RequestUri!.AbsoluteUri);
            Assert.Equal("CompanyTaxcode", Assert.Single(request.Headers).Key);
            Assert.Equal(site.Id.Value.ToString("D"), Assert.Single(request.Headers.GetValues("CompanyTaxcode")));
            Assert.DoesNotContain("synthetic-receipt-user", request.RequestUri.AbsoluteUri);
            return Task.FromResult(Response("{\"Success\":true,\"Data\":\"receipt-token\"}"));
        });
        Assert.Equal("receipt-token", await client.AuthenticateAsync(site));
    }

    [Theory]
    [InlineData("", ProviderFailure.EmptyResponse)]
    [InlineData("<html>synthetic-secret</html>", ProviderFailure.InvalidResponse)]
    [InlineData("{}", ProviderFailure.InvalidResponse)]
    [InlineData("null", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":false,\"Errors\":[\"synthetic-secret\"]}", ProviderFailure.Rejected)]
    public async Task Receipt_errors_are_parsed_on_non_2xx_without_exposing_payload(string body, ProviderFailure failure)
    {
        var client = Client((_, _) => Task.FromResult(Response(body)));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(MisaTestData.Site.Site));
        Assert.Equal(failure, error.Failure);
        Assert.DoesNotContain("synthetic-secret", error.Message);
    }

    [Fact]
    public async Task Receipt_endpoint_requires_allowlisted_https_and_complete_primary_credentials()
    {
        var client = Client((_, _) => throw new InvalidOperationException("No network"));
        var site = MisaTestData.Site.Site;
        site.PartnerUrl2 = "https://other.example.test/";
        Assert.Equal(ProviderFailure.Configuration, (await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(site))).Failure);
        site.PartnerUrl2 = MisaTestData.Site.Site.PartnerUrl2;
        site.Password = null;
        Assert.Equal(ProviderFailure.InvalidCredentials, (await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(site))).Failure);
    }

    private static MisaReceiptLoginClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        new(new HttpClient(new Handler(respond)), new MisaLoginOptions { AllowedHosts = ["misa.example.test"] });
    private static HttpResponseMessage Response(string body) => new(HttpStatusCode.Unauthorized) { Content = new StringContent(body) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }
}
