# Trạng thái eInvoice — điểm bắt đầu phiên kế tiếp

Ngày cập nhật: **2026-10-11**, Asia/Bangkok.

## Mục tiêu và checkpoint

Viết lại gateway bằng C#, tên eInvoice, chỉ hợp đồng PMS mới, giữ key/payload provider. Quyền và quy tắc nằm trong AGENTS.md, DECISIONS.md; không phụ thuộc lịch sử chat.

- Branch: `rewrite/foundation`.
- Baseline repo cũ: `66a9c742d0f6fb692d339076e6cd82780e3476be` tại `D:\Code\25.ezInvoice`.
- Checkpoint nền tảng `9316ec3`, FormSerial `92f9063`.
- Checkpoint phiên này: `Complete MISA baseline APIs with token isolation and durable write journal`; lấy hash bằng `git log -1 --oneline`.
- Cập nhật mới nhất D019: `Use local JSON site configuration with database-ready store interface`; lấy hash bằng git log. Người dùng bỏ admin cũ, tạm JSON, sau này DB riêng.
- Cập nhật diagnostics D020: `Expose sanitized MISA errors and persist invoice attempt diagnostics`; xem git log. Publish lỗi mới có message/status, journal lưu lỗi và 409 có previousAttempt/link tra cứu. Không thay cơ chế chặn retry.
- Cập nhật 2026-10-11: tài liệu bàn giao [INTEGRATION-AGENT](INTEGRATION-AGENT.md) và [JSON publish mẫu](examples/misa-publish.json) cho AI agent phần mềm bên ngoài; scope single HSM MISA sandbox, không đổi runtime/auth/retry hoặc mở kết nối mạng.
- P2 bắt đầu theo yêu cầu người dùng: **Viettel 13 action đã port**, router chung theo SiteId, journal giữ tương thích MISA và chống vượt claim qua đổi provider; xem [VIETTEL-API](VIETTEL-API.md). Checkpoint `Implement Viettel APIs with shared provider dispatch and durable journal`; lấy hash từ git log. D021 đã duyệt UTC+7/invariant cho provider mới, không đổi tiền/thuế/làm tròn hoặc MISA.
- **Bộ API MISA tương ứng toàn bộ 19 action sống baseline đã triển khai và kiểm thử offline. Sandbox/UAT/cutover chưa thực hiện.**
- P0 hoàn thành; P1 MISA code/fixture/HTTP giả lập hoàn thành, kiểm chứng thực tế còn chờ.
- Checkpoint P2: Viettel `0930848`, Cyberbill một phần `2122c10`, SUN `ef55f0e`, Minvoice `ac75844`. Đọc QUESTIONS Q012/Q013 trước khi expose phát hành Cyberbill; bước tiếp theo cụ thể nằm bên dưới.
- EasyInvoice nghiệp vụ checkpoint `1d05d97`; health/Login chưa chốt Q016, chưa coi 14 action baseline là đã xong.

## Đã hoàn thành

