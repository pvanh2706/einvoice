#nullable disable
namespace eInvoice.Providers.Minvoice.Wire;
public class MinvoiceModels
{
        public class M_InvoiceInputModel
        {
            public string editmode { get; set; }
            public List<InvoiceMaster> data { get; set; }
        }
        public class M_InvoceReplaceModel
        {
            public List<InvoiceMasterReplace> data { get; set; }
        }
        public class InvoiceMasterReplace : InvoiceMaster
        {
            public string inv_originalId { get; set; }
            public Nullable<DateTime> ngayvb { get; set; }
            public string sovb { get; set; }
        }
        public class InvoiceMasterAdjust
        {
            public string inv_InvoiceAuth_id { get; set; }
            public string inv_invoiceIssuedDate { get; set; }
			public string so_folio { get; set; }
			public string so_phong { get; set; }
			public string inv_arrivalDate { get; set; }
			public string inv_departureDate { get; set; }
			public List<invoiceDetailAdjust> data { get; set; }
        }
        public class InvoiceMaster
        {
            public Nullable<Guid> hoadon68_id { get; set; }
            public string inv_invoiceIssuedDate { get; set; }
            public string inv_invoiceSeries { get; set; }
            public Nullable<Int64> inv_invoiceNumber { get; set; }
            public string so_benh_an { get; set; }
            public string inv_currencyCode { get; set; }
            public Nullable<decimal> inv_exchangeRate { get; set; }
            public string inv_paymentMethodName { get; set; }
            public string inv_buyerDisplayName { get; set; }
            public string inv_buyerLegalName { get; set; }
            public string inv_buyerTaxCode { get; set; }
            public string inv_buyerAddressLine { get; set; }
            public string inv_buyerEmail { get; set; }
            public string inv_buyerBankAccount { get; set; }
            public string inv_buyerBankName { get; set; }
            public Nullable<decimal> inv_TotalAmount { get; set; }
            public Nullable<decimal> inv_discountAmount { get; set; }
            public Nullable<decimal> inv_vatAmount { get; set; }
            public Nullable<decimal> inv_TotalAmountWithoutVat { get; set; }
            public string so_folio { get; set; }
            public string so_phong { get; set; }
            public string inv_arrivalDate { get; set; }
            public string inv_departureDate { get; set; }
            public string sobaomat { get; set; }
            // Có được giảm thuế GTGT HDBH không
            public bool isDeductionNQ43 { get; set; }
            // Tỉ lệ giảm
            public Nullable<int> tlptdoanhthu20 { get; set; }
            // Số tiền giảm
            public Nullable<decimal> tgtck20 { get; set; }
            public List<InvoiceDetails> details { get; set; }
			public string so_hchieu { get; set; }
			public string cccd { get; set; }
			public Nullable<decimal> pt_phidichvu { get; set; }
			public Nullable<decimal> tongphi_dichvu { get; set; }
            public Nullable<decimal> inv_Amount { get; set; }
            public string mdvqhnsach_nmua { get; set; }

		}
        public class InvoiceDetails
        {
            public List<invoiceDetail> data { get; set; }
        }
        public class invoiceDetail
        {
            public int stt_rec0 { get; set; }
            public int tchat { get; set; }
            public string inv_itemCode { get; set; }
            public string inv_itemName { get; set; }
            public string inv_unitCode { get; set; }
            public Nullable<decimal> inv_quantity { get; set; }
            public Nullable<decimal> inv_unitPrice { get; set; }
            public Nullable<decimal> inv_discountPercentage { get; set; }
            public Nullable<decimal> inv_discountAmount { get; set; }
            public Nullable<decimal> inv_TotalAmountWithoutVat { get; set; }
            public Nullable<int> ma_thue { get; set; }
            public Nullable<decimal> inv_vatAmount { get; set; }
            public Nullable<decimal> inv_TotalAmount { get; set; }
            public Nullable<decimal> thhdv { get; set; }
            public Nullable<decimal> inv_Amount { get; set; }
            public Nullable<decimal> ortherFee { get; set; }
            public Nullable<int> inv_vatRateDeduction { get; set; }
            public Nullable<decimal> inv_vatAmountDeduction { get; set; }
			public Nullable<decimal> pt_phidv { get; set; }
			public Nullable<decimal> phidv { get; set; }
		}

        public class InvoiceDetailsAdjust
        {
            public List<invoiceDetailAdjust> data { get; set; }
        }
        public class invoiceDetailAdjust
        {
            public int stt { get; set; }
            public int tchat { get; set; }
            public string ma { get; set; }
            public string inv_itemName { get; set; }
            public string inv_unitCode { get; set; }
            public Nullable<decimal> inv_quantity { get; set; }
            public Nullable<decimal> inv_unitPrice { get; set; }
            public Nullable<decimal> inv_discountPercentage { get; set; }
            public Nullable<decimal> inv_discountAmount { get; set; }
            public Nullable<decimal> inv_TotalAmountWithoutVat { get; set; }
            public string ma_thue { get; set; }
            public Nullable<decimal> inv_vatAmount { get; set; }
            public Nullable<decimal> inv_TotalAmount { get; set; }
            public Nullable<decimal> ServiceRate { get; set; }
            public Nullable<decimal> ServiceCharge { get; set; }
            public Nullable<decimal> SubAmount { get; set; }
            public string ghi_chu { get; set; }
            public Nullable<decimal> thhdv { get; set; }
            public Nullable<decimal> inv_Amount { get; set; }
			public Nullable<decimal> ortherFee { get; set; }
            public Nullable<int> inv_vatRateDeduction { get; set; }
            public Nullable<decimal> inv_vatAmountDeduction { get; set; }
        }
        public class InvoiceDelete
        {
            public string editmode { get; set; }
            public List<invDel_detail> data { get; set; }
        }
        public class invDel_detail
        {
            public string inv_invoiceSeries { get; set; }
            public Nullable<Int64> inv_invoiceNumber { get; set; }
        }
        public class SendEmailCvt
        {
            public List<EmailId> id { get; set; }
            public string type { get; set; }
        }
        public class EmailId
        {
            public Guid? id { get; set; }
            public string nguoi_nhan { get; set; }
        }
}
public class GetInvoiceRequest { public string tuNgay { get; set; } public string denNgay { get; set; } public string khieu { get; set; } }
