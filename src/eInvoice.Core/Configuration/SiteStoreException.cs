namespace eInvoice.Core.Configuration;

public enum SiteStoreFailure { NotConfigured, NotFound, InvalidData, Unavailable }

public sealed class SiteStoreException(SiteStoreFailure failure) : Exception("Site configuration store could not resolve the requested site.")
{
    public SiteStoreFailure Failure { get; } = failure;
}
