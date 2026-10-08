# ĐỌC FILE NÀY TRƯỚC TIÊN

> **Cảnh báo #1:** `README.md` ở thư mục gốc nói dự án này là **CQRS / Event Sourcing / DDD**.
> **Điều đó SAI.** Đó là văn bản còn sót của template gốc.
> Nếu bạn (người hoặc AI) tin vào README, bạn sẽ dựng lại một event store không ai đọc,
> sáu project rỗng, và một tầng Domain không chứa nghiệp vụ nào.

Tài liệu này phục vụ việc **viết lại ezInvoice bằng công nghệ/ngôn ngữ khác** sao cho hệ thống mới
chạy **giống hệt** hệ thống cũ.

---

## 1. Hệ thống này thực sự là gì

Một **API trung gian (adapter/gateway) không lưu trạng thái**, đứng giữa:

```
PMS ezCloud  ──►  ezInvoice  ──►  9 nhà cung cấp hóa đơn điện tử Việt Nam
                      │
                      └──► ezInvoiceAdmin (hệ thống KHÁC, lấy cấu hình khách hàng)
                      └──► Elasticsearch (log nghiệp vụ)
```

ezInvoice **không lưu hóa đơn**, **không có DB nghiệp vụ cho hóa đơn**. Nó nhận request từ PMS,
tra cứu cấu hình của khách sạn, dịch sang định dạng riêng của từng nhà cung cấp, gọi đi, dịch
kết quả trả về.

**Toàn bộ giá trị của hệ thống nằm ở lớp dịch thuật đó** — không nằm ở kiến trúc.

---

## 2. Quy mô thật: 77.098 dòng C#, nhưng chỉ ~19.000 dòng là tri thức

| Loại | Dòng | % | Xử lý khi viết lại |
|---|---:|---:|---|
| Code tự sinh — WCF proxy `Reference.cs` (VNPT SOAP) | 44.865 | 58% | **Sinh lại từ WSDL**, không đọc, không port thủ công |
| Boilerplate template chết | ~13.000 | 17% | **Xóa hoàn toàn** |
| **Tri thức nghiệp vụ thật** | **~19.000** | **25%** | **Đây là thứ phải bảo toàn** |

Kiểm chứng nhanh:

```bash
find src -name "Reference.cs" -path "*Connected Services*" -exec cat {} + | wc -l   # 44865
find src -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -exec cat {} + | wc -l  # 77098
```

---

## 3. Dự án là bản fork của template EquinoxProject (Eduardo Pires, 2016)

Bằng chứng không thể chối cãi:

| Bằng chứng | Vị trí |
|---|---|
| `Copyright (c) 2016 Eduardo Pires` | `LICENSE:3` |
| Swagger vẫn để contact là Eduardo Pires | `src/ezInvoice.Services.Api/Configurations/SwaggerConfig.cs:21-22` |
| Migration EventStore mang timestamp **2016**1213130520 | `src/ezInvoice.Infra.Data/Migrations/EventStoreSQL/20161213130520_Initial.cs` |
| Script tạo database tên `[Equinox]`, "Script Date: 14/12/2016" | `sql/GenerateDataBase.sql:3-4` |
| `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md` của repo gốc | thư mục gốc |

**Hệ quả:** CQRS / MediatR / Event Sourcing / Domain Events **chỉ chạy cho entity mẫu `Customer`**
của template. Mọi event handler đều rỗng:

```csharp
// src/ezInvoice.Domain/Events/CustomerEventHandler.cs:12-31
// src/ezInvoice.Domain/Events/CompanyEventHandler.cs:10-15
return Task.CompletedTask;   // "// Send some greetings e-mail"
```

`PmsController` — nơi chứa 100% nghiệp vụ — **không hề dùng `IMediatorHandler`**.

---

## 4. Bản đồ CODE SỐNG vs CODE CHẾT

### ✅ PORT — đây là toàn bộ hệ thống thật

