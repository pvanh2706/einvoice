#nullable disable
#pragma warning disable CS0414 // Retain unused historical reserved-invoice endpoint for provenance.
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.EasyInvoice.Wire;
using eInvoice.Providers.Serialization;
using Newtonsoft.Json;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using Invoice = eInvoice.Providers.EasyInvoice.Wire.Invoice;
namespace eInvoice.Providers.EasyInvoice.Mapping
{
	internal static class EasyInvoiceRequestMapper
	{
		private static readonly string apiCreateAndPublish = "api/publish/importAndPublishInvoice";
		private static readonly string apiReplace = "api/business/replaceInvoice";
		private static readonly string apiAdjust = "api/business/adjustInvoice";
		private static readonly string apiCreateDraft = "api/publish/importInvoice";
		private static readonly string apiCreate = "api/publish/importReservedInvoice";
		private static readonly string apiVoid = "api/business/cancelInvoice";
		private static readonly string apiDelete = "api/business/removeUnsignedInvoice";
		private static readonly string apiGetPDF = "api/publish/getInvoicePdf";
		private static readonly string apiPreview = "api/publish/previewInvoice";
		private static readonly string apiPublish = "api/publish/issueInvoices";
		private static readonly string formatString = "G29";
		public static EasyInvoiceApiRequest ConvertCreateAndPublishInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;

