# Nhật ký checkpoint

## 2026-10-08 — P0 và kiểm tra kết nối MISA

- Branch `rewrite/foundation`, baseline code cũ `66a9c74`.
- Code checkpoint: `9316ec3` — Initialize eInvoice foundation and authenticated MISA connection check.
- Chốt C#, tên eInvoice, hợp đồng PMS mới, HTTP 4xx/5xx + JSON, API key có site scope/HTTPS, MISA đầu tiên.
- Hoàn thành P0, auth/site scope và MISA health-check; xem STATUS.
- Verify Release: 100 test passed, 0 warning/error; fake HTTP, chưa UAT.
- Tiếp theo: bảng variant MISA → GetInvoiceFormSerial (90s, token v3, double-encoded response) → token biên lai/cache concurrency → mapping phát hành.
- Không sửa code cũ, không push/deploy, không gọi API production.

## 2026-10-08 — Chạy local và chuẩn bị Postman

- Theo yêu cầu người dùng: build Release, chạy HTTPS localhost:7150; cấu hình client/key local synthetic riêng và lưu Postman environment/collection trong .local (ignored).
- Probe HTTP trực tiếp: 200/401/403/503 như kỳ vọng; TLS certificate localhost được xác minh trusted.
- Chưa kết nối MISA thật. Tiến độ adapter không thay đổi; bước tiếp theo vẫn FormSerial.

## 2026-10-08 — MISA FormSerial

- Lập MISA-VARIANTS từ code baseline; ghi rõ single/batch và hai bộ credential biên lai.
- Người dùng chốt năm FormSerial UTC+7 (D013), chưa chốt culture/timezone các mapping tiền/ngày khác.
- Port GET templates, token v3, header/key provider nguyên casing, timeout GET 90s; endpoint mới POST /api/eInvoice/form-serials có API key/site scope, invoiceType 0 và config request tùy chọn.
- Parser đọc JSON hợp lệ trước fallback escaped/wrapped array lịch sử (D014); fixture giữ các quy tắc mapping baseline.
- Verify Release: **151 test passed, 0 warning/error**; Debug cũng pass. Fake HTTP, chưa sandbox/UAT.
- Restart local https://localhost:7150: API PID 2404/session 85073; probe thực tế 200/401/403/400/503 đúng kỳ vọng. Collection Postman bổ sung 2 request FormSerial; key không đổi và không commit.
- Checkpoint: `Implement MISA form serial lookup with scoped API and UTC+7 year`.
- Tiếp theo: token biên lai + ngăn log credential query → token cache v3/biên lai phân tách tenant, single-flight/UTC expiry → fixture mapping phát hành.

## 2026-10-08 — Hoàn thiện bộ API MISA baseline

- Theo yêu cầu làm tới khi hoàn thành bộ API MISA: port đủ 19 action sống vào 15 POST route, hai hệ token, HSM/MTT/MTT HSM/vé/biên lai/USB, mapping request/response và capability route thực tế.
- Token v3 cache phân tách tenant/credential với HMAC, single-flight/expiry UTC/invalidation; biên lai token trong một thao tác; health-check login mới. Không log URI credential.
- Journal local durable Idempotency-Key/claims/replay qua restart; pending/unknown chặn retry. Validation/login/template lookup thất bại trước business POST giải phóng reservation.
- Thêm frozen baseline mapper độc lập và 16 payload fixture tổng hợp. Response correlation/partial batch/supplier code allowlist và validation có fake HTTP/API tests.
- Q008/Q009/Q010 đã hỏi, chưa có câu trả lời. Giữ công thức/culture/date/routing baseline; HSM CQT được port tên tax/send-pending theo phạm vi hàng đợi thật của provider. Không thay nghiệp vụ phụ thuộc câu trả lời và không gọi thật.
- OpenAPI 3.1 (18 paths, 37 schemas), Postman 19 request, MISA-API/runbook. Environment local giữ key cũ, thêm biến; credential/journal/.local không commit.
- Rà soát cuối: phase khóa điều chỉnh theo nhánh NCC thực tế kể cả mode USB/serial MTT; hủy khóa theo định danh NCC; journal hỏng báo pending và không cho retry.
- Verify Release cuối: **240 test passed, 0 warning/error**, không skip. Chưa admin/MISA thật, chưa sandbox/UAT/cutover.
- Restart localhost:7150 Release, **PID 18912/session 65209**; trusted TLS probe **25 ca đạt**, gồm OpenAPI/capabilities/journal và lỗi auth/config/idempotency dự kiến.
- Checkpoint: `Complete MISA baseline APIs with token isolation and durable write journal`; xem git log để lấy hash.
- Tiếp theo: câu trả lời Q008/Q009/Q010 và cấu hình sandbox/UAT từng variant; không port lại bộ MISA. Repo cũ giữ nguyên; chưa push/deploy.