| Đường dẫn | Vai trò |
|---|---|
| `src/ezInvoice.Services.Api/Controllers/ApiForPms/PmsController.cs` | **33 endpoint, 1.806 dòng — hợp đồng với PMS** |
| `src/ezInvoice.Services.Api/Controllers/ApiController.cs` | `GetAuthorizationToken` / `CustomResponse` / `ExceptionResponse` |
| `src/ezInvoice.Services.Api/Controllers/InvoiceController.cs` | API tra cứu log |
| `src/ezInvoice.Application/Services/ThirdPartyServices/**` | Định tuyến + 9 AppService |
| `src/ezInvoice.Application/EventSourcedNormalizers/**` | **~8.000 dòng mapping — trái tim nghiệp vụ** |
| `src/ezInvoice.Application/ViewModels/PmsViewModels/**` | Canonical invoice model |
| `src/ezInvoice.Application/Helper/MoneyToText.cs` | Đọc tiền bằng chữ tiếng Việt |
| `src/ezInvoice.Application/Helper/XmlHelper.cs` | 2 thuật toán serialize XML |
| `src/ezInvoice.Application/Services/CompanyAppService.cs` | Client gọi ezInvoiceAdmin |
| `src/ezInvoice.Infra.ThirdParty/**` (trừ `Connected Services/`) | 9 adapter + tầng Helper |
| `src/ezInvoice.Infra.ThirdParty/Helper/**` | Log, dịch lỗi, TLS, cắt PDF |

> ⚠️ Tên thư mục `EventSourcedNormalizers` **gây hiểu nhầm** — nó không liên quan gì đến
> Event Sourcing. Đây là các bộ chuyển đổi (converter) PMS ↔ nhà cung cấp.
> Ở hệ mới nên đặt tên là `Providers/{ten}/Mapping/`.

### ❌ KHÔNG PORT — di sản template, đã chết

| Đường dẫn | Bằng chứng chết |
|---|---|
| `src/ezInvoice.Domain/**` | Aggregate `Customer` mẫu, validation "18 tuổi", handler rỗng |
| `src/ezInvoice.Domain.Core/**` | `Entity`, `ValueObject<T>`, `IAggregateRoot` của NetDevPack |
| `src/ezInvoice.Infra.CrossCutting.Bus/**` | Commit cuối **2022-08-11** |
| `src/ezInvoice.Infra.CrossCutting.Identity/**` | Không dùng thật; không có thư mục Migrations |
| `src/ezInvoice.Infra.Data/**` | DB gần như không tham gia luồng nghiệp vụ (xem §5) |
| `src/ezInvoice.UI.Web/**` | Commit cuối **2022-08-11** |
| `Front/**` (Vue) | Commit cuối **2022-12-05**; gọi API `/Company/all-company` **đã bị xóa** |
| `AccountController.cs`, `CustomerController.cs` | Boilerplate Identity + CRUD mẫu |
| `sql/GenerateDataBase.sql` | Tạo DB `[Equinox]`, thiếu 4/5 bảng nghiệp vụ |
| `src/.../Vnpt/Connected Services/**` | 44.865 dòng tự sinh — **sinh lại từ WSDL** |

Kiểm chứng:
```bash
git log -1 --format=%ci -- src/ezInvoice.Infra.CrossCutting.Bus   # 2022-08-11
git log -1 --format=%ci -- src/ezInvoice.UI.Web                   # 2022-08-11
git log -1 --format=%ci -- Front                                  # 2022-12-05
```

### ⚠️ Code chết NẰM TRONG code sống — dễ port nhầm nhất

| Vị trí | Vấn đề |
|---|---|
| `FastConvertRequest.cs:929-940` `GetFastInfoToken` | **0 call site.** Hàm thật là `GetFastInfoTokenFromJson` (16 call site). Hai hàm đọc cùng 6 cột theo 2 cách **không tương thích** |
| `SunConvertRequest.cs:42-112, 226-352` | Các hàm `*_origin` — bản cũ, không được gọi |
| `MisaConvertRequest.cs:1991-2082` | Tạo hóa đơn nháp, có số hóa đơn cứng. Comment: *"Tạm dừng thi công do khách không cần nữa"* |
| `MisaConvertRequest.cs:2146-2160` | `CustomRound` không được gọi |
| `CyberbillConvertRequest.cs:256-287` | `GetCodeVAT` không được gọi |
| `ConvertPmsToMinvoice.cs` | Cả file không dùng, **và có lỗi chính tả trong bảng map** |
| `VnptV5AppService.cs:174-181` | `EditInvoiceDraft` không nối vào switch |
| `ThirdPartyAuthorize.cs` | **Toàn bộ thân class bị comment** |
| `.../Viettel/LoggingHandler.cs` | Có file `.cs` nhưng `.csproj` có `<Compile Remove="LoggingHandler.cs" />` → **không được biên dịch** |

> 🔴 Bài học: **đọc `.csproj` trước khi tin vào sự tồn tại của một file `.cs`.**

