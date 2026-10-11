# Cyberbill — tiến độ và hợp đồng hiện tại

Provider `CYBER_BILL`, `site.partner = 7`. Code lấy từ baseline `66a9c742d0f6fb692d339076e6cd82780e3476be`. Chưa UAT hoặc gọi Cyberbill thật.

## API đang dùng được qua kiểm thử offline

| Route POST `/api/eInvoice/…` | Body | Ý nghĩa |
|---|---|---|
| health-check | `{}` | Đăng nhập mới, không trả token |
| document | `invoiceType: 0`, `invoiceId`, `providerInvoiceCode` | PDF base64; typeFileDownload bỏ qua hoặc `pdf` |
| void | `invoiceType: 0`, `invoiceId`, `formNo`, `serial`, `invoiceDate` | Hủy; ngày `dd/MM/yyyy` |

Header HTTPS, `X-Api-Key`, `X-Site-Id`; void cần `Idempotency-Key` ổn định 8–128 ký tự. Site lấy từ JSON server như MISA/Viettel, credential không đặt trong tài liệu. `Providers:Cyberbill:AllowedHosts` mặc định rỗng; cấu hình đúng hostname HTTPS vào file local khi thử sandbox. Không tự thêm host/credential.

Đăng nhập JSON gửi nguyên key `username`, `password`, `doanhnghiep_mst`; token `result.access_token` được dùng riêng trong header Bearer của thao tác. Không cache/header/token static chung giữa khách sạn. Giữ nguyên endpoint và key phía Cyberbill. Transport timeout 100 giây, không redirect/bypass TLS/retry ghi.

Thành công NCC cần `success: true` và `result.maketqua: "01"`. Document cần `result.base64pdf` hợp lệ. Void trả `data: {"success":true}`; không suy ra số hóa đơn từ response hủy. Lỗi HTTP/business dùng JSON ProblemDetails, message lấy từ `motaketqua`/`error.details` sau che credential/token; không trả raw body. Mã NCC chưa có danh mục kiểm chứng, dùng `CYBER_BILL_ITEM_REJECTED`.

Tra cứu `GET /api/eInvoice/operations/void/{idempotencyKey}` đọc journal local, không gọi NCC. Completed replay cùng key/body; đã gửi mà lỗi giữ pending, cần đối soát. Không dùng key mới để vượt khóa. Invoice-attempts phase publish không phải tra cứu claim hủy.

## Phần đang chờ quyết định

Q012: dispatcher cũ CREATED/CHANGE/CREATED_AND_PUBLISH chỉ tạo; PUBLISH ký HSM hóa đơn có sẵn. Helper tạo–phát hành trực tiếp có endpoint rỗng, không được gọi. Chưa expose create/publish-existing/publish/adjust/replace/delete cho tới khi chốt luồng API.

Q013: mapper cũ parse `paymentMethod` thành số, khác tên thanh toán phía MISA/Viettel. Chưa chuyển sang `paymentMethodCode` khi chưa có quyết định; mapping baseline đang được giữ để đối chiếu.

7 fixture create/replace/adjust/sign/document/delete/void đã capture bằng công cụ frozen độc lập, không tham chiếu assembly provider runtime. Mapper giữ công thức tiền/thuế/default; scope invariant theo D021. Có fixture của một thao tác không đồng nghĩa API đó đã hoàn thành. Capabilities chỉ báo 4 action Login/HealthCheck/DownloadPdf/Void (3 route). Các thao tác còn lại trả 501 trước khi gọi Cyberbill.
