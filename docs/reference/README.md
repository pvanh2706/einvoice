# Nguồn tham chiếu cố định

- Code cũ chỉ đọc: `D:\Code\25.ezInvoice`.
- Commit baseline mới: `66a9c742d0f6fb692d339076e6cd82780e3476be`.
- Bộ tài liệu cũ trong `legacy-docs/` chốt tại `f77fa2e`; bản sao được lưu từ working tree và có hash trong `source-manifest.json`.
- Snapshot giữ nguyên tên lịch sử để kiểm chứng. Tên code mới là eInvoice; không thay chuỗi trong bản reference.
- Inventory mô tả **hành vi có trong code cũ**, không phải những adapter đã hoàn thành ở bản mới.

## Chênh lệch quan trọng từ f77fa2e đến baseline

4 commit, 8 file, 99 dòng thêm và 67 dòng xóa:

1. `2106689`: thay đổi timeout MISA và nhận diện TimeoutException bọc trong exception khác.
2. `173d49d`: thêm RequestPartnerTime, ResponsePartnerTime, UrlPartner vào log view model.
3. `6fe2c00`: thêm dữ liệu phản hồi provider trong các response lỗi MISA, Minvoice và VNPT.
4. `66a9c74`: Viettel buyerBankAccount/buyerBankName lấy từ BankAccount/BankName, thay cho PaymentBankAccount/PaymentBankName.

Tài liệu cũ có nhãn 26/33 action không nhất quán. Enum thực tế có **32 action**, giá trị -1..31 và bỏ số 13; controller có **33 endpoint**. VNPT_V5 có EDIT_DRAFT nhưng switch gọi nhánh create, không gọi helper EditInvoiceDraft đã chết.

## Capture lại có chủ đích

```powershell
python scripts/capture-legacy-inventory.py
```

Script đọc code bằng `git show` tại commit cố định, chỉ đọc các file allowlist; không đọc appsettings hoặc SQL chứa credential. Không tự nâng baseline. Nếu code cũ thay đổi, phải rà soát và ghi quyết định trước khi cập nhật.

Helper snapshot chỉ chuẩn hóa xuống dòng LF và bỏ whitespace cuối dòng; thuật toán giữ nguyên. Hash trong manifest là hash nội dung nguồn gốc trước chuẩn hóa.

Repo cũ có thay đổi local ở .gitignore/global.json/appsettings và docs chưa tracked lúc capture; chúng không được coi là code baseline mới.
