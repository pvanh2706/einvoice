# Tích hợp phát hành thử hóa đơn qua eInvoice — hướng dẫn cho AI agent

Ngày cập nhật: **11/10/2026**, múi giờ Việt Nam UTC+7. Hướng dẫn này tập trung vào MISA từ checkpoint `316ba42`. Bản mới đã thêm Viettel qua cùng router, xem [VIETTEL-API](VIETTEL-API.md); không suy ra mọi operation MISA đều hỗ trợ ở Viettel.

## 1. Mức sẵn sàng và phạm vi lần tích hợp đầu

Phần mềm có thể bắt đầu tích hợp và phát hành thử **qua MISA trên môi trường test**, sau khi quản trị eInvoice cung cấp địa chỉ truy cập được, API key còn hạn, SiteId và mẫu/ký hiệu được phép dùng. Bộ API MISA đã được kiểm thử offline với 268 ca; login MISA thực tế đã trả `connected: true` ngày 09/10/2026. Chưa có kết quả phát hành thực tế thành công được xác nhận, nên lần tích hợp này cũng là kiểm chứng sandbox, chưa phải xác nhận dùng production.

Phạm vi đề xuất: **một hóa đơn điện tử thông thường ký HSM**, `invoice.invoiceType = 0`, `electronicInvoiceType = 0`, ký hiệu không thuộc hóa đơn máy tính tiền. Lấy mẫu/ký hiệu thật từ API và xác nhận mẫu HSM với quản trị eInvoice. Vé, biên lai, USB và các nhà cung cấp khác nằm ngoài bước đầu này.

Tại thời điểm lập tài liệu:

- API local đang nghe tại `https://localhost:7150`; `GET /health/live` trả `{"service":"eInvoice","status":"alive"}`.
- API chỉ nghe loopback. Phần mềm trên **máy khác**, trong container hoặc môi trường cloud không thể dùng `localhost` để tới máy eInvoice. Quản trị eInvoice cần cung cấp URL HTTPS có thể truy cập từ backend của phần mềm, với chứng chỉ hợp lệ cho hostname đó.
- Key `postman-local` cũ hết hạn lúc 23:59:59 ngày **09/10/2026 UTC+7**. Cần key còn hiệu lực trước khi gọi API nghiệp vụ; tài liệu không chứa key thật.
- Adapter hiện có MISA và Viettel. Hướng dẫn này dùng site MISA; site của nhà cung cấp chưa có adapter trả `501 PROVIDER_NOT_IMPLEMENTED`, operation không được hỗ trợ trả `501 PROVIDER_OPERATION_NOT_SUPPORTED`.
- Chưa có API tự đối soát kết quả phát hành với MISA hoặc tự mở khóa lần gửi chưa xác định.

## 2. Thông tin cần nhận từ quản trị eInvoice

| Biến cấu hình phía phần mềm | Ý nghĩa |
|---|---|
| `EINVOICE_BASE_URL` | URL gốc của gateway, ví dụ `https://localhost:7150`; **không** bao gồm `/api/eInvoice` |
| `EINVOICE_API_KEY` | Key còn hiệu lực, được cấp riêng cho phần mềm và có quyền SiteId cần test |
| `EINVOICE_SITE_ID` | GUID khách sạn/đơn vị đã cấu hình MISA trên gateway |
| Mẫu/ký hiệu và cách ký | Mẫu được phép dùng trong môi trường test, xác định HSM thông thường hay MTT/USB |

Gọi API từ **backend của phần mềm**. API key lưu trong secret/config backend, không đưa vào frontend, URL, Git hoặc log. Phần mềm không cần nhận mật khẩu/token MISA. Gateway lấy cấu hình MISA từ SiteId trên server; không gửi `siteConfigInfo` hoặc `userInfo` trong luồng tích hợp này.

Không tự đổi API key, site, endpoint nhà cung cấp hoặc cấu hình gateway. Nếu thiếu thông tin, hỏi người phụ trách tích hợp. Chứng chỉ HTTPS phải được backend tin cậy; local có thể cài trust dev certificate, không tắt kiểm tra TLS để triển khai tích hợp.

## 3. Quy tắc HTTP/JSON

Mọi API nghiệp vụ cần:

```http
X-Api-Key: <key được cấp>
X-Site-Id: <GUID được cấp>
```

