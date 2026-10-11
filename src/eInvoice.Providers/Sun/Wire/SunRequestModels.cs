#nullable disable



namespace eInvoice.Providers.Sun.Wire
{
    public class SunCreateAndPublishInvoiceModel
    {
        public SunInvoiceModel Body { get; set; }
        public SunConnectionInfo Company { get; set; }
    }
	public class HealthCheckModel
	{
		public userInfo Body { get; set; }
		public SunConnectionInfo Company { get; set; }
	}

	public class DownloadPDFModel
	{
		public ParamDownloadPDF Body { get; set; }
		public SunConnectionInfo Company { get; set; }
	}

	public class VoidInvoiceModel
	{
		public ParamVoidInvoice Body { get; set; }
		public SunConnectionInfo Company { get; set; }
	}
	public class AdjustInvoiceModel
	{
		public SunAdjustInvoiceModel Body { get; set; }
		public SunConnectionInfo Company { get; set; }
	}

	public class ReplaceInvoiceModel
	{
		public SunReplaceInvoiceModel Body { get; set; }
		public SunConnectionInfo Company { get; set; }
	}

	public class ResponseConvertModel
    {
        public int Status { get; set; }
        public string Data { get; set; }
    }

    public class ParamDownloadPDF
    {
        // Mã tra cứu của hóa đơn
        public string sec { get; set; }
        // Bằng 1 thì lấy pdf hóa đơn nháp, khác 1 thì lấy pdf của hóa đơn thật
        public int invtmp { get; set; }
        // Mã số thuế của khách hàng
        public string taxc { get; set; }
    }

	public class ParamVoidInvoice
	{
		// sid của  hóa đơn gốc
		public string sid { get; set; }
		// Yêu cầu hủy hóa đơn = 3
		public int typ { get; set; }
		// Lý do hủy
		public string rea { get; set; }
		// Ngày sửa
		public string rdt { get; set; }
	}
}
