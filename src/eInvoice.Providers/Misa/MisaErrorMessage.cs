using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;

namespace eInvoice.Providers.Misa;

internal static class MisaErrorMessage
{
    internal static string? Sanitize(string? message, SiteConfiguration? site = null, string? token = null, string provider = "MISA") =>
        ProviderErrorMessage.Sanitize(message, site, token, provider);
}
