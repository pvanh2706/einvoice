using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Cyberbill.Mapping;
using eInvoice.Tests.Misa;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Cyberbill;

public class CyberbillMappingTests
{
    [Theory]
    [InlineData("create")]
    [InlineData("replace")]
    [InlineData("adjust")]
    [InlineData("sign")]
    [InlineData("document")]
    [InlineData("delete")]
    [InlineData("void")]
    public void Mapper_preserves_pinned_wire_fixtures_under_comma_decimal_culture(string operation)
    {
        var input = MisaTestData.Invoice(); input.Invoice!.PaymentMethod = "3"; input.Invoice.PaymentMethodCode = "3";
        input.Invoice.InvoiceNo = 1; input.OriginalInvoiceId = "OLD-001";
        var reference = new DownloadPdfRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0 };
        var data = operation is "sign" or "document" or "delete" ? (object)reference
            : operation == "void" ? new VoidInvoiceRequest { InvoiceId = "INV-001", ProviderInvoiceCode = "TX-001", InvoiceType = 0, FormNo = "1", Serial = "C26TAA", InvoiceDate = "08/10/2026" } : input;
        var context = new CyberbillMappingContext { Company = MisaTestData.Site.Site, Data = data, Action = (int)InvoiceAction.Create };
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var payload = ProviderMapping.Run<object>(() => operation switch
            {
                "create" => CyberbillRequestMapper.ConvertCreateInvoiceDraft(context).Body,
                "replace" => CyberbillRequestMapper.ConvertReplaceInvoiceRequest(context).Body,
                "adjust" => CyberbillRequestMapper.ConvertAdjustInvoiceRequest(context).Body,
                "sign" or "document" => CyberbillRequestMapper.ConvertSignAndDownloadPDFCyberbill(context).Body,
                "delete" => CyberbillRequestMapper.ConvertDeleteInvoiceCyberbill(context).Body,
                "void" => CyberbillRequestMapper.ConvertVoidInvoiceCyberbill(context).Body,
                _ => throw new InvalidOperationException(),
            }, "Cyberbill");
            Assert.Equal("fr-FR", CultureInfo.CurrentCulture.Name);
            var expected = JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Cyberbill", "Payloads", operation + ".json")));
            Assert.True(JToken.DeepEquals(expected, JToken.FromObject(payload)), JToken.FromObject(payload).ToString());
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }
}
