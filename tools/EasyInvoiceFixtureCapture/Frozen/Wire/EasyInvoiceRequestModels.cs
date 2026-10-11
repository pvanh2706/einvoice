#nullable disable
using System.Runtime.InteropServices;

namespace eInvoice.Capture.EasyInvoice.Wire
{
	/// <summary>
	/// Body gửi dữ liệu sang nhà cung cấp Easy Invoice
	/// {“XmlData”: “Chuỗi XML(bắt buộc)”, “Pattern”: “Ký hiệu mẫu số hóa đơn(bắt buộc)”}
	/// </summary>
	public class EasyInvoiceCreateAndPublishRequestModels
	{
		public string XmlData { get; set; } // Chuỗi XML(bắt buộc
		public string Pattern { get; set; } // Ký hiệu mẫu số hóa đơn(bắt buộc)
		public string Option { get; set; } // Ký hiệu mẫu số hóa đơn(bắt buộc)
	}

	public class EasyInvoicePreviewRequestModels : EasyInvoiceCreateAndPublishRequestModels
	{
		public string InvoiceType { get; set; } // Loại xem trước
		public string Ikey { get; set; } // Khóa xác định hóa đơn gốc
	}


	public class EasyInvoiceViewRequestModels
	{
		public string Ikey { get; set; } // Khóa xác định hóa đơn
		public string Pattern { get; set; } // Ký hiệu mẫu số hóa đơn(bắt buộc)
		public string Option { get; set; } // Giá trị tuỳ chọn
		//o	-1: Tệp xml
		//o	 0: Tệp pdf thông thường
		//o	 1: Tệp pdf chuyển đổi chứng minh nguồn gốc
		//o	 2: Tệp pdf chuyển đổi lưu trữ

	}
	public class EasyInvoicePublishRequestModels
	{
		public List<string> Ikeys { get; set; } // Khóa xác định hóa đơn
		public string Pattern { get; set; } // Ký hiệu mẫu số hóa đơn(bắt buộc)

	}


	public class EasyInvoiceAdjustOrReplaceRequestModels
	{
		public string XmlData { get; set; } // Chuỗi XML(bắt buộc
		public string Ikey { get; set; } // Chuỗi XML(bắt buộc
		public string Pattern { get; set; } // Ký hiệu mẫu số hóa đơn(bắt buộc)
	}


	public class EasyInvoiceApiRequest
	{
		public string urlAPI { get; set; }
		public Dictionary<string, string> headersAPI { get; set; }
		public string bodyPost { get; set; }
	}
}