POST gửi `Content-Type: application/json` và JSON UTF-8. Các thao tác ghi như `publish` thêm:

```http
Idempotency-Key: <key ổn định của lần phát hành>
```

- Key idempotency gồm **8–128 ký tự ASCII**: chữ, số, `-`, `_`; có thể dùng UUID dạng chuỗi.
- JSON đúng **camelCase**, phân biệt casing; key lạ/key cũ bị từ chối. Không gửi `ezInvoiceId`, `TypeEinvoice`, `ServiceSummarys` hoặc key theo wire của MISA.
- Tiền/số lượng/tỷ giá là **JSON number**, không phải chuỗi có dấu phân cách hàng nghìn. Với C#, dùng `decimal`.
- Ngày đầu vào `dd/MM/yyyy`; ví dụ `10/10/2026`. Các giá trị precision 0–28.
- Chỉ dùng các trường công bố. `CreateInvoiceRequest` **không có `requestId` ở cấp gốc**; có thể dùng `invoice.requestId` nếu cần, nhưng nó chưa được nối thành correlation server. `requestId` của response là ID gateway tạo.
- Giữ nguyên snapshot body cho một lần phát hành, kể cả ngày, dữ liệu bổ sung và các trường tùy chọn, khi kiểm tra/replay cùng key.

## 4. Trình tự tích hợp

### Bước A — Kiểm tra gateway và cấu hình MISA

`GET /health/live` không cần key; chỉ kiểm tra tiến trình, không xác nhận kết nối MISA.

`POST /api/eInvoice/health-check` với body `{}` và hai header xác thực. Đây là kiểm tra kết nối MISA thực tế, không phát hành hóa đơn.

Response thành công:

```json
{
  "provider": "MISA",
  "connected": true,
  "siteId": "11111111-1111-1111-1111-111111111111",
  "requestId": "server-correlation-id"
}
```

SiteId trong các ví dụ là GUID tổng hợp; thay bằng SiteId được cấp. Không gọi tiếp publish nếu bước này chưa thành công.

### Bước B — Lấy mẫu/ký hiệu

`POST /api/eInvoice/form-serials`:

```json
{"invoiceType": 0}
```

Response trả `forms` ở cấp gốc, **không phải `data`**. Dùng cặp `forms[].formNo` và `forms[].serial` do server trả về; chọn mẫu đang sử dụng (`status: 1`) phù hợp cách ký đã xác nhận. `forms: []` nghĩa không có mẫu hợp lệ để chọn. Năm truy vấn là năm hiện tại UTC+7, không gửi `year`.

Không lấy mẫu/ký hiệu trong ví dụ làm dữ liệu thật. `quantity`, `currentNo`, `fromNo`, `toNo` hiện giữ giá trị mặc định từ baseline, không dùng chúng để kết luận còn số hóa đơn.

Lưu ý nhánh MISA hiện vẫn nhận diện MTT theo ký hiệu: **ký tự thứ ba từ cuối là `M`**, hoặc mode = 1. Do đó mode = 0 với ký hiệu MTT vẫn có thể đi nhánh MTT. Đối với bước HSM thông thường trong tài liệu này, chọn đúng mẫu HSM không thuộc MTT; không tự sửa ký hiệu để ép nhánh.

### Bước C — Chuẩn bị và lưu lần gửi trước khi POST

Tạo bản ghi tích hợp bền vững phía phần mềm gồm `siteId`, `invoiceId`, operation `publish`, `idempotencyKey` và snapshot body. `invoiceId` là ID ổn định của hóa đơn trong phần mềm, phân biệt theo site; UUID là lựa chọn thuận tiện. Lưu key **trước** khi gọi HTTP, dùng ràng buộc/concurrency phía phần mềm để hai worker không cùng gửi một hóa đơn.

Một hóa đơn test mới độc lập dùng ID/key mới. Một hóa đơn đã gửi mà chưa biết kết quả phải giữ ID/key cũ; không tạo bản sao để vượt cơ chế chặn trùng.

### Bước D — Phát hành một hóa đơn

`POST /api/eInvoice/publish`. Body đầy đủ cho ví dụ một dịch vụ, không chiết khấu/phí dịch vụ, một mức thuế:

