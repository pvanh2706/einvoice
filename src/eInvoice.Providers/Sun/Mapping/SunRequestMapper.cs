#nullable disable
#pragma warning disable CS0414 // Historical unused field keys retained for fixture provenance.
using eInvoice.Core.Contracts;
using eInvoice.Providers.Sun.Wire;
using eInvoice.Providers.Sun.Formatting;
using Newtonsoft.Json.Linq;
using System.Globalization;
namespace eInvoice.Providers.Sun.Mapping
{
    internal static class SunRequestMapper
    {
		private static string c0 = "c0";
		private static string c1 = "c1";
		private static string c2 = "c2";
		private static string c3 = "c3";
		private static string c4 = "c4";
		private static string c5 = "c5";
		private static string c6 = "c6";
		private static string c7 = "c7";
		private static string c8 = "c8";
		private static string c9 = "c9";

		public static SunCreateAndPublishInvoiceModel ConvertCreateAndPublishInvoiceRequest(SunMappingContext model)
        {
            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			SunConnectionInfo company = CompanyInfo(model);
			//List<ExtraFieldMapping> extraDataConfigMap = model.ExtraData?.ToList();
			List<ExtraFieldMapping> extraDataConfigMap = model.ExtraData?.ToList();
			SunInvoiceModel invoice = ConvertInvoiceSun(invoiceRequest, company, extraDataConfigMap);
            SunCreateAndPublishInvoiceModel result = new SunCreateAndPublishInvoiceModel
            {
                Company = CompanyInfo(model),
                Body = invoice,
            };

            return result;
        }
        private static SunInvoiceModel ConvertInvoiceSun_origin(CreateInvoiceRequest invoiceRequest, SunConnectionInfo company, bool Adjust_Decrease = false)
        {
            int multiplier = Adjust_Decrease ? -1 : 1;
            Invoice invoice = invoiceRequest.Invoice;
            // Chưa map key name, systemCode, isMTT, typeTT, Disscount, delaySign - cần xem xét luôn, aun
            SunInvoiceModel result = new SunInvoiceModel
            {
                idt_ = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                idt = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss"),
                bcode = "",
                bname = invoice.CompanyName,
                buyer = invoice.CustomerName,
                btax = invoice.CustomerTax,
                baddr = invoice.CustomerAddress,
                btel = invoice.CustomerPhone,
                bmail = invoice.CustomerEmail,
                bacc = invoice.BankAccount,
                bbank = invoice.BankName,
                mdvqhnsach = invoice.BudgetCode,
                socccd_dinhdanh = invoice.IdentityCard,
                so_hochieu = invoice.PassportNum,
                form = invoice.FormNo,
                serial = invoice.Serial,
                note = invoice.Notice,
				// sid = (invoice.Sid ?? "") != "" ? invoice.Sid : company.Username2 + invoice.FormNo + invoice.Serial + $"{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}",
				// sid = company.Username2 + invoice.FormNo + invoice.Serial + $"{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}",
				// Chuyển qua dùng ezInvoiceId làm sid theo yêu cầu Sun (bản guid rút gọn 32 ký tự)
				// Mục đích để truyền lại key khi phát hanh hóa đơn nếu timeout để tránh bị double hóa đơn
				sid = invoice.InvoiceId.Replace("-", "").Substring(0, 32),
				stax = company.TaxNumber,
                paym = invoice.PaymentMethod,
                curr = invoice.CurrencyCode,
                exrt = invoice.ExchangeRate ?? 0,
                sum = (invoice.BeforeTaxAmount ?? 0) * multiplier,
                sumv = (invoice.BeforeTaxAmount ?? 0) * multiplier,
                vat = (invoice.TaxAmount ?? 0) * multiplier,
                vatv = (invoice.TaxAmount ?? 0) * multiplier,
                total = (invoice.AfterTaxAmount ?? 0) * multiplier,
                totalv = (invoice.AfterTaxAmount ?? 0) * multiplier,
                discount = "",
                type = "01GTKT",
                seq = "",
                score = 0,
                word = NumberReader.NumberToText((double)((invoice.AfterTaxAmount ?? 0) * multiplier)),
                c8 = (int?)invoice.ServiceRate ?? 0,
                c9 = (invoice.ServiceCharge ?? 0) * multiplier,
                c2 = 0,//discount amount
                name = "",
                tax = ConvertSunTax(invoiceRequest, multiplier),
                items = ConvertSunInvoiceDetail(invoiceRequest, multiplier),
                delaySign = 1,
				taxc = company.TaxNumber,
				// api = 1 Mặc định trong tài liệu và aun = 0(Nếu cấp số trên hệ thống hóa đơn)
				api = 1,
                aun = 0,
			};
            if (invoiceRequest.HotelExtra != null)
            {
                result.c0 = invoiceRequest.HotelExtra.ArrivalDate;
                result.c1 = invoiceRequest.HotelExtra.DepartureDate;
                result.c3 = invoiceRequest.HotelExtra.RoomCode;
				result.c4 = invoiceRequest.HotelExtra.NumGuest?.ToString();
				result.c5 = invoiceRequest.HotelExtra.FolioNum;
            }
            if (invoiceRequest.SpecialTaxSummaries != null && invoiceRequest.SpecialTaxSummaries.Any())
            {
                result.c6 = (int?)invoiceRequest.SpecialTaxSummaries.FirstOrDefault(e => e.SpecialTaxRate != 0)?.SpecialTaxRate ?? 0;
                result.c7 = (invoiceRequest.SpecialTaxSummaries.Sum(e => e.SpecialTaxAmount) ?? 0) * multiplier;
            }
            return result;
		}

