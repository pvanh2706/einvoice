# Quy tắc tiếp tục dự án eInvoice

## Bắt đầu mỗi phiên

1. Đọc `docs/STATUS.md`, `docs/PLAN.md`, `docs/DECISIONS.md`, `docs/QUESTIONS.md`.
2. Kiểm tra `git status --short`, branch và commit mới nhất. Giữ nguyên thay đổi của người dùng.
3. Đọc `docs/reference/README.md`; khi port một luồng, đọc code cũ và các mục liên quan trong `docs/reference/legacy-docs/03-so-tay-bay.md`.
4. Tiếp tục đúng bước trong STATUS. Đừng suy ra rằng adapter đã hoàn thành chỉ vì capability có trong inventory.

## Phạm vi và quyết định đã được người dùng cho phép

- Viết lại bằng C#; tên sản phẩm, namespace, solution và cấu hình mới là **eInvoice**.
- Chỉ dùng API/body mới. PMS sẽ được cập nhật; không triển khai alias `api/ezInvoice` hoặc key cũ.
- Được đổi key body phía PMS để rõ nghĩa; mọi thay đổi phải ghi ở `docs/API-CONTRACT.md`.
- **Không đổi key, XML element, SOAP action hoặc định dạng dữ liệu phía nhà cung cấp.**
- Code cũ tại `D:\Code\25.ezInvoice` là nguồn tham chiếu chỉ đọc. Baseline: `66a9c742d0f6fb692d339076e6cd82780e3476be`.
- Tên và đường dẫn lịch sử trong reference được giữ để truy vết. Không thay thế chuỗi hàng loạt trong provider payload.
- Công việc kéo dài nhiều phiên trên **máy này**; không phụ thuộc vào lịch sử chat để tiếp tục.

## Khi cần hỏi người dùng

- Hỏi khi có điểm chưa chắc chắn hoặc cần quyết định nghiệp vụ; ghi câu hỏi vào QUESTIONS trước khi dừng.
- Không tự chốt công thức tiền/thuế/làm tròn, định nghĩa trường, authentication, status HTTP, SSL exception hoặc quy tắc retry chưa được quyết định.
- Tiếp tục phần độc lập khi chờ trả lời. Không coi việc chưa có câu trả lời là chấp thuận.
- Cột quyết định trong sổ bẫy cũ còn trống. Quyết định mới thuộc DECISIONS; không chỉnh bản reference lịch sử.

## Triển khai

- .NET SDK được khóa trong `global.json`; build/test bằng solution mới, không dùng SDK config của repo cũ.
- Core độc lập với ASP.NET và provider transport. Mapping provider không phụ thuộc controller.
- Dùng decimal cho tiền. Giữ default, sentinel enum và sự khác biệt từng biến thể khi chưa có quyết định thay đổi.
- Không port Customer/template, CQRS/EventStore, Identity mẫu, Front/UI cũ.
- WCF client được sinh từ WSDL; không viết tay generated proxy.
- Không tự gọi API production, phát hành/hủy hóa đơn hoặc gửi email thật trong build/test/capture/shadow.
- Không retry thao tác ghi khi chưa có cơ chế chống trùng được kiểm chứng.
- Không commit credential, appsettings thật, log production hoặc dữ liệu cá nhân. Fixture dùng dữ liệu tổng hợp/đã che.
- Không push, deploy hoặc thay routing production chỉ vì bước kiểm thử cục bộ đã qua.

## Kiểm chứng và kết thúc phiên

- Chạy `pwsh -File scripts/verify.ps1` hoặc các lệnh tương đương ghi trong README.
- Test phải chứng minh hành vi: đối chiếu fixture, fake HTTP, lỗi/timeout, concurrency hoặc hợp đồng API; tránh test chỉ lặp lại code.
- Cập nhật STATUS: phần hoàn thành, kết quả test, phần chưa làm, vấn đề cần hỏi và bước tiếp theo cụ thể.
- Cập nhật PLAN, DECISIONS, QUESTIONS khi có thay đổi. Lưu checkpoint Git cục bộ khi phù hợp.
- Nếu build/test lỗi, ghi lỗi và cách tái hiện; không ghi phần còn dang dở là hoàn thành.
