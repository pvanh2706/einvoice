using eInvoice.Core.Contracts;

namespace eInvoice.Capture.Viettel.Mapping;

internal sealed class ViettelMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required dynamic Data { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
    public int Action { get; init; }
    public DateTimeOffset Now { get; init; }
}
