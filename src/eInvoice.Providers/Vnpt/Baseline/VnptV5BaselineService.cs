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
using System.Threading.Tasks;
namespace eInvoice.Providers.Vnpt.Baseline
{
    internal class VnptV5BaselineService
    {
        private readonly VnptV5Repository _vnptV5Repository;


        public VnptV5BaselineService(VnptV5Repository vnptV5Repository)
        {
            _vnptV5Repository = vnptV5Repository;
        }

        public async Task<ResponseModel> SyncDataToVnpt(VnptMappingContext model)
        {
            ResponseModel response;
			bool isInvoiceMTT = false;
			int? TypeEinvoice = 0;

            if (model.Action == (int)INVOICE_ACTION.DOWNLOAD_PDF || model.Action == (int)INVOICE_ACTION.DOWNLOAD_PDF_INV_DRAFT)
            {
                DownloadPdfRequest invoiceRequest = model.Data as DownloadPdfRequest;
                TypeEinvoice = invoiceRequest.ElectronicInvoiceType;
            }
            else if (model.Action == (int)INVOICE_ACTION.HEALTH_CHECK || model.Action == (int)INVOICE_ACTION.FORM_SERIAL || model.Action == (int)INVOICE_ACTION.GET_SERIAL_CERT_INFO ||
                model.Action == (int)INVOICE_ACTION.SEND_INVOICE_CQT || model.Action == (int)INVOICE_ACTION.GET_STATUS_INVOICE)
            {
				TypeEinvoice = 0;
			}
            else if (model.Action == (int)INVOICE_ACTION.VOID)
            {
				VoidInvoiceRequest invoiceRequest = model.Data as VoidInvoiceRequest;
				TypeEinvoice = invoiceRequest.ElectronicInvoiceType;
			}
            else if (model.Action == (int)INVOICE_ACTION.CREATED
					|| model.Action == (int)INVOICE_ACTION.DRAFT
					|| model.Action == (int)INVOICE_ACTION.PREVIEW
					|| model.Action == (int)INVOICE_ACTION.CREATED_AND_PUBLISH
					|| model.Action == (int)INVOICE_ACTION.ADJUST
					|| model.Action == (int)INVOICE_ACTION.REPLACE
					)
			{
                CreateInvoiceRequest invoiceRequest = model.Data as CreateInvoiceRequest;
                TypeEinvoice = invoiceRequest.ElectronicInvoiceType;
            }
			isInvoiceMTT = model.IsSerialMTT ? model.IsSerialMTT : TypeEinvoice == (int)INVOICE_TYPE.INVOICE_MTT;

			switch (model.Action)
            {
                case (int)INVOICE_ACTION.HEALTH_CHECK:
                case (int)INVOICE_ACTION.FORM_SERIAL:
                    response = await GetFormSerial(model);
                    break;
                case (int)INVOICE_ACTION.DRAFT:
                    response = isInvoiceMTT
						? await CreateInvoiceMTT(model)
                        : await CreateInvoice(model);
                    break;
				case (int)INVOICE_ACTION.EDIT_DRAFT:
					// response = await GetInvoiceByFkey(model);
					response = isInvoiceMTT
						? await CreateInvoiceMTT(model)
						: await CreateInvoice(model);
					break;
				case (int)INVOICE_ACTION.CREATED_AND_PUBLISH:
                case (int)INVOICE_ACTION.CREATED:
                    response = isInvoiceMTT
						? await CreateAndPublishInvoiceMTT(model)
                        : await CreateAndPublishInvoice(model);
                    break;
				case (int)INVOICE_ACTION.PUBLISH:
					response = await PublishInvoice(model);
					break;
				case (int)INVOICE_ACTION.CREATED_AND_PUBLISH_MULTI:
					response = await CreateAndPublishMultiInvoice(model);
					break;
				case (int)INVOICE_ACTION.DOWNLOAD_PDF:
                    response = isInvoiceMTT
                        ? await DownloadPDFMTT(model)
                        : await DownloadPDF(model);
                    break;
                case (int)INVOICE_ACTION.DOWNLOAD_PDF_INV_DRAFT:
                    response = isInvoiceMTT
                        ? await DownloadPDFInvoiceDraftMTT(model)
                        : await DownloadPDFInvoiceDraft(model);
                    break;
				case (int)INVOICE_ACTION.PREVIEW:
					response = await PreviewPDFInvoiceDraft(model);
					break;
				case (int)INVOICE_ACTION.VOID:
                    response = isInvoiceMTT
                        ? await VoidInvoiceMTT(model)
                        : await VoidInvoice(model);
                    break;
                case (int)INVOICE_ACTION.ADJUST_INFOR:
                case (int)INVOICE_ACTION.ADJUST_INCREASE:
                case (int)INVOICE_ACTION.ADJUST_DECREASE:
                case (int)INVOICE_ACTION.ADJUST:
                    response = isInvoiceMTT
                        ? await AdjustInvoiceMTT(model)
                        : await AdjustInvoice(model);
                    break;
                case (int)INVOICE_ACTION.REPLACE:
                    response = isInvoiceMTT
                        ? await ReplaceInvoiceMTT(model)
                        : await ReplaceInvoice(model);
                    break;
				case (int)INVOICE_ACTION.GET_SERIAL_CERT_INFO:
					response = await GetSerialCertInfo(model);
					break;
				case (int)INVOICE_ACTION.SEND_INVOICE_CQT:
					response = await SendToTaxAuthorityRequest(model);
					break;
				case (int)INVOICE_ACTION.GET_STATUS_INVOICE:
					response = await GetStatusInvoice(model);
					break;
				case (int)INVOICE_ACTION.DELETE:
					response = await DeleteInvoiceAPI(model);
					break;
				default:
                    response = new ResponseModel(322, "Đơn vi cung cấp không có chức năng này!");
                    break;
            }
            return response;
        }

