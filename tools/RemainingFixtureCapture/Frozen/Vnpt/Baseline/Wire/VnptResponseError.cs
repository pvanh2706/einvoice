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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eInvoice.Providers.Vnpt.Baseline
{
    internal static class VnptResponseError
    {
        public static Dictionary<string, Dictionary<string, string>> ListResponseError = new Dictionary<string, Dictionary<string, string>>
        {
            {
                VnptConst.FORM_SERIAL , new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:5", "Có lỗi xảy ra" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy company tương ứng cho user" },
                    {"ERR:20", "Không tìm thấy thông báo phát hành(mẫu hóa đơn)" },
                }
            },
            {
                VnptConst.DRAFT_INVOICE, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:3", "Dữ liệu xml đầu vào không đúng quy định" },
                    {"ERR:5", "Không phát hành được hóa đơn" },
                    {"ERR:6", "Không đủ số lượng hóa đơn cho lô thêm mới" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy company tương ứng cho user" },
                    {"ERR:10", "Lô có số hóa đơn vượt quá max cho phép" },
                    {"ERR:13", "Danh sách hóa đơn tồn tại hóa đơn trùng Fkey" },
                    {"ERR:20", "Pattern và serial không phù hợp, hoặc không tồn tại hóa đơn đã đăng kí có sử dụng Pattern và serial truyền vào" },
                    {"ERR:22", "Trùng số hóa đơn" }
                }
            },
            {
                VnptConst.SIGN_INVOICE, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:5", "Không phát hành được hóa đơn" },
                    {"ERR:6", "Danh sách Fkey không tồn tại" },
                    {"ERR:10", "Danh sách Fkey truyền vào vượt quá 200 fkey" },
                    {"ERR:15", "Danh sách Fkey đã phát hành" },
                    {"ERR:20", "Pattern và serial không phù hợp, hoặc không tồn tại hóa đơn đã đăng kí có sử dụng Pattern và serial truyền vào" },
                    {"ERR:29", "Lỗi chứng thư hết hạn" },
                    {"ERR:30", "Danh sách hóa đơn tồn tại ngày hóa đơn nhỏ hơn ngày hóa đơn đã phát hành" },
                }
            },
            {
                VnptConst.CREATE_AND_SIGN_INVOICE, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:2", "Pattern hoặc serial truyền vào rỗng" },
                    {"ERR:3", "Dữ liệu xml đầu vào không đúng quy định" },
                    {"ERR:4", "Không lấy được thông tin công ty (currentCompany null)" },
                    {"ERR:5", "Không phát hành được hóa đơn" },
                    {"ERR:6", "Không đủ số hóa đơn cho lô phát hành" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy company tương ứng cho user" },
                    {"ERR:10", "Lô có số hóa đơn vượt quá max cho phép" },
                    {"ERR:11", "Pattern hoặc serial không đúng định dạng" },
                    {"ERR:13", "Lỗi trùng fkey" },
                    {"ERR:15", "Ngày lập truyền vào lớn hơn ngày hiện tại hoặc XML không đúng định dạng (hóa đơn ngoại tệ không truyền tỷ giá)" },
                    {"ERR:20", "Pattern và serial không phù hợp, hoặc không tồn tại hóa đơn đã đăng kí có sử dụng Pattern và serial truyền vào" },
                    {"ERR:21", "Lỗi trùng số hóa đơn" },
                    {"ERR:22", "Thông tin người bán vượt maxlength" },
                    {"ERR:23", "Mã CQT rỗng" },
                    {"ERR:29", "Lỗi chứng thư hết hạn"},
                    {"ERR:30", "Danh sách hóa đơn tồn tại ngày hóa đơn nhỏ hơn ngày hóa đơn đã phát hành" }
                }
            },
            {
                VnptConst.ADJUST_INVOICE, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:2", "Hóa đơn cần điều chỉnh không tồn tại" },
                    {"ERR:3", "Dữ liệu xml đầu vào không đúng quy định" },
                    {"ERR:5", "Không phát hành được hóa đơn" },
                    {"ERR:6", "Dải hóa đơn cũ đã hết" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy company tương ứng cho user" },
                    {"ERR:8", "Hóa đơn cần điều chỉnh đã bị thay thế. Không thể điều chỉnh được nữa" },
                    {"ERR:9", "Trạng thái hóa đơn không được điều chỉnh" },
                    {"ERR:13", "Lỗi trùng fkey" },
                    {"ERR:14", "Lỗi trong quá trình thực hiện cấp số hóa đơn" },
                    {"ERR:15", "Lỗi khi thực hiện Deserialize chuỗi hóa đơn đầu vào" },
                    {"ERR:19", "Pattern truyền vào không giống với hóa đơn cần điều chỉnh" },
                    {"ERR:20", "Dải hóa đơn hết, User/Account không có quyền với Serial/Pattern và serial không phù hợp" },
                    {"ERR:29", "Lỗi chứng thư hết hạn" },
                    {"ERR:30", "Danh sách hóa đơn tồn tại ngày hóa đơn nhỏ hơn ngày hóa đơn đã phát hành" },
                }
            },
            {
                VnptConst.REPLACE_INVOICE, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:2", "Không tồn tại hóa đơn cần thay thế" },
                    {"ERR:3", "Dữ liệu xml đầu vào không đúng quy định" },
                    {"ERR:5", "Có lỗi trong quá trình thay thế hóa đơn" },
                    {"ERR:6", "Dải hóa đơn cũ đã hết" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy company tương ứng cho user" },
                    {"ERR:8", "Hóa đơn đã được thay thế rồi. Không thể thay thế nữa" },
                    {"ERR:13", "Lỗi trùng fkey" },
                    {"ERR:14", "Lỗi trong quá trình thực hiện cấp số hóa đơn" },
                    {"ERR:15", "Lỗi khi thực hiện Deserialize chuỗi hóa đơn đầu vào" },
                    {"ERR:19", "Trạng thái hóa đơn ko được thay thế" },
                    {"ERR:20", "Pattern và serial không phù hợp" },
                    {"ERR:29", "Lỗi chứng thư hết hạn" },
                    {"ERR:30", "Danh sách hóa đơn tồn tại ngày hóa đơn nhỏ hơn ngày hóa đơn đã phát hành" },
                }
            },
            {
                VnptConst.DOWNLOAD_PDF, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:2", "Chuỗi token không chính xác" },
                    {"ERR:4", "Không tìm thấy mẫu số tương ứng" },
                    {"ERR:5", "Có lỗi xảy ra" },
                    {"ERR:6", "Không tìm thấy hóa đơn (Chưa được phân quyền Serial hoặc hóa đơn không tồn tại)" },
                    {"ERR:7", "Không tìm thấy công ty" },
                    {"ERR:11", "Hóa đơn chưa thanh toán, đã bị hủy, bị thay thế, không được phép view" },
                    {"ERR:12", "Hoá đơn có mã chưa được thuế chấp nhận" },
                    {"ERR:13", "Hoá đơn không mã chưa được thuế chấp nhận" },
                }
            },
            {
                VnptConst.VOID, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền" },
                    {"ERR:2", "Không tìm thấy hóa đơn" },
                    {"ERR:6", "Lỗi không xác định" },
                    {"ERR:7", "Không tìm thấy công ty hoặc tài khoản không tồn tại" },
                    {"ERR:8", "Hóa đơn đã bị điều chỉnh/hủy/hóa đơn mới tạo không thể hủy được" },
                    {"ERR:9", "Hóa đơn đã thanh toán, không cho phép hủy" },
                    {"ERR:20", "Dải hóa đơn hết, User/Account không có quyền với Serial/Pattern và serial không phù hợp" },
                }
            },
            {
                VnptConst.DOWNLOAD_PDF_INV_DRAFT, new Dictionary<string, string>
                {
                    {"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền ServiceRole" },
                    {"ERR:2", "Fkey truyền lên rỗng" },
                    {"ERR:4", "Không tìm thấy dải thông báo phát hành" },
                    {"ERR:5", "Có lỗi xảy ra" },
                    {"ERR:6", "Không tìm thấy hóa đơn (Chưa được phân quyền Serial hoặc hóa đơn không tồn tại)" },
                    {"ERR:7", "User name không phù hợp, không tìm thấy thông tin công ty tương ứng cho user." },
                    {"ERR:12", "Hoá đơn có mã chưa được thuế chấp nhận" },
                    {"ERR:13", "Hoá đơn không mã chưa được thuế chấp nhận" },
                }
            },
			{
				VnptConst.GET_STATUS_INVOICE, new Dictionary<string, string>
				{
					{"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền thêm mới hóa đơn" },
					{"ERR:2", "Không tìm thấy hóa đơn tương ứng." },
					{"ERR:10", "Vượt quá số lượng 100 hóa đơn cần lấy" },
					{"ERR:20", "Không lấy được thông tin người dùng" },
					{"ERR:5", "Lỗi không xác định, không lấy được dữ liệu hóa đơn cấp mã theo dữ liệu truyền vào" },
				}
			},
			{
				VnptConst.SEND_INVOICE_CQT, new Dictionary<string, string>
				{
					{"ERR:1", "Tài khoản đăng nhập sai hoặc không có quyền"},
                    {"ERR:3", "Dữ liệu đầu vào không đúng quy định"},
                    {"ERR:4", "Dữ liệu ký không hợp lệ"},
                    {"ERR:5", "Có lỗi xảy ra"},
                    {"ERR:10", "Lô có số hóa đơn vượt quá số lượng tối đa cho phép (500)"},
                    {"ERR:11", "Toàn bộ lô hóa đơn đã gửi thuế hoặc không tìm thấy"},
                    {"ERR:12", "Thông điệp gửi thuế qua dung lượng 1MB"},
                    {"ERR:20", "Thông tin pattern hoặc serial không hợp lệ"},
                    {"ERR:21", "User name không phù hợp, không tìm thấy thông tin công ty tương ứng cho user."},
                    {"ERR:22", "Công ty chưa đăng ký chứng thư số"},
                    {"ERR:24", "Chứng thư truyền lên không đúng với chứng thư đăng ký trong hệ thống"},
                    {"ERR:26", "Chứng thư số hết hạn"},
                    {"ERR:27", "Chứng thư chưa đến thời điểm sử dụng"},
                    {"ERR:28", "Chưa có thông tin chứng thư trong hệ thống"},
                    {"ERR:35", "Thiếu thông tin pattern và serial"},
                    {"ERR:51", "Chứng thư số đã bị thu hồi"},
				}
			},
		};
    }
}
