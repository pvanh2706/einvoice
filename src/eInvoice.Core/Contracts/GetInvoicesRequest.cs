namespace eInvoice.Core.Contracts;

public class GetInvoicesRequest : ISiteScopedRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? Serial { get; set; }
    public SystemInformation? System { get; set; } = null;
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
