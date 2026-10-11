using System.Net.Http.Headers;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Misa;
using eInvoice.Providers.Viettel.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Viettel;

public sealed class ViettelTransport(HttpClient client, ViettelOptions options)
{
    public async Task<string> LoginAsync(SiteConfiguration site, CancellationToken cancellationToken)
    {
        Validate(site);
        var response = await Send(site, EndpointApi.LOGIN, new { username = site.Username, password = site.Password },
            null, form: false, write: false, cancellationToken);
        var token = response.Json["access_token"]?.Type == JTokenType.String ? Scalar(response.Json, "access_token") : null;
        if (!response.Success || string.IsNullOrWhiteSpace(token))
            throw Rejected(response.Json, site, null, response.Status, "VIETTEL_LOGIN_REJECTED");
        if (token.Length > 8192 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && !"._~+/=-".Contains(c)))
            throw Invalid("VIETTEL_LOGIN_INVALID_RESPONSE");
        return token;
    }

    public async Task<JObject> ExecuteAsync(SiteConfiguration site, string path, object body, bool form, bool write,
        CancellationToken cancellationToken, bool batch = false)
    {
        // Baseline logs in for each operation; token is request-local and never stored in shared HttpClient headers.
        var token = await LoginAsync(site, cancellationToken);
        var response = await Send(site, path, body, token, form, write, cancellationToken);
        var json = response.Json;
        if (!response.Success || HasError(json))
            throw Rejected(json, site, token, response.Status);
        if (!batch && json.Property("errorCode") is null) throw Invalid();
        if (batch && json["createInvoiceOutputs"] is not JArray)
            throw Invalid();
        return json;
    }

    private async Task<(JObject Json, int Status, bool Success)> Send(SiteConfiguration site, string path, object body,
        string? token, bool form, bool write, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (token is not null) request.Headers.Add("Cookie", "access_token=" + token);
        request.Content = form
            ? new FormUrlEncodedContent(JObject.FromObject(body).Properties().Select(p =>
                new KeyValuePair<string, string>(p.Name, p.Value.Type == JTokenType.Null ? "" : p.Value.ToString())))
            : new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (token is not null) WriteOperationJournal.MarkResponse((int)response.StatusCode, token);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text)) throw Invalid("VIETTEL_EMPTY_RESPONSE");
            JObject json;
            try { json = JObject.Parse(text); }
            catch (JsonException) { throw Invalid(); }
            return (json, (int)response.StatusCode, response.IsSuccessStatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "VIETTEL_TIMEOUT", "Viettel request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "VIETTEL_CONNECTION_ERROR", "Viettel connection failed."); }
    }

    private Uri Validate(SiteConfiguration site)
    {
        if (!Uri.TryCreate(site.PartnerUrl, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            !endpoint.AbsolutePath.EndsWith('/') || !options.AllowedHosts.Contains(endpoint.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "VIETTEL_ENDPOINT_NOT_ALLOWED", "Viettel endpoint is not allowed.");
        if (string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password) || string.IsNullOrWhiteSpace(site.TaxNumber))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "VIETTEL_CREDENTIALS_REQUIRED", "Viettel credentials are incomplete.");
        return endpoint;
    }

    internal static string? Scalar(JObject json, string name) => json[name] switch
    {
        null or { Type: JTokenType.Null } => null,
        JValue { Type: JTokenType.String or JTokenType.Integer } value => Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture),
        _ => throw Invalid(),
    };
    internal static bool HasError(JObject json) =>
        json.Property("errorCode") is not null && !string.IsNullOrEmpty(Scalar(json, "errorCode")) && Scalar(json, "errorCode") != "200" ||
        json.Property("code") is not null && Scalar(json, "code") != "200";

    internal static ProviderCallException Rejected(JObject json, SiteConfiguration site, string? token, int? status = null,
        string code = "VIETTEL_OPERATION_REJECTED") =>
        new(ProviderFailure.Rejected, code, "Viettel rejected the request.")
        {
            // Numeric HTTP-like codes are the only known codes here; unknown values are not echoed as supplier text.
            SupplierCode = SafeCode(Scalar(json, "errorCode") ?? Scalar(json, "code")),
            SupplierMessage = MisaErrorMessage.Sanitize(json["description"]?.Type == JTokenType.String ? json.Value<string>("description") : null,
                site, token, "VIETTEL"),
            SupplierHttpStatus = status,
        };

    internal static string? SafeCode(string? code) => string.IsNullOrEmpty(code) || code == "200" ? null :
        code is "400" or "401" or "403" or "404" or "409" or "422" or "500" or "502" or "503" or "504" ? code : "VIETTEL_ITEM_REJECTED";
    internal static ProviderCallException Invalid(string code = "VIETTEL_INVALID_RESPONSE") =>
        new(ProviderFailure.InvalidResponse, code, "Viettel response is invalid.");
}
