# Tích hợp dự án bên ngoài với eInvoice — tài liệu thi công cho AI

Cập nhật **22/10/2026**, UTC+7. Đối chiếu mã nguồn `D:\Code\40.eInvoice`, commit `1981f45` trên branch `rewrite/foundation`. Tài liệu mô tả API gateway hiện có; không yêu cầu dự án gọi trực tiếp API nhà cung cấp hóa đơn.

## 1. Kết quả cần triển khai

Backend của dự án gọi gateway eInvoice để phát hành hóa đơn, lưu kết quả, lấy tài liệu hóa đơn và theo dõi các lần gửi chưa biết kết quả. Gateway tự chọn nhà cung cấp theo `X-Site-Id`, quản lý tài khoản NCC, chuyển đổi payload và lưu journal chống trùng.

```text
Người dùng → Backend dự án → HTTPS eInvoice → Nhà cung cấp hóa đơn
                   ↓                ↓
          Hóa đơn + lần gửi      Journal lần gửi
```

Đã có code và kiểm thử offline cho 9 provider / 133 action baseline. **UAT phát hành thực tế và cutover chưa hoàn thành** theo STATUS của dự án. Tài liệu không khẳng định URL local đang chạy hoặc key test cũ còn hiệu lực. Chỉ chạy nghiệp vụ thật khi có môi trường và dữ liệu thử được cấp.

Phạm vi nên hoàn thành đầu tiên: một hóa đơn thông thường `invoiceType: 0`, đúng nhà cung cấp và cách ký được cấp. Sau đó mới thêm batch, điều chỉnh, thay thế, hủy, USB hoặc CQT. Những luồng này có khác biệt theo NCC.

## 2. Đầu vào AI phải nhận từ người phụ trách

| Cấu hình | Ý nghĩa |
|---|---|
| `EINVOICE_BASE_URL` | URL gốc HTTPS của gateway, không kèm `/api/eInvoice`; ví dụ local cùng máy: `https://localhost:7150` |
| `EINVOICE_API_KEY` | Key còn hạn, có quyền đúng site; lưu trong secrets backend |
| `EINVOICE_SITE_ID` | GUID đơn vị/khách sạn đã cấu hình trên gateway |
| Provider dự kiến | Đối chiếu với response `capabilities`; không gửi partner tùy ý trong body |
| Môi trường | Sandbox hay production; hostname và chứng chỉ truy cập được từ backend |
| Mẫu/ký hiệu/cách ký | `formNo`, `serial`, HSM/MTT/USB và `electronicInvoiceType` tương ứng |
| Mapping nghiệp vụ | Nguồn dữ liệu người mua, tiền, thuế, chiết khấu, phí, ngày hóa đơn và quy tắc làm tròn đã được duyệt |

`localhost` trong container/cloud/máy khác chỉ tới chính môi trường đó. Đường dẫn ổ đĩa `D:\Code\40.eInvoice` là nơi chứa source, không phải địa chỉ API. AI có thể xây client và mock khi thiếu cấu hình, nhưng phải liệt kê phần thiếu trước khi thử kết nối thật.

Gateway đã có cấu hình NCC theo SiteId; backend tích hợp không cần mật khẩu/token NCC. Luồng tiêu chuẩn **bỏ `siteConfigInfo` và `userInfo`**. Cấu hình request có thể ghi đè cấu hình server nên không nhận các trường đó trực tiếp từ frontend. Nếu nhiều tenant, backend tự ánh xạ tenant đã xác thực sang SiteId được phép.

## 3. Hợp đồng HTTP và JSON

Base route: `/api/eInvoice`, đúng chữ hoa/thường. Không dùng `/api/ezInvoice` hoặc key cũ.

```http
X-Api-Key: <key được cấp>
X-Site-Id: <GUID được cấp>
Content-Type: application/json
Idempotency-Key: <key bền vững cho thao tác ghi>
```

Hai header xác thực áp dụng cho mọi API nghiệp vụ kể cả GET journal/capabilities. `Content-Type` dùng khi có JSON body. `Idempotency-Key` cần cho thao tác ghi, gồm 8–128 ký tự ASCII: chữ, số, `-`, `_`; UUID dạng D phù hợp.

