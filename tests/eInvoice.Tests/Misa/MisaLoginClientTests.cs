using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;

namespace eInvoice.Tests.Misa;

public class MisaLoginClientTests
{
    private static SiteConfiguration Site => new()
    {
        Id = Guid.NewGuid(), Partner = 6, PartnerUrl = "https://misa.example.test/api/v3/",
        TaxNumber = "TEST-TAX", Username = "synthetic-user", Password = "synthetic-secret",
    };

    [Fact]
    public async Task Login_preserves_provider_keys_header_encoding_and_parses_body_before_http_status()
    {
        var client = Client(async (request, token) =>
        {
            Assert.Equal("https://misa.example.test/api/v3/auth/token", request.RequestUri!.AbsoluteUri);
            Assert.Equal("TEST-TAX", Assert.Single(request.Headers.GetValues("CompanyTaxcode")));
            Assert.Equal("utf-8", request.Content!.Headers.ContentType!.CharSet);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            Assert.Equal(new[] { "appid", "taxcode", "username", "password" },
                body.RootElement.EnumerateObject().Select(property => property.Name));
            Assert.Equal("9B226ED5-8653-11ED-8091-005056A6F699", body.RootElement.GetProperty("appid").GetString());
            Assert.Equal("synthetic-secret", body.RootElement.GetProperty("password").GetString());
            return Json(HttpStatusCode.BadRequest, "{\"Success\":true,\"Data\":\"synthetic-token\"}");
        });
        Assert.Equal("synthetic-token", await client.AuthenticateAsync(Site));
    }

    [Theory]
    [InlineData("UserNotExist", "MISA_ACCOUNT_NOT_AUTHORIZED")]
    [InlineData("MisaIdError", "MISA_INVALID_CREDENTIALS")]
    public async Task Business_errors_are_parsed_even_on_http_401(string error, string expected)
    {
        var client = Client((_, _) => Task.FromResult(Json(HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(new { Success = false, ErrorCode = "UnAuthorize", Errors = new[] { error } }))));
        var exception = await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(Site));
        Assert.Equal(expected, exception.Code);
        Assert.Equal(ProviderFailure.Rejected, exception.Failure);
    }

    [Theory]
    [InlineData("", ProviderFailure.EmptyResponse)]
    [InlineData("<html>synthetic-secret</html>", ProviderFailure.InvalidResponse)]
    [InlineData("null", ProviderFailure.InvalidResponse)]
    [InlineData("{\"Success\":false,\"Errors\":[\"synthetic-secret\"]}", ProviderFailure.Rejected)]
    public async Task Invalid_content_is_classified_without_leaking_body(string payload, ProviderFailure expected)
    {
        var client = Client((_, _) => Task.FromResult(Json(HttpStatusCode.InternalServerError, payload)));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(Site));
        Assert.Equal(expected, error.Failure);
        Assert.DoesNotContain("synthetic-secret", error.Message);
    }

    [Theory]
    [InlineData("https://unexpected.example.test/")]
    [InlineData("https://misa.example.test")]
    [InlineData("http://misa.example.test/")]
    [InlineData("https://misa.example.test/?redirect=elsewhere")]
    public async Task Invalid_endpoint_is_rejected_before_credentials_are_sent(string endpoint)
    {
        var site = Site;
        site.PartnerUrl = endpoint;
        var client = Client((_, _) => throw new InvalidOperationException("Network must not be called"));
        var error = await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(site));
        Assert.Equal(ProviderFailure.Configuration, error.Failure);
    }

    [Fact]
    public async Task Timeout_and_caller_cancellation_are_distinct()
    {
        var client = Client((_, _) => throw new TaskCanceledException("timeout", new TimeoutException()));
        var timeout = await Assert.ThrowsAsync<ProviderCallException>(() => client.AuthenticateAsync(Site));
        Assert.Equal(ProviderFailure.Timeout, timeout.Failure);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.AuthenticateAsync(Site, cancellation.Token));
    }

    private static MisaLoginClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        new(new HttpClient(new FakeHandler(respond)), new MisaLoginOptions { AllowedHosts = ["misa.example.test"] });
    private static HttpResponseMessage Json(HttpStatusCode status, string payload) => new(status)
    {
        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
    };
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
