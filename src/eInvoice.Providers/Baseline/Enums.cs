namespace eInvoice.Providers.Baseline
{
    public enum PARTNER
    {
        VNPT = 1,
        MINVOICE = 2,
        SUN = 3,
        VIETTEL = 4,
        VNPT_V5 = 5,
        MISA = 6,
        CYBER_BILL = 7,
        FAST = 8,
        EASY_INVOICE = 9,
    }

    public enum INVOICE_ACTION
    {
        /// <summary>
        /// Xoa bo hoa don cho ky
        /// </summary>
        DELETE = -1,
        /// <summary>
        /// Login
        /// </summary>
        LOGIN = 0,
        /// <summary>
        /// Lấy thông tin mẫu hóa đơn, ký hiệu
        /// </summary>
        FORM_SERIAL = 1,
        /// <summary>
        /// Phát hành hóa đơn, chưa ký
        /// </summary>
        CREATED = 2,
        /// <summary>
        /// Sửa hóa đơn
        /// </summary>
        CHANGE = 3,
        /// <summary>
        /// Kí hóa đơn
        /// </summary>
        PUBLISH = 4,
        /// <summary>
        /// Phát hành hóa đơn đã ký
        /// </summary>
        CREATED_AND_PUBLISH = 5,
        /// <summary>
        /// Hủy hóa đơn
        /// </summary>
        VOID = 6,
        /// <summary>
        /// Điều chỉnh tăng
        /// </summary>
        ADJUST_INCREASE = 7,
        /// <summary>
        /// Điều chỉnh giảm
        /// </summary>
        ADJUST_DECREASE = 8,
        /// <summary>
        /// Điều chỉnh thông tin
        /// </summary>
        ADJUST_INFOR = 9,
        /// <summary>
        /// Thay thế
        /// </summary>
        REPLACE = 10,
        /// <summary>
        /// Tải file PDF
        /// </summary>
        DOWNLOAD_PDF = 11,
        /// <summary>
        /// In chuyển đổi
        /// </summary>
        CONVERT_FOR_STORE = 12,
        /// <summary>
        /// Lấy thông tin công ty theo mst
        /// </summary>
        GET_INFO_BY_TAXCODE = 14,
        /// <summary>
        /// Dieu chinh gia tri hoa don 78
        /// </summary>
        ADJUST = 15,
        /// <summary>
        /// Dieu chinh gia tri hoa don 78
        /// </summary>
        SENDEMAIL = 16,
        /// <summary>
        /// Phát hành hóa đơn theo lô
        /// </summary>
        CREATED_AND_PUBLISH_MULTI = 17,
        /// <summary>
        /// HĐ nháp
        /// </summary>
        DRAFT = 18,
        /// <summary>
        /// Xem trước hóa đơn
        /// </summary>
        PREVIEW = 19,
        /// <summary>
        /// Kiểm tra kết nối
        /// </summary>
        HEALTH_CHECK = 20,
        /// <summary>
        /// Lấy thông tin HĐ theo Id
        /// </summary>
        SEARCH_BY_ID = 21,
        /// <summary>
        /// Lấy danh sách hóa đơn
        /// </summary>
        GET_INVOICE = 22,
        /// <summary>
        /// Tải file PDF hóa đơn nháp
        /// </summary>
        DOWNLOAD_PDF_INV_DRAFT = 23,
        /// <summary>
        /// Lấy Serial Cert để gửi CQT khi tạo hóa đơn MTT
        /// </summary>
        GET_SERIAL_CERT_INFO = 24,
        /// <summary>
        /// Gửi Invoice sang CQT
        /// </summary>
		SEND_INVOICE_CQT = 25,
        /// <summary>
        /// Lấy trạng thái hóa đơn
        /// </summary>
        GET_STATUS_INVOICE = 26,
        /// <summary>
        /// Lấy mã XML hash của hóa đơn
        /// </summary>
		GET_XML_INVOICE_HASH = 27,
		/// <summary>
		/// Gửi Invoice sang CQT từ dữ liệu lấy ở USB Token
		/// </summary>
		SEND_INVOICE_CQT_USB_TOKEN = 28,
		/// <summary>
		/// Lấy thông tin XML hóa đơn USB Token
		/// </summary>
		GET_XML_INVOICE = 29,
		/// <summary>
		/// Phát hành hóa đơn đã được ký ở USB Token
		/// </summary>
		CREATE_AND_PUBLISH_INVOICE_XML = 30,
        /// <summary>
        /// Chỉnh sửa hóa đơn nháp
        /// </summary>
		EDIT_DRAFT = 31,
	}

	public enum CODE_VAT_INVOICE_CYBER_BILL
	{
		/// <summary>
		/// Không kế khai
		/// </summary>
		TAX_NON_DECLARATION = -2,
		/// <summary>
		/// Không thuế
		/// </summary>
		TAX_FREE = -1,
		/// <summary>
		/// Thuế 0%
		/// </summary>
		TAX_0_PERCENT = 0,
		/// <summary>
		/// Thuế 5%
		/// </summary>
		TAX_5_PERCENT = 5,
		/// <summary>
		/// Thuế 10%
		/// </summary>
		TAX_10_PERCENT = 10,
		/// <summary>
		/// Thuế 5% x 70%
		/// </summary>
		TAX_5_70_PERCENT = 35,
		/// <summary>
		/// Thuế 10% x 70%
		/// </summary>
		TAX_10_70_PERCENT = 7,
	}

    public enum INVOICE_TYPE
    {
        /// <summary>
        /// Hóa đơn điện tử tạo từ Menu Einvoice - Hóa đơn điện tử dùng HSM
        /// </summary>
        INVOICE_NORMAL = 0,
        /// <summary>
        /// Hóa đơn điện tử tạo từ Máy tính tiền không  kýs
        /// </summary>
        INVOICE_MTT = 1,
		/// <summary>
		/// Hóa đơn điện tử tạo từ Menu Einvoice - Hóa đơn điện tử dùng chữ ký USB Token
		/// </summary>
		INVOICE_NORMAL_USB_TOKEN = 2,
		/// <summary>
		/// Hóa đơn điện tử tạo từ Máy tính tiền dùng chữ ký USB Token
		/// </summary>
		INVOICE_MTT_USB_TOKEN = 3,
		/// <summary>
		/// Hóa đơn điện tử tạo từ Máy tính tiền + ký HSM
		/// </summary>
		INVOICE_MTT_HSM = 4,
	}

    public enum INV_TYPE
    {
        /// <summary>
        /// Hóa đơn điện tử thường
        /// </summary>
        INV_NORMAL = 0,
        /// <summary>
        /// Vé điện tử
        /// </summary>
        INV_TICKET = 1,
        /// <summary>
        /// Biên lai điện tử
        /// </summary>
        INV_RECEIPT = 2
    }

	public enum PREVIEW_TYPE
    {
        /// <summary>
        /// Tạo mới
        /// </summary>
	    CREATE = 1,
        /// <summary>
        /// Điều chỉnh
        /// </summary>
	    ADJUST = 2,
		/// <summary>
		/// Thay thế
		/// </summary>
		REPLACE = 3,
		/// <summary>
		/// Điều chỉnh tăng
		/// </summary>
		ADJUST_INCREASE = 4,
		/// <summary>
		/// Điều chỉnh giảm
		/// </summary>
		ADJUST_DECREASE = 5,
		/// <summary>
		/// Điều chỉnh thông tin
        ///
		ADJUST_INFO = 6
    }

    public enum EASY_INVOICE_ADJUST_TYPE {
		/// <summary>
		/// Loại điều chỉnh tăng
		/// </summary>
		INCREASE = 2,
		/// <summary>
		/// Loại điều chỉnh giảm
		/// </summary>
		DECREASE = 3,
		/// <summary>
		/// Loai điều chỉnh thông tin
		/// </summary>
		INFO = 4
	}
    public enum EASY_INVOICE_TYPE
    {
		/// <summary>
		/// Hóa đơn tạo mới thông thường(Mặc định)
		/// </summary>
		NORMAL = 0,
		/// <summary>
		/// Hóa đơn thay thế
		/// </summary>
		REPLACE = 1,
		/// <summary>
		/// Hóa đơn điều chỉnh tăng
		/// </summary>
		ADJUST_INCREASE = 2,
		/// <summary>
		/// Hóa đơn điều chỉnh giảm
        /// </summary>
		ADJUST_DECREASE = 3,
		/// <summary>
		/// Hóa đơn điều chỉnh thông tin
		/// </summary>
		ADJUST_INFO = 4
	}
}
