# Hợp đồng PMS mới — eInvoice

Trạng thái: MISA 19 action và Viettel 13 action dispatcher baseline đã triển khai/kiểm thử offline. Sandbox/UAT chưa thực hiện. Phạm vi/validation/response/timeout/idempotency: [MISA-API](MISA-API.md), [VIETTEL-API](VIETTEL-API.md); schema [OpenAPI](openapi.misa.json) phục vụ tại `/openapi/einvoice.json` và URL `/openapi/misa.json` đã có.

Route chung chọn adapter theo partner của SiteId. `GET /api/eInvoice/capabilities` trả catalog thực tế của site; `/api/eInvoice/misa/capabilities` vẫn là catalog MISA riêng. Các route bổ sung Viettel: `draft`, `search`, `adjust/increase`, `adjust/decrease`, `adjust/information`; không thêm key body. Nhà cung cấp chưa triển khai hoặc không có operation trả 501 với code phân biệt PROVIDER_NOT_IMPLEMENTED / PROVIDER_OPERATION_NOT_SUPPORTED. FormSerial/status/CQT/USB hiện chỉ MISA.

Journal response thêm field nullable `provider`, record cũ không có thì null. Hash MISA giữ nguyên; các provider khác có attempt hash riêng nhưng claim site/invoice dùng chung. Không đổi key/ID hoặc site/provider để vượt khóa. Ngày/culture provider mới theo D021; không đổi các mapper MISA chưa được duyệt.

Tài liệu bàn giao cho AI agent phía phần mềm tích hợp: [INTEGRATION-AGENT](INTEGRATION-AGENT.md). Bắt đầu với single HSM MISA trên môi trường test; hướng dẫn phản ánh trạng thái hiện tại, không giả định đã có API đối soát/mở khóa hoặc adapter các nhà cung cấp khác.

- Base route: `/api/eInvoice`.
- Chỉ tên mới, không alias route/key lịch sử.
- JSON camelCase. Các key provider được định nghĩa trong model provider riêng; không dùng naming policy của API để serialize provider.
- Giá trị số partner/action/loại hóa đơn được giữ theo baseline; action 13 không tồn tại.
- Tiền giữ decimal; không chuyển float/double. Nullable/default có trong code cũ được giữ để chưa tự quyết nghiệp vụ.
- HTTP 4xx/5xx theo lỗi; JSON lỗi theo Problem Details, bổ sung `code` và `requestId`.
- Lỗi MISA business có providerCode/providerMessage đã lọc/providerHttpStatus nếu có; 409 của journal có previousAttempt và attemptStatusUrl. Xem MISA-API về tra cứu theo invoiceId và giữ lỗi qua restart. Không trả raw response/credential.
- Authentication: X-Api-Key; site scope: X-Site-Id; yêu cầu HTTPS. Body config phải trùng site được cấp quyền.
- JSON input phân biệt casing, từ chối key không biết/key cũ và chuỗi thay cho số. Validation nghiệp vụ sẽ bổ sung theo từng provider/luồng.

## Bảng đổi tên phía PMS

| Tên cũ | Property C# mới / JSON mới | Ý nghĩa |
|---|---|---|
| ezInvoiceId / EzInvoiceId | InvoiceId / invoiceId | ID hóa đơn do PMS cung cấp |
| ezInvoiceIds | InvoiceIds / invoiceIds | Danh sách ID hóa đơn |
| oldezInvoiceId | OriginalInvoiceId / originalInvoiceId | ID hóa đơn được thay thế/điều chỉnh |
| oldThirdPartyInvoiceNumber | OriginalProviderInvoiceNumber / originalProviderInvoiceNumber | Số hóa đơn gốc tại NCC |
| ThirdPartyInvoiceNumber | ProviderInvoiceNumber / providerInvoiceNumber | Số hóa đơn tại NCC |
| ThirdPartyInvoiceCode | ProviderInvoiceCode / providerInvoiceCode | Mã hóa đơn tại NCC |
| RequestID | RequestId / requestId | ID liên kết request |
| ServiceSummarys | ServiceSummaries / serviceSummaries | Tổng hợp phí dịch vụ |
| SpecialTaxSummarys | SpecialTaxSummaries / specialTaxSummaries | Tổng hợp thuế đặc biệt |
| TaxSummarys | TaxSummaries / taxSummaries | Tổng hợp thuế |
| InvoiceOld | OriginalInvoice / originalInvoice | Dữ liệu hóa đơn gốc |
| system / SystemInfomation | System / system; SystemInformation (type) | Thông tin hệ thống gửi |
| transdate | TransactionDate / transactionDate | Ngày của chi tiết dịch vụ, giữ nguyên string |
| taxcode | TaxCode / taxCode | Mã số thuế để tra cứu |
| email | Email / email | Email người nhận |
| ma_dvcs | UnitCode / unitCode | Mã đơn vị Minvoice, default VP |
| TypeEinvoice | ElectronicInvoiceType / electronicInvoiceType | Chế độ HSM/MTT/USB, giữ giá trị số |

