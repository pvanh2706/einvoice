# 🔴 Bảng ý nghĩa 6 cột credential theo từng nhà cung cấp

> **Đây là tri thức ngầm nguy hiểm nhất của toàn dự án.**
> Cùng 6 cột dữ liệu, nhưng **mỗi nhà cung cấp hiểu chúng theo một cách hoàn toàn khác nhau**.
> Không có quy ước chung. Không có tài liệu nào khác mô tả.
>
> **Migrate sai một cột = toàn bộ khách hàng của nhà cung cấp đó ngừng phát hành được hóa đơn.**

Trích và **xác minh trực tiếp** từ mã nguồn, commit `f77fa2e`.

---

## 1. Cấu trúc chung

`src/ezInvoice.Application/ViewModels/CompanyViewModel.cs:5-22`

```csharp
public class CompanyViewModel
{
    public Guid?    Id            { get; set; }
    public string   Name          { get; set; }
    public string   Address       { get; set; }
    public string   Email         { get; set; }
    public string   Phone         { get; set; }
    public string   TaxNumber     { get; set; }
    public int?     Partner       { get; set; }   // 1..9, xem 01-ma-tran-partner-action.md
    public string   PartnerUrl    { get; set; }   // ┐
    public string   PartnerUrl2   { get; set; }   // │
    public string   Username      { get; set; }   // ├ 6 CỘT NÀY: ý nghĩa khác nhau theo Partner
    public string   Password      { get; set; }   // │
    public string   Username2     { get; set; }   // │
    public string   Password2     { get; set; }   // ┘
    public string   MinvoiceToken { get; set; }   // state runtime, KHÔNG có cột trong DB
    public DateTime? MinvoiceExpired { get; set; } // state runtime, KHÔNG có cột trong DB
}
```

### Nguồn dữ liệu thật

⚠️ **Dữ liệu này KHÔNG nằm trong database của repo này.**

Từ commit `5177308` (EAS-26610, 18/01/2024) nó đến từ **một trong hai nguồn**:

| Nguồn | Điều kiện | Vị trí code |
|---|---|---|
| **A.** `SiteConfigInfo` trong body request | PMS tự gửi credentials lên | `PmsController.cs:1756-1774` |
| **B.** HTTP `openapi/oa02-site/detail` sang **ezInvoiceAdmin** | Không có `SiteConfigInfo` → dùng SiteId từ header | `CompanyAppService.cs:25-92` |

**Nguồn A được ưu tiên** — nếu có `SiteConfigInfo` thì không gọi ezInvoiceAdmin, không đụng DB.

> 🔴 Đây là **hai thế hệ tích hợp cùng tồn tại**. Bản mới chỉ implement một nguồn sẽ phá vỡ
> một nửa số client đang chạy.

> 🔴 Khi đi đường A, `PmsController.cs:1775-1776` **ép cứng** `MinvoiceToken = ""` và
> `MinvoiceExpired = DateTime.UtcNow` → mọi request Minvoice đi đường A đều **login lại từ đầu**.

---

## 2. BẢNG CHÍNH

| Partner | `PartnerUrl` | `PartnerUrl2` | `Username` | `Password` | `Username2` | `Password2` |
|---|---|---|---|---|---|---|
| **1 VNPT** | URL webservice | *(gán = `PartnerUrl`)* | `Account`<br>tài khoản **portal** | `ACpass` | `username`<br>tài khoản **webservice** | `pass` |
| **2 MINVOICE** | Base URL | — | `username` | `password` | **`ma_dvcs`**<br>(mã đơn vị cơ sở) | — |
| **3 SUN** | URL | dùng | `Username` | `Password` | `Username2` | `Password2` |
| **4 VIETTEL** | URL | — | `Username` | `Password` | — | — |
| **5 VNPT_V5** | URL webservice | — | `Account`<br>portal | `ACpass` | `username`<br>webservice | `pass` |
| **6 MISA** | URL **meInvoice v3**<br>(hóa đơn/vé/MTT) | 🔴 URL **hệ thống biên lai**<br>(hệ thống KHÁC) | user v3 | pass v3 | `userName`<br>**biên lai** | `passWord`<br>**biên lai** |
| **7 CYBER_BILL** | URL | — | `Username` | `Password` | — | — |
| **8 FAST** | 🔴 URL cho **MTT** | 🔴 URL cho **GTGT** | 🔴 **CHUỖI JSON** (MTT) | password MTT | 🔴 **CHUỖI JSON** (GTGT) | password GTGT |
| **9 EASY_INVOICE** | URL | — | `Username` | `Password` | — | — |

---

## 3. Chi tiết từng nhà cung cấp

### 3.1. VNPT (1) và VNPT_V5 (5) — hai cặp tài khoản khác vai trò

