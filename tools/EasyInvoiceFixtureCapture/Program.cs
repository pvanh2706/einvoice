using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Capture.EasyInvoice.Mapping;
using eInvoice.Capture.EasyInvoice.Wire;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var output = Path.GetFullPath(args.Length == 0 ? "tests/eInvoice.Tests/Fixtures/EasyInvoice/Payloads" : args[0]);
Directory.CreateDirectory(output);
EasyInvoiceMappingContext Context(object data, InvoiceAction action = InvoiceAction.CreateAndPublish) => new() { Company = MisaTestData.Site.Site, Data = data, Action = action };
var edgeOnly = args.Contains("--edge-only", StringComparer.Ordinal);
void Save(string name, EasyInvoiceApiRequest payload) { if (!edgeOnly) File.WriteAllText(Path.Combine(output, name + ".json"), payload.bodyPost + "\n"); }
var input = MisaTestData.Invoice(); input.OriginalInvoice = new eInvoice.Core.Contracts.Invoice { InvoiceId = "OLD-001", MaCQT = "OLD-TAX" };
Save("publish", EasyInvoiceRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(input)));
Save("create", EasyInvoiceRequestMapper.ConvertCreateInvoiceRequest(Context(input)));
Save("draft", EasyInvoiceRequestMapper.ConvertCreateDraftInvoiceRequest(Context(input)));
Save("replace", EasyInvoiceRequestMapper.ConvertReplaceInvoiceRequest(Context(input)));
foreach (var pair in new[] { ("adjust-increase", InvoiceAction.AdjustIncrease), ("adjust-decrease", InvoiceAction.AdjustDecrease), ("adjust-information", InvoiceAction.AdjustInformation) })
    Save(pair.Item1, EasyInvoiceRequestMapper.ConvertAdjustInvoiceRequest(Context(input, pair.Item2)));
foreach (var pair in new[] { ("preview", 1), ("preview-replace", 3), ("preview-increase", 4), ("preview-decrease", 5), ("preview-information", 6) })
{
    input.Invoice!.PreviewType = pair.Item2;
    Save(pair.Item1, EasyInvoiceRequestMapper.ConvertPreviewInvoiceRequest(Context(input)));
}
Save("publish-existing", EasyInvoiceRequestMapper.ConvertPublishInvoiceRequest(Context(new VoidInvoiceRequest { InvoiceId = "INV-001", Serial = "C26TAA" })));
Save("void", EasyInvoiceRequestMapper.ConvertVoidInvoiceRequest(Context(new VoidInvoiceRequest { InvoiceId = "INV-001", Serial = "C26TAA" })));
Save("delete", EasyInvoiceRequestMapper.ConvertDeleteInvoiceRequest(Context(new DeleteInvoiceRequest { InvoiceId = "INV-001", Serial = "C26TAA" })));
Save("document", EasyInvoiceRequestMapper.ConvertGetPDFInvoiceRequest(Context(new DownloadPdfRequest { InvoiceId = "INV-001", Serial = "C26TAA" })));
void Edge(string name, EasyInvoiceApiRequest payload) => File.WriteAllText(Path.Combine(output, name + ".json"), payload.bodyPost + "\n");
input.Invoice!.ExchangeRate = 0;
Edge("create-zero-exchange", EasyInvoiceRequestMapper.ConvertCreateInvoiceRequest(Context(input)));
Edge("adjust-zero-exchange", EasyInvoiceRequestMapper.ConvertAdjustInvoiceRequest(Context(input, InvoiceAction.AdjustIncrease)));
Edge("replace-zero-exchange", EasyInvoiceRequestMapper.ConvertReplaceInvoiceRequest(Context(input)));
input.Invoice.ExchangeRate = 1.25m;
input.ExtraData = [new ExtraField { Code = "arrival", StringValue = "01/10/2026" }, new ExtraField { Code = "folio", StringValue = "SYNTHETIC & <folio>" }];
input.TaxSummaries!.AddRange(new[] { new TaxSummary { TaxRate = 0, BeforeTaxAmount = 100.55m, TaxAmount = 0 }, new TaxSummary { TaxRate = 5, BeforeTaxAmount = 200.25m, TaxAmount = 10.0125m }, new TaxSummary { TaxRate = 8, BeforeTaxAmount = 90, TaxAmount = 7.2m }, new TaxSummary { TaxRate = -1, BeforeTaxAmount = 80, TaxAmount = 0 } });
var context = new EasyInvoiceMappingContext { Company = MisaTestData.Site.Site, Data = input, ExtraData = [new ExtraFieldMapping { Code = "arrival", ThirdPartyCode = "arrivalDate" }, new ExtraFieldMapping { Code = "folio", ThirdPartyCode = "folioNo" }] };
Edge("service-extra-multivat", EasyInvoiceRequestMapper.ConvertCreateAndPublishInvoiceRequest(context));
File.WriteAllText(Path.Combine(output, "capture-context.json"), JsonConvert.SerializeObject(new { baseline = "66a9c742d0f6fb692d339076e6cd82780e3476be", culture = "Invariant", timezone = "UTC+7", note = "20 offline bodies from frozen baseline; four edge cases added without rewriting the original 16. No runtime provider assembly/HTTP/auth headers." }, Formatting.Indented) + "\n");
Console.WriteLine(edgeOnly ? "Added four edge fixtures; original 16 retained." : "Captured 20 offline EasyInvoice payloads.");
