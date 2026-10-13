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
using System.IO;
using System.Linq;
namespace eInvoice.Providers.Vnpt.Baseline
{
    internal static class VnptConvertResponse
    {
        private const string RESPONSE_SUCCESS = "OK";
        private const int NumberOfDigits = 7;

		public static ResponseModel ConvertListInvByCusFkeyResponse(listInvByCusFkeyResponse vnptdata, dynamic prepareData)
		{
			string vnptResult = vnptdata.Body.listInvByCusFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.REPLACE_INVOICE);
			}

			List<ListInvByCusFkey> listHD = GetListInvByCusFkey(vnptResult);

			List<PmsStatusInvoiceResponse> result = new List<PmsStatusInvoiceResponse>();
			foreach (ListInvByCusFkey item in listHD)
			{
				result.Add(new PmsStatusInvoiceResponse
				{
					InvoiceId = item.cusCode,
					StatusInvoice = item.signStatus ?? 0,
					ProviderInvoiceNumber = item.invNum,
					SearchCode = item.cusCode,
					MaCQT = "",
				});
			}
			return ResponseModel.Success(result);
		}

		private static List<ListInvByCusFkey> GetListInvByCusFkey(string xmlString)
		{
			// Đổi do trùng tên model HDon đã khai báo từ trước
			string xmlStringConvert = xmlString.Replace("<Item>", "<ListInvByCusFkey>").Replace("</Item>", "</ListInvByCusFkey>");
			List<ListInvByCusFkey> result = new List<ListInvByCusFkey>();
			XmlSerializer serializer = new XmlSerializer(typeof(ListInvByCusFkey[]), new XmlRootAttribute("Data"));
			using (StringReader stringReader = new StringReader(xmlStringConvert))
			{
				ListInvByCusFkey[] items = (ListInvByCusFkey[])serializer.Deserialize(stringReader);
				result = items.ToList();
			}
			return result;
		}

		public static ResponseModel ConvertSearchByFkeyResponse(PublishService_V5.GetMCCQThueByFkeysResponse vnptdata, dynamic prepareData)
        {
            dynamic InvoiceId = prepareData.Data.InvoiceId;
            string vnptResult = vnptdata.Body.GetMCCQThueByFkeysResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.REPLACE_INVOICE);
            }

			string dataConvert = ConvertResultBase64ToString(vnptResult);
			List<PMSSearchHDonByFkey> listHD = GetListHDon(dataConvert);



			PmsSearchInvoiceResponse dataResponse = new PmsSearchInvoiceResponse()
			{
				InvoiceId = listHD[0].Fkey,
				ProviderInvoiceNumber = GetInvoiceNumber(listHD[0].SHDon, NumberOfDigits),
				CodeOfTax = listHD[0].MCCQThue
			};

			//foreach (PMSSearchHDonByFkey item in listHD)
			//{
			//	result.Add(new PmsStatusInvoiceResponse
			//	{
			//		InvoiceId = item.Fkey,
			//		StatusInvoice = item.TThai ?? 0,
			//		ProviderInvoiceNumber = GetInvoiceNumber(item.SHDon, NumberOfDigits),
			//		SearchCode = item.Fkey,
			//		MaCQT = item.MCCQThue,
			//	});
			//}

			return ResponseModel.Success(dataResponse);
		}

		private static string ConvertResultBase64ToString(string base64String)
		{
			byte[] bytes = Convert.FromBase64String(base64String);

			// Convert byte array to string using UTF-8 encoding
			string result = Encoding.UTF8.GetString(bytes);
			return result;
		}

		private static List<PMSSearchHDonByFkey> GetListHDon(string xmlString)
		{
			// Đổi do trùng tên model HDon đã khai báo từ trước
			string xmlStringConvert = xmlString.Replace("<HDon>", "<PMSSearchHDonByFkey>").Replace("</HDon>", "</PMSSearchHDonByFkey>");
			List<PMSSearchHDonByFkey> result = new List<PMSSearchHDonByFkey>();
			XmlSerializer serializer = new XmlSerializer(typeof(PMSSearchHDonByFkey[]), new XmlRootAttribute("DSHDon"));
			using (StringReader stringReader = new StringReader(xmlStringConvert))
			{
				PMSSearchHDonByFkey[] items = (PMSSearchHDonByFkey[])serializer.Deserialize(stringReader);
				result = items.ToList();
			}
			return result;
		}
		public static ResponseModel ConvertReplaceInvoiceResponse(ReplaceInvoiceActionResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ReplaceInvoiceActionResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.REPLACE_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}
		//public static ResponseModel ConvertReplaceInvoiceResponse(BusinessService_V5.ReplaceInvoiceActionResponse vnptdata, dynamic prepareData)
		//{
		//    dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
		//    string vnptResult = vnptdata.Body.ReplaceInvoiceActionResult;
		//    if (IsError(vnptResult))
		//    {
		//        return ErrorResponse(vnptResult, VnptConst.REPLACE_INVOICE);
		//    }
		//    return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		//}

		public static ResponseModel ConvertAdjustInvoiceResponse(AdjustInvoiceActionResponse vnptdata, dynamic prepareData)
        {
            dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
            string vnptResult = vnptdata.Body.AdjustInvoiceActionResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.ADJUST_INVOICE);
            }
            return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
        }

		//public static ResponseModel ConvertAdjustInvoiceResponse(BusinessService_V5.AdjustInvoiceActionResponse vnptdata, dynamic prepareData)
		//{
		//    dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
		//    string vnptResult = vnptdata.Body.AdjustInvoiceActionResult;
		//    if (IsError(vnptResult))
		//    {
		//        return ErrorResponse(vnptResult, VnptConst.ADJUST_INVOICE);
		//    }
		//    return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		//}

		public static ResponseModel ConvertVoidInvoiceVNPTResponse(cancelInvResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.cancelInvResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.VOID);
			}
			return ResponseModel.Success(vnptResult);
		}

		public static ResponseModel ConvertVoidInvoiceResponse(cancelInvNoPayResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.cancelInvNoPayResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.VOID);
            }
            return ResponseModel.Success(vnptResult);
        }

		public static ResponseModel ConvertDeleteInvoiceResponse(deleteInvoiceByFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.deleteInvoiceByFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.DELETE);
			}
			return ResponseModel.Success(vnptResult);
		}

		//public static ResponseModel ConvertVoidInvoiceResponse(BusinessService_V5.cancelInvNoPayResponse vnptdata)
		//{
		//    string vnptResult = vnptdata.Body.cancelInvNoPayResult;
		//    if (IsError(vnptResult))
		//    {
		//        return ErrorResponse(vnptResult, VnptConst.VOID);
		//    }
		//    return ResponseModel.Success(vnptResult);
		//}
		public static ResponseModel ConvertDownloadInvPDFDraftResponse(downloadNewInvPDFFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.downloadNewInvPDFFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.DOWNLOAD_PDF);
			}
			return ResponseModel.Success(new PmsDownloadPdfResponse
			{
				InvoicePDF = vnptResult,
				Type = 1,
			});
		}
		public static ResponseModel ConvertDeleteInvoiceV5Response(PublishService_V5.deleteInvoiceByFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.deleteInvoiceByFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.DELETE);
			}
			return ResponseModel.Success(vnptResult);
		}

		public static ResponseModel ConvertGetInvoiceByFkeyV5Response(PublishService_V5.GetInvDataByFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.GetInvDataByFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.GET_DATA_INVOICE_BY_FKEY);
			}
			return ResponseModel.Success(vnptResult);
		}

		public static ResponseModel ConvertDownloadInvPDFResponse(downloadInvPDFFkeyResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.downloadInvPDFFkeyResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.DOWNLOAD_PDF);
            }
            return ResponseModel.Success(new PmsDownloadPdfResponse
            {
                InvoicePDF = vnptResult,
                Type = 1,
            });
        }
        public static ResponseModel ConvertDownloadInvPDFResponseV5(PortalService_V5.downloadInvPDFFkeyNoPayResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.downloadInvPDFFkeyNoPayResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.DOWNLOAD_PDF);
            }
            return ResponseModel.Success(new PmsDownloadPdfResponse
            {
                InvoicePDF = vnptResult,
                Type = 1,
            });
        }
        public static ResponseModel ConvertDownloadInvPDFInvoiceDraftResponseV5(PortalService_V5.downloadNewInvPDFFkeyResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.downloadNewInvPDFFkeyResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.DOWNLOAD_PDF);
            }
            return ResponseModel.Success(new PmsDownloadPdfResponse
            {
                InvoicePDF = vnptResult,
                Type = 1,
            });
        }
        public static ResponseModel ConvertDownloadInvPDFResponse(PortalService_V5.downloadInvPDFFkeyResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.downloadInvPDFFkeyResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.DOWNLOAD_PDF);
            }
            return ResponseModel.Success(new PmsDownloadPdfResponse
            {
                InvoicePDF = vnptResult,
                Type = 1,
            });
        }

		public static ResponseModel ConvertCreateInvoiceResponse(ImportInvByPatternResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ImportInvByPatternResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}
		public static ResponseModel ConvertCreateInvoiceReceiptResponse(PublishService_TLYT.ImportInvResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ImportInvResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}

		public static ResponseModel ConvertCreateAndPublishInvoiceResponse(ImportAndPublishInvResponse vnptdata, dynamic prepareData)
        {
            dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
            string vnptResult = vnptdata.Body.ImportAndPublishInvResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
            }
            return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
        }
		public static ResponseModel ConvertPublishInvoiceResponse(PublishInvFkeyResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.InvoiceId;
			string vnptResult = vnptdata.Body.PublishInvFkeyResult;
			if (IsErrorPublish(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.PUBLISH_INVOICE);
			}
			return InvoicePublishSuccess(getDataSuccess(vnptResult), Convert.ToString(InvoiceId), NumberOfDigits);
		}
		public static ResponseModel ConvertPublishInvoiceReceiptResponse(PublishService_TLYT.publishInvByFkeyResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.InvoiceId;
			string vnptResult = vnptdata.Body.publishInvByFkeyResult;
			if (IsErrorPublish(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.PUBLISH_INVOICE);
			}
			return InvoicePublishSuccess(getDataSuccess(vnptResult), Convert.ToString(InvoiceId), NumberOfDigits);
		}

		public static ResponseModel ConvertPublishInvoiceFromDraftResponse(PublishService_V5.PublishInvFkeyResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.InvoiceId;
			string vnptResult = vnptdata.Body.PublishInvFkeyResult;
			if (IsErrorPublish(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.PUBLISH_INVOICE);
			}
			return InvoicePublishSuccess(getDataSuccess(vnptResult), Convert.ToString(InvoiceId), NumberOfDigits);
		}

		public static ResponseModel ConvertCreateInvoiceResponse(PublishService_V5.ImportInvByPatternResponse vnptdata, dynamic prepareData)
        {
            dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
            string vnptResult = vnptdata.Body.ImportInvByPatternResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.DRAFT_INVOICE);
            }
            return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
        }

        public static ResponseModel ConvertCreateAndPublishInvoiceResponse(PublishService_V5.ImportAndPublishInvResponse vnptdata, dynamic prepareData)
        {
            dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
            string vnptResult = vnptdata.Body.ImportAndPublishInvResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
            }
            return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
        }

		public static ResponseModel ConvertCreateAndPublishMultiInvoiceResponse(PublishService_V5.ImportAndPublishInvResponse vnptdata, dynamic prepareData)
		{
			string vnptResult = vnptdata.Body.ImportAndPublishInvResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			if (vnptResult.Contains(RESPONSE_SUCCESS))
			{
				// Chuỗi kết quả nếu thành công:  OK:5/002;C23GYY-4c9f0362-5bcb-4e1a-9196-56271e9622e9_29,ec98a907-9c84-41d2-ae6e-b8ee5b10a983_30
				const string SEPARATOR_SPLIT = "-";
				int indexWillSplit = vnptResult.IndexOf(SEPARATOR_SPLIT);
                if (indexWillSplit >=0)
                {
					string dataResponse = vnptResult.Substring(indexWillSplit + 1);
                    return GetDataMultiInvoiceResponse(dataResponse);
				}
				return new ResponseModel(message: vnptResult, data: vnptResult);
			}
			return new ResponseModel(message: vnptResult, data: vnptResult);
		}

		public static ResponseModel ConvertCreateAndPublishMultiInvoiceResponse(ImportAndPublishInvResponse vnptdata, dynamic prepareData)
		{
			string vnptResult = vnptdata.Body.ImportAndPublishInvResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			if (vnptResult.Contains(RESPONSE_SUCCESS))
			{
				// Chuỗi kết quả nếu thành công:  OK:5/002;C23GYY-4c9f0362-5bcb-4e1a-9196-56271e9622e9_29,ec98a907-9c84-41d2-ae6e-b8ee5b10a983_30
				// Chuỗi kết quả nếu thành công:  OK:01BLP0-001;AA-24E-f88c0811-1b87-4287-80ca-440e6c863494_18
				const string SEPARATOR_SPLIT = "-";
				int indexWillSplit = vnptResult.IndexOf(SEPARATOR_SPLIT);
				if (indexWillSplit >= 0)
				{
					string dataResponse = vnptResult.Substring(indexWillSplit + 1);
					return GetDataMultiInvoiceResponse(dataResponse);
				}
				return new ResponseModel(message: vnptResult, data: vnptResult);
			}
			return new ResponseModel(message: vnptResult, data: vnptResult);
		}

		public static ResponseModel ConvertCreateAndPublishMultiInvoiceTLYTResponse(ImportAndPublishInvResponse vnptdata, dynamic prepareData)
		{
			string vnptResult = vnptdata.Body.ImportAndPublishInvResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			if (vnptResult.Contains(RESPONSE_SUCCESS))
			{
				// Chuỗi kết quả nếu thành công:  OK:5/002;C23GYY-4c9f0362-5bcb-4e1a-9196-56271e9622e9_29,ec98a907-9c84-41d2-ae6e-b8ee5b10a983_30
				// Chuỗi kết quả nếu thành công:  OK:01BLP0-001;AA-24E-f88c0811-1b87-4287-80ca-440e6c863494_18
				const string SEPARATOR_SPLIT = "-";
				int indexWillSplit = vnptResult.IndexOf(SEPARATOR_SPLIT);
				if (indexWillSplit >= 0)
				{
					string dataResponse = vnptResult.Substring(indexWillSplit + 1);
					return GetDataMultiInvoiceResponse(dataResponse);
				}
				return new ResponseModel(message: vnptResult, data: vnptResult);
			}
			return new ResponseModel(message: vnptResult, data: vnptResult);
		}

		public static ResponseModel GetDataMultiInvoiceResponse(string dataResponse)
        {
            string[] ezInvoiceIDAndProviderInvoiceNumber = dataResponse.Split(',');
            if (ezInvoiceIDAndProviderInvoiceNumber.Any())
            {
                List<PmsInvoiceResponse> result = new List<PmsInvoiceResponse>();

				foreach (string item in ezInvoiceIDAndProviderInvoiceNumber)
                {
                    PmsInvoiceResponse itemInvoiceResponse = new PmsInvoiceResponse();
                    const string SEPARATOR_SUCCESS = "_";
                    if (item.Contains(SEPARATOR_SUCCESS))
                    {
                        string[] splitBySeparator = item.Split(SEPARATOR_SUCCESS.ToCharArray());
						itemInvoiceResponse.ProviderInvoiceNumber = GetInvoiceNumber(splitBySeparator[1].ToString(), NumberOfDigits);
                        string ezInvoiceID = splitBySeparator[0].Substring(splitBySeparator[0].Length - 36);

						itemInvoiceResponse.InvoiceId = ezInvoiceID;
						itemInvoiceResponse.SearchCode = ezInvoiceID;
                        itemInvoiceResponse.IsSuccess = true;
                        if (splitBySeparator.Length >= 3)
                        {
                            itemInvoiceResponse.MaCQT = splitBySeparator[2].ToString();
                        }
                        result.Add(itemInvoiceResponse);
					}
                }
				return ResponseModel.Success(result);
			}
            return ResponseModel.Success(dataResponse);
		}
		public static ResponseModel ConvertFormNoSerialResponse(getPublishInvsResponse vnptdata)
        {
            string vnptResult = vnptdata.Body.getPublishInvsResult;
            if (IsError(vnptResult))
            {
                return ErrorResponse(vnptResult, VnptConst.FORM_SERIAL);
            }
            return ResponseModel.Success(FormSerials(vnptResult));
        }

        //public static ResponseModel ConvertFormNoSerialResponse(BusinessService_V5.getPublishInvsResponse vnptdata)
        //{
        //    string vnptResult = vnptdata.Body.getPublishInvsResult;
        //    if (IsError(vnptResult))
        //    {
        //        return ErrorResponse(vnptResult, VnptConst.FORM_SERIAL);
        //    }
        //    return ResponseModel.Success(FormSerials(vnptResult));
        //}

        ///// <summary>
        ///// Xử lý output của vnpt sau khi tạo hóa đơn
        ///// </summary>
        ///// <param name="vnptResult"></param>
        ///// <returns></returns>
        //private static ResponseModel Response(string vnptResult, string action, string InvoiceId = null, int? numberOfDigits = null)
        //{
        //    if (vnptResult == null)
        //    {
        //        return new ResponseModel(322, "Nhà cung cấp không phản hồi!", vnptResult);
        //    }
        //    if (vnptResult.Contains(VnptConst.RESPONSE_ERROR) && action != VnptConst.SIGN_INVOICE)
        //    {
        //        KeyValuePair<string, Dictionary<string, string>> errorCreateAndSign = VnptResponseError.ListResponseError.FirstOrDefault(e => e.key == action);
        //        if (errorCreateAndSign.Value != null)
        //        {
        //            string errorDetail = errorCreateAndSign.Value.FirstOrDefault(e => e.key == vnptResult).Value;
        //            int statusError = 322;
        //            if (vnptResult.Contains(":") && int.TryParse(vnptResult.Split(':')[1], out statusError))
        //            { }
        //            if ((errorDetail ?? "") == "")
        //            {
        //                errorDetail = vnptResult;
        //            }
        //            return new ResponseModel(statusError, errorDetail, vnptResult);
        //        }
        //        return new ResponseModel(322, vnptResult, vnptResult);
        //    }
        //    if (action == VnptConst.DOWNLOAD_PDF)
        //    {
        //        return new ResponseModel(200, "success", new PmsDownloadPdfResponse
        //        {
        //            InvoiceId = InvoiceId,
        //            InvoicePDF = vnptResult,
        //            Type = 1,
        //        });
        //    }
        //    if (action == VnptConst.CONVERT_FOR_STORE)
        //    {
        //        return new ResponseModel(200, "success", new ThirdPartyResponseModel
        //        {
        //            ConvertForStore = vnptResult
        //        });
        //    }

        //}

        private static ResponseModel InvoiceSuccess (string vnptResult, string InvoiceId, int numberOfDigits)
        {
            if (vnptResult.Contains(RESPONSE_SUCCESS))
            {
                PmsInvoiceResponse dataResponse = new PmsInvoiceResponse()
                {
                    InvoiceId = InvoiceId,
                };
                const string SEPARATOR_SUCCESS = "_";
                if (vnptResult.Contains(SEPARATOR_SUCCESS))
                {
                    string[] splitBySeparator = vnptResult.Split(SEPARATOR_SUCCESS.ToCharArray());
                    dataResponse.ProviderInvoiceNumber = GetInvoiceNumber(splitBySeparator[1].ToString(), numberOfDigits);
                    dataResponse.SearchCode = InvoiceId;
                    if (splitBySeparator.Length >= 3)
                    {
                        dataResponse.MaCQT = splitBySeparator[2].ToString();
					}

                }
                return ResponseModel.Success(dataResponse);
            }
            return new ResponseModel(message: vnptResult, data: vnptResult);
        }

        private static string GetInvoiceNumber(string vnptInvoiceNumber, int? numberOfDigits)
        {
            numberOfDigits = numberOfDigits ?? NumberOfDigits;
            if (numberOfDigits.HasValue && numberOfDigits.Value > vnptInvoiceNumber.Length)
            {
                string map = new String('0', numberOfDigits.Value - vnptInvoiceNumber.Length);
                vnptInvoiceNumber = $"{map}{vnptInvoiceNumber}";
            }
            return vnptInvoiceNumber;
        }

        private static bool IsError(string vnptResult)
        {
            return vnptResult == null || vnptResult.Contains(VnptConst.RESPONSE_ERROR);
        }

		private static bool IsErrorPublish(string vnptResult)
		{
            if (vnptResult == null) return true;
            string dataSuccess = getDataSuccess(vnptResult);
            if (string.IsNullOrEmpty(dataSuccess) || !dataSuccess.Contains("_")) return true;
            string[] splitDataOK = dataSuccess.Split('_');
            return string.IsNullOrEmpty(splitDataOK[1]);
		}
		/// <summary>
		/// Lấy thông tin chuỗi có chữ OK: ERR:6#||ERR:15#||OK:#17d507ea-c9df-4e7a-9080-82b858666260122_129
		/// </summary>
		/// <returns></returns>
		private static string getDataSuccess(string vnptResult)
        {
            string SEPARATOR_SUCCESS = "||";
			string[] splitBySeparator = vnptResult.Split(SEPARATOR_SUCCESS);
            return splitBySeparator.LastOrDefault(item => item.TrimStart().StartsWith("OK:", StringComparison.Ordinal)) ?? "";
		}

		private static ResponseModel InvoicePublishSuccess(string vnptResult, string InvoiceId, int numberOfDigits)
	    {
			PmsInvoiceResponse dataResponse = new PmsInvoiceResponse()
			{
				InvoiceId = InvoiceId,
			};
			const string SEPARATOR_SUCCESS = "_";
			if (vnptResult.Contains(SEPARATOR_SUCCESS))
			{
				string[] splitBySeparator = vnptResult.Split(SEPARATOR_SUCCESS.ToCharArray());
				dataResponse.ProviderInvoiceNumber = GetInvoiceNumber(splitBySeparator[1].ToString(), numberOfDigits);
				dataResponse.SearchCode = InvoiceId;
			}
			return ResponseModel.Success(dataResponse);
		}

		private static List<FormSerialResponse> FormSerials(string input)
        {
            FormSerialVnptListResult formSerials = JsonConvert.DeserializeObject<FormSerialVnptListResult>(input);
            List<FormSerialResponse> result = formSerials.result.Select(item => new FormSerialResponse
            {
                InvoiceName = item.NameInvoice,
                FormNo = item.InvPattern,
                Serial = item.InvSerial,
                Quantity = item.Quantity,
                FromNo = item.FromNo,
                ToNo = item.ToNo,
                CurrentNo = item.ToNo,
                Status = item.Status,
                StartDate = item.StartDate,
            }).ToList();
            return result;
        }

        private static ResponseModel ErrorResponse(string vnptResult, string action)
        {
            if (vnptResult == null)
            {
				// Truyền theo tên tham số để chọn đúng overload Fail(error, data):
				// nếu để C# tự suy luận, vnptResult (string) sẽ bị gán vào errorMessage và ghi đè message của lỗi.
				return ResponseModel.Fail(Errors.ThirdPartyUnknownError, data: vnptResult);
			}
            if (vnptResult.Contains(VnptConst.RESPONSE_ERROR))
            {
                KeyValuePair<string, Dictionary<string, string>> errorInAction = VnptResponseError.ListResponseError.FirstOrDefault(e => e.Key == action);
                if (errorInAction.Value != null)
                {
                    string errorDetail = errorInAction.Value.FirstOrDefault(e => e.Key == vnptResult).Value;
                    int statusError = 322;
                    if (vnptResult.Contains(":") && int.TryParse(vnptResult.Split(':')[1], out statusError))
                    { }
					if ((errorDetail ?? "") == "")
					{
						errorDetail = vnptResult;
					}
							return ResponseModel.Fail(Errors.ThirdPartySpecificError, errorDetail, vnptResult);
						}
						return ResponseModel.Fail(Errors.ThirdPartySpecificError, vnptResult, vnptResult);
					}
					return ResponseModel.Success(vnptResult);
        }
    }
}
