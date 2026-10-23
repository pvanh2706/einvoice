namespace eInvoice.Core.Contracts;

public class SendToTaxAuthorityRequest : ISiteScopedRequest
{
    public List<string>? InvoiceIds { get; set; }
    public string? FormNo { get; set; }
    public string? Serial { get; set; }
    public int InvoiceType { get; set; }
    public int? ElectronicInvoiceType { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