- JSON UTF-8, **camelCase, phân biệt casing**. Key lạ/key cũ và chuỗi thay cho số bị từ chối.
- Tiền, thuế, tỷ giá và số lượng là JSON number. Dùng `decimal` hoặc thư viện decimal của ngôn ngữ đang dùng; không tính tiền bằng số thực nhị phân rồi làm tròn tùy ý.
- Ngày hóa đơn/chi tiết trong mẫu là `dd/MM/yyyy`. Không tự đổi sang ISO cho những trường này. Các trường ngày của API đọc phải theo schema từng route.
- Luôn đặt `invoice.invoiceType: 0` cho hóa đơn thông thường; DTO Invoice và DownloadPdfRequest hiện có default là **1 (vé)**. `invoiceType`: 0 hóa đơn, 1 vé, 2 biên lai; không phải mọi NCC hỗ trợ đủ ba loại.
- `electronicInvoiceType` là chế độ ký, khác `invoiceType`. Mẫu MISA HSM thường dùng 0; không suy ra toàn bộ bảng mode cho NCC khác.
- `CreateInvoiceRequest` không có `requestId` ở cấp gốc. `invoice.requestId` là trường riêng; response `requestId` do gateway tạo, hiện chưa nối correlation với ID client.
- Precision trong mẫu là số nguyên 0–28. Không tự diễn giải các giá trị thuế âm/sentinel ngoài quy tắc NCC đã thống nhất.
- Lưu body đã chốt trước khi gửi; không tái tạo ngày, số tiền, metadata, giá trị null hoặc trường tùy chọn khác khi replay.
- Kiểm tra chứng chỉ TLS. Không dùng `curl -k`, bỏ kiểm tra hostname hoặc chuyển sang HTTP để chạy nghiệp vụ.

## 4. Khám phá và chọn đúng luồng NCC

1. `GET /health/live` không cần key, trả `{"service":"eInvoice","status":"alive"}`; chỉ xác nhận tiến trình.
2. `GET /api/eInvoice/capabilities` có key/site; trả `provider` và `actions[]` gồm `action`, `name`, `route`. API này đọc catalog, không gọi NCC.
3. `POST /api/eInvoice/health-check`, body `{}`; kiểm tra cấu hình/kết nối tùy NCC.
4. Nếu capabilities có `/form-serials`, gọi POST với `{"invoiceType":0}` và chọn mẫu hợp lệ. Response dùng `forms` ở gốc. Nếu không hỗ trợ, nhận mẫu/ký hiệu từ người quản trị NCC.

Một số action có thể cùng route; phải đọc semantics của provider, không chỉ tên enum action.

| Partner cấu hình server | `provider` response | Luồng phát hành thông thường | Điểm cần chú ý |
|---|---|---|---|
| 1 | `VNPT` | `publish`; hoặc `draft` → `publish-existing` | Ký nháp cần invoiceId, invoiceType, formNo, serial; preview tạo nháp nên là thao tác ghi |
| 2 | `MINVOICE` | `publish`; hoặc `create` → `publish-existing` | invoiceId tạo mới là GUID; giữ riêng GUID NCC `providerInvoiceCode` và số NCC |
| 3 | `SUN` | `publish` | Dùng invoiceId GUID ổn định; `transactionId` trả về là sid, cần lưu để hủy/điều chỉnh |
| 4 | `VIETTEL` | `publish` hoặc `publish-batch` | Không có form-serials/status CQT theo baseline; mẫu/ký hiệu cấp ngoài API |
| 5 | `VNPT_V5` | `publish`; hoặc `draft` → `publish-existing` | `create` cũng có hành vi create-and-publish; publish giữ bước xóa nháp baseline nếu không đặt `deleteExistingDraft:false` |
| 6 | `MISA` | `publish` cho HSM phù hợp | MTT còn nhận diện theo serial; USB dùng chuỗi API XML riêng, không ghép vào HSM |
| 7 | `CYBER_BILL` | **`create` → `publish-existing`** | Không dùng publish để tạo rồi ký; `paymentMethod` là string mã `"1"`–`"5"`; create trả requiresSigning=true |
| 8 | `FAST` | `publish` hoặc `publish-batch` | Phân nhánh MTT/GTGT theo serial; draft chỉ MTT; health-check chỉ kiểm tra cấu hình |
| 9 | `EASY_INVOICE` | `publish`; hoặc `create`/`draft` → `publish-existing` | Ký nháp dùng invoiceId + serial; Ikey là ID PMS; health-check chỉ kiểm tra cấu hình |

