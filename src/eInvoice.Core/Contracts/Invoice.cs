namespace eInvoice.Core.Contracts;

public class Invoice
{
    public string? InvoiceId { get; set; }
    public string? FormNo { get; set; }
    public string? Serial { get; set; }
    public long? InvoiceNo { get; set; }
    public string? InvoiceDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerTax { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CompanyName { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentMethodCode { get; set; }
    public string? PaymentBankAccount { get; set; }
    public string? PaymentBankName { get; set; }
    public string? Notice { get; set; }
    public decimal? SubAmount { get; set; }
    public decimal? ServiceRate { get; set; }
    public decimal? ServiceCharge { get; set; }
    public decimal? BeforeTaxAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? AfterTaxAmount { get; set; }
    public HotelInformation? HotelExtra { get; set; }

    public string? Sid { get; set; }


    public string? RefId { get; set; } = string.Empty;
    /// <summary>
    /// 0 : hoá đơn điện tử <br/> 1 : vé điện tử <br/> 2 : biên lai điện tử
    /// </summary>
    public int? InvoiceType { get; set; } = 1;

    public string? SearchCode { get; set; }
    public string? XmlContent { get; set; }
    public int? BuyerNotGetInvoice { get; set; }
    public int? PricePrecision { get; set; }
    public int? QuantityPrecision { get; set; }
    public bool? IsMultiVATRate { get; set; }
    public decimal? Validation { get; set; }
    // 2 trường thêm cho nhà cung cấp Misa, dùng để đánh dấu là Hóa đơn bán hàng ES-188075
    public bool? IsTaxReduction43 { get; set; }
    public int? TaxReductionType { get; set; }
    public string? TaxReductionDescription { get; set; }
    public decimal? TaxReduction43Amount { get; set; }
    public string? TotalAmountInWords { get; set; }
    public string? PassportNum { get; set; }
    public string? IdentityCard { get; set; }
    public string? BudgetCode { get; set; }
    public string? MaCQT { get; set; }
    public string? UnitCode { get; set; }
    public string? UnitName { get; set; }
    public int? PreviewType { get; set; }
    public string? RequestId { get; set; }
    public int? AmountPrecision { get; set; }

}