		private static SunInvoiceModel ConvertInvoiceSun(CreateInvoiceRequest invoiceRequest, SunConnectionInfo company, List<ExtraFieldMapping> extraDataConfigMap, bool Adjust_Decrease = false)
		{
			int multiplier = Adjust_Decrease ? -1 : 1;
			Invoice invoice = invoiceRequest.Invoice;
			// Chưa map key name, systemCode, isMTT, typeTT, Disscount, delaySign - cần xem xét luôn, aun
			SunInvoiceModel result = new SunInvoiceModel
			{
				idt_ = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
				idt = DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss"),
				bcode = "",
				bname = invoice.CompanyName,
				buyer = invoice.CustomerName,
				btax = invoice.CustomerTax,
				baddr = invoice.CustomerAddress,
				btel = invoice.CustomerPhone,
				bmail = invoice.CustomerEmail,
				bacc = invoice.BankAccount,
				bbank = invoice.BankName,
				mdvqhnsach = invoice.BudgetCode,
				socccd_dinhdanh = invoice.IdentityCard,
				so_hochieu = invoice.PassportNum,
				form = invoice.FormNo,
				serial = invoice.Serial,
				note = invoice.Notice,
				// sid = (invoice.Sid ?? "") != "" ? invoice.Sid : company.Username2 + invoice.FormNo + invoice.Serial + $"{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}",
				// sid = company.Username2 + invoice.FormNo + invoice.Serial + $"{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}",
				// Chuyển qua dùng ezInvoiceId làm sid theo yêu cầu Sun (bản guid rút gọn 32 ký tự)
				// Mục đích để truyền lại key khi phát hanh hóa đơn nếu timeout để tránh bị double hóa đơn
				sid = invoice.InvoiceId.Replace("-", "").Substring(0, 32),
				stax = company.TaxNumber,
				paym = invoice.PaymentMethod,
				curr = invoice.CurrencyCode,
				exrt = invoice.ExchangeRate ?? 0,
				sum = (invoice.BeforeTaxAmount ?? 0) * multiplier,
				sumv = (invoice.BeforeTaxAmount ?? 0) * multiplier,
				vat = (invoice.TaxAmount ?? 0) * multiplier,
				vatv = (invoice.TaxAmount ?? 0) * multiplier,
				total = (invoice.AfterTaxAmount ?? 0) * multiplier,
				totalv = (invoice.AfterTaxAmount ?? 0) * multiplier,
				discount = "",
				type = "01GTKT",
				seq = "",
				score = 0,
				word = NumberReader.NumberToText((double)((invoice.AfterTaxAmount ?? 0) * multiplier)),
				c8 = (int?)invoice.ServiceRate ?? 0,
				c9 = (invoice.ServiceCharge ?? 0) * multiplier,
				c2 = 0,//discount amount
				name = "",
				tax = ConvertSunTax(invoiceRequest, multiplier),
				items = ConvertSunInvoiceDetail(invoiceRequest, multiplier),
				delaySign = 1,
				taxc = company.TaxNumber,
				// api = 1 Mặc định trong tài liệu và aun = 0(Nếu cấp số trên hệ thống hóa đơn)
				api = 1,
				aun = 0,
			};

			bool haveConfigMapDataWithPartner = extraDataConfigMap != null && extraDataConfigMap.Any();
			bool haveExtraDataFromClient = invoiceRequest.ExtraData != null && invoiceRequest.ExtraData.Any();

			if (haveConfigMapDataWithPartner && haveExtraDataFromClient)
			{
				result.c0 = GetDataCustomField(extraDataConfigMap, invoiceRequest.ExtraData, c0);
				result.c1 = GetDataCustomField(extraDataConfigMap, invoiceRequest.ExtraData, c1);
				result.c3 = GetDataCustomField(extraDataConfigMap, invoiceRequest.ExtraData, c3);
				result.c4 = GetDataCustomField(extraDataConfigMap, invoiceRequest.ExtraData, c4);
				result.c5 = GetDataCustomField(extraDataConfigMap, invoiceRequest.ExtraData, c5);
			}
			else if (invoiceRequest.HotelExtra != null)
			{
				result.c0 = invoiceRequest.HotelExtra.ArrivalDate;
				result.c1 = invoiceRequest.HotelExtra.DepartureDate;
				result.c3 = invoiceRequest.HotelExtra.RoomCode;
				result.c4 = invoiceRequest.HotelExtra.NumGuest?.ToString();
				result.c5 = invoiceRequest.HotelExtra.FolioNum;
			}
			if (invoiceRequest.SpecialTaxSummaries != null && invoiceRequest.SpecialTaxSummaries.Any())
			{
				result.c6 = (int?)invoiceRequest.SpecialTaxSummaries.FirstOrDefault(e => e.SpecialTaxRate != 0)?.SpecialTaxRate ?? 0;
				result.c7 = (invoiceRequest.SpecialTaxSummaries.Sum(e => e.SpecialTaxAmount) ?? 0) * multiplier;
			}
			return result;
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

		private static List<SunTaxModel> ConvertSunTax(CreateInvoiceRequest invoice, int multiplier)
        {
            if (invoice == null || invoice.TaxSummaries == null) return new List<SunTaxModel>();
            return invoice.TaxSummaries.Select(e => new SunTaxModel
            {
                amt = (e.BeforeTaxAmount ?? 0) * multiplier,
                amtv = (e.BeforeTaxAmount ?? 0) * multiplier,
                vrt = (e.TaxRate ?? 0).ToString("#0.#####"),
                vrn = (e.TaxRate ?? 0).ToString("#0.#####") + "%",
                vat = (e.TaxAmount ?? 0) * multiplier,
                vatv = (e.TaxAmount ?? 0) * multiplier
            }).ToList();
        }
        private static List<SunInvoiceDetailModel> ConvertSunInvoiceDetail_origin(CreateInvoiceRequest invoice, int multiplier)
        {
            int stt = 1;
            decimal discount = 0;
            List<InvoiceDetail> details = invoice.Details;
            List<SunInvoiceDetailModel> detailData = new List<SunInvoiceDetailModel>();
            foreach (InvoiceDetail item in details)
            {
                string _transdate = null;
                if (string.IsNullOrEmpty(item.TransactionDate) == false)
                    _transdate = DateTime.ParseExact(item.TransactionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                SunInvoiceDetailModel _item = new SunInvoiceDetailModel
                {
                    line = stt,
                    transdate = _transdate,
                    type = "",
                    name = item.ItemName,
                    unit = item.UnitName,
                    quantity = item.Quantity ?? 0,
                    price = (item.Price ?? 0) * multiplier,
                    amount = (item.BeforeTaxAmount ?? 0) * multiplier,
                    amountv = (item.BeforeTaxAmount ?? 0) * multiplier,
                    vrt = (item.TaxRate ?? 0).ToString("#0.####"),
                    vrn = (item.TaxRate ?? 0).ToString("#0.####") + "%",
                    vat = (item.TaxAmount ?? 0) * multiplier,
                    vatv = (item.TaxAmount ?? 0) * multiplier,
                    total = (item.AfterTaxAmount ?? 0) * multiplier,
                    totalv = (item.AfterTaxAmount ?? 0) * multiplier,
                };
                detailData.Add(_item);
                stt++;
                discount += item.DiscountAmount ?? 0;
            }
            if (discount != 0)
            {
                SunInvoiceDetailModel _itemCK = new SunInvoiceDetailModel
                {
                    line = stt,
                    transdate = "",
                    type = "Chiết khấu",
                    name = "Chiết khấu",
                    unit = "",
                    quantity = 1,
                    price = discount * multiplier,
                    amount = discount * multiplier,
                    amountv = discount * multiplier,
                    vrt = "0",
                    vrn = "0%",
                    vat = 0,
                    vatv = 0,
                    total = discount * multiplier,
                    totalv = discount * multiplier,
                };
                detailData.Add(_itemCK);
                stt++;
            }
            decimal ServiceCharge = 0;
            decimal SvcTaxAmount = 0;
            decimal SvcAfterTaxAmount = 0;
            if (invoice.ServiceSummaries != null && invoice.ServiceSummaries.Count > 0)
            {
                foreach (ServiceSummary ServiceSummary in invoice.ServiceSummaries)
                {
                    try
                    {
                        ServiceCharge += ServiceSummary.ServiceCharge ?? 0;
                        SvcTaxAmount += ServiceSummary.TaxAmount ?? 0;
                        SvcAfterTaxAmount += ServiceSummary.AfterTaxAmount ?? 0;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
            if (ServiceCharge != 0)
            {
                decimal taxRate = invoice.ServiceSummaries != null ? invoice.ServiceSummaries[0].TaxRate ?? 0 : 0;
                decimal AftertaxSVC = SvcTaxAmount + ServiceCharge;

                SunInvoiceDetailModel _itemSVC = new SunInvoiceDetailModel
                {
                    line = stt,
                    transdate = "",
                    type = "",
                    name = "SVC Phí dịch vụ",
                    unit = "",
                    quantity = 1,
                    price = ServiceCharge * multiplier,
                    amount = ServiceCharge * multiplier,
                    amountv = ServiceCharge * multiplier,
                    vrt = taxRate.ToString("#0.##"),
                    vrn = taxRate.ToString("#0.##") + "%",
                    vat = SvcTaxAmount * multiplier,
                    vatv = SvcTaxAmount * multiplier,
                    total = SvcAfterTaxAmount * multiplier,
                    totalv = SvcAfterTaxAmount * multiplier,
                };
                detailData.Add(_itemSVC);
                stt++;
            }
            if (invoice.SpecialTaxSummaries != null && invoice.SpecialTaxSummaries.Count > 0)
            {
                SunInvoiceDetailModel _itemSpecialTax = invoice.SpecialTaxSummaries.GroupBy(e => 1)
                    .Select(e => new SunInvoiceDetailModel
                    {
                        line = stt,
                        transdate = "",
                        type = "",
                        name = "SVC Phí dịch vụ",
                        unit = "",
                        quantity = 1,
                        price = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
                        amount = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
                        amountv = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
                        vrt = (e.FirstOrDefault()?.TaxRate ?? 0).ToString("#0.##"),
                        vrn = (e.FirstOrDefault()?.TaxRate ?? 0).ToString("#0.##") + "%",
                        vat = (e.Sum(r => r.TaxAmount) ?? 0) * multiplier,
                        vatv = (e.Sum(r => r.TaxAmount) ?? 0) * multiplier,
                        total = (e.Sum(r => r.AfterTaxAmount) ?? 0) * multiplier,
                        totalv = (e.Sum(r => r.AfterTaxAmount) ?? 0) * multiplier,
                    }).FirstOrDefault();
                detailData.Add(_itemSpecialTax);
                stt++;
            }
            return detailData;
        }

		private static List<SunInvoiceDetailModel> ConvertSunInvoiceDetail(CreateInvoiceRequest invoice, int multiplier)
		{
			int stt = 1;
			decimal discount = 0;
			List<InvoiceDetail> details = invoice.Details;
			List<SunInvoiceDetailModel> detailData = new List<SunInvoiceDetailModel>();
			foreach (InvoiceDetail item in details)
			{
				string _transdate = null;
				if (string.IsNullOrEmpty(item.TransactionDate) == false)
					_transdate = DateTime.ParseExact(item.TransactionDate, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
				SunInvoiceDetailModel _item = new SunInvoiceDetailModel
				{
					line = stt,
					transdate = _transdate,
					type = "",
					name = item.ItemName,
					unit = item.UnitName,
					quantity = item.Quantity ?? 0,
					price = (item.Price ?? 0) * multiplier,
					amount = (item.BeforeTaxAmount ?? 0) * multiplier,
					amountv = (item.BeforeTaxAmount ?? 0) * multiplier,
					vrt = (item.TaxRate ?? 0).ToString("#0.####"),
					vrn = (item.TaxRate ?? 0).ToString("#0.####") + "%",
					vat = (item.TaxAmount ?? 0) * multiplier,
					vatv = (item.TaxAmount ?? 0) * multiplier,
					total = (item.AfterTaxAmount ?? 0) * multiplier,
					totalv = (item.AfterTaxAmount ?? 0) * multiplier,
				};
				detailData.Add(_item);
				stt++;
				discount += item.DiscountAmount ?? 0;
			}
			if (discount != 0)
			{
				SunInvoiceDetailModel _itemCK = new SunInvoiceDetailModel
				{
					line = stt,
					transdate = "",
					type = "Chiết khấu",
					name = "Chiết khấu",
					unit = "",
					quantity = 1,
					price = discount * multiplier,
					amount = discount * multiplier,
					amountv = discount * multiplier,
					vrt = "0",
					vrn = "0%",
					vat = 0,
					vatv = 0,
					total = discount * multiplier,
					totalv = discount * multiplier,
				};
				detailData.Add(_itemCK);
				stt++;
			}

			if (invoice.SpecialTaxSummaries != null && invoice.SpecialTaxSummaries.Count > 0)
			{
				SunInvoiceDetailModel _itemSpecialTax = invoice.SpecialTaxSummaries.GroupBy(e => 1)
					.Select(e => new SunInvoiceDetailModel
					{
						line = stt,
						transdate = "",
						type = "",
						name = "Thuế tiêu thụ đặc biệt",
						unit = "",
						quantity = 1,
						price = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
						amount = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
						amountv = (e.Sum(r => r.SpecialTaxAmount) ?? 0) * multiplier,
						vrt = (e.FirstOrDefault()?.TaxRate ?? 0).ToString("#0.##"),
						vrn = (e.FirstOrDefault()?.TaxRate ?? 0).ToString("#0.##") + "%",
						vat = (e.Sum(r => r.TaxAmount) ?? 0) * multiplier,
						vatv = (e.Sum(r => r.TaxAmount) ?? 0) * multiplier,
						total = (e.Sum(r => r.AfterTaxAmount) ?? 0) * multiplier,
						totalv = (e.Sum(r => r.AfterTaxAmount) ?? 0) * multiplier,
					}).FirstOrDefault();
				detailData.Add(_itemSpecialTax);
				stt++;
			}
			return detailData;
		}

		public static HealthCheckModel ConvertLoginRequest(SunMappingContext DataInput)
		{
			SiteConfiguration Company = DataInput.Company as SiteConfiguration;
			userInfo user = new userInfo
			{
				username = Company.Username,
				password = Company.Password
            };
            HealthCheckModel result = new HealthCheckModel
            {
                Body = user,
				Company = CompanyInfo(DataInput),
			};

            return result;
		}

		public static DownloadPDFModel ConvertDownloadPDFRequest(SunMappingContext DataInput, bool isInvoiceDraft)
		{
			SiteConfiguration Company = DataInput.Company as SiteConfiguration;
			DownloadPdfRequest pmsDownloadPDFRequest = DataInput.Data as DownloadPdfRequest;

            ParamDownloadPDF paramDownloadPDF = new ParamDownloadPDF()
            {
                sec = pmsDownloadPDFRequest.SearchCode,
                invtmp = isInvoiceDraft ? 1 : 0, // hóa đơn nháp thì truyền 1
                taxc = Company.TaxNumber,
			};
			DownloadPDFModel result = new DownloadPDFModel
			{
				Body = paramDownloadPDF,
				Company = CompanyInfo(DataInput),
			};

			return result;
		}


		public static DownloadPDFModel ConvertDownloadPDFDraftRequest(SunMappingContext DataInput)
		{
			SiteConfiguration Company = DataInput.Company as SiteConfiguration;
			CreateInvoiceRequest pmsDownloadPDFRequest = DataInput.Data as CreateInvoiceRequest;

			ParamDownloadPDF paramDownloadPDF = new ParamDownloadPDF()
			{
				sec = pmsDownloadPDFRequest.Invoice.SearchCode,
				invtmp = 1, // hóa đơn nháp thì truyền 1
				taxc = Company.TaxNumber,
			};
			DownloadPDFModel result = new DownloadPDFModel
			{
				Body = paramDownloadPDF,
				Company = CompanyInfo(DataInput),
			};

			return result;
		}

		public static VoidInvoiceModel ConvertVoidInvoiceRequest(SunMappingContext DataInput)
		{
			SiteConfiguration Company = DataInput.Company as SiteConfiguration;
			VoidInvoiceRequest pmsDownloadPDFRequest = DataInput.Data as VoidInvoiceRequest;

			ParamVoidInvoice paramVoidInvoice = new ParamVoidInvoice()
			{
				sid = pmsDownloadPDFRequest.Sid,
				typ = 3,
                rea = pmsDownloadPDFRequest.Reason,
				rdt = DataInput.Now.ToString("yyyy-MM-dd"),
			};
			VoidInvoiceModel result = new VoidInvoiceModel
			{
				Body = paramVoidInvoice,
				Company = CompanyInfo(DataInput),
			};

			return result;
		}

		public static AdjustInvoiceModel ConvertInvoiceAdjustRequest(SunMappingContext model, int AdjType)
		{
			AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			SunConnectionInfo company = CompanyInfo(model);
			bool Adjust_Decrease = AdjType == 1;//Điều chỉnh giảm
			List<ExtraFieldMapping> extraDataConfigMap = model.ExtraData?.ToList();


			SunInvoiceModel invoice = ConvertInvoiceSun(invoiceRequest, company, extraDataConfigMap, Adjust_Decrease);

			ParamAdjustData adj = new ParamAdjustData
			{
				seq = $"{invoiceRequest.Invoice.FormNo}-{invoiceRequest.Invoice.Serial}-{invoiceRequest.OriginalProviderInvoiceNumber}",
				rdt = model.Now.ToString("yyyy-MM-dd"),
				rea = AdjReason.GetValueOrDefault(AdjType),
				des = invoiceRequest.Invoice.SearchCode,
				sid = invoiceRequest.Invoice.Sid,
				adjtyp = AdjType.ToString(),
                typ = 2
			};

			SunAdjustInvoiceModel invoiceAdj = JObject.FromObject(invoice).ToObject<SunAdjustInvoiceModel>();
			invoiceAdj.adj = adj;
			invoiceAdj.invType = 4;
			invoiceAdj.adjType = AdjType;

			AdjustInvoiceModel result = new AdjustInvoiceModel
			{
				Company = CompanyInfo(model),
				Body = invoiceAdj,
			};

			return result;
		}

		public static ReplaceInvoiceModel ConvertInvoiceReplaceRequest(SunMappingContext model, int AdjType = 3)
		{
			AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
			SunConnectionInfo company = CompanyInfo(model);
			List<ExtraFieldMapping> extraDataConfigMap = model.ExtraData?.ToList();

			SunInvoiceModel invoice = ConvertInvoiceSun(invoiceRequest, company, extraDataConfigMap);

            ParamReplaceData adj = new ParamReplaceData
            {
                seq = $"{invoiceRequest.Invoice.FormNo}-{invoiceRequest.Invoice.Serial}-{invoiceRequest.OriginalProviderInvoiceNumber}",
                rdt = model.Now.ToString("yyyy-MM-dd"),
                rea = AdjReason.GetValueOrDefault(AdjType),
                des = invoiceRequest.Invoice.SearchCode,
                sid = invoiceRequest.Invoice.Sid,
                typ = 1
			};

			SunReplaceInvoiceModel invoiceAdj = JObject.FromObject(invoice).ToObject<SunReplaceInvoiceModel>();
			invoiceAdj.adj = adj;
			//invoiceAdj.invType = 4;
			//invoiceAdj.adjType = AdjType;

			ReplaceInvoiceModel result = new ReplaceInvoiceModel
			{
				Company = CompanyInfo(model),
				Body = invoiceAdj,
			};

			return result;
		}

		private static SunConnectionInfo CompanyInfo(SunMappingContext model)
		{
			return new SunConnectionInfo
			{
				CompanyId = Convert.ToString(model.Company.Id),
				TaxNumber = model.Company.TaxNumber,
				PartnerUrl = model.Company.PartnerUrl,
				Username = model.Company.Username,
				Password = model.Company.Password,
				PartnerUrl2 = model.Company.PartnerUrl2,
				Username2 = model.Company.Username2,
				Password2 = model.Company.Password2,
			};
		}

        public static Dictionary<int, string> AdjReason = new Dictionary<int, string>
        {
            {2,"Điều chỉnh tăng giá trị" },
            {1,"Điều chỉnh giảm giá trị" },
        };
    }
}
