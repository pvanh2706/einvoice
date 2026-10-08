namespace eInvoice.Core.Contracts;

public class SendEmailRequest : ISiteScopedRequest
{
    public string? Email { get; set; }
    public Guid? InvoiceId { get; set; }
    public SystemInformation? System { get; set; } = null;
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
