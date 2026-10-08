namespace eInvoice.Core.Contracts;

public class PublishSignedXmlRequest
{
    public string? InvoiceId { get; set; }
    public string? SearchCode { get; set; }
    public string? InvoiceData { get; set; }
    public int InvoiceType { get; set; }
    public string? RequestId { get; set; }
}
