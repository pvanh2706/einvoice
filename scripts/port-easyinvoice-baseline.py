"""One-time import of allowlisted EasyInvoice sources from the pinned baseline. No HTTP."""
from pathlib import Path
import subprocess, hashlib, json
root = Path(__file__).resolve().parent.parent
base = '66a9c742d0f6fb692d339076e6cd82780e3476be'
def source(path):
    return subprocess.check_output(['git', '-C', 'D:/Code/25.ezInvoice', 'show', base+':'+path]).decode('utf-8-sig')
path = 'src/ezInvoice.Application/EventSourcedNormalizers/EasyInvoiceNormalizes/EasyInvoiceConvertRequest.cs'
original = source(path)
s = original[original.index('namespace '):]
# Authentication is kept in transport; capture compares body/endpoint, never credential headers.
s = s[:s.index('\t\tpublic static Dictionary<string, string> CreateHeaderAPI')] + '\n\t}\n}\n'
bridges = {
    'ezInvoice.Application.EventSourcedNormalizers.EasyInvoiceNormalizes':'eInvoice.Providers.EasyInvoice.Mapping',
    'public static class EasyInvoiceConvertRequest':'internal static class EasyInvoiceRequestMapper',
    '(dynamic model':'(EasyInvoiceMappingContext model',
    'CompanyViewModel':'SiteConfiguration', 'PmsCreateInvoiceRequest':'CreateInvoiceRequest',
    'PMSAdjustReplaceInvoiceRequest':'AdjustOrReplaceInvoiceRequest',
    'PmsInvoiceModel':'CoreInvoice', 'PmsInvoiceDetailModel':'InvoiceDetail',
    'PmsTaxSummaryModel':'TaxSummary', 'PmsServiceSummaryModel':'ServiceSummary',
    'PmsDownloadPDFRequest':'DownloadPdfRequest', 'ExtraDataMapViewModel':'ExtraFieldMapping',
    '.TaxSummarys':'.TaxSummaries', '.ServiceSummarys':'.ServiceSummaries',
    '.oldezInvoiceId':'.OriginalInvoiceId', '.InvoiceOld':'.OriginalInvoice', '.ezInvoiceId':'.InvoiceId',
    'CreateHeaderAPI(companies)':'new Dictionary<string, string>()',
    'metadata':'Metadata',
    'Convert.ToInt32(model.Action)':'(int)model.Action',
    'INVOICE_ACTION.ADJUST_DECREASE':'InvoiceAction.AdjustDecrease',
    'INVOICE_ACTION.ADJUST_INCREASE':'InvoiceAction.AdjustIncrease',
    'PREVIEW_TYPE.':'PreviewType.',
    'EASY_INVOICE_ADJUST_TYPE.INFO':'EasyAdjustType.Info',
    'EASY_INVOICE_ADJUST_TYPE.INCREASE':'EasyAdjustType.Increase',
    'EASY_INVOICE_ADJUST_TYPE.DECREASE':'EasyAdjustType.Decrease',
    'EASY_INVOICE_TYPE.NORMAL':'EasyType.Normal',
    'EASY_INVOICE_TYPE.ADJUST_INFO':'EasyType.AdjustInfo',
    'EASY_INVOICE_TYPE.ADJUST_INCREASE':'EasyType.AdjustIncrease',
    'EASY_INVOICE_TYPE.ADJUST_DECREASE':'EasyType.AdjustDecrease',
    'EASY_INVOICE_TYPE.REPLACE':'EasyType.Replace',
}
for before, after in bridges.items(): s=s.replace(before, after)
prefix = '''#nullable disable
#pragma warning disable CS0414 // Retain unused historical reserved-invoice endpoint for provenance.
using eInvoice.Core;
using eInvoice.Core.Contracts;
using eInvoice.Core.Formatting;
using eInvoice.Providers.EasyInvoice.Wire;
using eInvoice.Providers.Serialization;
using Newtonsoft.Json;
using CoreInvoice = eInvoice.Core.Contracts.Invoice;
using Invoice = eInvoice.Providers.EasyInvoice.Wire.Invoice;
'''
context = '''using eInvoice.Core;
using eInvoice.Core.Contracts;
namespace eInvoice.Providers.EasyInvoice.Mapping;
internal sealed class EasyInvoiceMappingContext
{
    public required SiteConfiguration Company { get; init; }
    public required object Data { get; init; }
    public InvoiceAction Action { get; init; }
    public List<ExtraFieldMapping> ExtraData { get; init; } = [];
}
// Values are imported from the baseline enum, see provenance.
internal enum EasyAdjustType { Info = 4, Increase = 2, Decrease = 3 }
internal enum EasyType { Normal = 0, AdjustInfo = 4, AdjustIncrease = 2, AdjustDecrease = 3, Replace = 1 }
internal enum PreviewType { CREATE = 1, ADJUST = 2, REPLACE = 3, ADJUST_INCREASE = 4, ADJUST_DECREASE = 5, ADJUST_INFO = 6 }
internal sealed class Metadata
{
    public string? keyTag { get; set; }
    public string? valueType { get; set; }
    public string? keyLabel { get; set; }
    public string? stringValue { get; set; }
}
'''
files = {'Mapping/EasyInvoiceRequestMapper.cs':prefix+s,'Mapping/EasyInvoiceMappingContext.cs':context}
models = 'src/ezInvoice.Infra.ThirdParty/ezInvoice.Infra.ThirdParty.EasyInvoice/Models/'
for name in ['EasyInvoiceModels.cs','EasyInvoiceRequestModels.cs']:
    wire=source(models+name).replace('using Nest;', '').replace('ezInvoice.Infra.ThirdParty.EasyInvoice.Models','eInvoice.Providers.EasyInvoice.Wire')
    files['Wire/'+name]='#nullable disable\n'+wire
for name, content in files.items():
    content='\n'.join(line.rstrip() for line in content.splitlines())+'\n'
    target=root/'src/eInvoice.Providers/EasyInvoice'/name
    target.parent.mkdir(parents=True,exist_ok=True); target.write_text(content,encoding='utf-8')
    target=root/'tools/EasyInvoiceFixtureCapture/Frozen'/name
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(content.replace('eInvoice.Providers.EasyInvoice','eInvoice.Capture.EasyInvoice').replace('eInvoice.Providers.Serialization','eInvoice.Capture.Serialization'),encoding='utf-8')
xml=source('src/ezInvoice.Application/Helper/XmlHelper.cs')
xml=xml.replace('ezInvoice.Application.Heldper','eInvoice.Capture.Serialization')
xml='\n'.join(line.rstrip() for line in xml.splitlines())+'\n'
target=root/'tools/EasyInvoiceFixtureCapture/Frozen/Serialization/XmlHelper.cs'
target.parent.mkdir(parents=True,exist_ok=True); target.write_text('#nullable disable\n'+xml,encoding='utf-8')
target=root/'tools/EasyInvoiceFixtureCapture/source-provenance.json'
target.write_text(json.dumps({'baseline':base,'mapper':path,'decodedSourceSha256':hashlib.sha256(original.encode()).hexdigest(), 'notes':['Typed PMS bridges only; body/endpoint unchanged. Authentication tested in transport.', 'Frozen mapper retains duplicated financial blocks; runtime consolidation must match these fixtures.']},indent=2),encoding='utf-8')
print('Imported EasyInvoice mapper/wire and independent frozen source. Verify enum values before capture.')