FAST/EasyInvoice health-check thành công trả `connected:false`, `connectionVerified:false`, `configurationValid:true`; đây là kết quả hợp lệ của bước kiểm tra cấu hình. Những NCC có login thật trả `connected:true` khi thành công; giá trị đó cũng chưa chứng minh phát hành thành công.

Form-serials hiện hỗ trợ MISA, Minvoice, VNPT và VNPT_V5, chỉ `invoiceType:0`. Không tự gọi cho mọi site. Đối với MISA, năm truy vấn là năm hiện tại UTC+7, không gửi `year`; các trường số lượng mặc định không chứng minh còn hóa đơn.

MISA/FAST có nhánh nhận diện MTT khi ký tự thứ ba từ cuối của serial là `M`; riêng MISA còn có điều kiện mode. Không sửa ký hiệu để ép nhánh. Với MISA HSM thường, phải dùng mẫu không thuộc MTT và mode 0 đã được xác nhận.

## 5. Body phát hành một hóa đơn

Ví dụ dưới đây dành cho **MISA HSM thông thường**. Các tên trường là hợp đồng canonical dùng chung, nhưng không copy nguyên giá trị sang Cyberbill/Minvoice/SUN hoặc chế độ ký khác. Mẫu dữ liệu tổng hợp, thuế 10% không phải quy định thuế cho mọi hàng hóa/dịch vụ.

File JSON đầy đủ, trùng body bên dưới: [examples/integration-misa-hsm.json](examples/integration-misa-hsm.json). Trước lần gửi đầu, thay invoiceId, ngày, mẫu/ký hiệu và dữ liệu nghiệp vụ theo sandbox được cấp.

```json
{
  "invoice": {
    "invoiceId": "d0187db1-1679-44f5-a95d-ec0b8e72d865",
    "invoiceType": 0,
    "formNo": "1",
    "serial": "C26TAA",
    "invoiceDate": "22/10/2026",
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
  "details": [{
    "transactionDate": "22/10/2026",
    "itemCode": "SERVICE-TEST",
    "itemName": "Dịch vụ thử nghiệm tích hợp",
    "unitName": "Lần",
    "quantity": 1,
    "price": 100000,
    "discountAmount": 0,
    "discountPercent": 0,
    "subAmount": 100000,
    "serviceRate": 0,
    "serviceCharge": 0,
    "beforeTaxAmount": 100000,
    "taxRate": 10,
    "taxAmount": 10000,
    "afterTaxAmount": 110000,
    "exciseTaxRate": 0,
    "exciseTaxCharge": 0,
    "itemType": "1"
  }],
  "taxSummaries": [{"beforeTaxAmount":100000,"taxRate":10,"taxAmount":10000}],
  "serviceSummaries": [],
  "specialTaxSummaries": [],
  "extraData": [],
  "electronicInvoiceType": 0
}
```

Mapping tối thiểu cần làm rõ trong dự án tích hợp:

| Nhóm | Trường | Quy tắc |
|---|---|---|
| Định danh | invoiceId | ID hóa đơn bền vững do dự án cấp; UUID dạng D thuận tiện cho các NCC yêu cầu GUID |
| Mẫu/ngày | formNo, serial, invoiceDate | Dùng mẫu và ngày hợp lệ của môi trường; không lấy số hóa đơn trả về làm invoiceId |
| Người mua | customerName, customerAddress, customerPhone, customerEmail | Theo nghiệp vụ; email có thể kích hoạt gửi email NCC trong luồng phát hành |
| Doanh nghiệp mua | companyName, customerTax | Tên đơn vị và MST người mua, khác tên cá nhân |
| Thanh toán | currencyCode, exchangeRate, paymentMethod, paymentMethodCode | Theo NCC; không dùng paymentMethodCode thay mã số paymentMethod Cyberbill |
| Tổng tiền | subAmount, beforeTaxAmount, taxAmount, afterTaxAmount, serviceCharge | Truyền các giá trị đã tính và duyệt; gateway không tự sửa mọi tổng tiền |
| Chi tiết | details[] | Ít nhất một dòng; đồng bộ số lượng, đơn giá, chiết khấu, phí và thuế với tổng |
| Tổng hợp | taxSummaries, serviceSummaries, specialTaxSummaries | Theo mức thuế/phí; không bỏ dữ liệu cần thiết khi có nhiều thuế suất hoặc phí |
| Mở rộng | hotelExtra, extraData, isItemSpecialTax | Chỉ dùng khi cần và đã xác nhận mapping; không tự thêm field vào JSON |

