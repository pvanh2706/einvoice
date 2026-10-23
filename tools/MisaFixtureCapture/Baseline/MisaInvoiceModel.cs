// Provider contract/mapping ported from pinned legacy 66a9c74. Wire names must remain unchanged.
// Nullable warnings are disabled only in this baseline boundary; callers validate canonical input.
#nullable enable annotations
#nullable disable warnings
using Newtonsoft.Json;

namespace eInvoice.Capture.Misa.Wire
{
    public class MisaTicketModel
    {
        [JsonProperty("RefID")]
        public string RefID { get; set; }
        [JsonProperty("OriginalInvoiceData")]
        public OriginalInvoiceData OriginalInvoiceData { get; set; }
        public bool IsSendEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public string ReceiverName { get; set; }

    }

    public class MisaConnectionInfo
    {
        public string CompanyId { get; set; }
        public string PartnerUrl { get; set; }
        public string PartnerUrl2 { get; set; }
        public string Username { get; set; }
        public string Username2 { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }
        public string TaxNumber { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
    }



	public class CreateAndPublishInvoiceRequestNormal
	{
		public string RefID { get; set; }
		public OriginalInvoiceData OriginalInvoiceData { get; set; }
		public bool IsSendEmail { get; set; }
		public string ReceiverEmail { get; set; }
		public string ReceiverName { get; set; }
	}

	public class CreateAndPublishInvoiceMTTRequest
	{
		public string RefID { get; set; }
		public OriginalInvoiceDataNormal OrgInvoiceData { get; set; }
		public bool IsSendEmail { get; set; }
		public string ReceiverEmail { get; set; }
		public string ReceiverName { get; set; }
	}

    public class OriginalInvoiceDataNormal : OriginalInvoiceData
    {
        /// <summary>
        /// Là hóa đơn tạo từ MTT
        /// </summary>
        public bool? IsInvoiceCalculatingMachine { get; set; }
		public int? TaxReductionType { get; set; }
	}
    /// <summary>
    /// Thông tin vé gốc
    /// </summary>
    public class OriginalInvoiceData
    {
        /// <summary>
        /// ID của vé trên Client App
        /// </summary>
        public string RefID { get; set; }

        /// <summary>
        /// Ký hiệu vé
        /// </summary>
        public string InvSeries { get; set; }

        /// <summary>
        /// Tên vé
        /// </summary>
        public string InvoiceName { get; set; }

        /// <summary>
        /// Ngày vé
        /// </summary>
        /// <returns></returns>
        public string InvDate { get; set; }

        /// <summary>
        /// Mã loại tiền tệ
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Tỷ giá
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Hình thức thanh toán
        /// </summary>
        public string PaymentMethodName { get; set; }

        /// <summary>
        /// Tính chất vé (1: thay thế; 2: điều chỉnh)
        /// </summary>
        public int? ReferenceType { get; set; }

        /// <summary>
        /// Là vé gửi dạng bảng tổng hợp
        /// </summary>
        public bool IsInvoiceSummary { get; set; }

        /// <summary>
        /// Loại vé bị thay thế/điều chỉnh (1: vé 123; 2: vé có mã; 3: vé 51; 4: vé đặt in 123)
        /// </summary>
        public int? OrgInvoiceType { get; set; }

        /// <summary>
        /// Ký hiệu mẫu hđ bị thay thế/điều chỉnh
        /// </summary>
        public string OrgInvTemplateNo { get; set; }

        /// <summary>
        /// Ký hiệu hđ bị thay thế/điều chỉnh
        /// </summary>
        public string OrgInvSeries { get; set; }

        /// <summary>
        /// Số hđ bị thay thế/điều chỉnh
        /// </summary>
        public string OrgInvNo { get; set; }

        /// <summary>
        /// Ngày vé bị thay thế/điều chỉnh
        /// </summary>
        public string OrgInvDate { get; set; }

        /// <summary>
        /// Tên đơn vị mua
        /// </summary>
        public string BuyerLegalName { get; set; }

        /// <summary>
        /// MST người mua
        /// </summary>
        public string BuyerTaxCode { get; set; }

        /// <summary>
        /// Địa chỉ người mua
        /// </summary>
        public string BuyerAddress { get; set; }

        /// <summary>
        /// Mã khách hàng
        /// </summary>
        public string BuyerCode { get; set; }

        /// <summary>
        /// ĐT người mua
        /// </summary>
        public string BuyerPhoneNumber { get; set; }

        /// <summary>
        /// Email người mua
        /// </summary>
        public string BuyerEmail { get; set; }

