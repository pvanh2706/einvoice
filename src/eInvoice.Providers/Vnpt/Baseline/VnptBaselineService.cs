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
using System.Threading.Tasks;
namespace eInvoice.Providers.Vnpt.Baseline
{
	internal class VnptBaselineService
	{
		private readonly VnptRepository _vnptRepository;
		private readonly VnptRepository_TLYT _vnptRepository_TLYT;
		private readonly VnptV5Repository _vnptV5Repository;


		public string Url
		{
			set
			{
				_vnptV5Repository.Url = value;
				_vnptRepository.Url = value;
			}
		}
		public string Url_TLYT
		{
			set => _vnptRepository_TLYT.Url_TLYT = value;
		}

		public VnptBaselineService(VnptRepository vnptRepository, VnptRepository_TLYT vnptRepository_TLYT, VnptV5Repository vnptV5Repository)
		{
			_vnptRepository = vnptRepository;
			_vnptRepository_TLYT = vnptRepository_TLYT;
			_vnptV5Repository = vnptV5Repository;
		}


		public async Task<ResponseModel> SyncDataToVnpt(VnptMappingContext model)
		{
			ResponseModel response;
			switch (model.Action)
			{
				case (int)INVOICE_ACTION.HEALTH_CHECK:
				case (int)INVOICE_ACTION.FORM_SERIAL:
					response = await GetFormSerial(model);
					break;
				case (int)INVOICE_ACTION.DRAFT:
					response = await CreateInvoice(model);
					break;
				case (int)INVOICE_ACTION.CREATED_AND_PUBLISH:
					response = await CreateAndPublishInvoice(model);
					break;
				case (int)INVOICE_ACTION.CREATED_AND_PUBLISH_MULTI:
					response = await CreateAndPublishMultiInvoice(model);
					break;
				case (int)INVOICE_ACTION.PUBLISH:
					response = await PublishInvoice(model);
					break;
				case (int)INVOICE_ACTION.DOWNLOAD_PDF:
					response = await DownloadPDF(model);
					break;
				case (int)INVOICE_ACTION.DOWNLOAD_PDF_INV_DRAFT:
					response = await DownloadPDFDraft(model);
					break;
				case (int)INVOICE_ACTION.PREVIEW:
					response = await PreviewPDFInvoiceDraft(model);
					break;
				case (int)INVOICE_ACTION.VOID:
					response = await VoidInvoice(model);
					break;
				case (int)INVOICE_ACTION.DELETE:
					response = await DeleteInvoice(model);
					break;
				case (int)INVOICE_ACTION.ADJUST_INFOR:
				case (int)INVOICE_ACTION.ADJUST_INCREASE:
				case (int)INVOICE_ACTION.ADJUST_DECREASE:
					response = await AdjustInvoice(model);
					break;
				case (int)INVOICE_ACTION.REPLACE:
					response = await ReplaceInvoice(model);
					break;
				case (int)INVOICE_ACTION.SEARCH_BY_ID:
					response = await SearchByFkey(model);
					break;
				case (int)INVOICE_ACTION.GET_STATUS_INVOICE:
					response = await GetStatusInvoice(model);
					break;
				default:
					response = new ResponseModel(322, "Đơn vi cung cấp không có chức năng này!");
					break;
			}
			return response;
		}

