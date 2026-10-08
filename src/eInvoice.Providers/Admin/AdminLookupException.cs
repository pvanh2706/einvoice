using System.Net;

namespace eInvoice.Providers.Admin;

public enum AdminFailure
{
    MissingConfiguration,
    Timeout,
    Connection,
    HttpError,
    InvalidResponse,
    MissingSite,
}

public sealed class AdminLookupException(AdminFailure failure, string message,
    HttpStatusCode? statusCode = null, Exception? innerException = null) : Exception(message, innerException)
{
    public AdminFailure Failure { get; } = failure;
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
