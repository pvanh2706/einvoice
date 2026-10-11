"""Generate synthetic SUN requests without reading local credentials or calling HTTP."""
from pathlib import Path
from copy import deepcopy
import json
root=Path(__file__).resolve().parent.parent
invoice=json.loads((root/'docs/examples/misa-publish.json').read_text(encoding='utf-8'))
invoice['invoice'].update(invoiceId='{{invoiceId}}',formNo='{{formNo}}',serial='{{serial}}',invoiceDate='{{invoiceDate}}')
invoice['details'][0]['transactionDate']='{{invoiceDate}}'
items=[]
def add(name,route,body=None,key=None,method='POST',auth=True):
    headers=[{'key':'Content-Type','value':'application/json'}] if method=='POST' else []
    if auth: headers += [{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
    if key: headers.append({'key':'Idempotency-Key','value':'{{'+key+'}}'})
    request={'method':method,'header':headers,'url':'{{baseUrl}}'+route,'description':'SUN synthetic sandbox example. Configure partner=3 and both endpoints/host allowlists separately. Keep invoice ID, key and body stable. No automatic retry or reset.'}
    if body is not None: request['body']={'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}
    items.append({'name':name,'request':request})
add('Liveness','/health/live',method='GET',auth=False)
add('OpenAPI','/openapi/einvoice.json',method='GET',auth=False)
add('Capabilities','/api/eInvoice/capabilities',method='GET')
add('Fresh health check','/api/eInvoice/health-check',{})
add('Publish one invoice','/api/eInvoice/publish',invoice,'publishKey')
draft=deepcopy(invoice);draft['invoice']['invoiceId']='{{draftInvoiceId}}'
add('Create draft - does not confirm publication','/api/eInvoice/draft',draft,'draftKey')
add('Preview existing draft','/api/eInvoice/preview',{'invoice':{'invoiceType':0,'searchCode':'{{draftSearchCode}}'}})
for slug,key in [('adjust/increase','increaseKey'),('adjust/decrease','decreaseKey'),('replace','replaceKey')]:
    body=deepcopy(invoice);body['invoice']['invoiceId']='{{'+key.removesuffix('Key')+'InvoiceId}}'
    body['invoice'].update(sid='{{originalSid}}',searchCode='{{originalSearchCode}}')
    body['originalProviderInvoiceNumber']='{{originalProviderInvoiceNumber}}'
    add(slug,'/api/eInvoice/'+slug,body,key)
add('Download issued PDF','/api/eInvoice/document',{'invoiceType':0,'searchCode':'{{searchCode}}','typeFileDownload':'pdf'})
add('Download draft PDF','/api/eInvoice/document/draft',{'invoiceType':0,'searchCode':'{{draftSearchCode}}','typeFileDownload':'pdf'})
add('Void - sid comes from publish transactionId','/api/eInvoice/void',{'invoiceType':0,'sid':'{{sid}}','reason':'Synthetic cancellation reason'},'voidKey')
add('Read original publish attempt','/api/eInvoice/operations/publish/{{publishKey}}',method='GET')
add('Read invoice publish claim','/api/eInvoice/invoice-attempts?invoiceId={{invoiceId}}&phase=publish',method='GET')
values={'baseUrl':'https://localhost:7150','apiKey':'','siteId':'','invoiceId':'33333333-3333-3333-3333-333333333333','draftInvoiceId':'44444444-4444-4444-4444-444444444444',
    'increaseInvoiceId':'55555555-5555-5555-5555-555555555555','decreaseInvoiceId':'66666666-6666-6666-6666-666666666666','replaceInvoiceId':'77777777-7777-7777-7777-777777777777',
    'formNo':'','serial':'','invoiceDate':'11/10/2026','searchCode':'','draftSearchCode':'','sid':'','originalSid':'','originalSearchCode':'','originalProviderInvoiceNumber':''}
for key in ['publishKey','draftKey','increaseKey','decreaseKey','replaceKey','voidKey']: values[key]='sun-'+key+'-001'
target=root/'docs/postman'
collection={'info':{'name':'eInvoice SUN API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json','description':'10 baseline actions, offline tested, sandbox UAT still required. No status/form/batch. Draft and preview do not confirm publication.'},'item':items}
environment={'name':'eInvoice SUN template - configure sandbox locally','values':[{'key':k,'value':v,'enabled':True,'type':'secret' if k=='apiKey' else 'default'} for k,v in values.items()],'_postman_variable_scope':'environment'}
(target/'eInvoice-SUN.postman_collection.json').write_text(json.dumps(collection,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(target/'eInvoice-SUN.environment.template.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
print(f'Wrote {len(items)} synthetic SUN requests; no credentials/network.')