```json
{
  "invoice": {
    "invoiceId": "AGENT-HSM-TEST-0001",
    "invoiceType": 0,
    "formNo": "1",
    "serial": "C26TAA",
    "invoiceDate": "10/10/2026",
    "customerName": "Khách thử nghiệm tích hợp",
    "customerAddress": "Địa chỉ thử nghiệm",
    "currencyCode": "VND",
    "exchangeRate": 1,
    "paymentMethod": "TM/CK",
    "subAmount": 100000,
    "serviceRate": 0,
    "serviceCharge": 0,
    "beforeTaxAmount": 100000,
    "taxRate": 10,
    "taxAmount": 10000,
    "afterTaxAmount": 110000,
    "isMultiVATRate": false,
    "pricePrecision": 0,
    "quantityPrecision": 0,
    "amountPrecision": 0
  },
  "details": [
    {
      "itemCode": "SERVICE-TEST",
      "itemName": "Dịch vụ thử nghiệm tích hợp",
      "unitName": "Lần",
      "quantity": 1,
      "price": 100000,
      "subAmount": 100000,
      "discountAmount": 0,
      "discountPercent": 0,
      "serviceRate": 0,
      "serviceCharge": 0,
      "beforeTaxAmount": 100000,
      "taxRate": 10,
      "taxAmount": 10000,
      "afterTaxAmount": 110000,
      "exciseTaxRate": 0,
      "exciseTaxCharge": 0,
      "itemType": "1",
      "transactionDate": "10/10/2026"
    }
  ],
  "taxSummaries": [
    {"beforeTaxAmount": 100000, "taxRate": 10, "taxAmount": 10000}
  ],
  "serviceSummaries": [],
  "specialTaxSummaries": [],
  "extraData": [],
  "electronicInvoiceType": 0
}
```

Bản JSON riêng để copy: [examples/misa-publish.json](examples/misa-publish.json). Trước **lần gửi đầu tiên**, thay ID, mẫu/ký hiệu, ngày và dữ liệu nghiệp vụ theo môi trường được cấp. Thuế 10% chỉ là dữ liệu tổng hợp của ví dụ, không phải quy tắc tính thuế cho mọi hàng hóa/dịch vụ. Ví dụ không đặt email; khi có `customerEmail`, mapper phát hành có thể yêu cầu MISA gửi email theo hành vi hiện tại.

Các nhóm dữ liệu mở rộng gồm thông tin người mua doanh nghiệp (`companyName`, `customerTax`...), ngân hàng, nhiều thuế suất, chiết khấu, giảm thuế, `hotelExtra` và `extraData`. Chỉ sử dụng khi đã thống nhất mapping nghiệp vụ. Gateway không tự tính lại toàn bộ số tiền để sửa request; phần mềm chịu trách nhiệm gửi các tổng/chi tiết nhất quán.

### Bước E — Đọc kết quả phát hành

Response HTTP 200 ví dụ:

```json
{
  "provider": "MISA",
  "siteId": "11111111-1111-1111-1111-111111111111",
  "data": [
    {
      "invoiceId": "AGENT-HSM-TEST-0001",
      "providerInvoiceNumber": "000001",
      "transactionId": "synthetic-transaction-id",
      "searchCode": "synthetic-transaction-id",
      "taxAuthorityCode": null,
      "invoiceData": null,
      "isSuccess": true,
      "providerErrorCode": null,
      "providerMessage": null
    }
  ],
  "requestId": "server-correlation-id"
}
```

Ghép item bằng `invoiceId`, kiểm tra đúng site, provider và `isSuccess: true`; không chỉ kiểm tra HTTP 200. Với phạm vi `/publish` HSM trong tài liệu này, đó là kết quả phát hành thành công mà gateway đã nhận từ MISA. Lưu `providerInvoiceNumber`, `transactionId`, `searchCode`, `taxAuthorityCode` khi có, key gốc và `requestId` server. Không giả định mọi trường đều có giá trị.

Phát hành và CQT tiếp nhận là hai trạng thái riêng. `isSuccess: true` của `xml/create` cũng không có nghĩa đã hoàn tất phát hành; tài liệu này không hướng dẫn chuỗi ký USB.

## 5. Mất response, lỗi và chống phát hành trùng

### Tra cứu kết quả gateway đã lưu

Hai API GET đều cần key/site và **không gọi MISA**:

```http
GET /api/eInvoice/operations/publish/<idempotencyKey gốc>
GET /api/eInvoice/invoice-attempts?invoiceId=<invoiceId đã URL-encode>&phase=publish
```

