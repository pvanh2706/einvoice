using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa.Mapping;
using eInvoice.Providers.Misa.Wire;

namespace eInvoice.Providers.Misa;

public sealed class MisaInvoiceService(MisaBusinessTransport transport, TimeProvider clock) : IMisaInvoiceService
{
    public Task<IReadOnlyList<InvoiceResult>> PublishAsync(ResolvedSite site, CreateInvoiceRequest request, CancellationToken cancellationToken) =>
        Publish(site, [request], batch: false, cancellationToken);

    public Task<IReadOnlyList<InvoiceResult>> PublishBatchAsync(ResolvedSite site, InvoiceBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Invoices is null || request.Invoices.Count == 0) MisaRequestValidation.Fail("Batch must contain at least one invoice.");
        return Publish(site, request.Invoices!, batch: true, cancellationToken);
    }

    private async Task<IReadOnlyList<InvoiceResult>> Publish(ResolvedSite site, List<CreateInvoiceRequest> inputs, bool batch, CancellationToken cancellationToken)
    {
        foreach (var request in inputs)
        {
            if (request is null) MisaRequestValidation.Fail("Batch must not contain null invoices.");
            MisaRequestValidation.Create(request!);
            if (request!.SiteConfigInfo is not null && request.SiteConfigInfo.Site?.Id != site.Site.Id)
                MisaRequestValidation.Fail("Batch item site configuration must match the authorized site.");
        }
        var first = inputs[0];
        var invoiceType = first.Invoice!.InvoiceType!.Value;
        var mode = first.ElectronicInvoiceType;
        if (inputs.Any(item => item.Invoice!.InvoiceType != invoiceType || item.ElectronicInvoiceType != mode ||
                item.Invoice.Serial != first.Invoice.Serial || item.Invoice.FormNo != first.Invoice.FormNo) ||
            inputs.Select(item => item.Invoice!.InvoiceId).Distinct(StringComparer.Ordinal).Count() != inputs.Count)
            MisaRequestValidation.Fail("Batch invoices must share type, mode, form and serial, with distinct invoice IDs.");
        var requests = inputs.Select(Clone).ToList();
        object payload;
        string path;
        string? receiptToken = null;
        if (invoiceType == 2)
        {
            MisaRequestValidation.Require(site.Site.Username2, "receipt publishing username2");
            MisaRequestValidation.Require(site.Site.Password2, "receipt publishing password2");
            receiptToken = await transport.ReceiptTokenAsync(site.Site, cancellationToken);
            var serialData = await transport.SendAsync(site.Site, EndpointApi.INVOICE_SERIAL, null, true, false, cancellationToken, receiptToken);
            var serial = MisaBusinessTransport.ReadArray<InvFromSerialResponseModel>(serialData).FirstOrDefault(item => item.TemplateCode == first.Invoice.Serial);
            if (serial is null)
                throw new InvoiceRequestException("MISA_RECEIPT_TEMPLATE_NOT_FOUND", "Receipt serial does not match a published MISA template.");
            payload = Map(() => MisaRequestMapper.ConvertCreateAndPublishReceiptRequest(Context(site, requests),
                MisaRequestMapper.ConvertConnectionRequest(Context(site, requests)), serial));
            path = EndpointApi.CREATE_AND_PUBLISH_RECEIPT;
        }
        else if (invoiceType == 1)
        {
            payload = Map(() => MisaRequestMapper.ConvertCreateAndPublishTicketRequest(Context(site, requests)));
            path = EndpointApi.CREATE_AND_PUBLISH_TICKET;
        }
        else
        {
            var context = Context(site, batch ? requests : requests[0]);
            // Preserve dispatcher behavior until Q009 is answered: batch's legacy dispatcher used mode 0.
            var isMtt = IsMtt(first.Invoice.Serial, batch ? 0 : mode);
            if (isMtt && mode == 4)
                payload = Map<object>(() => batch ? MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTHSMMultiRequest(context)
                    : MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTHSMRequest(context));
            else if (isMtt || mode == 3)
                payload = Map<object>(() => batch ? MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTMultiRequest(context)
                    : MisaRequestMapper.ConvertCreateAndPublishInvoiceMTTRequest(context));
            else if (mode == 0)
                payload = Map<object>(() => batch ? MisaRequestMapper.ConvertCreateAndPublishInvoiceHSMMultiRequest(context)
                    : MisaRequestMapper.ConvertCreateAndPublishInvoiceHSMRequest(context));
            else throw new InvoiceRequestException("MISA_USB_WORKFLOW_REQUIRED", "Use the XML/signature workflow for USB invoices.");
            path = isMtt && mode != 4 || mode == 3 ? EndpointApi.CREATE_AND_PUBLISH_INVOICE_MTT : EndpointApi.CREATE_AND_PUBLISH_INVOICE_HSM;
        }
        var data = await transport.SendAsync(site.Site, path, payload, invoiceType == 2, true, cancellationToken, receiptToken);
        return MisaResultMapper.Published(data, inputs.Select(item => item.Invoice!.InvoiceId!).ToList(), invoiceType, batch);
    }

    public async Task<InvoiceDocumentResult> PreviewAsync(ResolvedSite site, AdjustOrReplaceInvoiceRequest request, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Create(request, request.Invoice?.PreviewType is 2 or 3);
        RequireNormal(request.Invoice!.InvoiceType);
        var copy = Clone(request);
        var payload = Map(() => MisaRequestMapper.ConvertPreviewInvoiceRequest(Context(site, copy), IsMtt(copy.Invoice!.Serial, copy.ElectronicInvoiceType)));
        var data = await transport.SendAsync(site.Site, EndpointApi.PREVIEW_INVOICE, payload, false, true, cancellationToken);
        if (string.IsNullOrEmpty(data)) throw MisaBusinessTransport.InvalidResponse();
        return new(data, "url");
    }

    public async Task<IReadOnlyList<InvoiceResult>> AdjustAsync(ResolvedSite site, AdjustOrReplaceInvoiceRequest request, bool replace, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Create(request, reference: true);
        RequireNormal(request.Invoice!.InvoiceType);
        var copy = Clone(request);
        var context = Context(site, copy, replace ? InvoiceAction.Replace : InvoiceAction.Adjust);
        var mtt = IsMtt(copy.Invoice!.Serial, copy.ElectronicInvoiceType);
        object payload;
        string path;
        if (mtt)
        {
            payload = Map(() => MisaRequestMapper.ConvertAdjustAndReplaceMTTRequest(context));
            path = EndpointApi.CREATE_AND_PUBLISH_INVOICE_MTT;
        }
        else if (RequiresAdjustmentXml(copy))
        {
            payload = Map(() => MisaRequestMapper.ConvertAdjustAndReplaceUsbTokenRequest(context));
            path = EndpointApi.CREATE_INVOICE_USB_TOKEN;
        }
        else
        {
            payload = Map(() => MisaRequestMapper.ConvertAdjustAndReplaceHSMRequest(context));
            path = EndpointApi.CREATE_AND_PUBLISH_INVOICE_HSM;
        }
        var data = await transport.SendAsync(site.Site, path, payload, false, true, cancellationToken);
        return MisaResultMapper.Published(data, [request.Invoice.InvoiceId!], 0, false);
    }

    public async Task<InvoiceDocumentResult> DownloadAsync(ResolvedSite site, DownloadPdfRequest request, CancellationToken cancellationToken)
    {
        if (request.InvoiceType is not (0 or 1 or 2)) MisaRequestValidation.Fail("invoiceType must be 0, 1 or 2.");
        var receipt = request.InvoiceType == 2;
        var id = request.InvoiceType == 0 ? request.SearchCode : request.TransactionId;
        MisaRequestValidation.Require(id, request.InvoiceType == 0 ? "searchCode" : "transactionId");
        var fileType = request.TypeFileDownload ?? "pdf";
        if (fileType is not ("pdf" or "xml")) MisaRequestValidation.Fail("typeFileDownload must be pdf or xml.");
        var path = receipt ? EndpointApi.DOWNLOAD_INVOICE : request.InvoiceType == 1 ? EndpointApi.DOWNLOAD_PDF :
            IsMtt(request.Serial, request.ElectronicInvoiceType) ? EndpointApi.DOWNLOAD_PDF_MTT : EndpointApi.DOWNLOAD_PDF_HSM + "=" + fileType;
        var data = await transport.SendAsync(site.Site, path, new[] { id! }, receipt, false, cancellationToken);
        return MisaResultMapper.Document(data);
    }

    public async Task VoidAsync(ResolvedSite site, VoidInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request.InvoiceType is not (0 or 1 or 2)) MisaRequestValidation.Fail("invoiceType must be 0, 1 or 2.");
        MisaRequestValidation.Require(request.Reason, "reason");
        MisaRequestValidation.Require(request.ProviderInvoiceNumber, "providerInvoiceNumber");
        MisaRequestValidation.Require(request.InvoiceType == 0 ? request.SearchCode : request.TransactionId, "transactionId/searchCode");
        var context = Context(site, request);
        var receipt = request.InvoiceType == 2;
        var payload = Map<object>(() => receipt ? MisaRequestMapper.ConvertCancelInvoiceModel(context) :
            request.InvoiceType == 0 ? MisaRequestMapper.ConvertCancelMTTModel(context) : MisaRequestMapper.ConvertCancelTicketModel(context));
        var path = receipt ? EndpointApi.CANCEL_INVOICE : request.InvoiceType == 0 ? EndpointApi.VOID_INVOICE_MTT : EndpointApi.VOID_INVOICE;
        _ = await transport.SendAsync(site.Site, path, payload, receipt, false, cancellationToken);
    }

    public Task<string?> SendTaxAsync(ResolvedSite site, SendPendingTaxAuthorityRequest request, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Require(site.Site.Username2, "site.username2 (MISA AppID)");
        var payload = Map(() => MisaRequestMapper.ConvertSendCQTRequest(Context(site, request)));
        return transport.SendAsync(site.Site, EndpointApi.SEND_INVOICE_MTT, payload, false, false, cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceStatusResult>> StatusAsync(ResolvedSite site, SendToTaxAuthorityRequest request, CancellationToken cancellationToken)
    {
        RequireNormal(request.InvoiceType);
        if (request.InvoiceIds is null || request.InvoiceIds.Count == 0 || request.InvoiceIds.Any(string.IsNullOrWhiteSpace))
            MisaRequestValidation.Fail("invoiceIds must contain nonempty PMS invoice IDs.");
        var payload = Map(() => MisaRequestMapper.ConverGetStatusInvoiceByRefIDMTTRequest(Context(site, request)));
        var path = IsMtt(request.Serial, request.ElectronicInvoiceType) ? EndpointApi.GET_STATUS_INVOICE_BY_REFID_MTT : EndpointApi.GET_STATUS_INVOICE_BY_REFID;
        var data = await transport.SendAsync(site.Site, path, payload, false, false, cancellationToken);
        var result = MisaResultMapper.Status(data);
        if (result.Count == 0 || result.Any(item => !request.InvoiceIds!.Contains(item.InvoiceId, StringComparer.Ordinal)))
            throw MisaBusinessTransport.InvalidResponse();
        return result;
    }

    public async Task<IReadOnlyList<InvoiceResult>> CreateXmlAsync(ResolvedSite site, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Create(request);
        RequireNormal(request.Invoice!.InvoiceType);
        var payload = Map(() => MisaRequestMapper.ConvertGetXMLInvoiceRequest(Context(site, Clone(request))));
        var data = await transport.SendAsync(site.Site, EndpointApi.CREATE_INVOICE_USB_TOKEN, payload, false, true, cancellationToken);
        return MisaResultMapper.Published(data, [request.Invoice.InvoiceId!], 0, false);
    }

    public async Task<IReadOnlyList<InvoiceHashResult>> HashAsync(ResolvedSite site, GetXmlHashRequest request, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Require(request.CertBase64, "certBase64");
        try { _ = Convert.FromBase64String(request.CertBase64!); }
        catch (FormatException) { MisaRequestValidation.Fail("certBase64 must be valid Base64."); }
        var payload = Map(() => MisaRequestMapper.ConvertGetXMLInvoiceHashRequest(Context(site, request)));
        var data = await transport.SendAsync(site.Site, EndpointApi.GET_XML_INVOICE_HASH, payload, false, false, cancellationToken);
        return MisaResultMapper.Hash(data);
    }

    public Task<string?> SendSignedTaxAsync(ResolvedSite site, SendSignedTaxAuthorityRequest request, CancellationToken cancellationToken)
    {
        MisaRequestValidation.Require(request.SignatureValue, "signatureValue");
        MisaRequestValidation.Require(request.MessageCode, "messageCode");
        var payload = Map(() => MisaRequestMapper.ConvertSendInvoiceCQTUSBTokenRequest(Context(site, request)));
        return transport.SendAsync(site.Site, EndpointApi.SEND_INVOICE_MTT, payload, false, false, cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceResult>> PublishSignedXmlAsync(ResolvedSite site, PublishSignedXmlRequest request, CancellationToken cancellationToken)
    {
        RequireNormal(request.InvoiceType);
        MisaRequestValidation.Require(request.InvoiceId, "invoiceId");
        MisaRequestValidation.Require(request.SearchCode, "searchCode");
        MisaRequestValidation.Require(request.InvoiceData, "invoiceData");
        var payload = Map(() => MisaRequestMapper.ConvertCreateAndPublishInvoiceXMLRequest(Context(site, request)));
        var data = await transport.SendAsync(site.Site, EndpointApi.CREATE_AND_PUBLISH_INVOICE_USB_TOKEN, payload, false, true, cancellationToken);
        return MisaResultMapper.Published(data, [request.InvoiceId!], 0, false);
    }

    private MisaMappingContext Context(ResolvedSite site, object request, InvoiceAction action = InvoiceAction.CreateAndPublish) => new()
    {
        Company = site.Site, Data = request, ExtraData = site.ExtraFieldMappings,
        Action = (int)action, Now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)),
    };
    public static bool RequiresAdjustmentXml(AdjustOrReplaceInvoiceRequest request) =>
        request.ElectronicInvoiceType == 2 && !IsMtt(request.Invoice?.Serial, request.ElectronicInvoiceType);
    private static bool IsMtt(string? serial, int? mode) => mode == 1 || serial is { Length: >= 3 } && serial[^3] == 'M';
    private static void RequireNormal(int? type)
    {
        if (type != 0) throw new InvoiceRequestException("INVOICE_TYPE_NOT_SUPPORTED", "This MISA operation supports invoiceType 0 only.");
    }
    private static T Clone<T>(T value)
    {
        var copy = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
        if (copy is CreateInvoiceRequest invoice) invoice.ExtraData ??= [];
        return copy;
    }
    private static T Map<T>(Func<T> map)
    {
        try { return map(); }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or NullReferenceException or ArgumentException or OverflowException)
        {
            throw new InvoiceRequestException("INVALID_INVOICE_MAPPING_INPUT", "Invoice fields do not satisfy the MISA mapping requirements.");
        }
    }
}