## 2026-10-08 — Bỏ admin cũ, tạm JSON để test

- Người dùng xác nhận D019/Q011: không dùng link admin cũ, tự lưu DB riêng, tạm JSON. Runtime bỏ đăng ký HTTP client/admin error mapping; nguồn cấu hình là JsonSiteConfigurationStore qua ISiteConfigurationStore, giữ config request ưu tiên.
- JSON server wrapper sites gồm site/extraDataMap, chọn đúng ID, không fallback network/site khác. Đọc lại mỗi lookup; credential đổi dùng ở request sau. JSON sai/ID trùng/thiếu/unavailable phân loại safe JSON, không log nội dung/path/credential.
- Tạo sites.Local.json ignored từ template, GUID local có sẵn, credential/endpoint trống; giữ API key và không overwrite file site sẵn có. File mẫu commit docs/sites.example.json; update LOCAL-SETUP/API-CONTRACT/Postman/AGENTS. Chưa chọn DB/schema.
- Thêm 17 test store/API fake HTTP, cả health/forms/document/publish lấy config JSON và không có IAdminClient trong DI. Final verify: **257 test pass, 0 warning/error**. Lần build đầu bị DLL lock do host cũ; đã xác minh/dừng process rồi chạy lại verify thành công.
- Restart Release https://localhost:7150, **PID 3548/session 81966**. Probe 9 ca dùng public dev certificate localhost làm trust riêng, kiểm tra hostname/TLS bình thường. Health 400 MISA_CREDENTIALS_REQUIRED (file mẫu trống), forms 503 MISA_ENDPOINT_NOT_ALLOWED (endpoint trống); không còn ADMIN_NOT_CONFIGURED và không gọi NCC.
- Checkpoint: `Use local JSON site configuration with database-ready store interface`. Tiếp theo: người dùng điền sandbox JSON + host allowlist, restart khi thay allowlist, UAT theo variant; Q008/Q009/Q010 vẫn mở. Không sửa repo cũ/push/deploy.

## 2026-10-09 — Restart sau khi người dùng điền cấu hình

- Người dùng báo đã điền thông tin và yêu cầu reset API; chỉ restart host, không reset journal hay sửa credential/key.
- Xác minh executable của process đang giữ port 7150 rồi dừng PID 3548; chạy lại Release không build tại https://localhost:7150.
- Host mới PID 16664/session 5464; GET /health/live qua HTTPS xác minh certificate/hostname trả 200 service=eInvoice, status=alive.
- Không gọi health-check MISA hoặc thao tác NCC thật trong lần này. Runtime code không đổi; verify đầy đủ gần nhất vẫn 257 test đạt.

## 2026-10-09 — Sửa allowlist MISA sau lỗi health-check

- Chẩn đoán chỉ in scheme/hostname và các cờ validation, không in credential/full config: URL có HTTPS và slash cuối, hostname testapi.meinvoice.vn nhưng AllowedHosts trống.
- Thêm đúng hostname người dùng đã cấu hình vào appsettings.Local.json (ignored), giữ các trường khác. Restart host sau xác minh executable: PID 14168/session 81336.
- Thử lại riêng POST /api/eInvoice/health-check qua HTTPS local: **200, provider=MISA, connected=true**. Đã liên hệ endpoint MISA do người dùng cấu hình; không gọi phát hành/hủy/CQT/email hoặc nghiệp vụ khác.
- Không thay runtime code, không xóa journal. Last verify vẫn 257 test đạt; UAT các luồng còn lại vẫn chưa thực hiện.

