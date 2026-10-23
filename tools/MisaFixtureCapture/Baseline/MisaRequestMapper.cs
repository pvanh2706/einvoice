// Provider contract/mapping ported from pinned legacy 66a9c74. Wire names must remain unchanged.
// Nullable warnings are disabled only in this baseline boundary; callers validate canonical input.
#nullable enable annotations
#nullable disable warnings
using System.Globalization;
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Capture.Misa.Wire;
namespace eInvoice.Capture.Misa.Mapping
{
	internal static class MisaRequestMapper
	{
		private static string CustomField1 = "CustomField1";
		private static string CustomField2 = "CustomField2";
		private static string CustomField3 = "CustomField3";
		private static string CustomField4 = "CustomField4";
		private static string CustomField5 = "CustomField5";
		private static string CustomField6 = "CustomField6";
		private static string CustomField7 = "CustomField7";
		private static string CustomField8 = "CustomField8";
		private static string CustomField9 = "CustomField9";
		private static string CustomField10 = "CustomField10";
		private static string formNo_GTGT = "1";
		private static string formNo_HDBH = "2";
		private static string TimeFix = " 00:00:00 AM";
		private static string KeyExtraDataNotice = "InvoiceNotice";
		private static string KeyExtraDataTACompany = "TACompanyCode";


		public static MisaConnectionInfo ConvertConnectionRequest(MisaMappingContext model)
		{
			return new MisaConnectionInfo
			{
				CompanyId = model.Company.Id.ToString(),
				PartnerUrl = model.Company.PartnerUrl,
				PartnerUrl2 = model.Company.PartnerUrl2,
				TaxNumber = model.Company.TaxNumber,
				Username = model.Company.Username,
				Username2 = model.Company.Username2,
				Password = model.Company.Password,
				Password2 = model.Company.Password2,
				Address = model.Company.Address,
				Name = model.Company.Name
			};
		}

