using eInvoice.Providers.Common;
using eInvoice.Core.Contracts;
using Newtonsoft.Json;

namespace eInvoice.Providers.Misa;

public sealed class MisaReceiptLoginClient(HttpClient httpClient, MisaLoginOptions options) : IMisaReceiptLoginClient
{
    public async Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseUrl = MisaEndpoints.RequireBaseUrl(site, options, receipt: true);
        if (site.Id is null || site.Id == Guid.Empty || string.IsNullOrWhiteSpace(site.TaxNumber) ||
            string.IsNullOrWhiteSpace(site.Username) || string.IsNullOrWhiteSpace(site.Password))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "MISA_RECEIPT_CREDENTIALS_REQUIRED",
                "MISA receipt login requires site ID, tax number, username and password.");
        // External key/casing retained. Escape values so &, +, # and Unicode credentials stay intact.
        var endpoint = new Uri(baseUrl + "user/token?appid=" + Uri.EscapeDataString(site.Id.Value.ToString("D")) +
            "&taxcode=" + Uri.EscapeDataString(site.TaxNumber) + "&username=" + Uri.EscapeDataString(site.Username) +
            "&password=" + Uri.EscapeDataString(site.Password));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("CompanyTaxcode", site.Id.Value.ToString("D"));
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(body))
                throw new ProviderCallException(ProviderFailure.EmptyResponse, "MISA_RECEIPT_EMPTY_RESPONSE", "MISA returned an empty receipt login response.");
            var result = JsonConvert.DeserializeObject<ReceiptTokenEnvelope>(body);
            if (result is null) throw new JsonSerializationException("Receipt envelope is missing.");
            if (result.Success && !string.IsNullOrEmpty(result.Data) && string.IsNullOrEmpty(result.ErrorCode) &&
                (result.Errors is null || result.Errors.Count == 0)) return result.Data;
            throw new ProviderCallException(ProviderFailure.Rejected, "MISA_RECEIPT_LOGIN_REJECTED", "MISA rejected receipt authentication.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex)
        {
            throw new ProviderCallException(ProviderFailure.Timeout, "MISA_RECEIPT_TIMEOUT", "MISA receipt login timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderCallException(ProviderFailure.Connection, "MISA_RECEIPT_CONNECTION_ERROR", "Unable to authenticate MISA receipts.", ex);
        }
        catch (JsonException ex)
        {
            throw new ProviderCallException(ProviderFailure.InvalidResponse, "MISA_RECEIPT_INVALID_RESPONSE", "MISA returned invalid receipt login JSON.", ex);
        }
    }

    private sealed class ReceiptTokenEnvelope
    {
        [JsonProperty("Success", Required = Required.Always)] public bool Success { get; set; }
        [JsonProperty("Data")] public string? Data { get; set; }
        [JsonProperty("ErrorCode")] public string? ErrorCode { get; set; }
        [JsonProperty("Errors")] public List<string>? Errors { get; set; }
    }
}
