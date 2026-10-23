# Minvoice API

Provider `MINVOICE`, `site.partner = 2`. Đã port 15 action dispatcher baseline; fixture và HTTP giả lập không phải UAT Minvoice thật. Bản nguồn cố định `66a9c742d0f6fb692d339076e6cd82780e3476be`.

## Cấu hình và xác thực

JSON server theo SiteId như các adapter khác. `partnerUrl` là URL HTTPS có slash cuối; hostname khai báo tại `Providers:Minvoice:AllowedHosts` trong appsettings.Local.json. Allowlist mặc định rỗng. Credential thật chỉ ở file local ignored/secrets. Không dùng site MISA hiện tại thay cho Minvoice.

D024 người dùng chọn giữ các luồng auth cũ để đối chiếu: health-check login bằng `username`, `password`, `ma_dvcs = username2`; nghiệp vụ login với `ma_dvcs = "VP"` và gửi header `Authorization: Bear <token>`. Health-check chỉ xác nhận login thành công, không trả token Bearer cho PMS. Token riêng trong mỗi thao tác, không cache hoặc static cross-tenant. Địa chỉ login chuẩn hóa Uri tới `api/Account/Login`, không giữ dấu slash kép ngẫu nhiên của helper cũ. Chưa thống nhất thành Bearer.

Nghiệp vụ login tối đa 100 giây. Health login, Save và SaveSign tối đa 300 giây mỗi HTTP call; các call khác 100 giây. Cancellation client được truyền xuyên suốt, không redirect/bypass TLS hoặc tự retry ghi. Registry thuế cố định `mst.minvoice.com.vn` cần allowlist riêng; vẫn login nghiệp vụ trước lookup nhưng **không chuyển token/credential sang registry**.

## Route

Mọi route dưới `/api/eInvoice`, dùng HTTPS, `X-Api-Key`, `X-Site-Id`. Các thao tác ghi cần `Idempotency-Key` ổn định 8–128 ký tự; giữ nguyên body khi tra cứu/replay.

| POST route | DTO/field chính | Endpoint NCC | Ghi |
|---|---|---|---|
| health-check | `{}` | Account/Login | Không |
| form-serials | invoiceType=0 | Invoice68/GetTypeInvoiceSeries, GET | Không |
| publish | CreateInvoiceRequest | InvoiceApi78/SaveSign, editmode=1 | Có |
| create | CreateInvoiceRequest | InvoiceApi78/Save, editmode=1 | Có |
| change | AdjustOrReplaceInvoiceRequest | InvoiceApi78/Save, editmode=2 | Có |
| delete | DeleteInvoiceRequest, serial, providerInvoiceNumber | InvoiceApi78/Save, editmode=3 | Có |
| publish-existing | PublishExistingInvoiceRequest | InvoiceApi78/Sign | Có |
| adjust | AdjustOrReplaceInvoiceRequest | InvoiceApi78/DieuChinh | Có |
| replace | AdjustOrReplaceInvoiceRequest | InvoiceApi78/ThayThe | Có |
| void | VoidInvoiceRequest | InvoiceApi78/HuyHoaDon | Có |
| document | DownloadPdfRequest | InvoiceApi78/PrintInvoice?id=…, GET | Không |
| invoices | GetInvoicesRequest | InvoiceApi78/GetInvoices | Không |
| tax-code | GetTaxCodeInfoRequest | System/SearchTaxCode?tax=…, GET registry riêng | Không |
| email | SendEmailRequest | Invoice68/SendEmailMulti | Có |

Login và HealthCheck cùng route health-check, capabilities đủ 15 action qua 14 route. Không có batch/preview/draft/tăng–giảm–thông tin/USB trong dispatcher Minvoice; không giả lập chúng. `create` tạo hóa đơn chưa ký; `/publish` tạo và ký trực tiếp, `/publish-existing` ký hóa đơn nháp có sẵn.

## ID và body

Minvoice dùng GUID. `invoice.invoiceId` khi tạo/thay thế phải là GUID ổn định vì gửi vào `hoadon68_id`; invoiceType=0, serial, ngày `dd/MM/yyyy`, ít nhất một detail. Giữ công thức tiền, tax -1 khi null, làm tròn/default và case/key wire, kể cả `ortherFee`. Không đổi key NCC để sửa chính tả.

Response InvoiceResult giữ `invoiceId` PMS để correlation và có **`providerInvoiceCode`** là GUID `hoadon68_id` NCC. Lưu cả hai, không thay mã PMS bằng GUID NCC. Thao tác có gửi hoadon68_id kiểm tra GUID response khớp. Điều chỉnh baseline không gửi ID mới, chỉ `inv_InvoiceAuth_id` của hóa đơn gốc; GUID kết quả có thể do NCC cấp.

