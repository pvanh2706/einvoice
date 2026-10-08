using System.Net;
using System.Text.Json;
using eInvoice.Core.Contracts;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Api;

public class MisaOperationTests
{
    [Theory]
    [InlineData("hsm-single", "publish", 0, 0)]
    [InlineData("mtt-single", "publish", 0, 1)]
    [InlineData("mtt-hsm-single", "publish", 0, 4)]
    [InlineData("hsm-batch", "publish-batch", 0, 0)]
    [InlineData("mtt-batch", "publish-batch", 0, 1)]
    [InlineData("mtt-hsm-batch", "publish-batch", 0, 4)]
    [InlineData("ticket", "publish", 1, 0)]
    [InlineData("receipt", "publish", 2, 0)]
    [InlineData("preview", "preview", 0, 0)]
    [InlineData("preview-replace", "preview", 0, 0)]
    [InlineData("adjust-hsm", "adjust", 0, 0)]
    [InlineData("replace-hsm", "replace", 0, 0)]
    [InlineData("adjust-mtt", "adjust", 0, 1)]
    [InlineData("adjust-usb", "adjust", 0, 2)]
    [InlineData("xml-create", "xml/create", 0, 2)]
    [InlineData("xml-publish", "xml/publish", 0, 2)]
    public async Task Live_api_mapping_matches_frozen_baseline_payload(string fixture, string route, int type, int mode)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "fixture-request-001");
        var invoice = MisaTestData.Invoice(type, mode);
        if (fixture == "preview-replace") invoice.Invoice!.PreviewType = 3;
        object request = route switch
        {
            "publish-batch" => new InvoiceBatchRequest { Invoices = [invoice] },
            "xml/publish" => new PublishSignedXmlRequest { InvoiceId = "INV-001", SearchCode = "transaction-001", InvoiceData = "<synthetic-signed-invoice/>" },
            _ => invoice,
        };
        using var body = MisaApiTestFactory.Body(request, route is "publish" or "xml/create" ? typeof(CreateInvoiceRequest) : null);
        using var response = await client.PostAsync("/api/eInvoice/" + route, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var providerCall = factory.Calls.Last(call => call.Method == HttpMethod.Post && !call.Url.EndsWith("auth/token", StringComparison.Ordinal));
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Misa", "Payloads", fixture + ".json")));
        var actual = JToken.Parse(providerCall.Body!);
        Assert.True(JToken.DeepEquals(expected, actual), $"{fixture} mismatch:\n{actual}");
        Assert.Equal("utf-8", providerCall.Encoding);
        Assert.Equal("TEST-TAX", providerCall.Headers["CompanyTaxCode"]);
        Assert.Equal(type == 2 ? "Bearer synthetic-receipt-token" : "Bearer synthetic-v3-token", providerCall.Headers["Authorization"]);
        var responseJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-v3-secret", responseJson);
        Assert.DoesNotContain("synthetic-receipt-secret", responseJson);
        Assert.DoesNotContain("synthetic-v3-token", responseJson);
        Assert.DoesNotContain("synthetic-receipt-token", responseJson);
    }

    [Theory]
    [InlineData(0, 0, "code/itg/invoicepublished/downloadinvoice?downloadDataType=pdf")]
    [InlineData(0, 1, "code/itg/invoice-calculating-published/download?downloadDataType=pdf")]
    [InlineData(1, 0, "itg/invoicepublished/downloadinvoice?downloadDataType=pdf")]
    [InlineData(2, 0, "invoice/download?downloadDatatype=pdf")]
    public async Task Document_routes_preserve_search_vs_transaction_ids(int type, int mode, string expectedSuffix)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest
        {
            InvoiceType = type, ElectronicInvoiceType = mode, SearchCode = "search-001", TransactionId = "transaction-001",
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = factory.Calls.Last();
        Assert.EndsWith(expectedSuffix, call.Url);
        Assert.Equal(type == 0 ? "[\"search-001\"]" : "[\"transaction-001\"]", call.Body);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("base64", json.RootElement.GetProperty("data").GetProperty("format").GetString());
    }

    [Theory]
    [InlineData(0, "code/itg/invoicepublished/cancel", "InvNo", "CancelReason")]
    [InlineData(1, "itg/invoicepublished/cancel", "InvNo", "CancelReason")]
    [InlineData(2, "invoice/deleted", "RefNo", "DeletedReason")]
    public async Task Void_preserves_provider_request_variants(int type, string path, string numberKey, string reasonKey)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "void-request-001");
        using var response = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(new VoidInvoiceRequest
        {
            InvoiceId = "INV-001", InvoiceType = type, ProviderInvoiceNumber = "000001", SearchCode = "search-001",
            TransactionId = "transaction-001", Reason = "Synthetic reason",
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = factory.Calls.Last();
        Assert.EndsWith(path, call.Url);
        var body = JObject.Parse(call.Body!);
        Assert.Equal("000001", body.Value<string>(numberKey));
        Assert.Equal("Synthetic reason", body.Value<string>(reasonKey));
        Assert.Equal(type == 0 ? "search-001" : "transaction-001", body.Value<string>("TransactionID"));
    }

    [Theory]
    [InlineData(0, "code/invoicepublished/invoice-status/refid")]
    [InlineData(1, "code/itg/invoice-calculating-published/invoice-status/refid")]
    public async Task Status_preserves_external_ezInvoiceID_key_but_returns_new_pms_key(int mode, string suffix)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var response = await client.PostAsync("/api/eInvoice/status", MisaApiTestFactory.Body(new SendToTaxAuthorityRequest
        {
            InvoiceIds = ["INV-001"], InvoiceType = 0, ElectronicInvoiceType = mode,
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.EndsWith(suffix, factory.Calls.Last().Url);
        var request = JObject.Parse(factory.Calls.Last().Body!);
        Assert.Equal(new[] { "ezInvoiceID" }, request.Properties().Select(property => property.Name));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INV-001", body.RootElement.GetProperty("data")[0].GetProperty("invoiceId").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("data")[0].GetProperty("taxAuthorityStatus").GetInt32());
    }

    [Fact]
    public async Task Xml_hash_and_signed_tax_preserve_wire_fields()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var hash = await client.PostAsync("/api/eInvoice/xml/hash", MisaApiTestFactory.Body(new GetXmlHashRequest { CertBase64 = "c3ludGhldGljLWNlcnQ=" }));
        Assert.Equal(HttpStatusCode.OK, hash.StatusCode);
        var payload = JObject.Parse(factory.Calls.Last().Body!);
        Assert.Equal("TEST-TAX", payload.Value<string>("TaxCode"));
        Assert.Equal("c3ludGhldGljLWNlcnQ=", payload.Value<string>("X509RawData"));
        Assert.True(payload.Value<long>("TakeTime") > 0);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "signed-tax-request-001");
        using var signed = await client.PostAsync("/api/eInvoice/tax/send-signed", MisaApiTestFactory.Body(new SendSignedTaxAuthorityRequest
        {
            SignatureValue = "synthetic-signature", MessageCode = "MSG-001",
        }));
        Assert.Equal(HttpStatusCode.OK, signed.StatusCode);
        payload = JObject.Parse(factory.Calls.Last().Body!);
        Assert.Equal(new[] { "SignatureValue", "MessageCode" }, payload.Properties().Select(property => property.Name));
        Assert.Equal("synthetic-signature", payload.Value<string>("SignatureValue"));
    }

    [Fact]
    public async Task Receipt_logs_never_include_password_query_or_secondary_credentials()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "receipt-log-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(type: 2), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = string.Join("\n", factory.Logs);
        Assert.DoesNotContain("password=", logs);
        Assert.DoesNotContain("synthetic-v3-secret", logs);
        Assert.DoesNotContain("synthetic-receipt-secret", logs);
    }

    [Fact]
    public async Task Missing_receipt_template_prevents_publishing()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.Contains("user/token?", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("receipt-token") : MisaApiTestFactory.Envelope(Array.Empty<object>());
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "receipt-missing-template-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(type: 2), typeof(CreateInvoiceRequest)));
        await Problem(response, 400, "MISA_RECEIPT_TEMPLATE_NOT_FOUND");
        Assert.DoesNotContain(factory.Calls, call => call.Url.Contains("publishinvoicesoftdream", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Completed_publish_replay_and_changed_key_block_duplicate_writes()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "publish-idempotent-001");
        using var first = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Single(factory.Calls, call => call.Url.EndsWith("publishhsm", StringComparison.Ordinal));
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "publish-idempotent-002");
        using var duplicate = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(duplicate, 409, "MISA_INVOICE_ALREADY_ATTEMPTED");
        using var status = await client.GetAsync("/api/eInvoice/operations/publish/publish-idempotent-001");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Contains("completed", await status.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Transport_timeout_does_not_retry_and_pending_attempt_blocks_replay()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("token") : throw new TaskCanceledException("synthetic-secret");
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "publish-timeout-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(response, 504, "MISA_OPERATION_TIMEOUT");
        Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
        using var replay = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(replay, 409, "MISA_OUTCOME_UNCERTAIN");
        Assert.Single(factory.Calls, call => call.Url.EndsWith("publishhsm", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Mixed_batch_and_invalid_invoice_are_rejected_before_provider_calls()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "invalid-batch-001");
        using var batch = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest
        {
            Invoices = [MisaTestData.Invoice(), MisaTestData.Invoice(mode: 1, id: "INV-002")],
        }));
        await Problem(batch, 400, "INVALID_INVOICE_REQUEST");
        var invalid = MisaTestData.Invoice();
        invalid.Invoice!.InvoiceDate = "31/02/2026";
        using var single = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(invalid, typeof(CreateInvoiceRequest)));
        await Problem(single, 400, "INVALID_INVOICE_REQUEST");
        Assert.Empty(factory.Calls);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("publish-batch")]
    [InlineData("preview")]
    [InlineData("document")]
    [InlineData("void")]
    [InlineData("adjust")]
    [InlineData("replace")]
    [InlineData("status")]
    [InlineData("xml/create")]
    [InlineData("xml/hash")]
    [InlineData("tax/send-signed")]
    [InlineData("tax/send-pending")]
    [InlineData("xml/publish")]
    public async Task Every_operation_requires_authentication_and_site_permission(string route)
    {
        using var factory = new MisaApiTestFactory();
        using var noKey = factory.Client(authenticated: false);
        using var unauthorized = await noKey.PostAsync("/api/eInvoice/" + route, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        using var otherSite = factory.Client(siteId: Guid.NewGuid());
        using var denied = await otherSite.PostAsync("/api/eInvoice/" + route, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Empty(factory.Calls);
    }

    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("missing-idempotency", 400, "IDEMPOTENCY_KEY_REQUIRED")]
    [InlineData("bad-site-body", 400, "SITE_CONFIGURATION_MISMATCH")]
    [InlineData("other-provider", 501, "PROVIDER_NOT_IMPLEMENTED")]
    [InlineData("resolver-mismatch", 400, "INVALID_SITE_CONFIGURATION")]
    public async Task Publication_preconditions_block_provider_calls(string mode, int status, string code)
    {
        using var factory = new MisaApiTestFactory();
        var request = MisaTestData.Invoice();
        if (mode == "bad-site-body") request.SiteConfigInfo = new SiteInfo { Site = new SiteConfiguration { Id = Guid.NewGuid(), Partner = 6 } };
        if (mode is "other-provider" or "resolver-mismatch") factory.Resolve = (_, _) =>
        {
            var site = MisaTestData.Site;
            if (mode == "other-provider") site.Site.Partner = 1;
            else site.Site.Id = Guid.NewGuid();
            return site;
        };
        using var client = factory.Client();
        if (mode != "missing-idempotency") client.DefaultRequestHeaders.Add("Idempotency-Key", "precondition-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(request, typeof(CreateInvoiceRequest)));
        await Problem(response, status, code);
        Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Nested_batch_configuration_cannot_change_authorized_site()
    {
        using var factory = new MisaApiTestFactory();
        var request = MisaTestData.Invoice();
        request.SiteConfigInfo = new SiteInfo { Site = new SiteConfiguration { Id = Guid.NewGuid(), Partner = 6 } };
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "batch-site-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest { Invoices = [request] }));
        await Problem(response, 400, "INVALID_INVOICE_REQUEST");
        Assert.Empty(factory.Calls);
    }

    [Fact]
    public async Task Batch_results_correlate_by_id_and_keep_partial_failures_without_exposing_error_text()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("token") :
            MisaApiTestFactory.Envelope(new[]
            {
                new { RefID = "INV-002", InvNo = (string?)null, ErrorCode = (string?)"synthetic-secret/error-text" },
                new { RefID = "INV-001", InvNo = (string?)"000001", ErrorCode = (string?)null },
            });
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "partial-batch-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish-batch", MisaApiTestFactory.Body(new InvoiceBatchRequest
        {
            Invoices = [MisaTestData.Invoice(), MisaTestData.Invoice(id: "INV-002")],
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("synthetic-secret", text);
        using var body = JsonDocument.Parse(text);
        var results = body.RootElement.GetProperty("data");
        Assert.Equal("INV-002", results[0].GetProperty("invoiceId").GetString());
        Assert.False(results[0].GetProperty("isSuccess").GetBoolean());
        Assert.Equal("MISA_ITEM_REJECTED", results[0].GetProperty("providerErrorCode").GetString());
        Assert.True(results[1].GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task Foreign_invoice_identity_in_provider_response_is_not_reported_as_success()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("token") :
            MisaApiTestFactory.Envelope(new[] { new { RefID = "OTHER-INVOICE", InvNo = "000001" } });
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "identity-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(response, 502, "MISA_OPERATION_INVALID_RESPONSE");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Provider_auth_rejection_invalidates_cached_token_for_next_request_without_replay(bool expiredIn200)
    {
        using var factory = new MisaApiTestFactory();
        var logins = 0;
        var documents = 0;
        factory.Respond = call =>
        {
            if (call.Url.EndsWith("auth/token", StringComparison.Ordinal)) return MisaApiTestFactory.Envelope("token-" + ++logins);
            if (++documents == 1) return new HttpResponseMessage(expiredIn200 ? HttpStatusCode.OK : HttpStatusCode.Unauthorized)
                { Content = new StringContent(expiredIn200 ? "{\"Success\":false,\"ErrorCode\":\"TokenExpiredCode\"}" : "{\"Success\":false,\"ErrorCode\":\"UnAuthorize\"}") };
            return MisaApiTestFactory.Envelope(new[] { new { Data = "c3ludGhldGlj" } });
        };
        using var client = factory.Client();
        var request = new DownloadPdfRequest { InvoiceType = 0, SearchCode = "search-001", ElectronicInvoiceType = 0 };
        using var first = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(request));
        await Problem(first, 422, "MISA_OPERATION_REJECTED");
        Assert.Equal(1, documents);
        using var second = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, logins);
        Assert.Equal(2, documents);
    }

    [Fact]
    public async Task Health_check_always_contacts_misa_even_when_a_business_token_is_cached()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        using var document = await client.PostAsync("/api/eInvoice/document", MisaApiTestFactory.Body(new DownloadPdfRequest { InvoiceType = 0, SearchCode = "search-001" }));
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        using var health = await client.PostAsync("/api/eInvoice/health-check", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(2, factory.Calls.Count(call => call.Url.EndsWith("auth/token", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Failed_login_does_not_permanently_reserve_an_invoice_that_was_never_sent()
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = _ => MisaApiTestFactory.Envelope("", success: false);
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "login-failure-test-001");
        using var failed = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(failed, 422, "MISA_LOGIN_REJECTED");
        factory.Respond = null;
        using var corrected = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        Assert.Single(factory.Calls, call => call.Url.EndsWith("publishhsm", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("", 502, "MISA_OPERATION_EMPTY_RESPONSE")]
    [InlineData("<html>synthetic-secret</html>", 502, "MISA_OPERATION_INVALID_RESPONSE")]
    [InlineData("{\"Data\":\"[]\"}", 502, "MISA_OPERATION_INVALID_RESPONSE")]
    [InlineData("{\"Success\":true,\"Data\":\"[]\"}", 502, "MISA_OPERATION_INVALID_RESPONSE")]
    [InlineData("{\"Success\":true,\"Data\":\"[{}]\"}", 502, "MISA_OPERATION_INVALID_RESPONSE")]
    [InlineData("{\"Success\":false,\"Errors\":[\"synthetic-secret\"]}", 422, "MISA_OPERATION_REJECTED")]
    public async Task Invalid_operation_envelopes_do_not_report_success_or_leak_body(string payload, int status, string code)
    {
        using var factory = new MisaApiTestFactory();
        factory.Respond = call => call.Url.EndsWith("auth/token", StringComparison.Ordinal) ? MisaApiTestFactory.Envelope("token") :
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(payload) };
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "invalid-envelope-test-001");
        using var response = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(MisaTestData.Invoice(), typeof(CreateInvoiceRequest)));
        await Problem(response, status, code);
        Assert.DoesNotContain("synthetic-secret", await response.Content.ReadAsStringAsync());
        Assert.Single(factory.Calls, call => call.Url.EndsWith("publishhsm", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("adjust")]
    [InlineData("replace")]
    public async Task Mtt_serial_adjustment_keeps_publish_claim_even_with_legacy_usb_mode(string operation)
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "adjustment-claim-test-001");
        var request = MisaTestData.Invoice(mode: 2);
        request.Invoice!.Serial = "1C26MAA";
        using var adjusted = await client.PostAsync("/api/eInvoice/" + operation, MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.OK, adjusted.StatusCode);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "adjustment-claim-test-002");
        using var repeated = await client.PostAsync("/api/eInvoice/publish", MisaApiTestFactory.Body(request, typeof(CreateInvoiceRequest)));
        await Problem(repeated, 409, "MISA_INVOICE_ALREADY_ATTEMPTED");
        Assert.Single(factory.Calls, call => !call.Url.EndsWith("auth/token", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Void_claim_tracks_provider_identity_when_pms_invoice_id_changes()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "void-claim-test-001");
        var request = new VoidInvoiceRequest { InvoiceType = 0, InvoiceId = "INV-001", SearchCode = "search-001", ProviderInvoiceNumber = "000001", Reason = "synthetic reason" };
        using var first = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(request));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        request.InvoiceId = "INV-002";
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "void-claim-test-002");
        using var repeated = await client.PostAsync("/api/eInvoice/void", MisaApiTestFactory.Body(request));
        await Problem(repeated, 409, "MISA_INVOICE_ALREADY_ATTEMPTED");
        Assert.Single(factory.Calls, call => !call.Url.EndsWith("auth/token", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Hsm_cqt_keeps_appid_ticks_sign_type_and_rejects_an_unsupported_id_filter()
    {
        using var factory = new MisaApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "hsm-cqt-test-001");
        using var response = await client.PostAsync("/api/eInvoice/tax/send-pending", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = JObject.Parse(factory.Calls.Last().Body!);
        Assert.Equal(new[] { "TakeTime", "AppID", "SignType" }, payload.Properties().Select(property => property.Name));
        Assert.Equal("synthetic-receipt-user", payload.Value<string>("AppID"));
        Assert.Equal("HSM", payload.Value<string>("SignType"));
        var calls = factory.Calls.Count;
        using var invalid = await client.PostAsync("/api/eInvoice/tax/send-pending", new StringContent("{\"invoiceIds\":[\"INV-001\"]}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(calls, factory.Calls.Count);
    }
}
