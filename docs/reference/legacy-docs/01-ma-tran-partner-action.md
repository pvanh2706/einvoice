# Ma trận Nhà cung cấp × Hành động

> Ma trận này hiện **chỉ tồn tại ẩn trong các câu lệnh `switch`** rải rác ở 9 file `*AppService.cs`.
> Không có tài liệu nào khác mô tả nó. Đây là **bảng định tuyến trung tâm** của hệ thống.

Trích tự động từ mã nguồn, commit `f77fa2e`.

---

## 1. Hai tầng định tuyến

```
PmsController.<Action>()                    ← 33 endpoint HTTP
        │
        ├─ PrepareDataToThirdParty.*        ← dựng PrepareDataModel {Action, Company, Data, ExtraData}
        │
        ▼
ThirdPartyAppService.SendToThirdParty()     ← TẦNG 1: switch (Company.Partner)  [1..9]
        │
        ▼
{Vnpt|VnptV5|Minvoice|Sun|Viettel|Misa|Cyberbill|Fast|EasyInvoice}AppService
        │
        ▼
    switch (model.Action)                   ← TẦNG 2: switch (INVOICE_ACTION)  [-1..31]
```

**Tầng 1** — `src/ezInvoice.Application/Services/ThirdPartyServices/ThirdPartyAppService.cs:35`
Không khớp partner → `Message = "Đơn vị cung cấp chưa được kết nối!"`

**Tầng 2** — trong từng `*AppService.cs`
Không khớp action → `ResponseModel(322, "Đơn vi cung cấp không có chức năng này!")`

> ⚠️ Chuỗi lỗi `"Đơn vi cung cấp không có chức năng này!"` **thiếu dấu** ở chữ "vi".
> Giữ nguyên — PMS có thể đang so khớp chuỗi.

---

## 2. Enum `PARTNER`

`src/ezInvoice.Application/Services/ThirdPartyServices/EnumDefine.cs:5-16`

| Giá trị | Tên | AppService | Giao thức |
|---:|---|---|---|
| 1 | `VNPT` | `VnptAppService` | SOAP (TT32/V1) |
| 2 | `MINVOICE` | `MinvoiceAppService` | REST JSON (tên trường tiếng Việt viết tắt) |
| 3 | `SUN` | `SunAppService` | REST JSON (key rút gọn `c0..c9`) |
| 4 | `VIETTEL` | `ViettelAppService` | REST JSON |
| 5 | `VNPT_V5` | `VnptV5AppService` | SOAP (TT78/V5 + MTT + TLYT) |
| 6 | `MISA` | `MisaAppService` | REST JSON (2 hệ thống con) |
| 7 | `CYBER_BILL` | `CyberbillAppService` | REST JSON (tiếng Việt không dấu) |
| 8 | `FAST` | `FastAppService` | **REST (MTT) + SOAP (GTGT)** |
| 9 | `EASY_INVOICE` | `EasyInvoiceAppService` | REST + XML |

---

## 3. Enum `INVOICE_ACTION`

`EnumDefine.cs:18-148`

| Giá trị | Tên | Ý nghĩa |
|---:|---|---|
| -1 | `DELETE` | Xóa bỏ hóa đơn chờ ký |
| 0 | `LOGIN` | Đăng nhập |
| 1 | `FORM_SERIAL` | Lấy mẫu hóa đơn, ký hiệu |
| 2 | `CREATED` | Phát hành hóa đơn, chưa ký |
| 3 | `CHANGE` | Sửa hóa đơn |
| 4 | `PUBLISH` | Ký hóa đơn |
| 5 | `CREATED_AND_PUBLISH` | Phát hành hóa đơn đã ký |
| 6 | `VOID` | Hủy hóa đơn |
| 7 | `ADJUST_INCREASE` | Điều chỉnh tăng |
| 8 | `ADJUST_DECREASE` | Điều chỉnh giảm |
| 9 | `ADJUST_INFOR` | Điều chỉnh thông tin |
| 10 | `REPLACE` | Thay thế |
| 11 | `DOWNLOAD_PDF` | Tải file PDF |
| 12 | `CONVERT_FOR_STORE` | In chuyển đổi |
| **13** | — | ⚠️ **KHÔNG TỒN TẠI** |
| 14 | `GET_INFO_BY_TAXCODE` | Lấy thông tin công ty theo MST |
| 15 | `ADJUST` | Điều chỉnh giá trị hóa đơn TT78 |
| 16 | `SENDEMAIL` | Gửi email |
| 17 | `CREATED_AND_PUBLISH_MULTI` | Phát hành hóa đơn theo lô |
| 18 | `DRAFT` | Hóa đơn nháp |
| 19 | `PREVIEW` | Xem trước hóa đơn |
| 20 | `HEALTH_CHECK` | Kiểm tra kết nối |
| 21 | `SEARCH_BY_ID` | Lấy thông tin HĐ theo Id |
| 22 | `GET_INVOICE` | Lấy danh sách hóa đơn |
| 23 | `DOWNLOAD_PDF_INV_DRAFT` | Tải PDF hóa đơn nháp |
| 24 | `GET_SERIAL_CERT_INFO` | Lấy Serial Cert để gửi CQT khi tạo HĐ MTT |
| 25 | `SEND_INVOICE_CQT` | Gửi hóa đơn sang Cơ quan Thuế |
| 26 | `GET_STATUS_INVOICE` | Lấy trạng thái hóa đơn |
| 27 | `GET_XML_INVOICE_HASH` | Lấy mã XML hash |
| 28 | `SEND_INVOICE_CQT_USB_TOKEN` | Gửi CQT từ dữ liệu ký ở USB Token |
| 29 | `GET_XML_INVOICE` | Lấy XML hóa đơn (USB Token) |
| 30 | `CREATE_AND_PUBLISH_INVOICE_XML` | Phát hành hóa đơn đã ký ở USB Token |
| 31 | `EDIT_DRAFT` | Chỉnh sửa hóa đơn nháp |

