#nullable disable
#pragma warning disable CS1998, CS0219, CS0414, CS8981
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using Invoice = eInvoice.Providers.Fast.Wire.Invoice;
using metadata = eInvoice.Providers.Fast.Mapping.Metadata;
using eInvoice.Providers.Fast.Wire;
namespace eInvoice.Providers.Fast.Mapping
{
	internal static class FastRequestMapper
	{
		private const string ApiCreateAndPublish = "api/command/ExecuteCommand";
		private const string ApiSoapCreateAndPublish = "AppService/FastEInvoice.PortalService.asmx";
		private const string Chrw254 = "þ";
		private const int FastActionGeneral = 0;
		private const int FastActionPreview = 600;

		public enum FAST_METHOD
		{
			CREATE_AND_PUBLIC = 7312,
			ADJUST_INVOICE = 7322,
			REPLACE_INVOICE = 7352,
			VOID_INVOICE = 7330,
			GET_PDF = 7380,
			GET_PDF_DRAFT = 7381,
			PREVIEW_INVOICE = 7312,
			DRAFT_INVOICE = 7311,
		}

		public enum FAST_METHOD_GTGT
		{
			CREATE_AND_PUBLIC = 310,
			ADJUST_INVOICE = 320,
			REPLACE_INVOICE = 350,
			VOID_INVOICE = 330,
			GET_PDF = 380,
			PREVIEW_INVOICE = 312,
		}
		#region HDDT Máy tính tiền
		public static FastApiRequest ConvertCreateAndPublishInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, (int)FAST_METHOD.CREATE_AND_PUBLIC);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, extraMasterFields, pmsBodyRequest.TaxSummaries, model);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		public static FastApiRequest ConvertCreateAndPublishMultiInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			List<CreateInvoiceRequest> pmsBodyRequest = model.Data as List<CreateInvoiceRequest>;

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, (int)FAST_METHOD.CREATE_AND_PUBLIC);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;
			string bodyResult = BuildRequestMultiInvoiceBody(pmsBodyRequest, extraDataMapConfig);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		public static FastApiRequest ConvertBase64PDFInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			DownloadPdfRequest pmsDownloadPDFRequest = model.Data as DownloadPdfRequest;

			bool isDraft = model.Action == (int)INVOICE_ACTION.DOWNLOAD_PDF_INV_DRAFT;
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			int methodID = isDraft ? (int)FAST_METHOD.GET_PDF_DRAFT : (int)FAST_METHOD.GET_PDF;
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, methodID);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);


			string jsonBody = string.Empty;
			if (isDraft)
			{
				FastBodyGetPDFDraftBase64 bodyGetPDFBase64 = new FastBodyGetPDFDraftBase64();
				bodyGetPDFBase64.Key = pmsDownloadPDFRequest.InvoiceId;
				bodyGetPDFBase64.InvoiceDate = pmsDownloadPDFRequest.InvoiceDate;
				jsonBody = JsonConvert.SerializeObject(bodyGetPDFBase64);
			}
			else
			{
				FastBodyGetPDFBase64 bodyGetPDFBase64 = new FastBodyGetPDFBase64();
				bodyGetPDFBase64.keySearch = pmsDownloadPDFRequest.SearchCode;
				jsonBody = JsonConvert.SerializeObject(bodyGetPDFBase64);
			}


			return CreateFastApiRequest(jsonBody, headers, url);
		}

		public static FastApiRequest ConvertVoidInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			bool IsSerialMTT = model.IsSerialMTT;

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, IsSerialMTT ? (int)FAST_METHOD.VOID_INVOICE : (int)FAST_METHOD_GTGT.VOID_INVOICE);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			VoidInvoiceRequest pmsData = model.Data as VoidInvoiceRequest;
			List<FastBodyVoidInvoiceDetail> itemBodyFast = new List<FastBodyVoidInvoiceDetail>
			{
				new FastBodyVoidInvoiceDetail
				{
					keySearch = pmsData.SearchCode,
					reason = pmsData.Reason,
				}
			};

			FastBodyVoidInvoice bodyFast = new FastBodyVoidInvoice();
			bodyFast.key = itemBodyFast;
			string jsonBody = JsonConvert.SerializeObject(bodyFast);

			return CreateFastApiRequest(jsonBody, headers, url);
		}

		public static FastApiRequest ConvertAdjustInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string adjustmentType = GetAdjustmentType(Convert.ToInt32(model.Action));
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, (int)FAST_METHOD.ADJUST_INVOICE);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			Dictionary<string, object> extraMasterFields = CreateExtraDataFastIfNeeded(extraDataMapConfig, pmsBodyRequest);
			string bodyResult = BuildRequestAdjustOrReplaceBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, pmsBodyRequest.OriginalInvoice, adjustmentType, extraMasterFields, pmsBodyRequest.TaxSummaries);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		public static FastApiRequest ConvertReplaceInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, (int)FAST_METHOD.REPLACE_INVOICE);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			Dictionary<string, object> extraMasterFields = CreateExtraDataFastIfNeeded(extraDataMapConfig, pmsBodyRequest);
			string adjustmentType = "4";
			string bodyResult = BuildRequestAdjustOrReplaceBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, pmsBodyRequest.OriginalInvoice, adjustmentType, extraMasterFields, pmsBodyRequest.TaxSummaries);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		public static FastApiRequest ConvertPreviewInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionPreview, (int)FAST_METHOD.PREVIEW_INVOICE);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, extraMasterFields, pmsBodyRequest.TaxSummaries, model);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		public static FastApiRequest ConvertCreateDraftInvoiceRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);

			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string url = CreateUrlAPI(fastInfoToken, FastActionGeneral, (int)FAST_METHOD.DRAFT_INVOICE);
			Dictionary<string, string> headers = CreateHeaderAPI(fastInfoToken);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, extraMasterFields, pmsBodyRequest.TaxSummaries, model);

			return CreateFastApiRequest(bodyResult, headers, url);
		}

		#endregion

		#region HDDT Giá trị gia tăng
		public static FastApiSoapRequest ConvertCheckKeyRequest(FastMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			string soapBodyCheckKey = CreateSoapBodyCheckKey(fastInfoToken);

			return CreateFastApiSoapRequest(soapBodyCheckKey, url);
		}

		public static FastApiSoapRequest ConvertGetTokenRequest(FastMappingContext model, string keyToken)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			// Tạo hash key token
			string hashKeyToken = CreateHashKeyToken(fastInfoToken, keyToken);
			string soapBodyGetToken = CreateSoapBodyGetToken(fastInfoToken, hashKeyToken);

			return CreateFastApiSoapRequest(soapBodyGetToken, url);
		}

		public static FastApiSoapRequest ConvertCreateAndPublishInvoiceBySoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);

			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, extraMasterFields, pmsBodyRequest.TaxSummaries, model);
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyResult));
			string soapBodyCreateAndPublish = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionGeneral, (int)FAST_METHOD_GTGT.CREATE_AND_PUBLIC);

			return CreateFastApiSoapRequest(soapBodyCreateAndPublish, url);
		}

		public static FastApiSoapRequest ConvertCreateAndPublishMultiInvoiceSoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			List<CreateInvoiceRequest> pmsBodyRequest = model.Data as List<CreateInvoiceRequest>;

			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;
			string bodyResult = BuildRequestMultiInvoiceBody(pmsBodyRequest, extraDataMapConfig);
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyResult));
			string soapBodyCreateAndPublish = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionGeneral, (int)FAST_METHOD_GTGT.CREATE_AND_PUBLIC);

			return CreateFastApiSoapRequest(soapBodyCreateAndPublish, url);
		}

		public static FastApiSoapRequest ConvertBase64PDFInvoiceSoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			string url = CreateUrlAPISoap(companies);

			DownloadPdfRequest pmsDownloadPDFRequest = model.Data as DownloadPdfRequest;
			FastBodyGetPDFBase64Soap bodyGetPDFBase64 = new FastBodyGetPDFBase64Soap();
			bodyGetPDFBase64.key = pmsDownloadPDFRequest.SearchCode;
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bodyGetPDFBase64)));
			string soapBodyGetBase64PDF = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionGeneral, (int)FAST_METHOD_GTGT.GET_PDF);

			return CreateFastApiSoapRequest(soapBodyGetBase64PDF, url);
		}

		public static FastApiSoapRequest ConvertPreviewInvoiceSoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);
			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, extraMasterFields, pmsBodyRequest.TaxSummaries, model);
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyResult));
			string soapBodyCreateAndPublish = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionPreview, (int)FAST_METHOD_GTGT.CREATE_AND_PUBLIC);

			return CreateFastApiSoapRequest(soapBodyCreateAndPublish, url);
		}

		public static FastApiSoapRequest ConvertAdjustInvoiceSoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);

			string adjustmentType = GetAdjustmentType(Convert.ToInt32(model.Action));
			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string bodyResult = BuildRequestAdjustOrReplaceBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, pmsBodyRequest.OriginalInvoice, adjustmentType, extraMasterFields, pmsBodyRequest.TaxSummaries);
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyResult));
			string soapBodyCreateAndPublish = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionGeneral, (int)FAST_METHOD_GTGT.ADJUST_INVOICE);

			return CreateFastApiSoapRequest(soapBodyCreateAndPublish, url);
		}

		public static FastApiSoapRequest ConvertReplaceInvoiceSoapRequest(FastMappingContext model, string keyTokenForInvoice)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			pmsBodyRequest.Invoice.TotalAmountInWords = MoneyToText.DocTienBangChu(pmsBodyRequest.Invoice.AfterTaxAmount.Value * pmsBodyRequest.Invoice.ExchangeRate.Value);
			List<ExtraFieldMapping> extraDataMapConfig = model.ExtraData as List<ExtraFieldMapping>;

			string url = CreateUrlAPISoap(companies);
			FastInfoToken fastInfoToken = GetFastInfoTokenFromJson(companies, model.IsSerialMTT);

			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				extraMasterFields = CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			string adjustmentType = "4";
			string bodyResult = BuildRequestAdjustOrReplaceBody(pmsBodyRequest.Invoice, pmsBodyRequest.Details, pmsBodyRequest.OriginalInvoice, adjustmentType, extraMasterFields, pmsBodyRequest.TaxSummaries);
			string bodyInvoiceBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyResult));
			string soapBodyCreateAndPublish = CreateSoapBodyWithBase64Data(fastInfoToken, keyTokenForInvoice, bodyInvoiceBase64, FastActionGeneral, (int)FAST_METHOD_GTGT.REPLACE_INVOICE);

			return CreateFastApiSoapRequest(soapBodyCreateAndPublish, url);
		}
		#endregion

		public static string CreateUrlAPI(FastInfoToken fastInfoToken, int action, int method)
		{
			return $"{fastInfoToken.PartnerUrl}{ApiCreateAndPublish}?action={action}&method={method}&clientCode={fastInfoToken.clientCode}&proxyCode={fastInfoToken.proxyCode}";
		}

		public static string CreateUrlAPISoap(SiteConfiguration companies)
		{
			return $"{companies.PartnerUrl2}{ApiSoapCreateAndPublish}";
		}

		public static Dictionary<string, string> CreateHeaderAPI(FastInfoToken fastInfoToken)
		{
			string md5Password = ComputeMD5Hash(fastInfoToken.password);
			string checkSum = ComputeMD5Hash(md5Password + fastInfoToken.clientCode);

			Dictionary<string, string> headers = new Dictionary<string, string>
			{
				{ "user", fastInfoToken.user },
				{ "unitcode", fastInfoToken.UnitCode },
				{ "checkSum", checkSum },
			};
			return headers;
		}

		public static Dictionary<string, object> CreateExtraDataFast(List<ExtraFieldMapping> extraDataMapConfig, CreateInvoiceRequest pmsBodyRequest)
		{
			Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
			List<metadata> extraDataMapPMS = new List<metadata>();
			extraMasterFields["ExternalDetail1"] = "";
			extraMasterFields["ExternalDetail2"] = "";
			extraMasterFields["ExternalDetail3"] = "";

			if (pmsBodyRequest.ExtraData != null && pmsBodyRequest.ExtraData.Any())
			{
				extraDataMapPMS = pmsBodyRequest.ExtraData
					.Join(extraDataMapConfig, p => p.Code, e => e.Code, (p, e) => new metadata
					{
						keyTag = e.ThirdPartyCode,
						valueType = e.ThirdPartyDataType,
						keyLabel = e.ThirdPartyLabel,
						stringValue = p.StringValue,
					})
					.ToList();
				foreach (metadata itemMap in extraDataMapPMS)
				{
					if (itemMap.keyTag == "External1")
					{
						extraMasterFields["External1"] = itemMap.stringValue;
					}
					if (itemMap.keyTag == "External2")
					{
						extraMasterFields["External2"] = itemMap.stringValue;
					}
					if (itemMap.keyTag == "External3")
					{
						extraMasterFields["External3"] = itemMap.stringValue;
					}
					// Thêm các trường cho detail nếu có
					if (itemMap.keyTag == "ExternalDetail1")
					{
						extraMasterFields["ExternalDetail1"] = itemMap.stringValue;
					}
					if (itemMap.keyTag == "ExternalDetail2")
					{
						extraMasterFields["ExternalDetail2"] = itemMap.stringValue;
					}
					if (itemMap.keyTag == "ExternalDetail3")
					{
						extraMasterFields["ExternalDetail3"] = itemMap.stringValue;
					}
				}
			}
			return extraMasterFields;
		}

		public static string BuildRequestBody(CoreInvoice sourceMaster, List<InvoiceDetail> sourceDetail, Dictionary<string, object> extraMasterFields, List<TaxSummary> taxSummaryModels, dynamic model)
		{
			bool isDraft = model.Action == (int)INVOICE_ACTION.DRAFT;
			if (extraMasterFields.Count() == 0)
			{
				extraMasterFields["ExternalDetail1"] = "";
				extraMasterFields["ExternalDetail2"] = "";
				extraMasterFields["ExternalDetail3"] = "";
			}
			List<object[]> details = new List<object[]>();
			foreach (var item in sourceDetail)
			{
				if (isDraft)
				{
					string itemType = string.IsNullOrEmpty(item.ItemType) ? "01" : item.ItemType;
					details.Add(new object[]
					{
						"1", "", item.ItemName, item.UnitName, item.Quantity, item.Price,
						item.BeforeTaxAmount, item.TaxRate, item.TaxAmount, item.DiscountPercent ?? 0, item.DiscountAmount ?? 0, itemType,
						extraMasterFields["ExternalDetail1"], extraMasterFields["ExternalDetail2"], extraMasterFields["ExternalDetail3"]
					});
				}
				else
				{
					details.Add(new object[]
					{
						"1", "", item.ItemName, item.UnitName, item.Quantity, item.Price,
						item.BeforeTaxAmount, item.TaxRate, item.TaxAmount, item.DiscountPercent ?? 0, item.DiscountAmount ?? 0,
						extraMasterFields["ExternalDetail1"], extraMasterFields["ExternalDetail2"], extraMasterFields["ExternalDetail3"]
					});
				}
			}
			string paymentMethod = isDraft ? "" : sourceMaster.PaymentMethod;
			// CustomerType
			// Base master fields
			Dictionary<string, object> masterData = new Dictionary<string, object>
			{
				{ "Key", sourceMaster.InvoiceId.Replace("-", "") },
				{ "InvoiceDate", sourceMaster.InvoiceDate },
				{ "CustomerCode", "" },
				{ "CustomerName", sourceMaster.CompanyName },
				{ "CustomerTaxCode", sourceMaster.CustomerTax },
				{ "Address", sourceMaster.CustomerAddress },
				{ "PhoneNumber", sourceMaster.CustomerPhone },
				{ "IDCardNo", sourceMaster.IdentityCard },
				{ "Currency", sourceMaster.CurrencyCode },
				{ "ExchangeRate", sourceMaster.ExchangeRate },
				{ "Amount", sourceMaster.BeforeTaxAmount },
				{ "DiscountAmount", 0 },
				{ "TaxAmount", sourceMaster.TaxAmount },
				{ "TotalAmount", sourceMaster.AfterTaxAmount },
				{ "AmountInWords", sourceMaster.TotalAmountInWords },
				{ "HumanName", "" },
				{ "Buyer", sourceMaster.CustomerName },
				{ "PaymentMethod", paymentMethod },
				{ "EmailDeliver", sourceMaster.CustomerEmail },
				{ "PassportNo", sourceMaster.PassportNum },
				{ "BuyerUnit", sourceMaster.BudgetCode },
			};

			taxSummaryModels.ForEach(item =>
			{
				if (item.TaxRate == 8)
				{
					masterData.Add("TaxAmount0", item.TaxAmount);
				}
				if (item.TaxRate == 5)
				{
					masterData.Add("TaxAmount5", item.TaxAmount);
				}
				if (item.TaxRate == 10)
				{
					masterData.Add("TaxAmount10", item.TaxAmount);
				}

			});

			if (extraMasterFields != null && extraMasterFields.Any())
			{
				foreach (var kv in extraMasterFields)
				{
					masterData[kv.Key] = kv.Value;
				}
			}

			// Hóa đơn nháp có thêm trường ItemType trong detail
			var detailColumns = isDraft
				? new List<string>
				{
					"ProcessType", "ItemCode", "ItemName", "UOM", "Quantity", "Price",
					"Amount", "TaxRate", "TaxAmount", "DiscountRate", "DiscountAmount", "ItemType",
					"External1", "External2", "External3"
				}
				: new List<string>
				{
					"ProcessType", "ItemCode", "ItemName", "UOM", "Quantity", "Price",
					"Amount", "TaxRate", "TaxAmount", "DiscountRate", "DiscountAmount",
					"External1", "External2", "External3"
				};
			// Lấy BeforeTaxAmount do nhà cung cấp báo lấy Amount = Tiền sau phí, sau giảm giá
			var request = new RequestData
			{
				voucherBook = sourceMaster.Serial,
				data = new InvoiceData
				{
					structure = new Structure
					{
						master = masterData.Keys.ToList(),
						detail = detailColumns
					},
					invoices = new List<Invoice>
					{
						new Invoice
						{
							master = masterData.Values.ToList(),
							detail = details,
						}
					}
				},
			};

			return JsonConvert.SerializeObject(request, Formatting.Indented);
		}

		public static string BuildRequestMultiInvoiceBody_old(List<CreateInvoiceRequest> pmsRequests)
		{
			CoreInvoice pmsInvoiceFirst = pmsRequests[0].Invoice;
			List<Invoice> invoices = new List<Invoice>();
			foreach (var itemPMSRequest in pmsRequests)
			{
				CoreInvoice sourceMaster = itemPMSRequest.Invoice;
				List<InvoiceDetail> sourceDetail = itemPMSRequest.Details;
				sourceMaster.TotalAmountInWords = MoneyToText.DocTienBangChu(itemPMSRequest.Invoice.AfterTaxAmount.Value * itemPMSRequest.Invoice.ExchangeRate.Value);

				Invoice invoice = new Invoice();
				// Gán giá trị cho Master
				invoice.master = new List<object>
				{
					sourceMaster.InvoiceId.Replace("-", ""), sourceMaster.InvoiceDate, "", sourceMaster.CompanyName, sourceMaster.CustomerTax,
					sourceMaster.CustomerAddress, sourceMaster.CustomerPhone, sourceMaster.IdentityCard, sourceMaster.CurrencyCode, sourceMaster.ExchangeRate,
					sourceMaster.BeforeTaxAmount, "0.00", sourceMaster.TaxAmount, sourceMaster.AfterTaxAmount,
					sourceMaster.TotalAmountInWords, "", sourceMaster.CustomerName, sourceMaster.PaymentMethod
				};

				// Gán giá trị cho Details
				List<object[]> details = new List<object[]>();
				foreach (var item in sourceDetail)
				{
					details.Add(new object[]
					{
					"1", "", item.ItemName, item.UnitName, item.Quantity, item.Price,
					item.BeforeTaxAmount, item.TaxRate, item.TaxAmount, item.DiscountPercent, item.DiscountAmount
					});
				}
				invoice.detail = details;

				invoices.Add(invoice);
			}

			var request = new RequestData
			{
				voucherBook = pmsInvoiceFirst.Serial,
				data = new InvoiceData
				{
					structure = new Structure
					{
						master = new List<string>
						{
							"Key", "InvoiceDate", "CustomerCode", "CustomerName", "CustomerTaxCode",
							"Address", "PhoneNumber", "IDCardNo", "Currency", "ExchangeRate",
							"Amount", "DiscountAmount", "TaxAmount", "TotalAmount",
							"AmountInWords", "HumanName", "Buyer", "PaymentMethod"
						},
						detail = new List<string>
						{
							"ProcessType", "ItemCode", "ItemName", "UOM", "Quantity", "Price",
							"Amount", "TaxRate", "TaxAmount", "DiscountRate", "DiscountAmount"
						}
					},
					invoices = invoices
				}
			};

			return JsonConvert.SerializeObject(request, Formatting.Indented);
		}

		public static string BuildRequestMultiInvoiceBody(List<CreateInvoiceRequest> pmsRequests, List<ExtraFieldMapping> extraDataMapConfig)
		{
			if (pmsRequests == null || !pmsRequests.Any()) return string.Empty;

			CoreInvoice pmsInvoiceFirst = pmsRequests[0].Invoice;
			var allMasterKeys = new HashSet<string>
			{
				"Key", "InvoiceDate", "CustomerCode", "CustomerName", "CustomerTaxCode",
				"Address", "PhoneNumber", "IDCardNo", "Currency", "ExchangeRate",
				"Amount", "DiscountAmount", "TaxAmount", "TotalAmount",
				"AmountInWords", "HumanName", "Buyer", "PaymentMethod", "EmailDeliver", "PassportNo", "BuyerUnit"
			};

			List<Invoice> invoices = new();

			foreach (var itemPMSRequest in pmsRequests)
			{
				var sourceMaster = itemPMSRequest.Invoice;
				var sourceDetail = itemPMSRequest.Details;
				var taxSummaryModels = itemPMSRequest.TaxSummaries;

				sourceMaster.TotalAmountInWords = MoneyToText.DocTienBangChu(
					sourceMaster.AfterTaxAmount.Value * sourceMaster.ExchangeRate.Value
				);

				Dictionary<string, object> extraMasterFields = new Dictionary<string, object>();
				if (extraDataMapConfig != null && extraDataMapConfig.Any())
				{
					extraMasterFields = CreateExtraDataFast(extraDataMapConfig, itemPMSRequest);
				}

				// Tạo dictionary cho master động
				var masterDict = new Dictionary<string, object>
				{
					{ "Key", sourceMaster.InvoiceId.Replace("-", "") },
					{ "InvoiceDate", sourceMaster.InvoiceDate },
					{ "CustomerCode", "" },
					{ "CustomerName", sourceMaster.CompanyName },
					{ "CustomerTaxCode", sourceMaster.CustomerTax },
					{ "Address", sourceMaster.CustomerAddress },
					{ "PhoneNumber", sourceMaster.CustomerPhone },
					{ "IDCardNo", sourceMaster.IdentityCard },
					{ "Currency", sourceMaster.CurrencyCode },
					{ "ExchangeRate", sourceMaster.ExchangeRate },
					{ "Amount", sourceMaster.BeforeTaxAmount },
					{ "DiscountAmount", "0.00" },
					{ "TaxAmount", sourceMaster.TaxAmount },
					{ "TotalAmount", sourceMaster.AfterTaxAmount },
					{ "AmountInWords", sourceMaster.TotalAmountInWords },
					{ "HumanName", "" },
					{ "Buyer", sourceMaster.CustomerName },
					{ "PaymentMethod", sourceMaster.PaymentMethod },
					{ "EmailDeliver", sourceMaster.CustomerEmail },
					{ "PassportNo", sourceMaster.PassportNum },
					{ "BuyerUnit", sourceMaster.BudgetCode },
				};

				taxSummaryModels.ForEach(item =>
				{
					if (item.TaxRate == 8)
					{
						masterDict.Add("TaxAmount0", item.TaxAmount);
					}
					if (item.TaxRate == 5)
					{
						masterDict.Add("TaxAmount5", item.TaxAmount);
					}
					if (item.TaxRate == 10)
					{
						masterDict.Add("TaxAmount10", item.TaxAmount);
					}

				});

				// Thêm key động nếu có
				if (extraMasterFields != null && extraMasterFields.Any())
				{
					foreach (var kv in extraMasterFields)
					{
						masterDict[kv.Key] = kv.Value;
						allMasterKeys.Add(kv.Key); // gom toàn bộ key dùng trong mọi invoice
					}
				}

				// build detail
				var details = sourceDetail.Select(item => new object[]
				{
					"1", "", item.ItemName, item.UnitName, item.Quantity, item.Price,
					item.BeforeTaxAmount, item.TaxRate, item.TaxAmount, item.DiscountPercent, item.DiscountAmount
				}).ToList();

				// build master theo thứ tự allMasterKeys
				var masterOrdered = allMasterKeys.Select(k => masterDict.ContainsKey(k) ? masterDict[k] : null).ToList();

				invoices.Add(new Invoice
				{
					master = masterOrdered,
					detail = details
				});
			}

			var request = new RequestData
			{
				voucherBook = pmsInvoiceFirst.Serial,
				data = new InvoiceData
				{
					structure = new Structure
					{
						master = allMasterKeys.ToList(),
						detail = new List<string>
						{
							"ProcessType", "ItemCode", "ItemName", "UOM", "Quantity", "Price",
							"Amount", "TaxRate", "TaxAmount", "DiscountRate", "DiscountAmount"
						}
					},
					invoices = invoices
				}
			};

			return JsonConvert.SerializeObject(request, Formatting.Indented);
		}

		public static string BuildRequestAdjustOrReplaceBody(CoreInvoice sourceMaster, List<InvoiceDetail> sourceDetail, CoreInvoice invoiceOld, string adjustmentType, Dictionary<string, object> extraMasterFields, List<TaxSummary> taxSummaryModels)
		{
			List<object[]> details = new List<object[]>();
			if (extraMasterFields.Count() == 0)
			{
				extraMasterFields["ExternalDetail1"] = "";
				extraMasterFields["ExternalDetail2"] = "";
				extraMasterFields["ExternalDetail3"] = "";
			}
			foreach (var item in sourceDetail)
			{
				details.Add(new object[]
				{
					"1", "", item.ItemName, item.UnitName, item.Quantity, item.Price,
					item.BeforeTaxAmount, item.TaxRate, item.TaxAmount, item.DiscountPercent, item.DiscountAmount,
					extraMasterFields["ExternalDetail1"], extraMasterFields["ExternalDetail2"], extraMasterFields["ExternalDetail3"]
				});
			}

			// Base master fields
			Dictionary<string, object> masterData = new Dictionary<string, object>
			{
				{ "Key", sourceMaster.InvoiceId.Replace("-", "") },
				{ "InvoiceDate", sourceMaster.InvoiceDate },
				{ "CustomerCode", "" },
				{ "CustomerName", sourceMaster.CompanyName },
				{ "CustomerTaxCode", sourceMaster.CustomerTax },
				{ "Address", sourceMaster.CustomerAddress },
				{ "PhoneNumber", sourceMaster.CustomerPhone },
				{ "IDCardNo", sourceMaster.IdentityCard },
				{ "Currency", sourceMaster.CurrencyCode },
				{ "ExchangeRate", sourceMaster.ExchangeRate },
				{ "Amount", sourceMaster.BeforeTaxAmount },
				{ "DiscountAmount", "0.00" },
				{ "TaxAmount", sourceMaster.TaxAmount },
				{ "TotalAmount", sourceMaster.AfterTaxAmount },
				{ "AmountInWords", sourceMaster.TotalAmountInWords },
				{ "HumanName", "" },
				{ "Buyer", sourceMaster.CustomerName },
				{ "PaymentMethod", sourceMaster.PaymentMethod },
				{ "EmailDeliver", sourceMaster.CustomerEmail },
				{ "PassportNo", sourceMaster.PassportNum },
				{ "BuyerUnit", sourceMaster.BudgetCode },
			};

			taxSummaryModels.ForEach(item =>
			{
				if (item.TaxRate == 8)
				{
					masterData.Add("TaxAmount0", item.TaxAmount);
				}
				if (item.TaxRate == 5)
				{
					masterData.Add("TaxAmount5", item.TaxAmount);
				}
				if (item.TaxRate == 10)
				{
					masterData.Add("TaxAmount10", item.TaxAmount);
				}

			});

			if (extraMasterFields != null && extraMasterFields.Any())
			{
				foreach (var kv in extraMasterFields)
				{
					masterData[kv.Key] = kv.Value;
				}
			}


			var request = new RequestDataAdjustOrReplace
			{
				voucherBook = sourceMaster.Serial,
				originalInvoice = invoiceOld.SearchCode,
				adjustmentType = adjustmentType,
				data = new InvoiceData
				{
					structure = new Structure
					{
						master = masterData.Keys.ToList(),
						detail = new List<string>
						{
							"ProcessType", "ItemCode", "ItemName", "UOM", "Quantity", "Price",
							"Amount", "TaxRate", "TaxAmount", "DiscountRate", "DiscountAmount",
							"External1", "External2", "External3"
						}
					},
					invoices = new List<Invoice>
					{
						new Invoice
						{
							master = masterData.Values.ToList(),
							detail = details
						}
					}
				}
			};

			return JsonConvert.SerializeObject(request, Formatting.Indented);
		}

		public static string CreateSoapBodyCheckKey(FastInfoToken fastInfoToken)
		{
			return $@"<?xml version=""1.0"" encoding=""utf-8""?>
				<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
							xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
							xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
				<soap:Body>
					<CheckKey xmlns=""http://tempuri.org/"">
					<proxyCode>{fastInfoToken.proxyCode}</proxyCode>
					<clientCode>{fastInfoToken.clientCode}</clientCode>
					<user>{fastInfoToken.user}</user>
					<data></data>
					</CheckKey>
				</soap:Body>
				</soap:Envelope>";
		}

		public static string CreateSoapBodyGetToken(FastInfoToken fastInfoToken, string hashKeyToken)
		{
			return $@"<?xml version=""1.0"" encoding=""utf-8""?>
				<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
					xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
					xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
				<soap12:Body>
					<GetKey xmlns=""http://tempuri.org/"">
						<proxyCode>{fastInfoToken.proxyCode}</proxyCode>
						<clientCode>{fastInfoToken.clientCode}</clientCode>
						<user>{fastInfoToken.user}</user>
						<hash>{hashKeyToken}</hash>
					</GetKey>
				</soap12:Body>
				</soap12:Envelope>";
		}

		public static string CreateSoapBodyWithBase64Data(FastInfoToken fastInfoToken, string keyTokenForInvoice, string bodyInvoiceBase64 = "", int fastActionGeneral = 0, int fastMethod = 0)
		{
			return $@"<?xml version=""1.0"" encoding=""utf-8""?>
				<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
							xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
							xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
				<soap:Body>
					<ExcuteCommand xmlns=""http://tempuri.org/"">
						<action>{fastActionGeneral}</action>
						<method>{fastMethod}</method>
						<user>{fastInfoToken.user}</user>
						<clientCode>{fastInfoToken.clientCode}</clientCode>
						<proxyCode>{fastInfoToken.proxyCode}</proxyCode>
						<unitCode>{fastInfoToken.UnitCode}</unitCode>
						<data>{bodyInvoiceBase64}</data>
						<checkSum>{keyTokenForInvoice}</checkSum>
					</ExcuteCommand>
				</soap:Body>
				</soap:Envelope>";
		}

		public static string CreateHashKeyToken(FastInfoToken fastInfoToken, string keyToken)
		{
			string md5Password = ComputeMD5Hash(fastInfoToken.password);
			string md5PasswordAndkeyToken = ComputeMD5Hash(md5Password + keyToken);
			string hash = md5PasswordAndkeyToken + Chrw254 + keyToken;
			return hash;
		}

		static string ComputeMD5Hash(string input)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] inputBytes = Encoding.UTF8.GetBytes(input ?? "");
				byte[] hashBytes = md5.ComputeHash(inputBytes);
				return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
			}
		}

		public static FastInfoToken GetFastInfoToken(SiteConfiguration company)
		{
			return new FastInfoToken
			{
				clientCode = company.Username2,
				proxyCode = company.Password2,
				user = company.Username,
				PartnerUrl = company.PartnerUrl,
				password = company.Password,
				UnitCode = company.PartnerUrl2
			};
		}

		public static FastInfoToken GetFastInfoTokenFromJson(SiteConfiguration company, bool isInvoiceMTT)
		{
			//string jsonToken = "{\"clientCode\":\"FAST_CLIENT_001\",\"proxyCode\":\"PROXY_001\",\"user\":\"fastuser@company.com\",\"password\":\"securepassword123\",\"unitCode\":\"UNIT_001\"}";
			string partnerUrl = isInvoiceMTT ? company.PartnerUrl : company.PartnerUrl2;
			string password = isInvoiceMTT ? company.Password : company.Password2;
			string jsonToken = isInvoiceMTT ? company.Username : company.Username2;
			FastJsonTokenDB dataTokenFast = JsonConvert.DeserializeObject<FastJsonTokenDB>(jsonToken);
			return new FastInfoToken
			{
				clientCode = dataTokenFast.clientC,
				proxyCode = dataTokenFast.proxyC,
				user = dataTokenFast.user,
				PartnerUrl = partnerUrl,
				password = password,
				UnitCode = dataTokenFast.unitC,
			};
		}

		// Helper methods
		private static string GetAdjustmentType(int action)
		{
			return action switch
			{
				(int)INVOICE_ACTION.ADJUST_INFOR => "3",
				(int)INVOICE_ACTION.ADJUST_DECREASE => "2",
				(int)INVOICE_ACTION.ADJUST_INCREASE => "1",
				_ => ""
			};
		}

		private static Dictionary<string, object> CreateExtraDataFastIfNeeded(List<ExtraFieldMapping> extraDataMapConfig, AdjustOrReplaceInvoiceRequest pmsBodyRequest)
		{
			if (extraDataMapConfig != null && extraDataMapConfig.Any())
			{
				return CreateExtraDataFast(extraDataMapConfig, pmsBodyRequest);
			}
			return new Dictionary<string, object>();
		}

		private static FastApiRequest CreateFastApiRequest(string bodyResult, Dictionary<string, string> headers, string url)
		{
			return new FastApiRequest
			{
				bodyPost = bodyResult,
				headersAPI = headers,
				urlAPI = url
			};
		}

		private static FastApiSoapRequest CreateFastApiSoapRequest(string bodyXML, string url)
		{
			return new FastApiSoapRequest
			{
				bodyXMLPost = bodyXML,
				urlAPI = url
			};
		}
	}
}
