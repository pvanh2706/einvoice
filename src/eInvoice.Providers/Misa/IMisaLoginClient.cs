using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public interface IMisaLoginClient
{
    Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default);
}
