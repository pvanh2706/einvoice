using eInvoice.Providers.Common;
using System.Globalization;
using System.Net.Http.Headers;
using eInvoice.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Misa;

public sealed class MisaFormSerialClient(HttpClient httpClient, IMisaLoginClient login,
    MisaLoginOptions options, TimeProvider timeProvider, MisaTokenCache? tokenCache = null) : IMisaFormSerialClient
{
    public async Task<IReadOnlyList<FormSerialResponse>> GetAsync(SiteConfiguration site,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseUrl = MisaEndpoints.RequireBaseUrl(site, options);
        var token = await login.AuthenticateAsync(site, cancellationToken);
        // D013: query the Vietnamese calendar year, independent of the machine's local timezone.
        var year = timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Year;
        var endpoint = new Uri(baseUrl + "code/itg/InvoicePublishing/templates?invyear=" +
            year.ToString(CultureInfo.InvariantCulture), UriKind.Absolute);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("CompanyTaxCode", site.TaxNumber);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            // Preserve MISA's business envelope even when HTTP status is 4xx/5xx.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                if (string.IsNullOrEmpty(body))
                    throw new ProviderCallException(ProviderFailure.EmptyResponse, "MISA_FORM_SERIAL_EMPTY_RESPONSE",
                        "MISA returned an empty form/serial response.");
                return Parse(body);
            }
            catch (ProviderCallException ex)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || MisaErrorCodes.IsTokenError(ex.SupplierCode))
                    tokenCache?.Invalidate(site, false, options.ApplicationId);
                throw;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex)
        {
            throw new ProviderCallException(ProviderFailure.Timeout, "MISA_FORM_SERIAL_TIMEOUT",
                "MISA form/serial request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderCallException(ProviderFailure.Connection, "MISA_FORM_SERIAL_CONNECTION_ERROR",
                "Unable to retrieve MISA forms/serials.", ex);
        }
        catch (JsonException ex)
        {
            throw InvalidResponse(ex);
        }
        catch (FormatException ex)
        {
            throw InvalidResponse(ex);
        }
    }

    private static IReadOnlyList<FormSerialResponse> Parse(string body)
    {
        var envelope = JsonConvert.DeserializeObject<TemplateEnvelope>(body);
        if (envelope is null || envelope.Errors is null) throw InvalidResponse();
        if (!envelope.Success || envelope.Errors.Count != 0 || !string.IsNullOrEmpty(envelope.ErrorCode))
            throw new ProviderCallException(ProviderFailure.Rejected, "MISA_FORM_SERIAL_REJECTED",
                "MISA rejected the form/serial request.")
                { SupplierCode = MisaErrorCodes.Safe(envelope.ErrorCode ?? envelope.Errors.FirstOrDefault()) };
        if (string.IsNullOrWhiteSpace(envelope.Data)) throw InvalidResponse();

        JToken data;
        try
        {
            data = JToken.Parse(envelope.Data);
            if (data.Type == JTokenType.String) data = JToken.Parse(data.Value<string>()!);
        }
        catch (JsonReaderException)
        {
            // Endpoint-specific legacy fallback for escaped/wrapped arrays. Decode valid JSON first
            // so escaped quotes in template names are preserved instead of replaced globally.
            data = JToken.Parse(envelope.Data.Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\"[", "[", StringComparison.Ordinal).Replace("]\"", "]", StringComparison.Ordinal));
        }
        if (data is not JArray) throw InvalidResponse();
        var templates = data.ToObject<List<TemplateData?>>()!;
        var result = new List<FormSerialResponse>();
        foreach (var template in templates)
        {
            if (template is null) throw InvalidResponse();
            if (template.IsInheritFromOldTemplate) continue;
            if (template.TemplateName is null) throw InvalidResponse();
            result.Add(new FormSerialResponse
            {
                InvoiceName = template.TemplateName.Length >= 40 ? template.TemplateName[..40] : template.TemplateName,
                FormNo = template.InvTemplateNo,
                Serial = template.InvSeries,
                Status = template.Inactive ? 0 : 1,
                IsMultipleTaxRates = template.BusinessAreas == 34,
            });
        }
        return result;
    }

    private static ProviderCallException InvalidResponse(Exception? inner = null) =>
        new(ProviderFailure.InvalidResponse, "MISA_FORM_SERIAL_INVALID_RESPONSE",
            "MISA returned an invalid form/serial response.", inner);

    private sealed class TemplateEnvelope
    {
        [JsonProperty("Success", Required = Required.Always)] public bool Success { get; set; }
        [JsonProperty("Data")] public string? Data { get; set; }
        [JsonProperty("Errors")] public List<string>? Errors { get; set; } = [];
        [JsonProperty("ErrorCode")] public string? ErrorCode { get; set; }
    }

    private sealed class TemplateData
    {
        [JsonProperty("TemplateName")] public string? TemplateName { get; set; }
        [JsonProperty("InvTemplateNo")] public string? InvTemplateNo { get; set; }
        [JsonProperty("InvSeries")] public string? InvSeries { get; set; }
        [JsonProperty("Inactive")] public bool Inactive { get; set; }
        [JsonProperty("BusinessAreas")] public int BusinessAreas { get; set; }
        [JsonProperty("IsInheritFromOldTemplate")] public bool IsInheritFromOldTemplate { get; set; }
    }
}