`VnptConvertRequest.cs:59-63` (và mọi hàm dựng request khác):

```csharp
Account  = model.Company.Username,    // tài khoản PORTAL
ACpass   = model.Company.Password,
username = model.Company.Username2,   // tài khoản WEBSERVICE
pass     = model.Company.Password2,
```

> ⚠️ Không phải "tài khoản chính / tài khoản phụ". Đây là **hai tài khoản khác nhau trên hai
> hệ thống khác nhau của VNPT**, và **cả hai đều bắt buộc** trong cùng một lời gọi SOAP.

> ⚠️ Một số API VNPT dùng tên tham số `userName` / `userPass` thay vì `username` / `pass`
> cho **cùng cặp giá trị**. Phải kiểm tra từng operation trong WSDL.

`ThirdPartyAppService.cs:42-43` — với VNPT (1), `Url_TLYT` được gán **bằng chính `PartnerUrl`**:
```csharp
_vnptAppService.Url      = data.Company.PartnerUrl;
_vnptAppService.Url_TLYT = data.Company.PartnerUrl;   // KHÔNG phải PartnerUrl2
```

---

### 3.2. MINVOICE (2) — `Username2` là mã đơn vị, không phải tài khoản

`MinvoiceAppService.cs:167-171`:

```csharp
new MinvoiceLoginRequest
{
    ma_dvcs  = request.Company.Username2,   // ← MÃ ĐƠN VỊ CƠ SỞ, không phải username
    username = request.Company.Username,
    password = request.Company.Password,
}
```

> 🔴 Người vận hành nhìn form nhập liệu thấy ô tên "Username2" sẽ điền tài khoản vào đó.
> Bản mới **phải đổi tên ô này thành `Mã đơn vị cơ sở (ma_dvcs)`**.

`Password2` **không được dùng** cho Minvoice.

---

### 3.3. MISA (6) — `PartnerUrl2` trỏ tới một hệ thống HOÀN TOÀN KHÁC

MISA thực chất là **2 hệ thống con nằm sau 2 URL khác nhau**, mỗi hệ thống có cơ chế lấy token
riêng, endpoint riêng, envelope response riêng:

| Cột | Hệ thống | Dùng cho |
|---|---|---|
| `PartnerUrl` | **meInvoice v3** | Hóa đơn, Vé điện tử, Hóa đơn máy tính tiền (MTT) |
| `PartnerUrl2` | **Hệ thống biên lai điện tử** | Biên lai (receipt) |

`MisaRepository.cs` — mọi endpoint biên lai đều ghép từ `PartnerUrl2`:
```
:414  $"{connection.PartnerUrl2}{EndpointApi.CREATE_AND_PUBLISH_RECEIPT}"
:453  $"{connection.PartnerUrl2}{EndpointApi.DOWNLOAD_INVOICE}"
:487  $"{connection.PartnerUrl2}{EndpointApi.CANCEL_INVOICE}"
:517  $"{connection.PartnerUrl2}{EndpointApi.INVOICE_SERIAL}"
```

`MisaConvertRequest.cs:1878-1879` — tài khoản biên lai:
```csharp
userName = connection.Username2,
passWord = connection.Password2
```

#### AppID — hằng số hard-code, KHÔNG lấy từ cấu hình

`MisaRepository.cs:13`:
```csharp
private string APPID_MISA = "9B226ED5-8653-11ED-8091-005056A6F699";
// "Bên Misa thông báo dùng chung key này cho tất cả các môi trường"
```

> ⚠️ Đây là **hằng số toàn cục**, không phải per-tenant. Bản mới nên đưa vào cấu hình
> (không hard-code) nhưng **giữ nguyên giá trị**.

#### Cache token

`MisaRepository.cs:14` — `static List<TokenMisaByCompany> tokenMisaByCompanies`, TTL **7 ngày**.
Xem bẫy F-series trong [`03-so-tay-bay.md`](03-so-tay-bay.md) (race condition, lỗi múi giờ,
mật khẩu plaintext trong RAM).

---

### 3.4. 🔴 FAST (8) — trường hợp nguy hiểm nhất

FAST **nhồi cả một chuỗi JSON vào cột `Username`**, và chọn cặp cột nào phụ thuộc **runtime**
vào loại hóa đơn.

#### Hàm THỰC SỰ được dùng (16 call site)

`FastConvertRequest.cs:943-959`:

