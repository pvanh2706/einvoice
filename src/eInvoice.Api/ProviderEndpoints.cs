using eInvoice.Api.Security;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Misa;

namespace eInvoice.Api;

public static class ProviderEndpoints
{
    public static void MapProviderApi(this WebApplication app)
    {
        Map<CreateInvoiceRequest>(app, "draft");
        Map<CreateInvoiceRequest>(app, "draft/edit");
        Map<GetFormSerialsRequest>(app, "certificate");
        Map<SendToTaxAuthorityRequest>(app, "tax/send");
        Map<SearchInvoiceRequest>(app, "search");
        Map<DownloadPdfRequest>(app, "document/draft");
        Map<CreateInvoiceRequest>(app, "create");
        Map<AdjustOrReplaceInvoiceRequest>(app, "change");
        Map<DeleteInvoiceRequest>(app, "delete");
        Map<PublishExistingInvoiceRequest>(app, "publish-existing");
        Map<GetInvoicesRequest>(app, "invoices");
        Map<GetTaxCodeInfoRequest>(app, "tax-code");
        Map<SendEmailRequest>(app, "email");
        Map<AdjustOrReplaceInvoiceRequest>(app, "adjust/increase");
        Map<AdjustOrReplaceInvoiceRequest>(app, "adjust/decrease");
        Map<AdjustOrReplaceInvoiceRequest>(app, "adjust/information");
        app.MapGet("/api/eInvoice/capabilities", async (HttpContext http, ISiteConfigurationResolver resolver, ProviderDispatcher providers) =>
        {
            var site = await Resolve(http, resolver, null);
            var name = site.Site.Partner == 6 ? "MISA" : providers.Get(site.Site.Partner).Name;
            var routes = site.Site.Partner == 6 ? MisaImplementedCapabilities.Routes : providers.Get(site.Site.Partner).Routes;
            return Results.Ok(new { provider = name, actions = routes.OrderBy(p => (int)p.Key).Select(p =>
                new { action = (int)p.Key, name = p.Key.ToString(), route = "/api/eInvoice/" + p.Value }) });
        }).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();
    }

    private static void Map<T>(WebApplication app, string operation) where T : ISiteScopedRequest =>
        app.MapPost("/api/eInvoice/" + operation, async (T request, HttpContext http, ISiteConfigurationResolver resolver, ProviderDispatcher providers) =>
        {
            var site = await Resolve(http, resolver, request.SiteConfigInfo);
            if (site.Site.Partner == 6) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "This operation is not implemented for MISA.");
            var result = await providers.ExecuteAsync(operation, site, request, http);
            return Results.Ok(new { provider = providers.Get(site.Site.Partner).Name, siteId = site.Site.Id, data = result, requestId = http.TraceIdentifier });
        }).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();

    private static async Task<ResolvedSite> Resolve(HttpContext http, ISiteConfigurationResolver resolver, SiteInfo? supplied)
    {
        var id = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
        var site = await resolver.ResolveAsync(supplied, id, http.RequestAborted);
        if (site.Site.Id != id) throw new SiteConfigurationException("Resolved site configuration does not match the authorized site.");
        return site;
    }
}