> 🔴 **Số 13 bị nhảy.** Đừng "chuẩn hóa" lại thành liên tục — các giá trị số này xuất hiện trong
> request của PMS và trong toàn bộ log lịch sử trên Elasticsearch.

---

## 4. MA TRẬN CHÍNH — 9 NCC × 26 action

Ký hiệu: **✅** hỗ trợ · **➖** không hỗ trợ (trả `322`) · **🚫** có code nhưng **bị comment**

| Action | VNPT<br>(1) | MINV<br>(2) | SUN<br>(3) | VTEL<br>(4) | VNPT_V5<br>(5) | MISA<br>(6) | CYBER<br>(7) | FAST<br>(8) | EASY<br>(9) |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| `LOGIN` (0) | ➖ | ✅ | ➖ | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ |
| `HEALTH_CHECK` (20) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `FORM_SERIAL` (1) | ✅ | ✅ | ➖ | 🚫 | ✅ | ✅ | ➖ | ➖ | ➖ |
| `DRAFT` (18) | ✅ | ➖ | ✅ | ✅ | ✅ | 🚫 | 🚫 | ✅ | ✅ |
| `EDIT_DRAFT` (31) | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ | ➖ |
| `CREATED` (2) | ➖ | ✅ | ➖ | ➖ | ✅ | ➖¹ | ✅ | ➖ | ✅ |
| `CHANGE` (3) | ➖ | ✅ | ➖ | ➖ | ➖ | ➖ | ✅ | ➖ | ➖ |
| `PUBLISH` (4) | ✅ | ✅ | ➖ | ➖ | ✅ | ➖ | ✅ | ➖ | ✅ |
| `CREATED_AND_PUBLISH` (5) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `CREATED_AND_PUBLISH_MULTI` (17) | ✅ | ➖ | ➖ | ✅ | ✅ | ✅ | ➖ | ✅ | ➖ |
| `VOID` (6) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `DELETE` (-1) | ✅ | ✅ | ➖ | ➖ | ✅ | ➖ | ✅ | ➖ | ✅ |
| `ADJUST_INCREASE` (7) | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ | ➖ | ✅ | ✅ |
| `ADJUST_DECREASE` (8) | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ | ➖ | ✅ | ✅ |
| `ADJUST_INFOR` (9) | ✅ | ➖ | ➖ | ✅ | ✅ | ✅ | ➖ | ✅ | ✅ |
| `ADJUST` (15) | ➖ | ✅ | ➖ | ➖ | ✅ | ✅ | ✅ | ➖ | ➖ |
| `REPLACE` (10) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `PREVIEW` (19) | ✅ | ➖ | ✅ | ✅ | ✅ | ✅ | ➖ | ✅ | ✅ |
| `DOWNLOAD_PDF` (11) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `DOWNLOAD_PDF_INV_DRAFT` (23) | ✅ | ➖ | ✅ | ➖ | ✅ | ➖² | ➖ | ✅ | ➖ |
| `SEARCH_BY_ID` (21) | ✅ | ➖ | ➖ | ✅ | ➖ | 🚫 | ➖ | ➖ | ➖ |
| `GET_INVOICE` (22) | ➖ | **✅** | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ |
| `GET_INFO_BY_TAXCODE` (14) | ➖ | **✅** | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ |
| `SENDEMAIL` (16) | ➖ | **✅** | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ |
| `GET_STATUS_INVOICE` (26) | ✅ | ➖ | ➖ | ➖ | ✅ | ✅ | ➖ | ➖ | ➖ |
| `GET_SERIAL_CERT_INFO` (24) | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ | ➖ |
| `SEND_INVOICE_CQT` (25) | ➖ | ➖ | ➖ | ➖ | ✅ | ✅ | ➖ | ➖ | ➖ |
| `GET_XML_INVOICE_HASH` (27) | ➖ | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ |
| `SEND_INVOICE_CQT_USB_TOKEN` (28) | ➖ | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ |
| `GET_XML_INVOICE` (29) | ➖ | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ |
| `CREATE_AND_PUBLISH_INVOICE_XML` (30) | ➖ | ➖ | ➖ | ➖ | ➖ | **✅** | ➖ | ➖ | ➖ |
| `CONVERT_FOR_STORE` (12) | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ |

