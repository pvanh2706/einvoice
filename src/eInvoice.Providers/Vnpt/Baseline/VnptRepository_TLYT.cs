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
using PublishService_TLYT;
namespace eInvoice.Providers.Vnpt.Baseline;
internal sealed class VnptRepository_TLYT(IVnptSoapInvoker invoker, CancellationToken ct)
{
    private string UrlValue = "";
    public string Url_TLYT { set => UrlValue=value; }
    public Task<publishInvByFkeyResponse> PublishInvoiceReceipt(publishInvByFkeyRequest request) => invoker.CallAsync<publishInvByFkeyResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "publishInvByFkeyAsync", request, ct);
public Task<ImportInvResponse> CreateInvoiceReceipt(ImportInvRequest request) => invoker.CallAsync<ImportInvResponse>(typeof(PublishServiceSoapClient), UrlValue+"publishservice.asmx", "ImportInvAsync", request, ct);
}
