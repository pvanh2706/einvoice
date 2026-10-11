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

## 2026-10-11 — Bắt đầu P2, Viettel và router/journal chung

- Người dùng yêu cầu làm tiếp các nhà cung cấp khác. Đã giải thích MISA hoàn thành code/offline nhưng chưa đầy đủ UAT/publish thực tế. Không cần hỏi lại quyền làm provider tiếp theo.
- Người dùng duyệt D021: ngày UTC+7 và số invariant cho provider mới, giữ công thức tiền/thuế/làm tròn. Viettel epoch 00:00 UTC+7, ngày hủy default UTC+7; culture scope chỉ trong mapper mới. MISA không đổi routing/date ngoài D013.
- Viettel port 13 action baseline qua 12 operation HTTP, JSON/form-urlencoded, login từng operation và Cookie access_token, không cache token/header chung. Thêm router theo SiteId, capabilities thực tế, draft/search và ba route điều chỉnh; không giả lập form/status Viettel không có.
- Tách WriteOperationJournal, MisaWriteJournal là wrapper tương thích. MISA giữ hash/đường dẫn; attempt provider khác hash riêng nhưng claims invoice/site chung, chống đổi provider để vượt khóa. Record thêm provider nullable; legacy vẫn đọc được. Không đổi pending/retry/unlock.
- Frozen tool Viettel độc lập assembly runtime + 11 fixture baseline tổng hợp; 35 tests mới đưa tổng **303 passed**, verify locked restore + Release **0 warning/error**. 25 paths/39 schemas OpenAPI chung, URL cũ giữ; 17 Postman Viettel không credential, docs VIETTEL-API và hợp đồng/AGENTS/PLAN cập nhật.
- Dừng verified process 14072 để build, chưa restart trong lúc đang tiếp tục P2. Không gọi NCC thật, không sửa sites.Local/key/allowlist thật, không phát hành/hủy/CQT/email, không sửa repo cũ.
- Checkpoint `Implement Viettel APIs with shared provider dispatch and durable journal`. Tiếp theo Cyberbill; Minvoice/SUN/EasyInvoice/Fast/VNPT/VNPT_V5 còn chưa port.

## 2026-10-11 — Cyberbill, phần độc lập với quyết định nghiệp vụ

- Port auth/transport, mapping và 7 fixture frozen độc lập. Tách ProviderCallException và message sanitizer sang Common, giữ wrapper MISA tương thích. Không đổi financial formula hoặc wire key.
- Chỉ expose health-check/document/void, capabilities 4 action. Publish chưa expose vì Q012 luồng tạo–ký và Q013 paymentMethod đang chờ; câu hỏi ghi QUESTIONS.
- Verify locked restore/Release build 0 warning/error, **329 tests passed**, 26 test mới so với Viettel. Kiểm chứng token tenant isolation, redaction, wire fixture, PDF, replay/lookup và pending giữ khóa. OpenAPI chỉ báo CYBER_BILL ở 3 route đã xong.
- API local vẫn dừng để build trong P2; credential/site/key/journal thật không sửa, không gọi NCC thật. Tiếp tục SUN độc lập trong lúc chờ.

## 2026-10-11 — SUN đầy đủ theo baseline

- Port 10 action baseline qua SiteId/router chung, giữ hai URL, Bearer và form hủy, wire keys/công thức/default. Scope UTC+7/invariant đã được người dùng duyệt D021. Health chỉ login mới một lần, không trả token.
- Frozen capture độc lập gồm 10 payload. Chuẩn hóa binding dynamic/extension ToList thành context typed để chạy source; không đổi mapping tiền/thuế. Sid vẫn thuật toán baseline, thêm collision claim và validation mã ngắn trước NCC; transactionId response là sid. Draft không trả số phát hành, preview tải PDF nháp sẵn có.
- Verify locked restore/Release **0 warning/error, 358 passed, 0 failed/skipped**, 29 tests mới. OpenAPI 26 paths/39 schemas; Postman SUN 15 requests không credential. Không liên hệ SUN thật, không phát hành/hủy/CQT/email thật, không sửa journal/site/key local.
- Cyberbill Q012/Q013 vẫn chờ. Tiếp theo Minvoice; code dispatcher đã đọc sơ bộ, chưa port; token static và hai luồng login khác nhau cần xác minh trước UAT. Các provider khác chưa hoàn thành. Checkpoint `Implement SUN baseline APIs with UTC7 mapping and sid claims`.
- Checkpoint SUN `ef55f0e`, Cyberbill một phần `2122c10`. Restart Release local **PID 5352/session 47782**; HTTPS trust mặc định GET /health/live 200, OpenAPI 26 paths/v0.4.0 đúng artifact mới. Chỉ probe public không gọi NCC. Key test cũ vẫn hết hạn, không gia hạn/đổi credential/site/allowlist thật hoặc mở port ngoài loopback.

## 2026-10-11 — Minvoice baseline

- Người dùng yêu cầu tiếp tục. Đọc checkpoint/source và sổ bẫy F14–F22/C02/C03. Q014 người dùng chọn giữ từng luồng auth để đối chiếu (D024): VP/Bear nghiệp vụ, Username2 cho health. Token riêng mỗi operation, không static/cache cross-tenant; giữ timeout theo nhánh baseline, TLS/allowlist guard.
- Port 15 action qua 14 POST route, giữ money/tax/default/ngoại lệ site và key wire kể cả ortherFee; typed mapper/fixture độc lập 11 payload, UTC+7/invariant. Tách providerInvoiceCode/originalProviderInvoiceCode khỏi ID PMS. Optional null bị bỏ để không đổi fingerprint MISA cũ; regression replay journal legacy đạt. PDF kiểm tra bytes %PDF-, body lỗi không base64 thành success; tax registry map model field thay object rỗng, cần UAT shape thực tế.
- Thêm create/change/delete/publish-existing/invoices/tax-code/email, form-serials chung cho Minvoice. Q015 sửa nháp tiếp sau completed đã hỏi, chưa có quyết định; tạm giữ claim một lần theo GUID NCC, pending không retry.
- Lần verify đầu có 1 test host cũ giả định /api/eInvoice/Create chưa tồn tại; nay /create đã triển khai và ASP.NET route không phân biệt hoa/thường. Thay ca unknown route bằng đường dẫn không tồn tại, giữ test alias api/ezInvoice bị từ chối. Verify lại **397 passed, 0 failed/skipped**, locked restore/Release **0 warning/error**, 39 test mới. OpenAPI 33 paths/42 schemas, Postman Minvoice 19 request synthetic.
- Dừng process 5352 sau xác minh executable để build; không sửa journal/site/key/allowlist local, không gọi Minvoice/registry/email thật. Tiếp theo EasyInvoice; xử lý Q012/Q013/Q015 khi người dùng trả lời. Các provider chưa port không đánh dấu xong.
- Checkpoint Minvoice `ac75844`. Restart Release local PID 17716/session 22602, HTTPS /health/live 200 và OpenAPI 33 paths/v0.5.0 đúng artifact mới. Chỉ probe public, không gọi NCC; key test cũ vẫn hết hạn. Metadata runtime đã cập nhật trong .local/local-api.json.
