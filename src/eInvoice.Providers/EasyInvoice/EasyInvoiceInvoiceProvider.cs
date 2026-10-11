using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.EasyInvoice.Mapping;
using eInvoice.Providers.EasyInvoice.Wire;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.EasyInvoice;

public sealed class EasyInvoiceInvoiceProvider(EasyInvoiceTransport transport) : IInvoiceProvider
{
    public int PartnerId => 9;
    public string Name => "EASY_INVOICE";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Create] = "create", [InvoiceAction.Draft] = "draft", [InvoiceAction.Delete] = "delete",
        [InvoiceAction.CreateAndPublish] = "publish", [InvoiceAction.Publish] = "publish-existing",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.Preview] = "preview", [InvoiceAction.Void] = "void",
        [InvoiceAction.AdjustIncrease] = "adjust/increase", [InvoiceAction.AdjustDecrease] = "adjust/decrease",
        [InvoiceAction.AdjustInformation] = "adjust/information", [InvoiceAction.Replace] = "replace",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is not ("document" or "preview");
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        CreateInvoiceRequest data when operation != "preview" => [(operation is "draft" or "create" ? "draft:" : "publish:") + data.Invoice?.InvoiceId],
        PublishExistingInvoiceRequest data => ["publish:" + data.InvoiceId],
        DeleteInvoiceRequest data => ["delete:" + data.InvoiceId],
        VoidInvoiceRequest data => ["void:" + data.InvoiceId],
        _ => [],
    };
    public Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) =>
        throw new UnsupportedProviderOperationException("EASY_INVOICE_HEALTH_NOT_VERIFIED", "EasyInvoice has no connection verification API in the baseline; health behavior is awaiting a decision.");

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "EasyInvoice does not support this operation.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        object input = copy;
        string? expectedId = null;
        if (copy is CreateInvoiceRequest create)
        {
            if (create.Invoice is null) Fail("invoice is required.");
            var invoice = create.Invoice!;
            Normal(invoice.InvoiceType); Require(invoice.InvoiceId, "invoice.invoiceId"); Require(invoice.Serial, "invoice.serial");
            if (!DateTime.TryParseExact(invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoice.invoiceDate must be dd/MM/yyyy.");
            if (create.Details is not { Count: > 0 }) Fail("details must contain at least one item.");
            expectedId = invoice.InvoiceId;
            var originalRequired = operation is "replace" or "adjust/increase" or "adjust/decrease" or "adjust/information" ||
                operation == "preview" && invoice.PreviewType is 3 or 4 or 5 or 6;
            if (originalRequired)
            {
                if (create is not AdjustOrReplaceInvoiceRequest) Fail("Original invoice information is required.");
                var original = (AdjustOrReplaceInvoiceRequest)create;
                Require(original.OriginalInvoiceId, "originalInvoiceId");
                if (original.OriginalInvoice is null) Fail("originalInvoice is required.");
                Require(original.OriginalInvoice!.InvoiceId, "originalInvoice.invoiceId");
                if (original.OriginalInvoice.InvoiceId != original.OriginalInvoiceId) Fail("originalInvoice.invoiceId must match originalInvoiceId.");
                SameKey(original.OriginalProviderInvoiceCode, original.OriginalInvoiceId);
                if (original.OriginalInvoiceId == invoice.InvoiceId) Fail("The new invoiceId must differ from originalInvoiceId.");
            }
            if (operation == "preview" && invoice.PreviewType is not (null or 1 or 2 or 3 or 4 or 5 or 6)) Fail("invoice.previewType is unsupported.");
        }
        else if (copy is PublishExistingInvoiceRequest sign)
        {
            Normal(sign.InvoiceType); Require(sign.InvoiceId, "invoiceId"); Require(sign.Serial, "serial"); SameKey(sign.ProviderInvoiceCode, sign.InvoiceId);
            input = new VoidInvoiceRequest { InvoiceId = sign.InvoiceId, Serial = sign.Serial, InvoiceType = sign.InvoiceType };
            expectedId = sign.InvoiceId;
        }
        else if (copy is DownloadPdfRequest document)
        {
            Normal(document.InvoiceType); Require(document.InvoiceId, "invoiceId"); Require(document.Serial, "serial"); SameKey(document.ProviderInvoiceCode, document.InvoiceId);
            if (document.TypeFileDownload is not (null or "pdf")) Fail("EasyInvoice document supports pdf only.");
        }
        else if (copy is DeleteInvoiceRequest delete)
        {
            Normal(delete.InvoiceType); Require(delete.InvoiceId, "invoiceId"); Require(delete.Serial, "serial");
        }
        else if (copy is VoidInvoiceRequest cancel)
        {
            Normal(cancel.InvoiceType); Require(cancel.InvoiceId, "invoiceId"); Require(cancel.Serial, "serial"); SameKey(cancel.ProviderInvoiceCode, cancel.InvoiceId);
        }
        else throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "EasyInvoice does not support this request.");
        // Validate configuration before building XML; endpoint and authentication never come from PMS.
        _ = transport.Validate(site.Site);
        var action = Routes.Single(p => p.Value == operation).Key;
        var context = new EasyInvoiceMappingContext { Company = site.Site, Data = input, ExtraData = site.ExtraFieldMappings.ToList(), Action = action };
        var payload = ProviderMapping.Run(() => operation switch
        {
            "create" => EasyInvoiceRequestMapper.ConvertCreateInvoiceRequest(context),
            "draft" => EasyInvoiceRequestMapper.ConvertCreateDraftInvoiceRequest(context),
            "publish" => EasyInvoiceRequestMapper.ConvertCreateAndPublishInvoiceRequest(context),
            "publish-existing" => EasyInvoiceRequestMapper.ConvertPublishInvoiceRequest(context),
            "replace" => EasyInvoiceRequestMapper.ConvertReplaceInvoiceRequest(context),
            "adjust/increase" or "adjust/decrease" or "adjust/information" => EasyInvoiceRequestMapper.ConvertAdjustInvoiceRequest(context),
            "preview" => EasyInvoiceRequestMapper.ConvertPreviewInvoiceRequest(context),
            "document" => EasyInvoiceRequestMapper.ConvertGetPDFInvoiceRequest(context),
            "delete" => EasyInvoiceRequestMapper.ConvertDeleteInvoiceRequest(context),
            "void" => EasyInvoiceRequestMapper.ConvertVoidInvoiceRequest(context),
            _ => throw new InvalidOperationException(),
        }, "EasyInvoice");
        var path = operation switch
        {
            "create" or "draft" => "api/publish/importInvoice", "publish" => "api/publish/importAndPublishInvoice",
            "publish-existing" => "api/publish/issueInvoices", "replace" => "api/business/replaceInvoice",
            "adjust/increase" or "adjust/decrease" or "adjust/information" => "api/business/adjustInvoice",
            "preview" => "api/publish/previewInvoice", "document" => "api/publish/getInvoicePdf",
            "delete" => "api/business/removeUnsignedInvoice", "void" => "api/business/cancelInvoice", _ => throw new InvalidOperationException(),
        };
        if (operation is "document" or "preview")
            return new InvoiceDocumentResult(await transport.DocumentAsync(site.Site, path, payload.bodyPost, cancellationToken), "base64");
        var json = await transport.ExecuteAsync(site.Site, path, payload.bodyPost, true, cancellationToken);
        if (operation is "void" or "delete") return new { success = true };
        var resultToken = json["Data"] is JObject dataContainer ? dataContainer["Invoices"] : null;
        if (resultToken?.Type == JTokenType.String)
        {
            try { resultToken = JToken.Parse(resultToken.Value<string>()!); } catch (Newtonsoft.Json.JsonException) { throw EasyInvoiceTransport.Invalid(); }
        }
        if (resultToken is not JArray { Count: 1 } results || results[0] is not JObject result) throw EasyInvoiceTransport.Invalid();
        if (EasyInvoiceTransport.Scalar(result["Ikey"]) != expectedId) throw EasyInvoiceTransport.Invalid();
        var number = EasyInvoiceTransport.Scalar(result["No"]);
        if (operation is "create" or "draft") number = null; // Import creates an unsigned invoice without a reserved number.
        else if (!long.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var issuedNumber) || issuedNumber <= 0) throw EasyInvoiceTransport.Invalid();
        return new[] { new InvoiceResult(expectedId!, number, null, EasyInvoiceTransport.Scalar(result["LookupCode"]), EasyInvoiceTransport.Scalar(result["TaxAuthorityCode"]), null, true, null) };
    }
    private static void SameKey(string? supplierCode, string? pmsId) { if (supplierCode is not null && supplierCode != pmsId) Fail("EasyInvoice Ikey is the PMS invoiceId; providerInvoiceCode must match it when supplied."); }
    private static void Normal(int? type) { if (type != 0) Fail("EasyInvoice currently supports invoiceType 0 only."); }
    private static void Require(string? value, string name) { if (string.IsNullOrWhiteSpace(value)) Fail(name + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
}
