using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace eInvoice.Api.Security;

public sealed class ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options,
    ILoggerFactory loggerFactory, UrlEncoder encoder, TimeProvider clock)
    : AuthenticationHandler<ApiKeyOptions>(options, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var values))
            return Task.FromResult(AuthenticateResult.NoResult());
        if (!Request.IsHttps || values.Count != 1 || string.IsNullOrWhiteSpace(values[0]) || values[0]!.Length > 1024)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key request."));

        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!));
        foreach (var client in Options.Clients)
        {
            foreach (var key in client.Keys)
            {
                if (key.ExpiresAtUtc is { } expiry && expiry <= clock.GetUtcNow()) continue;
                // Configuration is validated at startup; hashes, not plaintext keys, are stored on the server.
                if (!CryptographicOperations.FixedTimeEquals(candidate, Convert.FromHexString(key.Sha256))) continue;
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, client.ClientId),
                    new("keyId", key.KeyId),
                };
                claims.AddRange(client.AllowedSiteIds.Select(site => new Claim("siteId", site.ToString("D"))));
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
            }
        }
        return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
    }
}
