# Bộ API Viettel

Ngày cập nhật 11/10/2026. Partner **4**, chọn adapter theo cấu hình SiteId trên server. Đã port **13 action sống** của dispatcher baseline `66a9c74`, qua 12 operation HTTP, trong đó Login/HealthCheck dùng chung health-check. Chưa UAT hoặc gọi Viettel thật.

API dùng chung body camelCase, X-Api-Key, X-Site-Id, HTTPS như [API-CONTRACT](API-CONTRACT.md). Site MISA đang có giữ nguyên; tạo site Viettel riêng và cấp quyền API key cho site đó. Không gửi mật khẩu/token NCC trong body PMS.

## Cấu hình server

Trong JSON sites local thêm site riêng có `site.partner: 4`, `partnerUrl` là URL nền HTTPS của Viettel có `/` cuối, `taxNumber`, `username`, `password`; các trường còn lại theo [sites.example.json](sites.example.json). Không đưa file thật vào Git. Thêm đúng hostname do quản trị cung cấp vào `Providers:Viettel:AllowedHosts` trong appsettings.Local.json; mặc định rỗng nên endpoint chưa cấu hình bị chặn. Không đoán endpoint sandbox/production hoặc tắt TLS.

Ví dụ cấu hình allowlist **tổng hợp, không dùng để gọi thật**:

```json
{"Providers":{"Viettel":{"AllowedHosts":["viettel.example.test"]}}}
```

Thay hostname bằng hostname môi trường được cấp. Restart khi sửa allowlist/key; JSON site được đọc ở request tiếp theo. Nếu client ở máy khác cần URL HTTPS gateway truy cập được; local vẫn loopback.

Viettel login theo baseline: POST `auth/login`, JSON `{username,password}`, lấy `access_token`, business gửi **Cookie `access_token=<token>`**. Token chỉ dùng trong một operation, không cache/dùng header chung giữa các khách sạn. Fresh health-check chỉ trả connected, không trả token. Các call dùng UTF-8, timeout 100 giây theo default HttpClient baseline, không retry/redirect hoặc lưu cookie tự động.

## Endpoint đã triển khai

| Route dưới `/api/eInvoice` | Body | Ý nghĩa | Idempotency-Key |
|---|---|---|---|
| POST health-check | `{}` / ProviderHealthCheckRequest | Login Viettel thực tế | Không |
| POST publish | CreateInvoiceRequest | Tạo và phát hành | Có |
| POST publish-batch | InvoiceBatchRequest | Tạo/phát hành lô, kiểm tra từng isSuccess | Có |
| POST draft | CreateInvoiceRequest | Lưu/cập nhật nháp; không xác nhận đã phát hành | Có |
| POST preview | AdjustOrReplaceInvoiceRequest | Preview PDF Base64 | Không |
| POST adjust/increase | AdjustOrReplaceInvoiceRequest | Điều chỉnh tăng | Có |
| POST adjust/decrease | AdjustOrReplaceInvoiceRequest | Điều chỉnh giảm, giữ dấu tiền baseline | Có |
| POST adjust/information | AdjustOrReplaceInvoiceRequest | Điều chỉnh thông tin | Có |
| POST replace | AdjustOrReplaceInvoiceRequest | Thay thế | Có |
| POST document | DownloadPdfRequest | Tải PDF Base64 | Không |
| POST void | VoidInvoiceRequest | Hủy; wire form-urlencoded | Có |
| POST search | SearchInvoiceRequest | Tra cứu transactionUuid bằng invoiceId PMS; wire form-urlencoded | Không |

`GET /api/eInvoice/capabilities` trả đúng catalog của site hiện tại; không gọi NCC. `GET /api/eInvoice/misa/capabilities` là catalog MISA riêng, không dùng để suy ra capability Viettel. Schema chung tại `GET /openapi/einvoice.json`; URL `/openapi/misa.json` cũ vẫn phục vụ cùng schema, operation có x-providers để xác định nhà cung cấp hỗ trợ.

Viettel baseline **không có** action FormSerial hoặc status CQT. `/form-serials`, `/status`, `/adjust` chung và các route USB/CQT của MISA trả 501 khi site là Viettel. Không thêm capability vì chỉ thấy model/helper trong code cũ. Mẫu/ký hiệu Viettel phải lấy từ môi trường Viettel/quản trị đã cấu hình.

## Body và mapping

Hóa đơn Viettel hiện hỗ trợ `invoiceType: 0`. Body có invoice/details/taxSummaries/extraData như mẫu chung; cần ID/mẫu/ký hiệu/ngày/currency và tổng tiền, details có quantity/price. Precision giá 0..28. Không tự tính lại hoặc đổi công thức thuế/chiết khấu/tiền từ baseline.

