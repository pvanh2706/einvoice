# Trạng thái eInvoice — điểm bắt đầu phiên kế tiếp

Ngày cập nhật: **2026-10-08**, Asia/Bangkok.

## Mục tiêu

Viết lại gateway hóa đơn bằng C# trong repo này, tên eInvoice, chỉ hợp đồng PMS mới, giữ key/payload provider. Xem AGENTS.md và DECISIONS.md.

## Phiên hiện tại

- Branch: `rewrite/foundation`.
- Code checkpoint đã kiểm chứng: **`9316ec3`** (`Initialize eInvoice foundation and authenticated MISA connection check`). Commit tiếp theo chỉ ghi nhận bàn giao phiên.
- Baseline repo cũ: `66a9c742d0f6fb692d339076e6cd82780e3476be` tại `D:\Code\25.ezInvoice`.
- **P0 hoàn thành; P1 mới có kiểm tra kết nối MISA. Chưa phải bản thay thế production.**
- Đã chốt hợp đồng mới, HTTP 4xx/5xx + JSON lỗi, API key theo hệ PMS/site scope/HTTPS; MISA đầu tiên.

## Đã làm

- Khởi tạo solution và project .NET 10 với SDK 10.0.401.
- Lưu quy tắc, kế hoạch và quyết định để tiếp tục giữa các phiên.
- Snapshot docs lịch sử, hash nguồn và inventory: 33 endpoint, 32 action (không số 13), 9 provider.
- Canonical DTO với key mới; decimal/default/nullable giữ baseline; JSON strict phía PMS, serialization riêng phía provider.
- Capability catalog là baseline, không đồng nghĩa adapter mới đã hoàn thành.
- Hai bộ đọc tiền, ba serializer XML; fixture offline 32 ca tiền và 9 ca XML từ helper cũ.
- Admin client async với JSON UTF-16/external SiteId; resolver ưu tiên config request; phân loại lỗi/network/cancellation.
- API key hash SHA-256, expiry/rotation, quyền theo site; chặn site khác qua header/body, yêu cầu HTTPS.
- MISA login v3 stateless: giữ key/header/UTF-8; đọc lỗi cả trên 4xx/5xx; host allowlist; không redirect/bypass TLS/retry.
- GET /health/live và POST /api/eInvoice/health-check; health-check MISA không trả credential/token.
- JSON Problem Details với code/requestId và HTTP status phù hợp.

## Kiểm thử gần nhất

`powershell -NoProfile -File scripts/verify.ps1`: **PASS**, Release **0 warning, 0 error**, **100 test passed**, không skip.

Fake HTTP/synthetic data; chưa gọi admin/MISA thật, chưa sandbox/UAT. Fixture helper không phải golden transaction production. Repo cũ giữ nguyên thay đổi local trước phiên này. Chưa push/deploy.

## Chưa làm

- MISA FormSerial, token cache/biên lai, phát hành/batch/preview/adjust/replace/PDF/void/CQT/USB.
- 8 provider còn lại, OpenAPI đầy đủ, API log/catalogue.
- Logging correlation/requestId PMS, metrics/throttling và config/forwarded proxy headers production.
- Golden giao dịch thực tế, culture/timezone và quyết định các bẫy tiền/thuế/TLS.

## Bước tiếp theo nếu phiên bị ngắt

1. Đọc AGENTS, DECISIONS, QUESTIONS, PLAN, docs/MISA.md; kiểm tra git status.
2. Lập bảng variant từ MisaAppService, MisaConvertRequest và MisaRepository; đọc sổ bẫy nhóm F.
3. Triển khai **GetInvoiceFormSerial**: token v3, provider body/header, response double-encoded, timeout **90 giây** theo baseline. Test fake HTTP/fixture trước khi expose route mới.
4. Tách token biên lai và cache theo site/endpoint/account/credential version; test concurrency/UTC expiry trước luồng phát hành.
5. Hỏi culture/timezone và quy tắc tiền chưa rõ trước khi thay cách tính; không tự retry thao tác ghi.
6. Chạy verify; cập nhật STATUS/PLAN/SESSION-LOG và checkpoint cục bộ.

## Lưu ý

- Không chạy lại bootstrap trong .local để ghi đè code đã phát triển; file đó không thuộc Git.
- Capture inventory/helper có chủ đích; không tự nâng baseline/thay expected output để làm test qua.
- TLS mặc định được kiểm tra; hỏi ngoại lệ khi có endpoint cụ thể; không yêu cầu credential thật gửi trong chat/Git.

## Chạy local để thử Postman — 2026-10-08

- Release build lại: 0 warning/error; API được khởi chạy tại `https://localhost:7150`, chỉ loopback.
- Process API ở thời điểm chạy: PID 6036, launcher/session exec 78509. Kiểm tra process và listener trước khi dừng/chạy lại; PID có thể được tái sử dụng.
- Key thử theo client `postman-local`, site synthetic `11111111-1111-1111-1111-111111111111`, hết hạn cuối ngày 2026-10-09 (UTC+7). Key gốc chỉ nằm trong `.local`, hash trong appsettings.Local.json; không commit.
- Import `.local/eInvoice-local.postman_environment.json` và `.local/eInvoice-local.postman_collection.json`. Environment có baseUrl/apiKey/siteId.
- HTTP thực tế đã kiểm tra: liveness 200, thiếu key 401, site khác 403, key hợp lệ + chưa cấu hình admin 503 ADMIN_NOT_CONFIGURED.
- Chưa có endpoint/credential admin hoặc MISA thật; không coi 503 là lỗi build. Muốn thử kết nối MISA thật cần cấu hình local/host allowlist/credential sandbox.
- Chạy lại bằng `dotnet run --project src/eInvoice.Api -c Release --no-build --no-restore --no-launch-profile --urls https://localhost:7150` khi process đã dừng.