Các trường còn lại dùng camelCase từ property C#; giữ tên hiện tại nếu chưa xác minh được ý nghĩa. `SiteConfigInfo`, `PartnerUrl`, `Username2`... tạm giữ nghĩa cũ cho adapter cấu hình; thiết kế cấu hình provider có kiểu sẽ được bổ sung riêng.

Nguồn cấu hình server hiện là JSON local theo D019, sau này thay bằng DB riêng qua ISiteConfigurationStore. Client admin cũ chỉ được giữ để đối chiếu baseline, không đăng ký hoặc gọi trong API.

## Endpoint đã có

- `GET /health/live`: không cần API key, không gọi dịch vụ ngoài; trả `{"service":"eInvoice","status":"alive"}`. Đây là liveness, chưa phải readiness cho mọi provider.
- `POST /api/eInvoice/health-check`: yêu cầu HTTPS, X-Api-Key, X-Site-Id; body `{}` hoặc `{"requestId":"pms-request-001"}` để lấy config từ JSON server. Nếu có siteConfigInfo, config request được ưu tiên và ID phải khớp header.

Hiện health-check chỉ hỗ trợ MISA (partner 6), dùng token v3 như luồng Login/HealthCheck baseline; chưa kiểm tra riêng token biên lai. Response thành công không trả password/token/config:

```json
{
  "provider": "MISA",
  "connected": true,
  "siteId": "11111111-1111-1111-1111-111111111111",
  "requestId": "server-correlation-id"
}
```

requestId response/lỗi là ID server tạo. requestId PMS gửi hiện chưa tích hợp vào logging; sẽ giữ riêng khi bổ sung log.

| HTTP | Tình huống / code |
|---|---|
| 400 | Body sai/key cũ; SITE_ID_REQUIRED, SITE_CONFIGURATION_MISMATCH, INVALID_SITE_CONFIGURATION, MISA_CREDENTIALS_REQUIRED |
| 401 | Thiếu/sai/hết hạn API key hoặc request không qua HTTPS |
| 403 | SITE_ACCESS_DENIED |
| 404 | SITE_CONFIGURATION_NOT_FOUND hoặc route không có |
| 422 | MISA_LOGIN_REJECTED, MISA_ACCOUNT_NOT_AUTHORIZED, MISA_INVALID_CREDENTIALS |
| 501 | PROVIDER_NOT_IMPLEMENTED (health-check provider khác MISA) |
| 502 | MISA_CONNECTION_ERROR, MISA_EMPTY_RESPONSE, MISA_INVALID_RESPONSE |
| 503 | SITE_STORE_NOT_CONFIGURED, SITE_STORE_INVALID_DATA, SITE_STORE_UNAVAILABLE, MISA_ENDPOINT_NOT_ALLOWED |
| 504 | MISA_TIMEOUT |
| 500 | EINVOICE_INTERNAL_ERROR; không trả exception/raw provider body |

JSON lỗi (`application/problem+json`) ví dụ:

```json
{
  "title": "Access to this site is denied",
  "status": 403,
  "code": "SITE_ACCESS_DENIED",
  "requestId": "server-correlation-id"
}
```

Có thể có field chuẩn `type`. Lỗi body do framework có thể dùng HTTP_400; lỗi bắt bởi exception handler dùng INVALID_REQUEST.

## Lấy mẫu/ký hiệu MISA

`POST /api/eInvoice/form-serials`, HTTPS, X-Api-Key và X-Site-Id như health-check.

```json
{"invoiceType":0,"requestId":"pms-form-001"}
```

