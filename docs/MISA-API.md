# Bộ API MISA

Code bao phủ **19 action sống** trong dispatcher baseline `66a9c74`, thông qua 15 route POST mới.
Một route có thể thay nhiều action cũ nếu mapping giống nhau. Đã kiểm thử offline; sandbox/UAT chưa làm.

Base `https://localhost:7150/api/eInvoice`; partner MISA = 6. API key, HTTPS và site scope
giữ quy tắc trong API-CONTRACT. JSON input strict: camelCase, không nhận key cũ/không biết.

## Endpoint

| POST route | Body | Phạm vi | Idempotency-Key |
|---|---|---|---|
| health-check | ProviderHealthCheckRequest | Login/HealthCheck v3 thật, bypass cache; không trả token | Không |
| form-serials | GetFormSerialsRequest | Hóa đơn 0, năm UTC+7 | Không |
| publish | CreateInvoiceRequest | Hóa đơn HSM/MTT/MTT HSM theo nhánh baseline; vé; biên lai | Có |
| publish-batch | InvoiceBatchRequest | Batch cùng site/type/mode/form/serial, ID khác nhau | Có |
| preview | AdjustOrReplaceInvoiceRequest | Hóa đơn 0; previewType 2/3 cần originalInvoice | Không |
| adjust | AdjustOrReplaceInvoiceRequest | Mapping chung của Adjust/Increase/Decrease/Information baseline | Có |
| replace | AdjustOrReplaceInvoiceRequest | ReferenceType 1; hóa đơn 0 | Có |
| document | DownloadPdfRequest | Hóa đơn/vé/biên lai; content base64 | Không |
| void | VoidInvoiceRequest | Hóa đơn/vé/biên lai, cần reason | Có |
| status | SendToTaxAuthorityRequest | Tra cứu SendTaxStatus theo invoiceIds; hóa đơn 0 | Không |
| tax/send-pending | SendPendingTaxAuthorityRequest | HSM gửi hàng đợi MISA theo site/AppID; **không có bộ lọc invoiceIds** | Có |
| xml/create | CreateInvoiceRequest | Chuẩn bị XML hóa đơn 0 cho chuỗi USB | Có |
| xml/hash | GetXmlHashRequest | MessageCode/MessageHash theo certificate public Base64 | Không |
| tax/send-signed | SendSignedTaxAuthorityRequest | Gửi MessageCode + SignatureValue đã ký bên ngoài | Có |
| xml/publish | PublishSignedXmlRequest | Gửi XML đã ký + invoiceId/searchCode | Có |

- `GET /api/eInvoice/misa/capabilities`: danh sách action/route đã implement, cần key/site; không gọi MISA.
- `GET /api/eInvoice/operations/{operation}/{idempotencyKey}`: đọc journal của site được cấp quyền, không gọi MISA.
  Slug operation dùng dấu `-` thay `/`, ví dụ `xml-create`, `xml-publish`, `tax-send-signed`, `tax-send-pending`.
- `GET /api/eInvoice/invoice-attempts?invoiceId=...&phase=publish`: tra cứu claim của invoice trong site được cấp quyền,
  không cần biết key gốc và không gọi MISA. Phase chỉ `publish` hoặc `xml-create` (mặc định publish).
- `GET /openapi/misa.json`: OpenAPI public chứa schema/example metadata, không chứa cấu hình/key/token.
- `GET /health/live`: liveness.

Draft/Create/SearchById/DownloadDraftPdf/SendEmail riêng/CertificateInfo không được thêm vào MISA
vì không có nhánh sống tương ứng. IsSendEmail trong payload phát hành vẫn theo baseline CustomerEmail.

## Input và response

Create cần invoice, details không rỗng, invoiceId/formNo/serial/date/currencyCode, exchangeRate và
beforeTaxAmount/taxAmount/afterTaxAmount. Ngày `dd/MM/yyyy`, precision 0..28; hóa đơn 0 phải có
electronicInvoiceType 0..4. Chi tiết cần quantity/price; biên lai cần invoice.taxRate.
Không tự suy ra/đổi công thức tiền; tiền dùng decimal, rounding AwayFromZero và các khác biệt single/batch
được đối chiếu fixture. Những giá trị thuế âm/sentinel có trong baseline được giữ.

Body cấu hình site tùy chọn như health-check. Batch dùng cấu hình cấp wrapper; cấu hình từng item nếu có
phải cùng site, không được đổi khách sạn. Không chọn credential/extra mapping từ item khác.

Response chuẩn:

```json
{
  "provider":"MISA",
  "siteId":"11111111-1111-1111-1111-111111111111",
  "data":[{
    "invoiceId":"INV-001",
    "providerInvoiceNumber":"000001",
    "transactionId":"provider-transaction-id",
    "searchCode":"provider-transaction-id",
    "taxAuthorityCode":"provider-tax-code",
    "invoiceData":null,
    "isSuccess":true,
    "providerErrorCode":null
  }],
  "requestId":"server-correlation-id"
}
```

