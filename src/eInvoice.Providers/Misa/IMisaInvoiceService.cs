using eInvoice.Core.Configuration;
using eInvoice.Core.Contracts;

namespace eInvoice.Providers.Misa;

public interface IMisaInvoiceService
{
    Task<IReadOnlyList<InvoiceResult>> PublishAsync(ResolvedSite site, CreateInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceResult>> PublishBatchAsync(ResolvedSite site, InvoiceBatchRequest request, CancellationToken cancellationToken);
    Task<InvoiceDocumentResult> PreviewAsync(ResolvedSite site, AdjustOrReplaceInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceResult>> AdjustAsync(ResolvedSite site, AdjustOrReplaceInvoiceRequest request, bool replace, CancellationToken cancellationToken);
    Task<InvoiceDocumentResult> DownloadAsync(ResolvedSite site, DownloadPdfRequest request, CancellationToken cancellationToken);
    Task VoidAsync(ResolvedSite site, VoidInvoiceRequest request, CancellationToken cancellationToken);
    Task<string?> SendTaxAsync(ResolvedSite site, SendPendingTaxAuthorityRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceStatusResult>> StatusAsync(ResolvedSite site, SendToTaxAuthorityRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceResult>> CreateXmlAsync(ResolvedSite site, CreateInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceHashResult>> HashAsync(ResolvedSite site, GetXmlHashRequest request, CancellationToken cancellationToken);
    Task<string?> SendSignedTaxAsync(ResolvedSite site, SendSignedTaxAuthorityRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceResult>> PublishSignedXmlAsync(ResolvedSite site, PublishSignedXmlRequest request, CancellationToken cancellationToken);
}
