using eInvoice.Providers.Vnpt.Baseline.MTT;
#nullable disable
#pragma warning disable CS1998, CS0219, CS0414
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using eInvoice.Providers.Vnpt.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Dynamic;
using System.Globalization;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using PublishService_V5;
using PortalService_V5;
using BusinessService_V5;
namespace eInvoice.Providers.Vnpt.Baseline
{
	internal static class VnptMTTConvertRequestCurrent
	{
		private const int NOT_CONVERT_TCVN3_TO_UNICODE = 0;
		public static List<string> guidNotSendServiceSummaryMTT = new List<string>
			{
				"E0E57A80-081C-403F-9384-3E706172EAF1", // Samten Hills
				"0759C12F-A22F-4DF8-8FC9-9762DD52C56D", // Lumiere
				"F97FF80D-8EEA-41BE-A515-AF92EE24FBC4", // TTC Đà Lạt
				"D4CA28B8-E813-43AB-874C-BA443DB340B8", // TTC MeKong
				"F97FF80D-8EEA-41BE-A515-AF92EE24FBC4", // Nova Phan Thiết
                "879124F5-E39B-4A63-956F-F5732D79A041", // Nova Cầu Đất
                "0853A6A5-3220-4B2E-A63F-8719463C64A2", // Gami
				"8AC4935D-7029-48ED-8D09-C9C4C67B6906", // Mikazuki
				"AC51CE32-C055-4471-BE1B-33B641478C7E", // Yên Tử
			};
		public static bool IsNotSendServiceSummary(Guid? guidCheck)
		{
			return guidNotSendServiceSummaryMTT.Contains(guidCheck == null ? "" : guidCheck.ToString());
		}

		//public static BusinessService_MTT.getPublishInvsRequest ConvertFormNoSerialRequest(dynamic model)
		//{
		//	return new BusinessService_MTT.getPublishInvsRequest()
		//	{
		//		Body = new BusinessService_MTT.getPublishInvsRequestBody()
		//		{
		//			userName = model.Company.Username2,
		//			userPass = model.Company.Password2,
		//		}
		//	};
		//}

		public static GetCertInfoRequest ConvertGetSerialCertInfoRequest(dynamic model)
		{

			return new GetCertInfoRequest()
			{
				Body = new GetCertInfoRequestBody()
				{
					userName = model.Company.Username2,
					password = model.Company.Password2,
				}
			};
		}
		public static SendInvMTTFkeyRequest SendToTaxAuthorityRequest(dynamic model, string SerialCert)
		{

			SendToTaxAuthorityRequest invoiceRequest = model.Data as SendToTaxAuthorityRequest;

			return new SendInvMTTFkeyRequest()
			{
				Body = new SendInvMTTFkeyRequestBody()
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = invoiceRequest.FormNo,
					serial = invoiceRequest.Serial,
					lstFkey = string.Join("_", invoiceRequest.InvoiceIds),
					serialCert = SerialCert
				}
			};
		}

