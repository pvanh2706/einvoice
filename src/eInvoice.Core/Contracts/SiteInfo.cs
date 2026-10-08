namespace eInvoice.Core.Contracts;

public class SiteInfo
{
    public SiteConfiguration? Site { get; set; } = null;
    public List<ExtraFieldMapping>? ExtraDataMap { get; set; } = new List<ExtraFieldMapping>();
}
