namespace eInvoice.Core.Contracts;

public class VoidInvoiceRequest : ISiteScopedRequest
{
    public string? InvoiceId { get; set; }
    public string? ProviderInvoiceNumber { get; set; }
    public string? ProviderInvoiceCode { get; set; }

    public string? Sid { get; set; }


    public string? TransactionId { get; set; } = null;
    /// <summary>
    /// 0 : hoá đơn điện tử <br/> 1 : vé điện tử <br/> 2 : biên lai điện tử
    /// </summary>
    public int InvoiceType { get; set; } = 1;

    public string? FormNo { get; set; }
    public string? Serial { get; set; }
    public string? InvoiceDate { get; set; }
    public string? AdditionalReferenceDate { get; set; }
    public string? AdditionalReferenceDesc { get; set; }
    public string? Reason { get; set; }
    public SystemInformation? System { get; set; } = null;
    public int? ElectronicInvoiceType { get; set; }
    public string? SearchCode { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public UserInfo? UserInfo { get; set; } = null;
    public string? RequestId { get; set; }
}