        /// <summary>
        /// Tên người mua hàng
        /// </summary>
        public string BuyerFullName { get; set; }

        /// <summary>
        /// Số TK ngân hàng người mua
        /// </summary>
        public string BuyerBankAccount { get; set; }

        /// <summary>
        /// Tên ngân hàng người mua
        /// </summary>
        public string BuyerBankName { get; set; }

        /// <summary>
        /// Tỷ lệ chiết khấu cho vé chiết khấu theo giá trị vé
        /// </summary>
        public decimal DiscountRate { get; set; }

        /// <summary>
        /// tổng tiền chưa thuế
        /// </summary>
        public decimal TotalAmountWithoutVATOC { get; set; }

        /// <summary>
        /// Tổng tiền thuế
        /// </summary>
        public decimal TotalVATAmountOC { get; set; }

        /// <summary>
        /// Tổng tiền CKTM
        /// </summary>
        public decimal TotalDiscountAmountOC { get; set; }

        /// <summary>
        /// Tổng tiền bằng số
        /// </summary>
        public decimal? TotalAmountOC { get; set; }

        /// <summary>
        /// Tổng tiền bằng chữ
        /// </summary>
        public string TotalAmountInWords { get; set; }

        #region "Các thông tin bổ sung"

        /// <summary>
        /// Chi tiết vé
        /// </summary>
        /// <returns></returns>
        public List<OriginalInvoiceDetail> OriginalInvoiceDetail { get; set; }

        /// <summary>
        /// Danh sách các loại thuế suất
        /// </summary>
        public List<TaxRateInfo> TaxRateInfo { get; set; }

        /// <summary>
        /// Danh sách các loại phí
        /// </summary>
        public List<FeeInfo> FeeInfo { get; set; }

        /// <summary>
        /// Định dạng số để hiển thị vé
        /// </summary>
        public OptionUserDefined OptionUserDefined { get; set; }

        /// <summary>
        /// ID mẫu vé
        /// </summary>
        public Guid? InvoiceTemplateID { get; set; }

        /// <summary>
        /// Tổng tiền bằng số quy đổi
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// tổng tiền chưa thuế quy đổi
        /// </summary>
        public decimal TotalAmountWithoutVAT { get; set; }

        /// <summary>
        /// Tổng tiền thuế quy đổi
        /// </summary>
        public decimal? TotalVATAmount { get; set; }

        /// <summary>
        /// Tổng tiền CKTM quy đổi
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Tổng tiền hàng
        /// </summary>
        public decimal? TotalSaleAmountOC { get; set; }

        /// <summary>
        /// Tổng tiền hàng quy đổi
        /// </summary>
        public decimal? TotalSaleAmount { get; set; }
        public bool IsTicket { get; set; }

        #endregion "Các thông tin bổ sung"

		/// <summary>
		/// Ngày đến
		/// </summary>
		public DateTime? TrsArrival { get; set; }

		/// <summary>
		/// Ngày đi
		/// </summary>
		public DateTime? TrsDeparture { get; set; }

		/// <summary>
		/// Phòng số
		/// </summary>
		public string RoomNo { get; set; }

		/// <summary>
		/// Ngày đến
		/// </summary>
		public string CheckIn { get; set; }

		/// <summary>
		/// Ngày đi
		/// </summary>
		public string CheckOut { get; set; }
		/// <summary>
		/// Khi hóa đơn áp dụng thuế suất 8% của nghị định 43 thì ở phần master (OriginalInvoiceDetail) bổ sung thêm Field
		/// </summary>
		public bool? IsTaxReduction43 { get; set; }

		/// <summary>
		/// Trường mở rộng 1
		/// </summary>
		public string CustomField1 { get; set; }
		/// <summary>
		/// Trường mở rộng 2
		/// </summary>
		public string CustomField2 { get; set; }

		/// <summary>
		/// Trường mở rộng 3
		/// </summary>
		public string CustomField3 { get; set; }

		/// <summary>
		/// Trường mở rộng 4
		/// </summary>
		public string CustomField4 { get; set; }

		/// <summary>
		/// Trường mở rộng 5
		/// </summary>
		public string CustomField5 { get; set; }

		/// <summary>
		/// Trường mở rộng 6
		/// </summary>
		public string CustomField6 { get; set; }

		/// <summary>
		/// Trường mở rộng 7
		/// </summary>
		public string CustomField7 { get; set; }

		/// <summary>
		/// Trường mở rộng 8
		/// </summary>
		public string CustomField8 { get; set; }

		/// <summary>
		/// Trường mở rộng 9
		/// </summary>
		public string CustomField9 { get; set; }

