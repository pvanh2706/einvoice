namespace eInvoice.Core.Contracts;

public class ProviderHealthCheckRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