Schema đầy đủ: [OpenAPI](openapi.misa.json), endpoint `GET /openapi/einvoice.json`; `/openapi/misa.json` cũng trả cùng file hiện tại. Schema có metadata `x-providers`; đọc kết hợp capabilities và tài liệu NCC. Không suy ra field nullable là có thể bỏ trong mọi nghiệp vụ.

## 6. Gọi API và đọc kết quả

`POST /api/eInvoice/publish` cho NCC có luồng tạo và phát hành trực tiếp. Response HTTP 200 điển hình:

```json
{
  "provider": "MISA",
  "siteId": "11111111-1111-1111-1111-111111111111",
  "data": [{
    "invoiceId": "d0187db1-1679-44f5-a95d-ec0b8e72d865",
    "providerInvoiceNumber": "000001",
    "transactionId": "synthetic-transaction-id",
    "searchCode": "synthetic-search-code",
    "taxAuthorityCode": null,
    "invoiceData": null,
    "isSuccess": true,
    "providerErrorCode": null,
    "providerMessage": null
  }],
  "requestId": "server-correlation-id"
}
```

Chỉ xác nhận phát hành khi operation có nghĩa phát hành/ký, response đúng site/provider, item đúng invoiceId, `isSuccess:true`, và có bằng chứng theo NCC. Nếu `requiresSigning:true`, kết quả mới là chưa ký. Không đánh dấu phát hành chỉ vì HTTP 200, tạo nháp thành công, có PDF preview hoặc journal completed.

**Kiểu `data` khác nhau theo provider/operation**: MISA, Viettel, SUN, Minvoice và EasyInvoice trả mảng InvoiceResult cho các luồng hóa đơn đã mô tả; Cyberbill trả một InvoiceResult; FAST single trả một object, batch trả mảng; VNPT/VNPT_V5 có thể trả object hoặc mảng tùy nhánh. Client chuẩn hóa object thành một phần tử khi operation cần InvoiceResult, rồi kiểm tra correlation. Không áp dụng bước chuẩn hóa này cho document, status hoặc các response `{success:true}`. Không giả định mọi JSON dưới `data` đều là mảng.

Lưu các field kết quả khi có: `providerInvoiceNumber` (string, giữ số 0 đầu), `providerInvoiceCode`, `transactionId`, `searchCode`, `taxAuthorityCode`, `requiresSigning`, `providerErrorCode`, `providerMessage`, `requestId`. Các mã này có mục đích khác nhau, không dùng thay thế tùy ý. `requiresSigning` có thể vắng mặt; vắng mặt không đủ để kết luận đã ký. Mã CQT có thể chưa có ngay; trạng thái CQT cần theo dõi riêng.

Batch body là `{"invoices":[<CreateInvoiceRequest>, ...]}` với một key cho toàn bộ body; nếu dùng siteConfigInfo thì nằm ở wrapper theo schema. HTTP 200 có thể chứa cả item lỗi. Ghép theo invoiceId, kiểm tra trùng/thiếu/ID lạ; không dựa thứ tự. Không tự gửi lại toàn batch hoặc đổi key cho item lỗi. Điều kiện đồng nhất theo NCC: MISA cùng site/type/mode/form/serial, FAST cùng serial; VNPT cùng type/form/serial, tối đa 500 và invoiceId GUID D. Viettel có quy tắc riêng.

### Luồng tạo rồi ký

Sau `create`/`draft`, lưu kết quả bước đầu trước khi gọi `publish-existing`. Mỗi operation có key và snapshot riêng. Khi bước ký lỗi, tra cứu bước ký; không tự tạo lại hóa đơn nháp.

Body `publish-existing` theo NCC (mọi ví dụ dùng `invoiceType:0`):

```json
{"invoiceId":"<ID PMS>","providerInvoiceCode":"<mã NCC từ create>","invoiceType":0}
```

Ví dụ trên dành cho Cyberbill. Minvoice cần thêm `providerInvoiceNumber` từ nháp; EasyInvoice dùng `invoiceId` + `serial`; VNPT/VNPT_V5 dùng `invoiceId` + `formNo` + `serial`. Không gửi nguyên placeholder hoặc lấy một body dùng cho mọi NCC.

Cyberbill tạo/điều chỉnh/thay thế trả `requiresSigning:true`; ký riêng thành công trả false. VNPT_V5 `create` có thể phát hành theo baseline, vì vậy **không coi mọi route tên create là tạo nháp**. Minvoice ký nháp giữ số theo request baseline; dùng `invoices` và UAT để đối chiếu trạng thái ký thực tế.

