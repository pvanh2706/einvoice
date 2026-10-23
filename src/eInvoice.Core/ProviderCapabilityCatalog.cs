using System.Collections.Frozen;

namespace eInvoice.Core;

/// <summary>Capabilities observed in the pinned legacy dispatcher, not implementation progress.</summary>
public static class ProviderCapabilityCatalog
{
    private static readonly FrozenDictionary<ProviderId, FrozenSet<InvoiceAction>> BaselineActions =
        new Dictionary<ProviderId, FrozenSet<InvoiceAction>>
        {
            [ProviderId.Vnpt] = new[]
            {
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.CreateAndPublishBatch,
                InvoiceAction.Delete,
                InvoiceAction.DownloadPdf,
                InvoiceAction.DownloadDraftPdf,
                InvoiceAction.Draft,
                InvoiceAction.GetFormSerials,
                InvoiceAction.GetInvoiceStatus,
                InvoiceAction.HealthCheck,
                InvoiceAction.Preview,
                InvoiceAction.Publish,
                InvoiceAction.Replace,
                InvoiceAction.SearchById,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Minvoice] = new[]
            {
                InvoiceAction.Adjust,
                InvoiceAction.Change,
                InvoiceAction.Create,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.Delete,
                InvoiceAction.DownloadPdf,
                InvoiceAction.GetFormSerials,
                InvoiceAction.GetTaxCodeInformation,
                InvoiceAction.GetInvoices,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Publish,
                InvoiceAction.Replace,
                InvoiceAction.SendEmail,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Sun] = new[]
            {
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.DownloadPdf,
                InvoiceAction.DownloadDraftPdf,
                InvoiceAction.Draft,
                InvoiceAction.HealthCheck,
                InvoiceAction.Preview,
                InvoiceAction.Replace,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Viettel] = new[]
            {
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.CreateAndPublishBatch,
                InvoiceAction.DownloadPdf,
                InvoiceAction.Draft,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Preview,
                InvoiceAction.Replace,
                InvoiceAction.SearchById,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.VnptV5] = new[]
            {
                InvoiceAction.Adjust,
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.Create,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.CreateAndPublishBatch,
                InvoiceAction.Delete,
                InvoiceAction.DownloadPdf,
                InvoiceAction.DownloadDraftPdf,
                InvoiceAction.Draft,
                InvoiceAction.EditDraft,
                InvoiceAction.GetFormSerials,
                InvoiceAction.GetCertificateInformation,
                InvoiceAction.GetInvoiceStatus,
                InvoiceAction.HealthCheck,
                InvoiceAction.Preview,
                InvoiceAction.Publish,
                InvoiceAction.Replace,
                InvoiceAction.SendToTaxAuthority,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Misa] = new[]
            {
                InvoiceAction.Adjust,
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.CreateAndPublishBatch,
                InvoiceAction.PublishSignedXml,
                InvoiceAction.DownloadPdf,
                InvoiceAction.GetFormSerials,
                InvoiceAction.GetInvoiceStatus,
                InvoiceAction.GetInvoiceXml,
                InvoiceAction.GetXmlHash,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Preview,
                InvoiceAction.Replace,
                InvoiceAction.SendToTaxAuthority,
                InvoiceAction.SendSignedTaxAuthority,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Cyberbill] = new[]
            {
                InvoiceAction.Adjust,
                InvoiceAction.Change,
                InvoiceAction.Create,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.Delete,
                InvoiceAction.DownloadPdf,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Publish,
                InvoiceAction.Replace,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.Fast] = new[]
            {
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.CreateAndPublishBatch,
                InvoiceAction.DownloadPdf,
                InvoiceAction.DownloadDraftPdf,
                InvoiceAction.Draft,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Preview,
                InvoiceAction.Replace,
                InvoiceAction.Void,
            }.ToFrozenSet(),
            [ProviderId.EasyInvoice] = new[]
            {
                InvoiceAction.AdjustDecrease,
                InvoiceAction.AdjustIncrease,
                InvoiceAction.AdjustInformation,
                InvoiceAction.Create,
                InvoiceAction.CreateAndPublish,
                InvoiceAction.Delete,
                InvoiceAction.DownloadPdf,
                InvoiceAction.Draft,
                InvoiceAction.HealthCheck,
                InvoiceAction.Login,
                InvoiceAction.Preview,
                InvoiceAction.Publish,
                InvoiceAction.Replace,
                InvoiceAction.Void,
            }.ToFrozenSet(),
        }.ToFrozenDictionary();

    public static bool SupportsBaselineAction(ProviderId provider, InvoiceAction action) =>
        BaselineActions.TryGetValue(provider, out var actions) && actions.Contains(action);

    public static IReadOnlySet<InvoiceAction> GetBaselineActions(ProviderId provider) =>
        BaselineActions.TryGetValue(provider, out var actions) ? actions : FrozenSet<InvoiceAction>.Empty;
}
