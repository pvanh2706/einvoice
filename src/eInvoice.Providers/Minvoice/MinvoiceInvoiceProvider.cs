using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Minvoice.Mapping;
using eInvoice.Providers.Minvoice.Wire;
using Newtonsoft.Json.Linq;
using static eInvoice.Providers.Minvoice.Wire.MinvoiceModels;

namespace eInvoice.Providers.Minvoice;

public sealed class MinvoiceInvoiceProvider(MinvoiceTransport transport, TimeProvider clock) : IInvoiceProvider
{
    public int PartnerId => 2;
    public string Name => "MINVOICE";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check", [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.GetFormSerials] = "form-serials", [InvoiceAction.Create] = "create",
        [InvoiceAction.Change] = "change", [InvoiceAction.Delete] = "delete",
        [InvoiceAction.CreateAndPublish] = "publish", [InvoiceAction.Publish] = "publish-existing",
        [InvoiceAction.Replace] = "replace", [InvoiceAction.Adjust] = "adjust", [InvoiceAction.Void] = "void",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.GetTaxCodeInformation] = "tax-code",
        [InvoiceAction.SendEmail] = "email", [InvoiceAction.GetInvoices] = "invoices",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation is "create" or "change" or "delete" or "publish" or "publish-existing" or "replace" or "adjust" or "void" or "email";
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) => request switch
    {
        AdjustOrReplaceInvoiceRequest data when operation == "change" => ["change:minvoice-code:" + ClaimCode(data.OriginalProviderInvoiceCode)],
        CreateInvoiceRequest data when operation is "publish" or "replace" => ["publish:" + data.Invoice?.InvoiceId, "publish:minvoice-code:" + ClaimCode(data.Invoice?.InvoiceId)],
        CreateInvoiceRequest data => [(operation == "create" ? "draft:" : "publish:") + data.Invoice?.InvoiceId],
        PublishExistingInvoiceRequest data => ["publish:" + data.InvoiceId, "publish:minvoice-code:" + ClaimCode(data.ProviderInvoiceCode)],
        DeleteInvoiceRequest data => ["delete:minvoice-number:" + data.Serial + ":" + (long.TryParse(data.ProviderInvoiceNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number.ToString(CultureInfo.InvariantCulture) : data.ProviderInvoiceNumber)],
        VoidInvoiceRequest data => ["void:minvoice-code:" + ClaimCode(data.ProviderInvoiceCode)],
        _ => [],
    };
    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) => _ = await transport.LoginAsync(site.Site, true, cancellationToken);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Minvoice does not support this operation.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var mapper = new MinvoiceRequestMapper { Now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime };
        if (copy is GetFormSerialsRequest forms)
        {
            Normal(forms.InvoiceType);
            var json = await transport.ExecuteAsync(site.Site, EndpointApi.FORM_SERIAL, null, false, cancellationToken, get: true);
            if (json["data"] is not JArray items) throw MinvoiceTransport.Invalid();
            return items.Select(item => item is JObject row ? new FormSerialResponse
            { FormNo = RequiredScalar(row, "invoiceForm"), Serial = RequiredScalar(row, "khhdon"), FromNo = 1 } : throw MinvoiceTransport.Invalid()).ToArray();
        }
        if (copy is DownloadPdfRequest document)
        {
            Normal(document.InvoiceType); ProviderId(document.ProviderInvoiceCode, "providerInvoiceCode");
            if (document.TypeFileDownload is not (null or "pdf")) Fail("Minvoice document supports pdf only.");
            return new InvoiceDocumentResult(await transport.DocumentAsync(site.Site, document.ProviderInvoiceCode!, cancellationToken), "base64");
        }
        if (copy is PublishExistingInvoiceRequest sign)
        {
            Normal(sign.InvoiceType); Require(sign.InvoiceId, "invoiceId"); ProviderId(sign.ProviderInvoiceCode, "providerInvoiceCode"); PositiveNumber(sign.ProviderInvoiceNumber, "providerInvoiceNumber");
            var reference = new VoidInvoiceRequest { InvoiceId = sign.ProviderInvoiceCode };
            var payload = Map(() => mapper.convertSignInv(JObject.FromObject(reference)));
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.SIGN, payload, true, cancellationToken);
            // Baseline Sign confirms code=00 but does not return a number; the existing number is carried explicitly.
            return new[] { new InvoiceResult(sign.InvoiceId!, sign.ProviderInvoiceNumber, null, null, null, null, true, null, ProviderInvoiceCode: sign.ProviderInvoiceCode) };
        }
        if (copy is DeleteInvoiceRequest delete)
        {
            Normal(delete.InvoiceType); Require(delete.InvoiceId, "invoiceId"); Require(delete.Serial, "serial"); PositiveNumber(delete.ProviderInvoiceNumber, "providerInvoiceNumber", int.MaxValue);
            var payload = Map(() => mapper.convertDataDelete(JObject.FromObject(delete)));
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.CREATE, payload, true, cancellationToken);
            return new { success = true };
        }
        if (copy is VoidInvoiceRequest cancel)
        {
            Normal(cancel.InvoiceType); Require(cancel.InvoiceId, "invoiceId"); ProviderId(cancel.ProviderInvoiceCode, "providerInvoiceCode"); PositiveNumber(cancel.ProviderInvoiceNumber, "providerInvoiceNumber");
            cancel.InvoiceId = cancel.ProviderInvoiceCode;
            var payload = Map(() => mapper.convertCancelInv(JObject.FromObject(cancel)));
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.VOID, payload, true, cancellationToken);
            return new { success = true };
        }
        if (copy is GetInvoicesRequest query)
        {
            Require(query.Serial, "serial");
            if (query.FromDate == DateTime.MinValue || query.ToDate == DateTime.MinValue) Fail("fromDate/toDate are required.");
            query.FromDate = Vietnam(query.FromDate).Date; query.ToDate = Vietnam(query.ToDate).Date;
            if (query.FromDate == DateTime.MinValue || query.ToDate == DateTime.MinValue || query.FromDate > query.ToDate) Fail("fromDate/toDate must be present and form a valid range.");
            var payload = Map(() => mapper.convertGetInv(JObject.FromObject(query)));
            var json = await transport.ExecuteAsync(site.Site, EndpointApi.INVOICES, payload, false, cancellationToken);
            if (json["data"] is not JArray rows) throw MinvoiceTransport.Invalid();
            return rows.Select(row => row is JObject item ? ListItem(item) : throw MinvoiceTransport.Invalid()).ToArray();
        }
        if (copy is GetTaxCodeInfoRequest tax)
        {
            Require(tax.TaxCode, "taxCode");
            var json = await transport.TaxCodeAsync(site.Site, tax.TaxCode!, cancellationToken);
            if (string.IsNullOrWhiteSpace(MinvoiceTransport.Scalar(json["ma_so_thue"]))) throw MinvoiceTransport.Invalid();
            return new TaxCodeInfoResult(MinvoiceTransport.Scalar(json["ten_cty"]), MinvoiceTransport.Scalar(json["dia_chi"]), MinvoiceTransport.Scalar(json["ma_so_thue"]),
                MinvoiceTransport.Scalar(json["masothue_id"]), MinvoiceTransport.Scalar(json["cqthuecap_tinh"]), MinvoiceTransport.Scalar(json["cqthue_ql"]),
                MinvoiceTransport.Scalar(json["nguoi_dai_dien"]), MinvoiceTransport.Scalar(json["ngay_thanh_lap"]));
        }
        if (copy is SendEmailRequest email)
        {
            if (email.ProviderInvoiceCode is null || email.ProviderInvoiceCode == Guid.Empty) Fail("providerInvoiceCode must be a non-empty Minvoice provider GUID for email.");
            Require(email.Email, "email");
            var payload = new SendEmailCvt { id = [new EmailId { id = email.ProviderInvoiceCode, nguoi_nhan = email.Email }], type = "Gửi hóa đơn" };
            _ = await transport.ExecuteAsync(site.Site, EndpointApi.EMAIL, payload, true, cancellationToken);
            return new { success = true };
        }
        if (copy is CreateInvoiceRequest data)
        {
            if (data.Invoice is null) Fail("invoice is required.");
            Normal(data.Invoice!.InvoiceType); ProviderId(data.Invoice.InvoiceId, "invoice.invoiceId");
            Require(data.Invoice.Serial, "invoice.serial");
            if (!DateTime.TryParseExact(data.Invoice.InvoiceDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Fail("invoice.invoiceDate must be dd/MM/yyyy.");
            if (data.Details is not { Count: > 0 }) Fail("details must contain at least one item.");
            if (data.Invoice.InvoiceNo == 0) Fail("invoice.invoiceNo must be omitted/null or greater than zero.");
            var correlation = data.Invoice.InvoiceId!;
            if (operation is "change" or "replace" or "adjust")
            {
                if (data is not AdjustOrReplaceInvoiceRequest original || string.IsNullOrWhiteSpace(original.OriginalProviderInvoiceCode)) Fail("originalProviderInvoiceCode is required.");
                var originalData = (AdjustOrReplaceInvoiceRequest)data;
                ProviderId(originalData.OriginalProviderInvoiceCode, "originalProviderInvoiceCode");
                if (operation is "change" or "replace") PositiveNumber(originalData.OriginalProviderInvoiceNumber, "originalProviderInvoiceNumber", operation == "change" ? int.MaxValue : long.MaxValue);
                originalData.OriginalInvoiceId = originalData.OriginalProviderInvoiceCode;
            }
            var model = JObject.FromObject(data);
            model["editmode"] = operation == "change" ? "2" : "1";
            var payload = Map(() => operation switch
            {
                "adjust" => mapper.convertAdjustInv(model, 0),
                "replace" => mapper.convertReplaceInv(model),
                _ => mapper.convertInvoiceFormat(model, site.Site.Id!.Value.ToString()),
            });
            var endpoint = operation switch { "publish" => EndpointApi.PUBLISH, "replace" => EndpointApi.REPLACE, "adjust" => EndpointApi.ADJUST, _ => EndpointApi.CREATE };
            var json = await transport.ExecuteAsync(site.Site, endpoint, payload, true, cancellationToken);
            return Result(json, correlation, operation, MinvoiceTransport.Scalar(payload["data"]?.FirstOrDefault()?["hoadon68_id"]));
        }
        throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Minvoice does not support this request.");
    }

    private static InvoiceResult[] Result(JObject json, string invoiceId, string operation, string? sentProviderId)
    {
        if (json["data"] is not JObject row) throw MinvoiceTransport.Invalid();
        var providerId = RequiredScalar(row, "hoadon68_id"); if (!Guid.TryParse(providerId, out var id) || id == Guid.Empty) throw MinvoiceTransport.Invalid();
        if (sentProviderId is not null && Guid.Parse(sentProviderId) != id) throw MinvoiceTransport.Invalid();
        var number = MinvoiceTransport.Scalar(row["shdon"]);
        if (operation is "publish" or "adjust" or "replace" && string.IsNullOrWhiteSpace(number)) throw MinvoiceTransport.Invalid();
        // Baseline Save/SaveSign reads codes in data; Adjust/Replace reads codes at the top level.
        var codes = operation is "adjust" or "replace" ? json : row;
        return [new InvoiceResult(invoiceId, number, null, MinvoiceTransport.Scalar(codes["sobaomat"]), MinvoiceTransport.Scalar(codes["macqt"]), null, true, null, ProviderInvoiceCode: providerId)];
    }
    private static InvoiceListItem ListItem(JObject row)
    {
        var code = RequiredScalar(row, "hoadon68_id"); if (!Guid.TryParse(code, out var id) || id == Guid.Empty) throw MinvoiceTransport.Invalid();
        try
        {
            var status = row["is_success"]?.Type is JTokenType.Null or null ? null : row["is_success"]!.ToObject<int?>();
            var date = row["nky"]?.Type is JTokenType.Null or null ? null : row["nky"]!.ToObject<DateTime?>();
            return new InvoiceListItem(code, status, date is { } signed ? Vietnam(signed) : null);
        }
        catch (Exception error) when (error is FormatException or ArgumentException or Newtonsoft.Json.JsonException or OverflowException) { throw MinvoiceTransport.Invalid(); }
    }
    private static DateTimeOffset Vietnam(DateTime date) => date.Kind == DateTimeKind.Unspecified
        ? new DateTimeOffset(date, TimeSpan.FromHours(7)) : new DateTimeOffset(date).ToOffset(TimeSpan.FromHours(7));
    private static JToken Map(Func<string> map) => ProviderMapping.Run(() => JToken.Parse(map()), "Minvoice");
    private static string RequiredScalar(JObject row, string key) => MinvoiceTransport.Scalar(row[key]) is { Length: > 0 } value ? value : throw MinvoiceTransport.Invalid();
    private static string? ClaimCode(string? value) => Guid.TryParse(value, out var id) ? id.ToString("D") : value;
    private static void PositiveNumber(string? value, string field, long maximum = long.MaxValue)
    { if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0 || number > maximum) Fail(field + " must be a positive invoice number."); }
    private static void ProviderId(string? value, string field) { if (!Guid.TryParse(value, out var id) || id == Guid.Empty) Fail(field + " must be a non-empty GUID for Minvoice."); }
    private static void Normal(int? type) { if (type != 0) Fail("Minvoice currently supports invoiceType 0 only."); }
    private static void Require(string? value, string field) { if (string.IsNullOrWhiteSpace(value)) Fail(field + " is required."); }
    private static void Fail(string message) => throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", message);
}