Response là object trực tiếp, không có wrapper `data`:

```json
{
  "attemptId": "<hash nội bộ>",
  "operation": "publish",
  "idempotencyKey": "<key gốc>",
  "state": "completed",
  "updatedAtUtc": "2026-10-10T03:00:00+00:00",
  "result": [
    {
      "invoiceId": "AGENT-HSM-TEST-0001",
      "providerInvoiceNumber": "000001",
      "transactionId": "synthetic-transaction-id",
      "searchCode": "synthetic-transaction-id",
      "taxAuthorityCode": null,
      "invoiceData": null,
      "isSuccess": true,
      "providerErrorCode": null,
      "providerMessage": null
    }
  ],
  "lastError": null,
  "originalRequestId": "server-correlation-id",
  "requiresReconciliation": false,
  "canRetry": false
}
```

| Dữ liệu thực tế | Cách hiểu và xử lý |
|---|---|
| `completed` + item đúng ID, `isSuccess: true` trong `result` | Gateway đã lưu kết quả thành công; cập nhật phần mềm từ `result` |
| `completed` nhưng item lỗi | Operation đã xử lý xong, không đồng nghĩa mọi item thành công; đọc lỗi từng item |
| `pending` | Đang xử lý **hoặc** đã gặp lỗi sau dispatch và đang giữ khóa; chưa thể suy ra trạng thái cuối của hóa đơn MISA |
| `lastError` có lỗi MISA | Hiển thị mã/message đã lọc; không coi lỗi trùng ID hoặc response thiếu bằng chứng là chứng minh hóa đơn chưa được phát hành |
| `not-found`, `canRetry: null` | Không có bản ghi tương ứng trong journal này; không chứng minh MISA chưa có hóa đơn |

Enum **hiện có** là `not-found`, `pending`, `completed`. `processing`, `rejected`, `unknown` mới là hướng cải tiến đã thảo luận, chưa có trong API. `canRetry: false` áp dụng cho attempt hiện hữu, kể cả completed; nó không phải cờ thành công/thất bại. Trạng thái pending có thể giữ lâu qua restart; polling không tự mở khóa. Record cũ có thể thiếu key, operation, lastError và requestId.

Mất response/timeout/lỗi mạng: ghi nhận trạng thái phía phần mềm là “Chưa xác định kết quả”, tra cứu GET; nếu vẫn pending thì đối soát cùng quản trị eInvoice/MISA. Không tự retry POST, kể cả khi MISA hoặc gateway trả 5xx. Nếu lookup thất bại vì key/TLS/network, giữ nguyên trạng thái chưa xác định.

Gateway hỗ trợ replay **kết quả completed** khi cùng site/operation/key và cùng body; không gửi lại MISA. Cùng key nhưng body khác trả 409. Đổi key cho một hóa đơn đã có claim cũng trả 409. Đừng cấu hình retry chung của HTTP client/proxy cho thao tác publish.

### Hợp đồng lỗi

HTTP lỗi có JSON Problem Details; đọc body cả khi status không thành công:

```json
{
  "title": "Provider rejected the request",
  "status": 422,
  "code": "MISA_INVOICE_REJECTED",
  "errorSource": "provider",
  "providerCode": "MISA_ITEM_REJECTED",
  "providerMessage": "Thông báo MISA đã được lọc, nếu có",
  "providerHttpStatus": 200,
  "requestId": "server-correlation-id"
}
```

Các trường `providerCode`, `providerMessage`, `providerHttpStatus`, `attempt`, `previousAttempt`, `attemptStatusUrl` là tùy trường hợp, có thể vắng mặt/null. Message đã che dữ liệu bí mật; không phải toàn bộ response MISA. `providerHttpStatus: 200` không đồng nghĩa nghiệp vụ thành công. Lỗi kết nối có thể chưa nhận được JSON, nên parser phải xử lý cả body rỗng/không phải JSON.

