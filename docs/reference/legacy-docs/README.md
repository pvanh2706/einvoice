# Bộ tài liệu bàn giao tri thức — ezInvoice

Phục vụ việc **viết lại ezInvoice bằng công nghệ/ngôn ngữ khác** sao cho hệ thống mới chạy
**giống hệt** hệ thống cũ.

> 🔴 **Bắt đầu từ [`00-START-HERE.md`](00-START-HERE.md).**
> `README.md` ở thư mục gốc repo mô tả sai kiến trúc dự án.

---

## Tài liệu hiện có

| File | Nội dung | Dòng |
|---|---|---:|
| [`00-START-HERE.md`](00-START-HERE.md) | Bản đồ code sống/chết · 5 sự thật phản trực giác · chiến lược kiểm chứng · thứ sẽ mất vĩnh viễn | ~250 |
| [`01-ma-tran-partner-action.md`](01-ma-tran-partner-action.md) | Ma trận 9 nhà cung cấp × 26 action · enum `PARTNER`/`INVOICE_ACTION` · 33 endpoint PMS | ~230 |
| [`02-bang-credential.md`](02-bang-credential.md) | 🔴 Ý nghĩa 6 cột credential theo từng NCC — **sai 1 cột = 1 NCC ngừng hoạt động** | ~290 |
| [`03-so-tay-bay.md`](03-so-tay-bay.md) | 194 bẫy đã biết, phân theo 8 nhóm, kèm cột quyết định | ~230 |
| [`CLAUDE.md`](CLAUDE.md) | Hướng dẫn điều hướng cho AI | ~120 |

---

## Ba phát hiện quan trọng nhất

### 1. Chỉ 25% mã nguồn là tri thức thật

| | Dòng | % |
|---|---:|---:|
| Code tự sinh (WCF proxy VNPT) | 44.865 | 58% |
| Boilerplate template chết | ~13.000 | 17% |
| **Tri thức nghiệp vụ thật** | **~19.000** | **25%** |

### 2. Dự án là fork của template EquinoxProject (2016)

CQRS / Event Sourcing / DDD trong README **là trang trí** — chúng chỉ chạy cho entity mẫu
`Customer`, mọi event handler đều rỗng. Nghiệp vụ hóa đơn **không đi qua MediatR**.

### 3. Golden dataset đã có sẵn trên Elasticsearch

`LoggingModel` ghi đủ 4 mốc của mỗi giao dịch:
`Request` → `RequestToThirdParty` → `ResponseFromThirdParty` → `Response`.

➡️ **Mỗi hóa đơn đã phát hành trên production là một test case hoàn chỉnh.**
Đây là cách duy nhất chứng minh "chạy giống hệt", vì repo **không có test nào**.

---

## Việc cần làm tiếp

### Ưu tiên P0 — làm TRƯỚC KHI tắt hệ cũ (không thể khôi phục sau)

| # | Việc | Chặn bởi |
|---|---|---|
| 1 | Trích **golden dataset** từ Elasticsearch | Quyền truy cập ES production |
| 2 | Dump **schema DB production thật** | DBA |
| 3 | Export **seed data** `InvoiceActions` + `InvoiceSystems` | DBA |
| 4 | Lấy **`appsettings.*.json` thật** trên 3 server IIS | Đội DevOps |
| 5 | Dump **cấu hình per-tenant** từ ezInvoiceAdmin | Đội ezInvoiceAdmin |

Chi tiết: [`00-START-HERE.md §7`](00-START-HERE.md).

### Tài liệu còn thiếu

- Đặc tả công thức tiền / làm tròn / thuế (bao gồm 2 biến thể "đọc tiền bằng chữ")
- OpenAPI đầy đủ cho 33 endpoint PMS
- Từ điển Canonical Invoice Model *(cần phỏng vấn đội PMS)*
- Ma trận ánh xạ trường: 1 dòng/trường PMS × 9 cột NCC
- Sổ tay mã lỗi hợp nhất
- Danh mục "NCC yêu cầu miệng" — quyết định không có trong tài liệu API nào

---

## Nguồn và tính kiểm chứng được

Toàn bộ tài liệu **trích từ mã nguồn**, mọi khẳng định kèm dẫn chiếu `file:line`,
chốt ở commit **`f77fa2e`** (2026-09-04).

Các khẳng định rủi ro cao (ma trận, bảng credential) đã được **xác minh thủ công** trực tiếp
trên code, không chỉ dựa vào trích xuất tự động.

Những chỗ **không thể suy ra từ code** được đánh dấu rõ **`PHẢI HỎI NGƯỜI`** kèm gợi ý hỏi ai.
