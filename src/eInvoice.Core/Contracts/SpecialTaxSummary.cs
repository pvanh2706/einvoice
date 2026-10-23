namespace eInvoice.Core.Contracts;

public class SpecialTaxSummary
{
    public decimal? BeforeSpecialTaxAmount { get; set; }
    public decimal? SpecialTaxAmount { get; set; }
    public decimal? SpecialTaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? AfterTaxAmount { get; set; }
}
