using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Cyberbill.Mapping;
using eInvoice.Providers.Cyberbill.Wire;

namespace eInvoice.Providers.Cyberbill;

// Preserve the approved two-step unsigned-create then HSM-sign workflow.
public sealed class CyberbillInvoiceProvider(CyberbillTransport transport) : IInvoiceProvider
{
    public int PartnerId => 7;
    public string Name => "CYBER_BILL";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check", [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.Void] = "void",
        [InvoiceAction.Create] = "create", [InvoiceAction.CreateAndPublish] = "create", [InvoiceAction.Change] = "change",
        [InvoiceAction.Publish] = "publish-existing", [InvoiceAction.Delete] = "delete",
        [InvoiceAction.Replace] = "replace", [InvoiceAction.Adjust] = "adjust",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is not ("health-check" or "document");
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        CreateInvoiceRequest invoice => [(operation == "change" ? "change:cyberbill:" : "draft:") + invoice.Invoice?.InvoiceId],
        PublishExistingInvoiceRequest invoice => ["publish:" + invoice.InvoiceId, "publish:cyberbill-code:" + invoice.ProviderInvoiceCode],
        DeleteInvoiceRequest invoice => ["delete:" + invoice.InvoiceId, "delete:cyberbill-code:" + invoice.ProviderInvoiceCode],
        VoidInvoiceRequest invoice => ["void:cyberbill:" + invoice.InvoiceId],
        _ => [],
    };
    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) =>
        _ = await transport.LoginAsync(site.Site, cancellationToken);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Cyberbill operation is not implemented yet.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var context = new CyberbillMappingContext { Company = site.Site, Data = copy, ExtraData = site.ExtraFieldMappings.ToList() };
        if (copy is CreateInvoiceRequest create && operation is "create" or "change" or "replace" or "adjust")
        {
            if (create.Invoice is null || create.Details is not { Count: > 0 }) Fail("invoice and details are required.");
            var data = create.Invoice!;
            Normal(data.InvoiceType); Require(data.InvoiceId, "invoice.invoiceId"); Require(data.FormNo, "invoice.formNo"); Require(data.Serial, "invoice.serial");
            if (!DateTime.TryParseExact(data.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoice.invoiceDate must be dd/MM/yyyy.");
            if (!int.TryParse(data.PaymentMethod, out var payment) || payment is < 1 or > 5) Fail("Cyberbill paymentMethod must be a code from 1 to 5.");
            if (operation is "replace" or "adjust")
            {
                if (create is not AdjustOrReplaceInvoiceRequest original || string.IsNullOrWhiteSpace(original.OriginalInvoiceId)) Fail("originalInvoiceId is required.");
                if (((AdjustOrReplaceInvoiceRequest)create).OriginalInvoiceId == data.InvoiceId) Fail("New and original invoiceIds must differ.");
            }
            object body = ProviderMapping.Run(() => operation switch
            {
                "replace" => CyberbillRequestMapper.ConvertReplaceInvoiceRequest(context).Body,
                "adjust" => CyberbillRequestMapper.ConvertAdjustInvoiceRequest(context).Body,
                _ => CyberbillRequestMapper.ConvertCreateInvoiceDraft(context).Body,
            }, "Cyberbill");
            var path = operation switch { "replace" => EndpointApi.REPLACE, "adjust" => EndpointApi.ADJUST, _ => EndpointApi.DRAFT };
            var result = await transport.ExecuteAsync(site.Site, path, body, true, cancellationToken);
            var code = Scalar(result, "magiaodich");
            if (string.IsNullOrWhiteSpace(code)) throw CyberbillTransport.Invalid();
            return new InvoiceResult(data.InvoiceId!, Scalar(result, "sohoadon"), code, code, null, null, true, null, ProviderInvoiceCode: code, RequiresSigning: true);
        }
        if (copy is PublishExistingInvoiceRequest sign && operation == "publish-existing")
        {
            Normal(sign.InvoiceType); Require(sign.InvoiceId, "invoiceId"); Require(sign.ProviderInvoiceCode, "providerInvoiceCode");
            var payload = ProviderMapping.Run(() => CyberbillRequestMapper.ConvertSignAndDownloadPDFCyberbill(context), "Cyberbill");
            var result = await transport.ExecuteAsync(site.Site, EndpointApi.SIGN, payload.Body, true, cancellationToken);
            var number = Scalar(result, "sohoadon"); if (string.IsNullOrWhiteSpace(number)) throw CyberbillTransport.Invalid();
            return new InvoiceResult(sign.InvoiceId!, number, sign.ProviderInvoiceCode, sign.ProviderInvoiceCode, Scalar(result, "macapcqt"), null, true, null,
                ProviderInvoiceCode: sign.ProviderInvoiceCode, RequiresSigning: false);
        }
        if (copy is DeleteInvoiceRequest deletion && operation == "delete")
        {
            Normal(deletion.InvoiceType); Require(deletion.InvoiceId, "invoiceId"); Require(deletion.ProviderInvoiceCode, "providerInvoiceCode");
            var payload = ProviderMapping.Run(() => CyberbillRequestMapper.ConvertDeleteInvoiceCyberbill(context), "Cyberbill");
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.DELETE, payload.Body, true, cancellationToken);
            return new { success = true };
        }
        if (operation == "document" && copy is DownloadPdfRequest document)
        {
            Normal(document.InvoiceType); Require(document.InvoiceId, "invoiceId"); Require(document.ProviderInvoiceCode, "providerInvoiceCode");
            if (document.TypeFileDownload is not (null or "pdf")) Fail("Cyberbill document supports pdf only.");
            var payload = ProviderMapping.Run(() => CyberbillRequestMapper.ConvertSignAndDownloadPDFCyberbill(context), "Cyberbill");
            var result = await transport.ExecuteAsync(site.Site, EndpointApi.DOWNLOAD_PDF, payload.Body, false, cancellationToken);
            var file = result["base64pdf"]?.Type == Newtonsoft.Json.Linq.JTokenType.String ? result.Value<string>("base64pdf") : null;
            if (string.IsNullOrWhiteSpace(file)) throw CyberbillTransport.Invalid();
            try { _ = Convert.FromBase64String(file); } catch (FormatException) { throw CyberbillTransport.Invalid(); }
            return new InvoiceDocumentResult(file, "base64");
        }
        if (operation == "void" && copy is VoidInvoiceRequest invoice)
        {
            Normal(invoice.InvoiceType); Require(invoice.InvoiceId, "invoiceId"); Require(invoice.FormNo, "formNo"); Require(invoice.Serial, "serial");
            if (!DateTime.TryParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoiceDate must be dd/MM/yyyy.");
            var payload = ProviderMapping.Run(() => CyberbillRequestMapper.ConvertVoidInvoiceCyberbill(context), "Cyberbill");
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.VOID, payload.Body, true, cancellationToken);
            return new { success = true };
        }
        throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Cyberbill does not support this request.");
    }
    private static void Normal(int? type) { if (type != 0) Fail("Cyberbill currently supports invoiceType 0 only."); }
    private static void Require(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) Fail(field + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
    private static string? Scalar(Newtonsoft.Json.Linq.JObject result, string key) => result[key] switch
    {
        null or { Type: Newtonsoft.Json.Linq.JTokenType.Null } => null,
        { Type: Newtonsoft.Json.Linq.JTokenType.String or Newtonsoft.Json.Linq.JTokenType.Integer } value => value.ToString(),
        _ => throw CyberbillTransport.Invalid(),
    };
}
