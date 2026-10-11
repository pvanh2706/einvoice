namespace eInvoice.Core.Contracts;

public class AdjustOrReplaceInvoiceRequest : CreateInvoiceRequest
{
    public string? OriginalInvoiceId { get; set; }//Mã HĐ bị thay thế/điều chỉnh
    public string? OriginalProviderInvoiceNumber { get; set; }
    public Invoice? OriginalInvoice { get; set; }
    [global::System.Text.Json.Serialization.JsonIgnore(Condition = global::System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginalProviderInvoiceCode { get; set; }
}
