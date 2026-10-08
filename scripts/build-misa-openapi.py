"""Build the offline MISA OpenAPI contract from canonical DTO properties. No credentials/network."""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parent.parent
contracts = root / 'src/eInvoice.Core/Contracts'
classes = {}
for path in contracts.glob('*.cs'):
    text = path.read_text(encoding='utf-8-sig')
    match = re.search(r'public (?:sealed )?class (\w+)(?:\s*:\s*(\w+))?', text)
    if not match or match[1].endswith('Exception'):
        continue
    props = re.findall(r'public\s+([\w<>?]+)\s+(\w+)\s*\{\s*get;\s*set;', text)
    classes[match[1]] = (match[2], props)

def schema_for(type_name):
    nullable = type_name.endswith('?')
    name = type_name.removesuffix('?')
    primitives = {'string': {'type':'string'}, 'Guid': {'type':'string','format':'uuid'},
                  'DateTime': {'type':'string','format':'date-time'}, 'decimal': {'type':'number'},
                  'int': {'type':'integer','format':'int32'}, 'long': {'type':'integer','format':'int64'}, 'bool': {'type':'boolean'}}
    if name.startswith('List<'):
        value = {'type':'array','items':schema_for(name[5:-1])}
    else:
        value = primitives.get(name, {'$ref':'#/components/schemas/'+name})
    return {'anyOf':[value,{'type':'null'}]} if nullable else value

def properties(name):
    base, props = classes[name]
    result = properties(base) if base in classes else {}
    result.update({prop[0].lower()+prop[1:]:schema_for(type_name) for type_name, prop in props})
    return result

schemas = {name:{'type':'object','additionalProperties':False,'properties':properties(name)} for name in classes}
def nullable_string(): return {'type':['string','null']}
schemas['InvoiceResult'] = {'type':'object','properties':{
    'invoiceId':{'type':'string'},'providerInvoiceNumber':nullable_string(),'transactionId':nullable_string(),
    'searchCode':nullable_string(),'taxAuthorityCode':nullable_string(),'invoiceData':nullable_string(),
    'isSuccess':{'type':'boolean'},'providerErrorCode':nullable_string()}}
schemas['InvoiceDocumentResult'] = {'type':'object','properties':{'content':{'type':'string'},'format':{'type':'string','enum':['base64','url']}}}
schemas['InvoiceStatusResult'] = {'type':'object','properties':{'invoiceId':{'type':'string'},'taxAuthorityStatus':{'type':'integer'}}}
schemas['InvoiceHashResult'] = {'type':'object','properties':{'messageCode':nullable_string(),'messageHash':nullable_string()}}
def array_of(name): return {'type':'array','items':{'$ref':'#/components/schemas/'+name}}
def success_schema(route):
    common={'provider':{'type':'string','enum':['MISA']},'siteId':{'type':'string','format':'uuid'},'requestId':{'type':'string'}}
    if route=='health-check': common['connected']={'type':'boolean'}
    elif route=='form-serials': common['forms']=array_of('FormSerialResponse')
    else:
        data=array_of('InvoiceResult')
        if route in ['preview','document']: data={'$ref':'#/components/schemas/InvoiceDocumentResult'}
        elif route=='status': data=array_of('InvoiceStatusResult')
        elif route=='xml/hash': data=array_of('InvoiceHashResult')
        elif route=='void': data={'type':'object','properties':{'success':{'type':'boolean'}}}
        elif route in ['tax/send-pending','tax/send-signed']: data={'type':'object','properties':{'providerData':nullable_string()}}
        common['data']=data
    return {'type':'object','properties':common}
schemas['ProblemDetails'] = {'type':'object','properties':{
    'type':{'type':'string'},'title':{'type':'string'},'status':{'type':'integer'},
    'code':{'type':'string'},'providerCode':{'type':'string'},'requestId':{'type':'string'},'traceId':{'type':'string'}},'required':['status','code','requestId']}
problem = {'description':'JSON error; no raw provider response or credential.',
           'content':{'application/problem+json':{'schema':{'$ref':'#/components/schemas/ProblemDetails'}}}}
site_header = {'name':'X-Site-Id','in':'header','required':True,'schema':{'type':'string','format':'uuid'}}
idempotency = {'name':'Idempotency-Key','in':'header','required':True,
               'description':'Reuse the same key AND identical body to retrieve a completed result. Never resend a pending/uncertain attempt with a new key.',
               'schema':{'type':'string','minLength':8,'maxLength':128,'pattern':'^[A-Za-z0-9_-]+$'}}
