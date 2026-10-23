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
using PortalService_V5;
using PublishService_V5;
namespace eInvoice.Providers.Vnpt.Baseline;
internal sealed class VnptV5Repository(IVnptSoapInvoker invoker, CancellationToken ct)
{
    private string UrlValue = "";
    public string Url { set => UrlValue=value; }
    public Task<AdjustInvoiceActionResponse> AdjustInvoice(AdjustInvoiceActionRequest request) => invoker.CallAsync<AdjustInvoiceActionResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "AdjustInvoiceActionAsync", request, ct);
public Task<ImportAndPublishInvResponse> CreateAndPublishInvoice(ImportAndPublishInvRequest request) => invoker.CallAsync<ImportAndPublishInvResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportAndPublishInvAsync", request, ct);
public Task<PublishInvFkeyResponse> PublishInvoice(PublishInvFkeyRequest request) => invoker.CallAsync<PublishInvFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "PublishInvFkeyAsync", request, ct);
public Task<downloadInvPDFFkeyNoPayResponse> DownloadPDF(downloadInvPDFFkeyNoPayRequest request) => invoker.CallAsync<downloadInvPDFFkeyNoPayResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadInvPDFFkeyNoPayAsync", request, ct);
public Task<downloadNewInvPDFFkeyResponse> DownloadPDFInvoiceDraft(downloadNewInvPDFFkeyRequest request) => invoker.CallAsync<downloadNewInvPDFFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadNewInvPDFFkeyAsync", request, ct);
public Task<getPublishInvsResponse> GetFormSerials(getPublishInvsRequest request) => invoker.CallAsync<getPublishInvsResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "getPublishInvsAsync", request, ct);
public Task<ReplaceInvoiceActionResponse> ReplaceInvoice(ReplaceInvoiceActionRequest request) => invoker.CallAsync<ReplaceInvoiceActionResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "ReplaceInvoiceActionAsync", request, ct);
public Task<cancelInvNoPayResponse> VoidInvoice(cancelInvNoPayRequest request) => invoker.CallAsync<cancelInvNoPayResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "cancelInvNoPayAsync", request, ct);
public Task<ImportInvByPatternResponse> CreateInvoice(ImportInvByPatternRequest request) => invoker.CallAsync<ImportInvByPatternResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportInvByPatternAsync", request, ct);
public Task<deleteInvoiceByFkeyResponse> DeleteInvoice(deleteInvoiceByFkeyRequest request) => invoker.CallAsync<deleteInvoiceByFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "deleteInvoiceByFkeyAsync", request, ct);
public Task<GetInvDataByFkeyResponse> GetInvoiceByFkey(GetInvDataByFkeyRequest request) => invoker.CallAsync<GetInvDataByFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "GetInvDataByFkeyAsync", request, ct);
public Task<GetMCCQThueByFkeysResponse> SearchByFkey(GetMCCQThueByFkeysRequest request) => invoker.CallAsync<GetMCCQThueByFkeysResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "GetMCCQThueByFkeysAsync", request, ct);
public Task<GetCertInfoResponse> GetSerialCertInfo(GetCertInfoRequest request) => invoker.CallAsync<GetCertInfoResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "GetCertInfoAsync", request, ct);
public Task<ImportInvByPatternMTTResponse> CreateInvoiceMTT(ImportInvByPatternMTTRequest request) => invoker.CallAsync<ImportInvByPatternMTTResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportInvByPatternMTTAsync", request, ct);
public Task<downloadNewInvPDFFkeyResponse> DownloadPDFInvoiceDraftMTT(downloadNewInvPDFFkeyRequest request) => invoker.CallAsync<downloadNewInvPDFFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadNewInvPDFFkeyAsync", request, ct);
public Task<ImportAndPublishInvMTTResponse> CreateAndPublishInvoiceMTT(ImportAndPublishInvMTTRequest request) => invoker.CallAsync<ImportAndPublishInvMTTResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportAndPublishInvMTTAsync", request, ct);
public Task<downloadInvPDFFkeyNoPayErrorResponse> DownloadPDFMTT(downloadInvPDFFkeyNoPayErrorRequest request) => invoker.CallAsync<downloadInvPDFFkeyNoPayErrorResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "downloadInvPDFFkeyNoPayErrorAsync", request, ct);
public Task<getInvViewFkeyResponse> DownloadPDFFkeyMTT(getInvViewFkeyRequest request) => invoker.CallAsync<getInvViewFkeyResponse>(typeof(PortalServiceSoapClient), UrlValue+"portalservice.asmx", "getInvViewFkeyAsync", request, ct);
public Task<AdjustInvMTTResponse> AdjustInvoiceMTT(AdjustInvMTTRequest request) => invoker.CallAsync<AdjustInvMTTResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "AdjustInvMTTAsync", request, ct);
public Task<ReplaceInvMTTResponse> ReplaceInvoiceMTT(ReplaceInvMTTRequest request) => invoker.CallAsync<ReplaceInvMTTResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "ReplaceInvMTTAsync", request, ct);
public Task<cancelInvNoPayResponse> VoidInvoiceMTT(cancelInvNoPayRequest request) => invoker.CallAsync<cancelInvNoPayResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "cancelInvNoPayAsync", request, ct);
public Task<SendInvMTTFkeyResponse> SendInvoiceCQT(SendInvMTTFkeyRequest request) => invoker.CallAsync<SendInvMTTFkeyResponse>(typeof(BusinessServiceSoapClient), UrlValue+"businessservice.asmx", "SendInvMTTFkeyAsync", request, ct);
public Task<GetMCCQThueByFkeysResponse> GetStatusInvoice(GetMCCQThueByFkeysRequest request) => invoker.CallAsync<GetMCCQThueByFkeysResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "GetMCCQThueByFkeysAsync", request, ct);
}
