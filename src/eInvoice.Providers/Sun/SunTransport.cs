using System.Net;
using System.Net.Http.Headers;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Sun.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Sun;

public sealed class SunTransport(HttpClient client, SunOptions options)
{
    public async Task<string> LoginAsync(SiteConfiguration site, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site, site.PartnerUrl2);
        var response = await Send(endpoint, EndpointApi.LOGIN, new { username = site.Username, password = site.Password }, null, false, false, cancellationToken);
        var token = response.Json["token"]?.Type == JTokenType.String ? response.Json.Value<string>("token") : null;
        if (!response.Success || string.IsNullOrWhiteSpace(token)) throw Rejected(response.Json, site, null, response.Status, "SUN_LOGIN_REJECTED");
        if (token.Length > 8192 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && !"._~+/=-".Contains(c))) throw Invalid();
        return token;
    }

    public async Task<JObject> ExecuteAsync(SiteConfiguration site, string path, object body, bool write, CancellationToken cancellationToken, bool pdf = false)
    {
        // Baseline has a separate endpoint for form-encoded cancellation. Validate before login/write.
        var form = path == EndpointApi.VOID;
        var endpoint = Validate(site, form ? site.PartnerUrl : site.PartnerUrl2);
        var token = await LoginAsync(site, cancellationToken);
        var response = await Send(endpoint, path, body, token, form, write, cancellationToken);
        var json = response.Json;
        if (!response.Success || json["error"] is { Type: not JTokenType.Null } ||
            json["summary"] is JObject summary && Scalar(summary["error"]) is { } error && error != "0")
            throw Rejected(json, site, token, response.Status);
        if (pdf)
        {
            if (json["pdf"]?.Type != JTokenType.String) throw Invalid();
        }
        else if (json["summary"] is not JObject valid || Scalar(valid["error"]) != "0" || json["success"] is not JArray)
            throw Invalid();
        return json;
    }

    private async Task<(JObject Json, int Status, bool Success)> Send(Uri endpoint, string path, object body, string? token, bool form, bool write, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = form
            ? new FormUrlEncodedContent(JObject.FromObject(body).Properties().Select(p => new KeyValuePair<string, string>(p.Name, p.Value.Type == JTokenType.Null ? "" : p.Value.ToString())))
            : new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (token is not null) WriteOperationJournal.MarkResponse((int)response.StatusCode, token);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) throw Invalid("SUN_EMPTY_RESPONSE");
            try { return (JObject.Parse(text), (int)response.StatusCode, response.IsSuccessStatusCode); }
            catch (JsonException) { throw Invalid(); }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "SUN_TIMEOUT", "Sun request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "SUN_CONNECTION_ERROR", "Sun connection failed."); }
    }

    private Uri Validate(SiteConfiguration site, string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            !endpoint.AbsolutePath.EndsWith('/') || !options.AllowedHosts.Contains(endpoint.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "SUN_ENDPOINT_NOT_ALLOWED", "Sun endpoint is not allowed.");
        if (string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password) || string.IsNullOrWhiteSpace(site.TaxNumber))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "SUN_CREDENTIALS_REQUIRED", "Sun credentials are incomplete.");
        return endpoint;
    }
    internal static string? Scalar(JToken? value) => value switch
    {
        null or { Type: JTokenType.Null } => null,
        JValue { Type: JTokenType.String or JTokenType.Integer } scalar => Convert.ToString(scalar.Value, System.Globalization.CultureInfo.InvariantCulture),
        _ => throw Invalid(),
    };
    private static ProviderCallException Rejected(JObject json, SiteConfiguration site, string? token, int? status, string code = "SUN_OPERATION_REJECTED")
    {
        var message = json["summary"] is JObject summary ? Scalar(summary["des_err"]) : null;
        if (message is null && json["error"]?.Type == JTokenType.String) message = json.Value<string>("error");
        return new ProviderCallException(ProviderFailure.Rejected, code, "Sun rejected the request.")
        {
            SupplierCode = "SUN_ITEM_REJECTED",
            SupplierMessage = ProviderErrorMessage.Sanitize(WebUtility.HtmlDecode(message), site, token, "SUN"),
            SupplierHttpStatus = status,
        };
    }
    internal static ProviderCallException Invalid(string code = "SUN_INVALID_RESPONSE") => new(ProviderFailure.InvalidResponse, code, "Sun response is invalid.");
}
