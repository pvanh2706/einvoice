using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;

using eInvoice.Providers.Misa;

namespace eInvoice.Providers.Common;

/// <summary>Durable local attempt/result journal. Pending attempts are never replayed against MISA automatically.</summary>
public class WriteOperationJournal(string directory, TimeProvider clock, string provider = "MISA")
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _directory = Path.GetFullPath(directory);
    public string DirectoryPath => _directory;
    private static readonly AsyncLocal<Attempt?> CurrentAttempt = new();
    internal static string? ResponseToken => CurrentAttempt.Value?.ResponseToken;
    internal static void MarkDispatched()
    {
        if (CurrentAttempt.Value is not { } attempt) return;
        attempt.Dispatched = true;
        attempt.HttpStatus = null;
    }
    internal static void MarkResponse(int status, string token)
    {
        if (CurrentAttempt.Value is not { } attempt) return;
        attempt.HttpStatus = status;
        attempt.ResponseToken = token;
    }

    public async Task<JsonElement> ExecuteAsync(Guid siteId, string operation, string key, object request,
        IReadOnlyList<string> invoiceClaims, Func<Task<object?>> execute, CancellationToken cancellationToken, string? requestId = null)
    {
        ValidateKey(key);
        Directory.CreateDirectory(_directory);
        var fingerprint = Hash(JsonSerializer.Serialize(request, request.GetType(), Json));
        var attemptId = AttemptHash(siteId, operation, key);
        var path = Path.Combine(_directory, attemptId + ".json");
        var claims = new List<string>();
        var reservationRecord = new Record(fingerprint, "pending", clock.GetUtcNow(), null, operation, key, null, requestId, provider);
        try
        {
            using var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(reservation, reservationRecord, Json, cancellationToken);
            await reservation.FlushAsync(cancellationToken);
            reservation.Flush(flushToDisk: true);
        }
        catch (IOException) when (File.Exists(path))
        {
            var previous = await Read(path, cancellationToken);
            if (previous is null)
                throw Conflict(provider + "_OUTCOME_UNCERTAIN", "The prior journal record is incomplete; reconcile before sending again.", Info(null, attemptId, "pending", operation, key));
            if (previous.Fingerprint != fingerprint)
                throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "Idempotency key was already used for a different request.", Info(previous, attemptId, operation: operation, key: key));
            if (previous.State == "completed" && previous.Result.HasValue) return previous.Result.Value;
            throw Conflict(provider + "_OUTCOME_UNCERTAIN", "A prior attempt is pending or has an uncertain outcome; reconcile with the provider before sending again.", Info(previous, attemptId, operation: operation, key: key));
        }
        try
        {
            foreach (var invoiceClaim in invoiceClaims.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                var claimPath = Path.Combine(_directory, Hash($"{siteId:D}|invoice|{invoiceClaim}") + ".claim");
                try
                {
                    using var file = new FileStream(claimPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    var bytes = Encoding.ASCII.GetBytes(attemptId);
                    await file.WriteAsync(bytes, cancellationToken);
                    await file.FlushAsync(cancellationToken);
                    file.Flush(flushToDisk: true);
                    claims.Add(claimPath);
                }
                catch (IOException) when (File.Exists(claimPath))
                {
                    throw Conflict(provider + "_INVOICE_ALREADY_ATTEMPTED", "An operation for this invoice has already been attempted; use its original idempotency key or reconcile with the provider.",
                        await ClaimInfo(claimPath, cancellationToken), invoiceClaim);
                }
            }
        }
        catch
        {
            foreach (var claim in claims) File.Delete(claim);
            File.Delete(path);
            throw;
        }
        var previousAttempt = CurrentAttempt.Value;
        var attempt = new Attempt();
        CurrentAttempt.Value = attempt;
        try
        {
            var result = await execute();
            var jsonResult = JsonSerializer.SerializeToElement(result, result?.GetType() ?? typeof(object), Json);
            await Save(path, reservationRecord with { State = "completed", UpdatedAtUtc = clock.GetUtcNow(), Result = jsonResult }, CancellationToken.None);
            return jsonResult;
        }
        catch (Exception error)
        {
            // Validation, mapping, login, template lookup and configuration failures before the business POST
            // are known non-writes. A dispatched operation remains pending even if its response is rejected.
            if (!attempt.Dispatched)
            {
                foreach (var claim in claims) File.Delete(claim);
                File.Delete(path);
            }
            else
            {
                if (error is ProviderCallException responseError) responseError.SupplierHttpStatus ??= attempt.HttpStatus;
                var failure = error switch
                {
                    ProviderCallException provider => new InvoiceAttemptError("provider", provider.Code, provider.SupplierCode,
                        provider.SupplierMessage, provider.SupplierHttpStatus),
                    OperationCanceledException => new InvoiceAttemptError("gateway", "REQUEST_CANCELLED"),
                    _ => new InvoiceAttemptError("gateway", "EINVOICE_INTERNAL_ERROR"),
                };
                var failedRecord = reservationRecord with { UpdatedAtUtc = clock.GetUtcNow(), LastError = failure };
                // Saving diagnostics must never mask the original error or release a dispatched invoice claim.
                try { await Save(path, failedRecord, CancellationToken.None); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                if (error is ProviderCallException providerFailure) providerFailure.Attempt = Info(failedRecord, attemptId);
            }
            throw;
        }
        finally { CurrentAttempt.Value = previousAttempt; }
        // Any transport/cancellation/unknown failure keeps pending record + claims. Never blindly repeat a write.
    }

    public async Task<InvoiceAttemptInfo> StatusAsync(Guid siteId, string operation, string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        var path = Path.Combine(_directory, AttemptHash(siteId, operation, key) + ".json");
        var record = await Read(path, cancellationToken);
        return Info(record, Path.GetFileNameWithoutExtension(path), File.Exists(path) ? "pending" : "not-found", operation, key);
    }

    public async Task<InvoiceAttemptInfo> InvoiceStatusAsync(Guid siteId, string invoiceId, string phase, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoiceId) || invoiceId.Length > 256 || phase is not ("publish" or "xml-create"))
            throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", "Specify invoiceId and phase publish or xml-create.");
        var path = Path.Combine(_directory, Hash($"{siteId:D}|invoice|{phase}:{invoiceId}") + ".claim");
        return File.Exists(path) ? await ClaimInfo(path, cancellationToken) : Info(null, null, "not-found");
    }

    private async Task<InvoiceAttemptInfo> ClaimInfo(string path, CancellationToken cancellationToken)
    {
        string owner;
        try { owner = (await File.ReadAllTextAsync(path, cancellationToken)).Trim(); }
        catch (FileNotFoundException) { return Info(null, null, "pending"); }
        if (owner.Length != 64 || !owner.All(char.IsAsciiHexDigit)) return Info(null, null, "pending");
        // Only a site-specific claim computed by this server can resolve an attempt; callers cannot supply arbitrary hashes.
        return Info(await Read(Path.Combine(_directory, owner + ".json"), cancellationToken), owner, "pending");
    }

    private static InvoiceAttemptInfo Info(Record? record, string? attemptId, string missingState = "pending", string? operation = null, string? key = null)
    {
        var state = record?.State ?? missingState;
        return new(attemptId, record?.Operation ?? operation, record?.IdempotencyKey ?? key, state, record?.UpdatedAtUtc,
            record?.Result, record?.LastError, record?.OriginalRequestId, state == "pending", state == "not-found" ? null : false, record?.Provider);
    }

    public static void ValidateKey(string key)
    {
        if (key.Length is < 8 or > 128 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new InvoiceRequestException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key must contain 8..128 ASCII letters, digits, '-' or '_'.");
    }
    private string AttemptHash(Guid siteId, string operation, string key) =>
        Hash(provider == "MISA" ? $"{siteId:D}|{operation}|{key}" : $"{siteId:D}|{provider}|{operation}|{key}");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static InvoiceOperationConflictException Conflict(string code, string message, InvoiceAttemptInfo? previous = null, string? claim = null) =>
        new(code, message) { PreviousAttempt = previous, InvoiceClaim = claim };
    private static async Task<Record?> Read(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return await JsonSerializer.DeserializeAsync<Record>(file, Json, cancellationToken);
        }
        catch (JsonException) { return null; } // A concurrent reservation/crash may leave an incomplete record: fail closed.
    }
    private static async Task Save(string path, Record record, CancellationToken cancellationToken)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, record, Json, cancellationToken);
                await file.FlushAsync(cancellationToken);
                file.Flush(flushToDisk: true);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private sealed record Record(string Fingerprint, string State, DateTimeOffset UpdatedAtUtc, JsonElement? Result,
        string? Operation = null, string? IdempotencyKey = null, InvoiceAttemptError? LastError = null, string? OriginalRequestId = null, string? Provider = null);
    private sealed class Attempt
    {
        public bool Dispatched { get; set; }
        public int? HttpStatus { get; set; }
        public string? ResponseToken { get; set; }
    }
}
