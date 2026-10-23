using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Capture.Misa.Mapping;
using eInvoice.Capture.Misa.Wire;
using eInvoice.Tests.Misa;
using Newtonsoft.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
var output = Path.GetFullPath(args.Length == 0 ? "tests/eInvoice.Tests/Fixtures/Misa/Payloads" : args[0]);
Directory.CreateDirectory(output);
MisaMappingContext Context(object request, InvoiceAction action = InvoiceAction.CreateAndPublish) => new()
{
    Company = MisaTestData.Site.Site, ExtraData = [], Data = request, Action = (int)action,
};
void Save(string name, object payload) => File.WriteAllText(Path.Combine(output, name + ".json"),
    JsonConvert.SerializeObject(payload, Formatting.Indented) + "\n");
Save("hsm-single", MisaRequestMapper.ConvertCreateAndPublishInvoiceHSMRequest(Context(MisaTestData.Invoice())));
Save("hsm-batch", MisaRequestMapper.ConvertCreateAndPublishInvoiceHSMMultiRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice() })));
Save("mtt-single", MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTRequest(Context(MisaTestData.Invoice(mode: 1))));
Save("mtt-batch", MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTMultiRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice(mode: 1) })));
Save("mtt-hsm-single", MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTHSMRequest(Context(MisaTestData.Invoice(mode: 4))));
Save("mtt-hsm-batch", MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTHSMMultiRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice(mode: 4) })));
Save("ticket", MisaRequestMapper.ConvertCreateAndPublishTicketRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice(type: 1) })));
Save("receipt", MisaRequestMapper.ConvertCreateAndPublishReceiptRequest(Context(new List<CreateInvoiceRequest> { MisaTestData.Invoice(type: 2) }),
    MisaRequestMapper.ConvertConnectionRequest(Context(MisaTestData.Invoice())),
    new InvFromSerialResponseModel { TemplateCode = "R26TEST", InvoiceSeries = "C26RAA", InvoiceType = "2" }));
Save("preview", MisaRequestMapper.ConvertPreviewInvoiceRequest(Context(MisaTestData.Invoice())));
var preview = MisaTestData.Invoice();
preview.Invoice!.PreviewType = 3;
Save("preview-replace", MisaRequestMapper.ConvertPreviewInvoiceRequest(Context(preview)));
Save("adjust-hsm", MisaRequestMapper.ConvertAdjustAndReplaceHSMRequest(Context(MisaTestData.Invoice(), InvoiceAction.Adjust)));
Save("replace-hsm", MisaRequestMapper.ConvertAdjustAndReplaceHSMRequest(Context(MisaTestData.Invoice(), InvoiceAction.Replace)));
Save("adjust-mtt", MisaRequestMapper.ConvertAdjustAndReplaceMTTRequest(Context(MisaTestData.Invoice(mode: 1), InvoiceAction.Adjust)));
Save("adjust-usb", MisaRequestMapper.ConvertAdjustAndReplaceUsbTokenRequest(Context(MisaTestData.Invoice(mode: 2), InvoiceAction.Adjust)));
Save("xml-create", MisaRequestMapper.ConvertGetXMLInvoiceRequest(Context(MisaTestData.Invoice(mode: 2))));
Save("xml-publish", MisaRequestMapper.ConvertCreateAndPublishInvoiceXMLRequest(Context(new PublishSignedXmlRequest
{
    InvoiceId = "INV-001", SearchCode = "transaction-001", InvoiceData = "<synthetic-signed-invoice/>",
})));
File.WriteAllText(Path.Combine(output, "capture-context.json"), JsonConvert.SerializeObject(new
{
    baseline = "66a9c742d0f6fb692d339076e6cd82780e3476be", culture = CultureInfo.CurrentCulture.Name,
    timezone = TimeZoneInfo.Local.Id, offsetAtInvoiceDate = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 8)).ToString(),
    note = "Offline synthetic characterization. Frozen legacy mapper with canonical DTO type/property bridges; no runtime provider reference.",
}, Formatting.Indented) + "\n");
Console.WriteLine("Captured 16 offline MISA payload fixtures. No network or live configuration was used.");
