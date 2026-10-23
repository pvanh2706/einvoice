namespace eInvoice.Providers.Minvoice.Wire;
public static class EndpointApi
{
    public const string LOGIN = "api/Account/Login";
    public const string FORM_SERIAL = "api/Invoice68/GetTypeInvoiceSeries";
    public const string CREATE = "api/InvoiceApi78/Save";
    public const string PUBLISH = "api/InvoiceApi78/SaveSign";
    public const string REPLACE = "api/InvoiceApi78/ThayThe";
    public const string ADJUST = "api/InvoiceApi78/DieuChinh";
    public const string VOID = "api/InvoiceApi78/HuyHoaDon";
    public const string SIGN = "api/InvoiceApi78/Sign";
    public const string DOCUMENT = "api/InvoiceApi78/PrintInvoice";
    public const string EMAIL = "api/Invoice68/SendEmailMulti";
    public const string INVOICES = "api/InvoiceApi78/GetInvoices";
    public const string TAX_CODE = "https://mst.minvoice.com.vn/api/System/SearchTaxCode";
}
