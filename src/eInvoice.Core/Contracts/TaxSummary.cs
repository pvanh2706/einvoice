namespace eInvoice.Core.Contracts;

public class TaxSummary
{
    public decimal? BeforeTaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
}
