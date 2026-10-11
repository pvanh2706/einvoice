using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Capture.Viettel.Mapping;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var output = Path.GetFullPath(args.Length == 0 ? "tests/eInvoice.Tests/Fixtures/Viettel/Payloads" : args[0]);
Directory.CreateDirectory(output);
ViettelMappingContext Context(object input, InvoiceAction action) => new()
{
    Company = MisaTestData.Site.Site, Data = input, Action = (int)action,
    Now = new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.FromHours(7)),
};
void Save(string name, object body) => File.WriteAllText(Path.Combine(output, name + ".json"), JsonConvert.SerializeObject(body, Formatting.Indented) + "\n");
Save("publish", ViettelRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(MisaTestData.Invoice(), InvoiceAction.CreateAndPublish)).Body);
Save("publish-batch", ViettelRequestMapper.ConvertCreateAndPublishMultiInvoiceRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice() }, InvoiceAction.CreateAndPublishBatch)).Body);
Save("draft", ViettelRequestMapper.ConvertCreateDraftInvoiceRequest(Context(MisaTestData.Invoice(), InvoiceAction.Draft)).Body);
Save("preview", ViettelRequestMapper.ConvertPreviewInvoiceRequest(Context(MisaTestData.Invoice(), InvoiceAction.Preview)).Body);
foreach (var (name, action) in new[] { ("replace", InvoiceAction.Replace), ("adjust-increase", InvoiceAction.AdjustIncrease), ("adjust-decrease", InvoiceAction.AdjustDecrease), ("adjust-information", InvoiceAction.AdjustInformation) })
    Save(name, ViettelRequestMapper.ConvertAdjustReplaceRequest(Context(MisaTestData.Invoice(), action)).Body);
Save("document", ViettelRequestMapper.ConvertDownloadPdfRequest(Context(new DownloadPdfRequest { InvoiceId = "INV-001", InvoiceType = 0, ProviderInvoiceNumber = "000001", FormNo = "1" }, InvoiceAction.DownloadPdf)).Body);
Save("search", ViettelRequestMapper.ConvertSearchByIdRequest(Context(new SearchInvoiceRequest { InvoiceId = "INV-001" }, InvoiceAction.SearchById)).Body);
Save("void", ViettelRequestMapper.ConvertVoidInvoiceRequest(Context(new VoidInvoiceRequest { InvoiceType = 0, ProviderInvoiceNumber = "000001", FormNo = "1", InvoiceDate = "08/10/2026", AdditionalReferenceDate = "10/10/2026", Reason = "Synthetic reason" }, InvoiceAction.Void)).Body);
File.WriteAllText(Path.Combine(output, "capture-context.json"), JsonConvert.SerializeObject(new
{
    baseline = "66a9c742d0f6fb692d339076e6cd82780e3476be", culture = "Invariant", timezone = "UTC+7",
    note = "Offline synthetic characterization of pinned mapper. Explicit UTC+7 date and canonical DTO bridges; no runtime provider assembly.",
}, Formatting.Indented) + "\n");
Console.WriteLine("Captured 11 offline Viettel payload fixtures.");
