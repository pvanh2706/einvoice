using System.Globalization;

namespace eInvoice.Providers.Common;

public static class ProviderDate
{
    public static long ToUnixMilliseconds(string date)
    {
        var local = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToUnixTimeMilliseconds();
    }
}
