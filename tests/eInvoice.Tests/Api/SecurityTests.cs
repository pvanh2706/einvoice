using eInvoice.Providers.Common;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eInvoice.Tests.Api;

public class SecurityTests
{
    private const string CurrentKey = "synthetic-current-key-for-offline-tests";
    private const string NextKey = "synthetic-next-key-for-offline-tests";
    private static readonly Guid AllowedSite = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherSite = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-key")]
    public async Task Unauthenticated_requests_get_json_401_without_provider_calls(string? key)
    {
        using var factory = new TestFactory();
        using var client = factory.Client(key, AllowedSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        await AssertProblem(response, HttpStatusCode.Unauthorized, "HTTP_401");
        Assert.Equal(0, factory.Provider.Calls);
    }

    [Fact]
    public async Task Key_cannot_access_another_site()
    {
        using var factory = new TestFactory();
        using var client = factory.Client(CurrentKey, OtherSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        await AssertProblem(response, HttpStatusCode.Forbidden, "SITE_ACCESS_DENIED");
        Assert.Equal(0, factory.Provider.Calls);
    }

    [Fact]
    public async Task Missing_site_header_is_rejected_before_provider_call()
    {
        using var factory = new TestFactory();
        using var client = factory.Client(CurrentKey, AllowedSite);
        client.DefaultRequestHeaders.Remove("X-Site-Id");
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        await AssertProblem(response, HttpStatusCode.BadRequest, "SITE_ID_REQUIRED");
        Assert.Equal(0, factory.Provider.Calls);
    }

    [Fact]
    public async Task Body_configuration_cannot_override_authorized_site()
    {
        using var factory = new TestFactory();
        using var client = factory.Client(CurrentKey, AllowedSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new
        {
            siteConfigInfo = new { site = new { id = OtherSite, partner = 6 } },
        });
        await AssertProblem(response, HttpStatusCode.BadRequest, "SITE_CONFIGURATION_MISMATCH");
        Assert.Equal(0, factory.Provider.Calls);
    }

    [Theory]
    [InlineData(CurrentKey)]
    [InlineData(NextKey)]
    public async Task Rotation_keys_work_and_response_contains_no_provider_token(string key)
    {
        using var factory = new TestFactory();
        using var client = factory.Client(key, AllowedSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { requestId = "request-001" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-provider-token", json);
        Assert.Equal(1, factory.Provider.Calls);
        using var body = JsonDocument.Parse(json);
        Assert.True(body.RootElement.GetProperty("connected").GetBoolean());
        Assert.Equal(AllowedSite, body.RootElement.GetProperty("siteId").GetGuid());
    }

    [Fact]
    public async Task Expired_key_and_plain_http_are_rejected()
    {
        using var factory = new TestFactory(expired: true);
        using var client = factory.Client(CurrentKey, AllowedSite);
        using var expiredResponse = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);
        using var currentFactory = new TestFactory();
        using var plainClient = currentFactory.Client(CurrentKey, AllowedSite, secure: false);
        using var plainResponse = await plainClient.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, plainResponse.StatusCode);
    }

    [Theory]
    [InlineData(ProviderFailure.Timeout, 504)]
    [InlineData(ProviderFailure.Connection, 502)]
    [InlineData(ProviderFailure.Rejected, 422)]
    public async Task Provider_errors_have_safe_json_and_correct_http_status(ProviderFailure failure, int status)
    {
        using var factory = new TestFactory(failure: failure);
        using var client = factory.Client(CurrentKey, AllowedSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { });
        await AssertProblem(response, (HttpStatusCode)status, "SYNTHETIC_PROVIDER_ERROR");
        Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_body_key_returns_json_400_without_provider_call()
    {
        using var factory = new TestFactory();
        using var client = factory.Client(CurrentKey, AllowedSite);
        using var response = await client.PostAsJsonAsync("/api/eInvoice/health-check", new { RequestID = "old" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(0, factory.Provider.Calls);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("requestId").GetString()));
    }

    private sealed class TestFactory(bool expired = false, ProviderFailure? failure = null) : WebApplicationFactory<Program>
    {
        public FakeMisa Provider { get; } = new(failure);
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Clients:0:ClientId"] = "pms-test",
                ["Authentication:Clients:0:AllowedSiteIds:0"] = AllowedSite.ToString(),
                ["Authentication:Clients:0:Keys:0:KeyId"] = "current",
                ["Authentication:Clients:0:Keys:0:Sha256"] = Hash(CurrentKey),
                ["Authentication:Clients:0:Keys:0:ExpiresAtUtc"] = expired ? "2000-01-01T00:00:00Z" : "2099-01-01T00:00:00Z",
                ["Authentication:Clients:0:Keys:1:KeyId"] = "next",
                ["Authentication:Clients:0:Keys:1:Sha256"] = Hash(NextKey),
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISiteConfigurationResolver>();
                services.RemoveAll<IMisaLoginClient>();
                services.AddSingleton<ISiteConfigurationResolver>(new FakeResolver());
                services.AddSingleton<IMisaLoginClient>(Provider);
            });
        }
        public HttpClient Client(string? key, Guid siteId, bool secure = true)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(secure ? "https://localhost" : "http://localhost"),
            });
            if (key is not null) client.DefaultRequestHeaders.Add("X-Api-Key", key);
            client.DefaultRequestHeaders.Add("X-Site-Id", siteId.ToString());
            return client;
        }
        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private sealed class FakeResolver : ISiteConfigurationResolver
    {
        public Task<ResolvedSite> ResolveAsync(SiteInfo? suppliedConfiguration, Guid? siteId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResolvedSite(new SiteConfiguration { Id = siteId, Partner = 6 }, []));
    }
    public sealed class FakeMisa(ProviderFailure? failure) : IMisaLoginClient
    {
        public int Calls { get; private set; }
        public Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (failure is not null) throw new ProviderCallException(failure.Value, "SYNTHETIC_PROVIDER_ERROR", "synthetic-secret");
            return Task.FromResult("synthetic-provider-token");
        }
    }
}
