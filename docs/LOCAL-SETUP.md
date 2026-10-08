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
  "eInvoiceAdmin": {
    "BaseAddress": "https://admin.example.test/",
    "TimeoutSeconds": 30
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
- Khi body chứa `siteConfigInfo`, `siteConfigInfo.site.id` phải trùng `X-Site-Id`; dữ liệu admin trả về cũng được kiểm tra ID.
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
local hiện thiếu admin nên body hợp lệ vẫn có thể trả 503. Không đổi cấu hình sang production để vượt lỗi này.

Nguồn hướng dẫn: [OWASP REST Security](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html). API key là thông tin bí mật xác thực service; HTTPS và kiểm tra quyền site vẫn bắt buộc.
