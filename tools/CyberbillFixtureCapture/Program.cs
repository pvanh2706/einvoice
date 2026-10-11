using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Capture.Cyberbill.Mapping;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var output = Path.GetFullPath(args.Length == 0 ? "tests/eInvoice.Tests/Fixtures/Cyberbill/Payloads" : args[0]);
Directory.CreateDirectory(output);
CyberbillMappingContext Context(object input, InvoiceAction action = InvoiceAction.Create) => new()
{ Company = MisaTestData.Site.Site, Data = input, Action = (int)action };
void Save(string name, object payload) => File.WriteAllText(Path.Combine(output, name + ".json"), JsonConvert.SerializeObject(payload, Formatting.Indented) + "\n");
var input = MisaTestData.Invoice(); input.Invoice!.PaymentMethod = "3"; input.Invoice.PaymentMethodCode = "3"; input.Invoice.InvoiceNo = 1;
input.OriginalInvoiceId = "OLD-001";
Save("create", CyberbillRequestMapper.ConvertCreateInvoiceDraft(Context(input)).Body);
Save("adjust", CyberbillRequestMapper.ConvertAdjustInvoiceRequest(Context(input, InvoiceAction.Adjust)).Body);
Save("replace", CyberbillRequestMapper.ConvertReplaceInvoiceRequest(Context(input, InvoiceAction.Replace)).Body);
var reference = new DownloadPdfRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 };
Save("sign", CyberbillRequestMapper.ConvertSignAndDownloadPDFCyberbill(Context(reference, InvoiceAction.Publish)).Body);
Save("document", CyberbillRequestMapper.ConvertSignAndDownloadPDFCyberbill(Context(reference, InvoiceAction.DownloadPdf)).Body);
Save("delete", CyberbillRequestMapper.ConvertDeleteInvoiceCyberbill(Context(reference, InvoiceAction.Delete)).Body);
Save("void", CyberbillRequestMapper.ConvertVoidInvoiceCyberbill(Context(new VoidInvoiceRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0, FormNo = "1", Serial = "C26TAA", InvoiceDate = "08/10/2026" }, InvoiceAction.Void)).Body);
File.WriteAllText(Path.Combine(output, "capture-context.json"), JsonConvert.SerializeObject(new
{
    baseline = "66a9c742d0f6fb692d339076e6cd82780e3476be", culture = "Invariant", timezone = "UTC+7",
    note = "Offline baseline mapping characterization only. Workflow/payment semantics decisions are separate; no runtime provider assembly.",
}, Formatting.Indented) + "\n");
Console.WriteLine("Captured 7 offline Cyberbill payload fixtures.");
