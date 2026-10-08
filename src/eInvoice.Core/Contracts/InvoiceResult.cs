namespace eInvoice.Core.Contracts;

public sealed record InvoiceResult(string InvoiceId, string? ProviderInvoiceNumber, string? TransactionId,
    string? SearchCode, string? TaxAuthorityCode, string? InvoiceData, bool IsSuccess, string? ProviderErrorCode);

public sealed record InvoiceStatusResult(string InvoiceId, int TaxAuthorityStatus);
public sealed record InvoiceHashResult(string? MessageCode, string? MessageHash);
public sealed record InvoiceDocumentResult(string Content, string Format);
