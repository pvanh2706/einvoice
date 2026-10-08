using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

/// <summary>Durable local attempt/result journal. Pending attempts are never replayed against MISA automatically.</summary>
public sealed class MisaWriteJournal(string directory, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _directory = Path.GetFullPath(directory);
    private static readonly AsyncLocal<Attempt?> CurrentAttempt = new();
    internal static void MarkDispatched() { if (CurrentAttempt.Value is { } attempt) attempt.Dispatched = true; }

    public async Task<JsonElement> ExecuteAsync(Guid siteId, string operation, string key, object request,
        IReadOnlyList<string> invoiceClaims, Func<Task<object?>> execute, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        Directory.CreateDirectory(_directory);
        var fingerprint = Hash(JsonSerializer.Serialize(request, request.GetType(), Json));
        var attemptId = Hash($"{siteId:D}|{operation}|{key}");
        var path = Path.Combine(_directory, attemptId + ".json");
        var claims = new List<string>();
        try
        {
            using var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(reservation, new Record(fingerprint, "pending", clock.GetUtcNow(), null), Json, cancellationToken);
            await reservation.FlushAsync(cancellationToken);
            reservation.Flush(flushToDisk: true);
        }
        catch (IOException) when (File.Exists(path))
        {
            var previous = await Read(path, cancellationToken);
            if (previous is null)
                throw Conflict("MISA_OUTCOME_UNCERTAIN", "The prior journal record is incomplete; reconcile before sending again.");
            if (previous.Fingerprint != fingerprint)
                throw Conflict("IDEMPOTENCY_KEY_CONFLICT", "Idempotency key was already used for a different request.");
            if (previous.State == "completed" && previous.Result.HasValue) return previous.Result.Value;
            throw Conflict("MISA_OUTCOME_UNCERTAIN", "A prior attempt is pending or has an uncertain outcome; reconcile with MISA before sending again.");
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
                    throw Conflict("MISA_INVOICE_ALREADY_ATTEMPTED", "An operation for this invoice has already been attempted; use its original idempotency key or reconcile with MISA.");
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
            await Save(path, new Record(fingerprint, "completed", clock.GetUtcNow(), jsonResult), CancellationToken.None);
            return jsonResult;
        }
        catch
        {
            // Validation, mapping, login, template lookup and configuration failures before the business POST
            // are known non-writes. A dispatched operation remains pending even if its response is rejected.
            if (!attempt.Dispatched)
            {
                foreach (var claim in claims) File.Delete(claim);
                File.Delete(path);
            }
            throw;
        }
        finally { CurrentAttempt.Value = previousAttempt; }
        // Any transport/cancellation/unknown failure keeps pending record + claims. Never blindly repeat a write.
    }

    public async Task<object> StatusAsync(Guid siteId, string operation, string key, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        var path = Path.Combine(_directory, Hash($"{siteId:D}|{operation}|{key}") + ".json");
        var record = await Read(path, cancellationToken);
        return new { operation, state = record?.State ?? (File.Exists(path) ? "pending" : "not-found"), updatedAtUtc = record?.UpdatedAtUtc, result = record?.Result };
    }

    public static void ValidateKey(string key)
    {
        if (key.Length is < 8 or > 128 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new InvoiceRequestException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key must contain 8..128 ASCII letters, digits, '-' or '_'.");
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static InvoiceOperationConflictException Conflict(string code, string message) => new(code, message);
    private static async Task<Record?> Read(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
    private sealed record Record(string Fingerprint, string State, DateTimeOffset UpdatedAtUtc, JsonElement? Result);
    private sealed class Attempt { public bool Dispatched { get; set; } }
}
