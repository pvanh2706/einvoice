using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public interface IMisaReceiptLoginClient
{
    Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default);
}
