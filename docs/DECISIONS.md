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

| D021 | 2026-10-11 | Các nhà cung cấp mới dùng ngày nghiệp vụ UTC+7 và định dạng số ổn định/invariant, giữ nguyên công thức tiền/thuế/làm tròn. Viettel epoch là 00:00 UTC+7, ngày hủy mặc định UTC+7; culture invariant chỉ trong mapper mới. Chưa đổi date/culture MISA ngoài phạm vi D013. | Người dùng trả lời câu hỏi khi tiếp tục các nhà cung cấp khác |
| D022 | 2026-10-11 | Bắt đầu P2 theo yêu cầu người dùng; Viettel port 13 action baseline qua router SiteId chung, giữ cookie access_token và wire JSON/form. Tách journal chung, bảo toàn hash/path MISA; attempt provider riêng nhưng claim invoice/site dùng chung, không mở khóa/retry. Capabilities theo site và OpenAPI chung, bổ sung draft/search/adjust tăng/giảm/thông tin; không giả lập FormSerial/status Viettel không có. | Triển khai theo yêu cầu tiếp tục provider, bảo toàn baseline và kiểm thử offline |

| D023 | 2026-10-11 | SUN port 10 action baseline, giữ hai URL, Bearer và form hủy; sid vẫn bỏ dấu gạch/32 ký tự đầu, trả dưới transactionId. Thêm claim theo sid bên cạnh invoiceId để chặn collision; mã ngắn trả 400 trước NCC. Ngày/culture theo D021; preview là PDF nháp đã có, draft không xác nhận phát hành. Không retry/unlock hoặc đổi công thức. | Lựa chọn hợp đồng theo D003, chống trùng theo D016/D022, đối chiếu fixture và HTTP giả lập |

| D024 | 2026-10-11 | Giữ từng luồng đăng nhập Minvoice cũ để đối chiếu trước sandbox/UAT: nghiệp vụ ma_dvcs=VP và header Bear, health-check dùng Username2/Bearer. Không thống nhất auth từ suy đoán; token được giữ riêng mỗi thao tác, không static/cache cross-tenant. | Người dùng trả lời Q014 |

| D025 | 2026-10-11 | EasyInvoice port 12 action nghiệp vụ, giữ Authentication MD5/nonce/epoch và JSON chứa XML, gom mapping phí/metadata/thuế chung nhưng giữ khác biệt tỷ giá 0 và các thuế suất baseline. Ikey là invoiceId PMS; publish-existing thêm serial nullable/bỏ khi null để replay Minvoice cũ không đổi. Create/draft là unsigned import, không xác nhận phát hành; PDF chỉ nhận bytes thực tế. Hai action Login/HealthCheck chưa quảng cáo là đã xác minh, Q016 đang chờ. | Triển khai theo D003/D009/D016/D021, 20 frozen payload độc lập và HTTP giả lập; không tự chốt Q016 |

Mỗi quyết định thay đổi hành vi cũ phải bổ sung lý do, phạm vi ảnh hưởng và cách kiểm thử. Quyết định thuộc nghiệp vụ cần người dùng xác nhận.
