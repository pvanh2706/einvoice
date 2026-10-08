using System.Text.Json;
using System.Text.Json.Serialization;
using eInvoice.Core;
using eInvoice.Core.Contracts;

namespace eInvoice.Tests.Contracts;

public class ContractTests
{
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
    };

    [Fact]
    public void New_contract_roundtrips_precise_money_and_names()
    {
        const string input = """
            {"invoice":{"invoiceId":"id-001","afterTaxAmount":123456789012345.1234567890123,"requestId":"request-001"},
             "taxSummaries":[{"taxRate":8,"taxAmount":0.08}],"electronicInvoiceType":4}
            """;
        var request = JsonSerializer.Deserialize<CreateInvoiceRequest>(input, ApiJson)!;
        Assert.Equal(123456789012345.1234567890123m, request.Invoice!.AfterTaxAmount);
        Assert.Equal("id-001", request.Invoice.InvoiceId);
        Assert.Equal(4, request.ElectronicInvoiceType);
        Assert.Equal(0.08m, Assert.Single(request.TaxSummaries!).TaxAmount);
        var json = JsonSerializer.Serialize(request, ApiJson);
        Assert.DoesNotContain("ezInvoice", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"invoiceId\":\"id-001\"", json);
    }

    [Theory]
    [InlineData("{\"ezInvoiceId\":\"old-key\"}")]
    [InlineData("{\"InvoiceId\":\"wrong-case\"}")]
    [InlineData("{\"afterTaxAmount\":\"12.50\"}")]
    public void Old_or_ambiguous_keys_and_string_money_are_rejected(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Invoice>(json, ApiJson));

    [Fact]
    public void Default_invoice_types_remain_distinct()
    {
        Assert.Equal(1, new Invoice().InvoiceType);
        Assert.Equal(1, new VoidInvoiceRequest().InvoiceType);
        Assert.Equal(1, new DownloadPdfRequest().InvoiceType);
        Assert.Equal(0, new GetFormSerialsRequest().InvoiceType);
        Assert.Equal("VP", new MinvoiceLoginRequest().UnitCode);
        Assert.Null(new CreateInvoiceRequest().TaxSummaries);
    }

    [Fact]
    public void Enum_values_keep_historical_gap_and_provider_numbers()
    {
        Assert.Equal(32, Enum.GetValues<InvoiceAction>().Length);
        Assert.False(Enum.IsDefined(typeof(InvoiceAction), 13));
        Assert.Equal(-1, (int)InvoiceAction.Delete);
        Assert.Equal(31, (int)InvoiceAction.EditDraft);
        Assert.Equal(6, (int)ProviderId.Misa);
        Assert.Equal(9, Enum.GetValues<ProviderId>().Length);
    }

    [Theory]
    [InlineData(InvoiceAction.Login)]
    [InlineData(InvoiceAction.HealthCheck)]
    [InlineData(InvoiceAction.AdjustIncrease)]
    [InlineData(InvoiceAction.AdjustDecrease)]
    [InlineData(InvoiceAction.AdjustInformation)]
    [InlineData(InvoiceAction.Adjust)]
    [InlineData(InvoiceAction.PublishSignedXml)]
    public void Misa_grouped_switch_arms_are_all_captured(InvoiceAction action) =>
        Assert.True(ProviderCapabilityCatalog.SupportsBaselineAction(ProviderId.Misa, action));

    [Theory]
    [InlineData(InvoiceAction.Create)]
    [InlineData(InvoiceAction.Draft)]
    [InlineData(InvoiceAction.DownloadDraftPdf)]
    [InlineData(InvoiceAction.SearchById)]
    public void Misa_preparation_or_commented_code_does_not_imply_support(InvoiceAction action) =>
        Assert.False(ProviderCapabilityCatalog.SupportsBaselineAction(ProviderId.Misa, action));

    [Fact]
    public void Vnpt_v5_edit_draft_is_supported_but_convert_for_store_is_not()
    {
        Assert.True(ProviderCapabilityCatalog.SupportsBaselineAction(ProviderId.VnptV5, InvoiceAction.EditDraft));
        Assert.All(Enum.GetValues<ProviderId>(), provider =>
            Assert.False(ProviderCapabilityCatalog.SupportsBaselineAction(provider, InvoiceAction.ConvertForStore)));
    }
}
