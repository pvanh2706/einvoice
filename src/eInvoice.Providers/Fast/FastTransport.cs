using System.Text;
using System.Xml.Linq;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Fast.Mapping;
using eInvoice.Providers.Fast.Wire;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Fast;

public sealed class FastTransport(HttpClient client, FastOptions options)
{
    internal void Validate(SiteConfiguration site, bool mtt)
    {
        FastInfoToken token;
        try { token = ProviderMapping.Run(() => FastRequestMapper.GetFastInfoTokenFromJson(site, mtt), "FAST"); }
        catch (Newtonsoft.Json.JsonException)
        { throw new ProviderCallException(ProviderFailure.InvalidCredentials, "FAST_CREDENTIALS_REQUIRED", "FAST credential JSON is invalid."); }
        ValidateUrl(token.PartnerUrl, true);
        if (new[] { token.clientCode, token.proxyCode, token.user, token.UnitCode, token.password }.Any(string.IsNullOrWhiteSpace))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "FAST_CREDENTIALS_REQUIRED", "FAST configuration is incomplete.");
        if (new[] { token.clientCode, token.proxyCode, token.user, token.UnitCode, token.password }.Any(v => v.Any(char.IsControl)) ||
            token.clientCode.IndexOfAny(['&', '?', '#']) >= 0 || token.proxyCode.IndexOfAny(['&', '?', '#']) >= 0)
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "FAST_CREDENTIALS_REQUIRED", "FAST credential values are invalid.");
    }

    private Uri ValidateUrl(string? url, bool baseUrl = false)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.UserInfo.Length > 0 || uri.Fragment.Length > 0 || (baseUrl && (uri.Query.Length > 0 || !uri.AbsolutePath.EndsWith('/'))) ||
            !options.AllowedHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "FAST_ENDPOINT_NOT_ALLOWED", "FAST endpoint is not allowed.");
        return uri;
    }

    internal Task<string> SendAsync(FastApiRequest payload, bool write, CancellationToken ct) =>
        SendAsync(payload.urlAPI, payload.bodyPost, "text/plain", payload.headersAPI, write, ct);
    internal Task<string> SendAsync(FastApiSoapRequest payload, bool write, CancellationToken ct) =>
        SendAsync(payload.urlAPI, payload.bodyXMLPost, "text/xml", null, write, ct);

    private async Task<string> SendAsync(string url, string body, string mediaType, Dictionary<string, string>? headers, bool write, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ValidateUrl(url));
        request.Content = new StringContent(body, Encoding.UTF8, mediaType);
        if (headers is not null)
            foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
        try
        {
            if (write) WriteOperationJournal.MarkDispatched();
            using var response = await client.SendAsync(request, ct);
            WriteOperationJournal.MarkResponse((int)response.StatusCode, "");
            if (!response.IsSuccessStatusCode)
                throw new ProviderCallException(ProviderFailure.Rejected, "FAST_HTTP_ERROR", "FAST rejected the request.") { SupplierHttpStatus = (int)response.StatusCode };
            var text = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(text)) throw Invalid();
            return text;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new ProviderCallException(ProviderFailure.Timeout, "FAST_TIMEOUT", "FAST request timed out."); }
        catch (HttpRequestException)
        { throw new ProviderCallException(ProviderFailure.Connection, "FAST_CONNECTION_ERROR", "FAST connection failed."); }
    }

    internal static string Element(string xml, string name)
    {
        try
        {
            var element = XDocument.Parse(xml).Descendants().SingleOrDefault(e => e.Name.LocalName == name);
            return !string.IsNullOrWhiteSpace(element?.Value) ? element.Value : throw Invalid();
        }
        catch (System.Xml.XmlException) { throw Invalid(); }
        catch (InvalidOperationException) { throw Invalid(); }
    }
    internal static JObject Json(string text, SiteConfiguration site, string? token = null)
    {
        JObject json;
        try { json = JObject.Parse(text); } catch (Newtonsoft.Json.JsonException) { throw Invalid(); }
        if (json["Success"]?.Type != JTokenType.Integer) throw Invalid();
        if (json.Value<int>("Success") != 1)
            throw new ProviderCallException(ProviderFailure.Rejected, "FAST_OPERATION_REJECTED", "FAST rejected the request.")
            { SupplierCode = "FAST_OPERATION_REJECTED", SupplierMessage = ProviderErrorMessage.Sanitize(json["Message"]?.Type == JTokenType.String ? json.Value<string>("Message") : null, site, token, "FAST") };
        return json;
    }
    internal static ProviderCallException Invalid() => new(ProviderFailure.InvalidResponse, "FAST_INVALID_RESPONSE", "FAST response is invalid.");
}
