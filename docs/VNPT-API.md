# VNPT / VNPT_V5 API

`site.partner: 1` → `VNPT` (17 action), `site.partner: 5` → `VNPT_V5` (21 action). Các nhánh hóa đơn/vé/biên lai và MTT giữ dispatcher baseline. Code và test offline đã hoàn thành; chưa UAT hoặc gọi VNPT thật.

Server cấu hình `Providers:Vnpt:AllowedHosts` cho cả hai adapter; site JSON có `partnerUrl` HTTPS kết thúc `/`. `username/password` là tài khoản API, `username2/password2` là tài khoản dịch vụ; tên tham số SOAP khác nhau được giữ nguyên từng operation. Proxy sinh từ WSDL được lấy từ baseline, không viết lại contract bằng tay. VNPT V1 còn dùng BusinessService_V5 và PublishService/PortalService V1; biên lai dùng PublishService_TLYT.

PMS dùng HTTPS, `X-Api-Key`, `X-Site-Id`. Thao tác ghi cần `Idempotency-Key`; preview VNPT cũng là thao tác ghi vì tạo nháp trước khi tải PDF. ID do PMS cấp phải ổn định. Batch yêu cầu GUID dạng `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` như parser baseline, đồng nhất invoiceType/formNo/serial và tối đa 500 hóa đơn.

| POST `/api/eInvoice/…` | VNPT | VNPT_V5 | Body/hành vi |
|---|---|---|---|
| health-check, form-serials | Có | Có | GetFormSerialsRequest; gọi getPublishInvs, không trả credential |
| draft | Có | Có | CreateInvoiceRequest; nháp chưa xác nhận ký |
| draft/edit | — | Có | Giữ nhánh import nháp của dispatcher; không dùng helper xóa rồi sinh GUID mới đã chết |
| create | — | Có | Giữ action CREATED gọi create-and-publish trong baseline |
| publish, publish-batch | Có | Có | CreateInvoiceRequest / InvoiceBatchRequest |
| publish-existing | Có | Có | PublishExistingInvoiceRequest: invoiceId, invoiceType, formNo, serial |
| preview | Có | Có | AdjustOrReplaceInvoiceRequest; import nháp → PDF; cần Idempotency-Key |
| adjust/increase, adjust/decrease, adjust/information, replace | Có | Có | AdjustOrReplaceInvoiceRequest, ID mới khác originalInvoiceId |
| adjust | — | Có | Điều chỉnh TT78 |
| document, document/draft | Có | Có | DownloadPdfRequest: invoiceId, invoiceType, serial, electronicInvoiceType |
| void, delete | Có | Có | VoidInvoiceRequest / DeleteInvoiceRequest; giữ hạn chế biến thể từ baseline |
| search | Có | — | SearchInvoiceRequest: invoiceId, formNo |
| status | Có | Có | SendToTaxAuthorityRequest: invoiceIds (tối đa 100), formNo, invoiceType |
| certificate | — | Có | GetFormSerialsRequest; trả serial chứng thư |
| tax/send | — | Có | SendToTaxAuthorityRequest: invoiceIds, formNo, serial; lấy certificate trước rồi gửi MTT |

VNPT_V5 `publish` mặc định giữ bước xóa nháp trước phát hành khi PMS có invoiceId như code cũ. Trường nullable mới `deleteExistingDraft: false` bỏ bước xóa khi PMS xác định đây là hóa đơn mới; bỏ trường/null giữ baseline. Không tự thay ID sau xóa; không phát sinh GUID khi thiếu ID. Không retry/unlock pending. `create` V5 và `publish` là hai action khác nhau dù baseline gọi cùng luồng NCC.

Wire XML giữ khai báo UTF-16 ở V1 dù SOAP UTF-8, thứ tự thẻ, null omission/thẻ rỗng và ngoại lệ phí dịch vụ theo site. Seller mapping dùng scope theo request thay cho LoggingContext toàn cục. Ngày/culture UTC+7/invariant theo D021; tiền/thuế/làm tròn không đổi. Bổ sung từ repo cũ commit `c09ad7d`: details[].isItemSpecialTax → Product.IsSum=4 ở mapper V4, null/false không thêm thẻ; không tự áp dụng vào nhánh V5/MTT khác với code cũ.

SOAP timeout 60 giây, không retry; cleanup/abort client khi xong hoặc hủy. Generated WCF formatter đã được kiểm thử offline với action, tên Account/ACpass/username/password và XML escaped. Kết quả ký có segment ERR cùng segment OK được xử lý theo segment OK; không báo lỗi giả cho hóa đơn đã ký. Batch/status từ chối ID ngoài request. PDF trả base64 hoặc URL theo nhánh baseline; không biến lỗi NCC thành tài liệu.

[VNPT Postman](postman/eInvoice-VNPT.postman_collection.json), [VNPT_V5 Postman](postman/eInvoice-VNPT_V5.postman_collection.json), environment cùng thư mục. Fixture: 12 payload baseline V1/V5/MTT/biên lai và 2 payload bổ sung IsItemSpecialTax; capture độc lập, không gọi NCC. [Nguồn baseline](reference/vnpt-source-provenance.json), [supplement hiện tại](reference/vnpt-current-supplement.json).
