namespace eInvoice.Core.Contracts;

public class DownloadPdfRequest : ISiteScopedRequest
{
    public string? InvoiceId { get; set; }
    public string? ProviderInvoiceNumber { get; set; }
    public string? ProviderInvoiceCode { get; set; }

    public string? TransactionId { get; set; }
    public int? InvoiceType { get; set; } = 1;


    public string? SearchCode { get; set; }

    public string? FormNo { get; set; }
    public SystemInformation? System { get; set; } = null;
    public int? ElectronicInvoiceType { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public string? TypeFileDownload { get; set; }
    public string? Serial { get; set; }
    public string? InvoiceDate { get; set; }
    public string? RequestId { get; set; }
}
