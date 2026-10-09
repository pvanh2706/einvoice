using System.Text.Json;

namespace eInvoice.Core.Contracts;

public sealed record InvoiceAttemptError(string Source, string Code, string? ProviderCode = null,
    string? ProviderMessage = null, int? ProviderHttpStatus = null);

public sealed record InvoiceAttemptInfo(string? AttemptId, string? Operation, string? IdempotencyKey,
    string State, DateTimeOffset? UpdatedAtUtc, JsonElement? Result, InvoiceAttemptError? LastError,
    string? OriginalRequestId, bool RequiresReconciliation, bool? CanRetry);
