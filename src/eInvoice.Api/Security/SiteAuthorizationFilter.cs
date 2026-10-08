using eInvoice.Core.Contracts;

namespace eInvoice.Api.Security;

public sealed class SiteAuthorizationFilter : IEndpointFilter
{
    public const string ContextKey = "eInvoice.AuthorizedSiteId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue("X-Site-Id", out var values) || values.Count != 1 ||
            !Guid.TryParse(values[0], out var siteId) || siteId == Guid.Empty)
            return Error(http, 400, "SITE_ID_REQUIRED", "A valid X-Site-Id header is required");

        if (!http.User.Claims.Any(claim => claim.Type == "siteId" &&
            Guid.TryParse(claim.Value, out var allowed) && allowed == siteId))
            return Error(http, 403, "SITE_ACCESS_DENIED", "Access to this site is denied");

        // Never accept credentials for a second site through request configuration.
        foreach (var argument in context.Arguments)
        {
            var supplied = (argument as ISiteScopedRequest)?.SiteConfigInfo;
            if (supplied is not null && supplied.Site?.Id != siteId)
                return Error(http, 400, "SITE_CONFIGURATION_MISMATCH", "Request configuration must match X-Site-Id");
        }

        http.Items[ContextKey] = siteId;
        return await next(context);
    }

    private static IResult Error(HttpContext context, int status, string code, string title) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?>
        {
            ["code"] = code,
            ["requestId"] = context.TraceIdentifier,
        });
}