		/// <summary>
		/// Trường mở rộng 10
		/// </summary>
		public string CustomField10 { get; set; }
		/// <summary>
		/// Có giảm trừ thuế không
		/// </summary>
		public bool? IsTaxReduction { get; set; }
		public decimal? ServiceFeeRate { get; set; }
		public decimal? ServiceAmount { get; set; }
		public bool IsHotelMultiVATRate { get; set; }
		public string BuyerIDNumber { get; set; }
		public string BuyerPassport { get; set; }
		public string BuyerBudgetCode { get; set; }
        public string SellerShopCode { get; set; }
        public string SellerShopName { get; set; }
	}

	/// <summary>
	/// Thông tin chi tiết vé gốc
	/// </summary>
	public class OriginalInvoiceDetail
    {
        /// <summary>
        /// Tính chất (1: HHDV; 2: khuyến mại; 3: chiết khẩu; 4: ghi chú/diễn giải)
        /// </summary>
        public int ItemType { get; set; }

        /// <summary>
        /// STT dòng mặt hàng (bắt đầu từ 1)
        /// </summary>
        public int LineNumber { get; set; }
        public int SortOrder { get; set; }

        /// <summary>
        /// Tên mặt hàng
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Đơn vị tính
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// Số lượng mặt hàng
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Đơn giá
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Tỷ lệ chiết khấu
        /// </summary>
        public decimal? DiscountRate { get; set; }

        /// <summary>
        /// Tiền chiết khấu
        /// </summary>
        public decimal? DiscountAmountOC { get; set; }

        /// <summary>
        /// Tiền chiết khấu quy đổi
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// Thành tiền
        /// </summary>
        public decimal? AmountOC { get; set; }

        /// <summary>
        /// Thành tiền quy đổi
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Thành tiền chưa thuế
        /// </summary>
        public decimal? AmountWithoutVATOC { get; set; }

        /// <summary>
        /// Thành tiền chưa thuế
        /// </summary>
        public decimal? AmountWithoutVAT { get; set; }

        /// <summary>
        /// Thành tiền sau thuế quy đổi
        /// </summary>
        public decimal? AmountAfterTax { get; set; }

        /// <summary>
        /// Tên loại thuế suất
        /// </summary>
        public string VATRateName { get; set; }

        /// <summary>
        /// Tiền thuế
        /// </summary>
        public decimal? VATAmountOC { get; set; }

        /// <summary>
        /// Tiền thuế quy đổi
        /// </summary>
        public decimal? VATAmount { get; set; }
        public decimal? ServiceFeeRate { get; set; }
        public decimal? ServiceAmount { get; set; }
        public decimal? ServiceAmountOC { get; set; }
        public string ItemCode { get; set; }
        public decimal? UnitPriceAfterExciseVAT { get; set; }
		public string CustomField2Detail { get; set; }
		public string CustomField1Detail { get; set; }
		public decimal? ExciseTaxRate { get; set; }
		public decimal? ExciseTaxAmountOC { get; set; }
		public decimal? ExciseTaxAmount { get; set; }
		public decimal? TaxReduction43Amount { get; set; }
		public decimal? TaxReduction43AmountOC { get; set; }
		public string CustomField3Detail { get; set; }
	}

    public class TaxRateInfo
    {
        /// <summary>
        /// Tên loại thuế suất
        /// </summary>
        public string VATRateName { get; set; }

        /// <summary>
        /// Tiền chưa thuế
        /// </summary>
        public decimal AmountWithoutVATOC { get; set; }

        /// <summary>
        /// Tiền thuế
        /// </summary>
        public decimal? VATAmountOC { get; set; }
    }
    public class FeeInfo
    {
        /// <summary>
        /// Tên loại phí
        /// </summary>
        public string FeeName { get; set; }

        /// <summary>
        /// Tiền phí
        /// </summary>
        public decimal? FeeAmountOC { get; set; }
    }
    /// <summary>
    /// Class chứa các tùy chọn cần dùng để hiển thị vé:
    /// Định dạng số, đồng tiền hạch toán
    /// </summary>
    public class OptionUserDefined
    {
        /// <summary>
        /// Đồng tiền hạch toán
        /// </summary>
        public string MainCurrency { get; set; }

		/// <summary>
		/// Định dạng đơn giá nguyên tệ
		/// </summary>
		public string UnitPriceOCDecimalDigits { get; set; }

		/// <summary>
		/// Định dạng đơn giá quy đổi
		/// </summary>
		public string UnitPriceDecimalDigits { get; set; }
		/// <summary>
		/// Định dạng số lượng
		/// </summary>
		public string QuantityDecimalDigits { get; set; }
		public string AmountDecimalDigits { get; set; }
		public string AmountOCDecimalDigits { get; set; }
		public string CoefficientDecimalDigits { get; set; }
		public string ExchangRateDecimalDigits { get; set; }

	}