| HTTP / code | Hướng xử lý phía phần mềm |
|---|---|
| 400 / `INVALID_INVOICE_REQUEST`, `INVALID_INVOICE_MAPPING_INPUT` | Kiểm tra body và mapping; không tự sửa nội dung một attempt đã dispatch |
| 400 / `IDEMPOTENCY_KEY_REQUIRED` | Kiểm tra một header key đúng định dạng; không tạo key mới cho hóa đơn đã gửi |
| 400 / `MISA_USB_WORKFLOW_REQUIRED` | Cách ký chưa phù hợp luồng HSM; làm rõ với quản trị trước khi gửi |
| 401 | Thiếu/sai/hết hạn key hoặc không dùng HTTPS; kiểm tra cấu hình tích hợp |
| 403 / `SITE_ACCESS_DENIED` | Key không được quyền site; không chuyển sang site khác để thử |
| 404 / `SITE_CONFIGURATION_NOT_FOUND` | Quản trị bổ sung/kiểm tra cấu hình site |
| 409 / `IDEMPOTENCY_KEY_CONFLICT` | Key đã gắn với body khác; phục hồi snapshot/key gốc, kiểm tra lần gửi trước |
| 409 / `MISA_INVOICE_ALREADY_ATTEMPTED`, `MISA_OUTCOME_UNCERTAIN` | Lỗi gateway chống trùng, không phải response MISA mới; đọc `previousAttempt`, tra cứu và đối soát |
| 422 / `MISA_INVOICE_REJECTED`, `MISA_OPERATION_REJECTED` | Hiển thị lỗi NCC; nếu có attempt pending/requiresReconciliation thì đối soát trước khi sửa/gửi lại |
| 500, 502, 504 hoặc mất response | Chưa đủ kết luận publish thất bại; GET journal trước và giữ ID/key |
| 503 / `SITE_STORE_*`, `MISA_ENDPOINT_NOT_ALLOWED` | Quản trị kiểm tra cấu hình server/allowlist; client không gửi credential MISA để né lỗi |
| 501 / `PROVIDER_NOT_IMPLEMENTED` | Site không dùng adapter MISA đang hỗ trợ |

Lỗi validation/login/config **trước business POST** có thể giải phóng reservation, nhưng không suy ra được quyền gửi lại chỉ từ HTTP status. Không có endpoint tự reset claim. Không đổi invoiceId/key, xóa journal hoặc phát hành bản sao để né lỗi 409. Chỉ gửi lại sau khi xác định attempt chưa được dispatch hoặc đã có quyết định đối soát phù hợp từ bên quản trị.

Nếu dùng `attemptStatusUrl` do response cung cấp, chỉ gọi đường dẫn tương đối trên **cùng base URL gateway**, kèm header key/site; không gửi key tới hostname bên ngoài.

## 6. Tra cứu CQT và tải PDF sau thành công

`POST /api/eInvoice/status` không yêu cầu Idempotency-Key:

```json
{
  "invoiceIds": ["AGENT-HSM-TEST-0001"],
  "invoiceType": 0,
  "formNo": "1",
  "serial": "C26TAA",
  "electronicInvoiceType": 0
}
```

Response `data` là mảng `{invoiceId, taxAuthorityStatus}`. Trường status hiện ánh xạ số `SendTaxStatus` của MISA; chưa công bố enum chuẩn hóa. Không tự đoán ý nghĩa 0/1/2; cần mapping được xác minh với môi trường MISA. Đây không phải API phục hồi số hóa đơn/kết quả publish khi journal pending. Response có thể không có đủ tất cả ID yêu cầu; không tự coi ID thiếu là thành công/thất bại.

`POST /api/eInvoice/document`, lấy `searchCode` từ kết quả publish, và giữ mẫu/mode của lần phát hành:

```json
{
  "invoiceType": 0,
  "searchCode": "<searchCode đã lưu>",
  "serial": "<serial của hóa đơn>",
  "electronicInvoiceType": 0,
  "typeFileDownload": "pdf"
}
```

Response thành công: `data = {"content":"<base64>","format":"base64"}`. Giải mã Base64 thành file PDF; không ghi chuỗi Base64 trực tiếp vào file PDF. API `preview` trả link trong `data.content` với `format: "url"`, khác với document; preview thành công không chứng minh publish thành công.

## 7. cURL để import vào Postman

Tạo environment Postman: `baseUrl`, `apiKey`, `siteId`, `idempotencyKey`. Key thật lấy qua kênh cấu hình được cấp. Thay placeholder `baseUrl` bằng URL gateway truy cập được nếu không test trên cùng máy.

Kiểm tra MISA:

