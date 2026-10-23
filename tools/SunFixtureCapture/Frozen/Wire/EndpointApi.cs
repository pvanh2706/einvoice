#nullable disable
namespace eInvoice.Capture.Sun.Wire
{
    public static class EndpointApi
    {
        public const string LOGIN = "api/signin";
        public const string CREATE_INVOICE_DRAFT = "api/invtmp";
        public const string CREATE_AND_PUBLISH_INVOICE = "api/savsigninv";
		public const string DOWNLOAD_PDF = "api/seek/searchpdf";
		public const string VOID = "api/cancelHD";
		public const string ADJUST = "api/adjniss";
		public const string REPLACE = "api/apiReplace";

		//public const string CREATE_MULTI = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createBatchInvoice/";
		//      public const string DOWNLOAD_PDF = "services/einvoiceapplication/api/InvoiceAPI/InvoiceUtilsWS/getInvoiceRepresentationFile";
		// public const string VOID = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/cancelTransactionInvoice";
        //      public const string DRAFT = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createOrUpdateInvoiceDraft/";
        //      public const string PREVIEW = "services/einvoiceapplication/api/InvoiceAPI/InvoiceUtilsWS/createInvoiceDraftPreview/";
        public const string SEARCH_BY_ID = "services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/searchInvoiceByTransactionUuid";
    }
}
