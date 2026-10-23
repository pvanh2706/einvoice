# SUN API

Adapter `SUN`, `site.partner = 3`, đủ **10 action sống baseline** qua 10 POST route. Đã kiểm thử offline bằng fixture/HTTP giả lập; chưa UAT SUN thật hoặc xác nhận production.

## Cấu hình

Cấu hình JSON server như MISA: site thuộc quyền API key và khớp `X-Site-Id`. SUN dùng `partnerUrl2` cho login và các API JSON; `partnerUrl` riêng cho hủy form-urlencoded. Cả hai cần HTTPS, slash cuối và hostname được khai báo tại `Providers:Sun:AllowedHosts` trong appsettings.Local.json. File thật ignored; không ghi credential vào tài liệu/Git. Mặc định allowlist rỗng.

Login `api/signin` gửi `username`, `password`; token chỉ trong header Bearer của thao tác, không trả về PMS. Mỗi thao tác login mới, không có token/header static dùng chung tenant. Timeout mỗi HTTP call 100 giây; không redirect hoặc bypass TLS.

## Route và dữ liệu

Mọi route dưới `/api/eInvoice`, header HTTPS + `X-Api-Key`, `X-Site-Id`. Các thao tác ghi cần `Idempotency-Key` ổn định 8–128 ký tự.

| POST route | Body | Supplier endpoint | Ghi |
|---|---|---|---|
| health-check | `{}` | api/signin | Không |
| publish | CreateInvoiceRequest | api/savsigninv | Có |
| draft | CreateInvoiceRequest | api/invtmp | Có |
| preview | AdjustOrReplaceInvoiceRequest, `invoice.searchCode` | api/seek/searchpdf, invtmp=1 | Không |
| document | DownloadPdfRequest, `searchCode` | api/seek/searchpdf, invtmp=0 | Không |
| document/draft | DownloadPdfRequest, `searchCode` | api/seek/searchpdf, invtmp=1 | Không |
| void | VoidInvoiceRequest, `sid`, `reason` | api/cancelHD | Có |
| replace | AdjustOrReplaceInvoiceRequest | api/apiReplace | Có |
| adjust/increase | AdjustOrReplaceInvoiceRequest | api/adjniss, AdjType=2 | Có |
| adjust/decrease | AdjustOrReplaceInvoiceRequest | api/adjniss, AdjType=1 | Có |

`invoiceType` phải là 0. Publish/draft/adjust/replace cần invoiceId, formNo, serial, invoiceDate `dd/MM/yyyy` và ít nhất một detail. Giữ công thức/default và dấu tiền của baseline; culture số invariant, ngày rdt theo UTC+7 (D021). SUN không có batch/status/FormSerial trong dispatcher baseline; những API đó trả 501.

SUN baseline tạo `sid = invoiceId.Replace("-", "").Substring(0,32)`. Dùng một GUID ổn định cho invoiceId; mã ngắn bị từ chối 400 trước NCC. Không sinh ID khác sau timeout. Gateway thêm claim theo sid bên cạnh invoiceId, chặn cả hai ID PMS khác nhau nhưng trùng 32 ký tự đầu. Chưa tự thay thuật toán sid phía SUN.

Replace/adjust cần **invoiceId mới**, `originalProviderInvoiceNumber`, và `invoice.sid` của hóa đơn gốc. Baseline tạo `adj.seq` từ **invoice.formNo + invoice.serial + originalProviderInvoiceNumber**, giữ nguyên; không tự đổi sang OriginalInvoice. `invoice.searchCode` chuyển sang adj.des. Nhánh replace giữ rea null và invType/adjType mặc định 0 như baseline; adjust tăng/giảm giữ reason/multiplier tương ứng. Cần UAT những giá trị này trước cutover.

## Kết quả và xử lý lỗi

Response gateway có `provider: "SUN"`, siteId, requestId, data. Phát hành/thay thế/điều chỉnh trả mảng InvoiceResult với providerInvoiceNumber từ `success[0].seq`, searchCode từ `sec`, **transactionId là sid mới**. Lưu sid này để hủy hoặc gửi `invoice.sid` khi điều chỉnh/thay thế tiếp theo. Draft trả searchCode và transactionId, không trả số phát hành; lưu nháp không có nghĩa hóa đơn đã phát hành.

PDF trả `data.content` base64 và `data.format: "base64"`; bỏ prefix data URI NCC nếu có, từ chối nội dung thiếu/base64 sai. Preview SUN tải PDF bản nháp có sẵn theo searchCode, không dựng preview trực tiếp từ invoice body. Hủy trả `data: {"success":true}`.

NCC cần summary.error=0 và success array; các thao tác tạo/phát hành cần đúng một kết quả, có searchCode và số phát hành khi áp dụng. Nếu NCC trả sid thì phải khớp request. Lỗi business 422, response sai/thiếu 502, timeout 504, cấu hình chưa đủ 400/503; luôn JSON lỗi. ProviderMessage lấy từ summary.des_err/error, giải mã HTML entities và che credential/token; không trả raw body. Code supplier chưa có danh mục xác minh dùng SUN_ITEM_REJECTED.

Giữ key/body ban đầu để replay completed. Sau dispatch mà lỗi/timeout vẫn pending và khóa, cần đối soát SUN; không tự retry/reset claim. Tra cứu journal `GET /operations/{operation}/{key}` (ví dụ operation `adjust-increase`) hoặc `/invoice-attempts?invoiceId=…&phase=publish` không gọi SUN. Gateway chưa có cơ chế reconcile tự động. Draft là tạo nháp một lần theo ID/key; chưa có API revision để sửa lại sau completed.

## Postman và kiểm chứng

Import [collection SUN](postman/eInvoice-SUN.postman_collection.json) và [environment template](postman/eInvoice-SUN.environment.template.json), điền key còn hạn, SiteId cấu hình SUN, mẫu/ký hiệu sandbox, ngày, số/searchCode/sid đã trả về. Không dùng site MISA hiện tại thay cho SUN. Collection không có script tự phát hành/retry.

10 payload fixture độc lập từ source pinned, dùng tool SunFixtureCapture không tham chiếu assembly Providers. 29 tests mới: payload/culture/ngày UTC+7, routing hai URL, token tenant isolation, PDF, thiếu/sai kết quả, timeout, replay/claim sid, endpoint guard và capability đủ baseline. Test không liên hệ SUN thật. Không capture lại expected từ runtime để làm test qua.