```csharp
public static FastInfoToken GetFastInfoTokenFromJson(CompanyViewModel company, bool isInvoiceMTT)
{
    string partnerUrl = isInvoiceMTT ? company.PartnerUrl  : company.PartnerUrl2;
    string password   = isInvoiceMTT ? company.Password    : company.Password2;
    string jsonToken  = isInvoiceMTT ? company.Username    : company.Username2;   // ← JSON!

    FastJsonTokenDB d = JsonConvert.DeserializeObject<FastJsonTokenDB>(jsonToken);
    return new FastInfoToken
    {
        clientCode = d.clientC,
        proxyCode  = d.proxyC,
        user       = d.user,
        UnitCode   = d.unitC,
        PartnerUrl = partnerUrl,
        password   = password,
    };
}
```

Nội dung JSON trong cột `Username` / `Username2`:
```json
{"clientC":"FAST_CLIENT_001","proxyC":"PROXY_001","user":"fastuser@company.com","unitC":"UNIT_001"}
```

#### Nghĩa là

| | Hóa đơn **MTT** (`isInvoiceMTT = true`) | Hóa đơn **GTGT** (`false`) |
|---|---|---|
| URL | `PartnerUrl` | `PartnerUrl2` |
| Password | `Password` | `Password2` |
| JSON (clientC/proxyC/user/unitC) | `Username` | `Username2` |

➡️ **Một công ty FAST lưu 2 bộ kết nối hoàn chỉnh trong cùng 1 dòng dữ liệu**, phân biệt bởi
loại hóa đơn tại thời điểm chạy.

> 🔴 Nếu bản mới tách bảng cấu hình theo NCC mà **không giữ khái niệm "bộ MTT / bộ GTGT"**,
> khách hàng FAST sẽ gửi hóa đơn MTT bằng credential của hóa đơn GTGT và ngược lại.

#### ⚠️ Bẫy: có một hàm CHẾT đọc cùng 6 cột theo cách KHÁC HẲN

`FastConvertRequest.cs:929-940` — **`GetFastInfoToken` có 0 call site**:

```csharp
public static FastInfoToken GetFastInfoToken(CompanyViewModel company)
{
    return new FastInfoToken {
        clientCode = company.Username2,     // ← khác hoàn toàn
        proxyCode  = company.Password2,
        user       = company.Username,
        PartnerUrl = company.PartnerUrl,
        password   = company.Password,
        UnitCode   = company.PartnerUrl2,   // ← dùng cột URL để chứa MÃ ĐƠN VỊ
    };
}
```

Kiểm chứng:
```bash
grep -rn "GetFastInfoToken\b" src --include="*.cs" | grep -v "public static"   # 0 kết quả
grep -rn "GetFastInfoTokenFromJson" src --include="*.cs" | grep -v "public static" | wc -l   # 16
```

> 🔴 **Ai đọc nhầm hàm này sẽ migrate sai toàn bộ dữ liệu FAST.**
> Xóa hàm chết này trước khi bàn giao, hoặc đánh dấu `[Obsolete]` thật to.

#### ⚠️ Giới hạn độ dài cột

Cả 6 cột đều khai `nvarchar(100)` — `Mappings/CompanyMap.cs:37-58` và
`Migrations/ezInvoiceContextModelSnapshot.cs:47-77`:

```csharp
builder.Property(c => c.Username).HasColumnType("nvarchar(100)").HasMaxLength(100);
```

Chuỗi JSON ví dụ ở trên **dài ~100 ký tự** và JSON thật (email dài, mã đơn vị dài) **sẽ vượt**.

> ⚠️ **Lưu ý về phạm vi:** giới hạn 100 ký tự này thuộc bảng `Companies` **cục bộ đã chết**
> (xem [`00-START-HERE.md §5.1`](00-START-HERE.md)). Dữ liệu FAST đang chạy nằm ở bảng `Site`
> của **ezInvoiceAdmin**, mà **schema của nó không có trong repo này**.
>
> ➡️ **BẮT BUỘC hỏi đội ezInvoiceAdmin** kiểu và độ dài thật của các cột đó, rồi chạy truy vấn
> kiểm tra cắt cụt ở §5.1. Nếu ezInvoiceAdmin cũng dùng `nvarchar(100)` thì gần như chắc chắn
> đã có bản ghi FAST bị hỏng dữ liệu trên production.

---

### 3.5. SUN (3) — dùng cả 6 cột, ý nghĩa thẳng

`SunConvertRequest.cs:584-597` truyền nguyên cả 6 cột vào `SunConnectionInfo`.

Ghi chú lịch sử: `Username2` **từng** được dùng làm tiền tố sinh `sid`:
```csharp
// sid = company.Username2 + invoice.FormNo + invoice.Serial + <unix timestamp>
```
Nay đã comment và thay bằng (`SunConvertRequest.cs:68-69`):
```csharp
// Chuyển qua dùng ezInvoiceId làm sid theo yêu cầu Sun (bản guid rút gọn 32 ký tự)
// Mục đích để truyền lại key khi phát hanh hóa đơn nếu timeout để tránh bị double hóa đơn
sid = invoice.ezInvoiceId.Replace("-", "").Substring(0, 32);
```

