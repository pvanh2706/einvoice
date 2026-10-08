namespace eInvoice.Core.Contracts;

public class GetTaxCodeInfoRequest : ISiteScopedRequest
{
    public string? TaxCode { get; set; }
    public SystemInformation? System { get; set; } = null;
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