---

## 5. Năm sự thật phản trực giác — bắt buộc phải biết

### 5.1. Database gần như không tham gia luồng nghiệp vụ

Từ commit `5177308` (EAS-26610, **18/01/2024**), toàn bộ cấu hình theo khách hàng đã **rời khỏi
database này** sang hệ thống **ezInvoiceAdmin** qua HTTP `openapi/oa02-site/detail`.

Các file `CompanyRepository.cs`, `ExtraDataMapRepository.cs`, `CompanyController.cs`,
`ExtraDataMapAppService.cs`… **đã bị xóa** (476 dòng, 12 file). Hai bảng `Companies` và
`ExtraDataMaps` còn khai `DbSet` nhưng **không repository nào đọc/ghi**.

➡️ Dependency THẬT là **HTTP API của ezInvoiceAdmin**, không phải SQL Server.

### 5.2. "Xác thực" thực chất là: biết SiteId là vào được

```
Authorization: Bearer <GUID SiteID>
```

`ApiController.GetAuthorizationToken()` đọc thô chuỗi token. Nếu parse được thành `Guid` thì
**dùng luôn làm SiteId**; nếu là JWT thì lấy claim `SiteID`.

Attribute `[ThirdPartyAuthorize]` trên `PmsController.cs:18` **đã bị comment**.
➡️ **33 endpoint nghiệp vụ hiện không có authorization filter nào.**

> Bật xác thực thật ở bản mới sẽ **chặn toàn bộ PMS đang chạy**. Phải có lộ trình chuyển đổi.

### 5.3. Có 2 nguồn cấu hình, và nguồn từ request được ưu tiên

`PmsController.cs:1755-1791`:
- Nếu request có `SiteConfigInfo` → **dùng thẳng credentials do PMS gửi lên**, không gọi ezInvoiceAdmin, không đụng DB.
- Nếu không → lấy SiteId từ header, gọi HTTP sang ezInvoiceAdmin.

Đây là **hai thế hệ tích hợp cùng tồn tại**. Chỉ implement một cái là phá vỡ một nửa số client.

### 5.4. Mọi endpoint luôn trả HTTP 200

Mã lỗi nằm trong body: `{ Status, Message, Data, Code }` với dải `200 / 322 / 4000 / 5000 / 6000 / 7000`.

Tệ hơn: `WebApiCustomMiddleware` **nuốt mọi exception** và không khôi phục `Response.Body`
→ khi lỗi, client nhận **HTTP 200 + body RỖNG**. PMS đã sống chung với hành vi này nhiều năm.

> 🔴 Trả `500 + JSON lỗi` "cho đúng chuẩn" ở bản mới = **thay đổi hợp đồng**. Phải hỏi đội PMS trước.

### 5.5. Không có một test nào

`ezInvoice.sln` có 12 project, **0 project test**. `appveyor.yml` chỉ `dotnet restore` + `dotnet build`.

➡️ **Lưới an toàn duy nhất phải tự xây**: xem §6.

---

## 6. Cách duy nhất chứng minh "chạy giống hệt": golden dataset từ Elasticsearch

`Helper/Models/LoggingModel.cs` ghi lên Elasticsearch (index `ezinvoice`) **đúng bộ 4 mốc** của
mỗi giao dịch:

```
Request  ──►  RequestToThirdParty  ──►  ResponseFromThirdParty  ──►  Response
(từ PMS)        (gửi sang NCC)             (NCC trả về)            (trả về PMS)
```

kèm `Action`, `ThirdParty`, `Company`, `Errors[]`, `RequestId`, `ClientIP`.

**Mỗi hóa đơn đã phát hành trên production là một test case hoàn chỉnh có sẵn.**

### Việc phải làm NGAY (trước khi tắt hệ cũ)

1. Xin quyền truy cập Elasticsearch production.
2. Trích 50–100 giao dịch cho **mỗi nhà cung cấp × mỗi action**.
3. Che dữ liệu nhạy cảm (mật khẩu, token, MST, tên khách).
4. Đóng gói thành fixture: `docs/rewrite/golden/{provider}/{action}/{case}/{4 file}`.

### Hai cảnh báo

- Trường PDF/Preview **đã bị cắt còn 100 ký tự** (`Helper/SubStringLogPDF.cs`) → muốn golden data
  đầy đủ cho `DownloadPDF` phải **bắt lại bằng proxy**, không lấy từ log được.