		public static GetMCCQThueByFkeysRequest GetStatusInvoiceRequest(dynamic model)
		{

			SendToTaxAuthorityRequest invoiceRequest = model.Data as SendToTaxAuthorityRequest;

			return new GetMCCQThueByFkeysRequest()
			{
				Body = new GetMCCQThueByFkeysRequestBody()
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = invoiceRequest.FormNo,
					fkeys = string.Join(";", invoiceRequest.InvoiceIds)
				}
			};
		}

		public static ImportInvByPatternMTTRequest ConvertCreateInvoiceRequest(dynamic model)
		{
			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			bool isSendServiceSummary = !IsNotSendServiceSummary(model.Company.Id);
			DSHDon invoices = new DSHDon
			{
				HDon = new HDon
				{
					key = pmsInvoice.InvoiceId.ToString(),
					DLHDon = new DLHDon
					{
						TTChung = GetTTChung(pmsInvoice),
						NDHDon = GetNDHDon(invoiceRequest, extraDataMap, isSendServiceSummary)
					}
				}
			};

			var result = new ImportInvByPatternMTTRequest()
			{
				Body = new ImportInvByPatternMTTRequestBody
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					// xmlInvData = XmlHelper.Serialize(invoices),
					// Dùng để làm động dữ liệu mapping với ExtraData 
					// => có thể cấu hình trường trong bảng ExtraDataMaps khi cần đầy thêm thông tin
					xmlInvData = XmlHelperObject.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
				}
			};
			return result;
		}
		public static downloadNewInvPDFFkeyRequest ConvertDownloadPDFInvoiceDraftRequest(dynamic model)
		{
			return new downloadNewInvPDFFkeyRequest
			{
				Body = new downloadNewInvPDFFkeyRequestBody
				{
					fkey = model.Data.InvoiceId,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}
		public static ImportAndPublishInvMTTRequest ConvertCreateAndPublishInvoiceRequest(dynamic model)
		{

			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			bool isSendServiceSummary = !IsNotSendServiceSummary(model.Company.Id);
			DSHDon invoices = new DSHDon
			{
				HDon = new HDon
				{
					key = pmsInvoice.InvoiceId.ToString(),
					DLHDon = new DLHDon
					{
						TTChung = GetTTChung(pmsInvoice),
						NDHDon = GetNDHDon(invoiceRequest, extraDataMap, isSendServiceSummary)
					}
				}
			};

			var result = new ImportAndPublishInvMTTRequest()
			{
				Body = new ImportAndPublishInvMTTRequestBody
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					// xmlInvData = XmlHelper.Serialize(invoices),
					// Dùng để làm động dữ liệu mapping với ExtraData 
					// => có thể cấu hình trường trong bảng ExtraDataMaps khi cần đầy thêm thông tin
					xmlInvData = XmlHelperObject.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
				}
			};
			return result;
		}
		public static downloadInvPDFFkeyNoPayErrorRequest ConvertDownloadPDFRequestNoPay(dynamic model)
		{
			return new downloadInvPDFFkeyNoPayErrorRequest
			{
				Body = new downloadInvPDFFkeyNoPayErrorRequestBody
				{
					fkey = model.Data.InvoiceId,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}
		public static getInvViewFkeyRequest ConvertDownloadPDFFkeyRequest(dynamic model)
		{
			return new getInvViewFkeyRequest
			{
				Body = new getInvViewFkeyRequestBody
				{
					fkey = model.Data.InvoiceId,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}
		public static AdjustInvMTTRequest ConvertAdjustInvoiceRequest(dynamic model, int action)
		{
			AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			bool isSendServiceSummary = !IsNotSendServiceSummary(model.Company.Id);
			var adjust = new DieuChinhHD
			{
				key = pmsInvoice.InvoiceId.ToString(),
				TTChung = GetTTChung(pmsInvoice),
				Type = ConvertInvoiceActionToTypeAdjust(action),
				NDHDon = GetNDHDon(invoiceRequest, extraDataMap, isSendServiceSummary),
			};

			var result = new AdjustInvMTTRequest()
			{
				Body = new AdjustInvMTTRequestBody
				{
					fkey = invoiceRequest.OriginalInvoiceId,
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					// xmlInvData = XmlHelper.Serialize(adjust),
					// Dùng để làm động dữ liệu mapping với ExtraData 
					// => có thể cấu hình trường trong bảng ExtraDataMaps khi cần đầy thêm thông tin
					xmlInvData = XmlHelperObject.Serialize(adjust),
					username = model.Company.Username2,
					pass = model.Company.Password2,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
					OldPattern = pmsInvoice.FormNo,
				}
			};
			return result;
		}
		public static ReplaceInvMTTRequest ConvertReplaceInvoiceRequest(dynamic model)
		{
			AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			bool isSendServiceSummary = !IsNotSendServiceSummary(model.Company.Id);
			var replace = new ThayTheHD
			{
				key = pmsInvoice.InvoiceId.ToString(),
				TTChung = GetTTChung(invoiceRequest.Invoice),
				NDHDon = GetNDHDon(invoiceRequest, extraDataMap, isSendServiceSummary)
			};

			var result = new ReplaceInvMTTRequest()
			{
				Body = new ReplaceInvMTTRequestBody
				{
					fkey = invoiceRequest.OriginalInvoiceId,
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					// xmlInvData = XmlHelper.Serialize(replace),
					// Dùng để làm động dữ liệu mapping với ExtraData 
					// => có thể cấu hình trường trong bảng ExtraDataMaps khi cần đầy thêm thông tin
					xmlInvData = XmlHelperObject.Serialize(replace),
					username = model.Company.Username2,
					pass = model.Company.Password2,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
					OldPattern = pmsInvoice.FormNo,
				}
			};
			return result;
		}
		public static cancelInvNoPayRequest ConvertVoidInvoiceRequest(dynamic model)
		{
			return new cancelInvNoPayRequest
			{
				Body = new cancelInvNoPayRequestBody
				{
					fkey = model.Data.InvoiceId,
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}


		// private
		private static Invoice MapInvoice(CreateInvoiceRequest invoiceRequest, CoreInvoice pmsInvoice)
		{
			//HotelReservationModel hotelReservation = pmsInvoice.HotelReservation;
			Invoice invoice = new Invoice
			{
				CusCode = pmsInvoice.InvoiceId.ToString(),
				CusName = pmsInvoice.CompanyName ?? "",
				CusAddress = pmsInvoice.CustomerAddress ?? "",
				CusPhone = pmsInvoice.CustomerPhone ?? "",
				CusTaxCode = pmsInvoice.CustomerTax ?? "",
				PaymentMethod = pmsInvoice.PaymentMethod ?? "",
				CusBankName = pmsInvoice.BankName ?? "",
				CusBankNo = pmsInvoice.BankAccount ?? "",
				ArisingDate = pmsInvoice.InvoiceDate ?? "",
				EmailDeliver = pmsInvoice.CustomerEmail ?? "",
				Buyer = pmsInvoice.CustomerName ?? "",
				// ServicechargeAmount = (pmsInvoice.ServiceCharge ?? 0).ToString(),
				Total = (pmsInvoice.BeforeTaxAmount ?? 0).ToString(),
				//DiscountRate = details.Sum(e => e.DiscountPercent).ToString(),
				//DiscountAmount = details.Sum(e => e.DiscountAmount).ToString(),
				//GrossValue = details.Where(e => e.TaxRate != 5 && e.TaxRate != 10).Sum(e => e.BeforeTaxAmount).ToString(),
				//GrossValue5 = details.Where(e => e.TaxRate == 5).Sum(e => e.BeforeTaxAmount).ToString(),
				//GrossValue10 = details.Where(e => e.TaxRate == 10).Sum(e => e.BeforeTaxAmount).ToString(),
				//VatAmount0 = details.Where(e => e.TaxRate != 5 && e.TaxRate != 10).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount5 = details.Where(e => e.TaxRate == 5).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount10 = details.Where(e => e.TaxRate == 10).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount30 = "",
				VATRate = (pmsInvoice.TaxRate ?? 0).ToString(),
				VATAmount = (pmsInvoice.TaxAmount ?? 0).ToString(),
				Amount = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
				AmountInWords = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0),
				CurrencyUnit = pmsInvoice.CurrencyCode ?? "",
				ExchangeRate = (pmsInvoice.ExchangeRate ?? 0).ToString(),
				ConvertedAmount = ((pmsInvoice.AfterTaxAmount ?? 0) * (pmsInvoice.ExchangeRate ?? 0)).ToString(),
				PaymentStatus = "1",
				Note = pmsInvoice.Notice ?? "",
				//RoomNo = hotelReservation?.RoomCode,
				//FolioNo = hotelReservation?.FolioNum,
				//Arrival = hotelReservation?.ArrivalDate,
				//Departure = hotelReservation?.DepartureDate,
				Products = ConvertDetail(invoiceRequest.Details)
			};
			return invoice;
		}
		private static List<Product> ConvertDetail(List<InvoiceDetail> details)
		{
			return details.Select(item => new Product()
			{
				ProdName = item.ItemName,
				ProdUnit = item.UnitName,
				ProdQuantity = item.Quantity.ToString(),
				ProdPrice = item.Price.ToString(),
				Total = item.BeforeTaxAmount.ToString(),
				//Discount = item.DiscountPercent.ToString(),
				//DiscountAmount = item.DiscountAmount.ToString(),
				VATRate = ((int)(item.TaxRate ?? 0)).ToString(),
				VATAmount = item.TaxAmount.ToString(),
				Amount = item.BeforeTaxAmount.ToString(),
				// Remark = item.Note,
				IsSum = (item.IsItemSpecialTax ?? false) ? "4" : null,
			}).ToList();
		}
		private static TTChung GetTTChung(CoreInvoice pmsInvoice)
		{
			return new TTChung()
			{
				// NBKe = ConvertToV5DateTime(pmsInvoice.InvoiceDate),
				NLap = ConvertToV5DateTime(pmsInvoice.InvoiceDate),
				DVTTe = pmsInvoice.CurrencyCode ?? "",
				TGia = (pmsInvoice.ExchangeRate ?? 0).ToString(),
				HTTToan = pmsInvoice.PaymentMethod ?? ""
			};
		}
		private static object GetNDHDon(CreateInvoiceRequest pmsRequest, List<ExtraFieldMapping> extraDataMap, bool isSendServiceSummary)
		{
			var company = VnptMappingScope.Company as SiteConfiguration;
			var pmsInvoice = pmsRequest.Invoice;
			List<ServiceSummary> serviceSummarys = pmsRequest.ServiceSummaries;
			List<SpecialTaxSummary> SpecialTaxSummarys = pmsRequest.SpecialTaxSummaries;
			// Dùng để làm động dữ liệu mapping với ExtraData 
			// => có thể cấu hình trường trong bảng ExtraDataMaps khi cần đầy thêm thông tin
			dynamic nMua = new ExpandoObject();
			nMua.Ten = pmsInvoice.CompanyName;
			nMua.MST = pmsInvoice.CustomerTax;
			nMua.DChi = pmsInvoice.CustomerAddress;
			nMua.SDThoai = pmsInvoice.CustomerPhone;
			nMua.DCTDTu = pmsInvoice.CustomerEmail;
			nMua.HVTNMHang = pmsInvoice.CustomerName;
			nMua.STKNHang = pmsInvoice.BankAccount;
			nMua.TNHang = pmsInvoice.BankName;
			nMua.CCCDan = pmsInvoice.IdentityCard;
			nMua.SHChieu = pmsInvoice.PassportNum;
			if (serviceSummarys != null && serviceSummarys.Any() && isSendServiceSummary)
			{
				nMua.ServicechargeRate = serviceSummarys.FirstOrDefault()?.ServiceRate;
				nMua.ServicechargeAmount = serviceSummarys.FirstOrDefault()?.AfterTaxAmount;
			}
			if (SpecialTaxSummarys != null && SpecialTaxSummarys.Any() && isSendServiceSummary)
			{
				nMua.LuxuryTaxRate = SpecialTaxSummarys.FirstOrDefault()?.SpecialTaxRate;
				nMua.VatAmount30 = SpecialTaxSummarys.FirstOrDefault()?.AfterTaxAmount;
			}
			List<TTin> extraDataMTT = new List<TTin>();
			if (extraDataMap != null && extraDataMap.Any() && pmsRequest.ExtraData != null && pmsRequest.ExtraData.Any())
			{
				var extraDataMapPMS = pmsRequest.ExtraData
					.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new
					{
						e.Code,
						e.ThirdPartyCode,
						p.StringValue,
						e.ThirdPartyDataType,
					})
					.ToList();
				foreach (var itemMap in extraDataMapPMS.Where(e => !e.ThirdPartyCode.Contains(".")))
				{
					//  nMua[itemMap.ThirdPartyCode] = itemMap.StringValue;
					((IDictionary<string, object>)nMua).Add(itemMap.ThirdPartyCode, itemMap.StringValue);
				}
				extraDataMTT = extraDataMapPMS.Where(e => e.ThirdPartyCode.Contains(".")).Select(e => new TTin
				{
					TTruong = e.ThirdPartyCode,
					KDLieu = e.ThirdPartyDataType,
					DLieu = e.StringValue,
				}).ToList();
			}
            dynamic tToan = new ExpandoObject();

			tToan.THTTLTSuat = new THTTLTSuat
			{
				LTSuat = GetLTSuat(pmsInvoice, pmsRequest.TaxSummaries)
			};
			tToan.TgTCThue = pmsInvoice.BeforeTaxAmount.ToString();
			tToan.TgTThue = (pmsInvoice.TaxAmount ?? 0).ToString();
			tToan.TgTTTBSo = (pmsInvoice.AfterTaxAmount ?? 0).ToString();
			tToan.TgTTTBChu = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0);

			if (extraDataMTT.Any(e => e.TTruong.Contains("NMua")))
			{
				TTKhac extraNMua = TTKhac(extraDataMTT, "NMua");
				((IDictionary<string, object>)nMua).Add("TTKhac", extraNMua);
			}

			if (extraDataMTT.Any(e => e.TTruong.Contains("TToan")))
			{
                TTKhac extraTToan = TTKhac(extraDataMTT, "TToan");
                ((IDictionary<string, object>)tToan).Add("TTKhac", extraTToan);
            }

			object NMua = nMua;

			// Cấu hình động để add thẻ MBan
            dynamic result = new ExpandoObject();
            dynamic nBan = new ExpandoObject();
			if (extraDataMTT.Any(e => e.TTruong.Contains("NBan")))
			{
				TTKhac extraNBan = TTKhac(extraDataMTT, "NBan");
				((IDictionary<string, object>)nBan).Add("TTKhac", extraNBan);
				result.NBan = nBan;
			}
            result.NMua = NMua;
            result.DSHHDVu = new eInvoice.Providers.Vnpt.Baseline.V5.DSHHDVu
            {
                HHDVu = VnptConvertRequest.GetDSHHDVu(pmsRequest)
            };
            result.TToan = tToan;
            return (object)result;
		}
		/// <summary>
		/// Lấy extra của các thẻ cha
		/// </summary>
		/// <returns></returns>
        private static TTKhac TTKhac(List<TTin> extraDataMTT, string Parent)
        {
            List<TTin> ttin =  extraDataMTT.Where(e => e.TTruong.Contains(Parent)).Select(e => new TTin
            {
                TTruong = e.TTruong.Split(".")[1],
                KDLieu = e.KDLieu,
                DLieu = e.DLieu,
            }).ToList();

			return new TTKhac
			{
				TTin = ttin,
			};
        }

        private static List<LTSuat> GetLTSuat(CoreInvoice pmsInvoice, List<TaxSummary> pmsTaxes)
		{
			List<LTSuat> result = new List<LTSuat>();

			if (pmsTaxes != null && pmsTaxes.Count() >= 2)
			{
				foreach (TaxSummary item in pmsTaxes)
				{
					result.Add(new LTSuat
					{
						TSuat = item.TaxRate.ToString(),
						ThTien = item.BeforeTaxAmount.ToString(),
						TThue = item.TaxAmount.ToString()
					});
				}
			}
			else
			{
				result = new List<LTSuat> {
					new LTSuat  {
						TSuat = pmsInvoice.TaxRate.ToString(),
						ThTien = pmsInvoice.BeforeTaxAmount.ToString(),
						TThue = pmsInvoice.TaxAmount.ToString()
					}
				};

			}
			return result;
		}

		private static string ConvertInvoiceActionToTypeAdjust(int invoiceAction)
		{
			string type = VnptConst.TYPE_ADJUST_INFOR;
			switch (invoiceAction)
			{
				case (int)INVOICE_ACTION.ADJUST_INCREASE:
					type = VnptConst.TYPE_ADJUST_INCREASE;
					break;
				case (int)INVOICE_ACTION.ADJUST_DECREASE:
					type = VnptConst.TYPE_ADJUST_DECREASE;
					break;
				case (int)INVOICE_ACTION.ADJUST_INFOR:
					type = VnptConst.TYPE_ADJUST_INFOR;
					break;
				default:
					break;
			}
			return type;
		}
		private static string ConvertToV5DateTime(string oldFormat)
		{
			string date = oldFormat;
			if (!string.IsNullOrEmpty(oldFormat))
			{
				if (DateTime.TryParseExact(oldFormat, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var temp))
				{
					date = temp.ToString("yyyy-MM-dd");
				}
			}

			return date ?? "";
		}
	}
}