		private async Task<ResponseModel> GetStatusInvoice(VnptMappingContext request)
		{
			try
			{
				var temp = request.Data as SendToTaxAuthorityRequest;
				int? invoiceType = temp.InvoiceType;

				if (invoiceType == (int)INV_TYPE.INV_NORMAL || invoiceType == (int)INV_TYPE.INV_TICKET)
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
				else if (invoiceType == (int)INV_TYPE.INV_RECEIPT)
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertListInvByCusFkeyRequest(request),
						Errors.EzInvoiceCreateRequest);

					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.ListInvByCusFkey(vnptRequest),
						Errors.ThirdPartyException);

					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertListInvByCusFkeyResponse(vnptResponse, request),
						Errors.EzInvoiceHandleResponse);
				}

				return new ResponseModel(322, "InvoiceType không được hỗ trợ");
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> SearchByFkey(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertSearchByIdRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptV5Repository.SearchByFkey(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertSearchByFkeyResponse(vnptResponse, request),
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
					() => VnptConvertRequest.ConvertReplaceInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.ReplaceInvoice(vnptRequest),
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
					() => VnptConvertRequest.ConvertAdjustInvoiceRequest(request, request.Action),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.AdjustInvoice(vnptRequest),
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
				var invoice = request.Data as VoidInvoiceRequest;

				if (invoice.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
				{
					var vnptRequestUnPayment = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertUnConfirmPaymentRequest(request),
						Errors.EzInvoiceCreateRequest);

					await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.UnConfirmPaymentInvoice(vnptRequestUnPayment),
						Errors.ThirdPartyException);

					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertVoidInvoiceRequest(request),
						Errors.EzInvoiceCreateRequest);

					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.VoidInvoice(vnptRequest),
						Errors.ThirdPartyException);

					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertVoidInvoiceVNPTResponse(vnptResponse),
						Errors.EzInvoiceHandleResponse);
				}
				else
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertVoidInvoiceNoPayRequest(request),
						Errors.EzInvoiceCreateRequest);

					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.VoidInvoiceNoPay(vnptRequest),
						Errors.ThirdPartyException);

					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertVoidInvoiceResponse(vnptResponse),
						Errors.EzInvoiceHandleResponse);
				}
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> DeleteInvoice(VnptMappingContext request)
		{
			DeleteInvoiceRequest dataPost = request.Data as DeleteInvoiceRequest;
			if (dataPost.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
			{
				return new ResponseModel(322, "Đơn vi cung cấp không có chức năng này!");
			}

			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDeleteInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.DeleteInvoice(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDeleteInvoiceResponse(vnptResponse),
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
					() => VnptConvertRequest.ConvertDownloadPDFRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.DownloadPDF(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> DownloadPDFDraft(VnptMappingContext request)
		{
			DownloadPdfRequest dataPost = request.Data as DownloadPdfRequest;
			if (dataPost.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
			{
				return new ResponseModel(322, "Đơn vi cung cấp không có chức năng này!");
			}
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertDownloadPDFDraftRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.DownloadPDFDraft(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFDraftResponse(vnptResponse),
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
					() => VnptConvertRequest.ConvertPreviewPDFInvoiceDraftRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.DownloadPDFDraft(vnptRequest),
					Errors.ThirdPartyException);

				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertDownloadInvPDFDraftResponse(vnptResponse),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> CreateInvoice(VnptMappingContext request)
		{
			CreateInvoiceRequest bodyRequestToezInvoice = request.Data as CreateInvoiceRequest;
			if (bodyRequestToezInvoice.Invoice.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
			{
				try
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertCreateInvoiceReceiptRequest(request),
						Errors.EzInvoiceCreateRequest);


					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository_TLYT.CreateInvoiceReceipt(vnptRequest),
						Errors.ThirdPartyException);


					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertCreateInvoiceReceiptResponse(vnptResponse, request),
						Errors.EzInvoiceHandleResponse);
				}
				catch (BusinessException ex)
				{
					return ResponseModel.Exception(ex.Error, ex);
				}
			}
			else
			{
				try
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertCreateInvoiceRequest(request),
						Errors.EzInvoiceCreateRequest);


					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.CreateInvoice(vnptRequest),
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
		}

		private async Task<ResponseModel> CreateAndPublishInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertCreateAndPublishInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.CreateAndPublishInvoice(vnptRequest),
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

		private async Task<ResponseModel> CreateAndPublishMultiInvoice(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertCreateAndPublishMultiInvoiceRequest(request),
					Errors.EzInvoiceCreateRequest);


				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.CreateAndPublishInvoice(vnptRequest),
					Errors.ThirdPartyException);


				return ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertResponse.ConvertCreateAndPublishMultiInvoiceTLYTResponse(vnptResponse, request),
					Errors.EzInvoiceHandleResponse);
			}
			catch (BusinessException ex)
			{
				return ResponseModel.Exception(ex.Error, ex);
			}
		}

		private async Task<ResponseModel> PublishInvoice(VnptMappingContext request)
		{
			VoidInvoiceRequest bodyRequestToezInvoice = request.Data as VoidInvoiceRequest;
			if (bodyRequestToezInvoice.InvoiceType == (int)INV_TYPE.INV_RECEIPT)
			{
				try
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertPublishInvoiceReceiptRequest(request),
						Errors.EzInvoiceCreateRequest);


					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository_TLYT.PublishInvoiceReceipt(vnptRequest),
						Errors.ThirdPartyException);


					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertPublishInvoiceReceiptResponse(vnptResponse, request),
						Errors.EzInvoiceHandleResponse);
				}
				catch (BusinessException ex)
				{
					return ResponseModel.Exception(ex.Error, ex);
				}
			}
			else
			{
				try
				{
					var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertRequest.ConvertPublishInvoiceRequest(request),
						Errors.EzInvoiceCreateRequest);


					var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
						() => _vnptRepository.PublishInvoice(vnptRequest),
						Errors.ThirdPartyException);


					return ExecuteAndTranslateException.ActionCurrentProject(
						() => VnptConvertResponse.ConvertPublishInvoiceResponse(vnptResponse, request),
						Errors.EzInvoiceHandleResponse);
				}
				catch (BusinessException ex)
				{
					return ResponseModel.Exception(ex.Error, ex);
				}
			}
		}

		private async Task<ResponseModel> GetFormSerial(VnptMappingContext request)
		{
			try
			{
				var vnptRequest = ExecuteAndTranslateException.ActionCurrentProject(
					() => VnptConvertRequest.ConvertFormNoSerialRequest(request),
					Errors.EzInvoiceCreateRequest);

				var vnptResponse = await ExecuteAndTranslateException.ActionThirdPartyAsync(
					() => _vnptRepository.GetFormSerials(vnptRequest),
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

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}