- Solution Core/Providers/Api/Tests .NET 10, SDK/package lock, script verify; snapshot nguồn/inventory 33 endpoint, 32 action, 9 provider.
- Canonical camelCase DTO mới, decimal/default/nullable baseline, JSON PMS strict và wire serialization riêng. Hai helper đọc tiền, ba serializer XML có fixture độc lập.
- JSON site store server theo X-Site-Id, ISiteConfigurationStore để thay DB; resolver ưu tiên config request. Admin client baseline chỉ giữ để đối chiếu/test, không đăng ký vào API. API key hash, expiry/rotation/HTTPS/site scope; HTTP 4xx/5xx + JSON lỗi an toàn.
- MISA login v3 và biên lai, HTTPS/host allowlist/no redirect. Query token biên lai escape đúng; tắt HttpClient logger chứa credential và kiểm thử không rò query/password.
- Cache v3 single-flight, tenant/endpoint/tax/account/credential fingerprint HMAC, bounded capacity, UTC expiry/invalidation; health-check luôn login mới. Biên lai tái sử dụng token trong một thao tác, chưa cache liên request vì expiry chưa xác minh.
- FormSerial UTC+7 year (D013), parsing JSON hợp lệ trước fallback legacy (D014); lọc/default/name/status giữ baseline.
- 15 POST route: health-check, form-serials, publish, publish-batch, preview, adjust, replace, document, void, status, tax/send-pending, xml/create, xml/hash, tax/send-signed, xml/publish. HSM/MTT/MTT HSM/vé/biên lai/USB theo nhánh baseline.
- GET capabilities có đúng 19 action và route tương ứng; GET operations tra cứu journal; GET /openapi/misa.json và liveness. Xem MISA-API.md, openapi.misa.json, docs/postman.
- Validation đầu vào/response, correlation invoiceId, batch partial errors, symbolic providerCode allowlist; giữ key NCC kể cả ezInvoiceID. Business lỗi có providerMessage đã che credential và providerHttpStatus; journal lưu lastError/key/operation/requestId gốc, GET invoice-attempts và 409 previousAttempt. Không tự retry ghi; token expired chỉ invalidates cho request sau.
- Journal local bền qua restart: Idempotency-Key, request fingerprint, claims site/invoice/phase, replay completed, chặn key/body mismatch và pending/unknown. Lỗi trước business POST giải phóng reservation; đã dispatch thì giữ pending. Chưa có reset/reconcile tự động, chưa dùng chung nhiều server.
- Frozen mapper độc lập trong tools/MisaFixtureCapture; 16 payload tổng hợp từ baseline cố định, culture invariant và máy UTC+7. Không thay expected theo runtime để làm test qua. Không phải golden production.
- OpenAPI 3.1: 19 paths, 39 schemas; Postman 20 request + template environment không credential; MISA-API/LOCAL-SETUP mô tả hợp đồng/diagnostics và trình tự USB.

## Kiểm chứng gần nhất

EasyInvoice **12 action nghiệp vụ/12 POST route** đã port, xem [EASYINVOICE-API](EASYINVOICE-API.md). Verify locked restore/Release **0 warning/error, 456 passed, 0 failed/skipped**, 59 tests mới so với Minvoice. 20 fixture độc lập giữ XML/tỷ giá 0/phí/metadata/tổng thuế; runtime gom ba khối mapper, Authentication từng request/tenant/nonce, PDF thật, correlation Ikey, replay/claim/pending và fingerprint Minvoice cũ có thêm serial nullable. OpenAPI 33 paths/42 schemas/v0.6.0, Postman 17 requests. **Health/Login chờ Q016**, tạm 501, capabilities chỉ 12 action nghiệp vụ; chưa UAT/NCC thật.

Minvoice **15 action baseline/14 POST route** đã port theo D024, xem [MINVOICE-API](MINVOICE-API.md). Verify locked restore/Release **0 warning/error, 397 passed, 0 failed/skipped**, 39 tests mới; 11 fixture độc lập gồm hai ngoại lệ phí dịch vụ. Bao phủ token tenant isolation, auth VP/Bear và unit health, ID NCC/PMS, PDF không nhận body lỗi, UTC+7/culture, lỗi/timeout/replay/claim và fingerprint replay MISA cũ. OpenAPI **33 paths/42 schemas**, Postman Minvoice **19 requests**. Không gọi NCC thật/UAT. Q015 đang chờ: change hiện khóa một lần theo GUID NCC, chưa tự cho sửa tiếp sau completed.

SUN 10 action baseline đã port, xem [SUN-API](SUN-API.md). Verify locked restore/Release **0 warning/error, 358 passed, 0 failed/skipped**, 29 tests mới. 10 fixture độc lập, UTC+7/invariant, hai endpoint và bearer/token isolation, sid collision claims, replay/pending/PDF/lỗi/timeout. OpenAPI **26 paths/39 schemas**, Postman SUN 15 requests; không gọi SUN thật hoặc UAT. Checkpoint `Implement SUN baseline APIs with UTC7 mapping and sid claims`.