## 7. Lưu trữ và chống phát hành trùng

Thiết kế sau là đề xuất cho **database dự án tích hợp**, không phải API hoặc schema bắt buộc của gateway.

Lưu trước HTTP: tenant/site, provider dự kiến, businessInvoiceId, invoiceId, operation, idempotencyKey, snapshot JSON, thời điểm gửi và trạng thái nội bộ. Lưu sau HTTP: mã NCC, response đã kiểm soát truy cập, requestId, lỗi và lịch sử lần gửi. Không lưu key API vào bản ghi lần gửi.

Dùng transaction/unique constraint và khóa worker theo `(siteId, invoiceId, giai đoạn nghiệp vụ)` để hai worker/nút bấm không tạo hai lần gửi. Lưu key trước khi network bắt đầu; restart phải phục hồi đúng key và body. Không giữ transaction database mở suốt thời gian chờ NCC.

Trạng thái nội bộ có thể là `Ready`, `Sending`, `DraftCreated`, `Issued`, `NeedsReconciliation`, `Rejected`; đó là lựa chọn của dự án, **không phải enum gateway**. Gateway journal hiện chỉ có `pending`, `completed`, `not-found`; chưa có API reconcile/unlock tự động.

Pseudo-code cho worker phát hành trực tiếp:

```text
1. Kiểm tra tenant/site/capability và hóa đơn đủ dữ liệu.
2. Trong transaction: lấy hoặc tạo attempt duy nhất, snapshot + key; khóa quyền gửi.
3. Nếu attempt cũ Sending/NeedsReconciliation: tra cứu journal trước, không tạo attempt mới.
4. POST một lần bằng key/body đã lưu; tắt retry POST ở HTTP client/proxy/job middleware.
5. Nếu có response hợp lệ: kiểm tra HTTP, envelope, operation và từng item.
6. Lưu Issued/DraftCreated/lỗi tương ứng, mã NCC và requestId.
7. Nếu timeout/ngắt kết nối/mất response/response hỏng hoặc lỗi sau gửi:
   lưu NeedsReconciliation; tra cứu journal bằng operation + key gốc.
8. Chỉ lấy lại kết quả khi journal completed có result hợp lệ.
   pending/not-found hoặc kết quả không đủ bằng chứng: giữ cần đối soát.
```

Không để cơ chế job retry tự POST lại trong bước 7. Có thể chạy job tra cứu GET với số lần/tổng thời gian hữu hạn và backoff cấu hình được. Hết hạn chờ thì hiển thị cần đối soát, không polling vô hạn.

### Tra cứu journal

```http
GET /api/eInvoice/operations/publish/<idempotencyKey>
GET /api/eInvoice/invoice-attempts?invoiceId=<URL-encoded-id>&phase=publish
```

GET operations dùng operation slug thay `/` bằng `-`, ví dụ `publish-existing`, `adjust-increase`, `xml-publish`, `tax-send-pending`. Invoice-attempts chỉ nhận phase `publish` hoặc `xml-create`; các bước draft/create/delete/void tra theo operations. Cả hai API đọc journal, không truy vấn NCC hoặc mở khóa.

Response journal ở cấp gốc, **không có wrapper data**:

```json
{
  "attemptId": "synthetic-attempt-id",
  "operation": "publish",
  "idempotencyKey": "integration-publish-0001",
  "state": "completed",
  "updatedAtUtc": "2026-10-22T03:00:00Z",
  "result": [{"invoiceId":"d0187db1-1679-44f5-a95d-ec0b8e72d865","providerInvoiceNumber":"000001","transactionId":null,"searchCode":"synthetic-search-code","taxAuthorityCode":null,"invoiceData":null,"isSuccess":true,"providerErrorCode":null}],
  "lastError": null,
  "originalRequestId": "original-server-correlation-id",
  "requiresReconciliation": false,
  "canRetry": false,
  "provider": "MISA"
}
```

`result` là dữ liệu operation đã lưu, tương ứng nội dung `data` của POST; không phải toàn envelope POST. `provider` của record cũ có thể null. Kiểm tra kiểu result theo operation.

