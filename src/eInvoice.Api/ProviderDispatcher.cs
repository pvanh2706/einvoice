using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Misa;

namespace eInvoice.Api;

public sealed class ProviderDispatcher(IEnumerable<IInvoiceProvider> implementations, MisaWriteJournal misaJournal, TimeProvider clock)
{
    private readonly IReadOnlyDictionary<int, IInvoiceProvider> _providers = implementations.ToDictionary(p => p.PartnerId);

    public IInvoiceProvider Get(int? partner) => partner is { } id && _providers.TryGetValue(id, out var provider) ? provider :
        throw new UnsupportedProviderOperationException("PROVIDER_NOT_IMPLEMENTED", "This provider is not implemented yet.");

    private WriteOperationJournal Journal(IInvoiceProvider provider) => new(misaJournal.DirectoryPath, clock, provider.Name);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, HttpContext http)
    {
        var provider = Get(site.Site.Partner);
        if (!provider.Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "This provider does not support this operation.");
        if (!provider.IsWrite(operation)) return await provider.ExecuteAsync(operation, site, request, http.RequestAborted);
        var keys = http.Request.Headers["Idempotency-Key"];
        if (keys.Count != 1 || string.IsNullOrWhiteSpace(keys[0]))
            throw new InvoiceRequestException("IDEMPOTENCY_KEY_REQUIRED", "A single Idempotency-Key header is required for this operation.");
        WriteOperationJournal.ValidateKey(keys[0]!);
        return await Journal(provider).ExecuteAsync(site.Site.Id!.Value, operation.Replace('/', '-'), keys[0]!, request,
            provider.Claims(operation, request), () => provider.ExecuteAsync(operation, site, request, http.RequestAborted),
            http.RequestAborted, http.TraceIdentifier);
    }

    public async Task<InvoiceAttemptInfo> StatusAsync(Guid siteId, string operation, string key, CancellationToken cancellationToken)
    {
        var result = await misaJournal.StatusAsync(siteId, operation, key, cancellationToken);
        if (result.State != "not-found") return result;
        foreach (var provider in _providers.Values)
        {
            result = await Journal(provider).StatusAsync(siteId, operation, key, cancellationToken);
            if (result.State != "not-found") return result;
        }
        return result;
    }
}
