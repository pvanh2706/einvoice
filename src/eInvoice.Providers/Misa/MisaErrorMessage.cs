using System.Text.RegularExpressions;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

/// <summary>Only supplier error message fields are accepted; never expose an envelope, request or auth response.</summary>
internal static class MisaErrorMessage
{
    internal static string? Sanitize(string? message, SiteConfiguration? site = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        var text = message;
        foreach (var secret in new[] { site?.Password, site?.Password2, site?.Username, site?.Username2, site?.MinvoiceToken, token, MisaWriteJournal.ResponseToken })
        {
            if (string.IsNullOrEmpty(secret)) continue;
            text = text.Replace(secret, "[redacted]", StringComparison.OrdinalIgnoreCase)
                .Replace(Uri.EscapeDataString(secret), "[redacted]", StringComparison.OrdinalIgnoreCase);
        }
        text = Regex.Replace(text, @"\bBearer\s+[^\s,;]+", "Bearer [redacted]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "\\b(password|pwd|passWord|token|access_token|api[-_]?key|secret|username)\\b\\s*[:=]\\s*(\"[^\"]*\"|'[^']*'|[^\\s,;]+)",
            "$1=[redacted]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = string.Concat(text.Select(character => char.IsControl(character) ? ' ' : character)).Trim();
        if (text.StartsWith('{') || text.StartsWith('[') && !text.StartsWith("[redacted]", StringComparison.Ordinal) || text.StartsWith('<'))
            return "MISA returned a structured error. Check providerCode and reconcile the invoice.";
        return text.Length <= 1000 ? text : text[..1000] + "…";
    }
}
