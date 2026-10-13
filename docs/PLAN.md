# Kế hoạch viết lại eInvoice

Trạng thái chi tiết hiện tại: `STATUS.md`. Baseline và những thay đổi kể từ tài liệu cũ: `reference/README.md`.

## P0 — Khung và khả năng tiếp tục giữa các phiên

- [x] Hoàn thành AGENTS, STATUS, PLAN, DECISIONS, QUESTIONS, API-CONTRACT và snapshot tài liệu tham chiếu.
- [x] Khóa SDK/package; solution Core/Providers/Api/Tests; script verify.
- [x] Trích inventory 33 endpoint, 32 action, 9 provider từ code baseline (capability chưa đồng nghĩa đã implement).
- [x] Port canonical DTO có tên mới, enum có giá trị số cũ; kiểm thử JSON/default/decimal.
- [x] Port các helper đọc tiền/XML và đóng băng mẫu từ code cũ bằng công cụ capture offline.
- [x] JSON site store theo D019, abstraction ISiteConfigurationStore để thay DB riêng; resolver ưu tiên cấu hình request. Client admin baseline chỉ giữ tham chiếu/test, không đăng ký vào API.
- [x] Host chạy được và health endpoint; endpoint phát hành MISA đã nối adapter và kiểm thử offline ở P1.

## P1 — Một provider đầy đủ theo từng luồng

- [x] Chốt API/authentication/status/provider đầu tiên (D002, D009, D010, D011).
- [x] API key + quyền theo site + rotation/expiry/HTTPS, kiểm thử header/body không thể đổi site.
- [x] MISA kiểm tra kết nối qua login transport; kiểm thử payload/lỗi fake HTTP.
- [x] Lập bảng variant baseline MISA; port FormSerial (90s, token v3, UTC+7), fixture và endpoint mới có site scope.
- [x] Dựng interface theo thao tác, context theo tenant, capability rõ ràng và transport test doubles.
- [x] Provider đầu tiên **MISA**: mapping request/response 19 action baseline, hai hệ credential, đọc/ghi, timeout và journal local chống trùng qua restart.
- [x] Expose endpoint mới cho những luồng đã hoàn thành; OpenAPI 3.1 + Postman + tài liệu request/response.
- [x] Bàn giao tài liệu INTEGRATION-AGENT và JSON mẫu cho phần mềm bên ngoài bắt đầu single HSM MISA sandbox; ghi rõ key test hết hạn, loopback và giới hạn trạng thái/đối soát. Chưa cấp key/URL mới hoặc xác nhận publish sandbox thành công.
- [x] Kiểm thử payload provider theo 16 fixture độc lập và kiểm thử end-to-end qua HTTP giả lập; tổng 268 test Release đạt, gồm JSON store/API và durable diagnostics/redaction/lookup.
- [ ] Sandbox/UAT từng variant và xử lý Q008/Q009/Q010 trước khi thay nghiệp vụ/cutover.

## P2 — Hoàn thành các provider còn lại

| Provider | Mapping | Transport | Regression fixture | API end-to-end | UAT |
|---|---|---|---|---|---|
| VNPT | 17 action baseline + IsItemSpecialTax current supplement | WSDL-generated SOAP V1/V5/TLYT; request scope | V1/V5/MTT/receipt trong 24 fixture FAST/VNPT độc lập | Đủ route fake invoker + WCF formatter; lỗi/replay/timeout | Chưa |
| MINVOICE | 15 action baseline; tiền/ngoại lệ site giữ nguyên | REST VP/Bear nghiệp vụ, unit cấu hình cho health theo D024; không static | 11 frozen payload, có hai ngoại lệ phí dịch vụ | 14 POST route; 397 tests toàn solution; Q015 sửa nháp tiếp đang chờ | Chưa |
| SUN | 10 action baseline, UTC+7/invariant | REST Bearer từng operation, hai endpoint JSON/form | 10 payload frozen | Đủ route fake HTTP, 358 tests toàn solution | Chưa |
| VIETTEL | 13 action baseline | REST cookie token từng operation, UTF-8 JSON/form, no retry | 11 frozen payload | Đủ route qua fake HTTP; toàn solution 303 tests | Chưa |
| VNPT_V5 | 21 action baseline, giữ CREATED/EDIT_DRAFT dispatcher | WSDL-generated SOAP V5, không retry ghi | Payload baseline và certificate/CQT fake | Đủ route và biến thể MTT/receipt, delete-before-publish | Chưa |
| MISA | 19 action sống baseline | v3 + biên lai, không retry ghi | 16 payload độc lập + FormSerial + helper | Đủ route qua HTTP giả lập, 268 test toàn solution; health-check thật 200, publish diagnostic bị khóa | Chưa |
| CYBER_BILL | 11 action, paymentMethod mã số theo D026 | REST Bearer từng operation; create → sign riêng | 7 payload độc lập | Đủ route, unsigned requiresSigning, replay/validation | Chưa |
| FAST | 13 action baseline, giữ TaxRate=8 → TaxAmount0 | REST text/plain MTT + SOAP GTGT; token scoped | 10 frozen payload + API fake HTTP | Đủ action; health configuration-only theo D027 | Chưa |
| EASY_INVOICE | 14 action, 12 nghiệp vụ + Login/Health config validation | REST Authentication riêng từng request, JSON/XML | 20 frozen payload | Health connected=false/connectionVerified=false; không gọi NCC | Chưa |

P2 code/offline hoàn thành ngày 13/10/2026: đủ 9 provider / 133 action sống baseline. Q012/Q013/Q016/Q017 đã chốt; Q015 và các cải tiến MISA vẫn chờ trước khi thay nghiệp vụ. Tiếp theo là UAT từng biến thể và P3, không làm lại adapter đã port. Không tự gọi NCC/production hoặc mở khóa journal.

## P3 — Vận hành và thay thế

- [ ] Log có correlation và che credential, metrics/time từng chặng, log search API và catalogue còn dùng.
- [x] Token v3/cache phân tách tenant + concurrency/invalidation tests; token biên lai dùng trong thao tác, health-check login mới.
- [ ] Culture/timezone ngoài năm FormSerial và routing MTT/batch chờ Q008/Q009; giữ công thức tiền/thuế baseline.
- [ ] Kiểm thử tích hợp sandbox, dữ liệu production đã che và PDF/XML đầy đủ.
- [ ] Shadow chỉ dựng payload, kế hoạch routing/rollback theo provider, runbook cấu hình.
- [ ] Người dùng xác nhận cutover. Đây là bước riêng sau khi triển khai và kiểm chứng xong.

## Điều kiện hoàn thành mỗi provider

Giá trị enum/capability đúng baseline; đúng credential; đúng key/type/null/default/encoding/order phía nhà cung cấp; đủ nhánh loại hóa đơn; response/error/timeout đúng quyết định; không gọi thật trong test; có fixture và test các trường hợp lỗi/biên; đã ghi rõ phần nào chưa được UAT.