| State | Xử lý |
|---|---|
| completed | Đọc result và diễn giải đúng operation/từng item; có thể lấy lại kết quả đã lưu. Gửi lại cùng key/body để replay completed không gọi NCC lần hai, nhưng ưu tiên GET |
| pending | Đã đặt reservation/chưa có kết quả cuối đủ xác nhận; giữ cần đối soát, canRetry=false; không đổi key/ID/provider/site |
| not-found | Chưa tìm thấy record trong journal này; canRetry=null, **không chứng minh NCC chưa phát hành**. Kiểm tra site, operation, key, instance/storage, claim invoice và bằng chứng NCC trước quyết định gửi |

Lỗi validation/login trước business write có thể giải phóng reservation; lỗi sau dispatch giữ pending kể cả NCC trả lỗi. `lastError` giúp chẩn đoán, không chứng minh kết quả cuối. Dù POST bị hủy bởi caller, NCC vẫn có thể đã xử lý. Không xóa claim, reset journal hoặc sinh invoiceId mới để vượt khóa.

Journal hiện lưu local, bền qua restart khi giữ đúng thư mục. Chưa có storage dùng chung nhiều instance. Khi cấu hình load balancing, phải thống nhất nơi xử lý/tra cứu với đơn vị vận hành; không giả định idempotency xuyên nhiều server có journal độc lập.

## 8. Lỗi và thời gian chờ

Lỗi thường là `application/problem+json` có `title`, `status`, `code`, `requestId`; một số lỗi framework dùng code `HTTP_400`/`HTTP_401`. Đọc body lỗi trước khi chuyển thành exception của ứng dụng.

```json
{
  "title": "A prior attempt has an uncertain outcome",
  "status": 409,
  "code": "MISA_OUTCOME_UNCERTAIN",
  "errorSource": "gateway",
  "requestId": "server-correlation-id"
}
```

Các field tùy tình huống: `providerCode`, `providerMessage`, `providerHttpStatus`, `attempt`, `previousAttempt`, `attemptStatusUrl`. Nếu có URL tra cứu, chỉ chấp nhận đường dẫn tương đối của gateway hoặc URL cùng origin đã cấu hình trước khi đính kèm API key. Không gửi key sang hostname từ dữ liệu lỗi.

| HTTP | Xử lý phía tích hợp |
|---|---|
| 400 | Sửa schema/kiểu dữ liệu/trường bắt buộc hoặc thiếu Idempotency-Key; kiểm tra tình trạng attempt trước khi sửa body đã gửi |
| 401 | Kiểm tra HTTPS và API key còn hạn; không tự đổi sang HTTP |
| 403 | Site ngoài quyền key; sửa mapping/cấp quyền, không đổi site để vượt lỗi |
| 404 | Có thể thiếu site/document/route; đọc code. Journal không có record trả state not-found, không mặc định là HTTP 404 |
| 409 | Xung đột key/body hoặc hóa đơn đã có attempt; đọc previousAttempt/attemptStatusUrl, tra cứu và đối soát |
| 422 | NCC từ chối nghiệp vụ; lưu code/message đã lọc; kiểm tra journal, không tự phát hành lại |
| 501 | Provider/operation không hỗ trợ; đối chiếu capabilities, không đoán route khác |
| 502/504 | Response NCC lỗi/không đủ dữ liệu hoặc timeout; với write coi là có thể đã gửi, tra journal |
| 500/503 | Lỗi nội bộ/storage/cấu hình/host allowlist; lưu requestId, phối hợp quản trị; không retry write mù |

Gateway và NCC có HTTP status riêng: `providerHttpStatus:200` vẫn có thể đi cùng HTTP gateway 422. Batch lỗi từng item dùng `providerErrorCode`, còn Problem Details dùng `providerCode` khi có; không đánh đồng hai field.

Không đặt một timeout ngắn chung cho mọi luồng: Minvoice login nghiệp vụ tối đa 100 giây rồi Save/SaveSign có thể tới 300 giây mỗi call; VNPT SOAP 60 giây mỗi call; nhiều NCC khác 100 giây mỗi call. Một operation có thể gồm nhiều call. Một số publish MISA giữ timeout provider vô hạn theo baseline nhưng hỗ trợ caller cancellation. Backend/proxy/job cần deadline phù hợp và chuyển sang đối soát khi hết hạn, không biến timeout thành kết luận chưa phát hành.

## 9. Tải tài liệu và trạng thái CQT

`POST /api/eInvoice/document`, body **phụ thuộc NCC**. Ví dụ MISA hóa đơn thông thường:

```json
{"invoiceType":0,"searchCode":"<searchCode đã lưu>","typeFileDownload":"pdf"}
```

