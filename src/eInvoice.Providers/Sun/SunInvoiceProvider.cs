using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Sun.Mapping;
using eInvoice.Providers.Sun.Wire;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Sun;

public sealed class SunInvoiceProvider(SunTransport transport, TimeProvider clock) : IInvoiceProvider
{
    public int PartnerId => 3;
    public string Name => "SUN";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.HealthCheck] = "health-check", [InvoiceAction.Draft] = "draft",
        [InvoiceAction.CreateAndPublish] = "publish", [InvoiceAction.Preview] = "preview",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.DownloadDraftPdf] = "document/draft",
        [InvoiceAction.Void] = "void", [InvoiceAction.Replace] = "replace",
        [InvoiceAction.AdjustIncrease] = "adjust/increase", [InvoiceAction.AdjustDecrease] = "adjust/decrease",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is "publish" or "draft" or "void" or "replace" or "adjust/increase" or "adjust/decrease";
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        CreateInvoiceRequest data when operation != "preview" => [
            (operation == "draft" ? "draft:" : "publish:") + data.Invoice?.InvoiceId,
            (operation == "draft" ? "draft:sun-sid:" : "publish:sun-sid:") + Sid(data.Invoice?.InvoiceId)],
        VoidInvoiceRequest data => ["void:sun-sid:" + data.Sid],
        _ => [],
    };
    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) => _ = await transport.LoginAsync(site.Site, cancellationToken);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Sun does not support this operation.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var context = new SunMappingContext { Company = site.Site, Data = copy, ExtraData = site.ExtraFieldMappings.ToList(), Now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime };
        if (copy is DownloadPdfRequest document)
        {
            Normal(document.InvoiceType); Require(document.SearchCode, "searchCode"); PdfType(document.TypeFileDownload);
            var payload = ProviderMapping.Run(() => SunRequestMapper.ConvertDownloadPDFRequest(context, operation == "document/draft").Body, "Sun");
            return await Document(site.Site, payload, cancellationToken);
        }
        if (copy is VoidInvoiceRequest cancel)
        {
            Normal(cancel.InvoiceType); Require(cancel.Sid, "sid"); Require(cancel.Reason, "reason");
            var payload = ProviderMapping.Run(() => SunRequestMapper.ConvertVoidInvoiceRequest(context).Body, "Sun");
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.VOID, payload, true, cancellationToken);
            return new { success = true };
        }
        if (copy is CreateInvoiceRequest data)
        {
            if (data.Invoice is null) Fail("invoice is required.");
            Normal(data.Invoice!.InvoiceType);
            if (operation == "preview")
            {
                Require(data.Invoice.SearchCode, "invoice.searchCode");
                var preview = ProviderMapping.Run(() => SunRequestMapper.ConvertDownloadPDFDraftRequest(context).Body, "Sun");
                return await Document(site.Site, preview, cancellationToken);
            }
            _ = Sid(data.Invoice.InvoiceId); Require(data.Invoice.FormNo, "invoice.formNo"); Require(data.Invoice.Serial, "invoice.serial");
            if (!DateTime.TryParseExact(data.Invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoice.invoiceDate must be dd/MM/yyyy.");
            if (data.Details is not { Count: > 0 }) Fail("details must contain at least one item.");
            if (operation is "replace" or "adjust/increase" or "adjust/decrease")
            {
                if (data is not AdjustOrReplaceInvoiceRequest original) Fail("Original invoice information is required.");
                Require(((AdjustOrReplaceInvoiceRequest)data).OriginalProviderInvoiceNumber, "originalProviderInvoiceNumber");
                Require(data.Invoice.Sid, "invoice.sid");
            }
            var payload = ProviderMapping.Run<SunInvoiceModel>(() => operation switch
            {
                "replace" => SunRequestMapper.ConvertInvoiceReplaceRequest(context).Body,
                "adjust/increase" => SunRequestMapper.ConvertInvoiceAdjustRequest(context, 2).Body,
                "adjust/decrease" => SunRequestMapper.ConvertInvoiceAdjustRequest(context, 1).Body,
                _ => SunRequestMapper.ConvertCreateAndPublishInvoiceRequest(context).Body,
            }, "Sun");
            var path = operation switch { "draft" => EndpointApi.CREATE_INVOICE_DRAFT, "replace" => EndpointApi.REPLACE, "adjust/increase" or "adjust/decrease" => EndpointApi.ADJUST, _ => EndpointApi.CREATE_AND_PUBLISH_INVOICE };
            var json = await transport.ExecuteAsync(site.Site, path, payload, true, cancellationToken);
            if (json["success"] is not JArray { Count: 1 } results || results[0] is not JObject result) throw SunTransport.Invalid();
            var returnedSid = SunTransport.Scalar(result["sid"]);
            if (returnedSid is not null && returnedSid != payload.sid) throw SunTransport.Invalid();
            var number = operation == "draft" ? null : SunTransport.Scalar(result["seq"]);
            var searchCode = SunTransport.Scalar(result["sec"]);
            if (string.IsNullOrWhiteSpace(searchCode) || operation != "draft" && string.IsNullOrWhiteSpace(number)) throw SunTransport.Invalid();
            return new[] { new InvoiceResult(data.Invoice.InvoiceId!, number, payload.sid, searchCode, null, null, true, null) };
        }
        throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Sun does not support this request.");
    }
    private async Task<InvoiceDocumentResult> Document(SiteConfiguration site, object payload, CancellationToken cancellationToken)
    {
        var json = await transport.ExecuteAsync(site, EndpointApi.DOWNLOAD_PDF, payload, false, cancellationToken, pdf: true);
        var content = json.Value<string>("pdf")!;
        const string prefix = "data:application/pdf;base64,";
        if (content.StartsWith(prefix, StringComparison.Ordinal)) content = content[prefix.Length..].TrimStart();
        if (string.IsNullOrWhiteSpace(content)) throw SunTransport.Invalid();
        try { _ = Convert.FromBase64String(content); } catch (FormatException) { throw SunTransport.Invalid(); }
        return new InvoiceDocumentResult(content, "base64");
    }
    internal static string Sid(string? id)
    {
        Require(id, "invoice.invoiceId"); var stripped = id!.Replace("-", "", StringComparison.Ordinal);
        if (stripped.Length < 32) Fail("Sun invoice.invoiceId must have at least 32 characters after removing hyphens; use a stable GUID.");
        return stripped[..32];
    }
    private static void PdfType(string? type) { if (type is not (null or "pdf")) Fail("Sun document supports pdf only."); }
    private static void Normal(int? type) { if (type != 0) Fail("Sun currently supports invoiceType 0 only."); }
    private static void Require(string? value, string name) { if (string.IsNullOrWhiteSpace(value)) Fail(name + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
}
