namespace eInvoice.Core.Contracts;

public sealed class InvoiceBatchRequest : ISiteScopedRequest
{
    public List<CreateInvoiceRequest>? Invoices { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? RequestId { get; set; }
}
