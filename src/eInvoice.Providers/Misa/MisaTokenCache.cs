using eInvoice.Providers.Common;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

/// <summary>Per-process token cache; credential fingerprints use a random process-specific HMAC key.</summary>
public sealed class MisaTokenCache(TimeProvider clock)
{
    private readonly byte[] _fingerprintKey = RandomNumberGenerator.GetBytes(32);
    private const int Capacity = 4096;
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim[] _gates = Enumerable.Range(0, 128).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private readonly long[] _versions = new long[128];
    private readonly object _storageGate = new();

    public async Task<string> GetAsync(SiteConfiguration site, bool receipt, string applicationId,
        Func<CancellationToken, Task<string>> authenticate, CancellationToken cancellationToken = default)
    {
        // No receipt expiry is supplied by baseline/provider: authenticate per operation, never reuse v3 tokens.
        if (receipt) return await authenticate(cancellationToken);
        var cacheKey = Key(site, receipt, applicationId);
        var slot = Convert.ToByte(cacheKey[..2], 16) % _gates.Length;
        var gate = _gates[slot];
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_entries.TryGetValue(cacheKey, out var entry) && clock.GetUtcNow() < entry.ExpiresAtUtc) return entry.Token;
            var version = Volatile.Read(ref _versions[slot]);
            var token = await authenticate(cancellationToken);
            if (string.IsNullOrEmpty(token))
                throw new ProviderCallException(ProviderFailure.InvalidResponse, "MISA_TOKEN_MISSING", "MISA authentication returned no token.");
            // Baseline v3 lifetime was 7 days. Keep that limit with UTC and a safety margin.
            // Receipt lifetime is not specified in baseline: keep it request-local rather than guessing.
            foreach (var expired in _entries.Where(pair => pair.Value.ExpiresAtUtc <= clock.GetUtcNow()))
                _entries.TryRemove(expired);
            lock (_storageGate)
            {
                if (_entries.Count < Capacity && version == Volatile.Read(ref _versions[slot]))
                    _entries[cacheKey] = new Entry(token, clock.GetUtcNow().AddDays(7).AddMinutes(-5));
            }
            return token;
        }
        finally { gate.Release(); }
    }

    public void Invalidate(SiteConfiguration site, bool receipt, string applicationId)
    {
        var key = Key(site, receipt, applicationId);
        var slot = Convert.ToByte(key[..2], 16) % _gates.Length;
        lock (_storageGate)
        {
            Interlocked.Increment(ref _versions[slot]);
            _entries.TryRemove(key, out _);
        }
    }

    private string Key(SiteConfiguration site, bool receipt, string applicationId)
    {
        var parts = new[] { receipt ? "receipt" : "v3", site.Id?.ToString("D"),
            receipt ? site.PartnerUrl2 : site.PartnerUrl, site.TaxNumber, site.Username, site.Password, applicationId };
        // Length-delimited values prevent ambiguous combinations; no plaintext password is retained as a cache key.
        var material = string.Concat(parts.Select(value => $"{value?.Length ?? -1}:{value}"));
        return Convert.ToHexString(HMACSHA256.HashData(_fingerprintKey, Encoding.UTF8.GetBytes(material)));
    }

    private sealed record Entry(string Token, DateTimeOffset ExpiresAtUtc);
}
