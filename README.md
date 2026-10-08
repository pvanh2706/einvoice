# eInvoice

Gateway hóa đơn điện tử viết lại bằng C#. Dự án nguồn tại `D:\Code\25.ezInvoice` được dùng để đối chiếu, không sửa trong quá trình viết lại.

**Trạng thái: nền tảng, kiểm tra kết nối và lấy mẫu/ký hiệu hóa đơn MISA. Chưa hoàn thành các luồng phát hành hóa đơn; chưa được xác nhận để thay production.**

## Tiếp tục phiên làm việc

Đọc [AGENTS.md](AGENTS.md), [docs/STATUS.md](docs/STATUS.md), [docs/PLAN.md](docs/PLAN.md), [docs/DECISIONS.md](docs/DECISIONS.md), [docs/QUESTIONS.md](docs/QUESTIONS.md). STATUS ghi kết quả kiểm thử và bước tiếp theo cụ thể; không cần dùng lịch sử chat để đoán tiến độ.

## Cấu trúc

- `src/eInvoice.Core`: canonical DTO, enum/capability baseline, resolver cấu hình và đọc tiền.
- `src/eInvoice.Providers`: admin HTTP client, MISA login/FormSerial transport, XML và bộ đọc tiền SUN.
- `src/eInvoice.Api`: host, API key, quyền theo khách sạn, JSON lỗi và endpoint.
- `tests/eInvoice.Tests`: regression fixture và kiểm thử HTTP giả lập; không gọi admin/provider thật.
- `docs/reference`: tài liệu lịch sử, inventory và hash nguồn theo commit cố định.
- `tools/LegacyFixtureCapture`: chụp mẫu tiền/XML từ helper cũ, hoàn toàn offline; không nằm trong solution runtime.

## Build và kiểm thử

SDK `10.0.401`, runtime .NET 10; xem `global.json`. Phiên bản NuGet nằm trong project và `packages.lock.json`.

```powershell
dotnet restore eInvoice.sln --locked-mode
dotnet build eInvoice.sln -c Release --no-restore
dotnet test eInvoice.sln -c Release --no-build --no-restore
```

Hoặc chạy `powershell -NoProfile -File scripts/verify.ps1` (cũng dùng được với pwsh).

## Chạy local

```powershell
dotnet run --project src/eInvoice.Api --no-launch-profile --urls http://localhost:5150
```

`GET /health/live` là liveness, không gọi dịch vụ ngoài. API nghiệp vụ yêu cầu HTTPS; cấu hình dev certificate hoặc TLS ở host khi cần thử endpoint đó. [Cấu hình local và API key](docs/LOCAL-SETUP.md).

Endpoint hiện có:

- `GET /health/live`: kiểm tra tiến trình.
- `POST /api/eInvoice/health-check`: xác thực và kiểm tra kết nối MISA; không trả token/credential.
- `POST /api/eInvoice/form-serials`: lấy mẫu/ký hiệu MISA cho `invoiceType: 0`, năm hiện tại theo UTC+7.

Hợp đồng mới, JSON mẫu và mã lỗi: [API-CONTRACT](docs/API-CONTRACT.md). Các route/key cũ không được hỗ trợ.

## Nguồn và fixture

Code baseline: `66a9c742d0f6fb692d339076e6cd82780e3476be`. [Reference](docs/reference/README.md) giải thích khác biệt với tài liệu cũ `f77fa2e`.

Fixture tiền/XML là characterization từ code cũ với dữ liệu tổng hợp, không phải golden transaction production. Chụp lại chỉ khi đã xem diff và có lý do; không tự cập nhật kết quả kỳ vọng để làm test qua.

```powershell
dotnet run --project tools/LegacyFixtureCapture -- tests/eInvoice.Tests/Fixtures/LegacyHelpers
```

Công cụ giữ Newtonsoft 13.0.1 đúng phiên bản cũ; production mới dùng 13.0.4 và được so với XML đã capture. Các source lịch sử giữ nguyên tên để truy vết; code runtime dùng eInvoice.
