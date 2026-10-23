#nullable disable
#pragma warning disable CS8981
// Wire names preserved from pinned baseline 66a9c74.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Capture.Viettel.Wire
{
    public class CreateInvoiceViettelModel
    {
        public CreateInvoiceViettelModel()
        {
            generalInvoiceInfo generalInvoiceInfo = new generalInvoiceInfo();
            buyerInfo buyerInfo = new buyerInfo();
            List<extAttribute> extAttribute = new List<extAttribute>();
            List<payments> payments = new List<payments>();
            deliveryInfo deliveryInfo = new deliveryInfo();
            List<itemInfo> itemInfo = new List<itemInfo>();
            List<discountItemInfo> discountItemInfo = new List<discountItemInfo>();
            List<metadata> metadata = new List<metadata>();
            invoiceFile invoiceFile = new invoiceFile();
            List<taxBreakdowns> taxBreakdowns = new List<taxBreakdowns>();
            summarizeInfo summarizeInfo = new summarizeInfo();
        }
        public generalInvoiceInfo generalInvoiceInfo { get; set; }
        public buyerInfo buyerInfo { get; set; }
        public sellerInfo sellerInfo { get; set; }
        public List<extAttribute> extAttribute { get; set; }
        public List<payments> payments { get; set; }
        public deliveryInfo deliveryInfo { get; set; }
        public List<itemInfo> itemInfo { get; set; }
        public List<discountItemInfo> discountItemInfo { get; set; }
        public List<metadata> metadata { get; set; }
        public invoiceFile invoiceFile { get; set; }
        public List<taxBreakdowns> taxBreakdowns { get; set; }
        public summarizeInfo summarizeInfo { get; set; }
    }

    public class generalInvoiceInfo
    {
        public string invoiceType { get; set; }
        public string templateCode { get; set; }
        public string invoiceSeries { get; set; }
        public long invoiceIssuedDate { get; set; }
        public string currencyCode { get; set; }
        /// <summary>
        /// Lập hóa đơn điều chỉnh: adjustmentType = ‘5’
        /// Lập hóa đơn thay thế: adjustmentType = ‘3’
        /// Tạo hóa đơn: adjustmentType = ‘1’
        /// </summary>
        public string adjustmentType { get; set; }
        public string invoiceNo { get; set; }

        #region bắt buộc khi thay thế hoặc điều chỉnh thông tin
        public string invoiceNote { get; set; }
        public string originalInvoiceId { get; set; }
        /// <summary>
        /// adjustmentInvoiceType =’1’ nếu là hóa đơn điều chỉnh tiền
        /// adjustmentInvoiceType = ‘2’ nếu là hóa đơn điều chỉnh thông tin
        /// </summary>
        public string adjustmentInvoiceType { get; set; }
        public long? originalInvoiceIssueDate { get; set; }
        public string additionalReferenceDesc { get; set; }
        public long? additionalReferenceDate { get; set; }
        #endregion

        public bool? paymentStatus { get; set; }
        public string paymentType { get; set; }
        public string paymentTypeName { get; set; }
        public bool? cusGetInvoiceRight { get; set; }
        public string buyerIdNo { get; set; }
        public string buyerIdType { get; set; }
        public string transactionUuid { get; set; }
        public string userName { get; set; }
        public Nullable<decimal> validation { get; set; }
        public Nullable<int> adjustAmount20 { get; set; }
        public Nullable<decimal> exchangeRate { get; set; }
    }
    public class buyerInfo
    {
        public string buyerName { get; set; }
        public string buyerLegalName { get; set; }
        public string buyerTaxCode { get; set; }
        public string buyerAddressLine { get; set; }
        public string buyerPhoneNumber { get; set; }
        public string buyerFaxNumber { get; set; }
        public string buyerEmail { get; set; }
        public string buyerBankName { get; set; }
        public string buyerBankAccount { get; set; }
        public string buyerDistrictName { get; set; }
        public string buyerCityName { get; set; }
        public string buyerCountryCode { get; set; }
        public string buyerIdNo { get; set; }
        public int? buyerIdType { get; set; }
        public int? buyerNotGetInvoice { get; set; }
        public string buyerBudgetCode { get; set; }
    }
    public class sellerInfo
    {
        public string sellerLegalName { get; set; }
        public string sellerTaxCode { get; set; }
        public string sellerAddressLine { get; set; }
        public string sellerPhoneNumber { get; set; }
        public string sellerEmail { get; set; }
        public string sellerBankName { get; set; }
        public string sellerBankAccount { get; set; }
        public string sellerDistrictName { get; set; }
        public string sellerCityName { get; set; }
        public string sellerCountryCode { get; set; }
    }
    public class extAttribute
    {

    }
    public class payments
    {
        public string paymentMethodName { get; set; }
        public string paymentMethod { get; set; }
    }
    public class itemInfo
    {
        public string lineNumber { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string unitCode { get; set; }
        public string unitName { get; set; }
        public decimal? unitPrice { get; set; }
        public decimal? quantity { get; set; }
        public decimal? itemTotalAmountWithoutTax { get; set; }
        public decimal? itemTotalAmountWithTax { get; set; }
        public decimal? itemTotalAmountAfterDiscount { get; set; }
        public decimal? itemDiscount { get; set; }
        public decimal? discount { get; set; }
        public decimal? taxPercentage { get; set; }
        public decimal? taxAmount { get; set; }
        public decimal? adjustmentTaxAmount { get; set; }
        public bool? isIncreaseItem { get; set; }
        public string itemNote { get; set; }
        public string batchNo { get; set; }
        public string expDate { get; set; }
        public int? selection { get; set; }
        public int? adjustRatio { get; set; }

    }
    public class deliveryInfo
    {

    }
    public class discountItemInfo
    {
        public string lineNumber { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string unitCode { get; set; }
        public string unitName { get; set; }
        public decimal? unitPrice { get; set; }
        public decimal? quantity { get; set; }
        public decimal? itemTotalAmountWithoutTax { get; set; }
        public decimal? taxPercentage { get; set; }
        public decimal? taxAmount { get; set; }
        public decimal? adjustmentTaxAmount { get; set; }
        public bool? isIncreaseItem { get; set; }
    }
    public class metadata
    {
        public string keyTag { get; set; }
        public string valueType { get; set; }
        public string keyLabel { get; set; }
        public string stringValue { get; set; }
    }
    public class invoiceFile
    {
        public string fileContent { get; set; }
        public string fileType { get; set; }
        public string fileExtension { get; set; }
    }
    public class meterReading
    {
        public decimal? previousIndex { get; set; }
        public decimal? currentIndex { get; set; }
        public decimal? factor { get; set; }
        public decimal? amount { get; set; }
    }
    public class summarizeInfo
    {
        public decimal sumOfTotalLineAmountWithoutTax { get; set; }
        public decimal totalAmountWithoutTax { get; set; }
        public decimal totalTaxAmount { get; set; }
        public decimal totalAmountWithTax { get; set; }
        public decimal? totalAmountWithTaxFrn { get; set; }
        public string totalAmountWithTaxInWords { get; set; }
        public bool? isTotalAmountPos { get; set; }
        public bool? isTotalTaxAmountPos { get; set; }
        public bool? isTotalAmtWithoutTaxPos { get; set; }
        public decimal? discountAmount { get; set; }
        public decimal? totalAmountAfterDiscount { get; set; }
        public bool? isDiscountAmtPos { get; set; }
    }
    public class taxBreakdowns
    {
        public decimal? taxPercentage { get; set; }
        public decimal? taxableAmount { get; set; }
        public decimal? taxAmount { get; set; }
        public bool? taxableAmountPos { get; set; }
        public bool? taxAmountPos { get; set; }
        public string taxExemptionReason { get; set; }
    }

    public class ViettelConnectionInfo
    {
        public string CompanyId { get; set; }
        public string PartnerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string TaxNumber { get; set; }
    }

    public class ViettelDownloadPdfModel
    {
        public string supplierTaxCode { get; set; }
        public string invoiceNo { get; set; }
        public string templateCode { get; set; }
        public string transactionUuid { get; set; }
        public string fileType { get; set; }
    }

    public class ViettelVoidInvoiceModel
    {
        public string supplierTaxCode { get; set; }
        public string templateCode { get; set; }
        public string invoiceNo { get; set; }
        public string strIssueDate { get; set; }
        public string additionalReferenceDate { get; set; }
        public string additionalReferenceDesc { get; set; }
        public string reasonDelete { get; set; }
    }

    public class ViettelSearchByIdModel
    {
        public string supplierTaxCode { get; set; }
        public string transactionUuid { get; set; }
    }
}
