#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Cyberbill.Wire
{
	public static class EndpointApi
	{
		public const string LOGIN = "api/services/hddtws/Authentication/GetToken";
		public const string CREATE_AND_PUBLISH = "";
		public const string REPLACE = "api/services/hddtws/GuiHoadon/GuiHoadonThayThe";
		public const string ADJUST = "api/services/hddtws/GuiHoadon/GuiHoadonDieuChinh";
		public const string DOWNLOAD_PDF = "api/services/hddtws/GuiHoaDon/TaiHoaDonPDF";
		public const string VOID = "api/services/hddtws/GuiHoaDon/GuiHoadonHuyBo";
		public const string DRAFT = "api/services/hddtws/GuiHoadon/GuiHoadonGoc";
		public const string PREVIEW = "api/services/hddtws/GuiHoaDon/TaiHoaDonPdfChuaKy";
		public const string SEARCH_BY_ID = "";
		public const string SIGN = "api/services/hddtws/XuLyHoaDon/KyHoaDonHSM";
		public const string DELETE = "api/services/hddtws/GuiHoadon/XoaHoaDon";
	}
}
