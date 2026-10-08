using System.Text.Json;
using System.Text.Json.Serialization;
using eInvoice.Api.Errors;
using eInvoice.Api.Security;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Admin;
using eInvoice.Providers.Misa;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables().AddCommandLine(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthentication(ApiKeyOptions.SchemeName)
    .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(ApiKeyOptions.SchemeName,
        options => builder.Configuration.GetSection("Authentication").Bind(options));
builder.Services.AddOptions<ApiKeyOptions>(ApiKeyOptions.SchemeName).Validate(options =>
    options.Clients.Select(client => client.ClientId).Distinct(StringComparer.Ordinal).Count() == options.Clients.Count &&
    options.Clients.SelectMany(client => client.Keys).Select(key => key.Sha256)
        .Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.Clients.Sum(client => client.Keys.Count) &&
    options.Clients.All(client => !string.IsNullOrWhiteSpace(client.ClientId) && client.AllowedSiteIds.Count > 0 &&
        client.AllowedSiteIds.All(id => id != Guid.Empty) && client.Keys.Count > 0 &&
        client.Keys.All(key => !string.IsNullOrWhiteSpace(key.KeyId) && key.Sha256.Length == 64 &&
            key.Sha256.All(Uri.IsHexDigit))), "API key clients must have IDs, allowed sites and SHA-256 key hashes.")
    .ValidateOnStart();
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:Misa").Get<MisaLoginOptions>() ?? new MisaLoginOptions());
builder.Services.AddHttpClient<IMisaLoginClient, MisaLoginClient>(client => client.Timeout = TimeSpan.FromSeconds(100))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
builder.Services.AddHttpClient<IMisaFormSerialClient, MisaFormSerialClient>(client => client.Timeout = TimeSpan.FromSeconds(90))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
builder.Services.AddScoped<ISiteConfigurationResolver, SiteConfigurationResolver>();
builder.Services.AddHttpClient<IAdminClient, AdminClient>(client =>
{
    var configuredAddress = builder.Configuration["eInvoiceAdmin:BaseAddress"];
    if (!string.IsNullOrWhiteSpace(configuredAddress))
    {
        if (!Uri.TryCreate(configuredAddress, UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(address.UserInfo) ||
            !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
        {
            throw new InvalidOperationException("eInvoiceAdmin:BaseAddress must be an absolute HTTP(S) URL without credentials/query/fragment.");
        }
        client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/");
    }
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("eInvoiceAdmin:TimeoutSeconds", 30));
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var status = context.HttpContext.Response.StatusCode;
    await Results.Problem(statusCode: status, title: "Request could not be completed",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = $"HTTP_{status}",
            ["requestId"] = context.HttpContext.TraceIdentifier,
        }).ExecuteAsync(context.HttpContext);
});
app.UseAuthentication();
app.UseAuthorization();

// Liveness only. This does not contact admin/providers or claim the invoice adapters are ready.
app.MapGet("/health/live", () => Results.Ok(new { service = "eInvoice", status = "alive" }));

app.MapPost("/api/eInvoice/health-check", async (ProviderHealthCheckRequest request, HttpContext http,
    ISiteConfigurationResolver resolver, IMisaLoginClient misa) =>
{
    var siteId = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
    var configuration = await resolver.ResolveAsync(request.SiteConfigInfo, siteId, http.RequestAborted);
    if (configuration.Site.Id != siteId)
        throw new SiteConfigurationException("Resolved site configuration does not match the authorized site.");
    if (configuration.Site.Partner != 6)
        return Results.Problem(statusCode: 501, title: "This provider is not implemented yet",
            extensions: new Dictionary<string, object?> { ["code"] = "PROVIDER_NOT_IMPLEMENTED", ["requestId"] = http.TraceIdentifier });
    _ = await misa.AuthenticateAsync(configuration.Site, http.RequestAborted);
    return Results.Ok(new { provider = "MISA", connected = true, siteId, requestId = http.TraceIdentifier });
}).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();

app.MapPost("/api/eInvoice/form-serials", async (GetFormSerialsRequest request, HttpContext http,
    ISiteConfigurationResolver resolver, IMisaFormSerialClient misa) =>
{
    if (request.InvoiceType != 0)
        return Results.Problem(statusCode: 400, title: "Form/serial lookup currently supports invoiceType 0 only",
            extensions: new Dictionary<string, object?> { ["code"] = "INVOICE_TYPE_NOT_SUPPORTED", ["requestId"] = http.TraceIdentifier });
    var siteId = (Guid)http.Items[SiteAuthorizationFilter.ContextKey]!;
    var configuration = await resolver.ResolveAsync(request.SiteConfigInfo, siteId, http.RequestAborted);
    if (configuration.Site.Id != siteId)
        throw new SiteConfigurationException("Resolved site configuration does not match the authorized site.");
    if (configuration.Site.Partner != 6)
        return Results.Problem(statusCode: 501, title: "This provider is not implemented yet",
            extensions: new Dictionary<string, object?> { ["code"] = "PROVIDER_NOT_IMPLEMENTED", ["requestId"] = http.TraceIdentifier });
    var forms = await misa.GetAsync(configuration.Site, http.RequestAborted);
    return Results.Ok(new { provider = "MISA", siteId, forms, requestId = http.TraceIdentifier });
}).RequireAuthorization().AddEndpointFilter<SiteAuthorizationFilter>();

app.Run();

public partial class Program;
