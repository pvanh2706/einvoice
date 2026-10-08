using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public interface IMisaLoginClient
{
    Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default);
    Task<string> CheckConnectionAsync(SiteConfiguration site, CancellationToken cancellationToken = default) =>
        AuthenticateAsync(site, cancellationToken);
}