```bash
curl --request POST '{{baseUrl}}/api/eInvoice/health-check' \
  --header 'Content-Type: application/json' \
  --header 'X-Api-Key: {{apiKey}}' \
  --header 'X-Site-Id: {{siteId}}' \
  --data '{}'
```

Lấy mẫu:

```bash
curl --request POST '{{baseUrl}}/api/eInvoice/form-serials' \
  --header 'Content-Type: application/json' \
  --header 'X-Api-Key: {{apiKey}}' \
  --header 'X-Site-Id: {{siteId}}' \
  --data '{"invoiceType":0}'
```

Phát hành (import cURL vào Postman, chọn body **raw / JSON**, dán JSON ở bước D và sửa dữ liệu trước lần gửi đầu):

```bash
curl --request POST '{{baseUrl}}/api/eInvoice/publish' \
  --header 'Content-Type: application/json' \
  --header 'X-Api-Key: {{apiKey}}' \
  --header 'X-Site-Id: {{siteId}}' \
  --header 'Idempotency-Key: {{idempotencyKey}}' \
  --data-binary '@misa-publish.json'
```

Nếu chạy cURL trong terminal, thay các biến Postman bằng giá trị/config của môi trường và đặt file JSON ở đúng đường dẫn; `{{...}}` không phải biến shell. Trong PowerShell dùng `curl.exe` và cú pháp xuống dòng PowerShell hoặc một dòng.

Tra cứu bằng key gốc:

```bash
curl --request GET '{{baseUrl}}/api/eInvoice/operations/publish/{{idempotencyKey}}' \
  --header 'X-Api-Key: {{apiKey}}' \
  --header 'X-Site-Id: {{siteId}}'
```

Collection đầy đủ có sẵn: [postman/eInvoice-MISA.postman_collection.json](postman/eInvoice-MISA.postman_collection.json). Schema toàn bộ DTO: tải `GET /openapi/misa.json` hoặc đọc [openapi.misa.json](openapi.misa.json). Các file này không chứa credential thật.

## 8. Yêu cầu triển khai cho AI agent phía phần mềm

1. Tạo client backend với base URL, API key và SiteId cấu hình riêng; dùng JSON camelCase/decimal, kiểm tra TLS, không retry tự động POST publish.
2. Lưu hóa đơn + snapshot body + key idempotency trước khi gửi. Giữ lịch sử attempt và khóa gửi đồng thời. Không sinh key mới trong mỗi lần HTTP.
3. Thêm luồng lấy mẫu, chọn đúng HSM, phát hành một hóa đơn, đọc từng kết quả, lưu mã NCC; giữ trạng thái CQT riêng.
4. Sau timeout/lỗi/mất response hoặc 409, gọi GET journal, cập nhật từ result nếu có; nếu chưa có kết quả xác nhận thì hiển thị chưa xác định/cần đối soát. Không polling vô hạn hoặc tự phát hành bản sao.
5. Parse Problem Details, lưu code/providerCode/providerMessage đã lọc + requestId để hỗ trợ; không ghi key/credential/toàn bộ thông tin người mua vào log chung.
6. Kiểm thử bằng fake HTTP: thành công, JSON lỗi 422, 409 có previousAttempt, 401/403, timeout rồi GET completed, GET pending/not-found, response hỏng/thiếu ID, hai worker gửi cùng invoice, key giữ nguyên qua restart. Không phát hành thật trong unit test/CI.
7. Khi chạy thử sandbox được người phụ trách cho phép, đối chiếu số hóa đơn/PDF trên MISA và kết quả phần mềm. Báo kết quả thực tế; không đánh dấu tích hợp hoàn tất chỉ vì health-check thành công.

Nếu mở rộng batch, route là `POST /api/eInvoice/publish-batch`, body `{"invoices":[<CreateInvoiceRequest>, ...]}`. Một batch có cùng site/type/mode/form/serial, ID khác nhau và một Idempotency-Key cho toàn bộ body. HTTP 200 có thể chứa cả item thành công và thất bại; không tự gửi lại cả batch hoặc item lỗi khi chưa đối soát claims. Đợt đầu nên hoàn tất single HSM trước.

Các route adjust/replace/void/CQT/USB nằm ngoài đợt đầu. Đặc biệt `tax/send-pending` tác động hàng đợi pending của MISA theo site, không có bộ lọc invoiceIds; không gọi nó như một bước mặc định sau publish một hóa đơn.
