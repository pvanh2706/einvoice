using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Fast.Mapping;
using eInvoice.Providers.Fast.Wire;
using Newtonsoft.Json.Linq;

namespace eInvoice.Providers.Fast;

public sealed class FastInvoiceProvider(FastTransport transport) : IInvoiceProvider
{
    public int PartnerId => 8;
    public string Name => "FAST";
    public bool VerifiesConnection => false;
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check", [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.CreateAndPublish] = "publish", [InvoiceAction.CreateAndPublishBatch] = "publish-batch",
        [InvoiceAction.Draft] = "draft", [InvoiceAction.Preview] = "preview", [InvoiceAction.Void] = "void",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.DownloadDraftPdf] = "document/draft",
        [InvoiceAction.AdjustIncrease] = "adjust/increase", [InvoiceAction.AdjustDecrease] = "adjust/decrease",
        [InvoiceAction.AdjustInformation] = "adjust/information", [InvoiceAction.Replace] = "replace",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is not ("preview" or "document" or "document/draft");
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        InvoiceBatchRequest batch => batch.Invoices?.SelectMany(i => Claims(operation, i)).ToArray() ?? [],
        CreateInvoiceRequest invoice when operation != "preview" => [(operation == "draft" ? "draft:" : "publish:") + invoice.Invoice?.InvoiceId],
        VoidInvoiceRequest invoice => ["void:fast:" + invoice.InvoiceId, "void:fast-search:" + invoice.SearchCode],
        _ => [],
    };
    public Task CheckConnectionAsync(ResolvedSite site, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var configured = false;
        if (!string.IsNullOrWhiteSpace(site.Site.PartnerUrl)) { transport.Validate(site.Site, true); configured = true; }
        if (!string.IsNullOrWhiteSpace(site.Site.PartnerUrl2)) { transport.Validate(site.Site, false); configured = true; }
        if (!configured) throw new ProviderCallException(ProviderFailure.Configuration, "FAST_ENDPOINT_NOT_ALLOWED", "FAST configuration is incomplete.");
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken ct)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "FAST does not support this operation.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var serial = copy switch { CreateInvoiceRequest x => x.Invoice?.Serial, InvoiceBatchRequest x => x.Invoices?.FirstOrDefault()?.Invoice?.Serial, DownloadPdfRequest x => x.Serial, VoidInvoiceRequest x => x.Serial, _ => null };
        Require(serial, "serial");
        var mtt = serial!.Length >= 3 && serial[^3] == 'M';
        if (operation == "draft" && !mtt) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "FAST draft is supported only for cash-register invoices.");
        var items = copy is InvoiceBatchRequest batch ? batch.Invoices : copy is CreateInvoiceRequest create ? [create] : null;
        if (items is not null)
        {
            if (items.Count == 0) Fail("invoices cannot be empty.");
            foreach (var item in items)
            {
                if (item.Invoice is null || item.Details is not { Count: > 0 }) Fail("invoice and details are required.");
                Require(item.Invoice!.InvoiceId, "invoice.invoiceId");
                if (item.Invoice.Serial != serial) Fail("Batch serials must match.");
                if (item.Invoice.AfterTaxAmount is null || item.Invoice.ExchangeRate is null) Fail("afterTaxAmount and exchangeRate are required.");
            }
            if (items.Select(i => i.Invoice!.InvoiceId).Distinct().Count() != items.Count) Fail("Batch invoiceIds must be unique.");
        }
        else if (copy is DownloadPdfRequest doc) { Require(doc.InvoiceId, "invoiceId"); if (operation == "document") Require(doc.SearchCode, "searchCode"); }
        else if (copy is VoidInvoiceRequest cancelled) { Require(cancelled.InvoiceId, "invoiceId"); Require(cancelled.SearchCode, "searchCode"); }
        if (copy is AdjustOrReplaceInvoiceRequest original && operation != "preview")
        { Require(original.OriginalInvoiceId, "originalInvoiceId"); if (original.OriginalInvoice is null) Fail("originalInvoice is required."); }
        transport.Validate(site.Site, mtt);
        var context = new FastMappingContext { Company = site.Site, Data = copy is InvoiceBatchRequest b ? b.Invoices! : copy,
            Action = (int)Routes.Single(x => x.Value == operation).Key, IsSerialMTT = mtt, ExtraData = site.ExtraFieldMappings.ToList() };
        var pdf = operation is "preview" or "document" or "document/draft";
        string result;
        string? sessionToken = null;
        if (mtt || operation == "void")
        {
            var payload = ProviderMapping.Run(() => operation switch
            {
                "publish" => FastRequestMapper.ConvertCreateAndPublishInvoiceRequest(context),
                "publish-batch" => FastRequestMapper.ConvertCreateAndPublishMultiInvoiceRequest(context),
                "draft" => FastRequestMapper.ConvertCreateDraftInvoiceRequest(context),
                "preview" => FastRequestMapper.ConvertPreviewInvoiceRequest(context),
                "document" or "document/draft" => FastRequestMapper.ConvertBase64PDFInvoiceRequest(context),
                "void" => FastRequestMapper.ConvertVoidInvoiceRequest(context),
                "replace" => FastRequestMapper.ConvertReplaceInvoiceRequest(context),
                _ => FastRequestMapper.ConvertAdjustInvoiceRequest(context),
            }, "FAST");
            result = await transport.SendAsync(payload, IsWrite(operation), ct);
        }
        else
        {
            var check = await transport.SendAsync(ProviderMapping.Run(() => FastRequestMapper.ConvertCheckKeyRequest(context), "FAST"), false, ct);
            var checkKey = FastTransport.Element(check, "CheckKeyResult");
            if (!checkKey.StartsWith("Ok:", StringComparison.OrdinalIgnoreCase) || checkKey.Length <= 3) throw FastTransport.Invalid();
            var tokenResponse = await transport.SendAsync(ProviderMapping.Run(() => FastRequestMapper.ConvertGetTokenRequest(context, checkKey[3..].Trim()), "FAST"), false, ct);
            var token = FastTransport.Element(tokenResponse, "GetKeyResult").Split(',')[0].Trim();
            if (token.Length == 0) throw FastTransport.Invalid();
            sessionToken = token;
            var payload = ProviderMapping.Run(() => operation switch
            {
                "publish" => FastRequestMapper.ConvertCreateAndPublishInvoiceBySoapRequest(context, token),
                "publish-batch" => FastRequestMapper.ConvertCreateAndPublishMultiInvoiceSoapRequest(context, token),
                "preview" => FastRequestMapper.ConvertPreviewInvoiceSoapRequest(context, token),
                "document" or "document/draft" => FastRequestMapper.ConvertBase64PDFInvoiceSoapRequest(context, token),
                "replace" => FastRequestMapper.ConvertReplaceInvoiceSoapRequest(context, token),
                _ => FastRequestMapper.ConvertAdjustInvoiceSoapRequest(context, token),
            }, "FAST");
            result = FastTransport.Element(await transport.SendAsync(payload, IsWrite(operation), ct), "ExcuteCommandResult");
        }
        if (pdf)
        {
            if (result.TrimStart().StartsWith('{')) { _ = FastTransport.Json(result, site.Site, sessionToken); throw FastTransport.Invalid(); }
            try { if (Convert.FromBase64String(result).Length == 0) throw FastTransport.Invalid(); } catch (FormatException) { throw FastTransport.Invalid(); }
            return new InvoiceDocumentResult(result, "base64");
        }
        var json = FastTransport.Json(result, site.Site, sessionToken);
        if (operation == "void") return new { success = true };
        JArray returned;
        try { returned = JArray.Parse(json.Value<string>("Message") ?? ""); } catch (Newtonsoft.Json.JsonException) { throw FastTransport.Invalid(); }
        if (returned.Count != items!.Count) throw FastTransport.Invalid();
        var mapped = new List<InvoiceResult>();
        foreach (var item in returned.OfType<JObject>())
        {
            var key = item.Value<string>("key");
            var input = items.SingleOrDefault(i => i.Invoice!.InvoiceId!.Replace("-", "") == key || i.Invoice.InvoiceId == key);
            if (input is null || mapped.Any(i => i.InvoiceId == input.Invoice!.InvoiceId)) throw FastTransport.Invalid();
            mapped.Add(new InvoiceResult(input.Invoice!.InvoiceId!, item.Value<string>("invoiceNo"), key, item.Value<string>("keySearch"),
                mtt ? item.Value<string>("verificationCode") : "", null, true, null));
        }
        if (mapped.Count != items.Count) throw FastTransport.Invalid();
        return copy is InvoiceBatchRequest ? mapped : mapped[0];
    }
    private static void Require(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) Fail(field + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
}
