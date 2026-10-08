using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using Microsoft.AspNetCore.Diagnostics;

namespace eInvoice.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted || context.RequestAborted.IsCancellationRequested) return false;

        var (status, code, title) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "INVALID_REQUEST", "Invalid request body"),
            InvoiceRequestException request => (400, request.Code, request.Message),
            InvoiceOperationConflictException conflict => (409, conflict.Code, conflict.Message),
            SiteConfigurationException => (400, "INVALID_SITE_CONFIGURATION", "Invalid site configuration"),
            SiteStoreException { Failure: SiteStoreFailure.NotConfigured } =>
                (503, "SITE_STORE_NOT_CONFIGURED", "Site configuration JSON file is not configured"),
            SiteStoreException { Failure: SiteStoreFailure.NotFound } =>
                (404, "SITE_CONFIGURATION_NOT_FOUND", "Site configuration was not found"),
            SiteStoreException { Failure: SiteStoreFailure.InvalidData } =>
                (503, "SITE_STORE_INVALID_DATA", "Site configuration JSON file is invalid"),
            SiteStoreException => (503, "SITE_STORE_UNAVAILABLE", "Site configuration store is unavailable"),
            ProviderCallException { Failure: ProviderFailure.Configuration } provider =>
                (503, provider.Code, "Provider endpoint is not configured"),
            ProviderCallException { Failure: ProviderFailure.InvalidCredentials } provider =>
                (400, provider.Code, "Provider credentials are incomplete"),
            ProviderCallException { Failure: ProviderFailure.Timeout } provider =>
                (504, provider.Code, "Provider request timed out"),
            ProviderCallException { Failure: ProviderFailure.Rejected } provider =>
                (422, provider.Code, "Provider rejected the request"),
            ProviderCallException provider => (502, provider.Code, "Provider response could not be processed"),
            _ => (500, "EINVOICE_INTERNAL_ERROR", "An internal error occurred"),
        };

        // Record the category and ID. Exception messages/response bodies may contain credentials.
        logger.LogError("Request {RequestId} failed with {Code} ({ExceptionType}).",
            context.TraceIdentifier, code, exception.GetType().Name);
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = code,
            ["requestId"] = context.TraceIdentifier,
        };
        if (exception is ProviderCallException { SupplierCode: not null } providerError)
            extensions["providerCode"] = providerError.SupplierCode;
        await Results.Problem(statusCode: status, title: title, extensions: extensions).ExecuteAsync(context);
        return true;
    }
}
