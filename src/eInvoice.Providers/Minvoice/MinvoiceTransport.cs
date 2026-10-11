using System.Net.Http.Headers;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Minvoice.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Minvoice;

public sealed class MinvoiceTransport(HttpClient client, MinvoiceOptions options)
{
    public async Task<string> LoginAsync(SiteConfiguration site, bool health, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site.PartnerUrl);
        if (string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password) || health && string.IsNullOrWhiteSpace(site.Username2))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "MINVOICE_CREDENTIALS_REQUIRED", "Minvoice credentials are incomplete.");
        var response = await Send(site, new Uri(endpoint, EndpointApi.LOGIN), HttpMethod.Post,
            new { ma_dvcs = health ? site.Username2 : "VP", username = site.Username, password = site.Password }, null,
            health ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(100), false, cancellationToken);
        var json = Parse(response.Bytes);
        var token = json["token"]?.Type == JTokenType.String ? json.Value<string>("token") : null;
        if (!response.Success || string.IsNullOrWhiteSpace(token)) throw Rejected(json, site, null, response.Status, "MINVOICE_LOGIN_REJECTED");
        if (token.Length > 8192 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && !"._~+/=-".Contains(c))) throw Invalid();
        return token;
    }

    public async Task<JObject> ExecuteAsync(SiteConfiguration site, string path, object? body, bool write, CancellationToken cancellationToken, bool get = false)
    {
        var endpoint = Validate(site.PartnerUrl);
        var token = await LoginAsync(site, false, cancellationToken);
        var longCall = path is EndpointApi.CREATE or EndpointApi.PUBLISH;
        var response = await Send(site, new Uri(endpoint, path), get ? HttpMethod.Get : HttpMethod.Post, body, token,
            longCall ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(100), write, cancellationToken);
        var json = Parse(response.Bytes);
        if (!response.Success || Scalar(json["code"]) is { } code && code != "00") throw Rejected(json, site, token, response.Status);
        if (Scalar(json["code"]) != "00") throw Invalid();
        return json;
    }

    public async Task<string> DocumentAsync(SiteConfiguration site, string providerId, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site.PartnerUrl);
        var token = await LoginAsync(site, false, cancellationToken);
        var response = await Send(site, new Uri(endpoint, EndpointApi.DOCUMENT + "?id=" + Uri.EscapeDataString(providerId)), HttpMethod.Get, null,
            token, TimeSpan.FromSeconds(100), false, cancellationToken);
        if (response.Success && response.Bytes.AsSpan().StartsWith("%PDF-"u8)) return Convert.ToBase64String(response.Bytes);
        var text = Encoding.UTF8.GetString(response.Bytes);
        if (text.Contains("There is no row at position 0.", StringComparison.Ordinal))
            throw new ProviderCallException(ProviderFailure.NotFound, "MINVOICE_DOCUMENT_NOT_FOUND", "Minvoice invoice was not found.") { SupplierHttpStatus = response.Status };
        try
        {
            var json = JObject.Parse(text);
            if (!response.Success || Scalar(json["code"]) is { } code && code != "00") throw Rejected(json, site, token, response.Status);
        }
        catch (JsonException) { }
        throw Invalid("MINVOICE_DOCUMENT_INVALID_RESPONSE");
    }

    public async Task<JObject> TaxCodeAsync(SiteConfiguration site, string taxCode, CancellationToken cancellationToken)
    {
        // Baseline fixed public registry host, still explicitly opt-in via the server allowlist.
        var endpoint = Validate(EndpointApi.TAX_CODE + "?tax=" + Uri.EscapeDataString(taxCode), isBase: false);
        _ = await LoginAsync(site, false, cancellationToken);
        var response = await Send(site, endpoint, HttpMethod.Get, null, null, TimeSpan.FromSeconds(100), false, cancellationToken);
        var json = Parse(response.Bytes);
        if (!response.Success || json["error"] is { Type: not JTokenType.Null }) throw Rejected(json, site, null, response.Status);
        return json;
    }

    private async Task<(byte[] Bytes, int Status, bool Success)> Send(SiteConfiguration site, Uri endpoint, HttpMethod method,
        object? body, string? token, TimeSpan timeout, bool write, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bear", token);
        if (body is not null) request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (token is not null) WriteOperationJournal.MarkResponse((int)response.StatusCode, token);
            return (await response.Content.ReadAsByteArrayAsync(deadline.Token), (int)response.StatusCode, response.IsSuccessStatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "MINVOICE_TIMEOUT", "Minvoice request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "MINVOICE_CONNECTION_ERROR", "Minvoice connection failed."); }
    }
    private Uri Validate(string? url, bool isBase = true)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Fragment) || isBase && (!string.IsNullOrEmpty(endpoint.Query) || !endpoint.AbsolutePath.EndsWith('/')) ||
            !options.AllowedHosts.Contains(endpoint.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "MINVOICE_ENDPOINT_NOT_ALLOWED", "Minvoice endpoint is not allowed.");
        return endpoint;
    }
    private static JObject Parse(byte[] bytes)
    {
        if (bytes.Length == 0) throw Invalid("MINVOICE_EMPTY_RESPONSE");
        try { return JObject.Parse(Encoding.UTF8.GetString(bytes)); } catch (JsonException) { throw Invalid(); }
    }
    internal static string? Scalar(JToken? value) => value switch
    {
        null or { Type: JTokenType.Null } => null,
        JValue { Type: JTokenType.String or JTokenType.Integer } scalar => Convert.ToString(scalar.Value, System.Globalization.CultureInfo.InvariantCulture),
        _ => throw Invalid(),
    };
    private static ProviderCallException Rejected(JObject json, SiteConfiguration site, string? token, int status, string code = "MINVOICE_OPERATION_REJECTED") =>
        new(ProviderFailure.Rejected, code, "Minvoice rejected the request.")
        {
            SupplierCode = Scalar(json["code"]) is "01" or "02" or "03" or "04" or "21" or "22" or "23" ? Scalar(json["code"]) : "MINVOICE_ITEM_REJECTED",
            SupplierMessage = ProviderErrorMessage.Sanitize(json["message"]?.Type == JTokenType.String ? json.Value<string>("message") : json["Message"]?.Type == JTokenType.String ? json.Value<string>("Message") : null, site, token, "MINVOICE"),
            SupplierHttpStatus = status,
        };
    internal static ProviderCallException Invalid(string code = "MINVOICE_INVALID_RESPONSE") => new(ProviderFailure.InvalidResponse, code, "Minvoice response is invalid.");
}
