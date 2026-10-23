using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Sun.Mapping;
internal sealed class SunMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required dynamic Data { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
    public DateTime Now { get; init; }
}
