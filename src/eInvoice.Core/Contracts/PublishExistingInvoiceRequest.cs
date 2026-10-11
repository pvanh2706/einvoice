namespace eInvoice.Core.Contracts;

public class PublishExistingInvoiceRequest : ISiteScopedRequest
{
    public string? InvoiceId { get; set; }
    public string? ProviderInvoiceCode { get; set; }
    public string? ProviderInvoiceNumber { get; set; }
    public int InvoiceType { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? RequestId { get; set; }
}
