using eInvoice.Core.Contracts;

namespace eInvoice.Core.Configuration;

/// <summary>Uses request configuration first, exactly as the legacy GetCompany branch does.</summary>
public sealed class SiteConfigurationResolver(ISiteConfigurationStore store) : ISiteConfigurationResolver
{
    public Task<ResolvedSite> ResolveAsync(SiteInfo? suppliedConfiguration, Guid? siteId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (suppliedConfiguration is not null)
        {
            if (suppliedConfiguration.Site is null)
            {
                // Do not fall back to a different tenant/config source when an explicit configuration is invalid.
                throw new SiteConfigurationException("Request configuration must contain a site.");
            }

            return Task.FromResult(new ResolvedSite(suppliedConfiguration.Site,
                suppliedConfiguration.ExtraDataMap?.ToArray() ?? []));
        }

        if (siteId is null || siteId == Guid.Empty)
        {
            throw new SiteConfigurationException("A site ID is required when request configuration is absent.");
        }

        return store.GetSiteAsync(siteId.Value, cancellationToken);
    }
}
