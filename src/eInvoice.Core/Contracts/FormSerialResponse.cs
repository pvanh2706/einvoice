namespace eInvoice.Core.Contracts;

public sealed class FormSerialResponse
{
    public string? InvoiceName { get; set; }
    public string? FormNo { get; set; }
    public string? Serial { get; set; }
    public int Quantity { get; set; }
    public int FromNo { get; set; }
    public int ToNo { get; set; }
    public int CurrentNo { get; set; }
    public int Status { get; set; }
    public string? StartDate { get; set; }
    public bool? IsMultipleTaxRates { get; set; }
}