- `change`, `replace`, `adjust` cần `originalProviderInvoiceCode` là GUID NCC gốc; `originalInvoiceId` vẫn là ID PMS, không dùng thay thế âm thầm. Change/replace cần originalProviderInvoiceNumber; change thay ID/number trong payload như baseline, replace dùng inv_originalId. Adjust body **object phẳng**, detail `stt` và `ma_thue` **string**, không áp dụng multiplier tăng/giảm hoặc đóng gói data master.
- `publish-existing`: invoiceId PMS, providerInvoiceCode NCC, providerInvoiceNumber đã có, invoiceType=0. Baseline Sign chỉ xác nhận code=00; số trong response được giữ từ request, chưa kiểm chứng độc lập việc NCC cấp số khi ký. Tra cứu invoices để đối chiếu trạng thái ký.
- `document`: invoiceType=0, providerInvoiceCode; typeFileDownload bỏ qua hoặc pdf. Query `id` được escape, dùng GUID NCC.
- `void`: invoiceId PMS, providerInvoiceCode, providerInvoiceNumber, invoiceType=0. Baseline ghi_chu là “Hủy hóa đơn số …”, ngày văn bản UTC+7; không chuyển Reason vào key NCC mới.
- `delete`: invoiceId PMS, serial, providerInvoiceNumber, invoiceType=0; số phải là Int32 dương như model cũ. Chỉ xóa nháp; hủy hóa đơn đã ký dùng void.
- `email`: providerInvoiceCode GUID NCC, email người nhận; invoiceId PMS tùy chọn. Payload NCC giữ `id: [{id, nguoi_nhan}]`, type “Gửi hóa đơn”. Không gửi email tự động trong test hoặc retry.
- `invoices`: fromDate/toDate/serial. Ngày không offset coi là ngày nghiệp vụ Việt Nam; timestamp có offset/Z chuyển UTC+7 trước lọc. Response mảng providerInvoiceCode/signStatus/signedAt (offset +07:00); không suy ra is_success ngoài giá trị NCC hoặc mở khóa journal.
- `tax-code`: taxCode được escape. Response TaxCodeInfoResult ánh xạ các trường model baseline ten_cty/dia_chi/ma_so_thue/masothue_id/cqthuecap_tinh/cqthue_ql/nguoi_dai_dien/ngay_thanh_lap; không còn trả object rỗng như bug app service cũ. Payload registry thực tế cần UAT; ngày thành lập vẫn string, không đoán format.

Giữ hai ngoại lệ tổng phí dịch vụ theo SiteId trong baseline. Fixture riêng kiểm chứng chúng lấy tổng detail/ServiceRate dòng đầu; chưa chuyển sang quy tắc mới/config chung. SearchCode trống giữ thuật toán timestamp UTC + random cũ; khi tích hợp nên gửi một invoice.searchCode ổn định. Không tự dùng searchCode làm idempotency key hoặc phát hành lại sau timeout.

## Response và journal

NCC thành công cần `code: "00"`, đúng cấu trúc data; code numeric 0 không coi là "00". Publish/adjust/replace cần số hóa đơn và GUID kết quả. Save/SaveSign lấy sobaomat/macqt trong data; adjust/replace giữ vị trí top-level như baseline. FormSerial ánh xạ invoiceForm/khhdon; fromNo=1, các int thiếu khác 0 và tên/ngày null theo canonical DTO.

PDF cần HTTP 2xx và bytes bắt đầu `%PDF-`; response JSON/HTML lỗi không bị biến thành PDF base64. Marker legacy “There is no row at position 0.” trả 404 MINVOICE_DOCUMENT_NOT_FOUND. Business lỗi 422; response thiếu/sai 502; timeout 504; luôn JSON ProblemDetails, message từ message/Message được che credential/token. Mã NCC chỉ echo allowlist 01/02/03/04/21/22/23, mã khác MINVOICE_ITEM_REJECTED; không trả raw body.

Completed replay cùng key/body. Pending/timeout/lỗi sau dispatch giữ khóa để đối soát, không tự retry. Claims ký dùng cả PMS ID và GUID NCC đã biết để không vượt khóa bằng đổi PMS ID; GUID/số trong claim được chuẩn hóa, wire giữ nguyên kiểu/case. Lỗi trước business POST giải phóng reservation. Journal local chưa dành cho nhiều instance.

**Q015 đang chờ:** change hiện khóa một lần theo GUID NCC. Chưa cho sửa tiếp sau completed cho tới khi người dùng chốt quy tắc; pending luôn cần đối soát. Create cũng giữ ID tạo nháp một lần. Không dùng ID/key khác để vượt khóa. Lookup `GET /operations/{operation}/{key}` không gọi NCC; invoices đọc NCC không tự reconcile/unlock.

Các field provider mới tùy chọn dùng JsonIgnore khi null, nên không đổi fingerprint request MISA cũ hoặc thêm field null vào result MISA; đã có regression replay record cũ. Không migrate/reset journal thật.

## Postman

Import [collection Minvoice](postman/eInvoice-MINVOICE.postman_collection.json) và [environment template](postman/eInvoice-MINVOICE.environment.template.json). Điền key còn hạn, SiteId Minvoice, mẫu/ký hiệu sandbox, ngày và provider GUID/số trả về; chỉ điền người nhận email sandbox khi chủ động test. 19 request tổng hợp, không credential hoặc script auto retry.

Kiểm chứng mapper bằng 11 fixture frozen độc lập, API và lỗi qua fake HTTP, concurrency/token tenant isolation, URL guard và MISA journal compatibility. Chưa có endpoint/credential sandbox Minvoice thực tế trong cấu hình local, không gọi NCC thật, chưa UAT/cutover.
