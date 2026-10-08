namespace eInvoice.Core.Contracts;

public class GetXmlHashRequest
{
    public string? CertBase64 { get; set; }
    public string? RequestId { get; set; }
}
