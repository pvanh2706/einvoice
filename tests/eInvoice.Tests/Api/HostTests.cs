using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eInvoice.Tests.Api;

public class HostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public HostTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Liveness_starts_without_admin_or_provider_configuration()
    {
        using var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("eInvoice", body.RootElement.GetProperty("service").GetString());
        Assert.Equal("alive", body.RootElement.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("/api/ezInvoice/Create")]
    [InlineData("/api/eInvoice/Create")]
    public async Task Unknown_or_unimplemented_routes_return_json_error_without_claiming_invoice_support(string route)
    {
        using var response = await _client.PostAsync(route, new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("HTTP_404", body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("requestId").GetString()));
    }
}
