using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;

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
        var status = System.Text.Json.JsonSerializer.SerializeToElement(await Journal().StatusAsync(MisaTestData.SiteId, "publish", "idempotency-001", default));
        Assert.Equal("pending", status.GetProperty("state").GetString());
        var error = await Assert.ThrowsAsync<InvoiceOperationConflictException>(() => Journal().ExecuteAsync(MisaTestData.SiteId,
            "publish", "idempotency-001", request, ["publish:INV-001"], NeverWrite, default));
        Assert.Equal("MISA_OUTCOME_UNCERTAIN", error.Code);
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
