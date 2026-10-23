// Provider contract/mapping ported from pinned legacy 66a9c74. Wire names must remain unchanged.
// Nullable warnings are disabled only in this baseline boundary; callers validate canonical input.
#nullable enable annotations
#nullable disable warnings
namespace eInvoice.Providers.Misa.Wire
{
    public class EndpointApi
    {
        public const string LOGIN = "auth/token";
        public const string TICKET_FORM_SERIAL = "invoicepublishing/tickettemplates?invyear=";
        public const string INVOICE_FORM_SERIAL = "code/itg/InvoicePublishing/templates?invyear=";



        public const string INVOICE_TOKEN = "user/token";
        public const string CREATE_AND_PUBLISH_TICKET = "itg/invoicepublishing/publishhsm";
        public const string VOID_INVOICE = "itg/invoicepublished/cancel";
        public const string DOWNLOAD_PDF = "itg/invoicepublished/downloadinvoice?downloadDataType=pdf";

        public const string CREATE_AND_PUBLISH_RECEIPT = "invoice/publishinvoicesoftdream";
        public const string INVOICE_SERIAL = "invoice-pub-mngt/iptemplatepublish";
        public const string CANCEL_INVOICE = "invoice/deleted";
        public const string DOWNLOAD_INVOICE = "invoice/download?downloadDatatype=pdf";

        public const string CREATE_AND_PUBLISH_INVOICE_USB_TOKEN = "code/itg/invoicepublishing";
		public const string CREATE_INVOICE_USB_TOKEN = "code/itg/invoicepublishing/createinvoice";
		public const string CREATE_AND_PUBLISH_INVOICE_HSM = "code/itg/invoicepublishing/publishhsm";

		public const string CREATE_AND_PUBLISH_INVOICE_MTT = "code/itg/invoice-calculating/invoiceandpublish";
		public const string DOWNLOAD_PDF_MTT = "code/itg/invoice-calculating-published/download?downloadDataType=pdf";
		public const string DOWNLOAD_PDF_HSM = "code/itg/invoicepublished/downloadinvoice?downloadDataType";
		public const string VOID_INVOICE_MTT = "code/itg/invoicepublished/cancel";
		public const string SEND_INVOICE_MTT = "code/itg/invoice-calculating/tax";
		public const string GET_STATUS_INVOICE_BY_REFID_MTT = "code/itg/invoice-calculating-published/invoice-status/refid";
		public const string GET_STATUS_INVOICE_BY_REFID = "code/invoicepublished/invoice-status/refid";
		public const string GET_XML_INVOICE_HASH = "code/itg/invoice-calculating/xmlhash";
		public const string PREVIEW_INVOICE = "code/itg/invoicepublishing/invoicelinkview?type=1";
		public const string DRAFT = "api/v2/v3sainvoice/Code";

	}
}