¹ MISA: `CREATED` có xử lý `TypeEinvoice` ở đầu hàm (`MisaAppService.cs:39-44`) nhưng
**không có nhánh trong switch chính** → rơi vào `default` → trả `322`.

² 🔴 **Bẫy tinh vi — MISA `DOWNLOAD_PDF_INV_DRAFT`:**
`MisaAppService.cs:29` xử lý `TypeEinvoice` cho **cả** `DOWNLOAD_PDF` **và** `DOWNLOAD_PDF_INV_DRAFT`:
```csharp
if (model.Action == (int)INVOICE_ACTION.DOWNLOAD_PDF || model.Action == (int)INVOICE_ACTION.DOWNLOAD_PDF_INV_DRAFT)
```
Nhưng switch chính (`:72`) **chỉ có** `DOWNLOAD_PDF`. → `DOWNLOAD_PDF_INV_DRAFT` rơi xuống `_ =>` → `322`.
> Code *chuẩn bị* cho action này nhưng *không xử lý* nó. Đọc lướt sẽ tưởng là có hỗ trợ.
> **Cần hỏi:** đây là bug (quên nối vào switch) hay cố ý? Hành vi hiện tại trên production là trả `322`.

### Nguồn từng cột

| NCC | File | Dòng switch |
|---|---|---|
| VNPT | `Services/ThirdPartyServices/VnptAppService.cs` | 51–98 |
| MINVOICE | `Services/ThirdPartyServices/MinvoiceAppService.cs` | 125–157 |
| SUN | `Services/ThirdPartyServices/SunAppService.cs` | 34–65 |
| VIETTEL | `Services/ThirdPartyServices/ViettelAppService.cs` | 26–63 |
| VNPT_V5 | `Services/ThirdPartyServices/VnptV5AppService.cs` | 61–133 |
| MISA | `Services/ThirdPartyServices/MisaAppService.cs` | 61–84 (`switch` biểu thức) |
| CYBER_BILL | `Services/ThirdPartyServices/CyberbillAppService.cs` | 26–58 |
| FAST | `Services/ThirdPartyServices/FastAppService.cs` | 29–61 |
| EASY_INVOICE | `Services/ThirdPartyServices/EasyInvoiceAppService.cs` | 28–65 |

---

## 5. Đặc quyền riêng — action chỉ 1 nhà cung cấp có

Đây là những chỗ **kiến trúc bị rò rỉ**: hợp đồng chung phải chứa cả những thao tác chỉ 1 NCC dùng.

| Action | Chỉ có ở | Ghi chú |
|---|---|---|
| `EDIT_DRAFT` (31) | **VNPT_V5** | Chỉnh sửa hóa đơn nháp |
| `GET_SERIAL_CERT_INFO` (24) | **VNPT_V5** | Lấy Serial Cert cho HĐ MTT |
| `GET_INVOICE` (22) | **MINVOICE** | Lấy danh sách hóa đơn |
| `GET_INFO_BY_TAXCODE` (14) | **MINVOICE** | Tra cứu MST |
| `SENDEMAIL` (16) | **MINVOICE** | Gửi email |
| `GET_XML_INVOICE_HASH` (27) | **MISA** | ┐ |
| `SEND_INVOICE_CQT_USB_TOKEN` (28) | **MISA** | ├ Bộ 4 action **luồng USB Token** |
| `GET_XML_INVOICE` (29) | **MISA** | ├ ký ở máy khách, 4 bước |
| `CREATE_AND_PUBLISH_INVOICE_XML` (30) | **MISA** | ┘ |

`CONVERT_FOR_STORE` (12) — **không NCC nào implement.** Enum tồn tại, endpoint không có.
→ Đề xuất: bỏ khi viết lại, sau khi xác nhận với đội PMS.

---

## 6. Bảng đối chiếu 33 endpoint PMS → INVOICE_ACTION

Route gốc: `api/ezInvoice` — `PmsController.cs:17`

