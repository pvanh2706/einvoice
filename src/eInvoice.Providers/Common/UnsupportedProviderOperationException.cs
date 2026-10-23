namespace eInvoice.Providers.Common;

public sealed class UnsupportedProviderOperationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
