# Cyberbill — 11 action baseline

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

## Tạo và ký riêng theo quyết định ngày 13/10/2026

Q012/Q013 đã được người dùng chốt: giữ hai bước tạo → ký và paymentMethod là mã số 1–5.

| POST /api/eInvoice/… | Body | Ý nghĩa |
|---|---|---|
| create | CreateInvoiceRequest | GuiHoadonGoc, tạo unsigned; action CreateAndPublish lịch sử cũng trỏ route này |
| change | AdjustOrReplaceInvoiceRequest | GuiHoadonGoc theo hành vi CHANGE cũ |
| replace, adjust | AdjustOrReplaceInvoiceRequest | ID mới và originalInvoiceId; tạo hóa đơn thay thế/điều chỉnh chưa ký |
| publish-existing | PublishExistingInvoiceRequest | invoiceId + providerInvoiceCode; KyHoaDonHSM |
| delete | DeleteInvoiceRequest | invoiceId + providerInvoiceCode; XoaHoaDon |

Các thao tác này cần Idempotency-Key. Create/replace/adjust trả InvoiceResult gồm providerInvoiceCode/magiaodich và requiresSigning=true. PMS dùng mã trả về trong publish-existing; ký thành công trả requiresSigning=false và mã CQT nếu có. Không tự gọi ký sau create; /publish không có luồng create rồi ký. PaymentMethod tên như TM/CK trả 400 trước NCC; paymentMethodCode không thay cho lựa chọn mã số này.

7 fixture create/replace/adjust/sign/document/delete/void từ source frozen độc lập được đối chiếu ở mapping và API fake HTTP. Mapper giữ công thức tiền/thuế/default, invariant theo D021. Capabilities có đủ 11 action sống baseline. Chưa UAT với Cyberbill thật.
