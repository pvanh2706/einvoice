// Provider contract/mapping ported from pinned legacy 66a9c74. Wire names must remain unchanged.
// Nullable warnings are disabled only in this baseline boundary; callers validate canonical input.
#nullable enable annotations
#nullable disable warnings
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eInvoice.Providers.Misa.Wire
{
    public class MisaTicketResponseModel
    {
        [JsonProperty("Success")]
        public bool Success { get; set; }

        [JsonProperty("ErrorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("DescriptionErrorCode")]
        public string DescriptionErrorCode { get; set; }

        [JsonProperty("Errors")]
        public List<string> Errors { get; set; } = new List<string>();

        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("CustomData")]
        public string CustomData { get; set; }
    }

    public class MisaReceiptResponseModel
    {
        public bool Success { get; set; }
        public string Data { get; set; }
        public string? NewData { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string? ErrorCode { get; set; }
        public string? ErrorCodeDetail { get; set; }
    }

    public class LoginResponseModel
    {

    }
    public class InvoiceResponseModel
    {
        public string RefID { get; set; }
        public string TransactionID { get; set; }
        public string InvTemplateNo { get; set; }
        public string InvSeries { get; set; }
        public string InvNo { get; set; }
        public string InvCode { get; set; }
        public string InvDate { get; set; }
        public string ErrorCode { get; set; }
        public string InvoiceData { get; set; }
        public string DescriptionErrorCode { get; set; }
    }
    public class CreateTicketResponseModel
    {
        public string RefID { get; set; }
        public string? TransactionID { get; set; }
        public string? InvNo { get; set; }
        public string? InvDate { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class DownloadPDFResponseModel
    {
        [JsonProperty("TransactionID")]
        public string TransactionID { get; set; }

        [JsonProperty("Data")]
        public string Data { get; set; }

        [JsonProperty("ErrorCode")]
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Thông tin công ty
    /// </summary>
    public class Company
    {
        /// <summary>
        /// ID công ty
        /// </summary>
        public int CompanyID { get; set; }

        /// <summary>
        /// Mã công ty
        /// </summary>
        public string CompanyCode { get; set; }

        /// <summary>
        /// Tên công ty
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Mã bảo mật
        /// </summary>
        public string SecureToken { get; set; }

        /// <summary>
        /// Mã số thuế công ty
        /// </summary>
        public string CompanyTaxCode { get; set; }

        /// <summary>
        /// Ngưng hoạt dộng
        /// </summary>
        public bool Inactive { get; set; }

        /// <summary>
        /// Địa chỉ công ty
        /// </summary>
        public string CompanyAddress { get; set; }

        /// <summary>
        /// Tỉnh/ Thành phố
        /// </summary>
        public string CompanyCity { get; set; }

        /// <summary>
        /// Mã nhân viên MISA
        /// </summary>
        public string MISAEmployeeCode { get; set; }

        /// <summary>
        /// Đơn vị tích hợp
        /// </summary>
        public int SourceType { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Ngày tích hợp
        /// </summary>
        public DateTime? IntegratedDate { get; set; }

        /// <summary>
        /// Mã số thuế không dấu
        /// </summary>
        public string CompanyTaxCodeWithoutSign { get; set; }

        /// <summary>
        /// Thông tin người đại diện pháp luật
        /// </summary>
        public string CompanyAgentFollowLaw { get; set; }

        /// <summary>
        /// ĐT cty
        /// </summary>
        public string CompanyTel { get; set; }

        /// <summary>
        /// Email cty
        /// </summary>
        public string CompanyEmail { get; set; }

        /// <summary>
        /// Website
        /// </summary>
        public string CompanyWebsite { get; set; }

        /// <summary>
        /// Lĩnh vực hoạt động
        /// </summary>
        public string BusinessType { get; set; }

        /// <summary>
        /// TK Ngân Hàng
        /// </summary>
        public string BankAccount { get; set; }

        /// <summary>
        /// Ngân hàng
        /// </summary>
        public string BankName { get; set; }

        /// <summary>
        /// Chi nhánh NH
        /// </summary>
        public string BankBranchName { get; set; }

        /// <summary>
        /// Cơ quan thuế cấp cục
        /// </summary>
        public string TaxationBureau { get; set; }

        /// <summary>
        /// CQ thuế quản lý
        /// </summary>
        public string TaxOrganManagement { get; set; }

        /// <summary>
        /// Mã CQ thuế quản lý
        /// </summary>
        public string TaxOrganManagementCode { get; set; }

        /// <summary>
        /// Người đại diện pháp luật
        /// </summary>
        public string Director { get; set; }

        /// <summary>
        /// Fax công ty
        /// </summary>
        public string CompanyFax { get; set; }

        /// <summary>
        /// nghệ nghiệp lĩnh vực hoạt động của công ty
        /// </summary>
        public string Career { get; set; }

        /// <summary>
        /// email người đại diện pháp luật
        /// </summary>
        public string LegalepresentationEmail { get; set; }

        /// <summary>
        /// SĐT người đại diện pháp luật
        /// </summary>
        public string LegalepresentationPhone { get; set; }

        /// <summary>
        /// Thông tư áp dụng
        /// </summary>
        public string CircularFollow { get; set; }

        /// <summary>
        /// True: sử dụng HĐ có mã
        /// </summary>
        public bool IsInvoiceWithCode { get; set; }

        /// <summary>
        /// Sử dụng vé
        /// </summary>
        public bool IsUsingTicket { get; set; }

        /// <summary>
        /// Người liên hệ
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// Địa chỉ liên hệ
        /// </summary>
        public string ContactAddress { get; set; }

        /// <summary>
        /// Email liên hệ
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// Điện thoại liên hệ
        /// </summary>
        public string ContactMobile { get; set; }

        /// <summary>
        /// Có tờ khai được chấp nhận hay chưa
        /// </summary>
        public bool HasAccepted123 { get; set; }
    }

    public class CreateInvoiceResponseModel
    {
        [JsonProperty("RefID")]
        public string RefID { get; set; }

        [JsonProperty("TransactionID")]
        public string? TransactionID { get; set; }

        [JsonProperty("TemplateCode")]
        public string? TemplateCode { get; set; }

        [JsonProperty("InvoiceSeries")]
        public string? InvoiceSeries { get; set; }

        [JsonProperty("InvoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonProperty("InvoiceIssuedDate")]
        public string? InvoiceIssuedDate { get; set; }

        [JsonProperty("ErrorCode")]
        public string? ErrorCode { get; set; }
    }

    public class InvFromSerialResponseModel
    {
        [JsonProperty("InvoiceTemplateID")]
        public string InvoiceTemplateID { get; set; }

        [JsonProperty("InvoiceTemplateName")]
        public string InvoiceTemplateName { get; set; }

        [JsonProperty("InvoiceType")]
        public string InvoiceType { get; set; }

        [JsonProperty("TemplateCode")]
        public string TemplateCode { get; set; }

        [JsonProperty("InvoiceSeries")]
        public string InvoiceSeries { get; set; }

        [JsonProperty("UsingStartedDate")]
        public DateTime UsingStartedDate { get; set; }

        [JsonProperty("TemplateType")]
        public int TemplateType { get; set; }

        [JsonProperty("BusinessAreas")]
        public int BusinessAreas { get; set; }
    }

    /// <summary>
    /// Mẫu hóa đơn
    /// </summary>
    public class TemplateData
    {
        /// <summary>
        /// ID mẫu
        /// </summary>
        public Guid IPTemplateID { get; set; }
        /// <summary>
        /// ID công ty
        /// </summary>
        public int CompanyID { get; set; }
        /// <summary>
        /// Tên mẫu
        /// </summary>
        public string TemplateName { get; set; }
        /// <summary>
        /// Mẫu số
        /// </summary>
        public string InvTemplateNo { get; set; }
        /// <summary>
        /// Ký hiệu
        /// </summary>
        public string InvSeries { get; set; }

        /// <summary>
        /// Ký hiệu
        /// </summary>
        public string OrgInvSeries { get; set; }
        /// <summary>
        /// Loại mẫu (stimul, xslt,...)
        /// </summary>
        public int TemplateType { get; set; }
        /// <summary>
        /// Loại hóa đơn (GTGT, bán hàng, xuất kho,...)
        /// </summary>
        public int InvoiceType { get; set; }
        /// <summary>
        /// Nghiệp vụ
        /// </summary>
        public int BusinessAreas { get; set; }
        /// <summary>
        /// Thứ tự
        /// </summary>
        public int SortOrder { get; set; }
        /// <summary>
        /// Ngày ký
        /// </summary>
        public DateTime? SignedDate { get; set; }
        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// Người tạo
        /// </summary>
        public string CreatedBy { get; set; }
        /// <summary>
        /// Ngày sửa
        /// </summary>
        public DateTime ModifiedDate { get; set; }
        /// <summary>
        /// Người sửa
        /// </summary>
        public string ModifiedBy { get; set; }
        /// <summary>
        /// Ngừng hoạt động
        /// </summary>
        /// <returns></returns>
        public bool Inactive { get; set; }
        /// <summary>
        /// Nội dung file mẫu
        /// </summary>
        public byte[] TemplateContent { get; set; }
        /// <summary>
        /// ID mẫu mặc định
        /// </summary>
        public Guid DefaultTemplateID { get; set; }
        /// <summary>
        /// Có phải mẫu custom không
        /// </summary>
        public bool IsCustomTemplate { get; set; }
        /// <summary>
        /// kế thừa từ mẫu cũ hay không
        /// </summary>
        public bool IsInheritFromOldTemplate { get; set; }

        /// <summary>
        /// Phiên bản mẫu xslt
        /// </summary>
        public int? XsltVersion { get; set; }
        /// <summary>
        /// Đã phát hành hđ hay chưa
        /// </summary>
        public bool IsPublished { get; set; }
    }
	public class StatusInvoiceResponse
	{
		public string RefID { get; set; }
		public string InvNo { get; set; }
		public DateTime InvDate { get; set; }
		public string InvSeries { get; set; }
		public string InvTempl { get; set; }
		public string TransactionID { get; set; }
		public int PublishStatus { get; set; }
		public int EInvoiceStatus { get; set; }
		public int ReferenceType { get; set; }
		public string InvoiceCode { get; set; }
		public string MessageCode { get; set; }
		public string SourceType { get; set; }
		public int SendTaxStatus { get; set; }
		public bool IsSentEmail { get; set; }
		public bool IsDelete { get; set; }
		public DateTime? DeletedDate { get; set; }
		public string DeletedReason { get; set; }
		public int ReceivedStatus { get; set; }
		public string OrgTransactionID { get; set; }
		public int OrgInvoiceStatus { get; set; }
		public int RefInvoiceStatus { get; set; }
		public string SummaryStatus { get; set; }
	}
    public class XMLInvoiceHashResponse
	{
        public string MessageCode { get; set; }
        public string MessageHash { get; set; }
    }
}
