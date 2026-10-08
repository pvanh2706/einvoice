using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

internal static class MisaEndpoints
{
    public static string RequireBaseUrl(SiteConfiguration site, MisaLoginOptions options, bool receipt = false)
    {
        var configuredUrl = receipt ? site.PartnerUrl2 : site.PartnerUrl;
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var address) || address.Scheme != "https" ||
            !configuredUrl.EndsWith("/", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) ||
            !string.IsNullOrEmpty(address.Fragment) ||
            !options.AllowedHosts.Contains(address.Host, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "MISA_ENDPOINT_NOT_ALLOWED",
                "MISA requires an HTTPS endpoint on the configured host allowlist.");
        return configuredUrl;
    }
}
