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
