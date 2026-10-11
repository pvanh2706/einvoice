using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Viettel.Mapping;
using eInvoice.Providers.Viettel.Wire;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Viettel;

public sealed class ViettelInvoiceProvider(ViettelTransport transport, TimeProvider clock) : IInvoiceProvider
{
    public int PartnerId => 4;
    public string Name => "VIETTEL";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check", [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.CreateAndPublish] = "publish", [InvoiceAction.CreateAndPublishBatch] = "publish-batch",
        [InvoiceAction.Draft] = "draft", [InvoiceAction.Preview] = "preview", [InvoiceAction.DownloadPdf] = "document",
        [InvoiceAction.Void] = "void", [InvoiceAction.Replace] = "replace", [InvoiceAction.SearchById] = "search",
        [InvoiceAction.AdjustIncrease] = "adjust/increase", [InvoiceAction.AdjustDecrease] = "adjust/decrease",
        [InvoiceAction.AdjustInformation] = "adjust/information",
    };

    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is "publish" or "publish-batch" or "draft" or "void" or "replace" || operation.StartsWith("adjust/", StringComparison.Ordinal);
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        InvoiceBatchRequest batch => batch.Invoices?.Where(i => i?.Invoice?.InvoiceId is not null).Select(i => "publish:" + i.Invoice!.InvoiceId).ToArray() ?? [],
        CreateInvoiceRequest invoice => [(operation == "draft" ? "draft:" : "publish:") + invoice.Invoice?.InvoiceId],
        VoidInvoiceRequest invoice => ["void:0:" + invoice.FormNo + "|" + invoice.ProviderInvoiceNumber],
        _ => [],
    };

    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) =>
        _ = await transport.LoginAsync(site.Site, cancellationToken);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Viettel does not support this operation.");
        var action = Routes.First(pair => pair.Value == operation).Key;
        var copy = Clone(request);
        var context = new ViettelMappingContext { Company = site.Site, Data = copy, ExtraData = site.ExtraFieldMappings.ToList(), Action = (int)action, Now = clock.GetUtcNow() };
        if (copy is CreateInvoiceRequest invoice)
        {
            Validate(invoice);
            if (operation == "replace" || operation.StartsWith("adjust/", StringComparison.Ordinal))
                Require(((AdjustOrReplaceInvoiceRequest)invoice).OriginalProviderInvoiceNumber, "originalProviderInvoiceNumber");
            var mapped = Map(() => operation is "replace" || operation.StartsWith("adjust/", StringComparison.Ordinal)
                ? ViettelRequestMapper.ConvertAdjustReplaceRequest(context)
                : operation == "preview" ? ViettelRequestMapper.ConvertPreviewInvoiceRequest(context)
                : operation == "draft" ? ViettelRequestMapper.ConvertCreateDraftInvoiceRequest(context)
                : ViettelRequestMapper.ConvertCreateAndPublishInvoiceRequest(context));
            var path = operation == "draft" ? EndpointApi.DRAFT : operation == "preview" ? EndpointApi.PREVIEW : EndpointApi.CREATE;
            var json = await transport.ExecuteAsync(site.Site, path + Uri.EscapeDataString(site.Site.TaxNumber!), mapped.Body, false, IsWrite(operation), cancellationToken);
            if (operation == "preview") return Document(json);
            if (operation == "draft") return new[] { new InvoiceResult(invoice.Invoice!.InvoiceId!, null, null, null, null, null, true, null) };
            return new[] { Result(json["result"] as JObject, invoice.Invoice!.InvoiceId!) };
        }
        if (copy is InvoiceBatchRequest batch)
        {
            if (batch.Invoices is not { Count: > 0 } || batch.Invoices.Any(i => i is null)) Fail("Batch needs non-null invoices.");
            foreach (var item in batch.Invoices!)
            {
                Validate(item);
                if (item.SiteConfigInfo is not null) Fail("Use the batch wrapper site configuration, not per-invoice configuration.");
            }
            var ids = batch.Invoices!.Select(i => i.Invoice!.InvoiceId!).ToArray();
            if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length) Fail("Batch invoice IDs must be distinct.");
            context = new ViettelMappingContext { Company = site.Site, Data = batch.Invoices, ExtraData = site.ExtraFieldMappings.ToList(), Action = (int)action, Now = clock.GetUtcNow() };
            var mapped = Map(() => ViettelRequestMapper.ConvertCreateAndPublishMultiInvoiceRequest(context));
            var json = await transport.ExecuteAsync(site.Site, EndpointApi.CREATE_MULTI + Uri.EscapeDataString(site.Site.TaxNumber!), mapped.Body, false, true, cancellationToken, batch: true);
            var outputs = (JArray)json["createInvoiceOutputs"]!;
            if (outputs.Count != ids.Length || outputs.Any(o => o is not JObject)) throw ViettelTransport.Invalid();
            var results = new List<InvoiceResult>();
            foreach (var output in outputs.Cast<JObject>())
            {
                var id = ViettelTransport.Scalar(output, "transactionUuid");
                if (id is null || !ids.Contains(id, StringComparer.Ordinal) || results.Any(r => r.InvoiceId == id)) throw ViettelTransport.Invalid();
                if (ViettelTransport.HasError(output))
                {
                    var error = ViettelTransport.Rejected(output, site.Site, null);
                    results.Add(new(id, null, null, null, null, null, false, error.SupplierCode, error.SupplierMessage));
                }
                else results.Add(Result(output["result"] as JObject, id));
            }
            return results;
        }
        if (copy is DownloadPdfRequest download)
        {
            Normal(download.InvoiceType); Require(download.ProviderInvoiceNumber, "providerInvoiceNumber");
            Require(download.FormNo, "formNo"); Require(download.InvoiceId, "invoiceId");
            if (download.TypeFileDownload is not (null or "pdf")) Fail("Viettel document currently supports pdf only.");
            var mapped = Map(() => ViettelRequestMapper.ConvertDownloadPdfRequest(context));
            return Document(await transport.ExecuteAsync(site.Site, EndpointApi.DOWNLOAD_PDF, mapped.Body, false, false, cancellationToken));
        }
        if (copy is VoidInvoiceRequest cancellation)
        {
            Normal(cancellation.InvoiceType); Require(cancellation.ProviderInvoiceNumber, "providerInvoiceNumber"); Require(cancellation.FormNo, "formNo");
            Require(cancellation.Reason, "reason"); Date(cancellation.InvoiceDate, "invoiceDate");
            if (cancellation.AdditionalReferenceDate is not null) Date(cancellation.AdditionalReferenceDate, "additionalReferenceDate");
            var mapped = Map(() => ViettelRequestMapper.ConvertVoidInvoiceRequest(context));
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.VOID, mapped.Body, true, true, cancellationToken);
            return new { success = true };
        }
        if (copy is SearchInvoiceRequest search)
        {
            Require(search.InvoiceId, "invoiceId");
            var mapped = Map(() => ViettelRequestMapper.ConvertSearchByIdRequest(context));
            var json = await transport.ExecuteAsync(site.Site, EndpointApi.SEARCH_BY_ID, mapped.Body, true, false, cancellationToken);
            if (json["result"] is not JArray { Count: 1 } result || result[0] is not JObject item) throw ViettelTransport.Invalid();
            return new[] { Result(item, search.InvoiceId!) };
        }
        throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Viettel does not support this request.");
    }

    private static InvoiceResult Result(JObject? result, string id)
    {
        if (result is null || string.IsNullOrWhiteSpace(ViettelTransport.Scalar(result, "invoiceNo")) ||
            result.Property("transactionUuid") is not null && ViettelTransport.Scalar(result, "transactionUuid") != id)
            throw ViettelTransport.Invalid();
        return new(id, ViettelTransport.Scalar(result, "invoiceNo"), null, ViettelTransport.Scalar(result, "reservationCode"),
            ViettelTransport.Scalar(result, "codeOfTax"), null, true, null);
    }
    private static InvoiceDocumentResult Document(JObject json)
    {
        var file = ViettelTransport.Scalar(json, "fileToBytes");
        if (string.IsNullOrWhiteSpace(file)) throw ViettelTransport.Invalid();
        try { _ = Convert.FromBase64String(file); } catch (FormatException) { throw ViettelTransport.Invalid(); }
        return new(file, "base64");
    }
    private static void Validate(CreateInvoiceRequest request)
    {
        if (request.Invoice is null) Fail("invoice is required.");
        var invoice = request.Invoice!;
        Normal(invoice.InvoiceType); Require(invoice.InvoiceId, "invoice.invoiceId"); Require(invoice.FormNo, "invoice.formNo");
        Require(invoice.Serial, "invoice.serial"); Require(invoice.CurrencyCode, "invoice.currencyCode"); Date(invoice.InvoiceDate, "invoice.invoiceDate");
        if (request.Details is not { Count: > 0 } || request.Details.Any(i => i is null || i.Quantity is null || i.Price is null)) Fail("details need quantity and price.");
        if (invoice.BeforeTaxAmount is null || invoice.TaxAmount is null || invoice.AfterTaxAmount is null) Fail("Invoice totals are required.");
        if (invoice.PricePrecision is < 0 or > 28) Fail("pricePrecision must be between 0 and 28.");
        if (request.TaxSummaries?.Any(i => i is null) == true || request.ExtraData?.Any(i => i is null) == true) Fail("Lists must not contain null items.");
    }
    private static void Normal(int? type) { if (type != 0) Fail("Viettel currently supports invoiceType 0 only."); }
    private static void Date(string? date, string field)
    { if (!DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail(field + " must be dd/MM/yyyy."); }
    private static void Require(string? text, string field) { if (string.IsNullOrWhiteSpace(text)) Fail(field + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
    private static ISiteScopedRequest Clone(ISiteScopedRequest request) => (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
    private static T Map<T>(Func<T> map)
    {
        var prior = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; return map(); }
        catch (Exception error) when (error is FormatException or ArgumentException or NullReferenceException or OverflowException)
        { throw new InvoiceRequestException("INVALID_INVOICE_MAPPING_INPUT", "Invoice fields do not satisfy Viettel mapping requirements."); }
        finally { CultureInfo.CurrentCulture = prior; }
    }
}
