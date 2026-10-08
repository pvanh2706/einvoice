namespace eInvoice.Core.Contracts;

public class MinvoiceLoginRequest
{
    public string? UnitCode { get; set; } = "VP";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public SystemInformation? System { get; set; } = null;
    public string? RequestId { get; set; }
}
