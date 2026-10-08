# Kế hoạch viết lại eInvoice

Trạng thái chi tiết hiện tại: `STATUS.md`. Baseline và những thay đổi kể từ tài liệu cũ: `reference/README.md`.

## P0 — Khung và khả năng tiếp tục giữa các phiên

- [x] Hoàn thành AGENTS, STATUS, PLAN, DECISIONS, QUESTIONS, API-CONTRACT và snapshot tài liệu tham chiếu.
- [x] Khóa SDK/package; solution Core/Providers/Api/Tests; script verify.
- [x] Trích inventory 33 endpoint, 32 action, 9 provider từ code baseline (capability chưa đồng nghĩa đã implement).
- [x] Port canonical DTO có tên mới, enum có giá trị số cũ; kiểm thử JSON/default/decimal.
- [x] Port các helper đọc tiền/XML và đóng băng mẫu từ code cũ bằng công cụ capture offline.
- [x] Client eInvoiceAdmin async, injectable HTTP, hỗ trợ body UTF-16 cũ, kiểm thử fake HTTP; resolver ưu tiên cấu hình request.
- [x] Host chạy được và health endpoint; chưa expose endpoint phát hành khi adapter chưa được port.

## P1 — Một provider đầy đủ theo từng luồng

- [x] Chốt API/authentication/status/provider đầu tiên (D002, D009, D010, D011).
- [x] API key + quyền theo site + rotation/expiry/HTTPS, kiểm thử header/body không thể đổi site.
- [x] MISA kiểm tra kết nối qua login transport; kiểm thử payload/lỗi fake HTTP.
- [x] Lập bảng variant baseline MISA; port FormSerial (90s, token v3, UTC+7), fixture và endpoint mới có site scope.
- [ ] Dựng interface theo thao tác, context theo tenant, capability rõ ràng và transport test doubles.
- [ ] Provider đầu tiên **MISA**: mapping request/response, credential, đọc/ghi, timeout và khóa chống trùng.
- [ ] Expose endpoint mới cho những luồng đã hoàn thành; mô tả OpenAPI + ví dụ request/response.
- [ ] Kiểm thử payload provider theo fixture và kiểm thử end-to-end qua HTTP giả lập.

## P2 — Hoàn thành các provider còn lại

| Provider | Mapping | Transport | Regression fixture | API end-to-end | UAT |
|---|---|---|---|---|---|
| VNPT | Chưa | Chưa | Chưa | Chưa | Chưa |
| MINVOICE | Chưa | Chưa | Chưa | Chưa | Chưa |
| SUN | Chưa | Chưa | Chưa | Chưa | Chưa |
| VIETTEL | Chưa | Chưa | Chưa | Chưa | Chưa |
| VNPT_V5 | Chưa | Chưa | Chưa | Chưa | Chưa |
| MISA | Login + FormSerial | Login v3 + GET templates | Login fake + FormSerial fixture + helper | Health-check + FormSerial | Chưa |
| CYBER_BILL | Chưa | Chưa | Chưa | Chưa | Chưa |
| FAST | Chưa | Chưa | Chưa | Chưa | Chưa |
| EASY_INVOICE | Chưa | Chưa | Chưa | Chưa | Chưa |

## P3 — Vận hành và thay thế

- [ ] Log có correlation và che credential, metrics/time từng chặng, log search API và catalogue còn dùng.
- [ ] Token/cache phân tách tenant + concurrency tests; culture/timezone theo quyết định đã chốt.
- [ ] Kiểm thử tích hợp sandbox, dữ liệu production đã che và PDF/XML đầy đủ.
- [ ] Shadow chỉ dựng payload, kế hoạch routing/rollback theo provider, runbook cấu hình.
- [ ] Người dùng xác nhận cutover. Đây là bước riêng sau khi triển khai và kiểm chứng xong.

## Điều kiện hoàn thành mỗi provider

Giá trị enum/capability đúng baseline; đúng credential; đúng key/type/null/default/encoding/order phía nhà cung cấp; đủ nhánh loại hóa đơn; response/error/timeout đúng quyết định; không gọi thật trong test; có fixture và test các trường hợp lỗi/biên; đã ghi rõ phần nào chưa được UAT.
