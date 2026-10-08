using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Tests.Misa;

public static class MisaTestData
{
    public static readonly Guid SiteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static ResolvedSite Site => new(new SiteConfiguration
    {
        Id = SiteId, Partner = 6, PartnerUrl = "https://misa.example.test/api/v3/",
        PartnerUrl2 = "https://misa.example.test/receipt/", TaxNumber = "TEST-TAX",
        Username = "synthetic-v3-user", Password = "synthetic-v3-secret",
        Username2 = "synthetic-receipt-user", Password2 = "synthetic-receipt-secret",
        Name = "Synthetic hotel", Address = "Synthetic address",
    }, []);

    public static AdjustOrReplaceInvoiceRequest Invoice(int type = 0, int mode = 0, string id = "INV-001") => new()
    {
        ElectronicInvoiceType = mode,
        Invoice = new Invoice
        {
            InvoiceId = id, InvoiceType = type, FormNo = "1", Serial = type == 2 ? "R26TEST" : mode is 1 or 3 or 4 ? "C26MAA" : "C26TAA",
            InvoiceDate = "08/10/2026", CustomerName = "Khách thử nghiệm", CompanyName = "Synthetic customer",
            CustomerAddress = "Synthetic address", CurrencyCode = "VND", ExchangeRate = 1, PaymentMethod = "TM/CK",
            SubAmount = 251, BeforeTaxAmount = 245.975m, TaxAmount = 24.5975m, AfterTaxAmount = 270.5725m,
            ServiceRate = 5, ServiceCharge = 12.55m, TaxRate = 10, IsMultiVATRate = true,
            RefId = "CUSTOMER-001", PricePrecision = 2, QuantityPrecision = 2, AmountPrecision = 2,
        },
        Details = [new InvoiceDetail
        {
            ItemCode = "ROOM", ItemName = "Dịch vụ thử nghiệm", UnitName = "Lần", Quantity = 2.5m, Price = 100.4m,
            DiscountAmount = 17.575m, DiscountPercent = 7, SubAmount = 251, ServiceRate = 5, ServiceCharge = 12.55m,
            BeforeTaxAmount = 245.975m, TaxRate = 10, TaxAmount = 24.5975m, AfterTaxAmount = 270.5725m,
            TransactionDate = "08/10/2026", ItemType = "1", ExciseTaxCharge = 0, ExciseTaxRate = 0,
        }],
        ServiceSummaries = [new ServiceSummary { ServiceRate = 5, ServiceCharge = 12.55m, TaxRate = 10, TaxAmount = 1.255m, AfterTaxAmount = 13.805m }],
        TaxSummaries = [new TaxSummary { TaxRate = 10, BeforeTaxAmount = 245.975m, TaxAmount = 24.5975m }],
        ExtraData = [],
        OriginalInvoice = new Invoice { InvoiceId = "OLD-001", FormNo = "1", Serial = "1C25TAA", InvoiceDate = "01/09/2026" },
        OriginalProviderInvoiceNumber = "0000123",
    };
}
