"""Synthetic collections for the completed remaining providers; no configuration/secrets/network."""
from pathlib import Path
from copy import deepcopy
import json
root=Path(__file__).resolve().parent.parent
template=json.loads((root/'docs/examples/misa-publish.json').read_text(encoding='utf-8'))
providers={
 'FAST':['health-check','draft','publish','publish-batch','preview','replace','adjust/increase','adjust/decrease','adjust/information','document','document/draft','void'],
 'VNPT':['health-check','form-serials','draft','publish','publish-batch','publish-existing','preview','replace','adjust/increase','adjust/decrease','adjust/information','document','document/draft','void','delete','search','status'],
 'VNPT_V5':['health-check','form-serials','certificate','draft','draft/edit','create','publish','publish-batch','publish-existing','preview','replace','adjust','adjust/increase','adjust/decrease','adjust/information','document','document/draft','void','delete','status','tax/send'],
 'CYBER_BILL':['health-check','create','change','publish-existing','replace','adjust','document','void','delete'],
}
for provider,routes in providers.items():
    items=[];values={'baseUrl':'https://localhost:7150','apiKey':'','siteId':'','invoiceId':'22222222-2222-2222-2222-222222222222','newInvoiceId':'33333333-3333-3333-3333-333333333333','originalInvoiceId':'11111111-2222-3333-4444-555555555555','formNo':'1','serial':'C26TAA','invoiceDate':'13/10/2026','providerInvoiceCode':'','providerInvoiceNumber':'','searchCode':''}
    for route in routes:
        body=deepcopy(template);body['invoice'].update(invoiceId='{{invoiceId}}',serial='{{serial}}',formNo='{{formNo}}',invoiceDate='{{invoiceDate}}')
        body['details'][0]['transactionDate']='{{invoiceDate}}'
        if provider=='CYBER_BILL': body['invoice']['paymentMethod']='3'
        if route in ['health-check','form-serials','certificate']:body={}
        elif route in ['document','document/draft','void','delete','publish-existing','search']:
            body={'invoiceId':'{{invoiceId}}','invoiceType':0,'formNo':'{{formNo}}','serial':'{{serial}}'}
            if route=='search':body={'invoiceId':'{{invoiceId}}','formNo':'{{formNo}}'}
            else:
                if provider=='CYBER_BILL':body['providerInvoiceCode']='{{providerInvoiceCode}}'
                if route in ['document','document/draft','void']:body.update(invoiceDate='{{invoiceDate}}',searchCode='{{searchCode}}')
                if route=='void':body['reason']='Synthetic sandbox cancellation'
        elif route in ['status','tax/send']:body={'invoiceType':0,'formNo':'{{formNo}}','serial':'{{serial}}','invoiceIds':['{{invoiceId}}']}
        elif route=='publish-batch':body={'invoices':[body]}
        elif route.startswith('adjust') or route=='replace':
            body['invoice']['invoiceId']='{{newInvoiceId}}'
            body.update(originalInvoiceId='{{originalInvoiceId}}',originalProviderInvoiceNumber='{{providerInvoiceNumber}}',originalInvoice={'invoiceId':'{{originalInvoiceId}}','serial':'{{serial}}','formNo':'{{formNo}}','invoiceDate':'01/10/2026'})
        if provider.startswith('VNPT') and route=='publish':body['deleteExistingDraft']=False
        write=route not in ['health-check','form-serials','certificate','search','status','document','document/draft'] and not (route=='preview' and provider=='FAST')
        headers=[{'key':'Content-Type','value':'application/json'},{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]
        if write:
            key=route.replace('/','_')+'Key';values[key]=provider.lower()+'-'+route.replace('/','-')+'-001';headers.append({'key':'Idempotency-Key','value':'{{'+key+'}}'})
        request={'method':'POST','header':headers,'url':'{{baseUrl}}/api/eInvoice/'+route,'description':'Synthetic example. Configure sandbox locally; execute selected operations individually. Keep invoiceId/key/body stable; pending requires reconciliation. FAST/EASY health only validates configuration. Cyberbill create is unsigned; use publish-existing to sign. VNPT preview writes a draft. Never run a collection blindly.' ,'body':{'mode':'raw','raw':json.dumps(body,indent=2,ensure_ascii=False),'options':{'raw':{'language':'json'}}}}
        items.append({'name':route,'request':request})
    items.insert(0,{'name':'Capabilities','request':{'method':'GET','url':'{{baseUrl}}/api/eInvoice/capabilities','header':[{'key':'X-Api-Key','value':'{{apiKey}}'},{'key':'X-Site-Id','value':'{{siteId}}'}]}})
    collection={'info':{'name':'eInvoice '+provider+' API','schema':'https://schema.getpostman.com/json/collection/v2.1.0/collection.json','description':'Offline baseline port; sandbox UAT required. FAST: use a serial with M at the third position from the end for MTT.'},'item':items}
    environment={'name':'eInvoice '+provider+' template','values':[{'key':k,'value':v,'enabled':True,'type':'secret' if k=='apiKey' else 'default'} for k,v in values.items()],'_postman_variable_scope':'environment'}
    target=root/'docs/postman'
    for name,content in [('postman_collection',collection),('environment.template',environment)]:
        (target/('eInvoice-'+provider+'.'+name+'.json')).write_text(json.dumps(content,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(provider,len(items),'requests')
