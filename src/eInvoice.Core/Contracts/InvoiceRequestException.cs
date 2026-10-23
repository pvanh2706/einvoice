namespace eInvoice.Core.Contracts;

public sealed class InvoiceRequestException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