routes = [
 ('health-check','ProviderHealthCheckRequest',False,'Check a fresh MISA v3 login; bypass token cache.'),
 ('form-serials','GetFormSerialsRequest',False,'InvoiceType 0 only. Current year UTC+7. GET timeout 90s after login.'),
 ('publish','CreateInvoiceRequest',True,'Publish one invoice/ticket/receipt. Normal USB uses the separate XML workflow.'),
 ('publish-batch','InvoiceBatchRequest',True,'Homogeneous batch. Inspect isSuccess on EACH result; results correlate by invoiceId, not position.'),
 ('preview','AdjustOrReplaceInvoiceRequest',False,'InvoiceType 0; provider preview URL. Original invoice required for previewType 2/3.'),
 ('adjust','AdjustOrReplaceInvoiceRequest',True,'InvoiceType 0 adjustment; new invoiceId and original invoice information required.'),
 ('replace','AdjustOrReplaceInvoiceRequest',True,'InvoiceType 0 replacement; new invoiceId and original invoice information required.'),
 ('document','DownloadPdfRequest',False,'Invoice uses searchCode; ticket/receipt use transactionId. Returns provider base64 content.'),
 ('void','VoidInvoiceRequest',True,'Cancel invoice/ticket/receipt with reason. No automatic retry.'),
 ('status','SendToTaxAuthorityRequest',False,'InvoiceType 0 status lookup by invoiceIds. External MISA key ezInvoiceID is preserved internally.'),
 ('tax/send-pending','SendPendingTaxAuthorityRequest',True,'MISA HSM sends all pending provider invoices for the site/AppID. NO invoiceIds filter. Legacy capability retained; never call automatically.'),
 ('xml/create','CreateInvoiceRequest',True,'Create USB invoice XML. Follow create -> hash -> client signing -> tax/send-signed -> xml/publish.'),
 ('xml/hash','GetXmlHashRequest',False,'Fetch USB message hashes using certificate Base64. Sign externally; never send private key.'),
 ('tax/send-signed','SendSignedTaxAuthorityRequest',True,'Submit externally signed messageCode/signatureValue to CQT.'),
 ('xml/publish','PublishSignedXmlRequest',True,'Publish signed XML after the USB CQT steps.'),
]
paths = {}
for route, contract, write, description in routes:
    paths['/api/eInvoice/'+route] = {'post':{
        'operationId':'misa_'+route.replace('/','_').replace('-','_'), 'tags':['MISA'],
        'summary':description,'security':[{'ApiKey':[]}], 'parameters':[site_header]+([idempotency] if write else []),
        'requestBody':{'required':True,'content':{'application/json':{'schema':{'$ref':'#/components/schemas/'+contract}}}},
        'responses':{'200':{'description':'Completed API operation. Business data is in data (forms for form-serials). Batch may contain per-item failures.',
            'content':{'application/json':{'schema':success_schema(route)}}},
                     **{str(code):problem for code in [400,401,403,404,409,422,500,501,502,503,504]}}
    }}
paths['/api/eInvoice/operations/{operation}/{idempotencyKey}'] = {'get':{
    'operationId':'misa_operation_status','tags':['MISA'],'summary':'Read local attempt state/result without contacting MISA.',
    'security':[{'ApiKey':[]}],'parameters':[site_header,
        {'name':'operation','in':'path','required':True,'schema':{'type':'string'},'description':'Route slug: publish, publish-batch, xml-create, xml-publish, tax-send-signed, adjust, replace, void.'},
        {'name':'idempotencyKey','in':'path','required':True,'schema':{'type':'string'}}],
    'responses':{'200':{'description':'state: not-found/pending/completed, updatedAtUtc, result.'},'400':problem,'401':problem,'403':problem}
}}
paths['/health/live'] = {'get':{'operationId':'liveness','responses':{'200':{'description':'Process is alive; no provider call.'}}}}
paths['/api/eInvoice/misa/capabilities'] = {'get':{'operationId':'misa_implemented_capabilities','security':[{'ApiKey':[]}],
    'parameters':[site_header],'responses':{'200':{'description':'Implemented action-to-route map; does not claim sandbox readiness.'},'401':problem,'403':problem}}}
document = {'openapi':'3.1.0','info':{'title':'eInvoice MISA API','version':'0.2.0',
    'description':'Rewrite contract. Synthetic/offline tests; sandbox UAT still required. Only new PMS route/keys. Provider wire keys unchanged.'},
    'servers':[{'url':'https://localhost:7150'}], 'paths':paths,
    'components':{'securitySchemes':{'ApiKey':{'type':'apiKey','in':'header','name':'X-Api-Key'}},'schemas':schemas}}
target = root / 'docs/openapi.misa.json'
target.write_text(json.dumps(document, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
print(f'Wrote {len(paths)} paths and {len(schemas)} canonical schemas; no configuration or secrets read.')
