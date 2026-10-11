#nullable disable
#pragma warning disable CS0108 // Preserve inherited wire members from the pinned baseline.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Cyberbill.Wire
{
	public class CyberbillConnectionInfo
	{
		public string CompanyId { get; set; }
		public string PartnerUrl { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string TaxNumber { get; set; }
	}

	public class CreateInvoiceCyberbillModel
	{
		public string doanhnghiep_mst { get; set; }
		public string loaihoadon_ma { get; set; }
		public string mauso { get; set; }
		public string kyhieu { get; set; }
		public string sophieu { get; set; }
		public string ma_hoadon { get; set; }
		public string ngaylap { get; set; }
		public string dnmua_mst { get; set; }
		public string dnmua_ten { get; set; }
		public string dnmua_tennguoimua { get; set; }
		public string dnmua_diachi { get; set; }
		public string dnmua_sdt { get; set; }
		public string dnmua_email { get; set; }
		//Cột hình thức thanh toán :
		//+ 1 -  tiền mặt,
		//+ 2 - chuyển khoản,
		//+ 3 -  tiền mặt/chuyển khoản,
		//+ 4 - thẻ tín dụng,
		//+ 5 - đối trừ công nợ
		//- Đối với hóa đơn trên 20 triệu yêu cầu không được chọn hình thức thanh toán là tiền mặt
		public int thanhtoan_phuongthuc { get; set; }
		public string thanhtoan_phuongthuc_ten { get; set; }
		public string thanhtoan_taikhoan { get; set; }
		public string thanhtoan_nganhang { get; set; }
		public string tiente_ma { get; set; }
		public decimal? tygiangoaite { get; set; }
		public string thanhtoan_thoihan { get; set; }
		public decimal? tongtien_chietkhau { get; set; }
		public string ghichu { get; set; }
		public decimal tongtien_chuavat { get; set; }
		public decimal? tienthue { get; set; } // cần confin lại trong tài liệu do bị trùng với dòng trên
		public decimal tongtien_covat { get; set; }
		public string nguoilap { get; set; }
		public string dulieudacthu01 { get; set; }
		public string dulieudacthu02 { get; set; }
		public string dulieudacthu03 { get; set; }
		public string dulieudacthu04 { get; set; }
		public string dulieudacthu05 { get; set; }
		public List<InvoiceDetailCyberbillModel> dschitiet { get; set; }
		public List<InvoiceTaxCyberbillModel> dsthuesuat { get; set; }
		public string hoadon_goc { get; set; }
		public string hopdong_so { get; set; }
		public string hopdong_ngayky { get; set; }
		public string file_hopdong { get; set; }
	}
	public class InvoiceDetailCyberbillModel
	{
		public int? stt { get; set; }
		//Loại hàng hóa:
		//+ 0 : Hàng hóa
		//+ 1: Dịch vụ
		//+ 2: Ghi chú
		public int hanghoa_loai { get; set; }
		//Hàng hóa, dịch vụ khuyến mãi
		//+ 0: Không khuyến mãi
		//+ 1: Hàng khuyến mãi
		public int? khuyenmai { get; set; }
		public string ma { get; set; }
		public string ten { get; set; }
		public string donvitinh { get; set; }
		public decimal? soluong { get; set; }
		public decimal? dongia { get; set; }
		public decimal? phantram_chietkhau { get; set; }
		public decimal? tongtien_chietkhau { get; set; }
		public decimal? phikhac_tyle { get; set; }
		public decimal? phikhac_sotien { get; set; }
		public decimal tongtien_chuathue { get; set; }
		public string mathue { get; set; }
		public decimal tongtien_cothue { get; set; }
	}
	public class InvoiceTaxCyberbillModel
	{
		public string mathue { get; set; }
		public decimal? tongtien_chiuthue { get; set; }
		public decimal? tongtien_thue { get; set; }
	}

	public class ReplaceInvoiceCyberbillModel : CreateInvoiceCyberbillModel
	{
		public string hoadon_goc { get; set; }
		public string hopdong_so { get; set; }
		public string hopdong_ngayky { get; set; }
		public string file_hopdong { get; set; }
	}

	public class AdjustInvoiceCyberbillModel : ReplaceInvoiceCyberbillModel{}

	public class VoidInvoiceCyberbillModel
	{
		public string doanhnghiep_mst { get; set; }
		public int hoadon_loai { get; set; }
		public string loaihoadon_ma { get; set; }
		public string mauso { get; set; }
		public string kyhieu { get; set; }
		public string ma_hoadon { get; set; }
		public string hoadon_goc { get; set; }
		public string ngaylap { get; set; }
		public string hopdong_so { get; set; }
		public string hopdong_ngayky { get; set; }
		public string file_hopdong { get; set; }
		public string nguoilap { get; set; }
	}
	public class DeleteInvoiceCyberbillModel
	{
		public string doanhnghiep_mst { get; set; }
		public string magiaodich { get; set; }
	}

	public class SignAndDownloadPDFInvoiceCyberbillModel
	{
		public string doanhnghiep_mst { get; set; }
		public string magiaodich { get; set; }
		public string ma_hoadon { get; set; }
	}

}
