#nullable disable
using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Vnpt.Baseline;
    public class PmsSearchInvoiceResponse
    {
        public string InvoiceId { get; set; }
        public string ProviderInvoiceNumber { get; set; }
        public string CodeOfTax { get; set; }
    }

    public class PmsInvoiceResponse
    {
        public string InvoiceId { get; set; }
        public string ProviderInvoiceNumber { get; set; }
        public string TransactionId { get; set; }
        public bool IsSuccess { get; set; } = false;
        public string SearchCode { get; set; }
        public string MaCQT { get; set; }
        public string InvoiceData { get; set; }
        public string Sun_sid { get; set; }
        public string MessageError { get; set; }
        public string ErrorCode { get; set; }
    }

    public class PmsDownloadPdfResponse
    {
        public string InvoicePDF { get; set; }
        public int Type { get; set; }// =1 Base64, 2: Link
    }
    public class MinvoiceGetTaxCodeInfoResponse
    {
        public string ten_cty { get; set; }
        public string dia_chi { get; set; }
        public string ma_so_thue { get; set; }
        public string masothue_id { get; set; }
        public string cqthuecap_tinh { get; set; }
        public string cqthue_ql { get; set; }
        public string nguoi_dai_dien { get; set; }
        public string ngay_thanh_lap { get; set; }
    }

    public class FormSerialResponse
    {
        public string InvoiceName { get; set; }
        public string FormNo { get; set; }
        public string Serial { get; set; }
        public int Quantity { get; set; }
        public int FromNo { get; set; }
        public int ToNo { get; set; }
        public int CurrentNo { get; set; }
        public int Status { get; set; } //1: sử dụng: 0: ngừng sử dụng
        public string StartDate { get; set; }
        public bool? IsMultipleTaxRates { get; set; }

	}

	public class PmsStatusInvoiceResponse
	{
		public string InvoiceId { get; set; }
		public int StatusInvoice { get; set; }
        public string ProviderInvoiceNumber { get; set; }
        public string SearchCode { get; set; }
        public string MaCQT { get; set; }
	}
	public class PMSHDon
	{
		public string KHMSHDon { get; set; }
		public string KHHDon { get; set; }
		public string SHDon { get; set; }
		public string MCCQThue { get; set; }
		public int? TThai { get; set; }
		public string MTLoi { get; set; }
		public string Fkey { get; set; }

	}

	public class PMSSearchHDonByFkey
	{
		public string KHMSHDon { get; set; }
		public string KHHDon { get; set; }
		public string SHDon { get; set; }
		public string MCCQThue { get; set; }
		public int? TThai { get; set; }
		public string MTLoi { get; set; }
		public string Fkey { get; set; }
		public string DLKy { get; set; }

	}
	public class ListInvByCusFkey
	{
		public string invNum { get; set; }
		public int? signStatus { get; set; }
		public string cusCode { get; set; }
		public string pattern { get; set; }
		public string serial { get; set; }

	}