Cyberbill đã có transport/auth, 7 mapping fixture baseline độc lập; API chỉ expose health-check/document/void (4 action). Q012 luồng tạo/ký và Q013 mã thanh toán đang chờ, publish trả 501 trước NCC. Xem [CYBERBILL-API](CYBERBILL-API.md). Verify locked restore/Release build **0 warning/error, 329 passed, 0 failed/skipped**, 26 test mới; không gọi NCC thật. OpenAPI chỉ liệt kê CYBER_BILL ở 3 route đã xong. Tiếp tục phần độc lập SUN trong lúc chờ.

Sau port Viettel ngày 11/10: `powershell -NoProfile -File scripts/verify.ps1` **PASS**, restore locked, Release **0 warning/error**, **303 passed, 0 failed/skipped** (35 ca mới). Bao phủ 11 wire fixture, login/cookie/auth, thiếu/sai kết quả, batch partial, timeout/lỗi login, endpoint guard, buyer bank/Customername metadata, replay và claim chung/provider hash isolation. OpenAPI **25 paths/39 schemas**, collection Viettel **17 requests** đã kiểm tra JSON/path; collection MISA giữ 20 requests. Không gọi Viettel thật hoặc UAT.

2026-10-10, thay đổi chỉ tài liệu: chạy lại `dotnet test eInvoice.sln -c Release --no-build --no-restore --verbosity minimal`, **268 passed, 0 failed/skipped** trên artifact Release đã build ở checkpoint 316ba42. Không build lại runtime hoặc restart. JSON request publish/status/document/form-serials trong tài liệu đã kiểm tra theo OpenAPI 3.1, body nhúng trùng file JSON riêng, số tiền mẫu/nhánh HSM nhất quán, route/link hợp lệ. `curl.exe` với TLS mặc định xác nhận GET /health/live trả alive; không gọi MISA trong phiên này. Key postman-local cũ đã hết hạn ngày 09/10/2026; chưa gia hạn/cấp key mới.

`powershell -NoProfile -File scripts/verify.ps1`: **PASS**, Release **0 warning, 0 error**, **268 test passed**, không skip. Có 11 ca diagnostics mới: supplier message/status, redaction và không lưu token/raw exception, lỗi qua restart/site/phase isolation, record cũ không bịa lỗi/key, batch item message, 409 không replay và auth/parameter lookup.

Bao phủ payload fixture, API thật qua HTTP giả lập, credential/token separation, no-secret logging, concurrency/cancellation/invalidation, journal replay/restart/duplicate, partial batch, lỗi/response sai/site scope và đối chiếu OpenAPI/capabilities với route thực tế.

Probe HTTPS local sau chuyển JSON/restart: **9 ca đạt** — health-check 400 MISA_CREDENTIALS_REQUIRED vì file mẫu credential trống; FormSerial 503 MISA_ENDPOINT_NOT_ALLOWED vì endpoint trống; auth/site/type/capabilities/OpenAPI/journal đúng kỳ vọng. Chứng chỉ public ASP.NET localhost export tại .local/dev-certificate.pem được dùng làm trust riêng cho probe, vẫn kiểm tra TLS/hostname. Không gọi NCC thật.

Test suite vẫn dùng fake HTTP. Ngày 2026-10-09 đã thử riêng health-check/login với endpoint testapi.meinvoice.vn do người dùng cấu hình: HTTP 200, connected=true. Chưa UAT các nghiệp vụ MISA còn lại hoặc cutover. Không gọi admin cũ. Repo cũ giữ nguyên thay đổi local trước phiên này; chưa push/deploy.

## Chưa làm và quyết định đang chờ

