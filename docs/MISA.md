# MISA — tiến độ adapter

## Đã triển khai

- Login transport `POST {PartnerUrl}auth/token`.
- Giữ key provider `appid`, `taxcode`, `username`, `password`; header `CompanyTaxcode`; JSON UTF-8.
- Đọc body trước khi xét HTTP status (MISA có body lỗi trên 4xx/5xx), nhận diện UnAuthorize + UserNotExist/MisaIdError.
- Client stateless; token chỉ trả về bên trong ứng dụng. API health-check không trả token hoặc cấu hình chứa credential.
- Phân biệt lỗi rỗng/JSON sai/bị từ chối/network/timeout và cancellation của caller.
- Mọi test dùng HTTP giả lập; **chưa sandbox/UAT**.

## Tiếp theo

1. Lập bảng variant từ `MisaAppService.cs`, `MisaConvertRequest.cs`, `MisaRepository.cs`: invoice/ticket/receipt × HSM/MTT/USB, single/batch.
2. Port GetInvoiceFormSerial và response double-encoded; giữ timeout 90 giây tại baseline.
3. Port token endpoint biên lai riêng. Không dùng nhầm bộ Username/Password và Username2/Password2.
4. Dựng token cache theo site + endpoint + account + credential version, single-flight khi đồng thời; UTC expiry được kiểm thử; không key cache bằng password plaintext.
5. Capture payload/response offline theo từng variant trước khi refactor mapping; giữ các khác biệt OrgInvoiceData/OriginalInvoiceData, AmountDecimalDigits, rounding và date format.
6. Triển khai CreateAndPublish, batch, Preview, Replace/Adjust, PDF, Void, CQT, USB theo từng luồng.

## Bẫy cần giữ và xác minh

- Default canonical InvoiceType vẫn là 1 (vé), FormSerial default 0; chưa tự đổi.
- Không nối Draft/Create/DownloadDraftPdf/SearchById vào dispatcher vì code cũ có chuẩn bị hoặc comment; baseline không hỗ trợ chúng.
- Login/HealthCheck dùng hệ token v3; hệ biên lai có endpoint/token khác.
- Không gộp các mapping MTT/HSM/MTT-HSM/vé/biên lai trước khi có fixture từng nhánh.
- Không retry phát hành tự động. Khi timeout, kết quả có thể chưa xác định; cần tra cứu/khóa chống trùng đã được xác minh.
- API lỗi mới giữ code nhà cung cấp nội bộ để phân loại, không trả raw body có thể chứa credential. Từ điển lỗi chi tiết cần bổ sung theo response mẫu.

Nguồn: baseline `66a9c74`, `src/ezInvoice.Application/Services/ThirdPartyServices/MisaAppService.cs`, `src/ezInvoice.Application/EventSourcedNormalizers/MisaNormalizes/`, `src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Misa/`; hash trong reference manifest.
