# Trạng thái eInvoice — điểm bắt đầu phiên kế tiếp

Ngày cập nhật: **2026-10-08**, Asia/Bangkok.

## Mục tiêu và checkpoint

Viết lại gateway bằng C#, tên eInvoice, chỉ hợp đồng PMS mới, giữ key/payload provider. Quyền và quy tắc nằm trong AGENTS.md, DECISIONS.md; không phụ thuộc lịch sử chat.

- Branch: `rewrite/foundation`.
- Baseline repo cũ: `66a9c742d0f6fb692d339076e6cd82780e3476be` tại `D:\Code\25.ezInvoice`.
- Checkpoint nền tảng `9316ec3`, FormSerial `92f9063`.
- Checkpoint phiên này: `Complete MISA baseline APIs with token isolation and durable write journal`; lấy hash bằng `git log -1 --oneline`.
- **Bộ API MISA tương ứng toàn bộ 19 action sống baseline đã triển khai và kiểm thử offline. Sandbox/UAT/cutover chưa thực hiện.**
- P0 hoàn thành; P1 MISA code/fixture/HTTP giả lập hoàn thành, kiểm chứng thực tế còn chờ.

## Đã hoàn thành

- Solution Core/Providers/Api/Tests .NET 10, SDK/package lock, script verify; snapshot nguồn/inventory 33 endpoint, 32 action, 9 provider.
- Canonical camelCase DTO mới, decimal/default/nullable baseline, JSON PMS strict và wire serialization riêng. Hai helper đọc tiền, ba serializer XML có fixture độc lập.
- Admin client async UTF-16, resolver ưu tiên config request; API key hash, expiry/rotation/HTTPS/site scope ở header và body; HTTP 4xx/5xx + JSON lỗi an toàn.
- MISA login v3 và biên lai, HTTPS/host allowlist/no redirect. Query token biên lai escape đúng; tắt HttpClient logger chứa credential và kiểm thử không rò query/password.
- Cache v3 single-flight, tenant/endpoint/tax/account/credential fingerprint HMAC, bounded capacity, UTC expiry/invalidation; health-check luôn login mới. Biên lai tái sử dụng token trong một thao tác, chưa cache liên request vì expiry chưa xác minh.
- FormSerial UTC+7 year (D013), parsing JSON hợp lệ trước fallback legacy (D014); lọc/default/name/status giữ baseline.
- 15 POST route: health-check, form-serials, publish, publish-batch, preview, adjust, replace, document, void, status, tax/send-pending, xml/create, xml/hash, tax/send-signed, xml/publish. HSM/MTT/MTT HSM/vé/biên lai/USB theo nhánh baseline.
- GET capabilities có đúng 19 action và route tương ứng; GET operations tra cứu journal; GET /openapi/misa.json và liveness. Xem MISA-API.md, openapi.misa.json, docs/postman.
- Validation đầu vào/response, correlation invoiceId, batch partial errors, symbolic providerCode allowlist; giữ key NCC kể cả ezInvoiceID. Không tự retry ghi; token expired chỉ invalidates cho request sau.
- Journal local bền qua restart: Idempotency-Key, request fingerprint, claims site/invoice/phase, replay completed, chặn key/body mismatch và pending/unknown. Lỗi trước business POST giải phóng reservation; đã dispatch thì giữ pending. Chưa có reset/reconcile tự động, chưa dùng chung nhiều server.
- Frozen mapper độc lập trong tools/MisaFixtureCapture; 16 payload tổng hợp từ baseline cố định, culture invariant và máy UTC+7. Không thay expected theo runtime để làm test qua. Không phải golden production.
- OpenAPI 3.1: 18 paths, 37 schemas; Postman 19 request + template environment không credential; MISA-API/LOCAL-SETUP mô tả hợp đồng và trình tự USB.

## Kiểm chứng gần nhất

`powershell -NoProfile -File scripts/verify.ps1`: **PASS**, Release **0 warning, 0 error**, **240 test passed**, không skip.

Bao phủ payload fixture, API thật qua HTTP giả lập, credential/token separation, no-secret logging, concurrency/cancellation/invalidation, journal replay/restart/duplicate, partial batch, lỗi/response sai/site scope và đối chiếu OpenAPI/capabilities với route thực tế.

Probe HTTPS local sau restart: **25 ca đạt** — 200/401/403/400/503 đúng kỳ vọng, 19 action capabilities, OpenAPI 18 paths và journal not-found. TLS localhost được xác minh, không bỏ kiểm tra certificate.

