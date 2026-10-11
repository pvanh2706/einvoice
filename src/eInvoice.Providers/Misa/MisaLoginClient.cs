using eInvoice.Providers.Common;
using System.Text;
using eInvoice.Core.Contracts;
using Newtonsoft.Json;

namespace eInvoice.Providers.Misa;

/// <summary>Stateless MISA v3 login. Receipt authentication and token caching are separate pending work.</summary>
public sealed class MisaLoginClient(HttpClient httpClient, MisaLoginOptions options) : IMisaLoginClient
{
    public async Task<string> AuthenticateAsync(SiteConfiguration site, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(site.TaxNumber) || string.IsNullOrWhiteSpace(site.Username) ||
            string.IsNullOrWhiteSpace(site.Password))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "MISA_CREDENTIALS_REQUIRED",
                "MISA tax number, username and password are required.");
        var baseUrl = MisaEndpoints.RequireBaseUrl(site, options);

        // Concatenation deliberately preserves legacy base URL semantics. Do not rewrite provider keys.
        var endpoint = new Uri(baseUrl + "auth/token", UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("CompanyTaxcode", site.TaxNumber);
        request.Content = new StringContent(JsonConvert.SerializeObject(new
        {
            appid = options.ApplicationId,
            taxcode = site.TaxNumber,
            username = site.Username,
            password = site.Password,
        }), Encoding.UTF8, "application/json");
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            // MISA embeds business errors in 4xx/5xx bodies. Read and parse before interpreting status.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(body))
                throw new ProviderCallException(ProviderFailure.EmptyResponse, "MISA_EMPTY_RESPONSE",
                    "MISA returned an empty login response.");
            var result = JsonConvert.DeserializeObject<LoginResponse>(body);
            if (result is null)
                throw new ProviderCallException(ProviderFailure.InvalidResponse, "MISA_INVALID_RESPONSE",
                    "MISA returned an invalid login response.");
            if (result.Success && !string.IsNullOrEmpty(result.Data)) return result.Data;
            // Classify the known legacy error combinations without exposing arbitrary provider response content.
            var code = "MISA_LOGIN_REJECTED";
            if (Has(result.ErrorCode, "UnAuthorize"))
            {
                if (result.Errors?.Any(error => Has(error, "UserNotExist")) == true) code = "MISA_ACCOUNT_NOT_AUTHORIZED";
                else if (result.Errors?.Any(error => Has(error, "MisaIdError")) == true) code = "MISA_INVALID_CREDENTIALS";
            }
            throw new ProviderCallException(ProviderFailure.Rejected, code, "MISA rejected the login request.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex)
        {
            throw new ProviderCallException(ProviderFailure.Timeout, "MISA_TIMEOUT", "MISA login timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderCallException(ProviderFailure.Connection, "MISA_CONNECTION_ERROR", "Unable to connect to MISA.", ex);
        }
        catch (JsonException ex)
        {
            throw new ProviderCallException(ProviderFailure.InvalidResponse, "MISA_INVALID_RESPONSE", "MISA returned invalid JSON.", ex);
        }
    }

    private static bool Has(string? value, string code) => value?.Contains(code, StringComparison.OrdinalIgnoreCase) == true;
    private sealed class LoginResponse
    {
        [JsonProperty("Success")] public bool Success { get; set; }
        [JsonProperty("Data")] public string? Data { get; set; }
        [JsonProperty("ErrorCode")] public string? ErrorCode { get; set; }
        [JsonProperty("Errors")] public List<string>? Errors { get; set; }
    }
}
