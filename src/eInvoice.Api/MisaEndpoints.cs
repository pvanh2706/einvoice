using eInvoice.Api.Security;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa;
using Microsoft.AspNetCore.Mvc;

namespace eInvoice.Api;

internal static class MisaApiEndpoints
{
    public static void MapMisaApi(this WebApplication app)
    {
        app.MapGet("/api/eInvoice/misa/capabilities", () => Results.Ok(new
        {
            provider = "MISA",
            actions = MisaImplementedCapabilities.Routes.OrderBy(pair => (int)pair.Key).Select(pair => new
            {
                action = (int)pair.Key, name = pair.Key.ToString(), route = "/api/eInvoice/" + pair.Value,
            }),
        })).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();
        Map<CreateInvoiceRequest>(app, "publish", async (service, site, request, token) => await service.PublishAsync(site, request, token),
            request => Claims(request, "publish"));
        Map<InvoiceBatchRequest>(app, "publish-batch", async (service, site, request, token) => await service.PublishBatchAsync(site, request, token),
            request => request.Invoices?.SelectMany(item => item is null ? [] : Claims(item, "publish")).ToArray() ?? []);
        Map<AdjustOrReplaceInvoiceRequest>(app, "preview", async (service, site, request, token) => await service.PreviewAsync(site, request, token));
        Map<AdjustOrReplaceInvoiceRequest>(app, "adjust", async (service, site, request, token) => await service.AdjustAsync(site, request, false, token),
            request => Claims(request, MisaInvoiceService.RequiresAdjustmentXml(request) ? "xml-create" : "publish"));
        Map<AdjustOrReplaceInvoiceRequest>(app, "replace", async (service, site, request, token) => await service.AdjustAsync(site, request, true, token),
            request => Claims(request, MisaInvoiceService.RequiresAdjustmentXml(request) ? "xml-create" : "publish"));
        Map<DownloadPdfRequest>(app, "document", async (service, site, request, token) => await service.DownloadAsync(site, request, token));
        Map<VoidInvoiceRequest>(app, "void", async (service, site, request, token) =>
        {
            await service.VoidAsync(site, request, token);
            return new { success = true };
        }, request => ["void:" + request.InvoiceType + ":" + (request.InvoiceType == 0 ? request.SearchCode : request.TransactionId)]);
        Map<SendToTaxAuthorityRequest>(app, "status", async (service, site, request, token) => await service.StatusAsync(site, request, token));
        Map<SendPendingTaxAuthorityRequest>(app, "tax/send-pending", async (service, site, request, token) =>
            new { providerData = await service.SendTaxAsync(site, request, token) }, _ => []);
        Map<CreateInvoiceRequest>(app, "xml/create", async (service, site, request, token) => await service.CreateXmlAsync(site, request, token),
            request => Claims(request, "xml-create"));
        Map<GetXmlHashRequest>(app, "xml/hash", async (service, site, request, token) => await service.HashAsync(site, request, token));
        Map<SendSignedTaxAuthorityRequest>(app, "tax/send-signed", async (service, site, request, token) =>
            new { providerData = await service.SendSignedTaxAsync(site, request, token) }, request => ["tax-signed:" + request.MessageCode]);
        Map<PublishSignedXmlRequest>(app, "xml/publish", async (service, site, request, token) => await service.PublishSignedXmlAsync(site, request, token),
            request => ["publish:" + request.InvoiceId]);

        app.MapGet("/api/eInvoice/operations/{operation}/{idempotencyKey}", async (string operation, string idempotencyKey,
            HttpContext http, MisaWriteJournal journal) =>
        {
            var siteId = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
            return Results.Ok(await journal.StatusAsync(siteId, operation, idempotencyKey, http.RequestAborted));
        }).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();

        app.MapGet("/api/eInvoice/invoice-attempts", async (string invoiceId, string? phase, HttpContext http, MisaWriteJournal journal) =>
        {
            var siteId = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
            return Results.Ok(await journal.InvoiceStatusAsync(siteId, invoiceId, phase ?? "publish", http.RequestAborted));
        }).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();
    }

    private static IReadOnlyList<string> Claims(CreateInvoiceRequest request, string phase) =>
        request.Invoice?.InvoiceId is { } id ? [phase + ":" + id] : [];

    private static void Map<T>(WebApplication app, string operation,
        Func<IMisaInvoiceService, ResolvedSite, T, CancellationToken, Task<object?>> execute,
        Func<T, IReadOnlyList<string>>? claims = null) where T : ISiteScopedRequest
    {
        app.MapPost("/api/eInvoice/" + operation, async ([FromBody] T request, HttpContext http,
            ISiteConfigurationResolver resolver, IMisaInvoiceService service, MisaWriteJournal journal) =>
        {
            var siteId = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
            string? idempotencyKey = null;
            if (claims is not null)
            {
                var keys = http.Request.Headers["Idempotency-Key"];
                if (keys.Count != 1 || string.IsNullOrWhiteSpace(keys[0]))
                    throw new InvoiceRequestException("IDEMPOTENCY_KEY_REQUIRED", "A single Idempotency-Key header is required for this operation.");
                idempotencyKey = keys[0]!;
                MisaWriteJournal.ValidateKey(idempotencyKey);
            }
            var configuration = await resolver.ResolveAsync(request.SiteConfigInfo, siteId, http.RequestAborted);
            if (configuration.Site.Id != siteId)
                throw new SiteConfigurationException("Resolved site configuration does not match the authorized site.");
            if (configuration.Site.Partner != 6)
                return Results.Problem(statusCode: 501, title: "This provider is not implemented yet",
                    extensions: new Dictionary<string, object?> { ["code"] = "PROVIDER_NOT_IMPLEMENTED", ["requestId"] = http.TraceIdentifier });
            object? result;
            if (claims is null) result = await execute(service, configuration, request, http.RequestAborted);
            else
            {
                result = await journal.ExecuteAsync(siteId, operation.Replace('/', '-'), idempotencyKey!, request!, claims(request),
                    () => execute(service, configuration, request, http.RequestAborted), http.RequestAborted, http.TraceIdentifier);
            }
            return Results.Ok(new { provider = "MISA", siteId, data = result, requestId = http.TraceIdentifier });
        }).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();
    }
}
