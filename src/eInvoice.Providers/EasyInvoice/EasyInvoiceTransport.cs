using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.EasyInvoice;

public sealed class EasyInvoiceTransport(HttpClient client, EasyInvoiceOptions options, TimeProvider clock)
{
    internal Uri Validate(SiteConfiguration site)
    {
        if (!Uri.TryCreate(site.PartnerUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !uri.AbsolutePath.EndsWith('/') || !options.AllowedHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "EASY_INVOICE_ENDPOINT_NOT_ALLOWED", "EasyInvoice endpoint is not allowed.");
        if (string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password) ||
            site.Username.Any(char.IsControl) || site.Password.Any(char.IsControl))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "EASY_INVOICE_CREDENTIALS_REQUIRED", "EasyInvoice credentials are incomplete.");
        return uri;
    }

    public async Task<JObject> ExecuteAsync(SiteConfiguration site, string path, string jsonBody, bool write, CancellationToken cancellationToken)
    {
        var response = await Send(site, path, jsonBody, write, cancellationToken);
        var json = Parse(response.Bytes);
        if (!response.Success || Scalar(json["Status"]) is { } status && status != "2") throw Rejected(json, site, response.Authentication, response.Status);
        if (Scalar(json["Status"]) != "2") throw Invalid();
        return json;
    }

    public async Task<string> DocumentAsync(SiteConfiguration site, string path, string jsonBody, CancellationToken cancellationToken)
    {
        var response = await Send(site, path, jsonBody, false, cancellationToken);
        if (response.Success && response.Bytes.AsSpan().StartsWith("%PDF-"u8)) return Convert.ToBase64String(response.Bytes);
        try
        {
            var json = Parse(response.Bytes);
            if (!response.Success || Scalar(json["Status"]) is { } status && status != "2") throw Rejected(json, site, response.Authentication, response.Status);
        }
        catch (ProviderCallException error) when (error.Failure == ProviderFailure.InvalidResponse) { }
        throw Invalid("EASY_INVOICE_DOCUMENT_INVALID_RESPONSE");
    }

    private async Task<(byte[] Bytes, int Status, bool Success, string Authentication)> Send(SiteConfiguration site, string path, string jsonBody, bool write, CancellationToken cancellationToken)
    {
        var endpoint = Validate(site);
        var authentication = Authentication(site, clock.GetUtcNow(), Guid.NewGuid().ToString("N"));
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, path));
        request.Headers.Add("Authentication", authentication);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            WriteOperationJournal.MarkResponse((int)response.StatusCode, authentication);
            return (await response.Content.ReadAsByteArrayAsync(cancellationToken), (int)response.StatusCode, response.IsSuccessStatusCode, authentication);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "EASY_INVOICE_TIMEOUT", "EasyInvoice request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "EASY_INVOICE_CONNECTION_ERROR", "EasyInvoice connection failed."); }
    }

    internal static string Authentication(SiteConfiguration site, DateTimeOffset now, string nonce)
    {
        var timestamp = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        // MD5 is the supplier's required wire protocol, not password storage.
        var signature = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("POST" + timestamp + nonce)));
        return $"{signature}:{nonce}:{timestamp}:{site.Username}:{site.Password}";
    }
    private static JObject Parse(byte[] bytes)
    {
        if (bytes.Length == 0) throw Invalid("EASY_INVOICE_EMPTY_RESPONSE");
        try { return JObject.Parse(Encoding.UTF8.GetString(bytes)); } catch (JsonException) { throw Invalid(); }
    }
    internal static string? Scalar(JToken? value) => value switch
    {
        null or { Type: JTokenType.Null } => null,
        JValue { Type: JTokenType.String or JTokenType.Integer } scalar => Convert.ToString(scalar.Value, CultureInfo.InvariantCulture),
        _ => throw Invalid(),
    };
    private static ProviderCallException Rejected(JObject json, SiteConfiguration site, string authentication, int status) =>
        new(ProviderFailure.Rejected, "EASY_INVOICE_OPERATION_REJECTED", "EasyInvoice rejected the request.")
        {
            SupplierCode = "EASY_INVOICE_ITEM_REJECTED",
            SupplierMessage = ProviderErrorMessage.Sanitize(Scalar(json["Message"]), site, authentication, "EASY_INVOICE"),
            SupplierHttpStatus = status,
        };
    internal static ProviderCallException Invalid(string code = "EASY_INVOICE_INVALID_RESPONSE") => new(ProviderFailure.InvalidResponse, code, "EasyInvoice response is invalid.");
}
