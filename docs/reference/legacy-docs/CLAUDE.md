# Hướng dẫn cho AI làm việc trên bộ tài liệu này

## 🔴 Cảnh báo bắt buộc đọc trước

`README.md` ở thư mục gốc của repo ezInvoice mô tả dự án là **CQRS / Event Sourcing / DDD**.
**ĐIỀU ĐÓ SAI.** Đó là văn bản còn sót của template EquinoxProject (Eduardo Pires, 2016).

**Không được** suy ra kiến trúc từ:
- `README.md` gốc
- Tên thư mục (`EventSourcedNormalizers` **không** liên quan gì tới Event Sourcing — đó là các bộ chuyển đổi dữ liệu)
- Cấu trúc solution (12 project, chỉ 1 được deploy)
- Sự tồn tại của một file `.cs` (phải kiểm tra `.csproj` — có file bị `<Compile Remove>`)

**Luôn đọc [`00-START-HERE.md`](00-START-HERE.md) trước tiên.**

---

## Thứ tự đọc

| # | File | Khi nào cần |
|---|---|---|
| 1 | [`00-START-HERE.md`](00-START-HERE.md) | **Luôn luôn.** Code sống/chết, 5 sự thật phản trực giác, chiến lược kiểm chứng |
| 2 | [`01-ma-tran-partner-action.md`](01-ma-tran-partner-action.md) | Khi làm việc với định tuyến, enum, endpoint |
| 3 | [`02-bang-credential.md`](02-bang-credential.md) | Khi làm việc với cấu hình, kết nối, migrate dữ liệu |
| 4 | [`03-so-tay-bay.md`](03-so-tay-bay.md) | **Trước khi sửa bất kỳ hành vi nào có vẻ "sai"** |

---

## Quy tắc khi viết lại

### 1. Không "sửa cho đúng chuẩn" nếu chưa tra sổ bẫy

Phần lớn chỗ kỳ quặc trong ezInvoice là **cố ý** hoặc **đã được PMS/nhà cung cấp thích nghi**.
Ví dụ đã biết:

- Luôn trả **HTTP 200**, kể cả khi lỗi (mã lỗi nằm trong body)
- Khi exception → **HTTP 200 + body RỖNG**
- Gửi JSON bằng **UTF-16** sang ezInvoiceAdmin
- Token Minvoice gửi dạng `"Bear "` (thiếu chữ `er`)
- Bỏ qua kiểm tra chứng chỉ SSL, ép TLS 1.2
- Enum `INVOICE_ACTION` nhảy số (thiếu 13)
- Chuỗi lỗi `"Đơn vi cung cấp không có chức năng này!"` thiếu dấu

Mỗi mục trong [`03-so-tay-bay.md`](03-so-tay-bay.md) cần một **quyết định của con người**
(`GIỮ NGUYÊN` / `SỬA` / `CẦN HỎI`). **AI không tự quyết.**

### 2. Không port code chết

Xem bảng ở [`00-START-HERE.md §4`](00-START-HERE.md). Tóm tắt: bỏ `Domain`, `Domain.Core`,
`Infra.CrossCutting.Bus`, `Infra.CrossCutting.Identity`, `Infra.Data`, `UI.Web`, `Front`,
`AccountController`, `CustomerController`, toàn bộ Event Sourcing.

Đặc biệt chú ý **code chết nằm lẫn trong code sống** (§4, bảng cuối) — ví dụ
`GetFastInfoToken` có 0 call site nhưng đọc credential theo cách hoàn toàn khác hàm thật.

### 3. Không sinh lại 44.865 dòng WCF proxy bằng tay

`src/.../Vnpt/Connected Services/*/Reference.cs` là **code tự sinh**. Ở hệ mới:
lấy WSDL → dùng công cụ sinh client của ngôn ngữ đích.

### 4. Mọi thay đổi hành vi phải có golden test

Không có test nào trong repo cũ. Lưới an toàn duy nhất là golden dataset trích từ
Elasticsearch — xem [`00-START-HERE.md §6`](00-START-HERE.md).

### 5. Khi không chắc: đánh dấu `CẦN HỎI`, đừng đoán

Danh sách câu hỏi mở ở [`00-START-HERE.md §8`](00-START-HERE.md).
Đây là hệ thống phát hành **hóa đơn thuế** — đoán sai công thức tiền hoặc quy tắc làm tròn
tạo ra chứng từ sai về mặt pháp lý.

---

## Cách kiểm chứng một khẳng định trong tài liệu

Mọi dẫn chiếu đều theo dạng `đường/dẫn/file.cs:dòng`, chốt ở commit `f77fa2e`.

```bash
cd D:/Code/25.ezInvoice
git log -1 --format=%H            # xác nhận commit
sed -n '929,960p' src/ezInvoice.Application/EventSourcedNormalizers/FastNormalizes/FastConvertRequest.cs
```

Nếu số dòng lệch (do repo đã tiến triển), tìm bằng nội dung thay vì số dòng:
```bash
grep -rn "GetFastInfoTokenFromJson" src --include="*.cs"
```

---

## Trạng thái bộ tài liệu

| Tài liệu | Trạng thái |
|---|---|
| `00-START-HERE.md` | ✅ Hoàn chỉnh |
| `01-ma-tran-partner-action.md` | ✅ Hoàn chỉnh, trích tự động + xác minh tay |
| `02-bang-credential.md` | ✅ Hoàn chỉnh, xác minh trực tiếp từ code |
| `03-so-tay-bay.md` | ⚠️ 194 bẫy đã liệt kê, **cột "Quyết định" còn trống** |
| Golden dataset | ❌ **Chưa làm** — cần quyền truy cập Elasticsearch |
| Đặc tả công thức tiền / làm tròn | ❌ Chưa làm |
| OpenAPI 33 endpoint | ❌ Chưa làm |
| Từ điển Canonical Invoice Model | ❌ Chưa làm — cần phỏng vấn đội PMS |
| Sổ tay mã lỗi hợp nhất | ❌ Chưa làm |