- `invoiceId` PMS → `generalInvoiceInfo.transactionUuid`; key wire vẫn `transactionUuid`.
- Ngày `dd/MM/yyyy` → epoch milliseconds của **00:00 UTC+7**, không phụ thuộc timezone máy (người dùng duyệt 11/10). Định dạng số dùng invariant culture trong phạm vi mapper; không đổi culture của toàn bộ API hoặc mapper MISA.
- `BankAccount/BankName` → buyerBankAccount/buyerBankName, theo baseline mới nhất; không lấy PaymentBankAccount/PaymentBankName.
- Giữ Validation baseline: khi Validation thiếu/0, wire exchangeRate = 1 và gửi totalAmountAfterDiscount. Không tự sửa hành vi ngoại tệ.
- Giữ Math.Round mặc định của Viettel (ToEven), dấu tiền điều chỉnh giảm, BuyerNotGetInvoice default và quy tắc metadata Customername/FolioNum.
- Điều chỉnh/thay thế cần `originalProviderInvoiceNumber` và ID mới của hóa đơn kết quả. Các route tách tăng/giảm/thông tin để không đoán kiểu điều chỉnh; wire adjustmentType/adjustmentInvoiceType giữ baseline.
- Document cần `invoiceId`, `providerInvoiceNumber`, `formNo`, `invoiceType:0`; chỉ PDF. Search cần `invoiceId`.
- Void cần providerInvoiceNumber/formNo/invoiceDate/reason. Nếu bỏ additionalReferenceDate, dùng ngày hiện tại UTC+7; additionalReferenceDesc wire giữ mẫu hủy của baseline.
- Batch dùng site ở wrapper, không nhận siteConfigInfo từng item. ID phải khác nhau; từng item phải hợp lệ. Giữ payload mỗi invoice, không ép tất cả cùng mẫu/mode như nhánh MISA.

## Kết quả, lỗi và journal

Response thành công chuẩn `{provider:"VIETTEL",siteId,data,requestId}`. Publish trả mảng InvoiceResult: providerInvoiceNumber từ invoiceNo, searchCode từ reservationCode, taxAuthorityCode từ codeOfTax. Không tự tạo transactionId khi NCC không trả. Preview/document trả `{content,format:"base64"}`; draft trả item thành công của thao tác nháp, không có số phát hành là hợp lệ cho draft.

Single phải có envelope hợp lệ và invoiceNo khi phát hành thành công. Batch phải đủ đúng các transactionUuid, không lặp/không nhận ID ngoài request; lỗi từng item được giữ `isSuccess:false` và providerCode/message đã lọc, không báo toàn bộ thành công. Không giả định thứ tự response trùng request.

HTTP 422 khi NCC từ chối, 502 khi response rỗng/hỏng/thiếu bằng chứng, 504 timeout, 503 endpoint/host không được cấu hình, 400 credentials/body thiếu, 501 thao tác không hỗ trợ. Code bắt đầu `VIETTEL_` cho lỗi transport; providerCode chỉ trả các mã số HTTP-like đã xác định, mã khác thành VIETTEL_ITEM_REJECTED. providerMessage chỉ từ trường description đã che username/password/token và lọc payload có cấu trúc; không trả raw response.

Journal dùng hạ tầng chung đã tách từ MISA. Giữ nguyên hash/path đọc MISA cũ. Attempt Viettel có hash riêng theo provider/site/operation/key, **claim site/invoice/phase được dùng chung** để không vượt claim khi đổi provider. Record mới có field provider (MISA/VIETTEL); record lịch sử thiếu thì null. Không đổi trạng thái hiện tại: not-found/pending/completed; đã dispatch mà lỗi thì giữ pending, khóa và lastError qua restart.

- Key write 8..128 ASCII chữ/số/-/_, giữ nguyên key/body cho cùng lần gửi. Completed có thể replay kết quả, không gọi NCC lần hai.
- `GET /api/eInvoice/operations/publish/<key>` hoặc `GET /api/eInvoice/invoice-attempts?invoiceId=<id>&phase=publish` không gọi NCC; kết quả trực tiếp, không wrapper data.
- 409 có VIETTEL_INVOICE_ALREADY_ATTEMPTED hoặc VIETTEL_OUTCOME_UNCERTAIN và previousAttempt/link. IDEMPOTENCY_KEY_CONFLICT vẫn dùng chung. Không đổi key/ID hoặc xóa claim để gửi lại.
- Search gọi Viettel thật và có thể dùng để đối chiếu số hóa đơn đã phát hành; **không tự cập nhật/mở khóa journal**. Nếu search lỗi/không có result, không kết luận hóa đơn chưa tồn tại. Chưa có endpoint reconcile/unlock.
- Claim draft và publish tách phase để nháp không chặn bước phát hành; tra cứu draft theo `/operations/draft/<key>`. API invoice-attempts chỉ hỗ trợ phase publish/xml-create như trước.
- Hủy claim theo số hóa đơn + mẫu trong site; không phụ thuộc invoiceId PMS tùy chọn. Lỗi login/validation trước business write giải phóng reservation. Sau write không retry tự động.

## Kiểm chứng và thử local

11 fixture payload từ frozen mapper baseline, capture offline riêng tại tools/ViettelFixtureCapture, không tham chiếu assembly provider runtime. Capture dùng canonical DTO bridges, tiền tổng hợp và ngày UTC+7; không phải golden production, không capture lại expected để làm test qua.

API test fake HTTP bao phủ 11 payload, login/cookie, auth/site/endpoint guards, response thiếu/sai ID/JSON, batch partial, timeout, login lỗi trước write, không log secret, replay/claims chung và bank mapping. UAT Viettel chưa làm; không có endpoint/credential Viettel thật được cấp trong phiên này.

Import [collection Viettel](postman/eInvoice-VIETTEL.postman_collection.json) và [environment template](postman/eInvoice-VIETTEL.environment.template.json). Đặt key/site/mẫu/ký hiệu/ngày được cấp; key test MISA cũ đã hết hạn, template Viettel chưa có key hoặc site thật. Không dùng nguyên ID mẫu cho một giao dịch đã gửi; khi mất response phải giữ ID/key gốc và tra cứu.
