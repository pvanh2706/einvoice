namespace eInvoice.Core.Contracts;

public sealed record InvoiceListItem(string ProviderInvoiceCode, int? SignStatus, DateTimeOffset? SignedAt);
public sealed record TaxCodeInfoResult(string? CompanyName, string? Address, string? TaxCode, string? RegistrationId,
    string? TaxAuthorityProvince, string? TaxAuthorityOffice, string? LegalRepresentative, string? EstablishedDate);
