"""Synthetic Minvoice collection; no real config, credentials or provider calls."""
from pathlib import Path
from copy import deepcopy
import json
root=Path(__file__).resolve().parent.parent
invoice=json.loads((root/'docs/examples/misa-publish.json').read_text(encoding='utf-8'))
invoice['invoice'].update(invoiceId='{{invoiceId}}',serial='{{serial}}',invoiceDate='{{invoiceDate}}',searchCode='SYNTHETIC-SEARCH-001')
invoice['details'][0]['transactionDate']='{{invoiceDate}}'
items=[]
def add(name,route,body=None,key=None,method='POST',auth=True):
    headers=[{'key':'Content-Type','value':'application/json'}] if method=='POST' else []
    if auth: headers += [{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
    if key: headers.append({'key':'Idempotency-Key','value':'{{'+key+'}}'})
    request={'method':method,'header':headers,'url':'{{baseUrl}}'+route,'description':'Minvoice synthetic sandbox example. Configure partner=2, credentials and allowed HTTPS host separately. Persist key/body and providerInvoiceCode. No auto retry or claim reset.'}
    if body is not None: request['body']={'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}
    items.append({'name':name,'request':request})
add('Liveness','/health/live',method='GET',auth=False)
add('OpenAPI','/openapi/einvoice.json',method='GET',auth=False)
add('Capabilities','/api/eInvoice/capabilities',method='GET')
add('Health check - configured unit','/api/eInvoice/health-check',{})
add('Form and serial list','/api/eInvoice/form-serials',{'invoiceType':0})
add('Create and publish - SaveSign','/api/eInvoice/publish',invoice,'publishKey')
draft=deepcopy(invoice);draft['invoice']['invoiceId']='{{draftInvoiceId}}'
add('Create unsigned - Save','/api/eInvoice/create',draft,'createKey')
change=deepcopy(draft);change.update(originalProviderInvoiceCode='{{draftProviderInvoiceCode}}',originalProviderInvoiceNumber='{{draftProviderInvoiceNumber}}')
add('Change unsigned - Save editmode 2','/api/eInvoice/change',change,'changeKey')
add('Sign existing unsigned - Sign','/api/eInvoice/publish-existing',{'invoiceType':0,'invoiceId':'{{draftInvoiceId}}','providerInvoiceCode':'{{draftProviderInvoiceCode}}','providerInvoiceNumber':'{{draftProviderInvoiceNumber}}'},'signKey')
for slug,key in [('replace','replaceKey'),('adjust','adjustKey')]:
    body=deepcopy(invoice);body['invoice']['invoiceId']='{{'+slug+'InvoiceId}}'
    body.update(originalProviderInvoiceCode='{{providerInvoiceCode}}',originalProviderInvoiceNumber='{{providerInvoiceNumber}}')
    add(slug,'/api/eInvoice/'+slug,body,key)
add('Delete unsigned by serial and number','/api/eInvoice/delete',{'invoiceType':0,'invoiceId':'{{draftInvoiceId}}','serial':'{{serial}}','providerInvoiceNumber':'{{draftProviderInvoiceNumber}}'},'deleteKey')
add('Void signed invoice','/api/eInvoice/void',{'invoiceType':0,'invoiceId':'{{invoiceId}}','providerInvoiceCode':'{{providerInvoiceCode}}','providerInvoiceNumber':'{{providerInvoiceNumber}}'},'voidKey')
add('Download PDF by provider GUID','/api/eInvoice/document',{'invoiceType':0,'invoiceId':'{{invoiceId}}','providerInvoiceCode':'{{providerInvoiceCode}}','typeFileDownload':'pdf'})
add('Get invoice sign statuses','/api/eInvoice/invoices',{'fromDate':'2026-10-01T00:00:00','toDate':'2026-10-11T00:00:00','serial':'{{serial}}'})
add('Tax registry - separate allowlist required','/api/eInvoice/tax-code',{'taxCode':'{{taxCode}}'})
add('Email - explicit sandbox recipient only','/api/eInvoice/email',{'invoiceId':'{{invoiceId}}','providerInvoiceCode':'{{providerInvoiceCode}}','email':'{{email}}'},'emailKey')
add('Read original publish attempt','/api/eInvoice/operations/publish/{{publishKey}}',method='GET')
add('Read PMS invoice publish claim','/api/eInvoice/invoice-attempts?invoiceId={{invoiceId}}&phase=publish',method='GET')
values={'baseUrl':'https://localhost:7150','apiKey':'','siteId':'','invoiceId':'33333333-3333-3333-3333-333333333333','draftInvoiceId':'44444444-4444-4444-4444-444444444444',
 'replaceInvoiceId':'55555555-5555-5555-5555-555555555555','adjustInvoiceId':'66666666-6666-6666-6666-666666666666','serial':'','invoiceDate':'11/10/2026',
 'providerInvoiceCode':'','providerInvoiceNumber':'','draftProviderInvoiceCode':'','draftProviderInvoiceNumber':'','taxCode':'','email':''}
for key in ['publishKey','createKey','changeKey','signKey','replaceKey','adjustKey','deleteKey','voidKey','emailKey']: values[key]='minvoice-'+key+'-001'
target=root/'docs/postman'
collection={'info':{'name':'eInvoice MINVOICE API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json','description':'15 baseline actions, offline tests only. No sandbox UAT. Business VP/Bear and health configured unit retained. Pending never authorizes blind retry. Create/change are unsigned operations.'},'item':items}
environment={'name':'eInvoice MINVOICE template - configure sandbox locally','values':[{'key':k,'value':v,'enabled':True,'type':'secret' if k=='apiKey' else 'default'} for k,v in values.items()],'_postman_variable_scope':'environment'}
(target/'eInvoice-MINVOICE.postman_collection.json').write_text(json.dumps(collection,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(target/'eInvoice-MINVOICE.environment.template.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
print(f'Wrote {len(items)} synthetic MINVOICE requests; no credentials/network.')
