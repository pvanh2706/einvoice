using System.Net.Http.Headers;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Cyberbill.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Cyberbill;

public sealed class CyberbillTransport(HttpClient client, CyberbillOptions options)
{
    public async Task<string> LoginAsync(SiteConfiguration site, CancellationToken cancellationToken)
    {
        var response = await Send(site, EndpointApi.LOGIN, new { username = site.Username, password = site.Password, doanhnghiep_mst = site.TaxNumber }, null, false, cancellationToken);
        var token = response.Json["result"] is JObject result && result["access_token"]?.Type == JTokenType.String
            ? result.Value<string>("access_token") : null;
        if (!response.Success || string.IsNullOrWhiteSpace(token) || response.Json["success"]?.Type == JTokenType.Boolean && !response.Json.Value<bool>("success"))
            throw Rejected(response.Json, site, null, response.Status, "CYBER_BILL_LOGIN_REJECTED");
        if (token.Length > 8192 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && !"._~+/=-".Contains(c))) throw Invalid();
        return token;
    }

    public async Task<JObject> ExecuteAsync(SiteConfiguration site, string path, object body, bool write, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Cyberbill has no endpoint for this operation.");
        var token = await LoginAsync(site, cancellationToken);
        var response = await Send(site, path, body, token, write, cancellationToken);
        var json = response.Json;
        if (!response.Success || json["success"]?.Type == JTokenType.Boolean && !json.Value<bool>("success"))
            throw Rejected(json, site, token, response.Status);
        if (json["success"]?.Type != JTokenType.Boolean || json["result"] is not JObject result || result["maketqua"]?.Type != JTokenType.String)
            throw Invalid();
        if (!json.Value<bool>("success") || result.Value<string>("maketqua") != "01") throw Rejected(json, site, token, response.Status);
        return result;
    }

    private async Task<(JObject Json, int Status, bool Success)> Send(SiteConfiguration site, string path, object body,
        string? token, bool write, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (token is not null) WriteOperationJournal.MarkResponse((int)response.StatusCode, token);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) throw Invalid("CYBER_BILL_EMPTY_RESPONSE");
            JObject json;
            try { json = JObject.Parse(text); } catch (JsonException) { throw Invalid(); }
            return (json, (int)response.StatusCode, response.IsSuccessStatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "CYBER_BILL_TIMEOUT", "Cyberbill request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "CYBER_BILL_CONNECTION_ERROR", "Cyberbill connection failed."); }
    }

    private Uri Validate(SiteConfiguration site)
    {
        if (!Uri.TryCreate(site.PartnerUrl, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            !endpoint.AbsolutePath.EndsWith('/') || !options.AllowedHosts.Contains(endpoint.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "CYBER_BILL_ENDPOINT_NOT_ALLOWED", "Cyberbill endpoint is not allowed.");
        if (string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password) || string.IsNullOrWhiteSpace(site.TaxNumber))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "CYBER_BILL_CREDENTIALS_REQUIRED", "Cyberbill credentials are incomplete.");
        return endpoint;
    }

    private static ProviderCallException Rejected(JObject json, SiteConfiguration site, string? token, int status,
        string code = "CYBER_BILL_OPERATION_REJECTED") => new(ProviderFailure.Rejected, code, "Cyberbill rejected the request.")
        {
            SupplierCode = "CYBER_BILL_ITEM_REJECTED",
            SupplierMessage = ProviderErrorMessage.Sanitize(json["result"] is JObject result && result["motaketqua"]?.Type == JTokenType.String
                ? result.Value<string>("motaketqua") : json["error"] is JObject error && error["details"]?.Type == JTokenType.String
                    ? error.Value<string>("details") : null, site, token, "CYBER_BILL"),
            SupplierHttpStatus = status,
        };
    internal static ProviderCallException Invalid(string code = "CYBER_BILL_INVALID_RESPONSE") =>
        new(ProviderFailure.InvalidResponse, code, "Cyberbill response is invalid.");
}
