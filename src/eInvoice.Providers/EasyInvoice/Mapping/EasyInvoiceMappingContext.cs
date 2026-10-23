using eInvoice.Core;
using eInvoice.Core.Contracts;
namespace eInvoice.Providers.EasyInvoice.Mapping;
internal sealed class EasyInvoiceMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public InvoiceAction Action { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
}
// Values are imported from the baseline enum, see provenance.
internal enum EasyAdjustType { Info = 4, Increase = 2, Decrease = 3 }
internal enum EasyType { Normal = 0, AdjustInfo = 4, AdjustIncrease = 2, AdjustDecrease = 3, Replace = 1 }
internal enum PreviewType { CREATE = 1, ADJUST = 2, REPLACE = 3, ADJUST_INCREASE = 4, ADJUST_DECREASE = 5, ADJUST_INFO = 6 }
internal sealed class Metadata
{
    public string? keyTag { get; set; }
    public string? valueType { get; set; }
    public string? keyLabel { get; set; }
    public string? stringValue { get; set; }
}
