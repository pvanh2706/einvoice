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

public class FormSerialTests
{
    private const string Route = "/api/eInvoice/form-serials";
    private const string Key = "synthetic-form-serial-offline-key";
    private static readonly Guid SiteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherSite = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Default_type_runs_real_login_and_mapping_through_fake_provider_http()
    {
        using var factory = new TestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync(Route, new { requestId = "pms-001" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-secret", json);
        Assert.DoesNotContain("synthetic-token", json);
        using var body = JsonDocument.Parse(json);
        Assert.Equal("MISA", body.RootElement.GetProperty("provider").GetString());
        Assert.Equal(SiteId, body.RootElement.GetProperty("siteId").GetGuid());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("requestId").GetString()));
        var form = Assert.Single(body.RootElement.GetProperty("forms").EnumerateArray());
        Assert.Equal("Mẫu thử", form.GetProperty("invoiceName").GetString());
        Assert.Equal("1", form.GetProperty("formNo").GetString());
        Assert.Equal("C26TAA", form.GetProperty("serial").GetString());
        Assert.Equal(1, form.GetProperty("status").GetInt32());
        Assert.True(form.GetProperty("isMultipleTaxRates").GetBoolean());
        Assert.Equal(1, factory.LoginCalls);
        Assert.Equal(1, factory.GetCalls);
        Assert.Equal(TimeSpan.FromSeconds(90), factory.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IMisaFormSerialClient)).Timeout);
    }

    [Theory]
    [InlineData("{\"invoiceType\":1}")]
    [InlineData("{\"invoiceType\":2}")]
    [InlineData("{\"invoiceType\":null}")]
    [InlineData("{\"invoiceType\":-1}")]
    public async Task Unsupported_types_return_400_before_config_or_provider(string json)
    {
        using var factory = new TestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertProblem(response, 400, "INVOICE_TYPE_NOT_SUPPORTED");
        Assert.Equal(0, factory.ResolverCalls);
        Assert.Equal(0, factory.LoginCalls);
        Assert.Equal(0, factory.GetCalls);
    }

    [Theory]
    [InlineData("{\"InvoiceType\":0}")]
    [InlineData("{\"invoiceType\":\"0\"}")]
    [InlineData("{\"RequestID\":\"old\"}")]
    public async Task Old_or_invalid_keys_return_json_400_without_provider_calls(string json)
    {
        using var factory = new TestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(0, factory.LoginCalls);
        Assert.Equal(0, factory.GetCalls);
    }

    [Theory]
    [InlineData("no-key", 401, "HTTP_401")]
    [InlineData("other-site", 403, "SITE_ACCESS_DENIED")]
    [InlineData("missing-site", 400, "SITE_ID_REQUIRED")]
    [InlineData("body-mismatch", 400, "SITE_CONFIGURATION_MISMATCH")]
    [InlineData("plain-http", 401, "HTTP_401")]
    public async Task Site_scope_is_enforced_before_config_or_provider(string mode, int status, string code)
    {
        using var factory = new TestFactory();
        using var client = factory.Client(mode);
        var request = mode == "body-mismatch"
            ? (object)new { siteConfigInfo = new { site = new { id = OtherSite, partner = 6 } } }
            : new { };
        using var response = await client.PostAsJsonAsync(Route, request);
        await AssertProblem(response, status, code);
        Assert.Equal(0, factory.ResolverCalls);
        Assert.Equal(0, factory.LoginCalls);
        Assert.Equal(0, factory.GetCalls);
    }

    [Theory]
    [InlineData("rejected", 422, "MISA_FORM_SERIAL_REJECTED")]
    [InlineData("malformed", 502, "MISA_FORM_SERIAL_INVALID_RESPONSE")]
    [InlineData("empty", 502, "MISA_FORM_SERIAL_EMPTY_RESPONSE")]
    [InlineData("timeout", 504, "MISA_FORM_SERIAL_TIMEOUT")]
    [InlineData("connection", 502, "MISA_FORM_SERIAL_CONNECTION_ERROR")]
    [InlineData("login-error", 422, "MISA_INVALID_CREDENTIALS")]
    public async Task Provider_failures_use_safe_problem_details(string mode, int status, string code)
    {
        using var factory = new TestFactory(mode);
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync(Route, new { });
        await AssertProblem(response, status, code);
        Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, factory.LoginCalls);
        Assert.Equal(mode == "login-error" ? 0 : 1, factory.GetCalls);
    }

    [Theory]
    [InlineData(false, 501, "PROVIDER_NOT_IMPLEMENTED")]
    [InlineData(true, 400, "INVALID_SITE_CONFIGURATION")]
    public async Task Unsupported_provider_and_inconsistent_resolver_cannot_call_misa(bool mismatchedSite, int status, string code)
    {
        using var factory = new TestFactory(mismatchedSite ? "resolver-mismatch" : "other-provider");
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync(Route, new { });
        await AssertProblem(response, status, code);
        Assert.Equal(0, factory.LoginCalls);
        Assert.Equal(0, factory.GetCalls);
    }

    [Fact]
    public async Task Authorized_body_config_reaches_the_resolver()
    {
        using var factory = new TestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync(Route, new
        {
            invoiceType = 0,
            siteConfigInfo = new { site = new { id = SiteId, name = "Synthetic hotel", partner = 6 } },
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Synthetic hotel", factory.SuppliedConfiguration!.Site!.Name);
    }

    private static async Task AssertProblem(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("requestId").GetString()));
    }

    private sealed class TestFactory(string mode = "success") : WebApplicationFactory<Program>, ISiteConfigurationResolver
    {
        public int LoginCalls { get; private set; }
        public int GetCalls { get; private set; }
        public int ResolverCalls { get; private set; }
        public SiteInfo? SuppliedConfiguration { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Clients:0:ClientId"] = "form-serial-test",
                ["Authentication:Clients:0:AllowedSiteIds:0"] = SiteId.ToString(),
                ["Authentication:Clients:0:Keys:0:KeyId"] = "synthetic",
                ["Authentication:Clients:0:Keys:0:Sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Key))),
                ["Authentication:Clients:0:Keys:0:ExpiresAtUtc"] = "2099-01-01T00:00:00Z",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISiteConfigurationResolver>();
                services.AddSingleton<ISiteConfigurationResolver>(this);
                services.RemoveAll<MisaLoginOptions>();
                services.AddSingleton(new MisaLoginOptions { AllowedHosts = ["misa.example.test"] });
                services.AddHttpClient<IMisaLoginClient, MisaLoginClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler((_, _) =>
                    {
                        LoginCalls++;
                        return Task.FromResult(Response(mode == "login-error"
                            ? "{\"Success\":false,\"ErrorCode\":\"UnAuthorize\",\"Errors\":[\"MisaIdError\"]}"
                            : "{\"Success\":true,\"Data\":\"synthetic-token\"}"));
                    }));
                services.AddHttpClient<IMisaFormSerialClient, MisaFormSerialClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler((request, _) =>
                    {
                        GetCalls++;
                        Assert.Equal("Bearer synthetic-token", request.Headers.Authorization!.ToString());
                        if (mode == "timeout") throw new TaskCanceledException("synthetic-secret");
                        if (mode == "connection") throw new HttpRequestException("synthetic-secret");
                        var data = JsonSerializer.Serialize(new[] { new { TemplateName = "Mẫu thử", InvTemplateNo = "1", InvSeries = "C26TAA", BusinessAreas = 34 } });
                        var body = mode switch
                        {
                            "rejected" => "{\"Success\":false,\"Errors\":[\"synthetic-secret\"]}",
                            "malformed" => "<html>synthetic-secret</html>",
                            "empty" => "",
                            _ => JsonSerializer.Serialize(new { Success = true, Data = data }),
                        };
                        return Task.FromResult(Response(body));
                    }));
            });
        }

        public Task<ResolvedSite> ResolveAsync(SiteInfo? suppliedConfiguration, Guid? siteId, CancellationToken cancellationToken = default)
        {
            ResolverCalls++;
            SuppliedConfiguration = suppliedConfiguration;
            return Task.FromResult(new ResolvedSite(new SiteConfiguration
            {
                Id = mode == "resolver-mismatch" ? OtherSite : siteId,
                Partner = mode == "other-provider" ? 999 : 6,
                PartnerUrl = "https://misa.example.test/api/v3/", TaxNumber = "TEST-TAX",
                Username = "synthetic-user", Password = "synthetic-secret",
            }, []));
        }

        public HttpClient Client(string access = "authorized")
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(access == "plain-http" ? "http://localhost" : "https://localhost"),
            });
            if (access != "no-key") client.DefaultRequestHeaders.Add("X-Api-Key", Key);
            if (access != "missing-site") client.DefaultRequestHeaders.Add("X-Site-Id", (access == "other-site" ? OtherSite : SiteId).ToString());
            return client;
        }

        private static HttpResponseMessage Response(string body) => new(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                respond(request, cancellationToken);
        }
    }
}
