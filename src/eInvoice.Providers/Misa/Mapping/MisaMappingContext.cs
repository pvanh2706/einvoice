using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa.Mapping;

internal sealed class MisaMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public IReadOnlyList<ExtraFieldMapping> ExtraData { get; init; } = [];
    public int Action { get; init; }
    public DateTimeOffset Now { get; init; }
}

internal sealed class MisaMetadata
{
    public string? keyTag { get; set; }
    public string? valueType { get; set; }
    public string? keyLabel { get; set; }
    public string? stringValue { get; set; }
}