			string url = $"{companies.PartnerUrl}{apiCreateAndPublish}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceCreateAndPublishRequestModels easyInvoiceRequestModels = new EasyInvoiceCreateAndPublishRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.XmlData = CreateXMLInvoice(pmsBodyRequest, extraDataMap);
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertAdjustInvoiceRequest(EasyInvoiceMappingContext model)
		{
			int Action = (int)model.Action;
			string typeAdjust = ((int)EasyAdjustType.Info).ToString();
			if (Action == (int)InvoiceAction.AdjustDecrease)
			{
				typeAdjust = ((int)EasyAdjustType.Decrease).ToString();
			}
			else if (Action == (int)InvoiceAction.AdjustIncrease)
			{
				typeAdjust = ((int)EasyAdjustType.Increase).ToString();
			}

			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;

			string url = $"{companies.PartnerUrl}{apiAdjust}";
			Dictionary<string, string> headers = new Dictionary<string, string>();


			EasyInvoiceAdjustOrReplaceRequestModels easyInvoiceRequestModels = new EasyInvoiceAdjustOrReplaceRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.XmlData = CreateXMLAdjustInvoice(pmsBodyRequest, typeAdjust, extraDataMap);
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.OriginalInvoiceId;
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertReplaceInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;

			string url = $"{companies.PartnerUrl}{apiReplace}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceAdjustOrReplaceRequestModels easyInvoiceRequestModels = new EasyInvoiceAdjustOrReplaceRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.XmlData = CreateXMLReplaceInvoice(pmsBodyRequest, extraDataMap);
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.OriginalInvoiceId;
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertCreateDraftInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;

			string url = $"{companies.PartnerUrl}{apiCreateDraft}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceCreateAndPublishRequestModels easyInvoiceRequestModels = new EasyInvoiceCreateAndPublishRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.XmlData = CreateXMLInvoice(pmsBodyRequest, extraDataMap);
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertCreateInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			CreateInvoiceRequest pmsBodyRequest = model.Data as CreateInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;

			//string url = $"{companies.PartnerUrl}{apiCreate}";
			// Đổi thành API tạo nháp không có số hóa đơn (theo yêu cầu EasyInvoice) do nếu dùng apiCreate thì bắt buộc phải truyền thêm số hóa đơn khi tạo (số hóa đơn này PMS đang không quản lý được do phải lấy số hóa đơn cuối cùng phát hành +1)
			// Sau khi phát hành vẫn có thể gọi apiPublish để phát hành và ký giống với apiCreate, chỉ khác không có số hóa đơn trên PDF
			string url = $"{companies.PartnerUrl}{apiCreateDraft}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceCreateAndPublishRequestModels easyInvoiceRequestModels = new EasyInvoiceCreateAndPublishRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.XmlData = CreateXMLInvoice(pmsBodyRequest, extraDataMap);
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertVoidInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			VoidInvoiceRequest pmsBodyRequest = model.Data as VoidInvoiceRequest;
			string strSerial = pmsBodyRequest.Serial;

			string url = $"{companies.PartnerUrl}{apiVoid}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceViewRequestModels easyInvoiceRequestModels = new EasyInvoiceViewRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.InvoiceId;
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertDeleteInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			DeleteInvoiceRequest pmsBodyRequest = model.Data as DeleteInvoiceRequest;
			string strSerial = pmsBodyRequest.Serial;

			string url = $"{companies.PartnerUrl}{apiDelete}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceViewRequestModels easyInvoiceRequestModels = new EasyInvoiceViewRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.InvoiceId;
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertGetPDFInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			DownloadPdfRequest pmsBodyRequest = model.Data as DownloadPdfRequest;
			string strSerial = pmsBodyRequest.Serial;

			string url = $"{companies.PartnerUrl}{apiGetPDF}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoiceViewRequestModels easyInvoiceRequestModels = new EasyInvoiceViewRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.InvoiceId;
			easyInvoiceRequestModels.Option = "0";
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public static EasyInvoiceApiRequest ConvertPreviewInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			AdjustOrReplaceInvoiceRequest pmsBodyRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			string strSerial = pmsBodyRequest.Invoice.Serial;
			CoreInvoice pmsInvoice = pmsBodyRequest.Invoice;

			string url = $"{companies.PartnerUrl}{apiPreview}";
			Dictionary<string, string> headers = new Dictionary<string, string>();



			EasyInvoicePreviewRequestModels easyInvoiceRequestModels = new EasyInvoicePreviewRequestModels();
			easyInvoiceRequestModels.Pattern = strSerial;


			string invoiceType = ((int)EasyType.Normal).ToString();
			if (pmsInvoice.PreviewType == (int)PreviewType.ADJUST_INFO)
			{
				invoiceType = ((int)EasyType.AdjustInfo).ToString();
			}
			else if (pmsInvoice.PreviewType == (int)PreviewType.ADJUST_INCREASE)
			{
				invoiceType = ((int)EasyType.AdjustIncrease).ToString();
			}
			else if (pmsInvoice.PreviewType == (int)PreviewType.ADJUST_DECREASE)
			{
				invoiceType = ((int)EasyType.AdjustDecrease).ToString();
			}
			else if (pmsInvoice.PreviewType == (int)PreviewType.REPLACE)
			{
				invoiceType = ((int)EasyType.Replace).ToString();
			}

			if (pmsInvoice.PreviewType == (int)PreviewType.ADJUST_INFO ||
				pmsInvoice.PreviewType == (int)PreviewType.ADJUST_INCREASE ||
				pmsInvoice.PreviewType == (int)PreviewType.ADJUST_DECREASE)
			{
				easyInvoiceRequestModels.XmlData = CreateXMLAdjustInvoice(pmsBodyRequest, invoiceType, extraDataMap);
			}
			else if (pmsInvoice.PreviewType == (int)PreviewType.REPLACE)
			{
				easyInvoiceRequestModels.XmlData = CreateXMLReplaceInvoice(pmsBodyRequest, extraDataMap);
			}
			else
			{
				easyInvoiceRequestModels.XmlData = CreateXMLInvoice(pmsBodyRequest, extraDataMap);
			}
			easyInvoiceRequestModels.Option = "1";
			easyInvoiceRequestModels.Ikey = pmsBodyRequest.OriginalInvoice?.InvoiceId;
			easyInvoiceRequestModels.InvoiceType = invoiceType;


			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static EasyInvoiceApiRequest ConvertPublishInvoiceRequest(EasyInvoiceMappingContext model)
		{
			SiteConfiguration companies = model.Company as SiteConfiguration;
			VoidInvoiceRequest pmsBodyRequest = model.Data as VoidInvoiceRequest;

			string url = $"{companies.PartnerUrl}{apiPublish}";
			Dictionary<string, string> headers = new Dictionary<string, string>();

			EasyInvoicePublishRequestModels easyInvoiceRequestModels = new EasyInvoicePublishRequestModels();
			easyInvoiceRequestModels.Ikeys = new List<string>{ pmsBodyRequest.InvoiceId };
			easyInvoiceRequestModels.Pattern = pmsBodyRequest.Serial;
			string bodyPost = JsonConvert.SerializeObject(easyInvoiceRequestModels, Formatting.Indented);

			EasyInvoiceApiRequest apiRequest = new EasyInvoiceApiRequest();
			apiRequest.bodyPost = bodyPost;
			apiRequest.headersAPI = headers;
			apiRequest.urlAPI = url;

			return apiRequest;
		}

		public static string CreateXMLInvoice(CreateInvoiceRequest pmsBodyRequest, List<ExtraFieldMapping> extraDataMap)
		{
			var dataInvoices = new List<Inv>();
			dataInvoices.Add(CreateDataInvoice(pmsBodyRequest, extraDataMap));

			Invoices invoices = new Invoices
			{
				Inv = dataInvoices
			};
			// string result = XmlHelperObject.Serialize(invoices);
			string result = XmlHelper.Serialize(invoices);
			return result;
		}

		public static string CreateXMLAdjustInvoice(AdjustOrReplaceInvoiceRequest pmsBodyRequest, string typeAdjust, List<ExtraFieldMapping> extraDataMap)
		{
			AdjustInv adjustInvoices = CreateDataAdjustInvoice(pmsBodyRequest, typeAdjust, extraDataMap);
			string result = XmlHelper.Serialize(adjustInvoices);
			return result;
		}

		public static string CreateXMLReplaceInvoice(AdjustOrReplaceInvoiceRequest pmsBodyRequest, List<ExtraFieldMapping> extraDataMap)
		{
			ReplaceInv adjustInvoices = CreateDataReplaceInvoice(pmsBodyRequest, extraDataMap);
			string result = XmlHelper.Serialize(adjustInvoices);
			return result;
		}

		public static Inv CreateDataInvoice(CreateInvoiceRequest input, List<ExtraFieldMapping> mappings) => new Inv { Invoice = BuildInvoice<Invoice>(input, mappings, true) };
        public static AdjustInv CreateDataAdjustInvoice(AdjustOrReplaceInvoiceRequest input, string typeAdjust, List<ExtraFieldMapping> mappings)
        {
            var invoice = BuildInvoice<AdjustInv>(input, mappings, false);
            invoice.Type = typeAdjust;
            return invoice;
        }
        public static ReplaceInv CreateDataReplaceInvoice(AdjustOrReplaceInvoiceRequest input, List<ExtraFieldMapping> mappings) => BuildInvoice<ReplaceInv>(input, mappings, false);
        // Shared fees, ExtraData and tax totals. Normal/adjust/replace differences preserve baseline.
        private static T BuildInvoice<T>(CreateInvoiceRequest pmsBodyRequest, List<ExtraFieldMapping> extraDataMap, bool normal) where T : Invoice, new()
		{
			CoreInvoice pmsDataInvoice = pmsBodyRequest.Invoice;
			List<InvoiceDetail> pmsDataProduct = pmsBodyRequest.Details;
			List<TaxSummary> pmsTaxSummaryInvoice = pmsBodyRequest.TaxSummaries;
			List<ServiceSummary> pmsServiceSummaryInvoice = pmsBodyRequest.ServiceSummaries;
			List<Metadata> extraDataMapPMS = new List<Metadata>();
			var exchangeRate = (pmsDataInvoice.ExchangeRate ?? 0) == 0 ? 1 : pmsDataInvoice.ExchangeRate;

			var totalAmount = pmsDataProduct.Sum(x => (x.BeforeTaxAmount ?? 0));

			var listProduct = CreateDataProduct(pmsDataProduct);

			string extraProductJson = "";
			ExtraDataInvoice extraProduct = new ExtraDataInvoice();
			extraProduct.serviceCharge = 0;
			foreach (var item in pmsServiceSummaryInvoice)
			{
				extraProduct.serviceCharge += item.ServiceCharge ?? 0;
				extraProduct.serviceChargeRate = item.ServiceRate;
				extraProduct.SubTotal = totalAmount;
				// Nhà cung cấp yêu cầu để ẩn hiện phí trên hóa đơn "Khi nào có phí a truyền thêm hide = 1 cho giống khi tạo tay hđ a ạ"
				extraProduct.hide = 1;
				// Chuyển đổi ExtraProduct thành JSON


				ExtraDataProduct extraDataProduct = new ExtraDataProduct();
				extraDataProduct.icheck = 1;
				string extraDataProductJson = JsonConvert.SerializeObject(extraDataProduct, Formatting.Indented);

				listProduct.Count();
				listProduct.Add(new Product
				{
					Code = "",
					No = (listProduct.Count() + 1).ToString(),
					Feature = "",
					ProdName = "Phí phục vụ",
					ProdUnit = "",
					ProdQuantity = "",
					ProdPrice = "",
					Discount = "0",
					DiscountAmount = "0",
					Total = (item.ServiceCharge ?? 0).ToString(),
					VATRate = item.TaxRate?.ToString(formatString),
					VATAmount = item.TaxAmount?.ToString(),
					Amount = item.AfterTaxAmount?.ToString(formatString),
					Extra = extraDataProductJson,
				});
			}

			if (extraDataMap != null && extraDataMap.Any())
			{
				if (pmsBodyRequest.ExtraData != null && pmsBodyRequest.ExtraData.Any())
				{
					extraDataMapPMS = pmsBodyRequest.ExtraData
						.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new Metadata
						{
							keyTag = e.ThirdPartyCode,
							valueType = e.ThirdPartyDataType,
							keyLabel = e.ThirdPartyLabel,
							stringValue = p.StringValue,
						})
						.ToList();
					foreach (Metadata itemMap in extraDataMapPMS)
					{
						if (itemMap.keyTag == "arrivalDate")
						{
							extraProduct.arrivalDate = itemMap.stringValue;
						}
						if (itemMap.keyTag == "departureDate")
						{
							extraProduct.departureDate = itemMap.stringValue;
						}
						if (itemMap.keyTag == "folioNo")
						{
							extraProduct.folioNo = itemMap.stringValue;
						}
						if (itemMap.keyTag == "bkNo")
						{
							extraProduct.bkNo = itemMap.stringValue;
						}
						if (itemMap.keyTag == "EmployeeName")
						{
							extraProduct.EmployeeName = itemMap.stringValue;
						}
					}
				}
			}

			extraProductJson = JsonConvert.SerializeObject(extraProduct, Formatting.Indented);


			T invoiceItem = new T
			{
				TaxAuthorityCode = normal ? "" : ((AdjustOrReplaceInvoiceRequest)pmsBodyRequest).OriginalInvoice.MaCQT,
				Ikey = pmsDataInvoice.InvoiceId ?? Guid.NewGuid().ToString(),
				CusCode = "",
				Buyer = pmsDataInvoice.CustomerName,
				CusName = pmsDataInvoice.CompanyName,
				Email = pmsDataInvoice.CustomerEmail,
				EmailCC  = "",
				CusEmails = pmsDataInvoice.CustomerEmail,
				CusAddress = pmsDataInvoice.CustomerAddress,
				CusBankName = pmsDataInvoice.BankName,
				CusBankNo = pmsDataInvoice.BankAccount,
				CusPhone  = pmsDataInvoice.CustomerPhone,
				IdentityCardNumber = "",
				CusTaxCode = pmsDataInvoice.CustomerTax,
				PaymentMethod = pmsDataInvoice.PaymentMethod,
				ArisingDate = pmsDataInvoice.InvoiceDate,
				ExchangeRate = pmsDataInvoice.ExchangeRate?.ToString(formatString),
				CurrencyUnit  = pmsDataInvoice.CurrencyCode,
				Extra = "",
				Products = listProduct,
				Total = pmsDataInvoice.BeforeTaxAmount?.ToString(),
				//// Chuyển g lấy tổng tiền từ tổng tiền các sản phẩm để tránh sai lệch do BeforeTaxAmount trong Invoice không bằng tổng trong chi tiết sản phẩm khi cấu hình Phí "Hiển thị tiền phí ở khu vực tổng cộng"
				//Total = totalAmount.ToString(),
				VATRate = pmsDataInvoice.TaxRate?.ToString(formatString),
				VATAmount = pmsDataInvoice.TaxAmount?.ToString(),
				Amount = pmsDataInvoice.AfterTaxAmount?.ToString(formatString),
				AmountInWords = MoneyToText.DocTienBangChu((pmsDataInvoice.AfterTaxAmount ?? 0) * (normal ? (exchangeRate ?? 1) : (pmsDataInvoice.ExchangeRate ?? 1))),
				CusIdentification = pmsDataInvoice.IdentityCard,
				PassportNo = pmsDataInvoice.PassportNum,
				BudgetaryRelationshipCode = pmsDataInvoice.BudgetCode,
			};

			foreach (var item in pmsTaxSummaryInvoice)
			{
				if (item.TaxRate == 0)
				{
					invoiceItem.GrossValue0 = item.BeforeTaxAmount?.ToString();
					invoiceItem.VatAmount0 = item.TaxAmount?.ToString();
					invoiceItem.Amount0 = ((item.BeforeTaxAmount ?? 0) + (item.TaxAmount ?? 0)).ToString();
				}
				if (item.TaxRate == 5)
				{
					invoiceItem.GrossValue5 = item.BeforeTaxAmount?.ToString();
					invoiceItem.VatAmount5 = item.TaxAmount?.ToString();
					invoiceItem.Amount5 = ((item.BeforeTaxAmount ?? 0) + (item.TaxAmount ?? 0)).ToString();
				}
				if (item.TaxRate == 8)
				{
					invoiceItem.GrossValue8 = item.BeforeTaxAmount?.ToString();
					invoiceItem.VatAmount8 = item.TaxAmount?.ToString();
					invoiceItem.Amount8 = ((item.BeforeTaxAmount ?? 0) + (item.TaxAmount ?? 0)).ToString();
				}
				if (item.TaxRate == 10)
				{
					invoiceItem.GrossValue10 = item.BeforeTaxAmount?.ToString();
					invoiceItem.VatAmount10 = item.TaxAmount?.ToString();
					invoiceItem.Amount10 = ((item.BeforeTaxAmount ?? 0) + (item.TaxAmount ?? 0)).ToString();
				}
			}
			//string extraProductJson = "";
			//ExtraDataInvoice extraProduct = new ExtraDataInvoice();
			//extraProduct.serviceCharge = 0;
			//foreach (var item in pmsServiceSummaryInvoice)
			//{
			//	extraProduct.serviceCharge += item.ServiceCharge ?? 0;
			//	extraProduct.serviceChargeRate = item.ServiceRate;
			//	extraProduct.SubTotal = totalAmount;
			//	// Chuyển đổi ExtraProduct thành JSON
			//	extraProductJson = JsonConvert.SerializeObject(extraProduct, Formatting.Indented);
			//}
			invoiceItem.Extra = extraProductJson;

			return invoiceItem;
		}

		public static List<Product> CreateDataProduct (List<InvoiceDetail> pmsDataProduct)
		{
			List<Product> result = new List<Product>();
			int stt = 1;
			foreach (var item in pmsDataProduct)
			{
				Product product = new Product()
				{
					Code = "",
					No = stt.ToString(),
					Feature = "",
					ProdName = item.ItemName,
					ProdUnit = item.UnitName,
					ProdQuantity = item.Quantity?.ToString(),
					ProdPrice = item.Price?.ToString(),
					Discount = item.DiscountPercent?.ToString(),
					DiscountAmount = item.DiscountAmount?.ToString(),
					Total = item.BeforeTaxAmount?.ToString(),
					VATRate = item.TaxRate?.ToString(formatString),
					VATAmount = item.TaxAmount?.ToString(),
					Amount = item.AfterTaxAmount?.ToString(formatString),
					Extra = "",
				};
				stt += 1;
				result.Add(product);
			}
			return result;
		}


	}
}