- Document ES dùng `correlationId` làm `_id` → mỗi lần ghi là **ghi đè**, không phải append.
  Chỉ còn trạng thái cuối cùng.

### Chiến lược kiểm chứng

| Bước | Nội dung |
|---|---|
| 1. Contract test | Chạy golden fixture qua hệ mới, so `RequestToThirdParty` **byte-by-byte**. Bắt được sai làm tròn, sai thứ tự thẻ XML, sai format ngày |
| 2. Shadow run | Hệ mới nhận bản sao traffic production, **không gửi thật** sang NCC, chỉ log payload để so sánh. 2–4 tuần |
| 3. Cutover từng NCC | Một nhà cung cấp một lần, giữ hệ cũ song song, rollback bằng đổi routing |
| 4. Giữ nguyên schema log | Để dashboard/quy trình support hiện tại không vỡ |

---

## 7. Thứ sẽ MẤT VĨNH VIỄN nếu không cứu trước khi tắt hệ cũ

| # | Thứ cần cứu | Vì sao không có trong repo |
|---|---|---|
| 1 | **Golden dataset** từ Elasticsearch | Log có retention policy |
| 2 | **Schema DB production thật** | Migration `20220709044241_Initial.cs` **đã bị sửa tay 2 lần sau khi apply** (commit `dd7e77a`, `aac72c0`). `InvoiceActions`/`InvoiceSystems` **không có migration nào tạo ra** |
| 3 | **Seed data `InvoiceActions` + `InvoiceSystems`** | Không script, không `HasData`, không `.sql`. **Không tồn tại ở bất kỳ đâu** |
| 4 | **`appsettings.*.json` thật trên 3 server IIS** | `Master.pubxml` / `MS_UAT.pubxml` / `MS_TEST.pubxml` đều có `<Content Update="**\*.json" CopyToPublishDirectory="Never" />` — **cấu hình production chưa bao giờ nằm trong repo** |
| 5 | **Cấu hình per-tenant từ ezInvoiceAdmin** (`Site` + `ExtraDataMap`) | Nằm ở hệ thống khác. Credentials của hàng trăm khách sạn |

Lệnh kiểm chứng #4:
```bash
grep -l "CopyToPublishDirectory=\"Never\"" src/ezInvoice.Services.Api/Properties/PublishProfiles/*.pubxml
```

---

## 8. Thứ chỉ nằm trong đầu người — phải phỏng vấn

| Câu hỏi | Hỏi ai |
|---|---|
| `SubAmount` vs `BeforeTaxAmount` vs `AfterTaxAmount` khác nhau ở đâu? `ServiceCharge` đã gồm thuế chưa? | Đội PMS/ezCloud |
| PMS xử lý khác nhau ra sao với mã `5000` vs `6000` vs `7000` — có retry không? | Đội PMS |
| Trong 33 endpoint, cái nào **thực sự còn được gọi**? | Đội PMS + thống kê từ ES |
| Lý do nghiệp vụ của **9 GUID hard-code** không gửi `ServiceSummary` sang VNPT? | PO + ticket EAS-26971 |
| Lọc `ExtraDataMap` theo `(FormNo, Serial)` — chỉ Viettel hay mọi NCC? | Business owner |
| **Nhà cung cấp nào thực sự cần bỏ qua kiểm tra SSL?** (có thể chỉ 1–2) | Đội tích hợp |
| Tài liệu API gốc (WSDL VNPT, spec Viettel/MISA/…) | Xin lại từ 9 NCC |
| MisaWebApp (EAS-38213) — vì sao dừng, có tiếp không? | PO |

---

## 9. Đọc tiếp theo thứ tự

| # | Tài liệu | Nội dung |
|---|---|---|
| 1 | [`01-ma-tran-partner-action.md`](01-ma-tran-partner-action.md) | 9 NCC × 33 action — cái nào hỗ trợ cái nào |
| 2 | [`02-bang-credential.md`](02-bang-credential.md) | 🔴 Ý nghĩa 6 cột credential theo từng NCC — **sai 1 cột = 1 NCC ngừng hoạt động** |
| 3 | [`03-so-tay-bay.md`](03-so-tay-bay.md) | 194 bẫy đã biết, cần quyết định giữ/sửa |

---

*Sinh từ khảo sát tự động toàn bộ mã nguồn, 2026-09-04. Dẫn chiếu `file:line` theo commit `f77fa2e`.*
