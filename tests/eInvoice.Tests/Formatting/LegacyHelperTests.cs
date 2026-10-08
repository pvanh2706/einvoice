using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Sun.Formatting;

namespace eInvoice.Tests.Formatting;

public class LegacyHelperTests
{
    public static IEnumerable<object[]> MoneyCases() => ReadCases("money.json");
    public static IEnumerable<object[]> XmlCases() => ReadCases("xml.json");

    private static IEnumerable<object[]> ReadCases(string filename)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacyHelpers", filename)));
        return document.RootElement.EnumerateArray().Select(item => new object[] { item.Clone() }).ToArray();
    }

    [Theory]
    [MemberData(nameof(MoneyCases))]
    public void Money_readers_match_captured_legacy_output(JsonElement fixture)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var amount = fixture.GetProperty("amount").GetDecimal();
            var actual = fixture.GetProperty("reader").GetString() == "decimal"
                ? MoneyToText.DocTienBangChu(amount, fixture.GetProperty("suffix").GetString()!)
                : NumberReader.NumberToText((double)amount, fixture.GetProperty("suffix").GetBoolean());
            Assert.Equal(fixture.GetProperty("expected").GetString(), actual);
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [Theory]
    [MemberData(nameof(XmlCases))]
    public void Xml_preserves_encoding_escaping_nulls_and_array_shape(JsonElement fixture)
    {
        var count = fixture.GetProperty("count").GetInt32();
        var invoice = new Invoice
        {
            Name = "Khách & <dịch vụ>", Note = null,
            Items = Enumerable.Range(1, count).Select(i => new Item { Amount = 12.5m * i }).ToArray(),
        };
        var actual = fixture.GetProperty("serializer").GetString() switch
        {
            "typed" => XmlHelper.Serialize(invoice),
            "object" => XmlHelperObject.Serialize(invoice),
            "batch" => XmlHelperObjectMulti.Serialize(invoice),
            _ => throw new InvalidOperationException("Unknown fixture serializer"),
        };
        Assert.Equal(fixture.GetProperty("expected").GetString(), actual);
    }

    [Fact]
    public void Typed_xml_preserves_null_contract() => Assert.Null(XmlHelper.Serialize<Invoice?>(null));
}

[XmlRoot("Invoice")]
public class Invoice
{
    public string? Name { get; set; }
    public string? Note { get; set; }
    public Item[]? Items { get; set; }
}
public class Item
{
    public decimal Amount { get; set; }
}
