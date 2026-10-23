namespace eInvoice.Core.Contracts;

public class GetFormSerialsRequest : ISiteScopedRequest
{
    public SiteInfo? SiteConfigInfo { get; set; }
    /// <summary>
    /// 0 : hoá đơn điện tử <br/> 1 : vé điện tử <br/> 2 : biên lai điện tử
    /// </summary>
    public int? InvoiceType { get; set; } = 0;
    public string? RequestId { get; set; }
}