- Q008: phần date/culture MISA ngoài năm FormSerial vẫn giữ baseline. Provider mới đã được duyệt UTC+7/invariant theo D021.
- Q009: định tuyến MTT/batch dựa trên electronicInvoiceType rõ ràng. Hiện giữ điều kiện baseline; mode 4 cần serial MTT, batch không tự sửa dispatcher cũ. Xem MISA-VARIANTS.
- Q010: giữ hay giới hạn CQT HSM. Chức năng baseline được port với tên tax/send-pending, không nhận invoiceIds vì provider gửi hàng đợi đang chờ. Không coi im lặng là duyệt thay nghiệp vụ.
- Sandbox/UAT từng variant, golden đã che, token receipt expiry, quantity biên lai int, USB correlation/thứ tự và batch partial errors.
- Fast/VNPT/VNPT_V5 chưa port; Cyberbill đang làm một phần. Q015 sửa nháp Minvoice, Q016 health EasyInvoice đang chờ. Log/catalogue, correlation requestId PMS, metrics/throttling/proxy và cấu hình production còn làm sau.
- Storage/reconcile kết quả ghi chưa xác định và kế hoạch thay thế/rollback trước cutover.

## Bước tiếp theo khi tiếp tục

**Bước P2: MISA/Viettel/SUN/Minvoice đã có adapter baseline offline; EasyInvoice xong 12 action nghiệp vụ, health Q016 đang chờ; Cyberbill mới health/document/void, chờ Q012/Q013.** Tiếp theo Fast, rồi VNPT/VNPT_V5. Minvoice auth Q014 đã giải quyết D024; Q015 sửa tiếp nháp đang chờ. Fast cần đọc G01–G06/G26 và source enum/mapper/repository/proxy, username JSON chứa clientC/proxyC/user/unitC, hai URL MTT/GTGT; preserve wire text/plain và thứ tự cột, không tự sửa TaxRate=8 mapping. SOAP/WCF dùng proxy sinh từ WSDL, không viết tay generated code. Không ghi provider chưa port/UAT là hoàn thành. Nguồn pinned chỉ đọc; tiếp tục offline fixture/tests trước UAT. Không sửa credential/site/key hoặc xóa journal.

Phần mềm bên ngoài có thể bắt đầu tích hợp theo INTEGRATION-AGENT sau khi được cấp key còn hạn/SiteId/URL truy cập được và mẫu HSM sandbox đã xác nhận. Nếu nằm máy khác, localhost không dùng được; việc cấp endpoint HTTPS/network cần cấu hình riêng khi người dùng yêu cầu. Chưa có sandbox publish thành công được xác nhận; không đánh dấu production-ready. Các tên trạng thái processing/rejected/unknown mới được thảo luận, **chưa triển khai**; API vẫn pending/completed/not-found và chưa có tự reconcile/unlock.

1. Đọc AGENTS, STATUS, PLAN, DECISIONS, QUESTIONS, MISA-API, MISA-VARIANTS; kiểm tra git status/branch/log. Bộ MISA đã port, không làm lại.
2. Xử lý câu trả lời Q008/Q009/Q010/Q012/Q013/Q015/Q016 nếu có. Các thay đổi phụ thuộc chưa được duyệt; tiếp tục phần độc lập, giữ baseline nếu chưa có câu trả lời.
3. Người dùng điền endpoint/credential **sandbox** vào src/eInvoice.Api/sites.Local.json và hostname vào Providers:Misa:AllowedHosts trong appsettings.Local.json. Không yêu cầu gửi bí mật vào chat/Git. Site phải thuộc API key và khớp header/body/JSON. Không quay lại admin cũ; chọn DB/schema sau khi có yêu cầu.
4. UAT đọc (health/forms/preview/status/document) trước, rồi các thao tác ghi sandbox đã được cho phép. Ghi payload/response đã che và xác minh USB/batch/biên lai. Không gọi production hoặc tự reset pending để retry.
5. Nếu triển khai nhiều instance: thay journal local bằng storage có transaction/unique claims và cơ chế reconcile được thống nhất; làm runbook cutover riêng.
6. Khi có thay đổi code: chạy verify, cập nhật tài liệu và checkpoint Git cục bộ.

