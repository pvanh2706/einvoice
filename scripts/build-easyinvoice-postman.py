"""Synthetic EasyInvoice requests, no real configuration/credentials or HTTP."""
from pathlib import Path
from copy import deepcopy
import json
root=Path(__file__).resolve().parent.parent
invoice=json.loads((root/'docs/examples/misa-publish.json').read_text(encoding='utf-8'))
invoice['invoice'].update(invoiceId='{{invoiceId}}',serial='{{serial}}',invoiceDate='{{invoiceDate}}')
invoice['details'][0]['transactionDate']='{{invoiceDate}}'
items=[]
def add(name,route,body=None,key=None,method='POST',auth=True):
    headers=[{'key':'Content-Type','value':'application/json'}] if method=='POST' else []
    if auth: headers += [{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
    if key: headers.append({'key':'Idempotency-Key','value':'{{'+key+'}}'})
    request={'method':method,'header':headers,'url':'{{baseUrl}}'+route,'description':'EasyInvoice synthetic sandbox example. Partner 9; configure allowed HTTPS host and credentials on server. Ikey is PMS invoiceId. Persist key/body; pending never authorizes retry. No real provider calls in capture/tests.'}
    if body is not None: request['body']={'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}
    items.append({'name':name,'request':request})
add('Liveness','/health/live',method='GET',auth=False)
add('OpenAPI','/openapi/einvoice.json',method='GET',auth=False)
add('Capabilities','/api/eInvoice/capabilities',method='GET')
add('Create and publish','/api/eInvoice/publish',invoice,'publishKey')
for slug,key in [('create','createKey'),('draft','draftKey')]:
    body=deepcopy(invoice); body['invoice']['invoiceId']='{{draftInvoiceId}}'
    add(slug+' unsigned - choose ONE import route','/api/eInvoice/'+slug,body,key)
add('Publish existing draft','/api/eInvoice/publish-existing',{'invoiceType':0,'invoiceId':'{{draftInvoiceId}}','serial':'{{serial}}'},'signKey')
for slug,key in [('replace','replaceKey'),('adjust/increase','increaseKey'),('adjust/decrease','decreaseKey'),('adjust/information','informationKey')]:
    body=deepcopy(invoice); body['invoice']['invoiceId']='{{newInvoiceId}}'
    body.update(originalInvoiceId='{{invoiceId}}',originalInvoice={'invoiceId':'{{invoiceId}}','maCQT':'{{taxAuthorityCode}}'})
    add(slug,'/api/eInvoice/'+slug,body,key)
preview=deepcopy(invoice);preview['invoice']['previewType']=1
add('Preview normal XML','/api/eInvoice/preview',preview)
add('Download issued PDF','/api/eInvoice/document',{'invoiceType':0,'invoiceId':'{{invoiceId}}','serial':'{{serial}}','typeFileDownload':'pdf'})
add('Delete unsigned draft','/api/eInvoice/delete',{'invoiceType':0,'invoiceId':'{{draftInvoiceId}}','serial':'{{serial}}'},'deleteKey')
add('Void issued invoice','/api/eInvoice/void',{'invoiceType':0,'invoiceId':'{{invoiceId}}','serial':'{{serial}}'},'voidKey')
add('Read original publish attempt','/api/eInvoice/operations/publish/{{publishKey}}',method='GET')
add('Read invoice publish claim','/api/eInvoice/invoice-attempts?invoiceId={{invoiceId}}&phase=publish',method='GET')
values={'baseUrl':'https://localhost:7150','apiKey':'','siteId':'','invoiceId':'EASY-SYNTHETIC-001','draftInvoiceId':'EASY-DRAFT-001','newInvoiceId':'EASY-NEW-001','serial':'','invoiceDate':'11/10/2026','taxAuthorityCode':''}
for key in ['publishKey','createKey','draftKey','signKey','replaceKey','increaseKey','decreaseKey','informationKey','deleteKey','voidKey']:values[key]='easy-'+key+'-001'
collection={'info':{'name':'eInvoice EASY_INVOICE API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json','description':'12 implemented business actions; health behavior pending Q016. Offline only, UAT required. Create and draft are the same unsigned import, choose one. Each adjustment/replacement needs its own newInvoiceId/key; do not run all with the same identity.'},'item':items}
environment={'name':'eInvoice EASY_INVOICE template - configure sandbox locally','values':[{'key':key,'value':value,'enabled':True,'type':'secret' if key=='apiKey' else 'default'} for key,value in values.items()],'_postman_variable_scope':'environment'}
target=root/'docs/postman'
(target/'eInvoice-EASY_INVOICE.postman_collection.json').write_text(json.dumps(collection,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(target/'eInvoice-EASY_INVOICE.environment.template.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
print(f'Wrote {len(items)} synthetic EasyInvoice requests; no credentials/network.')
