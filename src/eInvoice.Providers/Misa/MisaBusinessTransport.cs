using System.Net;
using System.Net.Http.Headers;
using System.Text;
using eInvoice.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Misa;

public sealed class MisaBusinessTransport(HttpClient httpClient, IMisaLoginClient login,
    IMisaReceiptLoginClient receiptLogin, MisaLoginOptions options, MisaTokenCache cache)
{
    internal Task<string> ReceiptTokenAsync(SiteConfiguration site, CancellationToken cancellationToken) =>
        receiptLogin.AuthenticateAsync(site, cancellationToken);

    internal async Task<string?> SendAsync(SiteConfiguration site, string path, object? body,
        bool receipt, bool longRunning, CancellationToken cancellationToken, string? receiptToken = null)
    {
        var baseUrl = MisaEndpoints.RequireBaseUrl(site, options, receipt);
        var token = receipt ? receiptToken ?? await receiptLogin.AuthenticateAsync(site, cancellationToken)
            : await login.AuthenticateAsync(site, cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!longRunning) timeout.CancelAfter(TimeSpan.FromSeconds(100));
        try
        {
            using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, baseUrl + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("CompanyTaxCode", site.TaxNumber);
            if (body is not null) request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            if (body is not null) MisaWriteJournal.MarkDispatched();
            using var response = await httpClient.SendAsync(request, timeout.Token);
            var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
            try { return ReadEnvelope(responseBody); }
            catch (ProviderCallException ex)
            {
                // Invalidate for the next call only. Never replay a write after authentication/transport failure.
                if (!receipt && (response.StatusCode == HttpStatusCode.Unauthorized || MisaErrorCodes.IsTokenError(ex.SupplierCode)))
                    cache.Invalidate(site, false, options.ApplicationId);
                throw;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex)
        {
            throw new ProviderCallException(ProviderFailure.Timeout, "MISA_OPERATION_TIMEOUT", "MISA operation timed out; its outcome may need reconciliation.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderCallException(ProviderFailure.Connection, "MISA_OPERATION_CONNECTION_ERROR", "Unable to complete the MISA operation.", ex);
        }
        catch (FormatException ex) { throw InvalidResponse(ex); }
    }

    internal static string? ReadEnvelope(string body)
    {
        if (string.IsNullOrEmpty(body))
            throw new ProviderCallException(ProviderFailure.EmptyResponse, "MISA_OPERATION_EMPTY_RESPONSE", "MISA returned an empty operation response.");
        try
        {
            var result = JsonConvert.DeserializeObject<Envelope>(body);
            if (result is null) throw InvalidResponse();
            if (!result.Success || !string.IsNullOrEmpty(result.ErrorCode) || result.Errors?.Count > 0)
                throw new ProviderCallException(ProviderFailure.Rejected, "MISA_OPERATION_REJECTED", "MISA rejected the operation.")
                { SupplierCode = MisaErrorCodes.Safe(result.ErrorCode ?? result.Errors?.FirstOrDefault()) };
            return result.Data;
        }
        catch (JsonException ex) { throw InvalidResponse(ex); }
    }

    internal static List<T> ReadArray<T>(string? data, bool legacyEscapes = false)
    {
        if (string.IsNullOrWhiteSpace(data)) throw InvalidResponse();
        try
        {
            JToken parsed;
            try
            {
                parsed = JToken.Parse(data);
                if (parsed.Type == JTokenType.String) parsed = JToken.Parse(parsed.Value<string>()!);
            }
            catch (JsonReaderException) when (legacyEscapes)
            {
                parsed = JToken.Parse(data.Replace("\\\"", "\"", StringComparison.Ordinal));
            }
            if (parsed is not JArray array || array.Any(item => item.Type == JTokenType.Null)) throw InvalidResponse();
            return array.ToObject<List<T>>() ?? throw InvalidResponse();
        }
        catch (JsonException ex) { throw InvalidResponse(ex); }
    }

    internal static ProviderCallException InvalidResponse(Exception? inner = null) =>
        new(ProviderFailure.InvalidResponse, "MISA_OPERATION_INVALID_RESPONSE", "MISA returned an invalid operation response.", inner);

    private sealed class Envelope
    {
        [JsonProperty("Success", Required = Required.Always)] public bool Success { get; set; }
        [JsonProperty("Data")] public string? Data { get; set; }
        [JsonProperty("ErrorCode")] public string? ErrorCode { get; set; }
        [JsonProperty("Errors")] public List<string>? Errors { get; set; }
    }
}
