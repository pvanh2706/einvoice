namespace eInvoice.Core.Contracts;

public class SiteConfiguration
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }

    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxNumber { get; set; }
    public int? Partner { get; set; }
    public string? PartnerUrl { get; set; }
    public string? PartnerUrl2 { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Username2 { get; set; }
    public string? Password2 { get; set; }
    public string? MinvoiceToken { get; set; }
    public DateTime? MinvoiceExpired { get; set; }
}
