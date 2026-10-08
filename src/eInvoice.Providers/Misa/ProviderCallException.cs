namespace eInvoice.Providers.Misa;

public enum ProviderFailure
{
    Configuration,
    InvalidCredentials,
    Timeout,
    Connection,
    EmptyResponse,
    InvalidResponse,
    Rejected,
}

public sealed class ProviderCallException(ProviderFailure failure, string code, string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public ProviderFailure Failure { get; } = failure;
    public string Code { get; } = code;
    public string? SupplierCode { get; init; }
}
