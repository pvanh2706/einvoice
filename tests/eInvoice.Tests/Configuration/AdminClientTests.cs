using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Providers.Admin;

namespace eInvoice.Tests.Configuration;

public class AdminClientTests
{
    [Fact]
    public async Task Admin_request_preserves_external_keys_and_utf16_and_parses_site()
    {
        var id = Guid.NewGuid();
        var handler = new FakeHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://admin.example.test/root/openapi/oa02-site/detail", request.RequestUri!.AbsoluteUri);
            Assert.Equal("utf-16", request.Content!.Headers.ContentType!.CharSet);
            var bytes = await request.Content.ReadAsByteArrayAsync(token);
            using var body = JsonDocument.Parse(Encoding.Unicode.GetString(bytes));
            Assert.Equal(id, body.RootElement.GetProperty("SiteId").GetGuid());
            Assert.Single(body.RootElement.EnumerateObject());
            return Json(HttpStatusCode.OK, """
                {"StatusCode":"200","Data":{"Site":{"Id":"11111111-1111-1111-1111-111111111111","Partner":6,
                "Username":"synthetic-user","Password":"synthetic-secret"},"ExtraDataMap":[{"Code":"arrival","ThirdPartyCode":"CheckIn"}]}}
                """);
        });
        var result = await Client(handler).GetSiteAsync(id);
        Assert.Equal(6, result.Site.Partner);
        Assert.Equal("synthetic-user", result.Site.Username);
        Assert.Equal("CheckIn", Assert.Single(result.ExtraFieldMappings).ThirdPartyCode);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(201)]
    public async Task Unsuccessful_admin_http_is_classified_without_leaking_body(int status)
    {
        var client = Client(new FakeHandler((_, _) => Task.FromResult(Json((HttpStatusCode)status, "synthetic-secret"))));
        var error = await Assert.ThrowsAsync<AdminLookupException>(() => client.GetSiteAsync(Guid.NewGuid()));
        Assert.Equal(AdminFailure.HttpError, error.Failure);
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.DoesNotContain("synthetic-secret", error.Message);
    }

    [Theory]
    [InlineData("not-json", AdminFailure.InvalidResponse)]
    [InlineData("{\"Data\":null}", AdminFailure.MissingSite)]
    [InlineData("{\"Data\":{\"Site\":null}}", AdminFailure.MissingSite)]
    public async Task Invalid_or_missing_admin_payload_is_classified(string payload, AdminFailure expected)
    {
        var client = Client(new FakeHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload))));
        var error = await Assert.ThrowsAsync<AdminLookupException>(() => client.GetSiteAsync(Guid.NewGuid()));
        Assert.Equal(expected, error.Failure);
    }

    [Fact]
    public async Task Transport_timeout_is_different_from_caller_cancellation()
    {
        var client = Client(new FakeHandler((_, _) => throw new TaskCanceledException("timeout", new TimeoutException())));
        var timeout = await Assert.ThrowsAsync<AdminLookupException>(() => client.GetSiteAsync(Guid.NewGuid()));
        Assert.Equal(AdminFailure.Timeout, timeout.Failure);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetSiteAsync(Guid.NewGuid(), cancellation.Token));
    }

    [Fact]
    public async Task Missing_configuration_fails_without_calling_handler()
    {
        using var http = new HttpClient(new FakeHandler((_, _) => throw new InvalidOperationException("Must not call network")));
        var error = await Assert.ThrowsAsync<AdminLookupException>(() => new AdminClient(http).GetSiteAsync(Guid.NewGuid()));
        Assert.Equal(AdminFailure.MissingConfiguration, error.Failure);
    }

    private static AdminClient Client(HttpMessageHandler handler) => new(new HttpClient(handler)
    {
        BaseAddress = new Uri("https://admin.example.test/root/"),
    });
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
