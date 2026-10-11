using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Common;

public interface IInvoiceProvider
{
    int PartnerId { get; }
    string Name { get; }
    IReadOnlyDictionary<InvoiceAction, string> Routes { get; }
    bool Supports(string operation);
    bool IsWrite(string operation);
    IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request);
    Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken);
    Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken);
}
