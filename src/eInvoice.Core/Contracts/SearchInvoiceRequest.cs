namespace eInvoice.Core.Contracts;

public class SearchInvoiceRequest : ISiteScopedRequest
{
    public string? InvoiceId { get; set; }
    public string? FormNo { get; set; }
    public SystemInformation? System { get; set; } = null;
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
