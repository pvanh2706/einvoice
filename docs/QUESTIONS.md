# Câu hỏi và quyết định đang chờ

## Đã giải quyết

- Q001: Có giữ alias route/key cũ không? **Không**. PMS chuyển sang hợp đồng mới (D002).
- Q002: HTTP status/error? **HTTP 4xx/5xx phù hợp và luôn JSON lỗi** (D009).
- Q004: Provider đầu tiên? **MISA** (D010).
- Q003: Authentication? **API key theo hệ PMS, giới hạn khách sạn, HTTPS** (D011).
- Q007: Năm lấy mẫu/ký hiệu MISA theo múi giờ nào? **Cố định UTC+7** (D013).
- Q011: Không dùng admin cũ; tạm dùng JSON server để test, sau này DB riêng (D019). Site mẫu dùng GUID local có sẵn; credential/hostname sandbox do người dùng điền tại máy.

## Đang chờ trước khi thay đổi nghiệp vụ/cutover

- Q016 đã giải quyết 13/10/2026: EasyInvoice kiểm tra cấu hình, trả connectionVerified=false; người dùng cũng duyệt cùng hành vi cho FAST (Q017). Không gọi NCC hoặc giả định đã xác minh kết nối.

- Q014 đã giải quyết 11/10/2026: người dùng chọn giữ từng luồng Minvoice cũ để đối chiếu; nghiệp vụ VP/Bear, health-check Username2/Bearer. Token riêng theo thao tác, không static; xem D024.
- Q015 (đã hỏi 11/10/2026): Minvoice change cho sửa tiếp với key mới sau khi lần trước completed, nhưng pending/timeout vẫn khóa để đối soát, hay giữ khóa một lần theo hóa đơn? Chưa đổi cơ chế claim cho tới khi trả lời.

- Q012 đã giải quyết 13/10/2026: Cyberbill giữ hai bước tạo → ký như code cũ. create unsigned, publish-existing ký; không tự ghép hai bước.
- Q013 đã giải quyết 13/10/2026: Cyberbill giữ paymentMethod là mã số 1–5 theo baseline.

- Q008: Người dùng **đã duyệt cho các nhà cung cấp mới** ngày UTC+7 và định dạng số invariant, giữ công thức tiền/thuế/làm tròn (D021, 11/10/2026). Phần mở rộng date/ticks/culture MISA ngoài năm FormSerial vẫn chưa được duyệt; giữ baseline MISA.
- Q009 (đã hỏi, đang chờ): Định tuyến MTT/HSM theo electronicInvoiceType rõ ràng, kiểm tra ký hiệu nhất quán, từ chối batch trộn site/type/mode/form/serial? Hiện mapper/transport đã port, định tuyến vẫn theo baseline trong lúc chờ.
- Q010 (đã hỏi, đang chờ): Giữ CQT HSM hay giới hạn chỉ USB? Để hoàn tất chức năng tương đương baseline theo yêu cầu viết lại, route HSM được giữ dưới tên tax/send-pending, body không nhận invoiceIds. Chưa áp dụng/giả định một lựa chọn thay đổi nghiệp vụ; không gọi thật hay deploy.

- Q005: Có nhà cung cấp nào bắt buộc ngoại lệ kiểm tra chứng chỉ TLS? Cần endpoint/certificate sandbox thực tế; không bật bypass chung.
- Q006: Môi trường đang chạy sử dụng culture/timezone nào? Các mapping tiền/ngày/epoch cũ còn cần chốt; câu trả lời Q007 chỉ giải quyết năm truy vấn FormSerial.

## Cần dữ liệu bổ sung trước khi thay production

- Golden giao dịch đã che (request PMS, request provider, response provider, response PMS), PDF/XML đầy đủ.
- API/WSDL và sandbox credential từng provider; chuyển qua cấu hình local/secrets, không ghi vào tài liệu Git.
- Xác nhận ý nghĩa SubAmount, BeforeTaxAmount, AfterTaxAmount, ServiceCharge; những mục giữ/sửa trong sổ bẫy.
- Phạm vi còn dùng của API log, InvoiceActions/InvoiceSystems và hợp đồng hệ thống admin bên ngoài.

Các mục chưa có câu trả lời giữ trạng thái mở; không tự chuyển thành quyết định.