Minvoice cần providerInvoiceCode; Cyberbill cần invoiceId + providerInvoiceCode; Viettel cần invoiceId + providerInvoiceNumber + formNo; SUN dùng searchCode; EasyInvoice dùng invoiceId + serial; FAST dùng invoiceId + serial + searchCode; VNPT dùng invoiceId + serial và thông tin loại/mode theo tài liệu NCC.

Response thường là `{"provider":"...","siteId":"...","data":{"content":"...","format":"base64"},"requestId":"..."}`. Một số nhánh trả `format:"url"`. Base64 là nội dung file cần giải mã; URL là địa chỉ tài liệu, không phải base64. Không chuyển API key của gateway sang URL NCC. Preview thành công không xác nhận hóa đơn đã phát hành; preview VNPT/VNPT_V5 còn tạo nháp và yêu cầu Idempotency-Key.

`POST /status` hiện có MISA/VNPT/VNPT_V5, body SendToTaxAuthorityRequest chứa invoiceIds và thông tin mẫu/loại theo NCC. Giữ `taxAuthorityStatus` số do gateway trả, không tự đặt nghĩa enum chưa xác nhận. Minvoice `/invoices` là trạng thái ký NCC, không thay status CQT. Search/invoices/status không tự reconcile hoặc mở khóa journal.

**Không gọi `tax/send-pending` mặc định sau publish**: đây là luồng MISA tác động hàng đợi pending theo site/AppID, không lọc theo invoiceIds. VNPT_V5 `/tax/send` có danh sách ID và quy tắc riêng. USB MISA cần XML/hash/chữ ký bên ngoài; xem MISA-API trước khi triển khai.

## 10. cURL thử nghiệm

Ví dụ Bash, lấy cấu hình từ biến môi trường do người phụ trách cấp; không lưu key vào file Git. PowerShell dùng `curl.exe` và cú pháp biến/xuống dòng phù hợp. `invoice-body.json` là snapshot đã chuẩn bị, ví dụ lấy từ examples/integration-misa-hsm.json rồi sửa trước lần gửi đầu.

```bash
curl --silent --show-error "$EINVOICE_BASE_URL/health/live"

curl --silent --show-error "$EINVOICE_BASE_URL/api/eInvoice/capabilities" \
  -H "X-Api-Key: $EINVOICE_API_KEY" -H "X-Site-Id: $EINVOICE_SITE_ID"

curl --silent --show-error -X POST "$EINVOICE_BASE_URL/api/eInvoice/health-check" \
  -H "X-Api-Key: $EINVOICE_API_KEY" -H "X-Site-Id: $EINVOICE_SITE_ID" \
  -H 'Content-Type: application/json' --data '{}'

curl --silent --show-error -X POST "$EINVOICE_BASE_URL/api/eInvoice/form-serials" \
  -H "X-Api-Key: $EINVOICE_API_KEY" -H "X-Site-Id: $EINVOICE_SITE_ID" \
  -H 'Content-Type: application/json' --data '{"invoiceType":0}'

# Chỉ chạy cho đúng NCC/luồng trực tiếp được cấp; key phải được lưu trước HTTP.
curl --silent --show-error -X POST "$EINVOICE_BASE_URL/api/eInvoice/publish" \
  -H "X-Api-Key: $EINVOICE_API_KEY" -H "X-Site-Id: $EINVOICE_SITE_ID" \
  -H "Idempotency-Key: $EINVOICE_IDEMPOTENCY_KEY" \
  -H 'Content-Type: application/json' --data-binary '@invoice-body.json'

curl --silent --show-error "$EINVOICE_BASE_URL/api/eInvoice/operations/publish/$EINVOICE_IDEMPOTENCY_KEY" \
  -H "X-Api-Key: $EINVOICE_API_KEY" -H "X-Site-Id: $EINVOICE_SITE_ID"
```

Không chạy cả block như script tự động: form-serials phụ thuộc capabilities, publish phát sinh nghiệp vụ. cURL trên không có retry và không bỏ kiểm tra TLS. Dự án nên dùng collection NCC tương ứng trong [postman](postman), gửi từng request sau khi kiểm tra dữ liệu.

## 11. Công việc AI cần bàn giao trong dự án tích hợp

