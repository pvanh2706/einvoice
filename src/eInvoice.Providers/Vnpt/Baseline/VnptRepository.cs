#nullable disable
#pragma warning disable CS1998, CS0219, CS0414
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using eInvoice.Providers.Vnpt.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Dynamic;
using System.Globalization;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using BusinessService_V5;
using PortalService;
using PublishService;
namespace eInvoice.Providers.Vnpt.Baseline;
internal sealed class VnptRepository(IVnptSoapInvoker invoker, CancellationToken ct)
{
    private string UrlValue = "";
    public string Url { set => UrlValue=value; }
    public Task<ReplaceInvoiceActionResponse> ReplaceInvoice(ReplaceInvoiceActionRequest request) => invoker.CallAsync<ReplaceInvoiceActionResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "ReplaceInvoiceActionAsync", request, ct);
public Task<AdjustInvoiceActionResponse> AdjustInvoice(AdjustInvoiceActionRequest request) => invoker.CallAsync<AdjustInvoiceActionResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "AdjustInvoiceActionAsync", request, ct);
public Task<cancelInvResponse> VoidInvoice(cancelInvRequest request) => invoker.CallAsync<cancelInvResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "cancelInvAsync", request, ct);
public Task<cancelInvNoPayResponse> VoidInvoiceNoPay(cancelInvNoPayRequest request) => invoker.CallAsync<cancelInvNoPayResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "cancelInvNoPayAsync", request, ct);
public Task<unConfirmPaymentResponse> UnConfirmPaymentInvoice(unConfirmPaymentRequest request) => invoker.CallAsync<unConfirmPaymentResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "unConfirmPaymentAsync", request, ct);
public Task<deleteInvoiceByFkeyResponse> DeleteInvoice(deleteInvoiceByFkeyRequest request) => invoker.CallAsync<deleteInvoiceByFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "deleteInvoiceByFkeyAsync", request, ct);
public Task<downloadNewInvPDFFkeyResponse> DownloadPDFDraft(downloadNewInvPDFFkeyRequest request) => invoker.CallAsync<downloadNewInvPDFFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadNewInvPDFFkeyAsync", request, ct);
public Task<downloadInvPDFFkeyResponse> DownloadPDF(downloadInvPDFFkeyRequest request) => invoker.CallAsync<downloadInvPDFFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadInvPDFFkeyAsync", request, ct);
public Task<ImportInvByPatternResponse> CreateInvoice(ImportInvByPatternRequest request) => invoker.CallAsync<ImportInvByPatternResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportInvByPatternAsync", request, ct);
public Task<ImportAndPublishInvResponse> CreateAndPublishInvoice(ImportAndPublishInvRequest request) => invoker.CallAsync<ImportAndPublishInvResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportAndPublishInvAsync", request, ct);
public Task<PublishInvFkeyResponse> PublishInvoice(PublishInvFkeyRequest request) => invoker.CallAsync<PublishInvFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "PublishInvFkeyAsync", request, ct);
public Task<getPublishInvsResponse> GetFormSerials(getPublishInvsRequest request) => invoker.CallAsync<getPublishInvsResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "getPublishInvsAsync", request, ct);
public Task<listInvByCusFkeyResponse> ListInvByCusFkey(listInvByCusFkeyRequest request) => invoker.CallAsync<listInvByCusFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "listInvByCusFkeyAsync", request, ct);
}
