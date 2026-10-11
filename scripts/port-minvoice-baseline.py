"""One-time offline Minvoice mapper import from the pinned legacy commit. No configuration/HTTP."""
from pathlib import Path
import subprocess
import hashlib
import json
root=Path(__file__).resolve().parent.parent
base='66a9c742d0f6fb692d339076e6cd82780e3476be'
old=Path('D:/Code/25.ezInvoice')
def source(path): return subprocess.check_output(['git','show',base+':'+path],cwd=old).decode('utf-8-sig')
path='src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Minvoice/Config/APIServices.cs'
s=source(path)
def method(name):
    # Extract one exact method body, retaining nested initializer/interpolation braces.
    import re
    match=re.search(r'\s*(?:public|private)\s+(?:string|InvoiceMaster|InvoiceDetails|List<invoiceDetailAdjust>|InvoiceMasterReplace|InvoiceMasterAdjust)\s+'+name+r'\(',s)
    if not match: raise ValueError(name)
    start=match.start();brace=s.index('{',match.end());depth=1;pos=brace+1
    while depth:
        if s[pos]=='{': depth+=1
        elif s[pos]=='}': depth-=1
        pos+=1
    return s[start:pos].strip()
names=['convertInvoiceFormat','getMasterData','GetSearchCode','getdetailData','getdetailDataIncrease','convertDataDelete','convertReplaceInv','replaceMasterConvert','convertAdjustInv','adjustMasterConvert','convertCancelInv','convertSignInv','convertGetInv']
mapper='\n\n'.join(method(name) for name in names)
bridges={
 'PmsInvoiceModel':'Invoice','PmsInvoiceDetailModel':'InvoiceDetail','HotelExtraModel':'HotelInformation',
 'PMSExtraData':'ExtraField','PMSAdjustReplaceInvoiceRequest':'AdjustOrReplaceInvoiceRequest','PMSGetInvoiceRequest':'GetInvoicesRequest',
 '.oldezInvoiceId':'.OriginalInvoiceId','.oldThirdPartyInvoiceNumber':'.OriginalProviderInvoiceNumber',
 '.ezInvoiceId':'.InvoiceId','.ThirdPartyInvoiceNumber':'.ProviderInvoiceNumber',
 'DateTime.Now':'Now',
 }
for old_name,new_name in bridges.items(): mapper=mapper.replace(old_name,new_name)
mapper=mapper.replace('hoadon68_id = model.InvoiceId == null ? null : model.InvoiceId','hoadon68_id = model.InvoiceId == null ? null : Guid.Parse(model.InvoiceId)')
mapper=mapper.replace('master.hoadon68_id = model.InvoiceId == null ? null : model.InvoiceId','master.hoadon68_id = model.InvoiceId == null ? null : Guid.Parse(model.InvoiceId)')
mapper=mapper.replace('request.Invoice.InvoiceNo = request.OriginalProviderInvoiceNumber','request.Invoice.InvoiceNo = request.OriginalProviderInvoiceNumber == null ? null : Convert.ToInt64(request.OriginalProviderInvoiceNumber)')
mapper=mapper.replace('throw new Exception(', 'throw new InvoiceRequestException("INVALID_INVOICE_REQUEST", ')
header='''#nullable disable
#pragma warning disable CS0219 // Baseline unused local retained for characterization.
using eInvoice.Core.Contracts;
using eInvoice.Providers.Minvoice.Wire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using static eInvoice.Providers.Minvoice.Wire.MinvoiceModels;
namespace eInvoice.Providers.Minvoice.Mapping;
internal sealed class MinvoiceRequestMapper
{
    public DateTime Now { get; init; }
'''
mapper=header+mapper+'\n}\n'
for name in ['convertReplaceInv','convertAdjustInv','convertCancelInv','convertSignInv','convertGetInv']:
    mapper=mapper.replace('private string '+name,'public string '+name)
# Input bridges are separate from supplier wire types; do not change wire GUID/casing.
input_bridge='''#nullable disable
using eInvoice.Core.Contracts;
namespace eInvoice.Providers.Minvoice.Mapping;
internal sealed class MinvoiceCreateInvoiceRequest : AdjustOrReplaceInvoiceRequest
{
    public string editmode { get; set; }
}
'''
wire_path='src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.Minvoice/Models/MinvoiceModels.cs'
wire=source(wire_path)
start=wire.index('        public class M_InvoiceInputModel')
wire='#nullable disable\nnamespace eInvoice.Providers.Minvoice.Wire;\npublic class MinvoiceModels\n{\n'+wire[start:wire.index('        #endregion',start)]+'\n}\n'
# Remove duplicate legacy PMS model bridges from the wire-only group.
start=wire.index('        public class SendEmailInput')
wire=wire[:start]+'}\n'
wire+='public class GetInvoiceRequest { public string tuNgay { get; set; } public string denNgay { get; set; } public string khieu { get; set; } }\n'
files={'Mapping/MinvoiceRequestMapper.cs':mapper,'Mapping/MinvoiceCreateInvoiceRequest.cs':input_bridge,'Wire/MinvoiceModels.cs':wire}
for name,content in files.items():
    content='\n'.join(line.rstrip() for line in content.splitlines())+'\n'
    target=root/'src/eInvoice.Providers/Minvoice'/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(content,encoding='utf-8')
    frozen=root/'tools/MinvoiceFixtureCapture/Frozen'/name;frozen.parent.mkdir(parents=True,exist_ok=True)
    frozen.write_text(content.replace('eInvoice.Providers.Minvoice','eInvoice.Capture.Minvoice'),encoding='utf-8')
manifest={'baseline':base,'mapperSource':path,'mapperSha256':hashlib.sha256(s.encode()).hexdigest(),'wireSource':wire_path,
 'note':'Only live pure mapping methods extracted; typed PMS bridges/GUID parse and injected UTC+7 Now. GetSearchCode baseline random retained; fixtures set explicit SearchCode.'}
target=root/'tools/MinvoiceFixtureCapture/source-provenance.json';target.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('Imported Minvoice pure mapping and independent frozen source, no credentials or HTTP.')
