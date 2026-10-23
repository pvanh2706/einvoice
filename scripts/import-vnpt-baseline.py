"""One-time pinned VNPT source import. Generated WSDL proxies are copied unchanged.
Only allowlisted source paths are read. Refuses to overwrite runtime files.
"""
from pathlib import Path
import subprocess, re, hashlib, json
root=Path(__file__).resolve().parent.parent
old=Path('D:/Code/25.ezInvoice');base='66a9c742d0f6fb692d339076e6cd82780e3476be'
manifest={}
def source(path):
    s=subprocess.check_output(['git','show',base+':'+path],cwd=old).decode('utf-8-sig')
    manifest[path]=hashlib.sha256(s.encode()).hexdigest();return s
def write(path,s):
    p=root/path
    if p.exists():
        if p.read_text(encoding='utf-8') == s: return
        raise ValueError('Refusing overwrite '+str(p))
    p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8')
prefix='''#nullable disable
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
'''
bridges={
 'CompanyViewModel':'SiteConfiguration','PmsCreateInvoiceRequest':'CreateInvoiceRequest',
 'PMSAdjustReplaceInvoiceRequest':'AdjustOrReplaceInvoiceRequest','PmsInvoiceModel':'CoreInvoice',
 'PmsInvoiceDetailModel':'InvoiceDetail','PmsTaxSummaryModel':'TaxSummary',
 'PmsServiceSummaryModel':'ServiceSummary','PmsSpecialTaxSummaryModel':'SpecialTaxSummary',
 'PmsDownloadPDFRequest':'DownloadPdfRequest','ExtraDataMapViewModel':'ExtraFieldMapping',
 'SendInvoiceCQT':'SendToTaxAuthorityRequest','PmsSearchInvoiceByIdRequest':'SearchInvoiceRequest',
 'PMSExtraData':'ExtraField','PrepareDataModel':'VnptMappingContext',
 '.TaxSummarys':'.TaxSummaries','.ServiceSummarys':'.ServiceSummaries','.SpecialTaxSummarys':'.SpecialTaxSummaries',
 '.InvoiceOld':'.OriginalInvoice','.oldezInvoiceId':'.OriginalInvoiceId','.ezInvoiceId':'.InvoiceId',
 '.TypeEinvoice':'.ElectronicInvoiceType','.searchCode':'.SearchCode',
 '.oldThirdPartyInvoiceNumber':'.OriginalProviderInvoiceNumber','.ThirdPartyInvoiceNumber':'.ProviderInvoiceNumber',
 'DateTime.Now':'VnptMappingScope.Now',
 'LoggingContext.Init().Company':'VnptMappingScope.Company',
 'new JavaScriptSerializer().Deserialize<':'JsonConvert.DeserializeObject<',
}
def convert(s):
    for a,b in bridges.items(): s=s.replace(a,b)
    return s
def imports(s):
    return '\n'.join(line for line in s.splitlines() if line.startswith('using ') and
        not any(x in line for x in ['ezInvoice','Helper','Microsoft','Nancy','Nest','Serilog','static ']))+'\n'
normal='src/ezInvoice.Application/EventSourcedNormalizers/VnptNormalizes/'
for name in ['VnptConvertRequest.cs','VnptMTTConvertRequest.cs','VnptConvertResponse.cs','VnptMTTConvertResponse.cs']:
    s=source(normal+name)
    body=s[s.index('namespace '):]
    body=re.sub(r'namespace [\w.]+','namespace eInvoice.Providers.Vnpt.Baseline',body,count=1)
    body=convert(body)
    write('src/eInvoice.Providers/Vnpt/Baseline/'+name,prefix+imports(s)+body)