> 🔴 **`sid` là idempotency key** thỏa thuận riêng với Sun để chống phát hành trùng khi timeout.
> Bản mới **bắt buộc** giữ nguyên thuật toán sinh `sid` này, nếu không sẽ **phát hành hóa đơn đôi**.

---

### 3.6. VIETTEL (4), CYBERBILL (7), EASY_INVOICE (9) — đơn giản

Chỉ dùng 3 cột: `PartnerUrl`, `Username`, `Password`.

| NCC | Vị trí |
|---|---|
| VIETTEL | `ViettelConvertRequest.cs:368-370` |
| CYBERBILL | `CyberbillConvertRequest.cs:293-295` |
| EASY_INVOICE | `EasyInvoiceConvertRequest.cs:887-888` |

`PartnerUrl2`, `Username2`, `Password2` **không được dùng** cho 3 nhà cung cấp này.

---

## 4. Tóm tắt: cột nào dùng, cột nào bỏ trống

| Partner | `PartnerUrl` | `PartnerUrl2` | `Username` | `Password` | `Username2` | `Password2` |
|---|:--:|:--:|:--:|:--:|:--:|:--:|
| 1 VNPT | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ |
| 2 MINVOICE | ✅ | ➖ | ✅ | ✅ | ✅ | ➖ |
| 3 SUN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| 4 VIETTEL | ✅ | ➖ | ✅ | ✅ | ➖ | ➖ |
| 5 VNPT_V5 | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ |
| 6 MISA | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| 7 CYBER_BILL | ✅ | ➖ | ✅ | ✅ | ➖ | ➖ |
| 8 FAST | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| 9 EASY_INVOICE | ✅ | ➖ | ✅ | ✅ | ➖ | ➖ |

---

## 5. Việc phải làm khi migrate

### 5.1. Trước khi bắt đầu

1. **Dump toàn bộ dữ liệu cấu hình** từ ezInvoiceAdmin (bảng `Site`) — xem [`00-START-HERE.md §7`](00-START-HERE.md).
2. **Kiểm tra độ dài thật** của `Username` / `Username2` cho các bản ghi FAST:
   ```sql
   SELECT Id, LEN(Username) AS L1, LEN(Username2) AS L2
   FROM Site WHERE Partner = 8 ORDER BY L1 DESC;
   ```
   Nếu có giá trị = 100 chính xác → **dữ liệu đã bị cắt cụt**, phải lấy lại từ khách hàng.
3. **Validate JSON** trong cột FAST — bản ghi nào parse lỗi là bản ghi hỏng.

### 5.2. Thiết kế bản mới

Bỏ mô hình 6 cột vô nghĩa. Thay bằng cấu hình **có kiểu theo từng nhà cung cấp**:

```jsonc
// Ví dụ minh họa — mỗi provider có schema riêng, validate được
{
  "provider": "FAST",
  "config": {
    "mtt":  { "url": "...", "password": "...", "clientCode": "...", "proxyCode": "...", "user": "...", "unitCode": "..." },
    "gtgt": { "url": "...", "password": "...", "clientCode": "...", "proxyCode": "...", "user": "...", "unitCode": "..." }
  }
}
```

```jsonc
{
  "provider": "VNPT_V5",
  "config": {
    "url": "...",
    "portalAccount":     { "user": "...", "pass": "..." },   // Account / ACpass
    "webserviceAccount": { "user": "...", "pass": "..." }    // username / pass
  }
}
```

```jsonc
{
  "provider": "MISA",
  "config": {
    "meInvoiceV3": { "url": "...", "user": "...", "pass": "..." },
    "receipt":     { "url": "...", "user": "...", "pass": "..." }   // hệ thống KHÁC
  }
}
```

### 5.3. Bảo mật

> 🔴 Mật khẩu hiện lưu **plaintext** trong `nvarchar(100)`, và có credential thật commit vào git
> (`sql/AddCompany.sql:18-30`, mọi `appsettings*.json`).
> **Xoay vòng (rotate) toàn bộ trước khi bàn giao mã nguồn cho đội mới.**

---

## 6. Kiểm chứng bảng này

Lệnh tự chạy lại toàn bộ dẫn chiếu trong tài liệu:

```bash
cd D:/Code/25.ezInvoice
grep -rn "Company\.Username2\|Company\.Password2\|Company\.PartnerUrl2\|connection\.Username2\|connection\.PartnerUrl2" \
     src --include="*.cs" | grep -v "/obj/" | grep -v "public string"
```

---

*Trích và xác minh trực tiếp từ mã nguồn, commit `f77fa2e`, 2026-09-04.*
