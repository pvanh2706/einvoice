namespace eInvoice.Core.Contracts;

public class CreateInvoiceRequest : ISiteScopedRequest
{
    public Invoice? Invoice { get; set; }
    public List<InvoiceDetail>? Details { get; set; }
    public List<ServiceSummary>? ServiceSummaries { get; set; }
    public List<SpecialTaxSummary>? SpecialTaxSummaries { get; set; }
    public List<TaxSummary>? TaxSummaries { get; set; }
    public HotelInformation? HotelExtra { get; set; }
    public SystemInformation? System { get; set; } = null;
    public List<ExtraField>? ExtraData { get; set; }
    public int? ElectronicInvoiceType { get; set; }
    public SiteInfo? SiteConfigInfo { get; set; } = null;
    public UserInfo? UserInfo { get; set; } = null;
}
