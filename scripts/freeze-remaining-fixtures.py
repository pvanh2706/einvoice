"""Freeze pure baseline mapper sources for independent offline characterization.
Run only once; never overwrite a frozen mapper or expected payload.
"""
from pathlib import Path
import subprocess, json, hashlib
root=Path(__file__).resolve().parent.parent
tool=root/'tools/RemainingFixtureCapture'
sources=[]
for group in ['Fast/Mapping','Fast/Wire','Vnpt/Generated','Vnpt/Baseline/Wire']:
    sources.extend((root/'src/eInvoice.Providers'/group).glob('*.cs'))
for name in ['VnptConvertRequest.cs','VnptMTTConvertRequest.cs','VnptMappingContext.cs','VnptConst.cs']:
    sources.append(root/'src/eInvoice.Providers/Vnpt/Baseline'/name)
sources.append(root/'src/eInvoice.Providers/Baseline/Enums.cs')
hashes={}
for path in sources:
    relative=path.relative_to(root/'src/eInvoice.Providers');target=tool/'Frozen'/relative
    if target.exists(): raise ValueError('Frozen source already exists: '+str(target))
    target.parent.mkdir(parents=True,exist_ok=True)
    source=path.read_text(encoding='utf-8');target.write_text(source,encoding='utf-8')
    hashes[str(relative)]=hashlib.sha256(source.encode()).hexdigest()
# Preserve the baseline XML serializer implementation independent from runtime consolidation.
xml=subprocess.check_output(['git','-C','D:/Code/25.ezInvoice','show','66a9c742d0f6fb692d339076e6cd82780e3476be:src/ezInvoice.Application/Helper/XmlHelper.cs']).decode('utf-8-sig')
xml=xml.replace('ezInvoice.Application.Heldper','eInvoice.Providers.Serialization')
target=tool/'Frozen/Serialization/XmlHelper.cs';target.parent.mkdir(parents=True,exist_ok=True)
target.write_text('#nullable disable\n'+xml,encoding='utf-8')
(tool/'source-provenance.json').write_text(json.dumps({'baseline':'66a9c742d0f6fb692d339076e6cd82780e3476be','frozenFiles':hashes,'note':'Pure baseline mapping and generated WSDL contracts only; no runtime provider reference, orchestration, configuration reads or network.'},indent=2)+'\n',encoding='utf-8')
print('Frozen independent FAST/VNPT mapping sources. Capture is offline.')
