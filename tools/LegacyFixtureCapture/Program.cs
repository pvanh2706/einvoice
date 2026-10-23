using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using ezInvoice.Application.Heldper;
using ezInvoice.Infra.ThirdParty.SunEInvoice.Services;

// Offline characterization of the pinned legacy helpers, before refactoring the production implementation.
if (args.Length != 1) throw new ArgumentException("Provide the fixture output directory.");
Directory.CreateDirectory(args[0]);
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
var moneyCases = new List<object>();
foreach (var number in new decimal[] { 0, 1, 5, 10, 15, 21, 25, 101, 105, 1001, 1000001, 1234567890, -125, 12.5m, 12.6m })
{
    moneyCases.Add(new { amount = number, reader = "decimal", suffix = " đồng",
        expected = MoneyToText.DocTienBangChu(number) });
    moneyCases.Add(new { amount = number, reader = "sun", suffix = true,
        expected = NumberReader.NumberToText((double)number) });
}
moneyCases.Add(new { amount = 125m, reader = "decimal", suffix = " USD",
    expected = MoneyToText.DocTienBangChu(125m, " USD") });
moneyCases.Add(new { amount = 125m, reader = "sun", suffix = false,
    expected = NumberReader.NumberToText(125, false) });
var xmlCases = new List<object>();
foreach (var count in new[] { 0, 1, 2 })
{
    var invoice = new Invoice { Name = "Khách & <dịch vụ>", Note = null,
        Items = Enumerable.Range(1, count).Select(i => new Item { Amount = 12.5m * i }).ToArray() };
    xmlCases.Add(new { count, serializer = "typed", expected = XmlHelper.Serialize(invoice) });
    xmlCases.Add(new { count, serializer = "object", expected = XmlHelperObject.Serialize(invoice) });
    xmlCases.Add(new { count, serializer = "batch", expected = XmlHelperObjectMulti.Serialize(invoice) });
}
var options = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(Path.Combine(args[0], "money.json"), JsonSerializer.Serialize(moneyCases, options) + "\n");
File.WriteAllText(Path.Combine(args[0], "xml.json"), JsonSerializer.Serialize(xmlCases, options) + "\n");
Console.WriteLine($"Captured {moneyCases.Count} money and {xmlCases.Count} XML cases offline.");

[XmlRoot("Invoice")]
public class Invoice
{
    public string Name { get; set; }
    public string Note { get; set; }
    public Item[] Items { get; set; }
}
public class Item
{
    public decimal Amount { get; set; }
}
