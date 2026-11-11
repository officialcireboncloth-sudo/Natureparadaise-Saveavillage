"""Preserve accepted production before creating the six reference tulip appearances."""
import json,hashlib,base64
from g11_config import *
for p in [REVIEW,MASTER.parent,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 for folder in [A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Resources/Shops',A/'Data/Balance',TEXTURES,A/'Material/Shared/Plants']:
  for p in folder.rglob('*'):
   if p.is_file() and 'G11' not in p.parts and ATLAS not in p.name and p.name!='NP_Plants_Catalog.json':protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 baseline.write_text(json.dumps(dict(protected=protected,catalog=json.loads((TEXTURES/'NP_Plants_Catalog.json').read_text())),indent=2))
raw=GENERATED/'Tulip_Source_Sheet.png'
if not raw.exists():raw.write_bytes(base64.b64decode((ROOT/'tmp/G11_TextureSource.b64').read_text()))
(REVIEW/'ReferenceStudy.json').write_text(json.dumps(dict(source='Current user Tulip growth reference attachment',bulb='One tan pear-shaped fibrous bulb with a tapered papery neck and small basal roots',sprout='Same bulb, two tightly rolled green shoots',young='Three broad smooth strap-shaped leaves with pointed curved tips',bud='Two opposing broad leaves, continuous green stalk and one tapered rose-pink bud with green basal transition',ready='One pink cup, six overlapping rounded fleshy tepals, open upper cavity, two broad arching green leaves, no exposed bulb',harvest='Single cut tulip lying on its side, open flower at the left, long curved stem to the right and one broad attached leaf',direction='Thick cupped petal volumes, smooth edges, registered vertical fibres and vivid fresh colour'),indent=2))
print('G11_PREPARED',len(json.loads(baseline.read_text())['protected']),'protected files',flush=True)
