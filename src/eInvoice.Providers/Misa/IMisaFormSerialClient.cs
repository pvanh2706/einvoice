using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public interface IMisaFormSerialClient
{
    Task<IReadOnlyList<FormSerialResponse>> GetAsync(SiteConfiguration site,
        CancellationToken cancellationToken = default);
}