| # | Endpoint | Method | Action |
|---:|---|---|---|
| 1 | `MinvoiceLogin` | POST | `LOGIN` (0) — riêng Minvoice |
| 2 | `HealthCheck` | POST | `HEALTH_CHECK` (20) |
| 3 | `Login` | POST | `LOGIN` (0) |
| 4 | `Create` | POST | `CREATED` (2) |
| 5 | `Draft` | POST | `DRAFT` (18) |
| 6 | `Preview` | POST | `PREVIEW` (19) |
| 7 | `Change` | POST | `CHANGE` (3) |
| 8 | `Delete` | POST | `DELETE` (-1) |
| 9 | `Publish` | POST | `PUBLISH` (4) |
| 10 | `MinvoiceGetTaxCodeInfo` | POST | `GET_INFO_BY_TAXCODE` (14) |
| 11 | `ReplaceInvoice` | POST | `REPLACE` (10) |
| 12 | `AdjustIncreaseInvoice` | POST | `ADJUST_INCREASE` (7) |
| 13 | `AdjustInvoice` | POST | `ADJUST` (15) |
| 14 | `AdjustDecreaseInvoice` | POST | `ADJUST_DECREASE` (8) |
| 15 | `AdjustInforInvoice` | POST | `ADJUST_INFOR` (9) |
| 16 | `VoidInvoice` | POST | `VOID` (6) |
| 17 | `DownloadPDF` | POST | `DOWNLOAD_PDF` (11) |
| 18 | `CreateAndPublishInvoice` | POST | `CREATED_AND_PUBLISH` (5) |
| 19 | `CreateAndPublishMultiInvoice` | POST | `CREATED_AND_PUBLISH_MULTI` (17) |
| 20 | `FormSerials` | **GET** | `FORM_SERIAL` (1) |
| 21 | `FormSerialByType` | POST | `FORM_SERIAL` (1) |
| 22 | `SendEmail` | POST | `SENDEMAIL` (16) |
| 23 | `SearchById` | POST | `SEARCH_BY_ID` (21) |
| 24 | `GetInvoice` | POST | `GET_INVOICE` (22) |
| 25 | `DownloadPDFInvoiceDraft` | POST | `DOWNLOAD_PDF_INV_DRAFT` (23) |
| 26 | `GetSerialCertInfo` | POST | `GET_SERIAL_CERT_INFO` (24) |
| 27 | `SendInvoiceCQT` | POST | `SEND_INVOICE_CQT` (25) |
| 28 | `GetStatusInvoice` | POST | `GET_STATUS_INVOICE` (26) |
| 29 | `GetXMLHashInvoice` | POST | `GET_XML_INVOICE_HASH` (27) |
| 30 | `SendInvoiceCQTUSBToken` | POST | `SEND_INVOICE_CQT_USB_TOKEN` (28) |
| 31 | `GetXMLInvoice` | POST | `GET_XML_INVOICE` (29) |
| 32 | `CreateAndPublishInvoiceXML` | POST | `CREATE_AND_PUBLISH_INVOICE_XML` (30) |
| 33 | `EditDraft` | POST | `EDIT_DRAFT` (31) |

> ⚠️ `FormSerials` là endpoint **GET duy nhất** trong 33 cái. Nó không nhận body
> → không có `SiteConfigInfo` → **bắt buộc** đi đường header token.

> 🔴 Có **hai action C# cùng tên `GetXMLInvoice`** ở `PmsController.cs:1548` và `:1603` với 2 route
> khác nhau. Dễ map nhầm khi viết lại.

---

## 7. Thiết kế đề xuất cho hệ thống mới

Bản cũ **không có** hợp đồng chung — mỗi `*AppService` tự định nghĩa switch riêng, và
`IThirdPartyAppService` chỉ có đúng một method:

```csharp
// src/ezInvoice.Application/Interfaces/ThirdPartyInterfaces/IThirdPartyAppService.cs
Task<ResponseModel> SendToThirdParty(PrepareDataModel company);
```

→ Không có kiểu dữ liệu nào bảo vệ; mọi thứ đi qua `object Data` + ép kiểu runtime.

**Đề xuất bản mới:**

1. Định nghĩa `IEInvoiceProvider` với các thao tác **có kiểu rõ ràng**, mỗi thao tác một method.
2. Mỗi provider khai báo **tường minh** tập action hỗ trợ (`SupportedActions`) — biến ma trận
   ở §4 từ tri thức ẩn thành **dữ liệu kiểm chứng được bằng test**.
3. Thay các trường rò rỉ chi tiết nhà cung cấp (`sid`, `RefID`, `TransactionId`, `searchCode`)
   bằng một `providerState` mờ (opaque).
4. Viết test: với mỗi cặp (provider, action) **không** hỗ trợ, phải trả đúng `322` + đúng chuỗi
   `"Đơn vi cung cấp không có chức năng này!"`.

---

*Trích tự động từ mã nguồn, commit `f77fa2e`, 2026-09-04.*
