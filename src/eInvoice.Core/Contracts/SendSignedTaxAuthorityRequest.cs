namespace eInvoice.Core.Contracts;

public class SendSignedTaxAuthorityRequest
{
    public string? SignatureValue { get; set; }
    public string? MessageCode { get; set; }
    public string? RequestId { get; set; }
}
