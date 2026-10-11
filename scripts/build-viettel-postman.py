"""Generate synthetic Viettel integration examples; never read local site credentials."""
from pathlib import Path
import json
from copy import deepcopy

root = Path(__file__).resolve().parent.parent
target = root / 'docs/postman'
invoice = json.loads((root / 'docs/examples/misa-publish.json').read_text(encoding='utf-8'))
invoice['invoice'].update(invoiceId='VT-TEST-001', formNo='{{formNo}}', serial='{{serial}}', invoiceDate='{{invoiceDate}}')
invoice['details'][0]['transactionDate'] = '{{invoiceDate}}'
items = []

def add(name, route, body=None, key=None, method='POST', auth=True):
    headers = [{'key':'Content-Type','value':'application/json'}] if method == 'POST' else []
    if auth:
        headers += [{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
    if key:
        headers.append({'key':'Idempotency-Key','value':'{{'+key+'}}'})
    request = {'method':method,'header':headers,'url':'{{baseUrl}}'+route,
        'description':'Viettel synthetic sandbox example. Configure a separate server site partner=4 and host allowlist. Set issued form/serial/date before first send. Keep invoice ID, key and body fixed; no automatic retries or claim reset.'}
    if body is not None:
        request['body'] = {'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}
    items.append({'name':name,'request':request})

add('Liveness','/health/live',method='GET',auth=False)
add('OpenAPI','/openapi/einvoice.json',method='GET',auth=False)
add('Site provider capabilities','/api/eInvoice/capabilities',method='GET')
add('Viettel fresh connection check','/api/eInvoice/health-check',{})
add('Publish invoice','/api/eInvoice/publish',invoice,'publishKey')
batch1 = deepcopy(invoice); batch1['invoice']['invoiceId'] = 'VT-BATCH-001'
batch2 = deepcopy(invoice); batch2['invoice']['invoiceId'] = 'VT-BATCH-002'
add('Publish batch - inspect each item','/api/eInvoice/publish-batch',{'invoices':[batch1,batch2]},'batchKey')
draft = deepcopy(invoice); draft['invoice']['invoiceId'] = 'VT-DRAFT-001'
add('Save draft - does not mean published','/api/eInvoice/draft',draft,'draftKey')
add('Preview PDF','/api/eInvoice/preview',invoice)
for slug in ['adjust/increase','adjust/decrease','adjust/information','replace']:
    value = deepcopy(invoice)
    value['invoice']['invoiceId'] = 'VT-'+slug.upper().replace('/','-')+'-001'
    value['originalProviderInvoiceNumber'] = '{{providerInvoiceNumber}}'
    add(slug,'/api/eInvoice/'+slug,value,slug.replace('/','_')+'Key')
add('Download PDF','/api/eInvoice/document',{'invoiceId':'VT-TEST-001','invoiceType':0,'formNo':'{{formNo}}','providerInvoiceNumber':'{{providerInvoiceNumber}}','typeFileDownload':'pdf'})
add('Search issued invoice by original PMS ID','/api/eInvoice/search',{'invoiceId':'VT-TEST-001'})
add('Void issued invoice','/api/eInvoice/void',{'invoiceType':0,'formNo':'{{formNo}}','providerInvoiceNumber':'{{providerInvoiceNumber}}','invoiceDate':'{{invoiceDate}}','additionalReferenceDate':'{{invoiceDate}}','reason':'Synthetic cancellation reason'},'voidKey')
add('Read original publish result','/api/eInvoice/operations/publish/{{publishKey}}',method='GET')
add('Read invoice publish claim','/api/eInvoice/invoice-attempts?invoiceId=VT-TEST-001&phase=publish',method='GET')
collection = {'info':{'name':'eInvoice VIETTEL API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json','description':'Offline-tested Viettel routes. Sandbox UAT not yet done. No form-serials/status capability. Search reads provider state but does not unlock journal.'},'item':items}
values = {'baseUrl':'https://localhost:7150','apiKey':'','siteId':'22222222-2222-2222-2222-222222222222','formNo':'','serial':'','invoiceDate':'11/10/2026','providerInvoiceNumber':''}
for key in ['publishKey','batchKey','draftKey','adjust_increaseKey','adjust_decreaseKey','adjust_informationKey','replaceKey','voidKey']:
    values[key] = 'viettel-'+key+'-001'
environment = {'name':'eInvoice VIETTEL template - configure sandbox locally','values':[{'key':k,'value':v,'enabled':True,'type':'secret' if k=='apiKey' else 'default'} for k,v in values.items()],'_postman_variable_scope':'environment'}
(target/'eInvoice-VIETTEL.postman_collection.json').write_text(json.dumps(collection,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(target/'eInvoice-VIETTEL.environment.template.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
print(f'Wrote {len(items)} synthetic Viettel requests; no local configuration or credential read.')
