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
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eInvoice.Providers.Vnpt.Baseline.V5
{
    [XmlRoot(ElementName = "TTChung")]
    public class TTChung
    {

        //[XmlElement(ElementName = "SHDon")]
        //public string SHDon { get; set; }

        //[XmlElement(ElementName = "MHSo")]
        //public string MHSo { get; set; }

        //[XmlElement(ElementName = "SBKe")]
        //public string SBKe { get; set; }

        [XmlElement(ElementName = "NBKe")]
        public string NBKe { get; set; }

        [XmlElement(ElementName = "DVTTe")]
        public string DVTTe { get; set; }

        [XmlElement(ElementName = "TGia")]
        public string TGia { get; set; }

        [XmlElement(ElementName = "HTTToan")]
        public string HTTToan { get; set; }
    }

    [XmlRoot(ElementName = "NBan")]
    public class NBan
    {

        [XmlElement(ElementName = "Ten")]
        public string Ten { get; set; }

        [XmlElement(ElementName = "MST")]
        public string MST { get; set; }

        [XmlElement(ElementName = "DChi")]
        public string DChi { get; set; }

        [XmlElement(ElementName = "SDThoai")]
        public string SDThoai { get; set; }

        [XmlElement(ElementName = "DCTDTu")]
        public string DCTDTu { get; set; }

        [XmlElement(ElementName = "STKNHang")]
        public string STKNHang { get; set; }

        [XmlElement(ElementName = "TNHang")]
        public string TNHang { get; set; }

        [XmlElement(ElementName = "Fax")]
        public string Fax { get; set; }

        [XmlElement(ElementName = "LDDNBo")]
        public string LDDNBo { get; set; }

        [XmlElement(ElementName = "HDSo")]
        public string HDSo { get; set; }

        [XmlElement(ElementName = "HVTNXHang")]
        public string HVTNXHang { get; set; }

        [XmlElement(ElementName = "TNVChuyen")]
        public string TNVChuyen { get; set; }

        [XmlElement(ElementName = "PTVChuyen")]
        public string PTVChuyen { get; set; }

        [XmlElement(ElementName = "HDKTSo")]
        public string HDKTSo { get; set; }

        [XmlElement(ElementName = "HDKTNgay")]
        public string HDKTNgay { get; set; }
    }

    [XmlRoot(ElementName = "NMua")]
    public class NMua
    {

        [XmlElement(ElementName = "Ten")]
        public string Ten { get; set; }

        [XmlElement(ElementName = "MST")]
        public string MST { get; set; }

        [XmlElement(ElementName = "DChi")]
        public string DChi { get; set; }

        [XmlElement(ElementName = "MKHang")]
        public string MKHang { get; set; }

        [XmlElement(ElementName = "SDThoai")]
        public string SDThoai { get; set; }

        [XmlElement(ElementName = "DCTDTu")]
        public string DCTDTu { get; set; }

        [XmlElement(ElementName = "HVTNMHang")]
        public string HVTNMHang { get; set; }

        [XmlElement(ElementName = "STKNHang")]
        public string STKNHang { get; set; }

        [XmlElement(ElementName = "TNHang")]
        public string TNHang { get; set; }

        [XmlElement(ElementName = "HVTNNHang")]
        public string HVTNNHang { get; set; }
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


    [XmlRoot(ElementName = "HHDVu")]
    public class HHDVu
    {

        [XmlElement(ElementName = "TChat")]
        public string TChat { get; set; }

        [XmlElement(ElementName = "STT")]
        public string STT { get; set; }
        [XmlElement(ElementName = "MHHDVu")]
        public string MHHDVu { get; set; }

        [XmlElement(ElementName = "THHDVu")]
        public string THHDVu { get; set; }

        [XmlElement(ElementName = "DVTinh")]
        public string DVTinh { get; set; }

        [XmlElement(ElementName = "SLuong")]
        public string SLuong { get; set; }

        [XmlElement(ElementName = "DGia")]
        public string DGia { get; set; }

        [XmlElement(ElementName = "TLCKhau")]
        public string TLCKhau { get; set; }

        [XmlElement(ElementName = "STCKhau")]
        public string STCKhau { get; set; }

        [XmlElement(ElementName = "ThTien")]
        public string ThTien { get; set; }

        [XmlElement(ElementName = "TSuat")]
        public string TSuat { get; set; }

        [XmlElement(ElementName = "TThue")]
        public string TThue { get; set; }

        [XmlElement(ElementName = "TSThue")]
        public string TSThue { get; set; }
    }

    [XmlRoot(ElementName = "DSHHDVu")]
    public class DSHHDVu
    {

        [XmlElement(ElementName = "HHDVu")]
        public List<HHDVu> HHDVu { get; set; }
    }

    [XmlRoot(ElementName = "LTSuat")]
    public class LTSuat
    {

        [XmlElement(ElementName = "TSuat")]
        public string TSuat { get; set; }

        [XmlElement(ElementName = "ThTien")]
        public string ThTien { get; set; }

        [XmlElement(ElementName = "TThue")]
        public string TThue { get; set; }
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

        //[XmlElement(ElementName = "TTCKTMai")]
        //public string TTCKTMai { get; set; }

        [XmlElement(ElementName = "TgTTTBSo")]
        public string TgTTTBSo { get; set; }

        [XmlElement(ElementName = "TgTTTBChu")]
        public string TgTTTBChu { get; set; }
    }

    [XmlRoot(ElementName = "NDHDon")]
    public class NDHDon
    {

        [XmlElement(ElementName = "NBan")]
        public NBan NBan { get; set; }

        [XmlElement(ElementName = "NMua")]
        // public NMua NMua { get; set; }
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
        public NDHDon NDHDon { get; set; }
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
	[XmlRoot(ElementName = "DSHDon")]
	public class DSHDonMulti
	{

		[XmlElement(ElementName = "HDon")]
		public List<HDon> HDon { get; set; }
	}

}
