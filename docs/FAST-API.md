# FAST API

`site.partner: 8`, provider `FAST`. Đủ 13 action sống của baseline, kiểm thử offline cả REST MTT và SOAP GTGT. Chưa UAT với FAST thật.

Server cấu hình `Providers:Fast:AllowedHosts` và site JSON. MTT dùng `partnerUrl`, `username` chứa JSON `{clientC,proxyC,user,unitC}`, `password`; GTGT dùng `partnerUrl2`, `username2` chứa JSON cùng cấu trúc, `password2`. URL HTTPS có dấu `/` cuối. Không dùng hàm GetFastInfoToken cũ đã chết để đổi cột cấu hình.

PMS gửi HTTPS, `X-Api-Key`, `X-Site-Id`. Thao tác ghi cần `Idempotency-Key` ổn định. Chọn MTT khi ký hiệu có chữ `M` ở vị trí thứ ba từ cuối; các luồng khác dùng GTGT như dispatcher cũ. Draft chỉ có MTT. Không tự sửa dispatcher theo electronicInvoiceType.

| POST `/api/eInvoice/…` | Body | Hành vi |
|---|---|---|
| health-check | `{}` | Chỉ kiểm tra cấu hình, connected=false, connectionVerified=false, configurationValid=true; không gọi FAST |
| publish | CreateInvoiceRequest | Phát hành một hóa đơn |
| publish-batch | InvoiceBatchRequest | Phát hành lô đồng nhất ký hiệu, ID không trùng |
| draft | CreateInvoiceRequest | Nháp MTT |
| preview | AdjustOrReplaceInvoiceRequest | PDF xem trước; thao tác đọc theo baseline |
| adjust/increase, adjust/decrease, adjust/information, replace | AdjustOrReplaceInvoiceRequest | ID mới và originalInvoice bắt buộc |
| document, document/draft | DownloadPdfRequest | invoiceId, serial; document cần searchCode; nháp MTT dùng invoiceDate |
| void | VoidInvoiceRequest | invoiceId, serial, searchCode, reason; cả GTGT cũng dùng REST theo baseline |

Mapping giữ nguyên master/detail và thứ tự giá trị tương ứng, công thức tiền/thuế và TaxRate=8 → TaxAmount0. REST gửi UTF-8 `text/plain`, SOAP gửi UTF-8 `text/xml`; giữ tên SOAP `ExcuteCommand` và ký tự `þ` trong MD5 hash. GTGT gọi CheckKey → GetKey → business, token riêng cho một thao tác; không retry ghi. Timeout HTTP 100 giây, HTTPS/host allowlist, không redirect hoặc bypass TLS.

Response hóa đơn là InvoiceResult với invoiceId PMS giữ nguyên và transactionId/key NCC riêng. Batch kiểm tra ID trả về, số lượng và trùng ID; lỗi/timeout sau dispatch giữ pending để đối soát. PDF cần base64 hợp lệ; JSON lỗi không được trả như PDF.

[Postman](postman/eInvoice-FAST.postman_collection.json), [environment](postman/eInvoice-FAST.environment.template.json), [OpenAPI](openapi.misa.json). Chọn từng request và các ID riêng; không chạy mù toàn collection. 10 fixture pure mapping được capture từ source frozen độc lập, không tham chiếu provider assembly runtime.