        public string Url {
            set
            {
                _vnptV5Repository.Url = value;
            }
        }

		#region HDDT thông thường
		private async Task<ResponseModel> CreateInvoice(VnptMappingContext request)
        {
            //ImportInvByPatternRequest vnptRequest = VnptConvertRequest.ConvertCreateInvoiceRequestV5(request);
            //ImportInvByPatternResponse vnptResponse = await _vnptV5Repository.CreateInvoice(vnptRequest);
            //ResponseModel result = VnptConvertResponse.ConvertCreateInvoiceResponse(vnptResponse, request);
            //return result;

			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertCreateInvoiceRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.CreateInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertCreateInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> EditInvoiceDraft(VnptMappingContext request)
		{
			await DeleteInvoice(request);
			CreateInvoiceRequest invoiceRequest = request.Data as CreateInvoiceRequest;
			CoreInvoice pmsInvoice = invoiceRequest.Invoice;
			pmsInvoice.InvoiceId = Guid.NewGuid().ToString(); // gán lại GUID để tạo hóa đơn mới không bị trùng với ezInvoiceID đã xóa bên trên
			return await CreateInvoice(request);
		}
		private async Task<ResponseModel> DeleteInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDeleteInvoiceRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DeleteInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDeleteInvoiceV5Response(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> DeleteInvoiceAPI(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDeleteInvoiceAPIRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DeleteInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDeleteInvoiceV5Response(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> GetInvoiceByFkey(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertGetInvoiceByFkeyRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.GetInvoiceByFkey(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertGetInvoiceByFkeyV5Response(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> ReplaceInvoice(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertReplaceInvoiceRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.ReplaceInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertReplaceInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
        private async Task<ResponseModel> AdjustInvoice(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertAdjustInvoiceRequestV5(request, request.Action),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.AdjustInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertAdjustInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
        private async Task<ResponseModel> VoidInvoice(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertVoidInvoiceRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.VoidInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertVoidInvoiceResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
        private async Task<ResponseModel> DownloadPDF(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDownloadPDFRequestV5NoPay(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDF(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFResponseV5(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
        private async Task<ResponseModel> DownloadPDFInvoiceDraft(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDownloadPDFInvoiceDraftRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDFInvoiceDraft(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFInvoiceDraftResponseV5(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
		private async Task<ResponseModel> PreviewPDFInvoiceDraft(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertPreviewPDFInvoiceDraftRequestV5(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDFInvoiceDraft(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFInvoiceDraftResponseV5(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> CreateAndPublishInvoice(VnptMappingContext request)
        {
			try
			{
				if (request.HaveDeleteInvoiceDraft)
				{
					await DeleteInvoice(request);
				}

				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertCreateAndPublishInvoiceRequestV5(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.CreateAndPublishInvoice(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertCreateAndPublishInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
		private async Task<ResponseModel> PublishInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertPublishInvoiceTT78Request(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.PublishInvoice(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertPublishInvoiceFromDraftResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> CreateAndPublishMultiInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertCreateAndPublishInvoiceMultiRequestV5(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.CreateAndPublishInvoice(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertCreateAndPublishMultiInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> GetFormSerial(VnptMappingContext request)
        {
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertFormNoSerialRequestV5(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.GetFormSerials(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertFormNoSerialResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
        }
		#endregion

		#region HDDT dùng cho Máy tính tiền (MTT)
		private async Task<ResponseModel> GetSerialCertInfo(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertGetSerialCertInfoRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.GetSerialCertInfo(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertGetSerialCertInfoResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> CreateInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertCreateInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.CreateInvoiceMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertCreateInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> DownloadPDFInvoiceDraftMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertDownloadPDFInvoiceDraftRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDFInvoiceDraftMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertDownloadInvPDFInvoiceDraftResponseV5(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> CreateAndPublishInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				if (request.HaveDeleteInvoiceDraft)
				{
					await DeleteInvoice(request);
				}

				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertCreateAndPublishInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.CreateAndPublishInvoiceMTT(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertCreateAndPublishInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> DownloadPDFMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertDownloadPDFRequestNoPay(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDFMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertDownloadInvPDFResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> DownloadPDFFkeyMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertDownloadPDFFkeyRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.DownloadPDFFkeyMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertDownloadInvPDFFkeyResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> AdjustInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertAdjustInvoiceRequest(request, request.Action),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.AdjustInvoiceMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertAdjustInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> ReplaceInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertReplaceInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.ReplaceInvoiceMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertReplaceInvoiceResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> VoidInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertVoidInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.VoidInvoiceMTT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertVoidInvoiceResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> SendToTaxAuthorityRequest(VnptMappingContext request)
		{
			try
			{
				var serialCert = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.ConvertGetSerialCertInfoRequest(request),
					Errors.EzInvoiceCreateRequest);

				var serialCertResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.GetSerialCertInfo(serialCert),
					Errors.ThirdPartyException);

				string SerialCert = VnptMTTConvertResponse.GetSerialCertString(serialCertResponse);

				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.SendToTaxAuthorityRequest(request, SerialCert),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.SendInvoiceCQT(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.ConvertSendToTaxAuthorityRequestResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> GetStatusInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertRequest.GetStatusInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.GetStatusInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptMTTConvertResponse.GetStatusInvoiceResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}
		private async Task<ResponseModel> PublishInvoiceMTT(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertPublishInvoiceTT78Request(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.PublishInvoice(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertPublishInvoiceFromDraftResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		#endregion

		public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
