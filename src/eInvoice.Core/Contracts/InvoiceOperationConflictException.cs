namespace eInvoice.Core.Contracts;

public sealed class InvoiceOperationConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