	public class InvoiceDataDraft
	{
		public string RefID { get; set; }
        public int InvoiceType { get; set; } // lấy từ api lấy danh sách mẫu
        public int InvTemplateNo { get; set; } //  lấy từ api lấy danh sách mẫu
        public string InvSeries { get; set; } // lấy từ api lấy danh sách mẫu
        public string InvDate { get; set; } // ngày phát hành hoá đơn
		public string InvNo { get; set; }
        public int PublishStatus { get; set; } // cái này auto 0
        // trường này lấy từ mẫu
        public bool IsInheritFromOldTemplate { get; set; }
		// có phải là HĐ xăng dầu hay không
		public bool IsTemplatePetrol { get; set; }
        // thông tin người mua
        public string AccountObjectID { get; set; }
        public string AccountObjectTaxCode { get; set; }
        public string AccountObjectName { get; set; }
		public string AccountObjectCode { get; set; }
		public string AccountObjectAddress { get; set; }
		public string AccountObjectBankAccount { get; set; }
		public string AccountObjectBankName { get; set; }
		public string ContactName { get; set; }
		public string ReceiverEmail { get; set; }
		public string ReceiverName { get; set; }
		public string ReceiverMobile { get; set; }
		public int SendInvoiceStatus { get; set; }
		public int SendNumber { get; set; }
		public string PaymentMethod { get; set; } // phương thức thanh toán
		public string CurrencyCode { get; set; } // mã tiền
		public string CurrencyID { get; set; } // id tiền
		public decimal? ExchangeRate  { get; set; } // tỉ lệ chuyển đổi giữ nguyên tệ và quy đổi
	    public int TypeDiscount { get; set; } // loại chiết khấu 0 = Không chiết khấu 1 = Chiết khấu theo từng mặt hàng 2 = Chiết khấu theo tổng giá trị hóa đơn
		public int DiscountRate { get; set; } // tỷ lệ chiết khấu %
		public int ExchangeRateOperation { get; set; }
		// false 1 thuế xuất, true nhiều thuế xuất
		public bool IsMoreVATRate { get; set; }
		public decimal? VATRate { get; set; } // mức thuế GTGT , nếu là loại hoá đơn nhiều thuế xuất thì ở đây sẽ là mức thuế xuất cao nhất
        // tổng thành tiền chưa bao gồm thuế và chiết khấu
        public decimal TotalSaleAmountOC { get; set; } // 500000 + 25000 - 50000
		public decimal TotalSaleAmount { get; set; } // 500000 + 25000 - 50000
        // tổng chiết khấu (giảm giá)
        public decimal TotalDiscountAmountOC { get; set; } // 15000 + 750
		public decimal TotalDiscountAmount { get; set; } // 15000 + 750
        // tổng tiền chưa bao gồm thuế
        public decimal TotalAmountWithoutVATOC { get; set; } // 475000 - (15000 + 750)
		public decimal TotalAmountWithoutVAT { get; set; } // 475000 - (15000 + 750)
        // tổng tiền thuế
        public decimal TotalVATAmountOC { get; set; } // 48500 + 2425 - 5000
		public decimal TotalVATAmount { get; set; } // 48500 + 2425 - 5000
        // tổng thành tiền không thuế
        public decimal TotalAmountWithVATOC { get; set; } // nguyên tệ
		public decimal TotalAmountWithVAT { get; set; } // quy đổi
        // tổng thành tiền cuối cùng
        public decimal TotalAmountOC { get; set; } // 459250 + 45925
		public decimal TotalAmount { get; set; } // 459250 + 45925
		public int EInvoiceStatus { get; set; } // auto 1
        // thời gian tạo và sửa hoá đơn
        public string CreatedDate { get; set; }
		public string ModifiedDate { get; set; }
		public int SendToTaxStatus { get; set; }
		public string CompanyID { get; set; } // thay CompanyID từ api get token
		public string OrganizationUnitID { get; set; } // thay OrganizationUnitID từ api get token
		public string InvoiceTemplateID { get; set; } // lấy từ api lấy danh sách mẫu
		public string UserID { get; set; } // thay userid từ api get token
		public int BusinessArea { get; set; } // mẫu HĐ GTGT thường thì trường này = 0
		public int EntityState { get; set; } // tạo mới HĐ thì cái này auto 1
		public int EditVersion { get; set; } // tạo mới HĐ thì cái này auto 0
	}
}
