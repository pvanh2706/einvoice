#nullable disable
#pragma warning disable CS8981
// Wire names preserved from pinned baseline 66a9c74.

namespace eInvoice.Capture.Viettel.Wire
{
    public static class EndpointApi
    {
        public const string LOGIN = "auth/login";
        public const string CREATE = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createInvoice/";
        public const string CREATE_MULTI = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createBatchInvoice/";
        public const string DOWNLOAD_PDF = "services/einvoiceapplication/api/InvoiceAPI/InvoiceUtilsWS/getInvoiceRepresentationFile";
        public const string VOID = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/cancelTransactionInvoice";
        public const string DRAFT = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createOrUpdateInvoiceDraft/";
        public const string PREVIEW = "services/einvoiceapplication/api/InvoiceAPI/InvoiceUtilsWS/createInvoiceDraftPreview/";
        public const string SEARCH_BY_ID = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/searchInvoiceByTransactionUuid";
    }
}
