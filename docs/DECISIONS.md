# Quyết định

| ID | Ngày | Quyết định | Nguồn |
|---|---|---|---|
| D001 | 2026-10-08 | Dự án mới dùng C#, tên eInvoice; repo riêng tại `D:\Code\40.eInvoice`. | Người dùng |
| D002 | 2026-10-08 | Chỉ triển khai hợp đồng API mới; PMS được cập nhật. Không cần alias route/key cũ. | Trả lời của người dùng |
| D003 | 2026-10-08 | Được đổi key body PMS cho rõ nghĩa; không sửa key phía nhà cung cấp. | Người dùng |
| D004 | 2026-10-08 | Các điểm chưa chắc chắn/nghiệp vụ cần quyết định phải hỏi người dùng. | Người dùng |
| D005 | 2026-10-08 | Nhiều phiên trên máy hiện tại; lưu ngữ cảnh và tiến độ trong repository. | Người dùng |
| D006 | 2026-10-08 | Khung dự án dùng .NET 10, SDK 10.0.401 đã cài trên máy; latestPatch trong cùng feature band; package lock files được commit. | Lựa chọn triển khai |
| D007 | 2026-10-08 | 3 project runtime: Core, Providers, Api; 1 project Tests. Không dựng lại EventStore/template. | Lựa chọn triển khai dựa trên code sống |
| D008 | 2026-10-08 | API mới dùng JSON camelCase; `ezInvoiceId` → `invoiceId`, `oldezInvoiceId` → `originalInvoiceId`, `RequestID` → `requestId`. Bảng đổi đầy đủ nằm trong API-CONTRACT. | Quyền đổi key D003 |
| D009 | 2026-10-08 | API mới dùng HTTP 4xx/5xx phù hợp và JSON lỗi; bỏ hành vi HTTP 200 + body rỗng khi exception. | Người dùng |
| D010 | 2026-10-08 | Triển khai MISA đầu tiên. | Người dùng |
| D011 | 2026-10-08 | PMS chưa có dịch vụ JWT; dùng API key theo hệ PMS, giới hạn khách sạn, hỗ trợ đổi key và HTTPS. SiteId gửi riêng. | Người dùng |
| D012 | 2026-10-08 | Endpoint kiểm tra kết nối MISA không trả token/credential; request provider login giữ key/casing/encoding cũ. Host provider theo allowlist server; không bypass TLS hoặc follow redirect. | Triển khai theo D011; ngoại lệ TLS còn chờ xác minh |
| D013 | 2026-10-08 | Năm truy vấn mẫu/ký hiệu MISA cố định theo UTC+7, không phụ thuộc múi giờ máy chủ. Chỉ áp dụng FormSerial; chưa tự áp dụng vào các mapping tiền/ngày khác. Kiểm thử trước/sau 00:00 ngày 01/01 UTC+7 với clock local UTC. | Người dùng trả lời câu hỏi FormSerial |
| D014 | 2026-10-08 | FormSerial giải mã JSON hợp lệ trước; fallback ba Replace cũ chỉ cho chuỗi escaped/wrapped không hợp lệ. Giữ dấu ngoặc kép/backslash trong tên mẫu; giữ lọc mẫu kế thừa, cắt tên 40 ký tự UTF-16, status và BusinessAreas == 34. API mới thêm siteConfigInfo tùy chọn và trả forms; loại ngoài 0 trả 400 trước lookup/login. | Cải tiến parser/hợp đồng theo D003, D009; fixture + fake HTTP/API tests |

Mỗi quyết định thay đổi hành vi cũ phải bổ sung lý do, phạm vi ảnh hưởng và cách kiểm thử. Quyết định thuộc nghiệp vụ cần người dùng xác nhận.
