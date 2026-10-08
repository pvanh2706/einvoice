"""Generate a shareable Postman collection using synthetic values and variable references only."""
from pathlib import Path
import json

root = Path(__file__).resolve().parent.parent
target = root / 'docs/postman'
target.mkdir(parents=True, exist_ok=True)
invoice = {
 'invoice':{'invoiceId':'INV-PUBLISH-001','invoiceType':0,'formNo':'1','serial':'C26TAA','invoiceDate':'08/10/2026',
    'customerName':'Khách thử nghiệm','currencyCode':'VND','exchangeRate':1,'paymentMethod':'TM/CK',
    'subAmount':100000,'beforeTaxAmount':100000,'taxRate':10,'taxAmount':10000,'afterTaxAmount':110000,
    'isMultiVATRate':False,'pricePrecision':0,'quantityPrecision':0,'amountPrecision':0},
 'details':[{'itemCode':'ROOM','itemName':'Dịch vụ thử nghiệm','unitName':'Lần','quantity':1,'price':100000,
    'subAmount':100000,'beforeTaxAmount':100000,'taxRate':10,'taxAmount':10000,'afterTaxAmount':110000,
    'serviceCharge':0,'discountAmount':0,'exciseTaxCharge':0,'itemType':'1','transactionDate':'08/10/2026'}],
 'taxSummaries':[{'beforeTaxAmount':100000,'taxRate':10,'taxAmount':10000}], 'serviceSummaries':[], 'extraData':[],
 'electronicInvoiceType':0
}
def clone(value): return json.loads(json.dumps(value))
items=[]
def add(name, route, body=None, write=False, method='POST', auth=True):
    headers=[{'key':'Content-Type','value':'application/json'}] if method=='POST' else []
    if auth: headers += [{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
    if write: headers += [{'key':'Idempotency-Key','value':'{{idempotencyKey}}'}]
    request={'method':method,'header':headers,'url':'{{baseUrl}}'+route,
       'description':'Synthetic example. Configure sandbox/admin locally first. Keep key/body identical on retry; investigate pending/unknown before any resend.'}
    if body is not None: request['body']={'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}
    items.append({'name':name,'request':request})
add('Liveness','/health/live',method='GET',auth=False)
add('OpenAPI','/openapi/misa.json',method='GET',auth=False)
add('Implemented MISA capabilities','/api/eInvoice/misa/capabilities',method='GET')
add('MISA fresh connection check','/api/eInvoice/health-check',{'requestId':'postman-health-001'})
add('Invoice forms and serials','/api/eInvoice/form-serials',{'invoiceType':0,'requestId':'postman-form-001'})
add('Publish HSM invoice','/api/eInvoice/publish',invoice,write=True)
batch1=clone(invoice); batch1['invoice']['invoiceId']='INV-BATCH-001'
batch2=clone(invoice); batch2['invoice']['invoiceId']='INV-BATCH-002'
add('Publish homogeneous batch','/api/eInvoice/publish-batch',{'invoices':[batch1,batch2],'requestId':'postman-batch-001'},write=True)
add('Preview new invoice','/api/eInvoice/preview',invoice)
for route in ['adjust','replace']:
    value=clone(invoice); value['invoice']['invoiceId']='INV-'+route.upper()+'-001'
    value.update({'originalInvoiceId':'INV-ORIGINAL-001','originalProviderInvoiceNumber':'000001',
                 'originalInvoice':{'invoiceId':'INV-ORIGINAL-001','invoiceType':0,'formNo':'1','serial':'1C26TAA','invoiceDate':'01/09/2026'}})
    add(route.title()+' HSM invoice','/api/eInvoice/'+route,value,write=True)
add('Download invoice document','/api/eInvoice/document',{'invoiceType':0,'electronicInvoiceType':0,'searchCode':'{{searchCode}}','typeFileDownload':'pdf'})
add('Void invoice','/api/eInvoice/void',{'invoiceId':'INV-PUBLISH-001','invoiceType':0,'providerInvoiceNumber':'{{providerInvoiceNumber}}',
    'searchCode':'{{searchCode}}','reason':'Synthetic cancellation reason'},write=True)
add('Invoice CQT statuses','/api/eInvoice/status',{'invoiceIds':['INV-PUBLISH-001'],'invoiceType':0,'electronicInvoiceType':0})
add('HSM CQT - send pending queue (no invoice ID filter)','/api/eInvoice/tax/send-pending',{'requestId':'postman-cqt-pending-001'},write=True)
xml=clone(invoice); xml['invoice']['invoiceId']='INV-USB-001'; xml['electronicInvoiceType']=2
add('USB 1 - Create XML','/api/eInvoice/xml/create',xml,write=True)
add('USB 2 - Get message hashes','/api/eInvoice/xml/hash',{'certBase64':'{{certBase64}}'})
add('USB 3 - Send signed message','/api/eInvoice/tax/send-signed',{'messageCode':'{{messageCode}}','signatureValue':'{{signatureValue}}'},write=True)
add('USB 4 - Publish signed XML','/api/eInvoice/xml/publish',{'invoiceId':'INV-USB-001','invoiceType':0,'searchCode':'{{searchCode}}','invoiceData':'{{signedInvoiceXml}}'},write=True)
# JSON-encode XML/signature variables to preserve quotes/backslashes/newlines. Never log them.
items[-1]['event']=[{'listen':'prerequest','script':{'type':'text/javascript','exec':[
    "const body = {invoiceId: 'INV-USB-001', invoiceType: 0, searchCode: pm.environment.get('searchCode'), invoiceData: pm.environment.get('signedInvoiceXml')};",
    "pm.request.body.update(JSON.stringify(body));"
]}}]
add('Read publish attempt/result','/api/eInvoice/operations/publish/{{idempotencyKey}}',method='GET')
collection={'info':{'name':'eInvoice MISA API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
    'description':'Synthetic requests. Default local installation returns 503 until admin/MISA sandbox is configured. Write journal must persist across restarts.'},'item':items}
(target/'eInvoice-MISA.postman_collection.json').write_text(json.dumps(collection,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
values={'baseUrl':'https://localhost:7150','apiKey':'','siteId':'11111111-1111-1111-1111-111111111111','idempotencyKey':'postman-request-001',
        'searchCode':'','providerInvoiceNumber':'','certBase64':'','messageCode':'','signatureValue':'','signedInvoiceXml':''}
environment={'name':'eInvoice MISA template - set secrets locally','values':[{'key':key,'value':value,'enabled':True,'type':'secret' if key=='apiKey' else 'default'} for key,value in values.items()],
             '_postman_variable_scope':'environment'}
(target/'eInvoice-MISA.environment.template.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
print(f'Wrote {len(items)} synthetic Postman requests and an environment template without API key.')