## 2026-10-09 — Chẩn đoán publish và lưu lỗi NCC

- Người dùng cung cấp curl publish bị MISA_INVOICE_ALREADY_ATTEMPTED. Đọc journal site/invoice claim: prior pending, legacy không lưu key/operation/lỗi. Đối chiếu hash nhận diện key gốc khác key curl; không sửa record/claim, không gửi lại NCC.
- Thêm GET invoice-attempts theo site/invoiceId/phase; 409 có errorSource gateway, previousAttempt, attemptStatusUrl. Record mới lưu key/operation/requestId/lastError; đọc cũ nullable, pending vẫn khóa.
- D020: business envelope/item invoice 0 trả providerMessage từ field xác định sau redaction và providerHttpStatus; không trả raw envelope/Data lỗi, không lưu token/exception message. Batch có message item lỗi. HTTP supplier 200 vẫn có thể business 422.
- Verify Release **268 test passed, 0 warning/error**, 11 ca mới. Giữ financial fixture và retry behavior. Lookup không gọi NCC; metadata/key cùng site, phase tách biệt, pending survives restart.
- Restart local **PID 14072/session 95101**. Tái hiện request được cung cấp sau guard prior pending: 409 gateway + previousAttempt/link; GET invoice-attempts 200 pending. Không có provider write/reconcile/reset thực tế, chỉ lỗi mock trong tests; giữ journal/user credential ngoài Git.
- Checkpoint `Expose sanitized MISA errors and persist invoice attempt diagnostics`. Tiếp theo: nếu người dùng còn response cũ thì đối chiếu; đối soát MISA trước quyết định gửi lại. Response cũ đã bỏ không thể khôi phục từ hash. Các quyết định Q008/Q009/Q010 vẫn mở.

## 2026-10-10/11 — Tài liệu tích hợp cho AI agent phía phần mềm

- Người dùng hỏi phần mềm khác đã test phát hành được chưa và yêu cầu tài liệu để AI agent gọi API. Đối chiếu route/DTO/validation/mapping/result/journal/security ở checkpoint 316ba42; đủ code để bắt đầu thử MISA, chưa xác nhận publish sandbox thành công.
- Tạo docs/INTEGRATION-AGENT.md tự chứa hướng dẫn và request/response/cURL, JSON riêng docs/examples/misa-publish.json, liên kết từ README/API-CONTRACT. Scope single invoiceType 0, mode 0 HSM, mẫu non-MTT; hướng dẫn lưu snapshot/key trước gửi, parse từng item, lỗi/timeout/409/lookup và trạng thái CQT riêng.
- Xác minh listener localhost:7150 vẫn PID 14072; curl.exe HTTPS mặc định trả alive. Không gọi login/publish/CQT/hủy/email MISA. Chỉ đọc expiry cấu hình: key postman-local hết hạn cuối 09/10/2026 UTC+7. Không sửa key/site/credential, mở port hoặc restart. Python probe dùng file public certificate cũ không xác minh được TLS; dùng curl.exe trust Windows thành công, không bypass chứng chỉ.
- Chạy lại test Release không build/restore: **268 passed, 0 failed/skipped**. Kiểm tra JSON request bằng JSON Schema OpenAPI 3.1, body nhúng/file riêng đồng nhất, totals/nhánh HSM, route và link. Không thêm test/runtime dependency hoặc tự cập nhật financial fixture.
- Ghi rõ enum hiện tại not-found/pending/completed; processing/rejected/unknown đã thảo luận chưa được implement. Tra cứu journal không gọi MISA, status chỉ SendTaxStatus; chưa có API tự reconcile/unlock.
- Tiếp theo: bên tích hợp nhận key còn hạn và URL HTTPS truy cập được, SiteId/mẫu/cách ký sandbox; thực hiện UAT được cho phép và đối chiếu MISA. Nếu máy khác cần endpoint khác loopback. Không tự quyết mạng/key/production hoặc đổi ID để vượt claim cũ. Checkpoint `Document MISA sandbox integration for external AI agents`.
