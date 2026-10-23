namespace eInvoice.Providers.Misa;

public sealed class MisaLoginOptions
{
    // Shared integration application ID from the pinned provider source, not a PMS authentication secret.
    public string ApplicationId { get; set; } = "9B226ED5-8653-11ED-8091-005056A6F699";
    public List<string> AllowedHosts { get; set; } = [];
}
