# Prompt giao AI dự án tích hợp eInvoice

Copy nội dung dưới đây vào AI của dự án nhận, kèm bộ tài liệu eInvoice. Thay các placeholder có thông tin đã được cấp. API key thật đưa qua secrets của môi trường, không dán vào prompt hoặc tài liệu.

---

Hãy triển khai tích hợp phát hành hóa đơn điện tử qua gateway eInvoice trong backend của dự án hiện tại.

Đọc `EXTERNAL-INTEGRATION.md` trong bộ tài liệu đính kèm trước, rồi OpenAPI và tài liệu provider tương ứng. Nếu bộ docs đặt trong repository gateway thì đường dẫn là `D:\Code\40.eInvoice\docs\EXTERNAL-INTEGRATION.md`. Đọc AGENTS/convention của dự án hiện tại và tự xác định stack, database, job và phân quyền đang có. Không yêu cầu dự án chuyển stack để tích hợp.

Thông tin môi trường được cấp:

- Gateway base URL: `<EINVOICE_BASE_URL, HTTPS, không gồm /api/eInvoice>`.
- Site/tenant mapping: `<EINVOICE_SITE_ID hoặc nguồn mapping từng tenant>`.
- API key: lấy từ secret `EINVOICE_API_KEY` của backend.
- Provider dự kiến: `<MISA/VNPT/VNPT_V5/MINVOICE/SUN/VIETTEL/CYBER_BILL/FAST/EASY_INVOICE>`.
- Môi trường/cách ký: `<sandbox hoặc production, HSM/MTT/USB>`.
- Mẫu/ký hiệu: `<do quản trị cấp hoặc lấy form-serials khi hỗ trợ>`.
- Nguồn dữ liệu hóa đơn: `<module/entity hiện có; tự kiểm tra code nếu chưa chỉ rõ>`.

Yêu cầu thi công:

1. Xây HTTP client backend, cấu hình HTTPS/API key/site, khám phá capabilities và chọn đúng luồng provider. Không gọi API NCC trực tiếp; không gửi credential NCC/siteConfigInfo từ frontend.
2. Map hóa đơn/chi tiết/người mua/thuế/tiền từ domain sang DTO camelCase strict của gateway. Hóa đơn thông thường đặt invoiceType=0 rõ ràng. Tiền dùng decimal; không tự thay công thức thuế/chiết khấu/phí/làm tròn. Ghi lại trường cần người phụ trách xác nhận và tiếp tục phần độc lập.
3. Lưu invoiceId, operation, snapshot body, Idempotency-Key trước HTTP; khóa concurrency và phục hồi sau restart. Không tạo key mới cho mỗi lần gọi HTTP. Không retry POST write tự động ở client/job/proxy.
4. Triển khai phát hành, lưu đúng từng mã NCC và kết quả; parse HTTP 200 theo từng item/isSuccess/operation/requiresSigning. Với Cyberbill dùng create rồi publish-existing, không tự dùng publish. Phân biệt tạo nháp và phát hành ở từng NCC.
5. Timeout/mất response/409/lỗi sau gửi: lưu cần đối soát, tra GET operations hoặc invoice-attempts bằng key/ID gốc. Journal pending/not-found không chứng minh chưa phát hành. Không đổi ID/site/provider/key hoặc xóa claim để vượt khóa. Chưa có API tự reconcile/unlock.
6. Thêm chức năng xem kết quả, tải document, tra attempt và hiển thị cần đối soát; CQT là trạng thái riêng nếu NCC có hỗ trợ. Không gọi tax/send-pending mặc định sau publish.
7. Parse Problem Details và lưu requestId/code/message đã lọc; giữ API key/credential và thông tin người mua khỏi log chung. Không tắt TLS hoặc gửi key sang URL tài liệu NCC.
8. Viết migration, kiểm thử fake HTTP cần thiết: thành công, concurrent submit, restart/key persistence, timeout→completed, pending/not-found, 409, 401/403/422/502/504, response hỏng, batch partial nếu hỗ trợ và nháp→ký nếu có. Không phát hành/hủy/gửi email thật trong unit test/CI.
9. Bàn giao code tích hợp theo convention dự án, cấu hình mẫu không secret, hướng dẫn chạy, bảng mapping và báo cáo kiểm thử. Nếu thiếu URL/key/site/mẫu, hoàn thành code/mock độc lập và liệt kê chính xác cấu hình còn thiếu. Chỉ chạy sandbox nghiệp vụ khi đã được cấp môi trường và phạm vi thử; không tự deploy hoặc gọi production.

Làm phần có đủ thông tin ngay, không dừng ở kế hoạch. Chỉ hỏi khi thiếu dữ liệu hoặc quyết định nghiệp vụ ảnh hưởng tính đúng đắn. Báo rõ phần đã kiểm thử offline và phần chưa xác nhận bằng giao dịch sandbox thực tế.
