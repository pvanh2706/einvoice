namespace eInvoice.Core.Configuration;

public interface IAdminClient
{
    Task<ResolvedSite> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default);
}
