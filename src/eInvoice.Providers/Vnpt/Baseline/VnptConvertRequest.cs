using eInvoice.Providers.Vnpt.Baseline.V5;
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
using BusinessService_V5;
using PortalService;
using PublishService;
using System;
using System.Collections.Generic;
using System.Linq;
namespace eInvoice.Providers.Vnpt.Baseline
{
    internal static class VnptConvertRequest
    {
        private const int NOT_CONVERT_TCVN3_TO_UNICODE = 0;
        public static List<string> guidNotSendServiceSummary = new List<string>
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
        public static bool IsNotSendServiceSummary (Guid? guidCheck)
        {
            string guidCheckStr = guidCheck == null ? "" : guidCheck.ToString().ToUpper();
            bool result = guidNotSendServiceSummary.Contains(guidCheckStr);
			return result;
		}
		public static ReplaceInvoiceActionRequest ConvertReplaceInvoiceRequest(dynamic model)
        {
            AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
            CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			ReplaceInv invoiceReplace = MapInvoiceReplace(invoiceRequest, invoiceRequest.Invoice);
            invoiceReplace.key = pmsInvoice.InvoiceId.ToString();



			ReplaceInvoiceActionRequest result = new ReplaceInvoiceActionRequest()
            {
                Body = new ReplaceInvoiceActionRequestBody
				{
                    fkey = invoiceRequest.OriginalInvoiceId,
                    Account = model.Company.Username,
                    ACpass = model.Company.Password,
                    xmlInvData = XmlHelper.Serialize(invoiceReplace),
                    username = model.Company.Username2,
                    pass = model.Company.Password2,
                    pattern = pmsInvoice.FormNo,
                    serial = pmsInvoice.Serial,
                    convert = NOT_CONVERT_TCVN3_TO_UNICODE,
                }
            };
            return result;
        }

        public static BusinessService_V5.ReplaceInvoiceActionRequest ConvertReplaceInvoiceRequestV5(dynamic model)
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

