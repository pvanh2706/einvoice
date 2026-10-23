using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using eInvoice.Providers.Common;

namespace eInvoice.Tests.Misa;

public class MisaWriteJournalTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "eInvoice-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Same_key_replays_result_across_journal_instances_without_a_second_write()
    {
        var calls = 0;
        Task<object?> Execute() => Task.FromResult<object?>(new { invoiceId = "INV-001", number = ++calls });
        var request = new { invoiceId = "INV-001" };
        var first = await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", request, ["publish:INV-001"], Execute, default);
        var replay = await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", request, ["publish:INV-001"], Execute, default);
        Assert.Equal(first.GetRawText(), replay.GetRawText());
        Assert.Equal(1, calls);
        Assert.Contains("completed", System.Text.Json.JsonSerializer.Serialize(await Journal().StatusAsync(MisaTestData.SiteId, "publish", "idempotency-001", default)));
    }

    [Fact]
    public async Task Different_body_or_key_for_same_invoice_cannot_repeat_a_write()
    {
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", new { amount = 100 }, ["publish:INV-001"], () => Task.FromResult<object?>(new { success = true }), default);
        var changedBody = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "idempotency-001", new { amount = 200 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", changedBody.Code);
        var changedKey = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish-batch", "idempotency-002", new { amount = 100 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("MISA_INVOICE_ALREADY_ATTEMPTED", changedKey.Code);
    }

    [Fact]
    public async Task Timeout_pending_attempt_survives_restart_and_cannot_be_replayed()
    {
        await Assert.ThrowsAsync<ProviderCallException>(() => Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001",
            new { amount = 100 }, ["publish:INV-001"], () =>
            {
                MisaWriteJournal.MarkDispatched();
                throw new ProviderCallException(ProviderFailure.Timeout, "MISA_TIMEOUT", "synthetic-secret");
            }, default));
        var error = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "idempotency-001", new { amount = 100 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("MISA_OUTCOME_UNCERTAIN", error.Code);
        foreach (var file in Directory.GetFiles(_directory)) Assert.DoesNotContain("synthetic-secret", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task Concurrent_request_is_blocked_while_first_is_in_progress()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", new { amount = 100 }, ["publish:INV-001"], async () =>
        {
            started.SetResult();
            await release.Task;
            return new { success = true };
        }, default);
        await started.Task;
        var error = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "idempotency-001", new { amount = 100 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("MISA_OUTCOME_UNCERTAIN", error.Code);
        release.SetResult();
        await first;
    }

    [Fact]
    public async Task Validation_failure_releases_claims_and_separate_site_and_usb_phase_do_not_collide()
    {
        await Assert.ThrowsAsync<InvoiceRequestException>(() => Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001",
            new { amount = 100 }, ["publish:INV-001"], () => throw new InvoiceRequestException("INVALID_REQUEST", "Invalid input"), default));
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", new { amount = 200 }, ["publish:INV-001"], () => Task.FromResult<object?>(true), default);
        await Journal().ExecuteAsync(Guid.NewGuid(), "publish", "idempotency-001", new { amount = 200 }, ["publish:INV-001"], () => Task.FromResult<object?>(true), default);
        await Journal().ExecuteAsync(MisaTestData.SiteId, "xml-create", "idempotency-001", new { amount = 200 }, ["xml-create:INV-001"], () => Task.FromResult<object?>(true), default);
    }

    [Fact]
    public async Task Incomplete_journal_is_reported_pending_and_cannot_repeat_a_write()
    {
        var request = new { amount = 100 };
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "idempotency-001", request, ["publish:INV-001"], () => Task.FromResult<object?>(true), default);
        await File.WriteAllTextAsync(Assert.Single(Directory.GetFiles(_directory, "*.json")), "{");
        var status = await Journal().StatusAsync(MisaTestData.SiteId, "publish", "idempotency-001", default);
        Assert.Equal("pending", status.State);
        var error = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "idempotency-001", request, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("MISA_OUTCOME_UNCERTAIN", error.Code);
    }

    [Fact]
    public async Task Failure_details_and_original_key_survive_restart_and_are_isolated_by_site_and_phase()
    {
        await Assert.ThrowsAsync<ProviderCallException>(() => Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "original-key-001",
            new { amount = 100 }, ["publish:INV-001"], () =>
            {
                MisaWriteJournal.MarkDispatched();
                MisaWriteJournal.MarkResponse(200, "private-token-never-store");
                throw new ProviderCallException(ProviderFailure.Rejected, "MISA_INVOICE_REJECTED", "private-exception-never-store")
                    { SupplierCode = "InvalidParameter", SupplierMessage = "Ký hiệu không hợp lệ." };
            }, default, "original-server-request"));
        var restarted = Journal();
        var status = await restarted.InvoiceStatusAsync(MisaTestData.SiteId, "INV-001", "publish", default);
        Assert.Equal("original-key-001", status.IdempotencyKey);
        Assert.Equal("original-server-request", status.OriginalRequestId);
        Assert.Equal("InvalidParameter", status.LastError!.ProviderCode);
        Assert.Equal("Ký hiệu không hợp lệ.", status.LastError.ProviderMessage);
        Assert.Equal(200, status.LastError.ProviderHttpStatus);
        Assert.True(status.RequiresReconciliation);
        Assert.False(status.CanRetry);
        Assert.Equal("not-found", (await restarted.InvoiceStatusAsync(Guid.NewGuid(), "INV-001", "publish", default)).State);
        Assert.Equal("not-found", (await restarted.InvoiceStatusAsync(MisaTestData.SiteId, "INV-001", "xml-create", default)).State);
        var conflict = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => restarted.ExecuteAsync(MisaTestData.SiteId,
            "publish", "new-key-001", new { amount = 100 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal(status.AttemptId, conflict.PreviousAttempt!.AttemptId);
        Assert.Equal("publish:INV-001", conflict.InvoiceClaim);
        foreach (var file in Directory.GetFiles(_directory))
        {
            var text = await File.ReadAllTextAsync(file);
            Assert.DoesNotContain("private-token-never-store", text);
            Assert.DoesNotContain("private-exception-never-store", text);
        }
    }

    [Fact]
    public async Task Legacy_pending_record_can_be_found_by_invoice_without_inventing_missing_error_or_key()
    {
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "original-key-001", new { amount = 100 }, ["publish:INV-001"],
            () => Task.FromResult<object?>(true), default);
        var path = Assert.Single(Directory.GetFiles(_directory, "*.json"));
        var record = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        foreach (var property in new[] { "operation", "idempotencyKey", "lastError", "originalRequestId" }) record.Remove(property);
        record["state"] = "pending";
        record["result"] = null;
        await File.WriteAllTextAsync(path, record.ToJsonString());
        var status = await Journal().InvoiceStatusAsync(MisaTestData.SiteId, "INV-001", "publish", default);
        Assert.Equal("pending", status.State);
        Assert.Null(status.LastError);
        Assert.Null(status.IdempotencyKey);
        Assert.True(status.RequiresReconciliation);
        var blocked = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "new-key-001", new { amount = 100 }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("pending", blocked.PreviousAttempt!.State);
    }

    [Fact]
    public async Task Publish_timeout_does_not_report_a_prior_template_lookup_http_status_as_publish_response()
    {
        await Assert.ThrowsAsync<ProviderCallException>(() => Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "original-key-001",
            new { amount = 100 }, ["publish:INV-001"], () =>
            {
                MisaWriteJournal.MarkResponse(200, "template-token");
                MisaWriteJournal.MarkDispatched();
                throw new ProviderCallException(ProviderFailure.Timeout, "MISA_OPERATION_TIMEOUT", "timeout");
            }, default));
        var status = await Journal().InvoiceStatusAsync(MisaTestData.SiteId, "INV-001", "publish", default);
        Assert.Null(status.LastError!.ProviderHttpStatus);
        Assert.Equal("MISA_OPERATION_TIMEOUT", status.LastError.Code);
        Assert.False(status.CanRetry);
    }

    [Fact]
    public async Task Provider_change_cannot_replay_another_providers_result_or_bypass_invoice_claim()
    {
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "shared-key-001", new { invoiceId = "INV-001" }, ["publish:INV-001"],
            () => Task.FromResult<object?>(new { number = "MISA-001" }), default);
        var viettel = new WriteOperationJournal(_directory, TimeProvider.System, "VIETTEL");
        Assert.Equal("not-found", (await viettel.StatusAsync(MisaTestData.SiteId, "publish", "shared-key-001", default)).State);
        var conflict = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => viettel.ExecuteAsync(MisaTestData.SiteId,
            "publish", "shared-key-001", new { invoiceId = "INV-001" }, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("VIETTEL_INVOICE_ALREADY_ATTEMPTED", conflict.Code);
        Assert.Equal("MISA", conflict.PreviousAttempt!.Provider);
    }

    [Fact]
    public async Task Separate_provider_attempts_keep_own_results_across_restart()
    {
        await Journal().ExecuteAsync(MisaTestData.SiteId, "publish", "shared-key-001", new { invoiceId = "MISA-001" }, ["publish:MISA-001"],
            () => Task.FromResult<object?>(new { number = "MISA-001" }), default);
        await new WriteOperationJournal(_directory, TimeProvider.System, "VIETTEL").ExecuteAsync(MisaTestData.SiteId, "publish", "shared-key-001",
            new { invoiceId = "VIETTEL-001" }, ["publish:VIETTEL-001"], () => Task.FromResult<object?>(new { number = "VIETTEL-001" }), default);
        var misa = await Journal().StatusAsync(MisaTestData.SiteId, "publish", "shared-key-001", default);
        var viettel = await new WriteOperationJournal(_directory, TimeProvider.System, "VIETTEL").StatusAsync(MisaTestData.SiteId, "publish", "shared-key-001", default);
        Assert.Equal("MISA", misa.Provider); Assert.Equal("VIETTEL", viettel.Provider);
        Assert.NotEqual(misa.AttemptId, viettel.AttemptId);
        Assert.Contains("MISA-001", misa.Result!.Value.GetRawText()); Assert.Contains("VIETTEL-001", viettel.Result!.Value.GetRawText());
    }

    private MisaWriteJournal Journal() => new(_directory, TimeProvider.System);
    private static Task<object?> NeverWrite() => throw new InvalidOperationException("No second write is allowed");
    public void Dispose()
    {
        var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "eInvoice-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(_directory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe test cleanup path");
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
