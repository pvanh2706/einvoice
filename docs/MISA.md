# MISA — tiến độ adapter

## Đã triển khai

- Login transport `POST {PartnerUrl}auth/token`.
- Giữ key provider `appid`, `taxcode`, `username`, `password`; header `CompanyTaxcode`; JSON UTF-8.
- Đọc body trước khi xét HTTP status (MISA có body lỗi trên 4xx/5xx), nhận diện UnAuthorize + UserNotExist/MisaIdError.
- Client stateless; token chỉ trả về bên trong ứng dụng. API health-check không trả token hoặc cấu hình chứa credential.
- Phân biệt lỗi rỗng/JSON sai/bị từ chối/network/timeout và cancellation của caller.
- Mọi test dùng HTTP giả lập; **chưa sandbox/UAT**.
- FormSerial: `GET {PartnerUrl}code/itg/InvoicePublishing/templates?invyear={year}`, token v3, `Authorization: Bearer`, header `CompanyTaxCode`, không body. Timeout GET 90 giây; login vẫn 100 giây riêng, tổng luồng không bị giới hạn 90 giây.
- Năm hiện tại cố định UTC+7 theo D013. API chỉ nhận invoiceType 0; không tự mở vé/biên lai dù có hằng endpoint trong code cũ.
- Đọc envelope cả trên 4xx/5xx; Success phải true, Errors rỗng, ErrorCode rỗng. Data là chuỗi chứa mảng JSON hoặc chuỗi JSON lồng. Đọc JSON hợp lệ trước fallback Replace lịch sử (D014).
- Loại mẫu IsInheritFromOldTemplate; giữ thứ tự; TemplateName tối đa 40 ký tự UTF-16; InvTemplateNo → formNo, InvSeries → serial, Inactive → status 0/1, BusinessAreas 34 → isMultipleTaxRates true. Mảng rỗng là thành công; tên thiếu/null ở mẫu được giữ là lỗi response 502.
- Fixture tổng hợp đối chiếu mapping baseline, lỗi/timeout/cancellation/no retry, chuyển năm UTC+7 và hai site đồng thời. API test chạy cả login/parser thật qua HTTP giả lập.
- [Bảng variant từ baseline](MISA-VARIANTS.md) phân biệt code sống, nhánh thiếu và phần chưa port.

## Tiếp theo

1. Port token endpoint biên lai riêng. Token GET dùng Username/Password; body phát hành biên lai dùng Username2/Password2. Không gộp hai bộ credential.
2. Dựng token cache theo site + endpoint + account + credential version, single-flight khi đồng thời; UTC expiry được kiểm thử; không key cache bằng password plaintext.
3. Capture payload/response offline theo từng variant trước khi refactor mapping; giữ các khác biệt OrgInvoiceData/OriginalInvoiceData, AmountDecimalDigits, rounding và date format.
4. Triển khai CreateAndPublish, batch, Preview, Replace/Adjust, PDF, Void, CQT, USB theo từng luồng.

## Bẫy cần giữ và xác minh

- Default canonical InvoiceType vẫn là 1 (vé), FormSerial default 0; chưa tự đổi.
- Không nối Draft/Create/DownloadDraftPdf/SearchById vào dispatcher vì code cũ có chuẩn bị hoặc comment; baseline không hỗ trợ chúng.
- Login/HealthCheck dùng hệ token v3; hệ biên lai có endpoint/token khác.
- Không gộp các mapping MTT/HSM/MTT-HSM/vé/biên lai trước khi có fixture từng nhánh.
- Không retry phát hành tự động. Khi timeout, kết quả có thể chưa xác định; cần tra cứu/khóa chống trùng đã được xác minh.
- API lỗi mới giữ code nhà cung cấp nội bộ để phân loại, không trả raw body có thể chứa credential. Từ điển lỗi chi tiết cần bổ sung theo response mẫu.

Nguồn: baseline `66a9c74`, `src/ezInvoice.Application/Services/ThirdPartyServices/MisaAppService.cs`, `src/ezInvoice.Application/EventSourcedNormalizers/MisaNormalizes/`, `src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Misa/`; hash trong reference manifest.
