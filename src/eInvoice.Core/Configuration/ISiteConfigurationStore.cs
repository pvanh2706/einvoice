namespace eInvoice.Core.Configuration;

/// <summary>Server-owned site configuration source. Implement this interface when moving from JSON to a database.</summary>
public interface ISiteConfigurationStore
{
    Task<ResolvedSite> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default);
}
