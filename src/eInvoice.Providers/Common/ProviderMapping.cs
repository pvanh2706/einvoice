using System.Globalization;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Common;

internal static class ProviderMapping
{
    internal static T Run<T>(Func<T> map, string provider)
    {
        var prior = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; return map(); }
        catch (Exception error) when (error is FormatException or ArgumentException or NullReferenceException or OverflowException)
        { throw new InvoiceRequestException("INVALID_INVOICE_MAPPING_INPUT", "Invoice fields do not satisfy " + provider + " mapping requirements."); }
        finally { CultureInfo.CurrentCulture = prior; }
    }
}
