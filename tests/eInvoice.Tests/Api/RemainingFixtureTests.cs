using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Fast.Mapping;
using eInvoice.Providers.Common;
using eInvoice.Providers.Vnpt.Baseline;
using Newtonsoft.Json.Linq;

namespace eInvoice.Tests.Api;

public class RemainingFixtureTests
{
    private static void Fixture(string name, object value)
    {
        var expected = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Remaining", name + ".json")));
        Assert.True(JToken.DeepEquals(expected, JObject.FromObject(value)), name + " differs from pinned independent fixture.");
    }
    [Theory]
    [InlineData(true,"vi-VN")] [InlineData(false,"en-US")] [InlineData(true,"en-US")] [InlineData(false,"vi-VN")]
    public void Fast_mtt_and_gtgt_payloads_preserve_column_order_tax8_and_soap_hash(bool mtt, string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            ProviderMapping.Run(() =>
            {
            var prefix = "fast-" + (mtt ? "mtt-" : "gtgt-");
            FastMappingContext Context(object data, InvoiceAction action) => new() { Company = RemainingProviderTests.Site(8).Site, Data = data, IsSerialMTT = mtt, Action = (int)action };
            var input = RemainingProviderTests.Input(mtt);
            Fixture(prefix + "publish", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(input, InvoiceAction.CreateAndPublish)) : FastRequestMapper.ConvertCreateAndPublishInvoiceBySoapRequest(Context(input, InvoiceAction.CreateAndPublish), "SYNTHETIC-TOKEN"));
            Fixture(prefix + "batch", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishMultiInvoiceRequest(Context(new List<CreateInvoiceRequest> { RemainingProviderTests.Input(mtt) }, InvoiceAction.CreateAndPublishBatch)) : FastRequestMapper.ConvertCreateAndPublishMultiInvoiceSoapRequest(Context(new List<CreateInvoiceRequest> { RemainingProviderTests.Input(mtt) }, InvoiceAction.CreateAndPublishBatch), "SYNTHETIC-TOKEN"));
            Fixture(prefix + "replace", mtt ? (object)FastRequestMapper.ConvertReplaceInvoiceRequest(Context(RemainingProviderTests.Input(mtt), InvoiceAction.Replace)) : FastRequestMapper.ConvertReplaceInvoiceSoapRequest(Context(RemainingProviderTests.Input(mtt), InvoiceAction.Replace), "SYNTHETIC-TOKEN"));
            Fixture(prefix + "adjust", mtt ? (object)FastRequestMapper.ConvertAdjustInvoiceRequest(Context(RemainingProviderTests.Input(mtt), InvoiceAction.AdjustIncrease)) : FastRequestMapper.ConvertAdjustInvoiceSoapRequest(Context(RemainingProviderTests.Input(mtt), InvoiceAction.AdjustIncrease), "SYNTHETIC-TOKEN"));
            var eight = RemainingProviderTests.Input(mtt); eight.TaxSummaries![0].TaxRate = 8; eight.Invoice!.TaxRate = 8; eight.Details![0].TaxRate = 8;
            Fixture(prefix + "tax8", mtt ? (object)FastRequestMapper.ConvertCreateAndPublishInvoiceRequest(Context(eight, InvoiceAction.CreateAndPublish)) : FastRequestMapper.ConvertCreateAndPublishInvoiceBySoapRequest(Context(eight, InvoiceAction.CreateAndPublish), "SYNTHETIC-TOKEN"));
            return true;
            }, "FAST");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Fact]
    public void Vnpt_xml_credentials_and_null_elements_match_frozen_v1_v5_mtt_and_receipt_mapping()
    {
        var site = RemainingProviderTests.Site(5).Site;
        using var scope = new VnptMappingScope(site, false, TimeProvider.System);
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            VnptMappingContext Context(object data) => new() { Company = site, Data = data };
            AdjustOrReplaceInvoiceRequest Input(bool mtt = false) => RemainingProviderTests.Input(mtt);
            Fixture("vnpt-v1-publish", VnptConvertRequest.ConvertCreateAndPublishInvoiceRequest(Context(Input())));
            Fixture("vnpt-v1-batch", VnptConvertRequest.ConvertCreateAndPublishMultiInvoiceRequest(Context(new List<CreateInvoiceRequest> { Input() })));
            Fixture("vnpt-v1-adjust", VnptConvertRequest.ConvertAdjustInvoiceRequest(Context(Input()), (int)InvoiceAction.AdjustIncrease));
            Fixture("vnpt-v1-replace", VnptConvertRequest.ConvertReplaceInvoiceRequest(Context(Input())));
            Fixture("vnpt-v5-publish", VnptConvertRequest.ConvertCreateAndPublishInvoiceRequestV5(Context(Input())));
            Fixture("vnpt-v5-batch", VnptConvertRequest.ConvertCreateAndPublishInvoiceMultiRequestV5(Context(new List<CreateInvoiceRequest> { Input() })));
            Fixture("vnpt-v5-adjust", VnptConvertRequest.ConvertAdjustInvoiceRequestV5(Context(Input()), (int)InvoiceAction.AdjustIncrease));
            Fixture("vnpt-v5-replace", VnptConvertRequest.ConvertReplaceInvoiceRequestV5(Context(Input())));
            Fixture("vnpt-mtt-publish", VnptMTTConvertRequest.ConvertCreateAndPublishInvoiceRequest(Context(Input(true))));
            Fixture("vnpt-mtt-adjust", VnptMTTConvertRequest.ConvertAdjustInvoiceRequest(Context(Input(true)), (int)InvoiceAction.AdjustIncrease));
            Fixture("vnpt-mtt-replace", VnptMTTConvertRequest.ConvertReplaceInvoiceRequest(Context(Input(true))));
            Fixture("vnpt-receipt-draft", VnptConvertRequest.ConvertCreateInvoiceReceiptRequest(Context(RemainingProviderTests.Input(false,2))));
            var special = Input(); special.Details![0].IsItemSpecialTax = true;
            var specialRequest = VnptConvertRequest.ConvertCreateAndPublishInvoiceRequest(Context(special));
            Fixture("vnpt-current-special-tax", specialRequest);
            Assert.Contains("<IsSum>4</IsSum>", specialRequest.Body.xmlInvData);
            var specialMtt = Input(true); specialMtt.Details![0].IsItemSpecialTax = true;
            Fixture("vnpt-current-mtt-special-tax", VnptMTTConvertRequest.ConvertCreateAndPublishInvoiceRequest(Context(specialMtt)));
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }
}
