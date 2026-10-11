using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.EasyInvoice.Mapping;
using eInvoice.Providers.EasyInvoice.Wire;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.EasyInvoice;

public class EasyInvoiceMappingTests
{
    [Theory]
    [InlineData("publish")][InlineData("create")][InlineData("draft")][InlineData("replace")]
    [InlineData("adjust-increase")][InlineData("adjust-decrease")][InlineData("adjust-information")]
    [InlineData("preview")][InlineData("preview-replace")][InlineData("preview-increase")][InlineData("preview-decrease")][InlineData("preview-information")]
    [InlineData("publish-existing")][InlineData("void")][InlineData("delete")][InlineData("document")]
    [InlineData("create-zero-exchange")][InlineData("adjust-zero-exchange")][InlineData("replace-zero-exchange")][InlineData("service-extra-multivat")]
    public void Shared_runtime_mapper_matches_independent_frozen_baseline(string fixture)
    {
        var data = MisaTestData.Invoice(); data.OriginalInvoice = new eInvoice.Core.Contracts.Invoice { InvoiceId = "OLD-001", MaCQT = "OLD-TAX" };
        data.Invoice!.PreviewType = fixture switch { "preview" => 1, "preview-replace" => 3, "preview-increase" => 4, "preview-decrease" => 5, "preview-information" => 6, _ => null };
        if (fixture.Contains("zero-exchange", StringComparison.Ordinal)) data.Invoice.ExchangeRate = 0;
        var mappings = new List<ExtraFieldMapping>();
        if (fixture == "service-extra-multivat")
        {
            data.Invoice.ExchangeRate = 1.25m;
            data.ExtraData = [new ExtraField { Code = "arrival", StringValue = "01/10/2026" }, new ExtraField { Code = "folio", StringValue = "SYNTHETIC & <folio>" }];
            data.TaxSummaries!.AddRange(new[] { new TaxSummary { TaxRate = 0, BeforeTaxAmount = 100.55m, TaxAmount = 0 }, new TaxSummary { TaxRate = 5, BeforeTaxAmount = 200.25m, TaxAmount = 10.0125m }, new TaxSummary { TaxRate = 8, BeforeTaxAmount = 90, TaxAmount = 7.2m }, new TaxSummary { TaxRate = -1, BeforeTaxAmount = 80, TaxAmount = 0 } });
            mappings = [new ExtraFieldMapping { Code = "arrival", ThirdPartyCode = "arrivalDate" }, new ExtraFieldMapping { Code = "folio", ThirdPartyCode = "folioNo" }];
        }
        object request = fixture switch
        {
            "publish-existing" or "void" => new VoidInvoiceRequest { InvoiceId = "INV-001", Serial = "C26TAA" },
            "delete" => new DeleteInvoiceRequest { InvoiceId = "INV-001", Serial = "C26TAA" },
            "document" => new DownloadPdfRequest { InvoiceId = "INV-001", Serial = "C26TAA" }, _ => data,
        };
        var context = new EasyInvoiceMappingContext { Company = MisaTestData.Site.Site, Data = request, ExtraData = mappings,
            Action = fixture is "adjust-increase" or "adjust-zero-exchange" ? InvoiceAction.AdjustIncrease : fixture == "adjust-decrease" ? InvoiceAction.AdjustDecrease : InvoiceAction.AdjustInformation };
        var payload = ProviderMapping.Run(() => fixture switch
        {
            "create" or "create-zero-exchange" => EasyInvoiceRequestMapper.ConvertCreateInvoiceRequest(context),
            "draft" => EasyInvoiceRequestMapper.ConvertCreateDraftInvoiceRequest(context),
            "replace" or "replace-zero-exchange" => EasyInvoiceRequestMapper.ConvertReplaceInvoiceRequest(context),
            "adjust-increase" or "adjust-decrease" or "adjust-information" or "adjust-zero-exchange" => EasyInvoiceRequestMapper.ConvertAdjustInvoiceRequest(context),
            "publish-existing" => EasyInvoiceRequestMapper.ConvertPublishInvoiceRequest(context),
            "void" => EasyInvoiceRequestMapper.ConvertVoidInvoiceRequest(context),
            "delete" => EasyInvoiceRequestMapper.ConvertDeleteInvoiceRequest(context),
            "document" => EasyInvoiceRequestMapper.ConvertGetPDFInvoiceRequest(context),
            _ when fixture.StartsWith("preview", StringComparison.Ordinal) => EasyInvoiceRequestMapper.ConvertPreviewInvoiceRequest(context),
            _ => EasyInvoiceRequestMapper.ConvertCreateAndPublishInvoiceRequest(context),
        }, "EasyInvoice");
        var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "EasyInvoice", "Payloads", fixture + ".json")));
        Assert.True(JToken.DeepEquals(expected, JToken.Parse(payload.bodyPost)), payload.bodyPost);
    }
}
