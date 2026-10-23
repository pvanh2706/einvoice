using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public sealed class CachedMisaLoginClient(MisaLoginClient login, MisaTokenCache cache, MisaLoginOptions options) : IMisaLoginClient
{
    public Task<string> CheckConnectionAsync(SiteConfiguration site, CancellationToken cancellationToken = default) =>
        login.AuthenticateAsync(site, cancellationToken);
    public Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default)
    {
        _ = MisaEndpoints.RequireBaseUrl(site, options);
        return cache.GetAsync(site, false, options.ApplicationId, token => login.AuthenticateAsync(site, token), cancellationToken);
    }
}