1. Đọc cấu trúc/AGENTS của dự án nhận, xác định backend, database, cơ chế job, tenant và nguồn hóa đơn; dùng convention sẵn có.
2. Tạo cấu hình và gateway client backend: auth/site headers theo request, JSON strict, parse envelope/problem/journal/document, timeout có kiểm soát và không retry write tự động.
3. Tạo mapping domain → canonical DTO có kiểm tra tiền/ngày/ID; ghi bảng mapping cho trường chưa rõ, không tự chọn công thức thuế/làm tròn.
4. Tạo migration/storage attempt, snapshot và key bền vững; unique/concurrency guard, khôi phục qua restart. Không lưu secret trong attempt/log.
5. Thêm use case/API nội bộ có phân quyền: cấu hình/capabilities, chọn mẫu, phát hành, xem kết quả, tra cứu attempt, tải tài liệu; chọn hai bước tạo/ký khi NCC yêu cầu.
6. Hiển thị rõ đã phát hành, nháp/chưa ký, bị từ chối và cần đối soát; thao tác bấm lại không sinh invoice/key mới cho lần gửi chưa xác định.
7. Thêm kiểm thử cần thiết và hướng dẫn chạy/cấu hình. Liệt kê các phần thiếu để chạy sandbox; báo kết quả kiểm chứng thực tế và giới hạn còn lại.

Các ca cần kiểm chứng bằng fake HTTP, không phát hành thật trong CI:

| Tình huống | Kỳ vọng |
|---|---|
| Publish thành công | Ghép đúng ID, lưu đủ mã NCC và trạng thái |
| Hai worker/nút bấm đồng thời | Một attempt nghiệp vụ, key/snapshot không đổi |
| Crash/restart sau gửi | Phục hồi key cũ, tra journal trước hành động tiếp theo |
| Timeout rồi completed | Lấy result, cập nhật hóa đơn; không POST mới |
| pending hoặc not-found | Cần đối soát; không tự đổi key/ID |
| 409 + previousAttempt | Tra đúng attempt/claim và hiển thị lỗi hữu ích |
| 401/403/422/502/504 và response hỏng | Không báo phát hành thành công; lưu requestId, không lộ secret |
| HTTP 200 có item lỗi/thiếu/ID lạ | Không đánh dấu cả lô thành công |
| Create chưa ký | Chỉ DraftCreated; ký dùng đúng mã và key bước ký |
| Site khác tenant | Backend chặn, không chuyển cấu hình tùy ý từ frontend |
| PDF base64/URL | Xử lý theo format; không gửi key sang URL NCC |

Sandbox chỉ được coi đã xác nhận khi số hóa đơn, mã tra cứu, PDF và trạng thái ký trên NCC khớp dữ liệu lưu của dự án. Health-check và test offline không thay thế bước đó.

## 12. Tài liệu đối chiếu và prompt giao việc

Prompt có thể đưa trực tiếp cho AI dự án nhận: [examples/integration-agent-prompt.md](examples/integration-agent-prompt.md). Khi chuyển sang máy khác, gửi cả bộ docs/Postman/OpenAPI, không chỉ đường dẫn ổ D.

- [MISA-API](MISA-API.md), [MISA-VARIANTS](MISA-VARIANTS.md), [hướng dẫn MISA từng bước](INTEGRATION-AGENT.md).
- [VIETTEL-API](VIETTEL-API.md), [SUN-API](SUN-API.md), [MINVOICE-API](MINVOICE-API.md).
- [CYBERBILL-API](CYBERBILL-API.md), [EASYINVOICE-API](EASYINVOICE-API.md), [FAST-API](FAST-API.md), [VNPT-API](VNPT-API.md).
- [API-CONTRACT](API-CONTRACT.md), [LOCAL-SETUP](LOCAL-SETUP.md), [OpenAPI](openapi.misa.json), [STATUS](STATUS.md), [QUESTIONS](QUESTIONS.md).

Một số đoạn lịch sử trong tài liệu cũ phản ánh thời điểm chỉ có MISA hoặc ít provider hơn. Khi có khác biệt, đối chiếu route/capabilities/schema và source đang triển khai; không coi các số test/URL/key lịch sử là trạng thái live. Mã nguồn trọng tâm nếu AI có quyền đọc repo: `src/eInvoice.Api/Program.cs`, `MisaEndpoints.cs`, `ProviderEndpoints.cs`, `ProviderDispatcher.cs`, `src/eInvoice.Core/Contracts` và adapter của NCC tương ứng. Dự án nhận gọi HTTP; không cần tham chiếu assembly nội bộ gateway.
