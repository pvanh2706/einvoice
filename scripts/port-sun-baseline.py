"""One-time offline Sun mapper capture from the pinned, read-only legacy source."""
from pathlib import Path
import subprocess
import hashlib
import json
root = Path(__file__).resolve().parent.parent
base = '66a9c742d0f6fb692d339076e6cd82780e3476be'
old = Path('D:/Code/25.ezInvoice')
def source(path):
    return subprocess.check_output(['git','show',base+':'+path],cwd=old).decode('utf-8-sig')
path = 'src/ezInvoice.Application/EventSourcedNormalizers/SunNormalizes/SunConvertRequest.cs'
s = source(path)
original_hash = hashlib.sha256(s.encode()).hexdigest()
start = s.index('namespace ')
s = s[start:]
bridges = {
 'ezInvoice.Application.EventSourcedNormalizers.SunNormalizes':'eInvoice.Providers.Sun.Mapping',
 'public static class SunConvertRequest':'internal static class SunRequestMapper','PrepareDataModel':'SunMappingContext',
 'PmsCreateInvoiceRequest':'CreateInvoiceRequest','PmsInvoiceModel':'Invoice',
 'PmsInvoiceDetailModel':'InvoiceDetail','ExtraDataMapViewModel':'ExtraFieldMapping',
 'PMSExtraData':'ExtraField','PmsServiceSummaryModel':'ServiceSummary',
 'CompanyViewModel':'SiteConfiguration','PmsDownloadPDFRequest':'DownloadPdfRequest',
 'PMSAdjustReplaceInvoiceRequest':'AdjustOrReplaceInvoiceRequest',
 'SpecialTaxSummarys':'SpecialTaxSummaries','ServiceSummarys':'ServiceSummaries','TaxSummarys':'TaxSummaries',
 '.ezInvoiceId':'.InvoiceId','.transdate':'.TransactionDate','.searchCode':'.SearchCode',
 '.oldThirdPartyInvoiceNumber':'.OriginalProviderInvoiceNumber','.sid':'.Sid',
 'DateTime.Now':'model.Now',
 }
for old_name,new_name in bridges.items(): s=s.replace(old_name,new_name)
# The model variable is named DataInput in the void mapper.
s=s.replace('rdt = model.Now.ToString("yyyy-MM-dd"),\n\t\t\t};','rdt = DataInput.Now.ToString("yyyy-MM-dd"),\n\t\t\t};')
# Only PMS inputs were renamed; preserve the provider wire Body.sid and invoiceAdj.adj.
s=s.replace('sunRequest.Body.Sid','sunRequest.Body.sid')
prefix = '#nullable disable\n#pragma warning disable CS0414 // Historical unused field keys retained for fixture provenance.\nusing eInvoice.Core.Contracts;\nusing eInvoice.Providers.Sun.Wire;\nusing eInvoice.Providers.Sun.Formatting;\nusing Newtonsoft.Json.Linq;\nusing System.Globalization;\n'
s = prefix+s
s = s.replace('(dynamic model', '(SunMappingContext model').replace('(dynamic DataInput', '(SunMappingContext DataInput')
context = '''using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Sun.Mapping;
internal sealed class SunMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required dynamic Data { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
    public DateTime Now { get; init; }
}
'''
models = 'src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.SunEInvoice/ezInvoice.Infra.ThirdParty.SunEInvoice/Models/'
files = {'Mapping/SunRequestMapper.cs':s,'Mapping/SunMappingContext.cs':context}
for name in ['SunInvoiceModels.cs','SunRequestModels.cs','EndpointApi.cs']:
    wire = source(models+name)
    wire = wire.replace('using ezInvoice.Infra.ThirdParty.SunEInvoice.Configs;','')
    wire = wire.replace('ezInvoice.Infra.ThirdParty.SunEInvoice.Models','eInvoice.Providers.Sun.Wire')
    files['Wire/'+name] = '#nullable disable\n'+wire
# userInfo is the one extra live wire class used for health-check.
files['Wire/UserInfo.cs'] = '#nullable disable\nnamespace eInvoice.Providers.Sun.Wire;\npublic class userInfo { public string username { get; set; } public string password { get; set; } }\n'
for name,content in files.items():
    target=root/'src/eInvoice.Providers/Sun'/name
    target.parent.mkdir(parents=True,exist_ok=True);target.write_text(content,encoding='utf-8')
    frozen=root/'tools/SunFixtureCapture/Frozen'/name
    frozen.parent.mkdir(parents=True,exist_ok=True)
    frozen.write_text(content.replace('eInvoice.Providers.Sun','eInvoice.Capture.Sun'),encoding='utf-8')
reader = source('src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.SunEInvoice/Services/NumberReader.cs')
reader=reader.replace('ezInvoice.Infra.ThirdParty.SunEInvoice.Services','eInvoice.Capture.Sun.Formatting')
p=root/'tools/SunFixtureCapture/Frozen/Formatting/NumberReader.cs';p.parent.mkdir(parents=True,exist_ok=True)
p.write_text('#nullable disable\n'+reader,encoding='utf-8')
p=root/'.local/sun-source-provenance.json';p.parent.mkdir(parents=True,exist_ok=True)
p.write_text(json.dumps({'baseline':base,'mapper':path,'sha256':original_hash},indent=2),encoding='utf-8')
print('Imported pinned Sun mapper and independent frozen source; no HTTP/configuration.')
