using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Cyberbill.Mapping;

internal sealed class CyberbillMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required dynamic Data { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
    public int Action { get; init; }
}