## API local/Postman hiện tại

Ngày 2026-10-09 người dùng báo publish 409 khi đổi key. Đã đọc claim/record legacy (pending, không lỗi/key gốc) và đối chiếu hash để nhận diện key gốc; chi tiết request/result chỉ lưu .local, không commit. Đã tái hiện curl bị chặn 409 sau khi xác minh claim pending, không liên hệ MISA/phát hành lại. Lookup invoice-attempts 200 pending, requiresReconciliation=true, canRetry=false. Không xóa/migrate record cũ hoặc tự mở khóa. Chưa thể khôi phục response NCC quá khứ; đã hỏi người dùng nếu còn response đầu tiên, không chặn phần code độc lập.

Đã xử lý ADMIN_NOT_CONFIGURED bằng chuyển sang JSON theo D019; không gọi admin. Ngày 2026-10-09 người dùng đã điền thông tin nhưng health-check trả MISA_ENDPOINT_NOT_ALLOWED: URL HTTPS/trailing slash hợp lệ, allowlist local trống. Đã thêm đúng testapi.meinvoice.vn vào Providers:Misa:AllowedHosts, giữ nguyên các cấu hình khác/journal, restart Release. Health-check thật qua API local trả HTTP 200, provider=MISA, connected=true. Các kết quả credential/endpoint trống bên dưới là probe lịch sử ngày 2026-10-08, không phải trạng thái file hiện tại. Sửa nội dung JSON được đọc ở request tiếp theo, sửa allowlist/path/API key cần restart.

- `https://localhost:7150`, loopback, Release; **API PID 22660, exec session 32920** (restart 2026-10-11 sau EasyInvoice `1d05d97`). GET /health/live 200 và OpenAPI runtime 33 paths/v0.6.0 qua TLS Windows mặc định; không gọi NCC. Kiểm tra executable/listener trước khi dừng vì PID có thể tái sử dụng. Metadata tại .local/local-api.json; kết quả diagnostic rút gọn tại .local/publish-diagnostic-result.json, không lưu token/credential/raw body.
- Key client postman-local, site tổng hợp `11111111-1111-1111-1111-111111111111`, hết hạn cuối 2026-10-09 UTC+7. Key gốc chỉ ở .local; server giữ hash trong appsettings.Local.json (ignored).
- Import `.local/eInvoice-local.postman_environment.json` và `.local/eInvoice-local.postman_collection.json` (20 request). Key đã giữ nguyên, thêm biến invoiceId và request tra cứu invoice-attempts. Bản công khai/template tại docs/postman không chứa key.
- JournalDirectory local là `D:\Code\40.eInvoice\.local\misa-journal`, không xóa khi build/restart. Key idempotency phải ổn định khi retry cùng body; không sinh key mới để vượt pending.
- Probe trước khi người dùng điền thông tin (2026-10-08): JSON site được đọc đúng, health-check trả 400 MISA_CREDENTIALS_REQUIRED. Không phải kết quả kiểm thử sandbox thành công. Thiếu file/sai JSON trả SITE_STORE_*, không trả ADMIN_*.
- Chạy lại khi process đã dừng: `dotnet run --project src/eInvoice.Api -c Release --no-build --no-restore --no-launch-profile --urls https://localhost:7150`.
- Probe mới: .local/probe-json-sites.py và probe-json-results.json (9 ca). Guard chỉ cho file mẫu endpoint/credential trống để không gọi thật. Probe admin cũ đã lỗi thời, không dùng lại.

## Lưu ý

Không chạy lại bootstrap/port/freeze để ghi đè code hoặc capture lại expected từ runtime. Giữ baseline/manifest. TLS mặc định được kiểm tra; chỉ xem xét ngoại lệ khi có endpoint cụ thể. Không đưa credential/dữ liệu thật vào Git.