`invoiceType` mặc định 0 nếu bỏ qua. Vé (1), biên lai (2), null và giá trị khác hiện trả 400 `INVOICE_TYPE_NOT_SUPPORTED`, không gọi admin/provider. MISA partner 6 là provider duy nhất đã port; provider khác trả 501 `PROVIDER_NOT_IMPLEMENTED`.

`siteConfigInfo` tùy chọn mới trên request FormSerial; nếu có, cấu hình request được ưu tiên như health-check, `siteConfigInfo.site.id` phải khớp header. Không có cấu hình request thì lấy từ JSON server. Năm truy vấn là năm hiện tại theo UTC+7, không có key year do PMS gửi.

Response 200:

```json
{
  "provider":"MISA",
  "siteId":"11111111-1111-1111-1111-111111111111",
  "forms":[{
    "invoiceName":"Hóa đơn thử nghiệm",
    "formNo":"1",
    "serial":"C26TAA",
    "quantity":0,
    "fromNo":0,
    "toNo":0,
    "currentNo":0,
    "status":1,
    "startDate":null,
    "isMultipleTaxRates":true
  }],
  "requestId":"server-correlation-id"
}
```

`forms` giữ thứ tự, bỏ mẫu kế thừa. `invoiceName` tối đa 40 ký tự UTF-16; `status` 1 sử dụng, 0 ngừng sử dụng; `isMultipleTaxRates` theo BusinessAreas 34. MISA baseline không gán quantity/fromNo/toNo/currentNo/startDate nên vẫn 0/null, không thể suy ra số lượng hóa đơn từ các trường này. Không có mẫu hợp lệ: 200 với forms rỗng.

Lỗi auth/site/admin/login giữ bảng trên. Lỗi riêng FormSerial:

| HTTP | Code |
|---|---|
| 400 | INVOICE_TYPE_NOT_SUPPORTED |
| 422 | MISA_FORM_SERIAL_REJECTED |
| 502 | MISA_FORM_SERIAL_EMPTY_RESPONSE, MISA_FORM_SERIAL_INVALID_RESPONSE, MISA_FORM_SERIAL_CONNECTION_ERROR |
| 504 | MISA_FORM_SERIAL_TIMEOUT |

Timeout 90 giây áp dụng GET templates sau login; login tối đa 100 giây riêng. Token v3 có cache theo site/endpoint/tax/account/credential, single-flight và expiry UTC; health-check luôn đăng nhập mới. Không trả raw response/token/credential trong JSON lỗi/thành công. Xem MISA-API về invalidation và giới hạn token biên lai.

Import cURL trong Postman với environment local đang có (`apiKey`, `siteId`):

```bash
curl --request POST 'https://localhost:7150/api/eInvoice/form-serials' \
  --header 'Content-Type: application/json' \
  --header 'X-Api-Key: {{apiKey}}' \
  --header 'X-Site-Id: {{siteId}}' \
  --data-raw '{"invoiceType":0,"requestId":"postman-form-001"}'
```

Local có site mẫu nhưng credential/endpoint trống: health-check trả 400 MISA_CREDENTIALS_REQUIRED; FormSerial kiểm tra endpoint trước nên trả 503 MISA_ENDPOINT_NOT_ALLOWED. Thiếu file JSON trả 503 SITE_STORE_NOT_CONFIGURED. Test 200 dùng HTTP giả lập, chưa gọi sandbox. Setup JSON tại LOCAL-SETUP.

## Cyberbill một phần

`site.partner: 7`, provider `CYBER_BILL`: health-check, document và void đã port. Hợp đồng/giới hạn tại [CYBERBILL-API](CYBERBILL-API.md). Các route chưa hoàn thành trả 501; fixture mapping không phải capability API.

## SUN

`site.partner: 3`, provider `SUN`, đủ 10 action baseline. `document/draft` mới dùng DownloadPdfRequest; preview SUN tải PDF nháp đã có theo `invoice.searchCode`. Response transactionId của SUN là sid, dùng lại khi hủy/điều chỉnh. invoiceId cho SUN nên là GUID ổn định, tối thiểu 32 ký tự sau bỏ dấu `-`; gateway giữ thuật toán sid NCC và khóa cả sid để chặn trùng. Chi tiết ở [SUN-API](SUN-API.md).
