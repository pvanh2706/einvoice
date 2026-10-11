using System.Globalization;

namespace eInvoice.Capture.Viettel;

internal static class ProviderDate
{
    // Explicitly characterize the legacy server's UTC+7 date. No runtime provider dependency.
    public static long ToUnixMilliseconds(string date) => (long)(
        DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddHours(-7) -
        new DateTime(1970, 1, 1)).TotalMilliseconds;
}
