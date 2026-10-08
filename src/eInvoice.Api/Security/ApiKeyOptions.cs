using Microsoft.AspNetCore.Authentication;

namespace eInvoice.Api.Security;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "PmsApiKey";
    public List<PmsClient> Clients { get; set; } = [];
}

public sealed class PmsClient
{
    public string ClientId { get; set; } = "";
    public List<Guid> AllowedSiteIds { get; set; } = [];
    public List<PmsApiKey> Keys { get; set; } = [];
}

public sealed class PmsApiKey
{
    public string KeyId { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
