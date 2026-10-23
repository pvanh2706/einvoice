using DSHHDVu = eInvoice.Providers.Vnpt.Baseline.V5.DSHHDVu;
#nullable disable
#pragma warning disable CS1998, CS0219, CS0414
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using eInvoice.Providers.Vnpt.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Dynamic;
using System.Globalization;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using System.Collections.Generic;

namespace eInvoice.Providers.Vnpt.Baseline.MTT
{
	[XmlRoot(ElementName = "TTChung")]
	public class TTChung
	{

		[XmlElement(ElementName = "NLap")]
		public string NLap { get; set; }

		[XmlElement(ElementName = "DVTTe")]
		public string DVTTe { get; set; }

		[XmlElement(ElementName = "TGia")]
		public string TGia { get; set; }

		[XmlElement(ElementName = "HTTToan")]
		public string HTTToan { get; set; }

		// Tạm bỏ do chưa thấy dùng
		//[XmlElement(ElementName = "SHDon")]
		//public string SHDon { get; set; }

		//[XmlElement(ElementName = "MHSo")]
		//public string MHSo { get; set; }

		//[XmlElement(ElementName = "SBKe")]
		//public string SBKe { get; set; }

		//[XmlElement(ElementName = "NBKe")]
		//public string NBKe { get; set; }
	}

	[XmlRoot(ElementName = "NBan")]
	public class NBan
	{
		public TTKhac TTKhac { get; set; }
	}

	[XmlRoot(ElementName = "NMua")]
	public class NMua
	{

		[XmlElement(ElementName = "Ten")]
		public string Ten { get; set; }

		[XmlElement(ElementName = "MST")]
		public string MST { get; set; }

		[XmlElement(ElementName = "SDThoai")]
		public string SDThoai { get; set; }

		[XmlElement(ElementName = "CCCDan")]
		public string CCCDan { get; set; }

		[XmlElement(ElementName = "DCTDTu")]
		public string DCTDTu { get; set; }

		[XmlElement(ElementName = "MKHang")]
		public string MKHang { get; set; }



		[XmlElement(ElementName = "DChi")]
		public string DChi { get; set; }

		[XmlElement(ElementName = "HVTNMHang")]
		public string HVTNMHang { get; set; }

		//[XmlElement(ElementName = "STKNHang")]
		//public string STKNHang { get; set; }

		//[XmlElement(ElementName = "TNHang")]
		//public string TNHang { get; set; }

		//[XmlElement(ElementName = "HVTNNHang")]
		//public string HVTNNHang { get; set; }

		// Bổ sung thêm Extradata để hiển thị trên PDF
		[XmlElement(ElementName = "FolioNo")]
		public string FolioNo { get; set; }

		[XmlElement(ElementName = "RoomNo")]
		public string RoomNo { get; set; }

		[XmlElement(ElementName = "Arrival")]
		public string Arrival { get; set; }

		[XmlElement(ElementName = "Departure")]
		public string Departure { get; set; }
	}

	[XmlRoot(ElementName = "TTin")]
	public class TTin
	{

		[XmlElement(ElementName = "TTruong")]
		public string TTruong { get; set; }

		[XmlElement(ElementName = "KDLieu")]
		public string KDLieu { get; set; }

		[XmlElement(ElementName = "DLieu")]
		public string DLieu { get; set; }
	}

	[XmlRoot(ElementName = "TTKhac")]
	public class TTKhac
	{

		[XmlElement(ElementName = "TTin")]
		public List<TTin> TTin { get; set; }
	}

	[XmlRoot(ElementName = "LTSuat")]
	public class LTSuat
	{

		[XmlElement(ElementName = "TSuat")]
		public string TSuat { get; set; }

		[XmlElement(ElementName = "TThue")]
		public string TThue { get; set; }

		[XmlElement(ElementName = "ThTien")]
		public string ThTien { get; set; }

	}

	[XmlRoot(ElementName = "THTTLTSuat")]
	public class THTTLTSuat
	{

		[XmlElement(ElementName = "LTSuat")]
		public List<LTSuat> LTSuat { get; set; }
	}

	[XmlRoot(ElementName = "TToan")]
	public class TToan
	{

		[XmlElement(ElementName = "THTTLTSuat")]
		public THTTLTSuat THTTLTSuat { get; set; }

		[XmlElement(ElementName = "TgTCThue")]
		public string TgTCThue { get; set; }

		[XmlElement(ElementName = "TgTThue")]
		public string TgTThue { get; set; }

		[XmlElement(ElementName = "TTCKTMai")]
		public string TTCKTMai { get; set; }

		[XmlElement(ElementName = "TgTTTBSo")]
		public string TgTTTBSo { get; set; }

		[XmlElement(ElementName = "TgTTTBChu")]
		public string TgTTTBChu { get; set; }
		public object TTKhac { get; set; } = "";
	}

	[XmlRoot(ElementName = "NDHDon")]
	public class NDHDon
	{

		[XmlElement(ElementName = "NBan")]
		public object NBan { get; set; } = "";

		[XmlElement(ElementName = "NMua")]
		public object NMua { get; set; }

		[XmlElement(ElementName = "DSHHDVu")]
		public DSHHDVu DSHHDVu { get; set; }

		[XmlElement(ElementName = "TToan")]
		public TToan TToan { get; set; }
	}

	[XmlRoot(ElementName = "DLHDon")]
	public class DLHDon
	{

		[XmlElement(ElementName = "TTChung")]
		public TTChung TTChung { get; set; }

		[XmlElement(ElementName = "NDHDon")]
		public object NDHDon { get; set; }
	}

	[XmlRoot(ElementName = "HDon")]
	public class HDon
	{

		[XmlElement(ElementName = "key")]
		public string key { get; set; }

		[XmlElement(ElementName = "DLHDon")]
		public DLHDon DLHDon { get; set; }
	}

	[XmlRoot(ElementName = "DSHDon")]
	public class DSHDon
	{

		[XmlElement(ElementName = "HDon")]
		public HDon HDon { get; set; }
	}
}
