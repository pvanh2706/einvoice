using System.Collections.Frozen;
using eInvoice.Core;

namespace eInvoice.Providers.Misa;

/// <summary>Implemented routes for the live baseline dispatcher. Financial/provider variants retain their individual mappings.</summary>
public static class MisaImplementedCapabilities
{
    public static IReadOnlyDictionary<InvoiceAction, string> Routes { get; } = new Dictionary<InvoiceAction, string>
    {
        [InvoiceAction.Login] = "health-check",
        [InvoiceAction.HealthCheck] = "health-check",
        [InvoiceAction.GetFormSerials] = "form-serials",
        [InvoiceAction.CreateAndPublish] = "publish",
        [InvoiceAction.CreateAndPublishBatch] = "publish-batch",
        [InvoiceAction.Preview] = "preview",
        [InvoiceAction.Adjust] = "adjust",
        [InvoiceAction.AdjustIncrease] = "adjust",
        [InvoiceAction.AdjustDecrease] = "adjust",
        [InvoiceAction.AdjustInformation] = "adjust",
        [InvoiceAction.Replace] = "replace",
        [InvoiceAction.DownloadPdf] = "document",
        [InvoiceAction.Void] = "void",
        [InvoiceAction.GetInvoiceStatus] = "status",
        [InvoiceAction.SendToTaxAuthority] = "tax/send-pending",
        [InvoiceAction.GetInvoiceXml] = "xml/create",
        [InvoiceAction.GetXmlHash] = "xml/hash",
        [InvoiceAction.SendSignedTaxAuthority] = "tax/send-signed",
        [InvoiceAction.PublishSignedXml] = "xml/publish",
    }.ToFrozenDictionary();
}
