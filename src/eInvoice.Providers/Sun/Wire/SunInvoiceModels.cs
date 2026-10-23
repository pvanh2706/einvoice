#nullable disable
namespace eInvoice.Providers.Sun.Wire
{
    public class SunInvoiceDetailModel
    {
        public int line { get; set; }
        public string transdate { get; set; }
        public string name { get; set; }
        public string unit { get; set; }
        public decimal quantity { get; set; }
        public decimal price { get; set; }
        public decimal amount { get; set; }
        public decimal amountv { get; set; }
        public string vrt { get; set; }
        public string vrn { get; set; }
        public decimal vat { get; set; }
        public decimal vatv { get; set; }
        public decimal total { get; set; }
        public decimal totalv { get; set; }
        public string type { get; set; }
    }
    /// <summary>
    /// ngày đến (Arrival)   ---> c0 (dd/mm/yyyy)
    /// ngày đi (Departure) ---> c1 (dd/mm/yyyy)
    /// Chiết khấu (DiscountAmount)  --> c2
    /// Số phòng (Room)  ---> c3
    /// Số lượng khách --> c4
    /// Số chứng từ (Folio)  ---> c5
    /// Thuế tiêu thụ đặc biệt % (SPECRate)  ---> c6
    /// Thuế tiêu thụ đặc biệt (SPECAmount)  --->  c7
    /// Phí phục vụ % (SVCRate)  --->  c8
    /// Phí phục vụ (SVCAmount)  ---> c9
    /// </summary>
    public class SunInvoiceModel
    {
        public string idt_ { get; set; }
        public string bname { get; set; }
        public string idt { get; set; }
        public string type { get; set; }
        public string btax { get; set; }
        public string stax { get; set; }
        public string sid { get; set; }
        public string buyer { get; set; }
        public string btel { get; set; }
        public string bcode { get; set; }
        public string bmail { get; set; }
        public string form { get; set; }
        public string mdvqhnsach { get; set; }
        public string socccd_dinhdanh { get; set; }
        public string so_hochieu { get; set; }
        public string baddr { get; set; }
        public string bacc { get; set; }
        public string serial { get; set; }
        public string curr { get; set; }
        public decimal exrt { get; set; }
        public string paym { get; set; }
        public string bbank { get; set; }
        public string seq { get; set; }
        public string note { get; set; }
        public decimal sumv { get; set; }
        public decimal sum { get; set; }
        public decimal score { get; set; }
        public string discount { get; set; }
        public decimal vatv { get; set; }
        public decimal vat { get; set; }
        public string word { get; set; }
        public decimal totalv { get; set; }
        public decimal total { get; set; }
        public string c0 { get; set; }
        public string c1 { get; set; }
        public decimal c2 { get; set; }
        public string c3 { get; set; }
        public string c4 { get; set; }
        public string c5 { get; set; }
        public int? c6 { get; set; }
        public decimal c7 { get; set; }
        public int? c8 { get; set; }
        public decimal c9 { get; set; }
        public List<SunTaxModel> tax { get; set; }
        public string name { get; set; }
        public List<SunInvoiceDetailModel> items { get; set; }
        public int delaySign { get; set; }
		public string taxc { get; set; }
        public int api { get; set; }
        public int aun { get; set; }
	}

    public class SunTaxModel
    {
        public string vrt { get; set; }
        public string vrn { get; set; }
        public decimal amt { get; set; }
        public decimal amtv { get; set; }
        public decimal vat { get; set; }
        public decimal vatv { get; set; }
    }

    public class SunConnectionInfo
    {
        public string CompanyId { get; set; }
        public string TaxNumber { get; set; }
        public string PartnerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PartnerUrl2 { get; set; }
        public string Username2 { get; set; }
        public string Password2 { get; set; }
    }

    public class SunSearchCqt
    {
        public string sid { get; set; }
        public string taxc { get; set; }
    }

	public class SunAdjustInvoiceModel : SunInvoiceModel
    {
        public int adjType { get; set; }
        public int invType { get; set; }
		public ParamAdjustData adj { get; set; }
	}
	public class ParamAdjustData
	{
		public string seq { get; set; }
		public string rdt { get; set; }
		public string rea { get; set; }
		public string des { get; set; }
		public string sid { get; set; }
		public string adjtyp { get; set; }
        public int typ { get; set; }
	}

	public class SunReplaceInvoiceModel : SunInvoiceModel
	{
		public int adjType { get; set; }
		public int invType { get; set; }
		public ParamReplaceData adj { get; set; }
	}
	public class ParamReplaceData
	{
		public string seq { get; set; }
		public string rdt { get; set; }
		public string rea { get; set; }
		public string des { get; set; }
		public string sid { get; set; }
		public int typ { get; set; }
	}
}
