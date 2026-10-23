# API EasyInvoice

`site.partner: 9`, response `provider: "EASY_INVOICE"`. Đã port **12 action nghiệp vụ** trong baseline, kiểm thử HTTP giả lập và 20 payload từ mapper frozen độc lập. Hai action Login/HealthCheck đã hoàn thiện theo lựa chọn người dùng ngày 13/10: kiểm tra cấu hình endpoint/credential/allowlist, HTTP 200 với connected=false, connectionVerified=false, configurationValid=true; không gọi NCC. Tổng 14 action baseline. Chưa UAT với EasyInvoice thật.

Import [collection Postman](postman/eInvoice-EASY_INVOICE.postman_collection.json) và [environment template](postman/eInvoice-EASY_INVOICE.environment.template.json), hoặc [OpenAPI chung](openapi.misa.json). Collection có 18 request, không chứa tài khoản/key thật.

## Cấu hình và xác thực

Server đọc `sites.Local.json` theo `X-Site-Id`: `partner: 9`, `partnerUrl` HTTPS có dấu `/` cuối, `username`, `password`. Host phải được khai báo tại `Providers:EasyInvoice:AllowedHosts` (mặc định rỗng). Credential chỉ lưu file ignored/secrets; không gửi trong tài liệu, Git hay chat. Không dùng admin cũ.

PMS dùng HTTPS + `X-Api-Key`, `X-Site-Id`. Thao tác ghi bắt buộc `Idempotency-Key` 8–128 ký tự ASCII chữ/số/`_`/`-`. Các API đọc không cần key idempotency.

Transport EasyInvoice POST JSON UTF-8, timeout 100 giây theo baseline; không login trước mỗi operation. Mỗi request có header NCC **Authentication**, giữ đúng cấu trúc `signature:nonce:timestamp:username:password`. Signature là base64 MD5 của `POST` + timestamp UTC epoch giây + nonce GUID dạng `N` chữ thường; không đổi thuật toán wire. Header không trả/log ra PMS, không dùng DefaultRequestHeaders/shared auth. HTTPS/TLS mặc định, không redirect/cookie/bypass.

## Action và body

| Route POST `/api/eInvoice/…` | Body canonical | Endpoint NCC | Ý nghĩa |
|---|---|---|---|
| `create` | CreateInvoiceRequest | api/publish/importInvoice | Tạo không ký, không đặt trước số |
| `draft` | CreateInvoiceRequest | api/publish/importInvoice | Cùng nghiệp vụ create; chọn một route |
| `publish` | CreateInvoiceRequest | api/publish/importAndPublishInvoice | Tạo và phát hành |
| `publish-existing` | PublishExistingInvoiceRequest | api/publish/issueInvoices | Phát hành nháp theo Ikey |
| `replace` | AdjustOrReplaceInvoiceRequest | api/business/replaceInvoice | Thay thế, invoiceId mới |
| `adjust/increase` | AdjustOrReplaceInvoiceRequest | api/business/adjustInvoice | XML Type=2 |
| `adjust/decrease` | AdjustOrReplaceInvoiceRequest | api/business/adjustInvoice | XML Type=3, giữ dấu tiền PMS |
| `adjust/information` | AdjustOrReplaceInvoiceRequest | api/business/adjustInvoice | XML Type=4 |
| `preview` | AdjustOrReplaceInvoiceRequest | api/publish/previewInvoice | PDF từ XML, không journal/claim |
| `document` | DownloadPdfRequest | api/publish/getInvoicePdf | PDF đã có, Option="0" |
| `delete` | DeleteInvoiceRequest | api/business/removeUnsignedInvoice | Xóa không ký |
| `void` | VoidInvoiceRequest | api/business/cancelInvoice | Hủy đã phát hành |

`GET /api/eInvoice/capabilities` trả đúng 12 action đang implement, không gọi NCC. Form-serials, batch, change, search, invoices/status, email, XML/USB/CQT và document/draft không có luồng sống trong baseline EasyInvoice, trả 501.

Hóa đơn `invoiceType: 0`. Create/draft/publish/replace/adjust/preview cần `invoice.invoiceId` ổn định, `serial`, `invoiceDate` dạng `dd/MM/yyyy`, ít nhất một detail. Không tự tạo GUID khi thiếu PMS ID. Dùng body chung như [mẫu JSON](examples/misa-publish.json), thay ID/ký hiệu/ngày và các giá trị theo sandbox EasyInvoice. Các key wire **Pattern, XmlData, Ikey, Ikeys, InvoiceType, Option** và element XML giữ nguyên.

**Ikey là invoiceId PMS**, không phải số hóa đơn hay GUID độc lập từ NCC. Các API đọc/xóa/hủy/ký nháp dùng invoiceId + serial. Nếu gửi providerInvoiceCode thì giá trị phải bằng invoiceId; không dùng field này thay ID. Adjust/replace cần `originalInvoiceId`, `originalInvoice.invoiceId` trùng nhau và khác invoiceId mới, kèm `originalInvoice.maCQT` của hóa đơn gốc. Nếu có originalProviderInvoiceCode, nó phải bằng originalInvoiceId. Mã NCC được trả dưới taxAuthorityCode trong InvoiceResult; PMS lưu lại vào originalInvoice.maCQT khi điều chỉnh/thay thế.

Ký nháp:

