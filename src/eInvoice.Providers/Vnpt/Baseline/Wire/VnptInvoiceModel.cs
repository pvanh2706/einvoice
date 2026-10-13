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

namespace eInvoice.Providers.Vnpt.Baseline
{
    public class Product
    {
        public string Code { get; set; }
        public string ProdName { get; set; }
        public string ProdUnit { get; set; }
        public string ProdQuantity { get; set; }
        public string ProdPrice { get; set; }
        public string Amount { get; set; }
        //public string Remark { get; set; } tạm bỏ do truyền sang báo ERR3
        public string Total { get; set; }
        public string VATRate { get; set; }
        public string VATAmount { get; set; }
        public string Extra1 { get; set; }
		//public string Extra2 { get; set; } tạm bỏ do truyền sang báo ERR3
		//public string Discount { get; set; } tạm bỏ do truyền sang báo ERR3
		//public string DiscountAmount { get; set; } tạm bỏ do truyền sang báo ERR3
		public string IsSum { get; set; }
    }

    //public class VnptExtraItem
    //{
    //    public string Extra_Name { get; set; }
    //    public string Extra_Value { get; set; }
    //}

    //public class VnptExtra
    //{
    //    public VnptExtraItem Extra_item { get; set; }
    //}
    public class Invoice
    {
        public string key { get; set; }
        public string Buyer { get; set; }
        public string CusCode { get; set; }
        public string CusName { get; set; }
        public string CusAddress { get; set; }
        public string CusPhone { get; set; }
        public string CusTaxCode { get; set; }
        public string CusBankNo { get; set; }
        public string CusBankName { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string Total { get; set; }
        public string VATRate { get; set; }
        public string VATAmount { get; set; }
        public string Amount { get; set; }
        public string AmountInWords { get; set; }
        public string ArisingDate { get; set; }
        public string EmailDeliver { get; set; }
        //public string ServicechargeAmount { get; set; } // tạm bỏ do truyền sang bị lỗi ERR3
        public string CurrencyUnit { get; set; }
        public string ExchangeRate { get; set; }
        public string ConvertedAmount { get; set; }
        public string Note { get; set; }
        public string GrossValue { get; set; }
        public string GrossValue5 { get; set; }
        public string GrossValue8 { get; set; }
        public string GrossValue10 { get; set; }
        public string VatAmount0 { get; set; }
        public string VatAmount5 { get; set; }
        public string VatAmount8 { get; set; }
        public string VatAmount10 { get; set; }
        public string VatAmount30 { get; set; }
        public string RoomNo { get; set; }
        public string FolioNo { get; set; }
        public string Arrival { get; set; }
        public string Departure { get; set; }
        public string Type { get; set; }
        public string KindOfService { get; set; }
        public string QuestQuantity { get; set; }
        public string DiscountAmount { get; set; }
        public List<Product> Products { get; set; }
        public string ComBankName { get; set; }
        public string ComBankNo { get; set; }
        public string Extra { get; set; }
    }
    public class Inv
    {
        public string key { get; set; }
        public Invoice Invoice { get; set; }
    }

    public class Invoices
    {
        public Inv Inv { get; set; }
    }


	[XmlRoot(ElementName = "Invoices")]
	public class MultiInvoices
	{
		[XmlElement(ElementName = "Inv")]
		public List<Inv> Inv { get; set; }
	}

	public class AdjustInv : Invoice
    {
    }
	public class ReplaceInv : Invoice
	{
	}
}
