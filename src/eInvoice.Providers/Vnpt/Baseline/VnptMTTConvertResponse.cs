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
using PortalService_V5;
using PublishService_V5;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace eInvoice.Providers.Vnpt.Baseline
{
	internal class VnptMTTConvertResponse
	{
		private const string RESPONSE_SUCCESS = "OK";
		private const int NumberOfDigits = 7;

		public static ResponseModel ConvertFormNoSerialResponse(getPublishInvsResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.getPublishInvsResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.FORM_SERIAL);
			}
			return ResponseModel.Success(FormSerials(vnptResult));
		}
		public static ResponseModel ConvertCreateInvoiceResponse(ImportInvByPatternMTTResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ImportInvByPatternMTTResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.DRAFT_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}
		public static ResponseModel ConvertDownloadInvPDFInvoiceDraftResponseV5(downloadNewInvPDFFkeyResponse vnptdata)
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
		public static ResponseModel ConvertCreateAndPublishInvoiceResponse(ImportAndPublishInvMTTResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ImportAndPublishInvMTTResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.CREATE_AND_SIGN_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}
		public static ResponseModel ConvertDownloadInvPDFResponse(downloadInvPDFFkeyNoPayErrorResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.downloadInvPDFFkeyNoPayErrorResult;
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
		public static ResponseModel ConvertDownloadInvPDFFkeyResponse(getInvViewFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.getInvViewFkeyResult;
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
		public static ResponseModel ConvertAdjustInvoiceResponse(AdjustInvMTTResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.AdjustInvMTTResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.ADJUST_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
		}
		public static ResponseModel ConvertReplaceInvoiceResponse(ReplaceInvMTTResponse vnptdata, dynamic prepareData)
		{
			dynamic InvoiceId = prepareData.Data.Invoice.InvoiceId;
			string vnptResult = vnptdata.Body.ReplaceInvMTTResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.REPLACE_INVOICE);
			}
			return InvoiceSuccess(vnptResult, Convert.ToString(InvoiceId), NumberOfDigits);
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

		public static ResponseModel ConvertGetSerialCertInfoResponse(GetCertInfoResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.GetCertInfoResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.GET_SERIAL_CERT_INFO);
			}
			return SerialCertSuccess(vnptResult);
		}
		public static ResponseModel ConvertSendToTaxAuthorityRequestResponse(SendInvMTTFkeyResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.SendInvMTTFkeyResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.SEND_INVOICE_CQT);
			}
			return ResponseModel.Success(vnptResult);
		}
		public static ResponseModel GetStatusInvoiceResponse(GetMCCQThueByFkeysResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.GetMCCQThueByFkeysResult;
			if (IsError(vnptResult))
			{
				return ErrorResponse(vnptResult, VnptConst.GET_STATUS_INVOICE);
			}

			string dataConvert = ConvertResultBase64ToString(vnptResult);
			List<PMSHDon> listHD = GetListHDon(dataConvert);

			List<PmsStatusInvoiceResponse> result = new List<PmsStatusInvoiceResponse>();
			foreach (PMSHDon item in listHD)
			{
				result.Add(new PmsStatusInvoiceResponse
				{
					InvoiceId = item.Fkey,
					StatusInvoice = item.TThai ?? 0,
					ProviderInvoiceNumber = GetInvoiceNumber(item.SHDon, NumberOfDigits),
					SearchCode = item.Fkey,
					MaCQT = item.MCCQThue,
				});
			}

			return ResponseModel.Success(result);
		}
		// Private
		private static List<PMSHDon> GetListHDon(string xmlString)
		{
			// Đổi do trùng tên model HDon đã khai báo từ trước
			string xmlStringConvert = xmlString.Replace("<HDon>", "<PMSHDon>").Replace("</HDon>", "</PMSHDon>");
			List<PMSHDon> result = new List<PMSHDon>();
			XmlSerializer serializer = new XmlSerializer(typeof(PMSHDon[]), new XmlRootAttribute("DSHDon"));
			using (StringReader stringReader = new StringReader(xmlStringConvert))
			{
				PMSHDon[] items = (PMSHDon[])serializer.Deserialize(stringReader);
				result = items.ToList();
			}
			return result;
		}
		private static ResponseModel InvoiceSuccess(string vnptResult, string InvoiceId, int numberOfDigits)
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

		private static ResponseModel SerialCertSuccess(string vnptResult)
		{
			string resultConvert = ConvertResultBase64ToString(vnptResult);
			string serialCert = GetSerialCert(resultConvert);
			if (serialCert == null)
			{
				return new ResponseModel(message: vnptResult, data: serialCert);
			}
			return ResponseModel.Success(serialCert);
		}
		public static string GetSerialCertString(GetCertInfoResponse vnptdata)
		{
			string vnptResult = vnptdata.Body.GetCertInfoResult;
			string resultConvert = ConvertResultBase64ToString(vnptResult);
			string serialCert = GetSerialCert(resultConvert);
			if (serialCert == null)
			{
				return serialCert;
			}
			return serialCert;
		}
		private static string ConvertResultBase64ToString(string base64String)
		{
			byte[] bytes = Convert.FromBase64String(base64String);

			// Convert byte array to string using UTF-8 encoding
			string result = Encoding.UTF8.GetString(bytes);
			return result;
		}

		private static string GetSerialCert(string resultConvert)
		{
			string pattern = @"<SerialNumber>(.*?)</SerialNumber>";
			Match match = Regex.Match(resultConvert, pattern);

			if (match.Success)
			{
				string serialNumber = match.Groups[1].Value;
				return serialNumber;
			}
			return null;
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
				//return new ResponseModel(322, "Nhà cung cấp không phản hồi!", vnptResult);
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
