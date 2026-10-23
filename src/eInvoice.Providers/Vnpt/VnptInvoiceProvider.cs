using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Vnpt.Baseline;

namespace eInvoice.Providers.Vnpt;

public sealed class VnptInvoiceProvider(IVnptSoapInvoker invoker, VnptOptions options, TimeProvider clock)
    : VnptProviderBase(invoker, options, clock, false);
public sealed class VnptV5InvoiceProvider(IVnptSoapInvoker invoker, VnptOptions options, TimeProvider clock)
    : VnptProviderBase(invoker, options, clock, true);

public abstract class VnptProviderBase : IInvoiceProvider
{
    private readonly IVnptSoapInvoker invoker;
    private readonly VnptOptions options;
    private readonly TimeProvider clock;
    private readonly bool v5;
    protected VnptProviderBase(IVnptSoapInvoker invoker, VnptOptions options, TimeProvider clock, bool v5)
    {
        this.invoker = invoker; this.options = options; this.clock = clock; this.v5 = v5;
        var operation = new Dictionary<InvoiceAction, string>
        {
            [InvoiceAction.HealthCheck] = "health-check", [InvoiceAction.GetFormSerials] = "form-serials",
            [InvoiceAction.Draft] = "draft", [InvoiceAction.CreateAndPublish] = "publish",
            [InvoiceAction.CreateAndPublishBatch] = "publish-batch", [InvoiceAction.Publish] = "publish-existing",
            [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.DownloadDraftPdf] = "document/draft",
            [InvoiceAction.Preview] = "preview", [InvoiceAction.Void] = "void", [InvoiceAction.Delete] = "delete",
            [InvoiceAction.AdjustIncrease] = "adjust/increase", [InvoiceAction.AdjustDecrease] = "adjust/decrease",
            [InvoiceAction.AdjustInformation] = "adjust/information", [InvoiceAction.Replace] = "replace",
            [InvoiceAction.GetInvoiceStatus] = "status",
        };
        if (v5)
        {
            operation[InvoiceAction.Create] = "create"; operation[InvoiceAction.EditDraft] = "draft/edit";
            operation[InvoiceAction.Adjust] = "adjust"; operation[InvoiceAction.GetCertificateInformation] = "certificate";
            operation[InvoiceAction.SendToTaxAuthority] = "tax/send";
        }
        else operation[InvoiceAction.SearchById] = "search";
        Routes = operation;
    }
    public int PartnerId => v5 ? 5 : 1;
    public string Name => v5 ? "VNPT_V5" : "VNPT";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; }
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is not ("health-check" or "form-serials" or "certificate" or "search" or "status" or "document" or "document/draft");
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        InvoiceBatchRequest batch => batch.Invoices?.SelectMany(i => Claims(operation, i)).ToArray() ?? [],
        CreateInvoiceRequest invoice => [(operation is "draft" or "draft/edit" ? "draft:" : operation == "preview" ? "preview:" : "publish:") + invoice.Invoice?.InvoiceId],
        PublishExistingInvoiceRequest invoice => ["publish:" + invoice.InvoiceId],
        DeleteInvoiceRequest invoice => ["delete:" + invoice.InvoiceId],
        VoidInvoiceRequest invoice => ["void:vnpt:" + invoice.InvoiceId],
        SendToTaxAuthorityRequest invoice => invoice.InvoiceIds?.Select(i => "tax-send:" + i).ToArray() ?? [],
        _ => [],
    };
    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken ct) =>
        _ = await ExecuteAsync("form-serials", site, new GetFormSerialsRequest(), ct);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken ct)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "VNPT does not support this operation.");
        var url = site.Site.PartnerUrl;
        if (url is null || !url.EndsWith('/')) throw new ProviderCallException(ProviderFailure.Configuration, "VNPT_ENDPOINT_NOT_ALLOWED", "VNPT base URL must end in a slash.");
        _ = new VnptSoapInvoker(options).Validate(url);
        if (string.IsNullOrWhiteSpace(site.Site.Username2) || string.IsNullOrWhiteSpace(site.Site.Password2) ||
            IsWrite(operation) && (string.IsNullOrWhiteSpace(site.Site.Username) || string.IsNullOrWhiteSpace(site.Site.Password)))
            throw new ProviderCallException(ProviderFailure.InvalidCredentials, "VNPT_CREDENTIALS_REQUIRED", "VNPT credentials are incomplete.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var items = copy is InvoiceBatchRequest batch ? batch.Invoices : copy is CreateInvoiceRequest create ? [create] : null;
        if (items is not null)
        {
            if (items.Count == 0 || items.Count > 500) Fail("Supply 1 to 500 invoices.");
            foreach (var item in items)
            {
                if (item.Invoice is null || item.Details is not { Count: > 0 }) Fail("invoice and details are required.");
                Require(item.Invoice!.InvoiceId, "invoice.invoiceId"); Require(item.Invoice.FormNo, "invoice.formNo"); Require(item.Invoice.Serial, "invoice.serial");
                if (!DateTime.TryParseExact(item.Invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoice.invoiceDate must be dd/MM/yyyy.");
                if (item.Invoice.InvoiceType is not (0 or 1 or 2)) Fail("invoiceType is unsupported.");
                if (item.Invoice.Serial != items[0].Invoice!.Serial || item.Invoice.FormNo != items[0].Invoice!.FormNo || item.Invoice.InvoiceType != items[0].Invoice!.InvoiceType)
                    Fail("Batch serial, formNo and invoiceType must match.");
            }
            if (items.Select(i => i.Invoice!.InvoiceId).Distinct().Count() != items.Count) Fail("Batch invoiceIds must be unique.");
            if (copy is InvoiceBatchRequest && items.Any(i => !Guid.TryParseExact(i.Invoice!.InvoiceId, "D", out _)))
                Fail("VNPT batch invoiceIds must be GUIDs in the dashed D format required by the baseline result contract.");
        }
        if (copy is AdjustOrReplaceInvoiceRequest original && operation != "preview")
        { Require(original.OriginalInvoiceId, "originalInvoiceId"); if (original.Invoice?.InvoiceId == original.OriginalInvoiceId) Fail("New and original invoiceIds must differ."); }
        if (copy is SendToTaxAuthorityRequest tax)
        {
            if (tax.InvoiceIds is not { Count: > 0 } || tax.InvoiceIds.Any(string.IsNullOrWhiteSpace)) Fail("invoiceIds are required.");
            if (tax.InvoiceIds.Distinct().Count() != tax.InvoiceIds.Count || tax.InvoiceIds.Count > (operation == "status" ? 100 : 500)) Fail("invoiceIds are duplicated or exceed the operation limit.");
            Require(tax.FormNo, "formNo"); if (operation == "tax/send") Require(tax.Serial, "serial");
        }
        if (copy is DownloadPdfRequest doc) Require(doc.InvoiceId, "invoiceId");
        if (copy is SearchInvoiceRequest search) Require(search.InvoiceId, "invoiceId");
        if (copy is VoidInvoiceRequest cancel) Require(cancel.InvoiceId, "invoiceId");
        if (copy is PublishExistingInvoiceRequest sign) { Require(sign.InvoiceId, "invoiceId"); Require(sign.FormNo, "formNo"); Require(sign.Serial, "serial"); }
        var serial = items?.FirstOrDefault()?.Invoice?.Serial ?? (copy as DownloadPdfRequest)?.Serial ?? (copy as VoidInvoiceRequest)?.Serial;
        object data = copy is InvoiceBatchRequest b ? b.Invoices! : copy;
        if (copy is PublishExistingInvoiceRequest existing)
            data = new VoidInvoiceRequest { InvoiceId = existing.InvoiceId, InvoiceType = existing.InvoiceType, FormNo = existing.FormNo, Serial = existing.Serial };
        var context = new VnptMappingContext { Company = site.Site, Data = data,
            ExtraData = site.ExtraFieldMappings.ToList(), Action = (int)Routes.Single(p => p.Value == operation).Key,
            IsSerialMTT = serial is { Length: >= 3 } && serial[^3] == 'M',
            HaveDeleteInvoiceDraft = operation == "publish" && copy is CreateInvoiceRequest p && p.DeleteExistingDraft != false };
        using var scope = new VnptMappingScope(site.Site, IsWrite(operation), clock);
        var previousCulture = CultureInfo.CurrentCulture;
        ResponseModel result;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var repository5 = new VnptV5Repository(invoker, ct) { Url = url };
            if (v5) result = await new VnptV5BaselineService(repository5).SyncDataToVnpt(context);
            else
            {
                var service = new VnptBaselineService(new VnptRepository(invoker, ct), new VnptRepository_TLYT(invoker, ct), repository5) { Url = url, Url_TLYT = url };
                result = await service.SyncDataToVnpt(context);
            }
        }
        catch (Exception error) when (error is FormatException or IndexOutOfRangeException or System.Xml.XmlException or Newtonsoft.Json.JsonException)
        { throw new ProviderCallException(ProviderFailure.InvalidResponse, "VNPT_INVALID_RESPONSE", "VNPT response is invalid."); }
        finally { CultureInfo.CurrentCulture = previousCulture; }
        if (result.Status != 200)
            throw new ProviderCallException(ProviderFailure.Rejected, "VNPT_OPERATION_REJECTED", "VNPT rejected the request.")
            { SupplierCode = "VNPT_OPERATION_REJECTED", SupplierMessage = ProviderErrorMessage.Sanitize(result.Message, site.Site, null, Name) };
        if (copy is InvoiceBatchRequest expectedBatch)
        {
            if (result.Data is not IEnumerable<PmsInvoiceResponse> returned) throw InvalidResponse();
            var ids = returned.Select(i => i.InvoiceId).ToArray();
            if (ids.Length != expectedBatch.Invoices!.Count || ids.Distinct().Count() != ids.Length ||
                ids.Except(expectedBatch.Invoices.Select(i => i.Invoice!.InvoiceId)).Any()) throw InvalidResponse();
        }
        if (copy is SendToTaxAuthorityRequest status && operation == "status" && result.Data is IEnumerable<PmsStatusInvoiceResponse> statuses &&
            statuses.Any(s => !status.InvoiceIds!.Contains(s.InvoiceId))) throw InvalidResponse();
        return MapResult(result.Data, operation);
    }
    private static object? MapResult(object? value, string operation) => value switch
    {
        PmsInvoiceResponse invoice => new InvoiceResult(invoice.InvoiceId, invoice.ProviderInvoiceNumber, invoice.TransactionId, invoice.SearchCode, invoice.MaCQT, invoice.InvoiceData, true, null,
            RequiresSigning: operation is "draft" or "draft/edit" ? true : null),
        IEnumerable<PmsInvoiceResponse> invoices => invoices.Select(i => MapResult(i, operation)).ToArray(),
        PmsSearchInvoiceResponse invoice => new InvoiceResult(invoice.InvoiceId, invoice.ProviderInvoiceNumber, null, invoice.InvoiceId, invoice.CodeOfTax, null, true, null),
        PmsDownloadPdfResponse pdf => Document(pdf),
        IEnumerable<PmsStatusInvoiceResponse> statuses => statuses.Select(s => new InvoiceStatusResult(s.InvoiceId, s.StatusInvoice)).ToArray(),
        null => new { success = true },
        _ => value,
    };
    private static InvoiceDocumentResult Document(PmsDownloadPdfResponse pdf)
    {
        if (string.IsNullOrWhiteSpace(pdf.InvoicePDF)) throw new ProviderCallException(ProviderFailure.InvalidResponse, "VNPT_INVALID_RESPONSE", "VNPT document is empty.");
        if (pdf.Type == 1) { try { if (Convert.FromBase64String(pdf.InvoicePDF).Length == 0) Fail("Empty PDF."); } catch (FormatException) { throw new ProviderCallException(ProviderFailure.InvalidResponse, "VNPT_INVALID_RESPONSE", "VNPT document is invalid."); } }
        return new InvoiceDocumentResult(pdf.InvoicePDF, pdf.Type == 1 ? "base64" : "url");
    }
    private static void Require(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) Fail(field + " is required."); }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
    private static ProviderCallException InvalidResponse() => new(ProviderFailure.InvalidResponse, "VNPT_INVALID_RESPONSE", "VNPT response does not match the requested invoices.");
}
