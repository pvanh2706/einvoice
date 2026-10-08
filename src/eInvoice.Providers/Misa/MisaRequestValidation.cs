using System.Globalization;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

internal static class MisaRequestValidation
{
    internal static void Create(CreateInvoiceRequest request, bool reference = false)
    {
        if (request.Invoice is null) Fail("Invoice is required.");
        var invoice = request.Invoice!;
        Require(invoice.InvoiceId, "invoice.invoiceId");
        Require(invoice.FormNo, "invoice.formNo");
        Require(invoice.Serial, "invoice.serial");
        Date(invoice.InvoiceDate, "invoice.invoiceDate");
        if (invoice.InvoiceType is not (0 or 1 or 2)) Fail("invoice.invoiceType must be 0, 1 or 2.");
        if (request.ElectronicInvoiceType is < 0 or > 4 || (invoice.InvoiceType == 0 && request.ElectronicInvoiceType is null))
            Fail("electronicInvoiceType must specify a supported mode (0..4) for invoices.");
        Require(invoice.CurrencyCode, "invoice.currencyCode");
        if (invoice.ExchangeRate is null || invoice.AfterTaxAmount is null || invoice.BeforeTaxAmount is null || invoice.TaxAmount is null)
            Fail("Exchange rate and invoice monetary totals are required.");
        if (invoice.InvoiceType == 2 && invoice.TaxRate is null) Fail("Receipt tax rate is required.");
        Precision(invoice.PricePrecision);
        Precision(invoice.QuantityPrecision);
        Precision(invoice.AmountPrecision);
        if (request.Details is null || request.Details.Count == 0) Fail("At least one detail is required.");
        foreach (var detail in request.Details!)
        {
            if (detail is null || detail.Quantity is null || detail.Price is null) Fail("Every detail needs quantity and price.");
        }
        if (request.TaxSummaries?.Any(summary => summary is null || summary.TaxRate is null) == true)
            Fail("Every tax summary needs a tax rate.");
        if (request.ExtraData?.Any(field => field is null) == true || request.ServiceSummaries?.Any(summary => summary is null) == true)
            Fail("Request lists must not contain null items.");
        if (reference)
        {
            if (request is not AdjustOrReplaceInvoiceRequest { OriginalInvoice: not null } adjustment)
                Fail("Original invoice information is required for adjustment/replacement.");
            var original = ((AdjustOrReplaceInvoiceRequest)request).OriginalInvoice!;
            Require(original.FormNo, "originalInvoice.formNo");
            Require(original.Serial, "originalInvoice.serial");
            Date(original.InvoiceDate, "originalInvoice.invoiceDate");
            Require(((AdjustOrReplaceInvoiceRequest)request).OriginalProviderInvoiceNumber, "originalProviderInvoiceNumber");
        }
    }

    internal static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) Fail($"{field} is required.");
    }
    internal static void Date(string? value, string field)
    {
        if (!DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            Fail($"{field} must be a valid date in dd/MM/yyyy format.");
    }
    internal static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
    private static void Precision(int? value)
    {
        if (value is < 0 or > 28) Fail("Decimal precision must be between 0 and 28.");
    }
}
