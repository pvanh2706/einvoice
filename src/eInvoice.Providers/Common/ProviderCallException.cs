namespace eInvoice.Providers.Common;

public enum ProviderFailure
{
    Configuration,
    InvalidCredentials,
    Timeout,
    Connection,
    EmptyResponse,
    InvalidResponse,
    Rejected,
    NotFound,
}

public sealed class ProviderCallException(ProviderFailure failure, string code, string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public ProviderFailure Failure { get; } = failure;
    public string Code { get; } = code;
    public string? SupplierCode { get; init; }
    public string? SupplierMessage { get; init; }
    public int? SupplierHttpStatus { get; internal set; }
    public eInvoice.Core.Contracts.InvoiceAttemptInfo? Attempt { get; internal set; }
}
