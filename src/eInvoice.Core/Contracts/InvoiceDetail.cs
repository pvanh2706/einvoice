namespace eInvoice.Core.Contracts;

public class InvoiceDetail
{
    public string? TransactionDate { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? SubAmount { get; set; }
    public decimal? ServiceRate { get; set; }
    public decimal? ServiceCharge { get; set; }
    public decimal? BeforeTaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? AfterTaxAmount { get; set; }
    public string? Note { get; set; }
    public decimal? ExciseTaxRate { get; set; }
    public decimal? ExciseTaxCharge { get; set; }
    public int? TaxReductionType { get; set; } // % giảm thuế
    public decimal? TaxReduction43Amount { get; set; } // Số tiền giảm thuế quy đổi
    public decimal? TaxReduction43AmountOC { get; set; } // Số tiền giảm thuế nguyên tệ
    public string? RoomCode { get; set; }
    public string? ItemType { get; set; }
}
