# Hợp đồng PMS mới — eInvoice

Trạng thái: DTO nền tảng và kiểm tra kết nối MISA. Chưa expose luồng phát hành/điều chỉnh/PDF/USB. OpenAPI đầy đủ sẽ bổ sung cùng từng luồng.

- Base route dự kiến: `/api/eInvoice`.
- Chỉ tên mới, không alias route/key lịch sử.
- JSON camelCase. Các key provider được định nghĩa trong model provider riêng; không dùng naming policy của API để serialize provider.
- Giá trị số partner/action/loại hóa đơn được giữ theo baseline; action 13 không tồn tại.
- Tiền giữ decimal; không chuyển float/double. Nullable/default có trong code cũ được giữ để chưa tự quyết nghiệp vụ.
- HTTP 4xx/5xx theo lỗi; JSON lỗi theo Problem Details, bổ sung `code` và `requestId`.
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

Tên/key hợp đồng **admin bên ngoài** cũng được giữ theo dịch vụ đang chạy, ví dụ `SiteId` và `StatusCode`. Đổi tên app không tự đổi hợp đồng của hệ thống khác.

## Endpoint đã có

- `GET /health/live`: không cần API key, không gọi dịch vụ ngoài; trả `{"service":"eInvoice","status":"alive"}`. Đây là liveness, chưa phải readiness cho mọi provider.
- `POST /api/eInvoice/health-check`: yêu cầu HTTPS, X-Api-Key, X-Site-Id; body `{}` hoặc `{"requestId":"pms-request-001"}` để lấy config từ admin. Nếu có siteConfigInfo, config request được ưu tiên và ID phải khớp header.

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
| 502 | ADMIN_UNAVAILABLE, MISA_CONNECTION_ERROR, MISA_EMPTY_RESPONSE, MISA_INVALID_RESPONSE |
| 503 | ADMIN_NOT_CONFIGURED, MISA_ENDPOINT_NOT_ALLOWED |
| 504 | ADMIN_TIMEOUT, MISA_TIMEOUT |
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