```json
{"invoiceId":"EASY-DRAFT-001","serial":"<ký hiệu sandbox>","invoiceType":0}
```

Thay thế/điều chỉnh: gửi đầy đủ invoice/details/taxSummaries/serviceSummaries/extraData cho hóa đơn mới, thêm:

```json
{
  "originalInvoiceId":"EASY-SYNTHETIC-001",
  "originalInvoice":{"invoiceId":"EASY-SYNTHETIC-001","maCQT":"<mã CQT gốc>"}
}
```

Preview: `invoice.previewType` null/1 là thường; 3 thay thế; 4 tăng; 5 giảm; 6 thông tin. Giá trị 2 (generic adjust của PMS cũ) vẫn đi nhánh thường theo baseline EasyInvoice; dùng 4/5/6 khi cần xem điều chỉnh. XML/type NCC lần lượt 0/1/2/3/4; preview Option="1", Ikey từ originalInvoice.invoiceId nếu có. Nhánh thay thế/điều chỉnh cần thông tin gốc như bảng trên.

## Response và xử lý trạng thái

NCC thành công khi HTTP 2xx và Status=2; Data.Invoices chấp nhận mảng hoặc chuỗi JSON chứa mảng. Một request phải trả đúng một Ikey trùng invoiceId, response sai/thiếu không coi là đã phát hành. Các route phát hành cần No là số dương; response `data` là mảng InvoiceResult, ví dụ:

```json
{
  "provider":"EASY_INVOICE",
  "siteId":"11111111-1111-1111-1111-111111111111",
  "data":[{"invoiceId":"EASY-SYNTHETIC-001","providerInvoiceNumber":"000123","transactionId":null,"searchCode":"SEARCH","taxAuthorityCode":"TAX","invoiceData":null,"isSuccess":true,"providerErrorCode":null,"providerMessage":null}],
  "requestId":"server-correlation-id"
}
```

Create/draft trả providerInvoiceNumber=null, kể cả response có trường No; import này chưa xác nhận phát hành. Journal completed chỉ xác nhận operation đã nhận kết quả hợp lệ, **create/draft completed không có nghĩa hóa đơn đã phát hành**. Void/delete trả `data: {"success":true}` khi Status=2, không echo raw Message từ NCC.

Document/preview chỉ thành công khi HTTP 2xx và bytes bắt đầu `%PDF-`; trả InvoiceDocumentResult `{content:"<base64 PDF>",format:"base64"}`. JSON lỗi/HTML/body rỗng không được base64 thành PDF thành công.

| HTTP | Code/ý nghĩa |
|---|---|
| 400 | INVALID_INVOICE_REQUEST, EASY_INVOICE_CREDENTIALS_REQUIRED |
| 409 | key/body khác, claim trùng hoặc attempt pending; có diagnostics/link journal |
| 422 | EASY_INVOICE_OPERATION_REJECTED, providerCode generic EASY_INVOICE_ITEM_REJECTED, providerMessage đã che bí mật + providerHttpStatus |
| 501 | Luồng không có trong baseline; health-check kiểm tra cấu hình theo D027 |
| 502 | EASY_INVOICE_INVALID_RESPONSE / EMPTY_RESPONSE / DOCUMENT_INVALID_RESPONSE / CONNECTION_ERROR |
| 503 | EASY_INVOICE_ENDPOINT_NOT_ALLOWED |
| 504 | EASY_INVOICE_TIMEOUT |

Lưu key/body trước gửi. Replay key + body y hệt của completed lấy lại kết quả mà không gọi NCC. Create và draft cùng khóa draft theo invoiceId, không chạy hai route cho cùng hóa đơn. Publish, publish-existing, adjust và replace cùng khóa publish theo invoiceId mới, ngăn phát hành lại qua đổi route/key. Delete/void có claim riêng. Đã dispatch mà lỗi/timeout/response không xác định thì vẫn pending; **không tự retry bằng key/ID mới**. GET operations/invoice-attempts chỉ đọc journal local, không truy vấn NCC hoặc mở khóa. Không có reconcile tự động.

## Mapping và kiểm chứng còn lại

Runtime gom phí/ExtraData/tổng thuế chung cho create/adjust/replace; frozen source giữ từng khối cũ để so độc lập. Không đổi công thức/dấu tiền/làm tròn: dòng phí phục vụ vẫn tạo từ serviceSummaries; metadata arrivalDate/departureDate/folioNo/bkNo/EmployeeName giữ baseline; tổng riêng chỉ 0/5/8/10, thuế suất khác chưa được thêm vào bảng tổng. ExchangeRate=0: tiền bằng chữ luồng thường dùng 1, điều chỉnh/thay thế vẫn dùng 0; fixture giữ khác biệt này, chưa thống nhất nghiệp vụ. Số invariant theo D021; ngày dd/MM/yyyy là ngày nghiệp vụ Việt Nam, không phụ thuộc giờ máy.

Trước UAT cần sandbox host/credential/ký hiệu đã được người dùng cấu hình; kiểm tra PDF, response Ikey/No/LookupCode/TaxAuthorityCode, preview từng loại, phát hành nháp, điều chỉnh/thay thế/hủy/xóa. Kiểm chứng schema thực và cách NCC báo từ chối trước khi đổi error-code mapping hay financial mapping. Không gọi production hoặc ghi NCC thật trong build/tests/capture.
