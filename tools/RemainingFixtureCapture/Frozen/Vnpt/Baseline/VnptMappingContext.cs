using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Vnpt.Baseline;
internal sealed class VnptMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public int Action { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
    public bool IsSerialMTT { get; init; }
    public bool HaveDeleteInvoiceDraft { get; init; }
}
// The old mapper reads seller data through logging. Scoped state replaces that dependency.
internal sealed class VnptMappingScope : IDisposable
{
    private static readonly AsyncLocal<VnptMappingScope?> Current = new();
    private readonly VnptMappingScope? previous;
    private readonly SiteConfiguration company;
    private readonly bool write;
    private readonly DateTime now;
    internal VnptMappingScope(SiteConfiguration site, bool isWrite, TimeProvider clock)
    {
        previous = Current.Value; company = site; write = isWrite;
        now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
        Current.Value = this;
    }
    internal static SiteConfiguration Company => Current.Value?.company ?? throw new InvalidOperationException("VNPT mapping scope is missing.");
    internal static DateTime Now => Current.Value?.now ?? throw new InvalidOperationException("VNPT mapping scope is missing.");
    internal static bool IsWrite => Current.Value?.write ?? false;
    public void Dispose() => Current.Value = previous;
}
