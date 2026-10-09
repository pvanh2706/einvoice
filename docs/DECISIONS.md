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

| D015 | 2026-10-08 | Hoàn tất 19 action sống MISA theo baseline cố định, giữ công thức/culture/date/routing chưa được duyệt thay đổi (Q008/Q009). CQT HSM dùng route tax/send-pending, body không invoiceIds vì provider không lọc theo ID. Q010 vẫn mở; chưa gọi thật. | Yêu cầu hoàn thiện bộ MISA; lựa chọn hợp đồng theo D003, không suy ra duyệt cải tiến từ im lặng |
| D016 | 2026-10-08 | Các thao tác ghi MISA yêu cầu Idempotency-Key; journal local bền theo site/operation và claims invoice/phase, replay completed, body mismatch/pending trả 409. Lỗi trước business POST có thể retry; lỗi sau dispatch giữ pending để đối soát. Không tự retry/reset; storage nhiều instance là bước riêng. | Cải tiến chống trùng theo yêu cầu viết lại; fake HTTP + restart/concurrency/timeout tests |
| D017 | 2026-10-08 | Cache v3 bounded/single-flight, fingerprint HMAC tách tenant/endpoint/tax/account/credential, expiry UTC theo giới hạn 7 ngày baseline trừ 5 phút; health-check bypass cache. Biên lai chỉ tái sử dụng trong một thao tác vì chưa xác minh expiry liên request. Tắt logger URI chứa query credential. | Bảo toàn credential và kiểm chứng concurrency/invalidation/no-secret logging |
| D018 | 2026-10-08 | Giữ batch item success/failure và correlation theo invoiceId; chỉ trả symbolic providerCode từ allowlist, không trả raw supplier description. Response thiếu dữ liệu/ID ngoài request trả 502; wire key NCC giữ nguyên. | Hợp đồng rõ nghĩa/an toàn D003, D009; fixture + malformed/partial/error tests |

| D019 | 2026-10-08 | Bỏ dịch vụ admin cũ khỏi API. Cấu hình khách sạn tạm đọc từ JSON server sites.Local.json theo X-Site-Id; sau này DB riêng qua ISiteConfigurationStore. Giữ config request ưu tiên như hợp đồng hiện tại. Không chọn DB/schema lúc này. File thật ignored, lỗi JSON/ID trùng fail closed, không fallback network/site khác; thay credential được đọc ở request sau. | Người dùng: không dùng link admin cũ, sẽ lưu DB riêng, tạm dùng JSON để test |

| D020 | 2026-10-09 | Theo yêu cầu xem lỗi NCC khi publish: trả/lưu riêng message lỗi từ trường supplier xác định, sau che credential/token, lọc structured payload/control characters/giới hạn 1000 ký tự. Giữ providerCode allowlist và thêm providerHttpStatus. Journal lưu lastError/key/operation/requestId gốc; 409 trả previousAttempt và link tra cứu theo invoiceId. Đọc record cũ không bịa dữ liệu thiếu; không đổi retry/release claim hoặc sửa wire NCC. | Người dùng cung cấp curl lỗi publish và yêu cầu biết response/message để xử lý; fake HTTP/restart/site isolation/redaction tests |

Mỗi quyết định thay đổi hành vi cũ phải bổ sung lý do, phạm vi ảnh hưởng và cách kiểm thử. Quyết định thuộc nghiệp vụ cần người dùng xác nhận.
