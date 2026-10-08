# Câu hỏi và quyết định đang chờ

## Đã giải quyết

- Q001: Có giữ alias route/key cũ không? **Không**. PMS chuyển sang hợp đồng mới (D002).
- Q002: HTTP status/error? **HTTP 4xx/5xx phù hợp và luôn JSON lỗi** (D009).
- Q004: Provider đầu tiên? **MISA** (D010).
- Q003: Authentication? **API key theo hệ PMS, giới hạn khách sạn, HTTPS** (D011).
- Q007: Năm lấy mẫu/ký hiệu MISA theo múi giờ nào? **Cố định UTC+7** (D013).

## Cần hỏi trước khi expose API nghiệp vụ

- Q005: Có nhà cung cấp nào bắt buộc ngoại lệ kiểm tra chứng chỉ TLS? Cần endpoint/certificate sandbox thực tế; không bật bypass chung.
- Q006: Môi trường đang chạy sử dụng culture/timezone nào? Các mapping tiền/ngày/epoch cũ còn cần chốt; câu trả lời Q007 chỉ giải quyết năm truy vấn FormSerial.

## Cần dữ liệu bổ sung trước khi thay production

- Golden giao dịch đã che (request PMS, request provider, response provider, response PMS), PDF/XML đầy đủ.
- API/WSDL và sandbox credential từng provider; chuyển qua cấu hình local/secrets, không ghi vào tài liệu Git.
- Xác nhận ý nghĩa SubAmount, BeforeTaxAmount, AfterTaxAmount, ServiceCharge; những mục giữ/sửa trong sổ bẫy.
- Phạm vi còn dùng của API log, InvoiceActions/InvoiceSystems và hợp đồng hệ thống admin bên ngoài.

Các mục chưa có câu trả lời giữ trạng thái mở; không tự chuyển thành quyết định.
