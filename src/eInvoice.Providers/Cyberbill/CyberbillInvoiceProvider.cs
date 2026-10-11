using System.Globalization;
using System.Text.Json;
using eInvoice.Core;
using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;
using eInvoice.Providers.Common;
using eInvoice.Providers.Cyberbill.Mapping;
using eInvoice.Providers.Cyberbill.Wire;

namespace eInvoice.Providers.Cyberbill;

// Create/sign/payment semantics are awaiting Q012/Q013. Advertise only finished operations.
public sealed class CyberbillInvoiceProvider(CyberbillTransport transport) : IInvoiceProvider
{
    public int PartnerId => 7;
    public string Name => "CYBER_BILL";
    public IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check", [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.DownloadPdf] = "document", [InvoiceAction.Void] = "void",
    };
    public bool Supports(string operation) => Routes.Values.Contains(operation, StringComparer.Ordinal);
    public bool IsWrite(string operation) => operation == "void";
    public IReadOnlyList<string> Claims(string operation, ISiteScopedRequest request) =>
        request is VoidInvoiceRequest invoice ? ["void:cyberbill:" + invoice.InvoiceId] : [];
    public async Task CheckConnectionAsync(ResolvedSite site, CancellationToken cancellationToken) =>
        _ = await transport.LoginAsync(site.Site, cancellationToken);

    public async Task<object?> ExecuteAsync(string operation, ResolvedSite site, ISiteScopedRequest request, CancellationToken cancellationToken)
    {
        if (!Supports(operation)) throw new UnsupportedProviderOperationException("PROVIDER_OPERATION_NOT_SUPPORTED", "Cyberbill operation is not implemented yet.");
        var copy = (ISiteScopedRequest)JsonSerializer.Deserialize(JsonSerializer.Serialize(request, request.GetType()), request.GetType())!;
        var context = new CyberbillMappingContext { Company = site.Site, Data = copy, ExtraData = site.ExtraFieldMappings.ToList() };
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
}
