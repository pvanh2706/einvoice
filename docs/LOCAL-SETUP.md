# Môi trường local và API key

## Môi trường build

- Cài SDK khớp `global.json` hoặc bản patch được rollForward trong cùng feature band.
- Restore bằng `--locked-mode`. Khi chủ động đổi package, cập nhật lock file và chạy verify trước khi commit.
- Không sao chép appsettings thật của dự án cũ.

## Cấu hình chạy

Host đọc `src/eInvoice.Api/appsettings.Local.json` nếu có. File này bị Git ignore. Environment variables và command-line có độ ưu tiên cao hơn.

Ví dụ **placeholder**, không phải key hay cấu hình chạy thật:

```json
{
  "SiteConfiguration": {
    "JsonFilePath": "sites.Local.json"
  },
  "Authentication": {
    "Clients": [
      {
        "ClientId": "pms-main",
        "AllowedSiteIds": ["11111111-1111-1111-1111-111111111111"],
        "Keys": [
          {
            "KeyId": "current",
            "Sha256": "REPLACE_WITH_64_HEX_DIGITS_OF_KEY_HASH",
            "ExpiresAtUtc": "REPLACE_WITH_PLANNED_UTC_EXPIRY"
          }
        ]
      }
    ]
  },
  "Providers": {
    "Misa": {
      "AllowedHosts": ["misa.example.test"]
    }
  }
}
```

- Tạo API key bằng bộ sinh số ngẫu nhiên mật mã, ít nhất 32 byte ngẫu nhiên. PMS lưu key gốc; server lưu SHA-256 của chuỗi key gửi trong header, 64 ký tự hex.
- Có thể cấu hình hai key còn hiệu lực khi chuyển đổi. Hết hạn hoặc xóa key cũ sau khi PMS đã chuyển. Thay đổi cấu hình key ở phiên này cần khởi động lại host; chưa có admin quản lý key.
- Client ID và key hash không được trùng giữa các client. Mỗi client phải có danh sách site được phép; không có quyền wildcard.
- Cấu hình mặc định không có client: mọi yêu cầu API nghiệp vụ bị từ chối; liveness vẫn hoạt động.
- `X-Api-Key` dùng để xác thực; `X-Site-Id` dùng để chọn site và kiểm tra quyền. GUID site không thay thế API key.
- Khi body chứa `siteConfigInfo`, `siteConfigInfo.site.id` phải trùng `X-Site-Id`; cấu hình từ JSON cũng được kiểm tra ID.
- Endpoint MISA cần HTTPS, host trong allowlist và base URL có dấu `/` cuối (giữ cách nối `auth/token` của code cũ). Redirect và bypass TLS bị tắt.
- Nếu chạy sau reverse proxy, cần cấu hình forwarded headers với proxy tin cậy trước khi triển khai; phiên này chưa cấu hình proxy/IIS production.
- Không đưa API key vào URL, log hoặc Git. Chỉ dùng fake HTTP trong test.

API key hiện giới hạn client/site; quyền chi tiết theo action, throttling, lưu trữ key tập trung và quy trình cấp phát production còn nằm trong kế hoạch vận hành.

MISA write yêu cầu Idempotency-Key. Cấu hình `Providers:Misa:JournalDirectory` vào thư mục bền, ví dụ
`D:\Code\40.eInvoice\.local\misa-journal` trên máy này; không xóa journal khi rebuild/restart.
Thông tin replay/pending, expiry token và các credential khác nhau nằm trong [MISA-API](MISA-API.md).
Import collection docs/postman/eInvoice-MISA.postman_collection.json với environment local đang có.
Thêm biến idempotencyKey cố định cho lần test, searchCode/providerInvoiceNumber/certBase64/messageCode/signatureValue/signedInvoiceXml khi cần.
Không dùng {{$guid}} để thay key mỗi lần retry. Muốn test 200 thực tế cần site/credential/host sandbox;
Local JSON hiện chỉ chứa site mẫu, credential trống: health-check trả 400 MISA_CREDENTIALS_REQUIRED tới khi điền tài khoản sandbox. Không đổi cấu hình sang production để vượt lỗi này.

Nếu publish trả MISA_INVOICE_ALREADY_ATTEMPTED: đọc previousAttempt/attemptStatusUrl hoặc
`GET /api/eInvoice/invoice-attempts?invoiceId={{invoiceId}}&phase=publish` với cùng key/site auth.
Collection có request tra cứu theo invoiceId; đặt biến invoiceId ở environment. Không tự đổi key hoặc xóa journal.
Lỗi mới được lưu lastError đã lọc; record trước khi có diagnostics không thể khôi phục message quá khứ.

## Cấu hình khách sạn bằng JSON

Không còn gọi dịch vụ admin cũ. Copy [sites.example.json](sites.example.json) tới
`src/eInvoice.Api/sites.Local.json` (Git ignored). File có wrapper `sites`, mỗi phần tử gồm `site` và `extraDataMap`.
API chọn theo `X-Site-Id`; ID phải thuộc AllowedSiteIds của API key. Site mẫu dùng ID có sẵn trong environment local.

Điền `taxNumber`, `partnerUrl` (HTTPS, có `/` cuối), `username`, `password` cho MISA v3, `partner = 6`.
Biên lai cần thêm `partnerUrl2`, `username2`, `password2` theo MISA-API. Health-check không cần credential biên lai.
Trong appsettings.Local.json, thêm hostname tương ứng vào `Providers:Misa:AllowedHosts` (chỉ hostname, không scheme/path).
Thay allowlist/API key/path cần restart; sửa nội dung sites.Local.json được đọc ở request tiếp theo. Nên lưu file bằng thay thế nguyên tử khi cập nhật lúc host đang chạy; JSON dở dang bị từ chối, không dùng dữ liệu cũ hay site khác.

Body Postman health-check vẫn chỉ `{ "requestId": "postman-health-001" }`; không phải gửi password/config mỗi request.
File credential phải nằm ngoài Git; nếu dùng đường dẫn tùy chỉnh, tự bảo đảm file đó cũng bị ignore và hạn chế quyền đọc.
Không có API xuất cấu hình/credential. Không gửi bí mật trong chat.

| HTTP | Lỗi JSON store |
|---|---|
| 503 | SITE_STORE_NOT_CONFIGURED: thiếu file |
| 503 | SITE_STORE_INVALID_DATA: JSON sai schema hoặc ID null/rỗng/trùng |
| 503 | SITE_STORE_UNAVAILABLE: không đọc được file |
| 404 | SITE_CONFIGURATION_NOT_FOUND: không có SiteId được yêu cầu |

Khi chuyển sang DB riêng, implement `ISiteConfigurationStore.GetSiteAsync` và thay DI registration;
resolver/route/MISA mapper không cần biết nguồn cấu hình. Chưa chọn DB hoặc schema khi chưa có yêu cầu cụ thể.

Nguồn hướng dẫn: [OWASP REST Security](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html). API key là thông tin bí mật xác thực service; HTTPS và kiểm tra quyền site vẫn bắt buộc.
