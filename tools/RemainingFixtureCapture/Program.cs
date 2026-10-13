using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Fast.Mapping;
using eInvoice.Providers.Vnpt.Baseline;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var directory = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? "tests/eInvoice.Tests/Fixtures/Remaining");
Directory.CreateDirectory(directory);
void Save(string name, object payload)
{
    var path = Path.Combine(directory, name + ".json");
    if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite an expected fixture: " + name);
    File.WriteAllText(path, JsonConvert.SerializeObject(payload, Formatting.Indented) + "\n");
}
SiteConfiguration Site(bool fast) => new()
{
    Id = MisaTestData.SiteId, Partner = fast ? 8 : 5, Name = "Synthetic hotel", TaxNumber = "TEST-TAX", Address = "Synthetic address",
    PartnerUrl = fast ? "https://fast.example.test/" : "https://vnpt.example.test/", PartnerUrl2 = fast ? "https://fast-soap.example.test/" : null,
    Username = fast ? "{\"clientC\":\"CLIENT\",\"proxyC\":\"PROXY\",\"user\":\"synthetic-user\",\"unitC\":\"UNIT\"}" : "synthetic-account",
    Username2 = fast ? "{\"clientC\":\"CLIENT2\",\"proxyC\":\"PROXY2\",\"user\":\"synthetic-user2\",\"unitC\":\"UNIT2\"}" : "synthetic-user",
    Password = "synthetic-secret", Password2 = "synthetic-secret2",
};
AdjustOrReplaceInvoiceRequest Input(bool mtt = false, int type = 0)
{
    var input = MisaTestData.Invoice(type, mtt ? 1 : 0); input.OriginalInvoiceId = "OLD-001";
    input.Invoice!.TotalAmountInWords = "Synthetic amount"; input.HotelExtra = new HotelInformation(); input.Invoice.HotelExtra = input.HotelExtra;
    return input;
}
if (args.Contains("--current-special-tax"))
{
    var currentSite = Site(false);
    using var currentScope = new VnptMappingScope(currentSite, false, TimeProvider.System);
    var current = Input(); current.Details![0].IsItemSpecialTax = true;
    Save("vnpt-current-special-tax", VnptConvertRequestCurrent.ConvertCreateAndPublishInvoiceRequest(new VnptMappingContext { Company = currentSite, Data = current }));
    var currentMtt = Input(true); currentMtt.Details![0].IsItemSpecialTax = true;
    Save("vnpt-current-mtt-special-tax", VnptMTTConvertRequestCurrent.ConvertCreateAndPublishInvoiceRequest(new VnptMappingContext { Company = currentSite, Data = currentMtt }));
    Console.WriteLine("Captured 2 current-legacy special-tax fixtures independently from the c09ad7d supplement.");
    return;
}
foreach (var mtt in new[] { true, false })
{
    var prefix = "fast-" + (mtt ? "mtt-" : "gtgt-");
    FastMappingContext Context(object data, InvoiceAction action) => new() { Company = Site(true), Data = data, IsSerialMTT = mtt, Action = (int)action };
    Save(prefix + "publish", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(Input(mtt), InvoiceAction.CreateAndPublish)) : FastRequestMapper.ConvertCreateAndPublishInvoiceBySoapRequest(Context(Input(mtt), InvoiceAction.CreateAndPublish), "SYNTHETIC-TOKEN"));
    Save(prefix + "batch", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishMultiInvoiceRequest(Context(new List<CreateInvoiceRequest> { Input(mtt) }, InvoiceAction.CreateAndPublishBatch)) : FastRequestMapper.ConvertCreateAndPublishMultiInvoiceSoapRequest(Context(new List<CreateInvoiceRequest> { Input(mtt) }, InvoiceAction.CreateAndPublishBatch), "SYNTHETIC-TOKEN"));
    Save(prefix + "replace", mtt ? (object)FastRequestMapper.ConvertReplaceInvoiceRequest(Context(Input(mtt), InvoiceAction.Replace)) : FastRequestMapper.ConvertReplaceInvoiceSoapRequest(Context(Input(mtt), InvoiceAction.Replace), "SYNTHETIC-TOKEN"));
    Save(prefix + "adjust", mtt ? (object)FastRequestMapper.ConvertAdjustInvoiceRequest(Context(Input(mtt), InvoiceAction.AdjustIncrease)) : FastRequestMapper.ConvertAdjustInvoiceSoapRequest(Context(Input(mtt), InvoiceAction.AdjustIncrease), "SYNTHETIC-TOKEN"));
    var eight = Input(mtt); eight.TaxSummaries![0].TaxRate = 8; eight.Invoice!.TaxRate = 8; eight.Details![0].TaxRate = 8;
    Save(prefix + "tax8", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(eight, InvoiceAction.CreateAndPublish)) : FastRequestMapper.ConvertCreateAndPublishInvoiceBySoapRequest(Context(eight, InvoiceAction.CreateAndPublish), "SYNTHETIC-TOKEN"));
}
var site = Site(false);
using var scope = new VnptMappingScope(site, false, TimeProvider.System);
VnptMappingContext Vnpt(object data, InvoiceAction action = InvoiceAction.CreateAndPublish) => new() { Company = site, Data = data, Action = (int)action };
Save("vnpt-v1-publish", VnptConvertRequest.ConvertCreateAndPublishInvoiceRequest(Vnpt(Input())));
Save("vnpt-v1-batch", VnptConvertRequest.ConvertCreateAndPublishMultiInvoiceRequest(Vnpt(new List<CreateInvoiceRequest> { Input() })));
Save("vnpt-v1-adjust", VnptConvertRequest.ConvertAdjustInvoiceRequest(Vnpt(Input()), (int)InvoiceAction.AdjustIncrease));
Save("vnpt-v1-replace", VnptConvertRequest.ConvertReplaceInvoiceRequest(Vnpt(Input())));
Save("vnpt-v5-publish", VnptConvertRequest.ConvertCreateAndPublishInvoiceRequestV5(Vnpt(Input())));
Save("vnpt-v5-batch", VnptConvertRequest.ConvertCreateAndPublishInvoiceMultiRequestV5(Vnpt(new List<CreateInvoiceRequest> { Input() })));
Save("vnpt-v5-adjust", VnptConvertRequest.ConvertAdjustInvoiceRequestV5(Vnpt(Input()), (int)InvoiceAction.AdjustIncrease));
Save("vnpt-v5-replace", VnptConvertRequest.ConvertReplaceInvoiceRequestV5(Vnpt(Input())));
Save("vnpt-mtt-publish", VnptMTTConvertRequest.ConvertCreateAndPublishInvoiceRequest(Vnpt(Input(true))));
Save("vnpt-mtt-adjust", VnptMTTConvertRequest.ConvertAdjustInvoiceRequest(Vnpt(Input(true)), (int)InvoiceAction.AdjustIncrease));
Save("vnpt-mtt-replace", VnptMTTConvertRequest.ConvertReplaceInvoiceRequest(Vnpt(Input(true))));
Save("vnpt-receipt-draft", VnptConvertRequest.ConvertCreateInvoiceReceiptRequest(Vnpt(Input(false, 2))));
Console.WriteLine("Captured 22 independent synthetic FAST/VNPT wire fixtures; no supplier calls.");