Health-check/form-serials giữ response đã công bố (connected/forms), không đổi sang data.
Document/preview: `data = {content, format}` với format base64/url. Status: mảng invoiceId/taxAuthorityStatus;
hash: mảng messageCode/messageHash. Void: `{success:true}`. CQT: `{providerData: string|null}` giữ Data thành công
của provider vì baseline trả dữ liệu này cho PMS. Không trả envelope/raw body lỗi hoặc token.

Batch 200 nghĩa API đã xử lý response batch, **không có nghĩa mọi hóa đơn đều thành công**: kiểm tra isSuccess
từng item và nối bằng invoiceId, không dùng thứ tự. Unknown item code thành MISA_ITEM_REJECTED; chỉ 66 symbolic
code allowlist từ baseline được trả nguyên. Single item bị từ chối trả 422 MISA_INVOICE_REJECTED. Receipt batch
InvalidDuplicateRefID chỉ được coi thành công khi response còn có số hóa đơn để PMS cập nhật bù.

Status chỉ ánh xạ SendTaxStatus, không tự tạo enum mới. 200 phát hành/chuẩn bị XML không chứng minh CQT đã tiếp nhận;
PMS phải theo dõi status/hoàn tất các bước ký tương ứng.

## Lỗi và timeout

JSON Problem Details có code/requestId; providerCode chỉ có khi mã supplier đã qua allowlist.
Theo yêu cầu chẩn đoán 2026-10-09, business error mới có thêm `errorSource`, `providerMessage`, `providerHttpStatus`,
`attempt` và `attemptStatusUrl` khi có dữ liệu tương ứng. Message lấy từ trường lỗi DescriptionErrorCode/ErrorMessage/Message
của envelope, hoặc DescriptionErrorCode của item hóa đơn 0; che username/password/token đã biết, bearer và cặp key bí mật,
loại structured JSON/XML/HTML, control characters và giới hạn độ dài. Không trả raw envelope, Data lỗi, header/token hoặc request.
Batch item lỗi có thêm providerMessage đã lọc; vẫn phải kiểm tra từng isSuccess. Không thay supplier wire key.

`providerHttpStatus` là HTTP **MISA trả về**, có thể 200 dù MISA từ chối hóa đơn trong Data; HTTP gateway là 422.
Nếu không nhận được response (timeout/network), không tạo một HTTP status giả.

409 chống trùng là lỗi **gateway**, không phải một response mới của MISA. Response có `errorSource = gateway`,
`previousAttempt` và URL tra cứu. PreviousAttempt giữ key/operation/requestId gốc, trạng thái, kết quả hoặc lastError đã lọc nếu có.
Record pending giữ lastError qua restart; không log raw exception/response. LastError không có nghĩa đã xác minh trạng thái cuối trên MISA.
canRetry=false cho attempt hiện hữu; not-found cho canRetry=null vì journal không chứng minh hóa đơn chưa được tạo trên MISA.
Record cũ vẫn đọc được, nhưng key/lỗi/requestId không được lưu trước đó sẽ null; không thể tái tạo raw response quá khứ từ hash.
Không xóa claim/đổi key/invoiceId để vượt khóa. Phải đối soát với MISA trước khi quyết định gửi lại; không có API tự mở khóa.

| HTTP | Code chính |
|---|---|
| 400 | INVALID_INVOICE_REQUEST, INVALID_INVOICE_MAPPING_INPUT, INVOICE_TYPE_NOT_SUPPORTED, IDEMPOTENCY_KEY_REQUIRED, MISA_USB_WORKFLOW_REQUIRED, MISA_RECEIPT_TEMPLATE_NOT_FOUND |
| 409 | IDEMPOTENCY_KEY_CONFLICT, MISA_INVOICE_ALREADY_ATTEMPTED, MISA_OUTCOME_UNCERTAIN |
| 422 | MISA_OPERATION_REJECTED, MISA_INVOICE_REJECTED, MISA_DOCUMENT_REJECTED, MISA_RECEIPT_LOGIN_REJECTED |
| 502 | MISA_OPERATION_EMPTY_RESPONSE, MISA_OPERATION_INVALID_RESPONSE, MISA_OPERATION_CONNECTION_ERROR; lỗi token biên lai tương ứng MISA_RECEIPT_* |
| 504 | MISA_OPERATION_TIMEOUT, MISA_RECEIPT_TIMEOUT |

Auth/site/JSON store/host allowlist/login v3 như API-CONTRACT. Không gọi admin cũ; thiếu file/JSON sai/không đọc được trả 503 SITE_STORE_*, site không tồn tại trả 404. Provider khác MISA trả 501.
Không dùng EnsureSuccessStatusCode cho MISA; đọc envelope cả trên 4xx/5xx.

Login/đọc/CQT/void timeout 100s; FormSerial GET 90s. Publish/preview/create XML/publish XML giữ timeout vô hạn
có chủ đích của baseline, có cancellation từ caller. Nếu caller ngắt hoặc provider timeout sau khi business POST
đã bắt đầu, journal giữ pending. **Không tự retry write**, kể cả sau khi token bị từ chối.

