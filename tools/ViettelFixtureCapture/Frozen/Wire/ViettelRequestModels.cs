#nullable disable
#pragma warning disable CS8981
// Wire names preserved from pinned baseline 66a9c74.

namespace eInvoice.Capture.Viettel.Wire
{
    public class ViettelCreateAndPublishInvoiceModel
    {
        public CreateInvoiceViettelModel Body { get; set; }
        public ViettelConnectionInfo Company { get; set; }
    }
    public class ViettelVoidInvoiceRequest
    {
        public ViettelVoidInvoiceModel Body { get; set; }
        public ViettelConnectionInfo Company { get; set; }
    }
    public class ViettelSearchByIdRequest
    {
        public ViettelSearchByIdModel Body { get; set; }
        public ViettelConnectionInfo Company { get; set; }
    }
    public class ViettelDownloadPdfRequest
    {
        public ViettelDownloadPdfModel Body { get; set; }
        public ViettelConnectionInfo Company { get; set; }
    }

    public class ViettelCreateAndPublishMultiInvoiceModel
    {
        public CreateMultiInvoiceViettelModel Body { get; set; }
        public ViettelConnectionInfo Company { get; set; }
    }

    public class CreateMultiInvoiceViettelModel
    {
        public List<CreateInvoiceViettelModel> commonInvoiceInputs { get; set; }
    }
}
