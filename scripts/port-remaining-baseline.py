"""Allowlisted offline imports from the pinned baseline; never reads configuration or calls suppliers.
Run once for new files; refuses to overwrite an existing import.
"""
from pathlib import Path
import subprocess, re, json, hashlib
root=Path(__file__).resolve().parent.parent
base='66a9c742d0f6fb692d339076e6cd82780e3476be'
old=Path('D:/Code/25.ezInvoice')
manifest={}
def source(path):
    data=subprocess.check_output(['git','show',base+':'+path],cwd=old).decode('utf-8-sig')
    manifest[path]=hashlib.sha256(data.encode()).hexdigest()
    return data
def write(path,s):
    target=root/path
    if target.exists(): raise ValueError('Refusing to overwrite '+str(target))
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text('\n'.join(line.rstrip() for line in s.splitlines())+'\n',encoding='utf-8')
bridges={
 'CompanyViewModel':'SiteConfiguration','PmsCreateInvoiceRequest':'CreateInvoiceRequest',
 'PMSAdjustReplaceInvoiceRequest':'AdjustOrReplaceInvoiceRequest','PmsInvoiceModel':'CoreInvoice',
 'PmsInvoiceDetailModel':'InvoiceDetail','PmsTaxSummaryModel':'TaxSummary',
 'PmsServiceSummaryModel':'ServiceSummary','PmsSpecialTaxSummaryModel':'SpecialTaxSummary',
 'PmsDownloadPDFRequest':'DownloadPdfRequest','ExtraDataMapViewModel':'ExtraFieldMapping',
 'SendInvoiceCQT':'SendToTaxAuthorityRequest','PmsSearchInvoiceByIdRequest':'SearchInvoiceRequest',
 'PmsSearchInvoiceRequest':'SearchInvoiceRequest','PMSExtraData':'ExtraField',
 '.TaxSummarys':'.TaxSummaries','.ServiceSummarys':'.ServiceSummaries',
 '.SpecialTaxSummarys':'.SpecialTaxSummaries','.InvoiceOld':'.OriginalInvoice',
 '.oldezInvoiceId':'.OriginalInvoiceId','.ezInvoiceId':'.InvoiceId',
 '.TypeEinvoice':'.ElectronicInvoiceType','.searchCode':'.SearchCode',
 '.oldThirdPartyInvoiceNumber':'.OriginalProviderInvoiceNumber',
 '.ThirdPartyInvoiceNumber':'.ProviderInvoiceNumber',
}
def convert(s):
    for a,b in bridges.items(): s=s.replace(a,b)
    return s
prefix='''#nullable disable
#pragma warning disable CS1998, CS0219, CS0414
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.Serialization;
using eInvoice.Providers.Baseline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
'''
s=source('src/ezInvoice.Application/EventSourcedNormalizers/FastNormalizes/FastConvertRequest.cs')
s=s[s.index('namespace '):].replace('ezInvoice.Application.EventSourcedNormalizers.FastNormalizes','eInvoice.Providers.Fast.Mapping')
s=convert(s).replace('public static class FastConvertReQuest','internal static class FastRequestMapper').replace('(dynamic model','(FastMappingContext model')
write('src/eInvoice.Providers/Fast/Mapping/FastRequestMapper.cs',prefix+'using eInvoice.Providers.Fast.Wire;\n'+s)
for name in ['FastInvoiceModels.cs','FastRequestModels.cs']:
    s=source('src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Fast/Models/'+name)
    s=s.replace('using Nest;','').replace('ezInvoice.Infra.ThirdParty.Fast.Models','eInvoice.Providers.Fast.Wire')
    write('src/eInvoice.Providers/Fast/Wire/'+name,'#nullable disable\n'+s)
write('src/eInvoice.Providers/Fast/Mapping/FastMappingContext.cs','''using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Fast.Mapping;
internal sealed class FastMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public int Action { get; init; }
    public bool IsSerialMTT { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
}
''')
s=source('src/ezInvoice.Application/Services/ThirdPartyServices/EnumDefine.cs')
s=s[s.index('namespace '):].replace('ezInvoice.Application.Services.ThirdPartyServices','eInvoice.Providers.Baseline')
write('src/eInvoice.Providers/Baseline/Enums.cs',s)
write('docs/reference/remaining-source-provenance.json',json.dumps({'baseline':base,'sources':manifest},indent=2))
print('Imported FAST mapping/wire only, with pinned provenance.')
