#nullable disable
// Pinned 66a9c74 mapper; dead VAT helper omitted. Date/number formatting stabilized per D021.
using eInvoice.Core.Contracts;
using eInvoice.Capture.Cyberbill.Wire;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace eInvoice.Capture.Cyberbill.Mapping
{
	internal static class CyberbillRequestMapper
	{
		public static CyberbillConnectionInfo LoginRequest(CyberbillMappingContext model)
		{
			return CompanyInfo(model);
		}
		public static CyberbillCreateAndPublishInvoiceModel ConvertCreateInvoiceDraft(CyberbillMappingContext model)
		{
			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			CreateInvoiceCyberbillModel invoice = ConvertInvoiceCyber(Convert.ToInt32(model.Action), invoiceRequest, null, model.ExtraData, companyInfo);
			CyberbillCreateAndPublishInvoiceModel result = new CyberbillCreateAndPublishInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}

		public static CyberbillCreateAndPublishInvoiceModel ConvertCreateAndPublishInvoiceRequest(CyberbillMappingContext model)
		{
			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			CreateInvoiceCyberbillModel invoice = ConvertInvoiceCyber(Convert.ToInt32(model.Action), invoiceRequest, null, model.ExtraData, companyInfo);
			CyberbillCreateAndPublishInvoiceModel result = new CyberbillCreateAndPublishInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}
		public static CyberbillReplaceInvoiceModel ConvertReplaceInvoiceRequest(CyberbillMappingContext model)
		{
			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			CreateInvoiceCyberbillModel invoice = ConvertInvoiceCyber(Convert.ToInt32(model.Action), invoiceRequest, model.Data.OriginalInvoiceId, model.ExtraData, companyInfo);

			CyberbillReplaceInvoiceModel result = new CyberbillReplaceInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}
		public static CyberbillAdjustInvoiceModel ConvertAdjustInvoiceRequest(CyberbillMappingContext model)
		{
			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			CreateInvoiceCyberbillModel invoice = ConvertInvoiceCyber(Convert.ToInt32(model.Action), invoiceRequest, model.Data.OriginalInvoiceId, model.ExtraData, companyInfo);

			CyberbillAdjustInvoiceModel result = new CyberbillAdjustInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}
		public static CyberbillSignAndDownloadPDFInvoiceModel ConvertSignAndDownloadPDFCyberbill(CyberbillMappingContext model)
		{
			dynamic invoiceRequest = model.Data;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			SignAndDownloadPDFInvoiceCyberbillModel invoice = ConvertSignAndDownloadPDFCyberbill(invoiceRequest, companyInfo);

			CyberbillSignAndDownloadPDFInvoiceModel result = new CyberbillSignAndDownloadPDFInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}

		public static CyberbillDeleteInvoiceModel ConvertDeleteInvoiceCyberbill(CyberbillMappingContext model)
		{
			dynamic invoiceRequest = model.Data;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			DeleteInvoiceCyberbillModel invoice = ConvertDeleteInvoiceCyberbill(invoiceRequest, companyInfo);

			CyberbillDeleteInvoiceModel result = new CyberbillDeleteInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}
		public static CyberbillVoidInvoiceModel ConvertVoidInvoiceCyberbill(CyberbillMappingContext model)
		{
			dynamic invoiceRequest = model.Data;
			CyberbillConnectionInfo companyInfo = CompanyInfo(model);
			VoidInvoiceCyberbillModel invoice = ConvertVoidInvoiceCyberbill(invoiceRequest, companyInfo);

			CyberbillVoidInvoiceModel result = new CyberbillVoidInvoiceModel
			{
				Body = invoice,
				Company = companyInfo
			};
			return result;
		}

		/// <summary>
		/// Tạo body chung cho các chức năng Tạo hóa đơn, Điều chỉnh hóa dơn, Thay thế hóa đơn
		/// </summary>
		/// <param name="Action"></param>
		/// <param name="invoiceRequest"></param>
		/// <param name="oldThirdPartyInvoiceNumber"></param>
		/// <param name="extraDataMap"></param>
		/// <param name="connectionInfo"></param>
		/// <returns></returns>
		private static CreateInvoiceCyberbillModel ConvertInvoiceCyber(int Action, CreateInvoiceRequest invoiceRequest, string oldThirdPartyInvoiceNumber, List<ExtraFieldMapping> extraDataMap, CyberbillConnectionInfo connectionInfo)
		{
			Invoice pmsInvoice = invoiceRequest.Invoice;
			pmsInvoice.InvoiceId = pmsInvoice.InvoiceId ?? Guid.NewGuid().ToString();
			CreateInvoiceCyberbillModel result = new CreateInvoiceCyberbillModel();

			result.doanhnghiep_mst = connectionInfo.TaxNumber;
			result.loaihoadon_ma = "1"; // Mặc định ezCloud Fix là 1
			result.mauso = pmsInvoice.FormNo;
			result.kyhieu = pmsInvoice.Serial;
			result.sophieu = pmsInvoice.InvoiceNo.ToString(); // Cần kiểm tra lại
			result.ma_hoadon = pmsInvoice.InvoiceId.ToString();
			result.ngaylap = ConvertDateYYYYMMDD(pmsInvoice.InvoiceDate);

			result.dnmua_mst = pmsInvoice.CustomerTax;
			result.dnmua_ten = pmsInvoice.CompanyName;
			result.dnmua_tennguoimua = pmsInvoice.CustomerName;
			result.dnmua_diachi = pmsInvoice.CustomerAddress;
			result.dnmua_sdt = pmsInvoice.CustomerPhone;
			result.dnmua_email = pmsInvoice.CustomerEmail;

			result.thanhtoan_phuongthuc = Int32.Parse(pmsInvoice.PaymentMethod);
			result.thanhtoan_phuongthuc_ten = pmsInvoice.PaymentMethod;
			result.thanhtoan_taikhoan = pmsInvoice.BankAccount;
			result.thanhtoan_nganhang = pmsInvoice.BankName;

			result.tiente_ma = pmsInvoice.CurrencyCode;
			result.tygiangoaite = pmsInvoice.ExchangeRate;
			result.thanhtoan_thoihan = ""; // Đang fix
			result.tongtien_chietkhau = 0; // Đang fix
			result.ghichu = pmsInvoice.Notice;
			result.tongtien_chuavat = pmsInvoice.BeforeTaxAmount ?? 0;
			result.tienthue = pmsInvoice.TaxAmount ?? 0;
			result.tongtien_covat = pmsInvoice.AfterTaxAmount ?? 0;
			result.nguoilap = "";
			result.dulieudacthu01 = "";
			result.dulieudacthu02 = "";
			result.dulieudacthu03 = "";
			result.dulieudacthu04 = "";
			result.dulieudacthu05 = "";
			// Dùng cho Điều chỉnh, Thay thế hóa đơn
			result.hoadon_goc = oldThirdPartyInvoiceNumber;
			result.hopdong_so = "";
			result.hopdong_ngayky = "";
			result.file_hopdong = "";

			result.dschitiet = invoiceRequest.Details.Select((item, index) => new InvoiceDetailCyberbillModel
			{
				stt = index + 1,
				hanghoa_loai = 1, // Đang fix
				khuyenmai = 0, // Đang fix
				ma = "", // Đang fix
				ten = item.ItemName,
				donvitinh = item.UnitName,
				soluong = item.Quantity,
				dongia = item.Price,
				phantram_chietkhau = item.DiscountPercent,
				tongtien_chietkhau = item.DiscountAmount,
				phikhac_tyle = 0, // Đang fix
				phikhac_sotien = 0, // Đang fix
				tongtien_chuathue = item.SubAmount ?? 0,
				mathue = item.TaxRate.ToString(),
				tongtien_cothue = item.AfterTaxAmount ?? 0,
			}).ToList();

			if (invoiceRequest.TaxSummaries != null && invoiceRequest.TaxSummaries.Any())
			{
				result.dsthuesuat = invoiceRequest.TaxSummaries.Select(item => new InvoiceTaxCyberbillModel
				{
					mathue = item.TaxRate.ToString(),
					tongtien_thue = item.TaxAmount,
					tongtien_chiuthue = item.BeforeTaxAmount,
				}).ToList();
			}
			return result;
		}
		/// <summary>
		/// Tạo body cho chức năng Ký hóa đơn, Xem PDF
		/// </summary>
		/// <param name="invoiceRequest"></param>
		/// <param name="connectionInfo"></param>
		/// <returns></returns>
		private static SignAndDownloadPDFInvoiceCyberbillModel ConvertSignAndDownloadPDFCyberbill (dynamic invoiceRequest, CyberbillConnectionInfo connectionInfo)
		{
			SignAndDownloadPDFInvoiceCyberbillModel result = new SignAndDownloadPDFInvoiceCyberbillModel();
			result.doanhnghiep_mst = connectionInfo.TaxNumber;
			result.magiaodich = invoiceRequest.ProviderInvoiceCode;
			result.ma_hoadon = invoiceRequest.InvoiceId.ToString();

			return result;
		}
		/// <summary>
		/// Tạo body cho chức năng Xóa hóa đơn đã ký
		/// </summary>
		/// <param name="invoiceRequest"></param>
		/// <param name="connectionInfo"></param>
		/// <returns></returns>
		private static VoidInvoiceCyberbillModel ConvertVoidInvoiceCyberbill(dynamic invoiceRequest, CyberbillConnectionInfo connectionInfo)
		{
			VoidInvoiceCyberbillModel result = new VoidInvoiceCyberbillModel();
			result.doanhnghiep_mst = connectionInfo.TaxNumber;
			result.hoadon_loai = 7; // Mặc định fix số này là xóa bỏ
			result.loaihoadon_ma = "1"; // Mặc định ezCloud Fix là 1
			result.mauso = invoiceRequest.FormNo;
			result.kyhieu = invoiceRequest.Serial;
			result.ma_hoadon = "";
			result.ngaylap = ConvertDateYYYYMMDD(invoiceRequest.InvoiceDate);
			result.hoadon_goc = invoiceRequest.InvoiceId;
			result.hopdong_so = "";
			result.hopdong_ngayky = "";
			result.file_hopdong = "";
			result.nguoilap = "";

			return result;
		}

		/// <summary>
		/// Tạo body cho chức năng Xóa hóa đơn chưa ký
		/// </summary>
		/// <param name="invoiceRequest"></param>
		/// <param name="connectionInfo"></param>
		/// <returns></returns>
		private static DeleteInvoiceCyberbillModel ConvertDeleteInvoiceCyberbill(dynamic invoiceRequest, CyberbillConnectionInfo connectionInfo)
		{
			DeleteInvoiceCyberbillModel result = new DeleteInvoiceCyberbillModel();
			result.doanhnghiep_mst = connectionInfo.TaxNumber;
			result.magiaodich = invoiceRequest.ProviderInvoiceCode;
			return result;
		}
		private static CyberbillConnectionInfo CompanyInfo(CyberbillMappingContext model)
		{
			return new CyberbillConnectionInfo
			{
				CompanyId = Convert.ToString(model.Company.Id),
				PartnerUrl = model.Company.PartnerUrl,
				Username = model.Company.Username,
				Password = model.Company.Password,
				TaxNumber = model.Company.TaxNumber,
			};
		}
		private static string ConvertDateYYYYMMDD(string dateString)
		{
			DateTime date;
			if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
			{
				string formattedDate = date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				return formattedDate;
			}
			else
			{
				return "";
			}
		}
	}
}
