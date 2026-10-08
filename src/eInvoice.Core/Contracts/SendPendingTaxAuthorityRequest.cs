namespace eInvoice.Core.Contracts;

/// <summary>MISA HSM sends the provider's pending queue for this site; there is no invoice ID filter on the wire.</summary>
public sealed class SendPendingTaxAuthorityRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? RequestId { get; set; }
}
