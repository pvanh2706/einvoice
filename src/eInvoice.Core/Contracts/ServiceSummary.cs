namespace eInvoice.Core.Contracts;

public class ServiceSummary
{
    public decimal? ServiceRate { get; set; }
    public decimal? ServiceCharge { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? AfterTaxAmount { get; set; }
}
