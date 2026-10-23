namespace eInvoice.Core.Contracts;

public class ExtraFieldMapping
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? Code { get; set; }
    public string? ThirdPartyCode { get; set; }
    public string? ThirdPartyDataType { get; set; }
    public string? ThirdPartyLabel { get; set; }
    public string? Serial { get; set; }
    public string? FormNo { get; set; }
}
