using System.Net;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;

namespace eInvoice.Tests.Api;

public class MisaAttemptDiagnosticsTests
{
    [Fact]
    public async Task Supplier_item_error_is_redacted_persisted_and_returned_in_later_invoice_conflicts_without_resending()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call =>
        {
            var response = call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("synthetic-v3-token") :
                MisaApiTestFactory.Envelope(new[] { new { RefID = "INV-001", ErrorCode = "InvalidParameter",
                    DescriptionErrorCode = "Ký hiệu không tồn tại. password=synthetic-v3-secret, username=synthetic-v3-user, Bearer synthetic-v3-token" } });
            response.StatusCode = HttpStatusCode.OK;
            return response;
        };
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "diagnostic-original-001");
        using var failed = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, failed.StatusCode);
        var failureText = await failed.Content.ReadAsStringAsync();
        AssertNoSecrets(failureText);
        using var failure = JsonDocument.Parse(failureText);
        Assert.Equal("InvalidParameter", failure.RootElement.GetProperty("providerCode").GetString());
        Assert.Contains("Ký hiệu không tồn tại", failure.RootElement.GetProperty("providerMessage").GetString());
        Assert.Equal(200, failure.RootElement.GetProperty("providerHttpStatus").GetInt32());
        Assert.False(failure.RootElement.GetProperty("attempt").GetProperty("canRetry").GetBoolean());
        Assert.Equal("/api/eInvoice/operations/publish/diagnostic-original-001", failure.RootElement.GetProperty("attemptStatusUrl").GetString());

        using var repeated = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        using var repeatedBody = JsonDocument.Parse(await repeated.Content.ReadAsStringAsync());
        Assert.Equal("MISA_OUTCOME_UNCERTAIN", repeatedBody.RootElement.GetProperty("code").GetString());
        Assert.Equal("InvalidParameter", repeatedBody.RootElement.GetProperty("previousAttempt").GetProperty("lastError").GetProperty("providerCode").GetString());
        Assert.Equal("/api/eInvoice/operations/publish/diagnostic-original-001", repeatedBody.RootElement.GetProperty("attemptStatusUrl").GetString());

        using var lookup = await client.GetAsync("/api/eInvoice/invoice-attempts?invoiceId=INV-001");
        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
        var lookupText = await lookup.Content.ReadAsStringAsync();
        AssertNoSecrets(lookupText);
        using var invoice = JsonDocument.Parse(lookupText);
        var prior = invoice.RootElement;
        Assert.Equal("pending", prior.GetProperty("state").GetString());
        Assert.Equal("diagnostic-original-001", prior.GetProperty("idempotencyKey").GetString());
        Assert.Equal("InvalidParameter", prior.GetProperty("lastError").GetProperty("providerCode").GetString());
        Assert.Equal(failure.RootElement.GetProperty("requestId").GetString(), prior.GetProperty("originalRequestId").GetString());

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "diagnostic-changed-001");
        using var blocked = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var blockedText = await blocked.Content.ReadAsStringAsync();
        AssertNoSecrets(blockedText);
        using var conflict = JsonDocument.Parse(blockedText);
        Assert.Equal("gateway", conflict.RootElement.GetProperty("errorSource").GetString());
        Assert.Equal("diagnostic-original-001", conflict.RootElement.GetProperty("previousAttempt").GetProperty("idempotencyKey").GetString());
        Assert.Equal("/api/eInvoice/invoice-attempts?invoiceId=INV-001&phase=publish", conflict.RootElement.GetProperty("attemptStatusUrl").GetString());
        Assert.Single(factory.Calls, call => !call.Url.EndsWith("auth/token", StringComparison.Ordinal));

        using var byKey = await client.GetAsync("/api/eInvoice/operations/publish/diagnostic-original-001");
        Assert.Equal(HttpStatusCode.OK, byKey.StatusCode);
        using var keyLookup = JsonDocument.Parse(await byKey.Content.ReadAsStringAsync());
        Assert.Equal(prior.GetProperty("attemptId").GetString(), keyLookup.RootElement.GetProperty("attemptId").GetString());
    }

    [Fact]
    public async Task Envelope_error_has_supplier_http_status_and_only_explicit_message_fields_are_returned()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("synthetic-v3-token") :
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(JsonSerializer.Serialize(new
            {
                Success = false, ErrorCode = "InvalidParameter", Message = "Sai mẫu. password=synthetic-v3-secret token=synthetic-v3-token",
                Data = "raw-data-never-expose", Extra = "raw-field-never-expose",
            })) };
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "envelope-diagnostic-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        AssertNoSecrets(text);
        Assert.DoesNotContain("raw-data-never-expose", text);
        Assert.DoesNotContain("raw-field-never-expose", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal(400, body.RootElement.GetProperty("providerHttpStatus").GetInt32());
        Assert.Contains("Sai mẫu", body.RootElement.GetProperty("providerMessage").GetString());
    }

    [Theory]
    [InlineData(false, 401)]
    [InlineData(true, 200)]
    public async Task Invoice_attempt_lookup_requires_site_auth_and_never_contacts_misa(bool authenticated, int status)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client(authenticated);
        using var response = await client.GetAsync("/api/eInvoice/invoice-attempts?invoiceId=INV-001");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Empty(factory.Calls);
        if (authenticated)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("not-found", body.RootElement.GetProperty("state").GetString());
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("canRetry").ValueKind);
        }
    }

    [Fact]
    public async Task Structured_supplier_message_does_not_expose_an_echoed_request()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("synthetic-v3-token") :
            MisaApiTestFactory.Envelope(new[] { new { RefID = "INV-001", ErrorCode = "InvalidParameter", DescriptionErrorCode = "{\"password\":\"raw-private-value\",\"invoice\":\"raw-customer-data\"}" } });
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "structured-message-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("raw-private-value", text);
        Assert.DoesNotContain("raw-customer-data", text);
        Assert.Contains("structured error", text);
    }

    [Fact]
    public async Task Partial_batch_keeps_a_redacted_supplier_message_on_the_failed_item()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("synthetic-v3-token") :
            MisaApiTestFactory.Envelope(new[]
            {
                new { RefID = "INV-001", InvNo = (string?)"000001", ErrorCode = (string?)null, DescriptionErrorCode = (string?)null },
                new { RefID = "INV-002", InvNo = (string?)null, ErrorCode = (string?)"InvalidParameter", DescriptionErrorCode = (string?)"Sai dữ liệu. synthetic-v3-secret synthetic-v3-token" },
            });
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "batch-diagnostic-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest { Invoices = [MisaTestData.Invoice(), MisaTestData.Invoice(id: "INV-002")] }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        AssertNoSecrets(text);
        using var body = JsonDocument.Parse(text);
        var item = body.RootElement.GetProperty("data")[1];
        Assert.False(item.GetProperty("isSuccess").GetBoolean());
        Assert.Contains("Sai dữ liệu", item.GetProperty("providerMessage").GetString());
    }

    [Theory]
    [InlineData("/api/eInvoice/invoice-attempts?invoiceId=INV-001&phase=invalid")]
    [InlineData("/api/eInvoice/invoice-attempts")]
    public async Task Invalid_invoice_lookup_parameters_fail_without_provider_calls(string path)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Calls);
    }

    private static void AssertNoSecrets(string text)
    {
        foreach (var secret in new[] { "synthetic-v3-secret", "synthetic-v3-user", "synthetic-v3-token" }) Assert.DoesNotContain(secret, text);
    }
}
