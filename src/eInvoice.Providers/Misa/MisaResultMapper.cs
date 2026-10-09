using eInvoice.Core.Contracts;
using eInvoice.Providers.Misa.Wire;

namespace eInvoice.Providers.Misa;

internal static class MisaResultMapper
{
    internal static IReadOnlyList<InvoiceResult> Published(string? data, IReadOnlyList<string> requestedIds, int invoiceType, bool batch, SiteConfiguration? site = null)
    {
        var results = invoiceType switch
        {
            2 => MisaBusinessTransport.ReadArray<CreateInvoiceResponseModel>(data, legacyEscapes: true)
                .Select(item => Item(item.RefID, item.InvoiceNumber, item.TransactionID, null, null, null, item.ErrorCode,
                    allowDuplicate: batch)).ToList(),
            1 => MisaBusinessTransport.ReadArray<CreateTicketResponseModel>(data, legacyEscapes: true)
                .Select(item => Item(item.RefID, item.InvNo, item.TransactionID, null, null, null, item.ErrorCode)).ToList(),
            _ => MisaBusinessTransport.ReadArray<InvoiceResponseModel>(data)
                .Select(item => Item(item.RefID, item.InvNo, item.TransactionID, item.TransactionID, item.InvCode, item.InvoiceData, item.ErrorCode,
                    description: item.DescriptionErrorCode, site: site)).ToList(),
        };
        if (results.Count == 0) throw MisaBusinessTransport.InvalidResponse();
        if (results.Any(item => item.IsSuccess && string.IsNullOrEmpty(item.ProviderInvoiceNumber) &&
                string.IsNullOrEmpty(item.TransactionId) && string.IsNullOrEmpty(item.InvoiceData)))
            throw MisaBusinessTransport.InvalidResponse();
        if (!batch && results.Count == 1 && string.IsNullOrWhiteSpace(results[0].InvoiceId))
            results[0] = results[0] with { InvoiceId = requestedIds[0] };
        if (results.Count != requestedIds.Count || results.Select(item => item.InvoiceId).Distinct(StringComparer.Ordinal).Count() != results.Count ||
            results.Any(item => !requestedIds.Contains(item.InvoiceId, StringComparer.Ordinal)))
            throw MisaBusinessTransport.InvalidResponse();
        if (!batch && !results[0].IsSuccess)
            throw new ProviderCallException(ProviderFailure.Rejected, "MISA_INVOICE_REJECTED", "MISA rejected the invoice.")
                { SupplierCode = results[0].ProviderErrorCode, SupplierMessage = results[0].ProviderMessage };
        return results;
    }

    private static InvoiceResult Item(string? id, string? number, string? transactionId, string? searchCode,
        string? taxCode, string? xml, string? errorCode, bool allowDuplicate = false, string? description = null, SiteConfiguration? site = null)
    {
        var success = string.IsNullOrEmpty(errorCode) || (allowDuplicate && errorCode == "InvalidDuplicateRefID" && !string.IsNullOrEmpty(number));
        // Only known symbolic supplier codes cross the API boundary; arbitrary text stays private.
        var safeCode = MisaErrorCodes.Safe(errorCode);
        return new(id ?? "", number, transactionId, searchCode, taxCode, xml, success, safeCode,
            success ? null : MisaErrorMessage.Sanitize(description, site));
    }

    internal static InvoiceDocumentResult Document(string? data)
    {
        var documents = MisaBusinessTransport.ReadArray<DownloadPDFResponseModel>(data);
        if (documents.Count == 0) throw MisaBusinessTransport.InvalidResponse();
        if (!string.IsNullOrEmpty(documents[0].ErrorCode))
            throw new ProviderCallException(ProviderFailure.Rejected, "MISA_DOCUMENT_REJECTED", "MISA rejected the document request.");
        if (string.IsNullOrEmpty(documents[0].Data)) throw MisaBusinessTransport.InvalidResponse();
        return new(documents[0].Data, "base64");
    }

    internal static IReadOnlyList<InvoiceStatusResult> Status(string? data) =>
        MisaBusinessTransport.ReadArray<StatusInvoiceResponse>(data).Select(item => new InvoiceStatusResult(item.RefID, item.SendTaxStatus)).ToList();

    internal static IReadOnlyList<InvoiceHashResult> Hash(string? data) =>
        MisaBusinessTransport.ReadArray<XMLInvoiceHashResponse>(data).Select(item => new InvoiceHashResult(item.MessageCode, item.MessageHash)).ToList();
}