Chưa gọi admin/MISA thật, chưa sandbox/UAT. Repo cũ giữ nguyên thay đổi local trước phiên này. Chưa push/deploy.

## Chưa làm và quyết định đang chờ

- Q008: invariant culture và UTC+7 cho ngày/ticks ngoài năm FormSerial. Hiện giữ công thức, culture và DateTime.Now của mapper baseline.
- Q009: định tuyến MTT/batch dựa trên electronicInvoiceType rõ ràng. Hiện giữ điều kiện baseline; mode 4 cần serial MTT, batch không tự sửa dispatcher cũ. Xem MISA-VARIANTS.
- Q010: giữ hay giới hạn CQT HSM. Chức năng baseline được port với tên tax/send-pending, không nhận invoiceIds vì provider gửi hàng đợi đang chờ. Không coi im lặng là duyệt thay nghiệp vụ.
- Sandbox/UAT từng variant, golden đã che, token receipt expiry, quantity biên lai int, USB correlation/thứ tự và batch partial errors.
- 8 provider còn lại; log/catalogue còn dùng, correlation requestId PMS, metrics/throttling/proxy và cấu hình production.
- Storage/reconcile kết quả ghi chưa xác định và kế hoạch thay thế/rollback trước cutover.

## Bước tiếp theo khi tiếp tục

1. Đọc AGENTS, STATUS, PLAN, DECISIONS, QUESTIONS, MISA-API, MISA-VARIANTS; kiểm tra git status/branch/log. Bộ MISA đã port, không làm lại.
2. Xử lý câu trả lời Q008/Q009/Q010 nếu có. Các thay đổi phụ thuộc chưa được duyệt; tiếp tục phần độc lập, giữ baseline nếu chưa có câu trả lời.
3. Cấu hình admin/site hoặc siteConfigInfo và endpoint/host/credential **sandbox** trong local ignored config. Không yêu cầu gửi bí mật vào chat/Git. Site phải thuộc API key và khớp header/body/admin.
4. UAT đọc (health/forms/preview/status/document) trước, rồi các thao tác ghi sandbox đã được cho phép. Ghi payload/response đã che và xác minh USB/batch/biên lai. Không gọi production hoặc tự reset pending để retry.
5. Nếu triển khai nhiều instance: thay journal local bằng storage có transaction/unique claims và cơ chế reconcile được thống nhất; làm runbook cutover riêng.
6. Khi có thay đổi code: chạy verify, cập nhật tài liệu và checkpoint Git cục bộ.

## API local/Postman hiện tại

- `https://localhost:7150`, loopback, Release; **API PID 18912, exec session 65209**. Kiểm tra executable/listener trước khi dừng vì PID có thể tái sử dụng. Metadata tại .local/local-api.json.
- Key client postman-local, site tổng hợp `11111111-1111-1111-1111-111111111111`, hết hạn cuối 2026-10-09 UTC+7. Key gốc chỉ ở .local; server giữ hash trong appsettings.Local.json (ignored).
- Import `.local/eInvoice-local.postman_environment.json` và `.local/eInvoice-local.postman_collection.json` (19 request). Key đã giữ nguyên, thêm biến còn thiếu. Bản công khai/template tại docs/postman không chứa key.
- JournalDirectory local là `D:\Code\40.eInvoice\.local\misa-journal`, không xóa khi build/restart. Key idempotency phải ổn định khi retry cùng body; không sinh key mới để vượt pending.
- Chưa cấu hình admin/MISA: request nghiệp vụ có auth/header hợp lệ trả 503 ADMIN_NOT_CONFIGURED. Đây không phải kết quả kiểm thử sandbox thành công.
- Chạy lại khi process đã dừng: `dotnet run --project src/eInvoice.Api -c Release --no-build --no-restore --no-launch-profile --urls https://localhost:7150`.
- .local/probe-api.py chỉ probe host chưa cấu hình admin, có guard và dùng TLS trusted. .local/probe-results.json lưu 25 kết quả; không gọi NCC.

## Lưu ý

Không chạy lại bootstrap/port/freeze để ghi đè code hoặc capture lại expected từ runtime. Giữ baseline/manifest. TLS mặc định được kiểm tra; chỉ xem xét ngoại lệ khi có endpoint cụ thể. Không đưa credential/dữ liệu thật vào Git.
