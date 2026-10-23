#nullable disable
#pragma warning disable CS0219 // Baseline unused local retained for characterization.
using eInvoice.Core.Contracts;
using eInvoice.Providers.Minvoice.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using static eInvoice.Providers.Minvoice.Wire.MinvoiceModels;
namespace eInvoice.Providers.Minvoice.Mapping;
internal sealed class MinvoiceRequestMapper
{
    public DateTime Now { get; init; }
public string convertInvoiceFormat(JObject model, string companyId)
        {
            JObject res = new JObject();
            MinvoiceCreateInvoiceRequest request = model.ToObject<MinvoiceCreateInvoiceRequest>();
            M_InvoiceInputModel _cvOutput = new M_InvoiceInputModel();
            _cvOutput.editmode = request.editmode;
            if (request.editmode != "1" && request.OriginalInvoiceId != null)
            {
                request.Invoice.InvoiceId = request.OriginalInvoiceId;
                request.Invoice.InvoiceNo = request.OriginalProviderInvoiceNumber == null ? null : Convert.ToInt64(request.OriginalProviderInvoiceNumber);
            }
            InvoiceMaster master = getMasterData(request, companyId);
            _cvOutput.data = new List<InvoiceMaster>();
            _cvOutput.data.Add(master);

            return JsonConvert.SerializeObject(_cvOutput);
        }

public InvoiceMaster getMasterData(MinvoiceCreateInvoiceRequest request, string companyId)
        {
            Invoice model = request.Invoice;
            if (string.IsNullOrEmpty(model.InvoiceDate))
                throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", "Ngày hóa đơn không được bỏ trống");
            if (string.IsNullOrEmpty(model.Serial))
                throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", "Ký hiệu không được bỏ trống");
            if (request.editmode != "1" && model.InvoiceNo == null || model.InvoiceNo == 0)
                throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", "Vui lòng nhập số hóa đơn khi chỉnh sửa, xóa bỏ.");

            HotelInformation extra = request.HotelExtra;
            List<ExtraField> extraData = request.ExtraData;
            string soBenhAn = null;
            if (extraData != null)
            {
				foreach (ExtraField item in extraData)
                {
                    if (item.Code == "FolioNum") soBenhAn = item.StringValue;
					if (item.Code == "BAInvoiceCode") soBenhAn = item.StringValue;
				}
			}
            var tongPhiDichVu = model.ServiceCharge;
            var phanTramPhiDichVu = model.ServiceRate;
			if ((companyId.ToLower() == "36342869-8a14-4c31-b7be-7f46f5a92b13" || companyId.ToLower() == "96443794-55bb-4c12-881f-9e6cc1aa66ba") && request.Details != null && request.Details.Any())
            {
                var itemFirst = request.Details.FirstOrDefault();
				tongPhiDichVu = request.Details.Sum(i => (i.ServiceCharge ?? 0));
                phanTramPhiDichVu = itemFirst.ServiceRate;
			}
			InvoiceMaster master = new InvoiceMaster
            {
                hoadon68_id = model.InvoiceId == null ? null : Guid.Parse(model.InvoiceId),
                inv_invoiceIssuedDate = DateTime.ParseExact(model.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                inv_invoiceSeries = model.Serial,
                inv_invoiceNumber = model.InvoiceNo,
                so_benh_an = soBenhAn,
                inv_currencyCode = model.CurrencyCode,
                inv_exchangeRate = model.ExchangeRate,
                inv_paymentMethodName = model.PaymentMethod,
                inv_buyerDisplayName = model.CustomerName,
                inv_buyerLegalName = model.CompanyName,
                inv_buyerTaxCode = model.CustomerTax,
                inv_buyerAddressLine = model.CustomerAddress,
                inv_buyerEmail = model.CustomerEmail,
                inv_buyerBankAccount = model.BankAccount,
                inv_buyerBankName = model.BankName,
                inv_TotalAmount = model.AfterTaxAmount,
                inv_vatAmount = model.TaxAmount,
                inv_TotalAmountWithoutVat = model.BeforeTaxAmount,
                sobaomat = (model.SearchCode ?? "") != "" ? model.SearchCode : GetSearchCode(),
                isDeductionNQ43 = model.IsTaxReduction43 ?? false,
                tlptdoanhthu20 = model.TaxReductionType,
                tgtck20 = model.TaxReduction43Amount,
				cccd = model.IdentityCard,
				so_hchieu = model.PassportNum,
				pt_phidichvu = phanTramPhiDichVu,
				tongphi_dichvu = tongPhiDichVu,
				mdvqhnsach_nmua = model.BudgetCode
			};
            if (extra != null)
            {
				if (!string.IsNullOrEmpty(extra.FolioNum))
					master.so_folio = extra.FolioNum;
				if (!string.IsNullOrEmpty(extra.RoomCode))
					master.so_phong = extra.RoomCode;
				if (!string.IsNullOrEmpty(extra.ArrivalDate))
					master.inv_arrivalDate = extra.ArrivalDate;
				if (!string.IsNullOrEmpty(extra.DepartureDate))
					master.inv_departureDate = extra.DepartureDate;
			}

            InvoiceDetails details = getdetailData(request.Details, out decimal DiscountAmount);
            master.inv_Amount = details.data.Sum(e => e.inv_Amount);
            master.inv_discountAmount = DiscountAmount;
            master.details = new List<InvoiceDetails>
            {
                details
            };
            return master;
        }

private string GetSearchCode()
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            string totalMilliseconds = ((long)(DateTime.UtcNow - sTime).TotalMilliseconds).ToString();
            int ramdomNumber = new Random().Next(000, 999);
            return $"{totalMilliseconds.Substring(5)}{ramdomNumber}";
        }

public InvoiceDetails getdetailData(List<InvoiceDetail> model, out decimal totalDiscount)
        {
            InvoiceDetails details = new InvoiceDetails();
            details.data = new List<invoiceDetail>();
            int stt = 1;
            totalDiscount = 0;
            foreach (InvoiceDetail i in model)
            {
                invoiceDetail detail = new invoiceDetail();
                detail.stt_rec0 = stt;
                detail.tchat = 1;
                detail.inv_itemName = i.ItemName;
                detail.inv_unitCode = i.UnitName;
                detail.inv_quantity = i.Quantity;
                detail.inv_unitPrice = i.Price;
                detail.inv_discountPercentage = i.DiscountPercent;
                detail.inv_discountAmount = i.DiscountAmount;
                detail.inv_TotalAmountWithoutVat = i.BeforeTaxAmount;
                decimal mathue = i.TaxRate == null ? -1 : (decimal)i.TaxRate;
                detail.ma_thue = (int)(mathue);
                detail.inv_vatAmount = i.TaxAmount;
                detail.inv_TotalAmount = i.AfterTaxAmount;
                detail.thhdv = (i.Quantity == null || i.Price == null) ? 0 : i.Quantity * i.Price;
                detail.inv_Amount = (i.Quantity == null || i.Price == null) ? 0 : i.Quantity * i.Price;
                detail.ortherFee = i.ServiceCharge;
                detail.inv_vatRateDeduction = i.TaxReductionType;
                detail.inv_vatAmountDeduction = i.TaxReduction43Amount;
                detail.pt_phidv = i.ServiceRate;
                detail.phidv = i.ServiceCharge;
                details.data.Add(detail);
                stt++;
                totalDiscount += i.DiscountAmount == null ? 0 : (decimal)i.DiscountAmount;
            }

            return details;
        }

public List<invoiceDetailAdjust> getdetailDataIncrease(List<InvoiceDetail> model)
        {
            List<invoiceDetailAdjust> details = new List<invoiceDetailAdjust>();
            int stt = 1;
            foreach (InvoiceDetail i in model)
            {
                invoiceDetailAdjust detail = new invoiceDetailAdjust();
                detail.stt = stt;
                detail.tchat = 1;
                detail.inv_itemName = i.ItemName;
                detail.inv_unitCode = i.UnitName;
                detail.inv_quantity = i.Quantity;
                detail.inv_unitPrice = i.Price;
                detail.inv_discountPercentage = i.DiscountPercent;
                detail.inv_discountAmount = i.DiscountAmount;
                detail.inv_TotalAmountWithoutVat = i.BeforeTaxAmount;
                decimal mathue = (i.TaxRate == null ? -1 : (decimal)i.TaxRate);
                detail.ma_thue = mathue.ToString("#0.#####");
                detail.inv_vatAmount = i.TaxAmount;
                detail.inv_TotalAmount = i.AfterTaxAmount;
                detail.inv_vatRateDeduction = i.TaxReductionType;
                detail.inv_vatAmountDeduction = i.TaxReduction43Amount;
                //detail.ServiceRate = i.ServiceRate;
                //detail.ServiceCharge = i.ServiceCharge;
                //detail.SubAmount = i.SubAmount;
                //detail.ghi_chu = i.Note;
                detail.thhdv = (i.Quantity == null || i.Price == null) ? 0 : i.Quantity * i.Price;
                detail.inv_Amount = (i.Quantity == null || i.Price == null) ? 0 : i.Quantity * i.Price;
				detail.ortherFee = i.ServiceCharge;
				details.Add(detail);
                stt++;
            }

            return details;
        }

public string convertDataDelete(JObject model)
        {
            DeleteInvoiceRequest request = model.ToObject<DeleteInvoiceRequest>();
            InvoiceDelete invDel = new InvoiceDelete();
            invDel.editmode = "3";
            invDel.data = new List<invDel_detail>();
            invDel_detail detail = new invDel_detail();
            Int32.TryParse(request.ProviderInvoiceNumber, out Int32 invNumber);
            detail.inv_invoiceNumber = invNumber;
            detail.inv_invoiceSeries = request.Serial;
            invDel.data.Add(detail);
            return JsonConvert.SerializeObject(invDel);
        }

public string convertReplaceInv(JObject model)
        {
            AdjustOrReplaceInvoiceRequest request = model.ToObject<AdjustOrReplaceInvoiceRequest>();
            InvoiceMasterReplace master = replaceMasterConvert(request);
            M_InvoceReplaceModel minvoice = new M_InvoceReplaceModel();
            minvoice.data = new List<InvoiceMasterReplace>();
            minvoice.data.Add(master);
            return JsonConvert.SerializeObject(minvoice);
        }

private InvoiceMasterReplace replaceMasterConvert(AdjustOrReplaceInvoiceRequest request)
        {
            Invoice model = request.Invoice;
            HotelInformation extra = request.HotelExtra;
            InvoiceMasterReplace master = new InvoiceMasterReplace();
            master.inv_originalId = request.OriginalInvoiceId;
            master.hoadon68_id = model.InvoiceId == null ? null : Guid.Parse(model.InvoiceId);
            master.ngayvb = Now;
            master.inv_invoiceIssuedDate = DateTime.ParseExact(model.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"); ;
            master.inv_invoiceSeries = model.Serial;
            master.inv_invoiceNumber = Convert.ToInt64(request.OriginalProviderInvoiceNumber);
            master.so_benh_an = null;
            master.inv_currencyCode = model.CurrencyCode;
            master.inv_exchangeRate = model.ExchangeRate;
            master.inv_paymentMethodName = model.PaymentMethod;
            master.inv_buyerDisplayName = model.CustomerName;
            master.inv_buyerLegalName = model.CompanyName;
            master.inv_buyerTaxCode = model.CustomerTax;
            master.inv_buyerAddressLine = model.CustomerAddress;
            master.inv_buyerEmail = model.CustomerEmail;
            master.inv_buyerBankAccount = model.BankAccount;
            master.inv_buyerBankName = model.BankName;
            master.inv_TotalAmount = model.AfterTaxAmount;
            master.inv_discountAmount = 0;
            master.inv_vatAmount = model.TaxAmount;
            master.inv_TotalAmountWithoutVat = model.BeforeTaxAmount;
            master.isDeductionNQ43 = model.IsTaxReduction43 ?? false;
            master.tlptdoanhthu20 = model.TaxReductionType;
            master.tgtck20 = model.TaxReduction43Amount;
            //master.SubAmount = model.SubAmount;
            //master.ServiceCharge = model.ServiceCharge;
            //master.ServiceRate = model.ServiceRate;
            //master.ghi_chu = model.Notice;
            master.cccd = model.IdentityCard;
            master.so_hchieu = model.PassportNum;
            master.pt_phidichvu = model.ServiceRate;
            master.tongphi_dichvu = model.ServiceCharge;
            master.mdvqhnsach_nmua = model.BudgetCode;

			if (extra != null)
            {
                if (!string.IsNullOrEmpty(extra.FolioNum))
                    master.so_folio = extra.FolioNum;
                if (!string.IsNullOrEmpty(extra.RoomCode))
                    master.so_phong = extra.RoomCode;
                if (!string.IsNullOrEmpty(extra.ArrivalDate))
                    master.inv_arrivalDate = extra.ArrivalDate;
                if (!string.IsNullOrEmpty(extra.DepartureDate))
                    master.inv_departureDate = extra.DepartureDate;
            }
            InvoiceDetails details = getdetailData(request.Details, out decimal DiscountAmount);
            master.inv_Amount = details.data.Sum(e => e.inv_Amount);
            master.inv_discountAmount = DiscountAmount;
            master.details = new List<InvoiceDetails>();
            master.details.Add(details);
            return master;
        }

public string convertAdjustInv(JObject model, int method)
        {
            AdjustOrReplaceInvoiceRequest request = model.ToObject<AdjustOrReplaceInvoiceRequest>();
            InvoiceMasterAdjust master = adjustMasterConvert(request, method);

            return JsonConvert.SerializeObject(master);
        }

private InvoiceMasterAdjust adjustMasterConvert(AdjustOrReplaceInvoiceRequest request, int method)
        {
            Invoice model = request.Invoice;
            HotelInformation extra = request.HotelExtra;
            InvoiceMasterAdjust master = new InvoiceMasterAdjust();
            master.inv_InvoiceAuth_id = request.OriginalInvoiceId;
            master.inv_invoiceIssuedDate = DateTime.ParseExact(request.Invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"); ;
			if (extra != null)
			{
				if (!string.IsNullOrEmpty(extra.FolioNum))
					master.so_folio = extra.FolioNum;
				if (!string.IsNullOrEmpty(extra.RoomCode))
					master.so_phong = extra.RoomCode;
				if (!string.IsNullOrEmpty(extra.ArrivalDate))
					master.inv_arrivalDate = extra.ArrivalDate;
				if (!string.IsNullOrEmpty(extra.DepartureDate))
					master.inv_departureDate = extra.DepartureDate;
			}
			List<invoiceDetailAdjust> details = new List<invoiceDetailAdjust>();
            decimal DiscountAmount = 0;
            details = getdetailDataIncrease(request.Details);
            master.data = new List<invoiceDetailAdjust>();
            master.data = details;
            return master;
        }

public string convertCancelInv(JObject model)
        {
            VoidInvoiceRequest request = model.ToObject<VoidInvoiceRequest>();
            JObject body = new JObject
            {
                ["inv_InvoiceAuth_id"] = request.InvoiceId,
                ["ngayvb"] = Now.ToString("yyyy-MM-dd"),
                ["ghi_chu"] = $"Hủy hóa đơn số {request.ProviderInvoiceNumber}"
            };
            return body.ToString();
        }

public string convertSignInv(JObject model)
        {
            VoidInvoiceRequest request = model.ToObject<VoidInvoiceRequest>();
            JObject data = new JObject()
            {
                ["hoadon68_id"] = request.InvoiceId
            };
            return data.ToString();
        }

public string convertGetInv(JObject model)
        {
            GetInvoicesRequest request = model.ToObject<GetInvoicesRequest>();
            if (request != null)
            {
                GetInvoiceRequest minvoice = new GetInvoiceRequest()
                {
                    tuNgay = request.FromDate.ToString("yyyy-MM-dd"),
                    denNgay = request.ToDate.ToString("yyyy-MM-dd"),
                    khieu = request.Serial,
                };
                return JsonConvert.SerializeObject(minvoice);
            }
            return "";
        }
}
