namespace eInvoice.Core.Contracts;

public class PublishSignedXmlRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? InvoiceId { get; set; }
    public string? SearchCode { get; set; }
    public string? InvoiceData { get; set; }
    public int InvoiceType { get; set; }
    public string? RequestId { get; set; }
}
