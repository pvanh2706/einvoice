namespace eInvoice.Core.Contracts;

/// <summary>Implemented by API requests that may carry site credentials/configuration.</summary>
public interface ISiteScopedRequest
{
    SiteInfo? SiteConfigInfo { get; set; }
}
