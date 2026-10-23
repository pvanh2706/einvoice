using System.Globalization;
using eInvoice.Core.Contracts;
using eInvoice.Capture.Minvoice.Mapping;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var additionalOnly = args.Contains("--additional-only", StringComparer.Ordinal);
var output = Path.GetFullPath(args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? "tests/eInvoice.Tests/Fixtures/Minvoice/Payloads");
Directory.CreateDirectory(output);
var mapper = new MinvoiceRequestMapper { Now = new DateTime(2026, 10, 11, 1, 0, 0) };
void Save(string name, object value) => File.WriteAllText(Path.Combine(output, name + ".json"), JsonConvert.SerializeObject(value, Formatting.Indented) + "\n");
JObject Input() { var invoice = MisaTestData.Invoice(id: "33333333-3333-3333-3333-333333333333"); invoice.Invoice!.SearchCode = "SEARCH-001"; invoice.OriginalInvoiceId = "44444444-4444-4444-4444-444444444444"; return JObject.FromObject(invoice); }
if (!additionalOnly)
{
foreach (var mode in new[] { "1", "2" })
{
    var request = Input(); request["editmode"] = mode;
    Save(mode == "1" ? "create" : "change", JToken.Parse(mapper.convertInvoiceFormat(request, MisaTestData.SiteId.ToString())));
}
var publish = Input(); publish["editmode"] = "1";
Save("publish", JToken.Parse(mapper.convertInvoiceFormat(publish, MisaTestData.SiteId.ToString())));
Save("replace", JToken.Parse(mapper.convertReplaceInv(Input())));
Save("adjust", JToken.Parse(mapper.convertAdjustInv(Input(), 0)));
var reference = JObject.FromObject(new DeleteInvoiceRequest { InvoiceId = "33333333-3333-3333-3333-333333333333", ProviderInvoiceNumber = "0000123", Serial = "C26TAA" });
Save("delete", JToken.Parse(mapper.convertDataDelete(reference)));
Save("void", JToken.Parse(mapper.convertCancelInv(reference)));
Save("sign", JToken.Parse(mapper.convertSignInv(reference)));
Save("invoices", JToken.Parse(mapper.convertGetInv(JObject.FromObject(new GetInvoicesRequest { FromDate = new DateTime(2026, 10, 1), ToDate = new DateTime(2026, 10, 11), Serial = "C26TAA" }))));
Save("capture-context", new { baseline = "66a9c742d0f6fb692d339076e6cd82780e3476be", culture = "Invariant", timezone = "UTC+7", now = "2026-10-11T01:00:00", note = "Offline frozen mapper; no runtime provider assembly or network. SearchCode explicit, financial formulas unchanged." });
}
foreach (var (name, site) in new[] { ("service-site-a", "36342869-8a14-4c31-b7be-7f46f5a92b13"), ("service-site-b", "96443794-55bb-4c12-881f-9e6cc1aa66ba") })
{
    var request = Input(); request["editmode"] = "1";
    request["Details"]![0]!["ServiceCharge"] = 17.25m; request["Details"]![0]!["ServiceRate"] = 7.5m;
    Save(name, JToken.Parse(mapper.convertInvoiceFormat(request, site)));
}
Console.WriteLine(additionalOnly ? "Captured two additional service-site fixtures; existing nine payloads untouched." : "Captured 11 independent Minvoice mapping payloads.");
