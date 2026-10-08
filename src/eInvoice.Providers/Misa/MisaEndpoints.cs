using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

internal static class MisaEndpoints
{
    public static string RequireBaseUrl(SiteConfiguration site, MisaLoginOptions options)
    {
        if (!Uri.TryCreate(site.PartnerUrl, UriKind.Absolute, out var address) || address.Scheme != "https" ||
            !site.PartnerUrl.EndsWith("/", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) ||
            !string.IsNullOrEmpty(address.Fragment) ||
            !options.AllowedHosts.Contains(address.Host, StringComparer.OrdinalIgnoreCase))
            throw new ProviderCallException(ProviderFailure.Configuration, "MISA_ENDPOINT_NOT_ALLOWED",
                "MISA requires an HTTPS endpoint on the configured host allowlist.");
        return site.PartnerUrl;
    }
}
