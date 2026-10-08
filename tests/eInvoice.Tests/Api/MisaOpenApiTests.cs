using System.Net;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Core;
using eInvoice.Providers.Misa;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace eInvoice.Tests.Api;

public class MisaOpenApiTests
{
    [Fact]
    public async Task Openapi_covers_every_implemented_business_route_and_new_contract_property()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/openapi/misa.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var sources = factory.Services.GetRequiredService<EndpointDataSource>();
        foreach (var endpoint in sources.Endpoints.OfType<RouteEndpoint>().Where(item => item.RoutePattern.RawText?.StartsWith("/api/eInvoice/", StringComparison.Ordinal) == true))
            Assert.True(paths.TryGetProperty(endpoint.RoutePattern.RawText!, out _), endpoint.RoutePattern.RawText);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var type in new[] { typeof(CreateInvoiceRequest), typeof(AdjustOrReplaceInvoiceRequest), typeof(InvoiceBatchRequest), typeof(Invoice), typeof(GetXmlHashRequest) })
        {
            var properties = schemas.GetProperty(type.Name).GetProperty("properties");
            var expected = type.GetProperties().Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name)).Order(StringComparer.Ordinal);
            Assert.Equal(expected, properties.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        }
        Assert.False(paths.GetProperty("/api/eInvoice/publish").GetProperty("post").GetProperty("parameters").EnumerateArray()
            .All(parameter => parameter.GetProperty("name").GetString() != "Idempotency-Key"));
        Assert.DoesNotContain("api/ezInvoice", document.RootElement.GetRawText());
        Assert.Equal(ProviderCapabilityCatalog.GetBaselineActions(ProviderId.Misa).OrderBy(action => (int)action),
            MisaImplementedCapabilities.Routes.Keys.OrderBy(action => (int)action));
        foreach (var route in MisaImplementedCapabilities.Routes.Values.Distinct())
            Assert.True(paths.TryGetProperty("/api/eInvoice/" + route, out _), route);
    }
}