## Token, credential, journal

- V3 cache tối đa 7 ngày trừ margin 5 phút, tính UTC, single-flight. Key fingerprint HMAC theo site/endpoint/tax/account/password/applicationId;
  đổi credential không dùng token cũ, cache có giới hạn 4096 entry. 401/TokenExpiredCode/InvalidTokenCode làm invalidation cho lần gọi sau.
- Health-check luôn login mới, tránh báo connected từ cache. Receipt token chưa cache liên request vì không có expiry đáng tin trong baseline;
  một thao tác phát hành dùng cùng token cho lookup template và publish.
- Token receipt GET dùng Username/Password, query appid=site GUID, header CompanyTaxcode=GUID.
  Body publish dùng userName/passWord từ Username2/Password2. API HSM CQT dùng Username2 làm AppID theo baseline.
- Provider URI query được escape đúng và toàn bộ HttpClient logger của token/business transport được gỡ để tránh log password.
  Không thay key/header/casing/UTF-8/external ezInvoiceID.
- Journal cần thư mục **bền qua restart/build/deploy**. Local đã đặt `.local/misa-journal`; default fallback nằm cạnh binary và
  không phù hợp nếu quy trình deploy xóa thư mục đó. Khi vận hành phải cấu hình JournalDirectory riêng, quyền truy cập/backup/retention phù hợp.
- Cùng site/operation/key + body giống nhau: trả lại kết quả completed, không gửi MISA lần hai. Body khác: 409.
  Claim theo site/invoice/phase chặn đổi key hoặc batch overlap để phát hành trùng. Phase điều chỉnh theo nhánh provider thực tế;
  hủy khóa theo loại hóa đơn và searchCode/transactionId của provider, không phụ thuộc invoiceId PMS tùy chọn.
- Validation/config/login/template lookup lỗi trước business POST: giải phóng reservation, có thể sửa và gửi lại.
  Sau POST: lỗi/malformed/timeout/cancellation giữ pending và lưu lastError đã lọc; không gửi lại bằng key khác. Dùng journal/status của MISA để đối chiếu.
- Journal hỏng/ghi chưa đủ trả trạng thái pending và chặn retry, không báo not-found. Journal là storage local cho một host/máy hiện tại; chưa có distributed ledger hoặc API tự reset/reconcile pending.
  Batch partial failure cũng giữ claims: chưa tự đoán item nào được phép phát hành lại. Không xóa journal để thử lại mù.

## USB và CQT

PMS điều phối: xml/create → xml/hash → ký bằng private key ở PMS/token thiết bị → tax/send-signed → xml/publish.
Gateway chỉ nhận public certificate/signature/XML đã ký, không nhận private key.
Mỗi bước write dùng Idempotency-Key riêng hoặc cùng key theo operation scope; giữ nguyên key/body nếu gọi lại cùng bước.
Trạng thái tạo XML và publish là hai phase khác nhau để không chặn bước cuối.

Server không có đủ correlation từ các response baseline để xác nhận toàn bộ chuỗi USB/CQT theo từng invoice;
thứ tự là trách nhiệm PMS và được kiểm tra thêm trong sandbox trước cutover. API hash/HSM CQT có thể thao tác trên tập pending
của provider, không được giả định chỉ tác động một invoice. Chức năng HSM CQT được giữ tương đương baseline trong phạm vi viết lại;
lựa chọn giới hạn chỉ USB còn chờ Q010, chưa coi người dùng đã duyệt một thay đổi nghiệp vụ.

## Kiểm thử và phần cần chốt

16 fixture payload từ frozen mapper độc lập; schema/header/encoding, response lỗi/double encoding, auth/site, token concurrency,
expiry, query log, idempotency/restart/uncertain outcome đã kiểm thử fake HTTP.

Fixture được capture dưới culture invariant, local timezone SE Asia Standard Time UTC+7; chỉ FormSerial được chốt timezone cố định.
Các mapping ngày/ticks/string số khác và routing giữ baseline khi Q008/Q009 chưa có câu trả lời. Không gọi sự giữ nguyên này là phê duyệt đề xuất.
Đặc biệt mode 4 cần serial nhận diện MTT; batch mode 1/4 còn dựa trên serial như code cũ. Không tự sửa các nhánh thiếu.
F10 biên lai Quantity ép int vẫn giữ; golden thực tế và các công thức tiền cần xác nhận trước production.

Import [collection](postman/eInvoice-MISA.postman_collection.json) với environment local hiện có, hoặc
[environment template](postman/eInvoice-MISA.environment.template.json) rồi điền key tại máy. Không đưa credential thật vào Git/chat.
OpenAPI tại [openapi.misa.json](openapi.misa.json); scripts/build-misa-openapi.py và build-misa-postman.py tái tạo tài liệu synthetic.
