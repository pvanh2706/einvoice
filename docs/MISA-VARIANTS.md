# Variant MISA tại baseline

Nguồn đọc: commit `66a9c742d0f6fb692d339076e6cd82780e3476be`, MisaAppService,
MisaConvertRequest, MisaRepository, EndpointApi, EnumDefine. Bảng mô tả nhánh code sống,
không chứng minh nhánh đó hoạt động đúng ở production. eInvoice mới chỉ port login và FormSerial.

InvoiceType: 0 hóa đơn, 1 vé, 2 biên lai. TypeEinvoice (mới electronicInvoiceType):
0 HSM thường, 1 MTT, 2 USB thường, 3 MTT USB, 4 MTT HSM.

Dispatcher đặt `isInvoiceMTT = IsSerialMTT || TypeEinvoice == 1`.
Action batch không gán TypeEinvoice từ request trong dispatcher; nó mặc định 0 rồi hàm batch
đọc TypeEinvoice và InvoiceType từ phần tử đầu. Vì vậy batch loại 1/4 phụ thuộc IsSerialMTT,
không được giả định chạy cùng nhánh như single. Batch rỗng/khác loại cần validation khi port.

## Tạo và phát hành

| InvoiceType / điều kiện, xét theo thứ tự | Single mapper | Batch mapper | Repository / đường dẫn |
|---|---|---|---|
| 0, isInvoiceMTT && TypeEinvoice == 4 | ConvertCreateAndPublishInvoiceMTTHSMRequest | ConvertCreateAndPublishInvoiceMTTHSMMultiRequest | CreateAndPublishInvoiceMTTHSM; code/itg/invoicepublishing/publishhsm |
| 0, isInvoiceMTT hoặc TypeEinvoice == 3 | ConvertCreateAndPublishInvoiceMTTRequest | ConvertCreateAndPublishInvoiceMTTMultiRequest | CreateAndPublishInvoiceMTT; code/itg/invoice-calculating/invoiceandpublish |
| 0, !isInvoiceMTT && TypeEinvoice == 0 | ConvertCreateAndPublishInvoiceHSMRequest | ConvertCreateAndPublishInvoiceHSMMultiRequest | CreateAndPublishInvoiceHSM; code/itg/invoicepublishing/publishhsm |
| 0, TypeEinvoice == 2, !isInvoiceMTT | Không có nhánh trực tiếp | Không có nhánh trực tiếp | Response mapper nhận misaResponse null; dùng chuỗi USB XML riêng khi triển khai |
| 0, TypeEinvoice == 4, !isInvoiceMTT | Không có nhánh trực tiếp | Không có nhánh trực tiếp | Không tự sửa điều kiện baseline khi chưa hỏi nghiệp vụ |
| 1, mọi TypeEinvoice | ConvertCreateAndPublishTicketRequest | ConvertCreateAndPublishTicketRequest | CreateAndPublishTicket; itg/invoicepublishing/publishhsm |
| 2, mọi TypeEinvoice | GetInvoiceSerial rồi ConvertCreateAndPublishReceiptRequest | Cùng mapper, chọn serial từ phần tử đầu | CreateAndPublishReceipt; invoice/publishinvoicesoftdream, PartnerUrl2 |

HSM thường/MTT HSM dùng OriginalInvoiceData; MTT dùng OrgInvoiceData; vé dùng
OriginalInvoiceData. Giữ từng mapping single/batch riêng và AmountDecimalDigits khác nhau
cho đến khi có fixture. Transport phát hành đặt timeout vô hạn có chủ đích tại baseline.
Không tự retry thao tác ghi khi chưa có chống trùng/tra cứu kết quả đã xác minh.

## Các thao tác khác

| Thao tác | Nhánh sống tại baseline |
|---|---|
| Login / HealthCheck | Token v3 qua PartnerUrl auth/token, Username/Password, appid chung |
| FormSerial | Chỉ InvoiceType 0; token v3; code/itg/InvoicePublishing/templates?invyear=; GET 90s |
| Preview | Chỉ InvoiceType 0; login trước mapping để giữ lỗi login; ConvertPreviewInvoiceRequest(model, isInvoiceMTT), code/itg/invoicepublishing/invoicelinkview?type=1 |
| Adjust/Replace | Chỉ InvoiceType 0: isInvoiceMTT → mapper MTT + CreateAndPublishInvoiceMTT; thường 0 → mapper HSM + CreateAndPublishInvoiceHSM; thường USB 2 → mapper USB + GetXMLInvoice |
| PDF | InvoiceType 0: MTT → DownloadMTTPdf, còn lại DownloadHSMPdf; 1 → DownloadTicketPdf; 2 → DownloadInvoicePdf (PartnerUrl2/token biên lai) |
| Void | InvoiceType 0 → VoidInvoiceMTT; 1 → VoidTicket; 2 → VoidInvoice (PartnerUrl2/token biên lai) |
| CQT / status / hash / XML / publish signed XML | Nhánh riêng trong dispatcher; chưa port, cần đọc đủ điều kiện/mapping trước khi expose |
| Draft/Create/SearchById/DownloadDraftPdf | Không có action sống trong switch MISA dù có helper/chuẩn bị dữ liệu/comment |

## Credential/token biên lai — bước kế tiếp

- GetInvoiceToken dùng `GET {PartnerUrl2}user/token?appid={siteId}&taxcode={TaxNumber}&username={Username}&password={Password}`.
- Header token endpoint tên `CompanyTaxcode` nhưng giá trị **site/company GUID**, khác login v3.
- Body publish biên lai có `userName = Username2`, `passWord = Password2`; header business `CompanyTaxCode = TaxNumber` và bearer token biên lai.
- Credential đi qua query theo hợp đồng provider; phải tắt/che log URI của HttpClient trước khi port, không thử thật trong test.
- Cache hiện chưa port: cần tách token v3/biên lai, site/endpoint/account/credential version; single-flight và expiry UTC.

Múi giờ chỉ mới chốt UTC+7 cho **năm FormSerial**, chưa áp dụng cho date/epoch/money mapping.
