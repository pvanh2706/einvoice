namespace eInvoice.Core.Contracts;

public class SendSignedTaxAuthorityRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? SignatureValue { get; set; }
    public string? MessageCode { get; set; }
    public string? RequestId { get; set; }
}
