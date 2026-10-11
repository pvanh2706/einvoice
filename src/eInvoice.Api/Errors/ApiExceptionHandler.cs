using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using eInvoice.Providers.Common;
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
            UnsupportedProviderOperationException unsupported => (501, unsupported.Code, unsupported.Message),
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
            ProviderCallException { Failure: ProviderFailure.NotFound } provider =>
                (404, provider.Code, "Provider invoice was not found"),
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
        if (exception is ProviderCallException providerFailure)
        {
            if (providerFailure.SupplierMessage is not null) extensions["providerMessage"] = providerFailure.SupplierMessage;
            if (providerFailure.SupplierHttpStatus is not null) extensions["providerHttpStatus"] = providerFailure.SupplierHttpStatus;
            if (providerFailure.Attempt is not null)
            {
                extensions["attempt"] = providerFailure.Attempt;
                if (OperationStatusUrl(providerFailure.Attempt) is { } url) extensions["attemptStatusUrl"] = url;
            }
            extensions["errorSource"] = providerFailure.Failure is ProviderFailure.Configuration or ProviderFailure.InvalidCredentials ? "gateway" : "provider";
        }
        if (exception is InvoiceOperationConflictException conflictDetail)
        {
            extensions["errorSource"] = "gateway";
            if (conflictDetail.PreviousAttempt is not null)
            {
                extensions["previousAttempt"] = conflictDetail.PreviousAttempt;
                if (OperationStatusUrl(conflictDetail.PreviousAttempt) is { } url) extensions["attemptStatusUrl"] = url;
            }
            if (conflictDetail.InvoiceClaim is { } claim)
            {
                var colon = claim.IndexOf(':');
                if (colon > 0 && claim[..colon] is "publish" or "xml-create")
                    extensions["attemptStatusUrl"] = "/api/eInvoice/invoice-attempts?invoiceId=" + Uri.EscapeDataString(claim[(colon + 1)..]) + "&phase=" + claim[..colon];
            }
        }
        await Results.Problem(statusCode: status, title: title, extensions: extensions).ExecuteAsync(context);
        return true;
    }

    private static string? OperationStatusUrl(InvoiceAttemptInfo attempt) =>
        attempt.Operation is null || attempt.IdempotencyKey is null ? null :
            "/api/eInvoice/operations/" + Uri.EscapeDataString(attempt.Operation) + "/" + Uri.EscapeDataString(attempt.IdempotencyKey);
}
