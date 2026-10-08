using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Microsoft.Extensions.DependencyInjection;

namespace eInvoice.Tests.Api;

public sealed class JsonSiteConfigurationTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "einvoice-sites-" + Guid.NewGuid().ToString("N") + ".json");

    [Theory]
    [InlineData("/api/eInvoice/health-check")]
    [InlineData("/api/eInvoice/form-serials")]
    [InlineData("/api/eInvoice/document")]
    [InlineData("/api/eInvoice/publish")]
    public async Task Routes_resolve_server_json_and_contact_only_misa_without_admin(string route)
    {
        await Write(MisaTestData.Site.Site);
        using var factory = new MisaApiTestFactory { SiteFilePath = _path };
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "json-store-test-001");
        using var request = route switch
        {
            "/api/eInvoice/document" => MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceType = 0, SearchCode = "search-001" }),
            "/api/eInvoice/publish" => MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)),
            _ => new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        using var response = await client.PostAsync(route, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.Services.GetService<IAdminClient>());
        Assert.All(factory.Calls, call => Assert.StartsWith("https://misa.example.test/", call.Url));
        Assert.DoesNotContain("synthetic-v3-secret", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("missing", 503, "SITE_STORE_NOT_CONFIGURED")]
    [InlineData("invalid", 503, "SITE_STORE_INVALID_DATA")]
    [InlineData("unknown", 404, "SITE_CONFIGURATION_NOT_FOUND")]
    public async Task Configuration_errors_are_safe_json_and_never_call_a_provider(string mode, int status, string code)
    {
        if (mode == "invalid") await File.WriteAllTextAsync(_path, "invalid/synthetic-secret");
        if (mode == "unknown") await File.WriteAllTextAsync(_path, "{\"sites\":[]}");
        using var factory = new MisaApiTestFactory { SiteFilePath = _path };
        using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/health-check", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(status, (int)response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(text);
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("synthetic-secret", text);
        Assert.DoesNotContain(_path, text);
        Assert.Empty(factory.Calls);
    }

    private Task Write(SiteConfiguration site) => File.WriteAllTextAsync(_path, JsonSerializer.Serialize(
        new { sites = new[] { new SiteInfo { Site = site } } }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    public void Dispose() { if (File.Exists(_path)) File.Delete(_path); }
}