		public static Object ConvertPreviewInvoiceRequest(MisaMappingContext models, bool isInvoiceMTT = false)
		{
			try
			{
				//  CreateInvoiceRequest invoiceRequest = models.Data as CreateInvoiceRequest;
				//	Invoice invoice = invoiceRequest.Invoice;
				AdjustOrReplaceInvoiceRequest invoiceRequest = models.Data as AdjustOrReplaceInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				Invoice invoiceOld = invoiceRequest.OriginalInvoice;

				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				string checkIn = "";
				string checkOut = "";
				string roomCode = "";
				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								checkIn = ConvertDateFormat(itemMap.stringValue);
							}
							if (itemMap.keyTag == "CheckOut")
							{
								checkOut = ConvertDateFormat(itemMap.stringValue);
							}
							if (itemMap.keyTag == "RoomNo")
							{
								roomCode = itemMap.stringValue;
							}
						}
					}
				}

				if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
				{
					invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
				}

				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

				int? referenceType = null;
				int? orgInvoiceType = null;
				string orgInvTemplateNo = null;
				string orgInvSeries = null;
				string orgInvNo = null;
				string orgInvDate = null;

				if (invoice.PreviewType == 3 || invoice.PreviewType == 2)
				{
					referenceType = invoice.PreviewType == 3 ? 1 : 2; // 1: thay thế; 2: điều chỉnh
					orgInvoiceType = 1; // Mặc định lấy theo nghị định 123, tương ứng với số 1
					orgInvTemplateNo = invoiceOld.FormNo;
					orgInvSeries = RemoveFirstCharSerial(invoiceOld.Serial);
					orgInvNo = invoiceRequest.OriginalProviderInvoiceNumber;
					orgInvDate = DateTime.ParseExact(invoiceOld.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd");
				}


				Object result = new
				{
					RefID = invoice.InvoiceId.ToString(),
					InvSeries = invoice.Serial,
					InvoiceName = "Hóa đơn giá trị gia tăng",
					InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
					CurrencyCode = invoice.CurrencyCode,
					ExchangeRate = invoice.ExchangeRate,
					PaymentMethodName = invoice.PaymentMethod,
					BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
					BuyerLegalName = invoice.CompanyName,
					BuyerTaxCode = invoice.CustomerTax,
					BuyerAddress = invoice.CustomerAddress,
					BuyerFullName = (invoice.CustomerName ?? "").Trim(),
					BuyerBankAccount = invoice.BankAccount,
					BuyerBankName = invoice.BankName,
					BuyerPhoneNumber = invoice.CustomerPhone,
					BuyerEmail = invoice.CustomerEmail,

					BuyerIDNumber = invoice.IdentityCard,
					BuyerPassport = invoice.PassportNum,
					BuyerBudgetCode = invoice.BudgetCode,

					TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
					TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
					TotalDiscountAmount = totalDiscount,
					TotalDiscountAmountOC = totalDiscount,
					TotalAmountWithoutVATOC = invoice.BeforeTaxAmount,
					TotalAmountWithoutVAT = invoice.BeforeTaxAmount * (invoice.ExchangeRate ?? 0),
					TotalVATAmountOC = invoice.TaxAmount ?? 0,
					TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
					TotalAmountOC = invoice.AfterTaxAmount,
					TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
					TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
					OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
					TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
					FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
					OptionUserDefined = new OptionUserDefined
					{
						MainCurrency = "VND",
						UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
						UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
						QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
						AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
						AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
						CoefficientDecimalDigits = "0",
						ExchangRateDecimalDigits = "0",
					},
					IsTicket = false,
					IsInvoiceCalculatingMachine = isInvoiceMTT,
					IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
					TaxReductionType = invoice.TaxReductionType,
					CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
					CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
					CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
					CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
					CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
					CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
					CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
					CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
					CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
					CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
					CheckIn = checkIn + TimeFix,
					CheckOut = checkOut + TimeFix,
					RoomNo = roomCode,
					ServiceFeeRate = 0,
					ServiceAmount = invoice.ServiceCharge,
					IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
					SellerShopCode = invoice.UnitCode,
					SellerShopName = invoice.UnitName,

					ReferenceType = referenceType,
					OrgInvoiceType = orgInvoiceType,
					OrgInvTemplateNo = orgInvTemplateNo,
					OrgInvSeries = orgInvSeries,
					OrgInvNo = orgInvNo,
					OrgInvDate = orgInvDate
				};

				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<Object> ConvertGetXMLInvoiceRequest(MisaMappingContext models)
		{
			try
			{
				CreateInvoiceRequest invoiceRequest = models.Data as CreateInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);
				List<Object> result = new List<Object>
				{
					new
						{
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = "Hóa đơn giá trị gia tăng",
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
							BuyerLegalName = invoice.CompanyName,
							BuyerTaxCode = invoice.CustomerTax,
							BuyerAddress = invoice.CustomerAddress,
							BuyerFullName = (invoice.CustomerName ?? "").Trim(),
							BuyerBankAccount = invoice.BankAccount,
							BuyerBankName = invoice.BankName,
							BuyerPhoneNumber = invoice.CustomerPhone,
							BuyerEmail = invoice.CustomerEmail,

							BuyerIDNumber = invoice.IdentityCard,
							BuyerPassport = invoice.PassportNum,
							BuyerBudgetCode = invoice.BudgetCode,

							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = invoice.BeforeTaxAmount,
							TotalAmountWithoutVAT = invoice.BeforeTaxAmount * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
							TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
							FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = false,
							SellerShopCode = invoice.UnitCode,
							SellerShopName = invoice.UnitName,
						},
				};
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceMTTRequest> ConvertCreateAndPublishInvoiceMTTRequest(MisaMappingContext models)
		{
			try
			{
				CreateInvoiceRequest invoiceRequest = models.Data as CreateInvoiceRequest;
				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				Invoice invoice = invoiceRequest.Invoice;
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

				if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
				{
					invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
				}
				CreateAndPublishInvoiceMTTRequest itemInvoice = new CreateAndPublishInvoiceMTTRequest
				{
					OrgInvoiceData = new OriginalInvoiceDataNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						InvSeries = invoice.Serial,
						InvoiceName = GetInvoiceName(invoice.FormNo, true),
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						CurrencyCode = invoice.CurrencyCode,
						ExchangeRate = invoice.ExchangeRate,
						PaymentMethodName = invoice.PaymentMethod,
						BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
						BuyerLegalName = invoice.CompanyName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddress = invoice.CustomerAddress,
						BuyerFullName = (invoice.CustomerName ?? "").Trim(),
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
						TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
						TotalDiscountAmount = totalDiscount,
						TotalDiscountAmountOC = totalDiscount,
						TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
						TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
						TotalVATAmountOC = invoice.TaxAmount ?? 0,
						TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
						TotalAmountOC = invoice.AfterTaxAmount,
						TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
						TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
						OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
						TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
						FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
						OptionUserDefined = new OptionUserDefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
							AmountDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
							AmountOCDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
							CoefficientDecimalDigits = "0",
							ExchangRateDecimalDigits = "0",
						},
						IsTicket = false,
						IsInvoiceCalculatingMachine = true,
						IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
						TaxReductionType = invoice.TaxReductionType,
						CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
						CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
						CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
						CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
						CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
						CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
						CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
						CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
						CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
						CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
						ServiceFeeRate = 0,
						ServiceAmount = invoice.ServiceCharge,
						IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					},
					IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
					ReceiverEmail = invoice.CustomerEmail,
					ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",
				};
				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								itemInvoice.OrgInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								itemInvoice.OrgInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								itemInvoice.OrgInvoiceData.RoomNo = itemMap.stringValue;
							}
						}
					}
				}

				List<CreateAndPublishInvoiceMTTRequest> result = new List<CreateAndPublishInvoiceMTTRequest> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceMTTRequest> ConvertCreateAndPublishInvoiceMTTMultiRequest(MisaMappingContext models)
		{
			try
			{
				var result = new List<CreateAndPublishInvoiceMTTRequest>();
				var invoiceRequestMulti = models.Data as List<CreateInvoiceRequest>;

				foreach (var model in invoiceRequestMulti)
				{
					var invoiceRequest = model;
					List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
					Invoice invoice = invoiceRequest.Invoice;
					List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
					decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
					decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
					decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

					if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
					{
						invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
					}
					CreateAndPublishInvoiceMTTRequest itemInvoice = new CreateAndPublishInvoiceMTTRequest
					{
						OrgInvoiceData = new OriginalInvoiceDataNormal
						{
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = GetInvoiceName(invoice.FormNo, true),
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
							BuyerLegalName = invoice.CompanyName,
							BuyerTaxCode = invoice.CustomerTax,
							BuyerAddress = invoice.CustomerAddress,
							BuyerFullName = (invoice.CustomerName ?? "").Trim(),
							BuyerBankAccount = invoice.BankAccount,
							BuyerBankName = invoice.BankName,
							BuyerPhoneNumber = invoice.CustomerPhone,
							BuyerEmail = invoice.CustomerEmail,

							BuyerIDNumber = invoice.IdentityCard,
							BuyerPassport = invoice.PassportNum,
							BuyerBudgetCode = invoice.BudgetCode,

							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
							TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
							TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
							FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = false,
							IsInvoiceCalculatingMachine = true,
							IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
							TaxReductionType = invoice.TaxReductionType,
							CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
							CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
							CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
							CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
							CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
							CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
							CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
							CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
							CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
							CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
							ServiceFeeRate = 0,
							ServiceAmount = invoice.ServiceCharge,
							IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
							SellerShopCode = invoice.UnitCode,
							SellerShopName = invoice.UnitName,
						},
						IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
						ReceiverEmail = invoice.CustomerEmail,
						ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",
					};
					if (extraDataMap != null && extraDataMap.Any())
					{
						if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
						{
							extraDataMapPMS = invoiceRequest.ExtraData
								.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
								{
									keyTag = e.ThirdPartyCode,
									valueType = e.ThirdPartyDataType,
									keyLabel = e.ThirdPartyLabel,
									stringValue = p.StringValue,
								})
								.ToList();
							foreach (MisaMetadata itemMap in extraDataMapPMS)
							{
								if (itemMap.keyTag == "CheckIn")
								{
									itemInvoice.OrgInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "CheckOut")
								{
									itemInvoice.OrgInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "RoomNo")
								{
									itemInvoice.OrgInvoiceData.RoomNo = itemMap.stringValue;
								}
							}
						}
					}

					result.Add(itemInvoice);
				}
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceRequestNormal> ConvertCreateAndPublishInvoiceMTTHSMRequest(MisaMappingContext models)
		{
			try
			{
				CreateInvoiceRequest invoiceRequest = models.Data as CreateInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				OriginalInvoiceDataNormal invoiceDataMap = new OriginalInvoiceDataNormal();

				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								invoiceDataMap.CheckIn = itemMap.stringValue + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								invoiceDataMap.CheckOut = itemMap.stringValue + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								invoiceDataMap.RoomNo = itemMap.stringValue;
							}
						}
					}
				}
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

				if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
				{
					invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
				}

				CreateAndPublishInvoiceRequestNormal itemInvoice = new CreateAndPublishInvoiceRequestNormal
				{
					RefID = invoice.InvoiceId.ToString(),
					OriginalInvoiceData = new OriginalInvoiceDataNormal
					{
						IsInvoiceCalculatingMachine = true,
						RefID = invoice.InvoiceId.ToString(),
						InvSeries = invoice.Serial,
						InvoiceName = GetInvoiceName(invoice.FormNo, false),
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						CurrencyCode = invoice.CurrencyCode,
						ExchangeRate = invoice.ExchangeRate,
						PaymentMethodName = invoice.PaymentMethod,
						BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
						BuyerLegalName = invoice.CompanyName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddress = invoice.CustomerAddress,
						BuyerFullName = (invoice.CustomerName ?? "").Trim(),
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
						TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
						TotalDiscountAmount = totalDiscount,
						TotalDiscountAmountOC = totalDiscount,
						TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
						TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
						TotalVATAmountOC = invoice.TaxAmount ?? 0,
						TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
						TotalAmountOC = invoice.AfterTaxAmount,
						TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
						TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
						OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
						TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
						FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
						OptionUserDefined = new OptionUserDefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
							AmountDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
							AmountOCDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
							CoefficientDecimalDigits = "0",
							ExchangRateDecimalDigits = "0",
						},
						IsTicket = false,

						TrsArrival = null,
						TrsDeparture = null,
						CheckIn = null,
						IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
						TaxReductionType = invoice.TaxReductionType,
						CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
						CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
						CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
						CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
						CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
						CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
						CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
						CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
						CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
						CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
						ServiceFeeRate = 0,
						ServiceAmount = invoice.ServiceCharge,
						IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					},
					IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
					ReceiverEmail = invoice.CustomerEmail,
					ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",

				};

				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								itemInvoice.OriginalInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								itemInvoice.OriginalInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								itemInvoice.OriginalInvoiceData.RoomNo = itemMap.stringValue;
							}
						}
					}
				}

				List<CreateAndPublishInvoiceRequestNormal> result = new List<CreateAndPublishInvoiceRequestNormal> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceRequestNormal> ConvertCreateAndPublishInvoiceMTTHSMMultiRequest(MisaMappingContext models)
		{
			try
			{
				var result = new List<CreateAndPublishInvoiceRequestNormal>();
				var invoiceRequestMulti = models.Data as List<CreateInvoiceRequest>;

				foreach (var model in invoiceRequestMulti)
				{
					var invoiceRequest = model;
					Invoice invoice = invoiceRequest.Invoice;
					List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
					List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
					OriginalInvoiceDataNormal invoiceDataMap = new OriginalInvoiceDataNormal();

					if (extraDataMap != null && extraDataMap.Any())
					{
						if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
						{
							extraDataMapPMS = invoiceRequest.ExtraData
								.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
								{
									keyTag = e.ThirdPartyCode,
									valueType = e.ThirdPartyDataType,
									keyLabel = e.ThirdPartyLabel,
									stringValue = p.StringValue,
								})
								.ToList();
							foreach (MisaMetadata itemMap in extraDataMapPMS)
							{
								if (itemMap.keyTag == "CheckIn")
								{
									invoiceDataMap.CheckIn = itemMap.stringValue + TimeFix;
								}
								if (itemMap.keyTag == "CheckOut")
								{
									invoiceDataMap.CheckOut = itemMap.stringValue + TimeFix;
								}
								if (itemMap.keyTag == "RoomNo")
								{
									invoiceDataMap.RoomNo = itemMap.stringValue;
								}
							}
						}
					}
					decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
					decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
					decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

					if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
					{
						invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
					}

					CreateAndPublishInvoiceRequestNormal itemInvoice = new CreateAndPublishInvoiceRequestNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						OriginalInvoiceData = new OriginalInvoiceDataNormal
						{
							IsInvoiceCalculatingMachine = true,
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = GetInvoiceName(invoice.FormNo, false),
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
							BuyerLegalName = invoice.CompanyName,
							BuyerTaxCode = invoice.CustomerTax,
							BuyerAddress = invoice.CustomerAddress,
							BuyerFullName = (invoice.CustomerName ?? "").Trim(),
							BuyerBankAccount = invoice.BankAccount,
							BuyerBankName = invoice.BankName,
							BuyerPhoneNumber = invoice.CustomerPhone,
							BuyerEmail = invoice.CustomerEmail,

							BuyerIDNumber = invoice.IdentityCard,
							BuyerPassport = invoice.PassportNum,
							BuyerBudgetCode = invoice.BudgetCode,

							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
							TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
							TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
							FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = false,

							TrsArrival = null,
							TrsDeparture = null,
							CheckIn = null,
							IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
							TaxReductionType = invoice.TaxReductionType,
							CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
							CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
							CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
							CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
							CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
							CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
							CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
							CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
							CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
							CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
							ServiceFeeRate = 0,
							ServiceAmount = invoice.ServiceCharge,
							IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
							SellerShopCode = invoice.UnitCode,
							SellerShopName = invoice.UnitName,
						},
						IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
						ReceiverEmail = invoice.CustomerEmail,
						ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",

					};

					if (extraDataMap != null && extraDataMap.Any())
					{
						if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
						{
							extraDataMapPMS = invoiceRequest.ExtraData
								.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
								{
									keyTag = e.ThirdPartyCode,
									valueType = e.ThirdPartyDataType,
									keyLabel = e.ThirdPartyLabel,
									stringValue = p.StringValue,
								})
								.ToList();
							foreach (MisaMetadata itemMap in extraDataMapPMS)
							{
								if (itemMap.keyTag == "CheckIn")
								{
									itemInvoice.OriginalInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "CheckOut")
								{
									itemInvoice.OriginalInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "RoomNo")
								{
									itemInvoice.OriginalInvoiceData.RoomNo = itemMap.stringValue;
								}
							}
						}
					}

					result.Add(itemInvoice);
				}
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceRequestNormal> ConvertCreateAndPublishInvoiceHSMRequest(MisaMappingContext models)
		{
			try
			{
				CreateInvoiceRequest invoiceRequest = models.Data as CreateInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				OriginalInvoiceDataNormal invoiceDataMap = new OriginalInvoiceDataNormal();

				if (extraDataMap != null && extraDataMap.Any())
				{

					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								invoiceDataMap.CheckIn = itemMap.stringValue + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								invoiceDataMap.CheckOut = itemMap.stringValue + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								invoiceDataMap.RoomNo = itemMap.stringValue;
							}
						}
					}
					if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
					{
						invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
					}

				}
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);
				CreateAndPublishInvoiceRequestNormal itemInvoice = new CreateAndPublishInvoiceRequestNormal
				{
					RefID = invoice.InvoiceId.ToString(),
					OriginalInvoiceData = new OriginalInvoiceDataNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						InvSeries = invoice.Serial,
						InvoiceName = GetInvoiceName(invoice.FormNo, false),
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						CurrencyCode = invoice.CurrencyCode,
						ExchangeRate = invoice.ExchangeRate,
						PaymentMethodName = invoice.PaymentMethod,
						BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
						BuyerLegalName = invoice.CompanyName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddress = invoice.CustomerAddress,
						BuyerFullName = (invoice.CustomerName ?? "").Trim(),
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
						TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
						TotalDiscountAmount = totalDiscount,
						TotalDiscountAmountOC = totalDiscount,
						TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
						TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
						TotalVATAmountOC = invoice.TaxAmount ?? 0,
						TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
						TotalAmountOC = invoice.AfterTaxAmount,
						TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
						TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
						OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
						TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
						FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
						OptionUserDefined = new OptionUserDefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
							AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							CoefficientDecimalDigits = "0",
							ExchangRateDecimalDigits = "0",
						},
						IsTicket = false,
						IsInvoiceCalculatingMachine = false,

						TrsArrival = null,
						TrsDeparture = null,
						CheckIn = null,
						IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
						TaxReductionType = invoice.TaxReductionType,
						CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
						CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
						CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
						CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
						CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
						CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
						CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
						CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
						CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
						CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
						ServiceFeeRate = 0,
						ServiceAmount = invoice.ServiceCharge,
						IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					},
					IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
					ReceiverEmail = invoice.CustomerEmail,
					ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",

				};

				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								itemInvoice.OriginalInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								itemInvoice.OriginalInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								itemInvoice.OriginalInvoiceData.RoomNo = itemMap.stringValue;
							}
						}
					}
				}

				List<CreateAndPublishInvoiceRequestNormal> result = new List<CreateAndPublishInvoiceRequestNormal> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceRequestNormal> ConvertCreateAndPublishInvoiceHSMMultiRequest(MisaMappingContext models)
		{
			try
			{
				var result = new List<CreateAndPublishInvoiceRequestNormal>();
				var invoiceRequestMulti = models.Data as List<CreateInvoiceRequest>;

				foreach (var model in invoiceRequestMulti)
				{
					var invoiceRequest = model;
					Invoice invoice = invoiceRequest.Invoice;
					List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
					List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
					OriginalInvoiceDataNormal invoiceDataMap = new OriginalInvoiceDataNormal();

					if (extraDataMap != null && extraDataMap.Any())
					{

						if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
						{
							extraDataMapPMS = invoiceRequest.ExtraData
								.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
								{
									keyTag = e.ThirdPartyCode,
									valueType = e.ThirdPartyDataType,
									keyLabel = e.ThirdPartyLabel,
									stringValue = p.StringValue,
								})
								.ToList();
							foreach (MisaMetadata itemMap in extraDataMapPMS)
							{
								if (itemMap.keyTag == "CheckIn")
								{
									invoiceDataMap.CheckIn = itemMap.stringValue + TimeFix;
								}
								if (itemMap.keyTag == "CheckOut")
								{
									invoiceDataMap.CheckOut = itemMap.stringValue + TimeFix;
								}
								if (itemMap.keyTag == "RoomNo")
								{
									invoiceDataMap.RoomNo = itemMap.stringValue;
								}
							}
						}
						if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
						{
							invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
						}

					}
					decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
					decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
					decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);
					CreateAndPublishInvoiceRequestNormal itemInvoice = new CreateAndPublishInvoiceRequestNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						OriginalInvoiceData = new OriginalInvoiceDataNormal
						{
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = GetInvoiceName(invoice.FormNo, false),
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
							BuyerLegalName = invoice.CompanyName,
							BuyerTaxCode = invoice.CustomerTax,
							BuyerAddress = invoice.CustomerAddress,
							BuyerFullName = (invoice.CustomerName ?? "").Trim(),
							BuyerBankAccount = invoice.BankAccount,
							BuyerBankName = invoice.BankName,
							BuyerPhoneNumber = invoice.CustomerPhone,
							BuyerEmail = invoice.CustomerEmail,

							BuyerIDNumber = invoice.IdentityCard,
							BuyerPassport = invoice.PassportNum,
							BuyerBudgetCode = invoice.BudgetCode,

							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
							TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
							TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
							FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = false,
							IsInvoiceCalculatingMachine = false,

							TrsArrival = null,
							TrsDeparture = null,
							CheckIn = null,
							IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
							TaxReductionType = invoice.TaxReductionType,
							CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
							CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
							CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
							CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
							CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
							CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
							CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
							CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
							CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
							CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
							ServiceFeeRate = 0,
							ServiceAmount = invoice.ServiceCharge,
							IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
							SellerShopCode = invoice.UnitCode,
							SellerShopName = invoice.UnitName,
						},
						IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
						ReceiverEmail = invoice.CustomerEmail,
						ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",

					};

					if (extraDataMap != null && extraDataMap.Any())
					{
						if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
						{
							extraDataMapPMS = invoiceRequest.ExtraData
								.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
								{
									keyTag = e.ThirdPartyCode,
									valueType = e.ThirdPartyDataType,
									keyLabel = e.ThirdPartyLabel,
									stringValue = p.StringValue,
								})
								.ToList();
							foreach (MisaMetadata itemMap in extraDataMapPMS)
							{
								if (itemMap.keyTag == "CheckIn")
								{
									itemInvoice.OriginalInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "CheckOut")
								{
									itemInvoice.OriginalInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
								}
								if (itemMap.keyTag == "RoomNo")
								{
									itemInvoice.OriginalInvoiceData.RoomNo = itemMap.stringValue;
								}
							}
						}
					}

					result.Add(itemInvoice);
				}

				//List<CreateAndPublishInvoiceRequestNormal> result = new List<CreateAndPublishInvoiceRequestNormal> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceRequestNormal> ConvertAdjustAndReplaceHSMRequest(MisaMappingContext models)
		{
			try
			{
				AdjustOrReplaceInvoiceRequest invoiceRequest = models.Data as AdjustOrReplaceInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				Invoice invoiceOld = invoiceRequest.OriginalInvoice;
				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);
				if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
				{
					invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
				}
				CreateAndPublishInvoiceRequestNormal itemInvoice = new CreateAndPublishInvoiceRequestNormal
				{
					RefID = invoice.InvoiceId.ToString(),
					OriginalInvoiceData = new OriginalInvoiceDataNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						InvSeries = invoice.Serial,
						InvoiceName = "Hóa đơn giá trị gia tăng",
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						CurrencyCode = invoice.CurrencyCode,
						ExchangeRate = invoice.ExchangeRate,
						PaymentMethodName = invoice.PaymentMethod,
						BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
						BuyerLegalName = invoice.CompanyName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddress = invoice.CustomerAddress,
						BuyerFullName = (invoice.CustomerName ?? "").Trim(),
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
						TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
						TotalDiscountAmount = totalDiscount,
						TotalDiscountAmountOC = totalDiscount,
						TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
						TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
						TotalVATAmountOC = invoice.TaxAmount ?? 0,
						TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
						TotalAmountOC = invoice.AfterTaxAmount,
						TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
						TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
						OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
						TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
						FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),

						OptionUserDefined = new OptionUserDefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
							AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							CoefficientDecimalDigits = "0",
							ExchangRateDecimalDigits = "0",
						},
						IsTicket = false,
						IsInvoiceCalculatingMachine = false,

						ReferenceType = models.Action == (int)InvoiceAction.Replace ? 1 : 2, // 1: thay thế; 2: điều chỉnh
						OrgInvoiceType = 1, // Mặc định lấy theo nghị định 123, tương ứng với số 1
						OrgInvTemplateNo = invoiceOld.FormNo,
						OrgInvSeries = RemoveFirstCharSerial(invoiceOld.Serial),
						OrgInvNo = invoiceRequest.OriginalProviderInvoiceNumber,
						OrgInvDate = DateTime.ParseExact(invoiceOld.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
						TaxReductionType = invoice.TaxReductionType,
						CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
						CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
						CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
						CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
						CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
						CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
						CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
						CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
						CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
						CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
						ServiceFeeRate = 0,
						ServiceAmount = invoice.ServiceCharge,
						IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					}
				};

				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								itemInvoice.OriginalInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								itemInvoice.OriginalInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								itemInvoice.OriginalInvoiceData.RoomNo = itemMap.stringValue;
							}
						}
					}
				}

				List<CreateAndPublishInvoiceRequestNormal> result = new List<CreateAndPublishInvoiceRequestNormal> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<Object> ConvertCreateAndPublishInvoiceXMLRequest(MisaMappingContext models)
		{
			try
			{
				PublishSignedXmlRequest invoiceRequest = models.Data as PublishSignedXmlRequest;
				List<Object> result = new List<Object>
				{
					new
						{
							RefID = invoiceRequest.InvoiceId.ToString(),
							TransactionID = invoiceRequest.SearchCode,
							InvoiceData = invoiceRequest.InvoiceData,
							IsSendEmail = false,
							ReceiverEmail = "",
							ReceiverName = "",
							IsInvoiceSummary = false,
						},
				};
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<Object> ConvertAdjustAndReplaceUsbTokenRequest(MisaMappingContext models)
		{
			try
			{
				AdjustOrReplaceInvoiceRequest invoiceRequest = models.Data as AdjustOrReplaceInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				Invoice invoiceOld = invoiceRequest.OriginalInvoice;
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);
				List<Object> result = new List<Object>
				{
					new
						{
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = "Hóa đơn giá trị gia tăng",
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
							BuyerLegalName = invoice.CompanyName,
							BuyerTaxCode = invoice.CustomerTax,
							BuyerAddress = invoice.CustomerAddress,
							BuyerFullName = (invoice.CustomerName ?? "").Trim(),
							BuyerBankAccount = invoice.BankAccount,
							BuyerBankName = invoice.BankName,
							BuyerPhoneNumber = invoice.CustomerPhone,
							BuyerEmail = invoice.CustomerEmail,

							BuyerIDNumber = invoice.IdentityCard,
							BuyerPassport = invoice.PassportNum,
							BuyerBudgetCode = invoice.BudgetCode,

							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = invoice.BeforeTaxAmount,
							TotalAmountWithoutVAT = invoice.BeforeTaxAmount * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,

							TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
							FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = false,

							ReferenceType = models.Action == (int)InvoiceAction.Replace ? 1 : 2, // 1: thay thế; 2: điều chỉnh
							OrgInvoiceType = 1, // Mặc định lấy theo nghị định 123, tương ứng với số 1
							OrgInvTemplateNo = invoiceOld.FormNo,
							OrgInvSeries = RemoveFirstCharSerial(invoiceOld.Serial),
							OrgInvNo = invoiceRequest.OriginalProviderInvoiceNumber,
							OrgInvDate =  DateTime.ParseExact(invoiceOld.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
							SellerShopCode = invoice.UnitCode,
							SellerShopName = invoice.UnitName,
						},
				};
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static List<CreateAndPublishInvoiceMTTRequest> ConvertAdjustAndReplaceMTTRequest(MisaMappingContext models)
		{
			try
			{
				AdjustOrReplaceInvoiceRequest invoiceRequest = models.Data as AdjustOrReplaceInvoiceRequest;
				Invoice invoice = invoiceRequest.Invoice;
				Invoice invoiceOld = invoiceRequest.OriginalInvoice;
				List<ExtraFieldMapping> extraDataMap = models.ExtraData.ToList();
				List<MisaMetadata> extraDataMapPMS = new List<MisaMetadata>();
				decimal totalDiscount = GetTotalDiscountAmount(invoiceRequest.Details);
				decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(invoiceRequest.Details);
				decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(invoiceRequest.Details);

				if (HaveAddedExtraDataMapCustom(extraDataMap, KeyExtraDataNotice) && !string.IsNullOrEmpty(invoice.Notice))
				{
					invoiceRequest.ExtraData.Add(CreateExtraDataCustom(KeyExtraDataNotice, invoice.Notice));
				}

				CreateAndPublishInvoiceMTTRequest itemInvoice = new CreateAndPublishInvoiceMTTRequest
				{
					OrgInvoiceData = new OriginalInvoiceDataNormal
					{
						RefID = invoice.InvoiceId.ToString(),
						InvSeries = invoice.Serial,
						InvoiceName = "Hóa đơn giá trị gia tăng khởi tạo từ máy tính tiền",
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						CurrencyCode = invoice.CurrencyCode,
						ExchangeRate = invoice.ExchangeRate,
						PaymentMethodName = invoice.PaymentMethod,
						BuyerCode = GetBuyerCode(invoice, invoiceRequest.ExtraData),
						BuyerLegalName = invoice.CompanyName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddress = invoice.CustomerAddress,
						BuyerFullName = (invoice.CustomerName ?? "").Trim(),
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
						TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
						TotalDiscountAmount = totalDiscount,
						TotalDiscountAmountOC = totalDiscount,
						TotalAmountWithoutVATOC = invoice.BeforeTaxAmount ?? 0,
						TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
						TotalVATAmountOC = invoice.TaxAmount ?? 0,
						TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
						TotalAmountOC = invoice.AfterTaxAmount,
						TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
						TotalAmountInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
						OriginalInvoiceDetail = GetTicketDetail(invoiceRequest.Details, invoice.ExchangeRate, invoice.FormNo, invoiceRequest.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
						TaxRateInfo = GetTaxRateInfo(invoiceRequest.TaxSummaries, invoice.FormNo),
						FeeInfo = GetFeeInfo(invoiceRequest.ServiceSummaries),
						OptionUserDefined = new OptionUserDefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
							AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
							CoefficientDecimalDigits = "0",
							ExchangRateDecimalDigits = "0",
						},
						IsTicket = false,
						IsInvoiceCalculatingMachine = true,

						ReferenceType = models.Action == (int)InvoiceAction.Replace ? 1 : 2, // 1: thay thế; 2: điều chỉnh
						OrgInvoiceType = 1, // Mặc định lấy theo nghị định 123, tương ứng với số 1
						OrgInvTemplateNo = invoiceOld.FormNo,
						OrgInvSeries = RemoveFirstCharSerial(invoiceOld.Serial),
						OrgInvNo = invoiceRequest.OriginalProviderInvoiceNumber,
						OrgInvDate = DateTime.ParseExact(invoiceOld.InvoiceDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
						IsTaxReduction43 = invoice.IsTaxReduction43 != null ? invoice.IsTaxReduction43 : IsTaxReduction43(invoiceRequest.TaxSummaries, invoiceRequest.Details),
						TaxReductionType = invoice.TaxReductionType,
						CustomField1 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField1),
						CustomField2 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField2),
						CustomField3 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField3),
						CustomField4 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField4),
						CustomField5 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField5),
						CustomField6 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField6),
						CustomField7 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField7),
						CustomField8 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField8),
						CustomField9 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField9),
						CustomField10 = GetDataCustomField(extraDataMap, invoiceRequest.ExtraData, CustomField10),
						ServiceFeeRate = 0,
						ServiceAmount = invoice.ServiceCharge,
						IsHotelMultiVATRate = invoice.IsMultiVATRate ?? false,
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					}
				};

				if (extraDataMap != null && extraDataMap.Any())
				{
					if (invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any())
					{
						extraDataMapPMS = invoiceRequest.ExtraData
							.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new MisaMetadata
							{
								keyTag = e.ThirdPartyCode,
								valueType = e.ThirdPartyDataType,
								keyLabel = e.ThirdPartyLabel,
								stringValue = p.StringValue,
							})
							.ToList();
						foreach (MisaMetadata itemMap in extraDataMapPMS)
						{
							if (itemMap.keyTag == "CheckIn")
							{
								itemInvoice.OrgInvoiceData.CheckIn = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "CheckOut")
							{
								itemInvoice.OrgInvoiceData.CheckOut = ConvertDateFormat(itemMap.stringValue) + TimeFix;
							}
							if (itemMap.keyTag == "RoomNo")
							{
								itemInvoice.OrgInvoiceData.RoomNo = itemMap.stringValue;
							}
						}
					}
				}
				List<CreateAndPublishInvoiceMTTRequest> result = new List<CreateAndPublishInvoiceMTTRequest> { itemInvoice };
				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}
		public static string GetDataCustomField(List<ExtraFieldMapping> extraDataMap, List<ExtraField> extraData, string fieldName)
		{
			string result = "";
			List<string> rowCustomField = extraDataMap.Where(i => i.ThirdPartyCode == fieldName).Select(j => j.Code).ToList();
			if (rowCustomField.Any())
			{
				ExtraField itemMap = extraData.FirstOrDefault(i => rowCustomField.Contains(i.Code) && !String.IsNullOrEmpty(i.StringValue) && !String.IsNullOrWhiteSpace(i.StringValue));
				if (itemMap != null)
				{
					result = itemMap.StringValue;
				}
			}
			return result;
		}
		// Trong hóa đơn có mức thuế phí 8% thì cần đánh dấu IsTaxReduction43 bằng true
		public static bool IsTaxReduction43(List<TaxSummary> TaxSummarys, List<InvoiceDetail> Details)
		{
			return (TaxSummarys != null && TaxSummarys.Any(i => i.TaxRate == 8)) || (Details != null && Details.Any(i => i.TaxRate == 8));
		}

		private static List<FeeInfo> GetFeeInfo(List<ServiceSummary> serviceSummarys)
		{

			var result = new List<FeeInfo>();
			if (serviceSummarys == null) return result;
			foreach (var tax in serviceSummarys)
			{
				result.Add(new FeeInfo
				{
					FeeName = (int)tax.ServiceRate + "%",
					FeeAmountOC = tax.ServiceCharge,
				});
			}

			return result;
		}

		public static List<MisaTicketModel> ConvertCreateAndPublishTicketRequest(MisaMappingContext models)
		{
			try
			{
				var result = new List<MisaTicketModel>();
				var invoiceRequest = models.Data as List<CreateInvoiceRequest>;

				foreach (var model in invoiceRequest)
				{
					var invoice = model.Invoice;
					decimal totalDiscount = GetTotalDiscountAmount(model.Details);
					decimal totalAmountBeforeDiscount = GetTotalAmountBeforeDiscount(model.Details);
					decimal totalAmountWithoutVAT = GetTotalAmountWithoutVAT(model.Details);
					var item = new MisaTicketModel
					{
						RefID = invoice.InvoiceId.ToString(),
						OriginalInvoiceData = new OriginalInvoiceData
						{
							RefID = invoice.InvoiceId.ToString(),
							InvSeries = invoice.Serial,
							InvoiceName = "Hóa đơn giá trị gia tăng",
							InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null)
								.ToString("yyyy-MM-ddTHH:mm:sszzz"),
							CurrencyCode = invoice.CurrencyCode,
							ExchangeRate = invoice.ExchangeRate,
							PaymentMethodName = invoice.PaymentMethod,
							BuyerCode = GetBuyerCode(invoice, model.ExtraData),
							TotalSaleAmountOC = invoice.BeforeTaxAmount - totalDiscount,
							TotalSaleAmount = invoice.BeforeTaxAmount - totalDiscount * (invoice.ExchangeRate ?? 0),
							TotalDiscountAmount = totalDiscount,
							TotalDiscountAmountOC = totalDiscount,
							TotalAmountWithoutVATOC = (invoice.BeforeTaxAmount ?? 0),
							TotalAmountWithoutVAT = (invoice.BeforeTaxAmount ?? 0) * (invoice.ExchangeRate ?? 0),
							TotalVATAmountOC = invoice.TaxAmount ?? 0,
							TotalVATAmount = (invoice.TaxAmount ?? 0) * invoice.ExchangeRate,
							TotalAmountOC = invoice.AfterTaxAmount,
							TotalAmount = invoice.AfterTaxAmount * invoice.ExchangeRate,
							TotalAmountInWords =
								MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value),
							OriginalInvoiceDetail = GetTicketDetail(model.Details, invoice.ExchangeRate, invoice.FormNo, model.ServiceSummaries, invoice.IsMultiVATRate, invoice.PricePrecision ?? 0),
							TaxRateInfo = GetTaxRateInfo(model.TaxSummaries, invoice.FormNo),
							OptionUserDefined = new OptionUserDefined
							{
								MainCurrency = "VND",
								UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
								QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
								AmountDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								AmountOCDecimalDigits = invoice.AmountPrecision == null ? "0" : invoice.AmountPrecision.ToString(),
								CoefficientDecimalDigits = "0",
								ExchangRateDecimalDigits = "0",
							},
							IsTicket = true,
						},
						IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),
						ReceiverEmail = invoice.CustomerEmail,
						ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",
					};

					result.Add(item);
				}

				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		private static List<OriginalInvoiceDetail> GetTicketDetail(List<InvoiceDetail> details, decimal? exchangeRate, string formNo, List<ServiceSummary> serviceSummarys, bool? IsMultiVATRate, int PricePrecision)
		{
			var result = new List<OriginalInvoiceDetail>();
			for (int i = 0; i < details.Count; i++)
			{
				var item = details[i];
				result.Add(new OriginalInvoiceDetail
				{
					ItemType = 1,
					LineNumber = i + 1,
					SortOrder = i + 1,
					ItemName = item.ItemName,
					ItemCode = item.ItemCode,
					UnitName = item.UnitName,
					Quantity = item.Quantity,
					UnitPrice = ((item.ServiceCharge ?? 0) != 0)
						? Math.Round((((item.Quantity * item.Price) + item.ServiceCharge - item.DiscountAmount) ?? 0) / item.Quantity ?? 1, PricePrecision, MidpointRounding.AwayFromZero)
						: Math.Round((item.Price ?? 0), PricePrecision, MidpointRounding.AwayFromZero),
					UnitPriceAfterExciseVAT = (IsMultiVATRate ?? false) ? ((item.Quantity * item.Price) + item.ServiceCharge - item.DiscountAmount) : item.Price,
					DiscountRate = item.DiscountPercent ?? 0,
					DiscountAmountOC = Math.Round((item.DiscountAmount ?? 0), 0),
					DiscountAmount = Math.Round((item.DiscountAmount ?? 0), 0),
					AmountOC = Math.Round((item.Quantity ?? 0) * (item.Price ?? 0), MidpointRounding.AwayFromZero),
					Amount = Math.Round((item.Quantity ?? 0) * (item.Price ?? 0) * (exchangeRate ?? 0), MidpointRounding.AwayFromZero),
					AmountWithoutVATOC = item.BeforeTaxAmount,
					AmountWithoutVAT = item.BeforeTaxAmount,
					VATRateName = formNo == formNo_HDBH ? null : (item.TaxRate == null ? "" : Convert.ToInt32(item.TaxRate) + "%"),
					VATAmountOC = item.TaxAmount,
					VATAmount = item.TaxAmount,
					AmountAfterTax = item.AfterTaxAmount,
					ServiceFeeRate = item.ServiceCharge == 0 ? 0 : item.ServiceRate,
					ServiceAmount = item.ServiceCharge,
					ServiceAmountOC = item.ServiceCharge,
					CustomField2Detail = item.TransactionDate,
					ExciseTaxRate = item.ExciseTaxRate,
					ExciseTaxAmount = item.ExciseTaxCharge,
					ExciseTaxAmountOC = item.ExciseTaxCharge,
					CustomField1Detail = (item.Price ?? 0).ToString(),
					TaxReduction43Amount = item.TaxReduction43Amount ?? 0,
					TaxReduction43AmountOC = item.TaxReduction43AmountOC ?? 0,
					CustomField3Detail = (Math.Round(
							((item.Quantity * item.Price) + item.ServiceCharge - item.DiscountAmount) ?? 0, PricePrecision)).ToString(),
				});
			}
			return result;
		}

		private static List<Originalinvoicedetail> GetInvocieDetail(List<InvoiceDetail> details)
		{
			var result = new List<Originalinvoicedetail>();
			for (int i = 0; i < details.Count; i++)
			{
				var item = details[i];
				result.Add(new Originalinvoicedetail
				{
					LineNumber = i + 1,
					ItemCode = item.ItemName,
					ItemName = item.ItemName,
					UnitName = item.UnitName,
					Quantity = (int)item.Quantity.Value,
					Amount = item.AfterTaxAmount.Value,
					VatPercentage = item.TaxRate.Value,
					UnitPrice = item.Price.Value,
					VatAmount = item.TaxAmount.Value
				});
			}

			return result;
		}

		public static MisaCancelTicketRequestModel ConvertCancelTicketModel(MisaMappingContext model)
		{
			var cancelRequest = model.Data as VoidInvoiceRequest;
			return new MisaCancelTicketRequestModel
			{
				TransactionID = cancelRequest.TransactionId,
				InvNo = cancelRequest.ProviderInvoiceNumber,
				RefDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
				CancelReason = cancelRequest.Reason
			};
		}

		public static MisaCancelInvoiceRequestModel ConvertCancelInvoiceModel(MisaMappingContext model)
		{
			var cancelRequest = model.Data as VoidInvoiceRequest;
			return new MisaCancelInvoiceRequestModel
			{
				TransactionID = cancelRequest.TransactionId,
				RefNo = cancelRequest.ProviderInvoiceNumber,
				RefDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
				DeletedReason = cancelRequest.Reason
			};
		}

		public static List<string> ConvertDownloadPDFModel(MisaMappingContext model)
		{
			var request = model.Data as DownloadPdfRequest;
			return new List<string>() { request.TransactionId };
		}
		public static List<string> ConvertDownloadPDFMTTModel(MisaMappingContext model)
		{
			var request = model.Data as DownloadPdfRequest;
			return new List<string>() { request.SearchCode };
		}
		public static MisaCancelTicketRequestModel ConvertCancelMTTModel(MisaMappingContext model)
		{
			var cancelRequest = model.Data as VoidInvoiceRequest;
			return new MisaCancelTicketRequestModel
			{
				TransactionID = cancelRequest.SearchCode,
				InvNo = cancelRequest.ProviderInvoiceNumber,
				RefDate = DateTime.Now.ToString("yyyy-MM-dd"),
				CancelReason = cancelRequest.Reason
			};
		}
		private static List<TaxRateInfo> GetTaxRateInfo(List<TaxSummary> pmsTaxes, string formNo)
		{
			var result = new List<TaxRateInfo>();
			if (pmsTaxes == null) return result;
			List<TaxSummary> pmsTaxePostToThirdParty = formNo == formNo_HDBH ? pmsTaxes.Where(i => (i.TaxRate ?? 0) != 0).ToList() : pmsTaxes;
			foreach (var tax in pmsTaxePostToThirdParty)
			{
				result.Add(new TaxRateInfo
				{
					VATRateName = (int)tax.TaxRate + "%",
					VATAmountOC = tax.TaxAmount,
					AmountWithoutVATOC = tax.BeforeTaxAmount ?? 0
				});
			}

			return result;
		}

		public static MisaInvoiceRequestModel ConvertCreateAndPublishReceiptRequest(MisaMappingContext models,
			MisaConnectionInfo connection, InvFromSerialResponseModel serial)
		{
			try
			{
				var invoiceRequest = models.Data as List<CreateInvoiceRequest>;

				var result = new MisaInvoiceRequestModel
				{
					InvoiceDataList = new List<Invoicedatalist>(),
					userName = connection.Username2,
					passWord = connection.Password2
				};


				foreach (var model in invoiceRequest)
				{
					var invoice = model.Invoice;
					result.InvoiceDataList.Add(new Invoicedatalist
					{
						RefID = invoice.InvoiceId.ToString(),
						InvoiceType = serial.InvoiceType,
						TemplateCode = invoice.Serial,
						InvoiceSeries = serial.InvoiceSeries,
						CurrencyCode = invoice.CurrencyCode,
						AdjustmentType = 1,
						PaymentMethodName = invoice.PaymentMethod,
						SellerTaxCode = connection.TaxNumber,
						SellerAddressLine = connection.Address,
						SellerLegalName = connection.Name,
						BuyerDisplayName = invoice.CustomerName,
						BuyerTaxCode = invoice.CustomerTax,
						BuyerAddressLine = invoice.CustomerAddress,
						BuyerPhoneNumber = invoice.CustomerPhone,
						BuyerEmail = invoice.CustomerEmail,
						BuyerBankAccount = invoice.BankAccount,
						BuyerBankName = invoice.BankName,
						ExchangeRate = invoice.ExchangeRate.Value,
						VatPercentage = invoice.TaxRate.Value,
						TotalAmountWithoutVAT = invoice.BeforeTaxAmount.Value,
						TotalVATAmount = invoice.TaxAmount.Value,
						TotalAmountWithVAT = invoice.AfterTaxAmount.Value,
						TotalAmountWithVATInWords = MoneyToText.DocTienBangChu(invoice.AfterTaxAmount.Value),
						IsSendEmail = !string.IsNullOrEmpty(invoice.CustomerEmail),

						BuyerIDNumber = invoice.IdentityCard,
						BuyerPassport = invoice.PassportNum,
						BuyerBudgetCode = invoice.BudgetCode,

						ReceiverName = !string.IsNullOrEmpty(invoice.CustomerEmail) ? invoice.CustomerName : "",
						ReceiverEmail = invoice.CustomerEmail,
						InvDate = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null)
							.ToString("yyyy-MM-ddTHH:mm:sszzz"),
						AccountObjectCode = invoice.RefId,
						OriginalInvoiceDetail = GetInvocieDetail(model.Details),
						OptionUserDefined = new Optionuserdefined
						{
							MainCurrency = "VND",
							UnitPriceDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							UnitPriceOCDecimalDigits = invoice.PricePrecision == null ? "" : invoice.PricePrecision.ToString(),
							QuantityDecimalDigits = invoice.QuantityPrecision == null ? "" : invoice.QuantityPrecision.ToString(),
						},
						SellerShopCode = invoice.UnitCode,
						SellerShopName = invoice.UnitName,
					});
				}

				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static MisaSendCQTRequest ConvertSendCQTRequest(MisaMappingContext model)
		{
			MisaSendCQTRequest result = new MisaSendCQTRequest();
			DateTime now = DateTime.Now;
			long ticks = now.Ticks;

			result.TakeTime = ticks;
			result.AppID = model.Company.Username2;
			result.SignType = "HSM"; // Mặc định dùng HSM

			return result;
		}

		public static MisaGetStatusInvoiceByRefIDMTT ConverGetStatusInvoiceByRefIDMTTRequest(MisaMappingContext model)
		{
			MisaGetStatusInvoiceByRefIDMTT result = new MisaGetStatusInvoiceByRefIDMTT();
			SendToTaxAuthorityRequest data = model.Data as SendToTaxAuthorityRequest;
			result.ezInvoiceID = data.InvoiceIds;
			return result;
		}

		public static GetXMLInvoiceRequest ConvertGetXMLInvoiceHashRequest(MisaMappingContext model)
		{
			GetXMLInvoiceRequest result = new GetXMLInvoiceRequest();
			GetXmlHashRequest data = model.Data as GetXmlHashRequest;
			DateTime now = DateTime.Now;
			long ticks = now.Ticks;

			result.TakeTime = ticks;
			result.TaxCode = model.Company.TaxNumber;
			result.X509RawData = data.CertBase64;

			return result;
		}

		public static SendInvoiceToCQTRequest ConvertSendInvoiceCQTUSBTokenRequest(MisaMappingContext model)
		{
			SendInvoiceToCQTRequest result = new SendInvoiceToCQTRequest();
			SendSignedTaxAuthorityRequest data = model.Data as SendSignedTaxAuthorityRequest;
			DateTime now = DateTime.Now;
			long ticks = now.Ticks;

			result.SignatureValue = data.SignatureValue;
			result.MessageCode = data.MessageCode;

			return result;
		}

		/// <summary>
		/// Tạm dừng thi công do khách không cần nữa
		/// </summary>
		/// <param name="models"></param>
		/// <returns></returns>
		public static string GetInvoiceName(string formNo, bool isMTT)
		{
			string result = "";
			if (isMTT)
			{
				if (formNo == formNo_GTGT) result = "Hoá đơn GTGT khởi tạo từ máy tính tiền";
				if (formNo == formNo_HDBH) result = "Hoá đơn bán hàng khởi tạo từ máy tính tiền";
			}
			else
			{
				if (formNo == formNo_GTGT) result = "Hoá đơn giá trị gia tăng";
				if (formNo == formNo_HDBH) result = "Hoá đơn bán hàng";
			}
			return result;
		}
		public static string ConvertDateFormat(string date)
		{
			try
			{
				DateTime parsedDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
				string formattedDate = parsedDate.ToString("MM/dd/yyyy");
				return formattedDate;
			}
			catch
			{
				return "";
			}
		}
		public static bool IsHotelMultiVATRate(List<TaxSummary> pmsTaxes)
		{
			bool result = false;
			if (pmsTaxes != null && pmsTaxes.Any())
			{
				result = pmsTaxes.Count > 0;
			}
			return result;
		}
		private static decimal GetTotalDiscountAmount(List<InvoiceDetail> details)
		{
			decimal result = details.Sum(i => (i.DiscountAmount ?? 0));
			return result;
		}
		private static decimal GetTotalAmountBeforeDiscount(List<InvoiceDetail> details)
		{
			decimal result = details.Sum(i => roundMoney((i.Quantity ?? 0) * (i.Price ?? 0)));
			return result;
		}

		private static decimal GetTotalAmountWithoutVAT(List<InvoiceDetail> details)
		{
			decimal result = details
				.Sum(item =>
					(
						((item.Quantity ?? 0) * (item.Price ?? 0))
						+ (item.ServiceCharge ?? 0)
						+ (item.ExciseTaxCharge ?? 0)
						- (item.DiscountAmount ?? 0)
					)
				);
			return result;
		}

		private static decimal roundMoney(decimal num, int? noOfDec = 0)
		{
			return Math.Round(num, noOfDec ?? 0, MidpointRounding.AwayFromZero);
		}
		/// <summary>
		/// Dùng để xóa ký tự đầu trong Ký hiệu
		/// VD: 1C25MTT => C25MTT, 1C25TYY => C25TYY
		/// Lý do: Nhà cung cấp yêu cầu khi Điều chỉnh/Thay thế hóa đơn thì trường OrgInvSeries không được để 1C25MTT mà phải để là C25MTT
		/// Đã trao đổi để nhà cung cấp tự xử lý nhưng họ báo tự cắt chuỗi thủ công
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static string RemoveFirstCharSerial(string input)
		{
			return input.Length == 7 ? input.Substring(1) : input;
		}

		private static ExtraField CreateExtraDataCustom(string code, string value)
		{
			return new ExtraField
			{
				Code = code,
				StringValue = value,
				DataType = "string"
			};
		}
		private static bool HaveAddedExtraDataMapCustom(List<ExtraFieldMapping> extraDataMap, string code)
		{
			return extraDataMap.Any(e => e.Code == code);
		}
		/// <summary>
		/// Nếu RefID trống thì lấy thông tin từ ExtraData có code là "TACode"
		/// </summary>
		private static string GetBuyerCode(Invoice invoice, List<ExtraField> extraData)
		{
			if (!string.IsNullOrWhiteSpace(invoice.RefId))
			{
				return invoice.RefId;
			}
			return extraData?.Find(i => i.Code == KeyExtraDataTACompany)?.StringValue ?? "";
		}
	}
}
