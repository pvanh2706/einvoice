using eInvoice.Core.Contracts;

namespace eInvoice.Core.Configuration;

public interface ISiteConfigurationResolver
{
    Task<ResolvedSite> ResolveAsync(SiteInfo? suppliedConfiguration, Guid? siteId,
        CancellationToken cancellationToken = default);
}
