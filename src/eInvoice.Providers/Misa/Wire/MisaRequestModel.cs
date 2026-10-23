// Provider contract/mapping ported from pinned legacy 66a9c74. Wire names must remain unchanged.
// Nullable warnings are disabled only in this baseline boundary; callers validate canonical input.
#nullable enable annotations
#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Misa.Wire
{
    public class MisaRequestModel
    {

    }

    public class MisaCancelTicketRequestModel
    {
        public string TransactionID { get; set; }
        public string InvNo { get; set; }
        public string RefDate { get; set; }
        public string CancelReason { get; set; }
    }

    public class MisaCancelInvoiceRequestModel
    {
        public string TransactionID { get; set; }
        public string RefNo { get; set; }
        public string RefDate { get; set; }
        public string DeletedReason { get; set; }
    }

    public class MisaInvoiceRequestModel
    {
        public List<Invoicedatalist> InvoiceDataList { get; set; }
        public string userName { get; set; }
        public string passWord { get; set; }
    }

    public class Invoicedatalist
    {
        public string RefID { get; set; }
        public string InvoiceType { get; set; }
        public string TemplateCode { get; set; }
        public string InvoiceSeries { get; set; }
        public string CurrencyCode { get; set; }
        public int AdjustmentType { get; set; }
        public string PaymentMethodName { get; set; }
        public string SellerTaxCode { get; set; }
        public string SellerLegalName { get; set; }
        public string SellerAddressLine { get; set; }
        public object SellerBankAccount { get; set; }
        public object SellerBankName { get; set; }
        public string BuyerLegalName { get; set; }
        public string BuyerDisplayName { get; set; }
        public object BuyerTaxCode { get; set; }
        public object BuyerAddressLine { get; set; }
        public object BuyerPhoneNumber { get; set; }
        public object BuyerEmail { get; set; }
        public object BuyerBankAccount { get; set; }
        public object BuyerBankName { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal TotalAmountWithoutVAT { get; set; }
        public decimal TotalVATAmount { get; set; }
        public decimal TotalAmountWithVAT { get; set; }
        public string TotalAmountWithVATInWords { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmountWithVATFrn { get; set; }
        public bool IsSendEmail { get; set; }
        public object ReceiverName { get; set; }
        public object ReceiverEmail { get; set; }
        public string InvDate { get; set; }
        public string AccountObjectCode { get; set; }
        public List<Originalinvoicedetail> OriginalInvoiceDetail { get; set; }
        public Optionuserdefined OptionUserDefined { get; set; }
        public string ReceiptName { get; set; }

		public string BuyerIDNumber { get; set; }
		public string BuyerPassport { get; set; }
		public string BuyerBudgetCode { get; set; }
		public string SellerShopCode { get; set; }
		public string SellerShopName { get; set; }

	}

    public class Optionuserdefined
    {
        public string MainCurrency { get; set; }
		public string UnitPriceOCDecimalDigits { get; set; }

		/// <summary>
		/// Định dạng đơn giá quy đổi
		/// </summary>
		public string UnitPriceDecimalDigits { get; set; }
		/// <summary>
		/// Định dạng số lượng
		/// </summary>
		public string QuantityDecimalDigits { get; set; }
	}

    public class Originalinvoicedetail
    {
        public int LineNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal VatAmount { get; set; }
        public decimal UnitPrice { get; set; }
        public bool Promotion { get; set; }
    }
    public class MisaSendCQTRequest
    {
        public long TakeTime { get; set; }
        public string AppID { get; set; }
        public string SignType { get; set; }
    }

    public class MisaGetStatusInvoiceByRefIDMTT
    {
        public List<string> ezInvoiceID { get; set; }
    }

    public class GetXMLInvoiceRequest
    {
        public long TakeTime { get; set; }
        public string TaxCode { get; set; }
        public string X509RawData { get; set; }
    }

	public class SendInvoiceToCQTRequest
	{
		public string SignatureValue { get; set; }
		public string MessageCode { get; set; }
	}
}
