namespace eInvoice.Core.Contracts;

public class GetXmlHashRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; }
    public string? CertBase64 { get; set; }
    public string? RequestId { get; set; }
}
