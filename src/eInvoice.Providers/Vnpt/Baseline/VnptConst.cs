namespace eInvoice.Providers.Vnpt.Baseline
{
    public static class VnptConst
    {
        public const string RESPONSE_SUCCESS = "OK";
        public const string RESPONSE_ERROR = "ERR:";
        public const int EXCEPTION_ERROR_CODE = 322;
        public const string FORM_SERIAL = "FormSerial";
        public const string DRAFT_INVOICE = "CreateInvoice";
        public const string SIGN_INVOICE = "SignInvoice";
        public const string PUBLISH_INVOICE = "Publish";
        public const string CREATE_AND_SIGN_INVOICE = "CreateAndSignInvoice";
        public const string ADJUST_INVOICE = "AdjustInvoice";
        public const string REPLACE_INVOICE = "ReplaceInvoice";
        public const string DOWNLOAD_PDF = "DownloadPDF";
        public const string CONVERT_FOR_STORE = "ConvertForStore";
        public const string VOID = "CancelInvoice";

        public const string DELETE = "DeleteInvoice";
        public const string GET_DATA_INVOICE_BY_FKEY = "GetDataInvoiceByFkey";
        public const string DOWNLOAD_PDF_INV_DRAFT = "DownloadPDFInvoiceDraft";
        public const string GET_SERIAL_CERT_INFO = "GetSerialCertInfo";
        public const string SEND_INVOICE_CQT = "SendIncoieCQT";
        public const string GET_STATUS_INVOICE = "GetStatusInvoice";

        public const string TYPE_ADJUST_INCREASE = "2";
        public const string TYPE_ADJUST_DECREASE = "3";
        public const string TYPE_ADJUST_INFOR = "4";
    }
}