paths=subprocess.check_output(['git','ls-tree','-r','--name-only',base,'src/ezInvoice.Application/ViewModels/VnptViewModels'],cwd=old).decode().splitlines()
for path in paths:
    if not path.endswith('.cs'): continue
    s=source(path);s=re.sub(r'^using (?:ezInvoice|Nest|Microsoft|Helper)[^;]*;\s*','',s,flags=re.M)
    s=re.sub(r'namespace [\w.]+','namespace eInvoice.Providers.Vnpt.Baseline',s,count=1)
    write('src/eInvoice.Providers/Vnpt/Baseline/Wire/'+Path(path).name,prefix+s)
for name in ['VnptAppService','VnptV5AppService']:
    s=source('src/ezInvoice.Application/Services/ThirdPartyServices/'+name+'.cs')
    body=convert(s[s.index('namespace '):])
    body=re.sub(r'namespace [\w.]+','namespace eInvoice.Providers.Vnpt.Baseline',body,count=1)
    body=body.replace(' : I'+name,'').replace(name,name.replace('AppService','BaselineService'))
    body=body.replace('IVnptRepository_TLYT','VnptRepository_TLYT').replace('IVnptRepository','VnptRepository').replace('IVnptV5Repository','VnptV5Repository')
    body=re.sub(r'^\s*Log\.Information\([^\n]*\);','',body,flags=re.M)
    write('src/eInvoice.Providers/Vnpt/Baseline/'+name.replace('AppService','BaselineService')+'.cs',prefix+imports(s)+body)
proxy='src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Vnpt/Connected Services/'
for service in ['BusinessService_V5','PortalService','PortalService_V5','PublishService','PublishService_V5','PublishService_TLYT']:
    s=source(proxy+service+'/Reference.cs')
    write('src/eInvoice.Providers/Vnpt/Generated/'+service+'.cs','#nullable disable\n'+s)
# Repository facade delegates exact generated operations through an injectable invoker.
for name,path in [('VnptRepository',''),('VnptRepository_TLYT',''),('VnptV5Repository','V5/')]:
    s=source('src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Vnpt/'+path+name+'.cs')
    imp=imports(s)
    fields={k:v for v,k in re.findall(r'private\s+(\w+)\s+(_\w+Service)\s*;',s)}
    calls=[]
    for match in re.finditer(r'public async Task<([^>]+)>\s+(\w+)\((\w+)\s+request\)',s):
        start=match.end();end=s.find('\n        public ',start)
        # Bound at the next public method regardless of indentation.
        next_public=re.search(r'\n\s*public (?:async |string )',s[start:])
        block=s[start:start+next_public.start()] if next_public else s[start:]
        call=re.search(r'await\s+(_\w+Service)\.(\w+)\(request\)',block)
        if not call: raise ValueError(name+' '+match.group(2))
        field,op=call.groups();contract=fields[field]
        client=contract+'Client'
        suffix='publishservice.asmx' if 'publish' in field.lower() else 'portalservice.asmx' if 'portal' in field.lower() else 'businessservice.asmx'
        calls.append(f'public Task<{match.group(1)}> {match.group(2)}({match.group(3)} request) => invoker.CallAsync<{match.group(1)}>(typeof({client}), UrlValue+"{suffix}", "{op}", request, ct);')
    urlname='Url_TLYT' if name.endswith('_TLYT') else 'Url'
    body=f'''namespace eInvoice.Providers.Vnpt.Baseline;
internal sealed class {name}(IVnptSoapInvoker invoker, CancellationToken ct)
{{
    private string UrlValue = "";
    public string {urlname} {{ set => UrlValue=value; }}
    {chr(10).join(calls)}
}}
'''
    write('src/eInvoice.Providers/Vnpt/Baseline/'+name+'.cs',prefix+imp+body)
write('docs/reference/vnpt-source-provenance.json',json.dumps({'baseline':base,'sources':manifest,'note':'Generated WSDL proxies copied unchanged; mapping/service sources bridged to canonical DTOs; repository calls injectable.'},indent=2))
print('Imported VNPT pinned mapping, orchestration and generated proxies.')
