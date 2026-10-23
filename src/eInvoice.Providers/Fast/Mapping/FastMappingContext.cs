using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Fast.Mapping;
internal sealed class FastMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public int Action { get; init; }
    public bool IsSerialMTT { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
}