            var result = new BusinessService_V5.ReplaceInvoiceActionRequest()
            {
                Body = new BusinessService_V5.ReplaceInvoiceActionRequestBody
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
                }
            };
            return result;
        }

        public static BusinessService_V5.AdjustInvoiceActionRequest ConvertAdjustInvoiceRequest(dynamic model, int action)
        {
            AdjustOrReplaceInvoiceRequest invoiceRequest = model.Data as AdjustOrReplaceInvoiceRequest;
            CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			AdjustInv invoiceAdjust = MapInvoiceAdjust(invoiceRequest, invoiceRequest.Invoice);
            invoiceAdjust.key = pmsInvoice.InvoiceId.ToString();
            invoiceAdjust.Type = ConvertInvoiceActionToTypeAdjust(action);

			BusinessService_V5.AdjustInvoiceActionRequest result = new BusinessService_V5.AdjustInvoiceActionRequest()
            {
                Body = new BusinessService_V5.AdjustInvoiceActionRequestBody
				{
                    fkey = invoiceRequest.OriginalInvoiceId,
                    Account = model.Company.Username,
                    ACpass = model.Company.Password,
                    xmlInvData = XmlHelper.Serialize(invoiceAdjust),
                    username = model.Company.Username2,
                    pass = model.Company.Password2,
                    pattern = pmsInvoice.FormNo,
                    serial = pmsInvoice.Serial,
                    convert = NOT_CONVERT_TCVN3_TO_UNICODE,
                }
            };
            return result;
        }

        public static BusinessService_V5.AdjustInvoiceActionRequest ConvertAdjustInvoiceRequestV5(dynamic model, int action)
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

            var result = new BusinessService_V5.AdjustInvoiceActionRequest()
            {
                Body = new BusinessService_V5.AdjustInvoiceActionRequestBody
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
                }
            };
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

		public static cancelInvNoPayRequest ConvertVoidInvoiceNoPayRequest(dynamic model)
		{
			return new cancelInvNoPayRequest
			{
				Body = new cancelInvNoPayRequestBody
				{
					fkey = model.Data.InvoiceId,
					Account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}

		public static cancelInvRequest ConvertVoidInvoiceRequest(dynamic model)
        {
            return new cancelInvRequest
            {
                Body = new cancelInvRequestBody
                {
                    fkey = model.Data.InvoiceId,
					Account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

		public static unConfirmPaymentRequest ConvertUnConfirmPaymentRequest(dynamic model)
		{
			return new unConfirmPaymentRequest
			{
				Body = new unConfirmPaymentRequestBody
				{
					lstInvToken = $"{model.Data.FormNo};{model.Data.Serial};{model.Data.ProviderInvoiceNumber}",
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}

		public static deleteInvoiceByFkeyRequest ConvertDeleteInvoiceRequest(dynamic model)
		{
			return new deleteInvoiceByFkeyRequest
			{
				Body = new deleteInvoiceByFkeyRequestBody
				{
					lstFkey = model.Data.InvoiceId,
					Account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
				}
			};
		}
		public static PublishService_V5.deleteInvoiceByFkeyRequest ConvertDeleteInvoiceRequestV5(dynamic model)
		{
			return new PublishService_V5.deleteInvoiceByFkeyRequest
			{
				Body = new PublishService_V5.deleteInvoiceByFkeyRequestBody
				{
					lstFkey = model.Data.Invoice.InvoiceId.ToString(),
					Account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
				}
			};
		}


		public static PublishService_V5.deleteInvoiceByFkeyRequest ConvertDeleteInvoiceAPIRequest(dynamic model)
		{
			return new PublishService_V5.deleteInvoiceByFkeyRequest
			{
				Body = new PublishService_V5.deleteInvoiceByFkeyRequestBody
				{
					lstFkey = model.Data.InvoiceId,
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
				}
			};
		}

		public static PublishService_V5.GetInvDataByFkeyRequest ConvertGetInvoiceByFkeyRequestV5(dynamic model)
		{
			return new PublishService_V5.GetInvDataByFkeyRequest
			{
				Body = new PublishService_V5.GetInvDataByFkeyRequestBody
				{
					fkey = model.Data.Invoice.InvoiceId.ToString(),
					account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					accPass = model.Company.Password,
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
					pattern = model.Data.Invoice.FormNo,
				}
			};
		}

		public static PublishService_V5.PublishInvFkeyRequest ConvertPublishInvoiceTT78Request(dynamic model)
		{
			return new PublishService_V5.PublishInvFkeyRequest
			{
				Body = new PublishService_V5.PublishInvFkeyRequestBody
				{
					lsFkey = model.Data.InvoiceId,
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = model.Data.FormNo,
					serial = model.Data.Serial,
				}
			};
		}
		public static PublishInvFkeyRequest ConvertPublishInvoiceRequest(dynamic model)
		{
			return new PublishInvFkeyRequest
			{
				Body = new PublishInvFkeyRequestBody
				{
					lsFkey = model.Data.InvoiceId,
					Account = model.Data.UserInfo != null ? model.Data.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = model.Data.FormNo,
					serial = model.Data.Serial,
				}
			};
		}

		public static PublishService_TLYT.publishInvByFkeyRequest ConvertPublishInvoiceReceiptRequest(dynamic model)
		{
			PublishService_TLYT.ArrayOfString stringList = new PublishService_TLYT.ArrayOfString();
			stringList.Add(model.Data.InvoiceId);

			return new PublishService_TLYT.publishInvByFkeyRequest
			{
				Body = new PublishService_TLYT.publishInvByFkeyRequestBody
				{
					invFkeys = stringList,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = model.Data.FormNo,
					serial = model.Data.Serial,
				}
			};
		}

		public static BusinessService_V5.cancelInvNoPayRequest ConvertVoidInvoiceRequestV5(dynamic model)
        {
            return new BusinessService_V5.cancelInvNoPayRequest
			{
                Body = new BusinessService_V5.cancelInvNoPayRequestBody
                {
                    fkey = model.Data.InvoiceId,
                    Account = model.Company.Username,
                    ACpass = model.Company.Password,
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

		public static downloadNewInvPDFFkeyRequest ConvertDownloadPDFDraftRequest(dynamic model)
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

		public static downloadNewInvPDFFkeyRequest ConvertPreviewPDFInvoiceDraftRequest(dynamic model)
		{
			return new downloadNewInvPDFFkeyRequest
			{
				Body = new downloadNewInvPDFFkeyRequestBody
				{
					fkey = model.Data.Invoice.InvoiceId.ToString(),
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}

		public static downloadInvPDFFkeyRequest ConvertDownloadPDFRequest(dynamic model)
        {
            return new downloadInvPDFFkeyRequest
            {
                Body = new downloadInvPDFFkeyRequestBody
                {
                    fkey = model.Data.InvoiceId,
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

        public static PortalService_V5.downloadInvPDFFkeyRequest ConvertDownloadPDFRequestV5(dynamic model)
        {
            return new PortalService_V5.downloadInvPDFFkeyRequest
            {
                Body = new PortalService_V5.downloadInvPDFFkeyRequestBody
                {
                    fkey = model.Data.InvoiceId,
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

        public static PortalService_V5.downloadNewInvPDFFkeyRequest ConvertDownloadPDFInvoiceDraftRequestV5(dynamic model)
        {
            return new PortalService_V5.downloadNewInvPDFFkeyRequest
            {
                Body = new PortalService_V5.downloadNewInvPDFFkeyRequestBody
                {
                    fkey = model.Data.InvoiceId,
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

		public static PortalService_V5.downloadNewInvPDFFkeyRequest ConvertPreviewPDFInvoiceDraftRequestV5(dynamic model)
		{
			return new PortalService_V5.downloadNewInvPDFFkeyRequest
			{
				Body = new PortalService_V5.downloadNewInvPDFFkeyRequestBody
				{
					fkey = model.Data.Invoice.InvoiceId.ToString(),
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
				}
			};
		}

		public static PortalService_V5.downloadInvPDFFkeyNoPayRequest ConvertDownloadPDFRequestV5NoPay(dynamic model)
        {
            return new PortalService_V5.downloadInvPDFFkeyNoPayRequest
            {
                Body = new PortalService_V5.downloadInvPDFFkeyNoPayRequestBody
                {
                    fkey = model.Data.InvoiceId,
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }
		public static ImportInvByPatternRequest ConvertCreateInvoiceRequest(dynamic model)
		{

			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			Invoice invoice = MapInvoice(invoiceRequest, invoiceRequest.Invoice);

			Inv inv = new Inv
			{
				Invoice = invoice,
				key = pmsInvoice.InvoiceId.ToString(),
			};

			Invoices invoices = new Invoices
			{
				Inv = inv
			};

			List<Invoices> vnptBody = new List<Invoices>();
			vnptBody.Add(invoices);

			ImportInvByPatternRequest result = new ImportInvByPatternRequest()
			{
				Body = new ImportInvByPatternRequestBody
				{
					Account = invoiceRequest.UserInfo != null ? invoiceRequest.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					xmlInvData = XmlHelper.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
				}
			};
			return result;
		}

		public static PublishService_TLYT.ImportInvRequest ConvertCreateInvoiceReceiptRequest(dynamic model)
		{

			CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			Invoice invoice = MapInvoice(invoiceRequest, invoiceRequest.Invoice);

			Inv inv = new Inv
			{
				Invoice = invoice,
				key = pmsInvoice.InvoiceId.ToString(),
			};

			Invoices invoices = new Invoices
			{
				Inv = inv
			};

			List<Invoices> vnptBody = new List<Invoices>();
			vnptBody.Add(invoices);

			PublishService_TLYT.ImportInvRequest result = new PublishService_TLYT.ImportInvRequest()
			{
				Body = new PublishService_TLYT.ImportInvRequestBody
				{
					xmlInvData = XmlHelper.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
					tkTao = invoiceRequest.UserInfo != null ? invoiceRequest.UserInfo.UserName : model.Company.Username,
					pattern = pmsInvoice.FormNo,
					serial = pmsInvoice.Serial,
				}
		};
			return result;
		}

		public static ImportAndPublishInvRequest ConvertCreateAndPublishInvoiceRequest(dynamic model)
        {

            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
            CoreInvoice pmsInvoice = invoiceRequest.Invoice;
            Invoice invoice = MapInvoice(invoiceRequest, invoiceRequest.Invoice);

            Inv inv = new Inv
            {
                Invoice = invoice,
                key = pmsInvoice.InvoiceId.ToString(),
            };

            Invoices invoices = new Invoices
            {
                Inv = inv
            };

            List<Invoices> vnptBody = new List<Invoices>();
            vnptBody.Add(invoices);

            ImportAndPublishInvRequest result = new ImportAndPublishInvRequest()
            {
                Body = new ImportAndPublishInvRequestBody
                {
					Account = invoiceRequest.UserInfo != null ? invoiceRequest.UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					xmlInvData = XmlHelper.Serialize(invoices),
                    username = model.Company.Username2,
                    password = model.Company.Password2,
                    pattern = pmsInvoice.FormNo,
                    serial = pmsInvoice.Serial,
                    convert = NOT_CONVERT_TCVN3_TO_UNICODE,
                }
            };
            return result;
        }

		public static ImportAndPublishInvRequest ConvertCreateAndPublishMultiInvoiceRequest(dynamic model)
		{
			List<CreateInvoiceRequest> invoiceRequest = model.Data as List<CreateInvoiceRequest>;
			// CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
			List<Inv> invs = new List<Inv>();

            foreach (CreateInvoiceRequest item in invoiceRequest)
            {
				CoreInvoice pmsInvoice = item.Invoice;
				Invoice invoice = MapInvoice(item, item.Invoice);
				Inv inv = new Inv
				{
					Invoice = invoice,
					key = pmsInvoice.InvoiceId.ToString(),
				};
                invs.Add(inv);
			}


            MultiInvoices invoices = new MultiInvoices();
            invoices.Inv = invs;

			//Invoices invoices = new Invoices
			//{
			//	Inv = inv
			//};

			//List<Invoices> vnptBody = new List<Invoices>();
			//vnptBody.Add(invoices);

			ImportAndPublishInvRequest result = new ImportAndPublishInvRequest()
			{
				Body = new ImportAndPublishInvRequestBody
				{
					Account = invoiceRequest[0].UserInfo != null ? invoiceRequest[0].UserInfo.UserName : model.Company.Username,
					ACpass = model.Company.Password,
					xmlInvData = XmlHelper.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = invoiceRequest[0].Invoice.FormNo,
					serial = invoiceRequest[0].Invoice.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
				}
			};
			return result;
		}

		public static PublishService_V5.ImportAndPublishInvRequest ConvertCreateAndPublishInvoiceRequestV5(dynamic model)
        {

            CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
            CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;
			//DSHDon invoices = new DSHDon
			bool isSendServiceSummary = !IsNotSendServiceSummary(model.Company.Id);
			var invoices = new DSHDon
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

            var result = new PublishService_V5.ImportAndPublishInvRequest()
            {
                Body = new PublishService_V5.ImportAndPublishInvRequestBody
                {
                    Account = model.Company.Username,
                    ACpass = model.Company.Password,
					//  xmlInvData = XmlHelper.Serialize(invoices),
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
		public static PublishService_V5.ImportAndPublishInvRequest ConvertCreateAndPublishInvoiceMultiRequestV5(dynamic model)
		{

			List<HDon> hDons = new List<HDon>();
			List<CreateInvoiceRequest> invoiceRequest = model.Data as List<CreateInvoiceRequest>;
			List<ExtraFieldMapping> extraDataMap = model.ExtraData as List<ExtraFieldMapping>;

			foreach (CreateInvoiceRequest item in invoiceRequest)
			{
				CoreInvoice pmsInvoice = item.Invoice;
				HDon hDon = new HDon
				{
					key = pmsInvoice.InvoiceId.ToString(),
					DLHDon = new DLHDon
					{
						TTChung = GetTTChung(pmsInvoice),
						NDHDon = GetNDHDonMulti(item, extraDataMap)
					}
				};
				hDons.Add(hDon);
			}
			DSHDonMulti invoices = new DSHDonMulti();
			invoices.HDon = hDons;

			CoreInvoice pmsInvoiceFirst = invoiceRequest[0].Invoice;
			var result = new PublishService_V5.ImportAndPublishInvRequest()
			{
				Body = new PublishService_V5.ImportAndPublishInvRequestBody
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					xmlInvData = XmlHelperObjectMulti.Serialize(invoices),
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = pmsInvoiceFirst.FormNo,
					serial = pmsInvoiceFirst.Serial,
					convert = NOT_CONVERT_TCVN3_TO_UNICODE,
				}
			};
			return result;
		}
		public static PublishService_V5.ImportInvByPatternRequest ConvertCreateInvoiceRequestV5(dynamic model)
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

            var result = new PublishService_V5.ImportInvByPatternRequest()
            {
                Body = new PublishService_V5.ImportInvByPatternRequestBody
                {
                    Account = model.Company.Username,
                    ACpass = model.Company.Password,
					//  xmlInvData = XmlHelper.Serialize(invoices),
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
		/// <summary>
		/// Không dùng chung hàm với phát hành hóa đơn do khác thẻ
		/// </summary>
		/// <param name="invoiceRequest"></param>
		/// <param name="pmsInvoice"></param>
		/// <returns></returns>
		private static ReplaceInv MapInvoiceReplace(CreateInvoiceRequest invoiceRequest, CoreInvoice pmsInvoice)
		{
			//HotelReservationModel hotelReservation = pmsInvoice.HotelReservation;
			HotelInformation extraData = invoiceRequest.HotelExtra;
			DateTime now = VnptMappingScope.Now;
			var taxSummary = invoiceRequest.TaxSummaries;
			ReplaceInv invoice = new ReplaceInv
			{
				CusCode = pmsInvoice.InvoiceId.ToString(),
				Buyer = pmsInvoice.CustomerName ?? "",
				CusName = pmsInvoice.CompanyName ?? "",
				CusAddress = pmsInvoice.CustomerAddress ?? "",
				CusPhone = pmsInvoice.CustomerPhone ?? "",
				CusTaxCode = pmsInvoice.CustomerTax ?? "",
				PaymentMethod = pmsInvoice.PaymentMethod ?? "",
				KindOfService = now.ToString("MM/yyyy"),
				CurrencyUnit = pmsInvoice.CurrencyCode ?? "",
				Arrival = extraData?.ArrivalDate,
				Departure = extraData?.DepartureDate,
				FolioNo = extraData?.FolioNum,
				QuestQuantity = "0",
				RoomNo = extraData?.RoomCode,
				EmailDeliver = pmsInvoice.CustomerEmail ?? "",
				// ServicechargeAmount = (pmsInvoice.ServiceCharge ?? 0).ToString(),
				Products = ConvertDetail(invoiceRequest.Details),
				Total = (pmsInvoice.SubAmount ?? 0).ToString(), // Tài liệu hỏi lại là lấy tiền trước thuế phí
				DiscountAmount = invoiceRequest.Details.Sum(e => e.DiscountAmount).ToString(),
				VATRate = (pmsInvoice.TaxRate ?? 0).ToString(),
				VATAmount = (pmsInvoice.TaxAmount ?? 0).ToString(),
				Amount = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
				AmountInWords = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0),
				PaymentStatus = "1",
				ArisingDate = pmsInvoice.InvoiceDate ?? "",
				CusBankName = pmsInvoice.BankName ?? "",
				CusBankNo = pmsInvoice.BankAccount ?? "",
				GrossValue8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.BeforeTaxAmount.ToString(),
				VatAmount8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.TaxAmount.ToString(),
				GrossValue10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.BeforeTaxAmount.ToString(),
				VatAmount10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.TaxAmount.ToString(),
			};
			return invoice;
		}
        /// <summary>
        /// Không dùng chung hàm với phát hành hóa đơn do khác thẻ
        /// </summary>
        /// <param name="invoiceRequest"></param>
        /// <param name="pmsInvoice"></param>
        /// <returns></returns>
		private static AdjustInv MapInvoiceAdjust(CreateInvoiceRequest invoiceRequest, CoreInvoice pmsInvoice)
		{
			//HotelReservationModel hotelReservation = pmsInvoice.HotelReservation;
			HotelInformation extraData = invoiceRequest.HotelExtra;
			// TaxSummary taxSummary = invoiceRequest.TaxSummaries.FirstOrDefault();
			DateTime now = VnptMappingScope.Now;
			var taxSummary = invoiceRequest.TaxSummaries;
			AdjustInv invoice = new AdjustInv
			{
				CusCode = pmsInvoice.InvoiceId.ToString(),
				Buyer = pmsInvoice.CustomerName ?? "",
				CusName = pmsInvoice.CompanyName ?? "",
				CusAddress = pmsInvoice.CustomerAddress ?? "",
				CusPhone = pmsInvoice.CustomerPhone ?? "",
				CusTaxCode = pmsInvoice.CustomerTax ?? "",
				PaymentMethod = pmsInvoice.PaymentMethod ?? "",
				KindOfService = now.ToString("MM/yyyy"),
				CurrencyUnit = pmsInvoice.CurrencyCode ?? "",
				Arrival = extraData?.ArrivalDate,
				Departure = extraData?.DepartureDate,
				FolioNo = extraData?.FolioNum,
				QuestQuantity = "0",
				RoomNo = extraData?.RoomCode,
				EmailDeliver = pmsInvoice.CustomerEmail ?? "",
				// ServicechargeAmount = (pmsInvoice.ServiceCharge ?? 0).ToString(),
				Products = ConvertDetail(invoiceRequest.Details),
				Total = (pmsInvoice.SubAmount ?? 0).ToString(), // Tài liệu hỏi lại là lấy tiền trước thuế phí
				DiscountAmount = invoiceRequest.Details.Sum(e => e.DiscountAmount).ToString(),
				VATRate = (pmsInvoice.TaxRate ?? 0).ToString(),
				VATAmount = (pmsInvoice.TaxAmount ?? 0).ToString(),
				Amount = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
				AmountInWords = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0),
				PaymentStatus = "1",
				ArisingDate = pmsInvoice.InvoiceDate ?? "",
				CusBankName = pmsInvoice.BankName ?? "",
				CusBankNo = pmsInvoice.BankAccount ?? "",
				GrossValue8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.BeforeTaxAmount.ToString(),
				VatAmount8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.TaxAmount.ToString(),
				GrossValue10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.BeforeTaxAmount.ToString(),
				VatAmount10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.TaxAmount.ToString(),
			};
			return invoice;
		}

		private static Invoice MapInvoice(CreateInvoiceRequest invoiceRequest, CoreInvoice pmsInvoice)
        {
            //HotelReservationModel hotelReservation = pmsInvoice.HotelReservation;
            HotelInformation extraData = invoiceRequest.HotelExtra;
			var taxSummary = invoiceRequest.TaxSummaries;
			DateTime now = VnptMappingScope.Now;
			Invoice invoice = new Invoice
            {
                CusCode = pmsInvoice.InvoiceId.ToString(),
				Buyer = pmsInvoice.CustomerName ?? "",
				CusName = pmsInvoice.CompanyName ?? "",
                CusAddress = pmsInvoice.CustomerAddress ?? "",
                CusPhone = pmsInvoice.CustomerPhone ?? "",
                CusTaxCode = pmsInvoice.CustomerTax ?? "",
                PaymentMethod = pmsInvoice.PaymentMethod ?? "",
				KindOfService = now.ToString("MM/yyyy"),
				CurrencyUnit = pmsInvoice.CurrencyCode ?? "",
                Arrival = extraData?.ArrivalDate,
                Departure = extraData?.DepartureDate,
                FolioNo = extraData?.FolioNum,
				QuestQuantity = "0",
				RoomNo = extraData?.RoomCode,
				//EmailDeliver = pmsInvoice.CustomerEmail ?? "", bỏ do VNPT TLYT yêu cầu
				//ServicechargeAmount = (pmsInvoice.ServiceCharge ?? 0).ToString(),
				Products = ConvertDetail(invoiceRequest.Details),
				Total = (pmsInvoice.SubAmount ?? 0).ToString(), // Tài liệu hỏi lại là lấy tiền trước thuế phí
				DiscountAmount = invoiceRequest.Details.Sum(e => e.DiscountAmount).ToString(),
				VATRate = pmsInvoice.TaxRate == null ? null : ((int)(pmsInvoice.TaxRate ?? 0)).ToString(),
				VATAmount = pmsInvoice.TaxAmount == null ? null : (pmsInvoice.TaxAmount ?? 0).ToString(),
				Amount = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
				AmountInWords = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0),
				PaymentStatus = "1",
				ArisingDate = pmsInvoice.InvoiceDate ?? "",
                CusBankName = pmsInvoice.BankName ?? "",
                CusBankNo = pmsInvoice.BankAccount ?? "",
				GrossValue8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.BeforeTaxAmount.ToString(),
				VatAmount8 = taxSummary.FirstOrDefault(e => e.TaxRate == 8)?.TaxAmount.ToString(),
				GrossValue10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.BeforeTaxAmount.ToString(),
				VatAmount10 = taxSummary.FirstOrDefault(e => e.TaxRate == 10)?.TaxAmount.ToString(),

				//ArisingDate = pmsInvoice.InvoiceDate ?? "",
				//DiscountRate = details.Sum(e => e.DiscountPercent).ToString(),
				//DiscountAmount = details.Sum(e => e.DiscountAmount).ToString(),
				//GrossValue = details.Where(e => e.TaxRate != 5 && e.TaxRate != 10).Sum(e => e.BeforeTaxAmount).ToString(),
				//GrossValue5 = details.Where(e => e.TaxRate == 5).Sum(e => e.BeforeTaxAmount).ToString(),
				//GrossValue10 = details.Where(e => e.TaxRate == 10).Sum(e => e.BeforeTaxAmount).ToString(),
				//VatAmount0 = details.Where(e => e.TaxRate != 5 && e.TaxRate != 10).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount5 = details.Where(e => e.TaxRate == 5).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount10 = details.Where(e => e.TaxRate == 10).Sum(e => Math.Round(e.TaxAmount + (e.ServiceCharge ?? 0) * e.TaxRate / 100, 0, MidpointRounding.AwayFromZero)).ToString(),
				//VatAmount30 = "",
				//ExchangeRate = (pmsInvoice.ExchangeRate ?? 0).ToString(),
				//ConvertedAmount = ((pmsInvoice.AfterTaxAmount ?? 0) * (pmsInvoice.ExchangeRate ?? 0)).ToString(),
				//Note = pmsInvoice.Notice ?? "",
			};
            if (pmsInvoice.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
            {
                invoice.Extra = invoiceRequest.Details[0].ItemName;
			}
			if (pmsInvoice.InvoiceType != (int)INV_TYPE.INV_RECEIPT)
			{
				invoice.EmailDeliver = pmsInvoice.CustomerEmail ?? "";
			}
			return invoice;
        }

        private static List<Product> ConvertDetail(List<InvoiceDetail> details)
        {
            return details.Select((item, index) => new Product()
            {
                ProdName = item.ItemName,
                ProdUnit = item.UnitName,
                ProdQuantity = item.Quantity.ToString(),
                ProdPrice = item.Price.ToString(),
                Total = item.BeforeTaxAmount.ToString(),
                //Discount = item.DiscountPercent.ToString(),
                //DiscountAmount = item.DiscountAmount.ToString(),
                VATRate = item.TaxRate == null ? null :((int)(item.TaxRate ?? 0)).ToString(),
                VATAmount = item.TaxAmount == null ? null : item.TaxAmount.ToString(),
                Amount = item.AfterTaxAmount.ToString(),
                //Remark = item.Note,
                Extra1 = item.TransactionDate,
                //Extra2 = (index + 1).ToString(),
                IsSum = (item.IsItemSpecialTax ?? false) ? "4" : null,
            }).ToList();
        }

        public static getPublishInvsRequest ConvertFormNoSerialRequest(dynamic model)
        {
            return new getPublishInvsRequest()
            {
                Body = new getPublishInvsRequestBody()
                {
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

        public static BusinessService_V5.getPublishInvsRequest ConvertFormNoSerialRequestV5(dynamic model)
        {
            return new BusinessService_V5.getPublishInvsRequest()
            {
                Body = new BusinessService_V5.getPublishInvsRequestBody()
                {
                    userName = model.Company.Username2,
                    userPass = model.Company.Password2,
                }
            };
        }

        private static TTChung GetTTChung(CoreInvoice pmsInvoice)
        {
            return new TTChung()
            {
                NBKe = ConvertToV5DateTime(pmsInvoice.InvoiceDate),
                DVTTe = pmsInvoice.CurrencyCode ?? "",
                TGia = (pmsInvoice.ExchangeRate ?? 0).ToString(),
                HTTToan = pmsInvoice.PaymentMethod ?? ""
            };
        }

        private static NDHDon GetNDHDon(CreateInvoiceRequest pmsRequest, List<ExtraFieldMapping> extraDataMap, bool isSendServiceSummary)
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
			List<TTin> extraData = new List<TTin>();
			if (extraDataMap != null && extraDataMap.Any() && pmsRequest.ExtraData != null && pmsRequest.ExtraData.Any())
			{
				var extraDataMapPMS = pmsRequest.ExtraData
					.Join(extraDataMap, p => p.Code, e => e.Code, (p, e) => new
					{
						e.Code,
						e.ThirdPartyCode,
						p.StringValue,
						e.ThirdPartyDataType
					})
					.ToList();
				foreach (var itemMap in extraDataMapPMS.Where(e => !e.ThirdPartyCode.Contains(".")))
				{
					//  nMua[itemMap.ThirdPartyCode] = itemMap.StringValue;
					((IDictionary<string, object>)nMua).Add(itemMap.ThirdPartyCode, itemMap.StringValue);
				}

				extraData = extraDataMapPMS.Where(e => e.ThirdPartyCode.Contains(".")).Select(e => new TTin
				{
					TTruong = e.ThirdPartyCode,
					KDLieu = e.ThirdPartyDataType,
					DLieu = e.StringValue,
				}).ToList();
			}

			if (extraData.Any(e => e.TTruong.Contains("NMua")))
			{
				TTKhac extraNMua = TTKhac(extraData, "NMua");
				((IDictionary<string, object>)nMua).Add("TTKhac", extraNMua);
			}

			object NMua = nMua;
            return new NDHDon()
            {
                NBan = null,
                NMua = NMua,
                DSHHDVu = new DSHHDVu
                {
                    HHDVu = GetDSHHDVu(pmsRequest)
                },

                TToan = new TToan()
                {
                    THTTLTSuat = new THTTLTSuat
                    {
                        LTSuat = GetLTSuat(pmsInvoice, pmsRequest.TaxSummaries),
                    },
                    TgTCThue = pmsInvoice.BeforeTaxAmount.ToString(),
                    TgTThue = (pmsInvoice.TaxAmount ?? 0).ToString(),
                    TgTTTBSo = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
                    TgTTTBChu = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0)
                }
            };
        }

		/// <summary>
		/// Lấy extra của các thẻ cha
		/// </summary>
		/// <returns></returns>
		private static TTKhac TTKhac(List<TTin> extraDataMTT, string Parent)
		{
			List<TTin> ttin = extraDataMTT.Where(e => e.TTruong.Contains(Parent)).Select(e => new TTin
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

		private static NDHDon GetNDHDonMulti(CreateInvoiceRequest pmsRequest, List<ExtraFieldMapping> extraDataMap)
        {
            var company = VnptMappingScope.Company as SiteConfiguration;
            var pmsInvoice = pmsRequest.Invoice;

			return new NDHDon()
			{
				NBan = null,
				NMua = new NMua
				{
					Ten = pmsInvoice.CompanyName,
					MST = pmsInvoice.CustomerTax,
					DChi = pmsInvoice.CustomerAddress,
					SDThoai = pmsInvoice.CustomerPhone,
					DCTDTu = pmsInvoice.CustomerEmail,
					HVTNMHang = pmsInvoice.CustomerName,
					STKNHang = pmsInvoice.BankAccount,
					TNHang = pmsInvoice.BankName,
				},
				DSHHDVu = new DSHHDVu
				{
					HHDVu = GetDSHHDVu(pmsRequest)
				},
				TToan = new TToan()
				{
                    THTTLTSuat = new THTTLTSuat
                    {
                        LTSuat = GetLTSuat(pmsInvoice, pmsRequest.TaxSummaries),
                    },
                    TgTCThue = pmsInvoice.BeforeTaxAmount.ToString(),
					TgTThue = (pmsInvoice.TaxAmount ?? 0).ToString(),
					TgTTTBSo = (pmsInvoice.AfterTaxAmount ?? 0).ToString(),
					TgTTTBChu = MoneyToText.DocTienBangChu(pmsInvoice.AfterTaxAmount ?? 0)
				}
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

		public static List<HHDVu> GetDSHHDVu(CreateInvoiceRequest invoiceRequest)
        {
            List<HHDVu> dsHH = new List<HHDVu>();
			bool IsTaxReduction = invoiceRequest.Invoice.IsTaxReduction43 ?? false;
            for (int i = 0; i < invoiceRequest.Details.Count; i++)
            {
				dsHH.Add(new HHDVu
				{
					TChat = "1",
					STT = (i + 1).ToString(),
					THHDVu = invoiceRequest.Details[i].ItemName,
					DVTinh = invoiceRequest.Details[i].UnitName != null ? invoiceRequest.Details[i].UnitName.ToString() : "",
					SLuong = invoiceRequest.Details[i].Quantity.ToString(),
					DGia = invoiceRequest.Details[i].Price.ToString(),
					TLCKhau = (invoiceRequest.Details[i].DiscountPercent ?? 0) == 0 ? null : invoiceRequest.Details[i].DiscountPercent.ToString(),
					STCKhau = invoiceRequest.Details[i].DiscountAmount.ToString(),
					ThTien = invoiceRequest.Details[i].BeforeTaxAmount.ToString(),
					TSuat = IsTaxReduction ? "-1" : invoiceRequest.Details[i].TaxRate.ToString(),
					TThue = invoiceRequest.Details[i].TaxAmount.ToString(),
					TSThue = invoiceRequest.Details[i].AfterTaxAmount.ToString()
				});
            }
			if (invoiceRequest.Invoice.IsTaxReduction43 ?? false)
			{
				dsHH.Add(new HHDVu
				{
					TChat = "4",
					STT = (invoiceRequest.Details.Count + 1).ToString(),
					MHHDVu = "GHICHUNQ43",
					THHDVu = invoiceRequest.Invoice.TaxReductionDescription != null ? invoiceRequest.Invoice.TaxReductionDescription : "",
					TSuat = "-1",
				});
			}
            return dsHH;
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

		private static void AddProperty(ExpandoObject obj, string propertyName, object propertyValue)
		{
			var expandoDict = obj as IDictionary<string, object>;
			expandoDict[propertyName] = propertyValue;
		}


		public static PublishService_V5.GetMCCQThueByFkeysRequest ConvertSearchByIdRequest(dynamic model)
		{
			SearchInvoiceRequest invoiceRequest = model.Data as SearchInvoiceRequest;
			PublishService_V5.GetMCCQThueByFkeysRequest result = new PublishService_V5.GetMCCQThueByFkeysRequest()
			{
				Body = new PublishService_V5.GetMCCQThueByFkeysRequestBody
				{
					Account = model.Company.Username,
					ACpass = model.Company.Password,
					username = model.Company.Username2,
					password = model.Company.Password2,
					pattern = invoiceRequest.FormNo,
					fkeys = invoiceRequest.InvoiceId,
				}
			};
			return result;
		}

		public static listInvByCusFkeyRequest ConvertListInvByCusFkeyRequest(dynamic model)
		{
			// SearchInvoiceRequest invoiceRequest = model.Data as SearchInvoiceRequest;
			SendToTaxAuthorityRequest invoiceRequest = model.Data as SendToTaxAuthorityRequest;
			listInvByCusFkeyRequest result = new listInvByCusFkeyRequest()
			{
				Body = new listInvByCusFkeyRequestBody
				{
					userName = model.Company.Username2,
					userPass = model.Company.Password2,
					key = string.Join(";", invoiceRequest.InvoiceIds),
				}
			};
			return result;
		}

	}
}
