"""Capture existing production before adding G10; decode the generated texture source."""
import json,hashlib,base64
from g10_config import *
for p in [REVIEW,MASTER.parent,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 for directory in [A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Resources/Shops',A/'Data/Balance',TEXTURES,A/'Material/Shared/Plants']:
  for p in directory.rglob('*'):
   if p.is_file() and 'G10' not in p.parts and ATLAS not in p.name and p.name!='NP_Plants_Catalog.json':protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 baseline.write_text(json.dumps(dict(protected=protected,catalog=json.loads((TEXTURES/'NP_Plants_Catalog.json').read_text())),indent=2))
data=json.loads(baseline.read_text())
if 'catalog' not in data:
 data['catalog']=json.loads((TEXTURES/'NP_Plants_Catalog.json').read_text());baseline.write_text(json.dumps(data,indent=2))
raw=GENERATED/'RadialFlowers_Source_Sheet.png'
if not raw.exists():raw.write_bytes(base64.b64decode((ROOT/'tmp/G10_TextureSource.b64').read_text()))
(REVIEW/'DesignStudy.json').write_text(json.dumps(dict(referenceStatus='No new growth-reference attachments supplied for G10; follows the existing project species plan and accepted G09 visual direction.',direction='Sculpted closed petals, crisp registered leaf/petal textures, saturated albedo, curved connected floral stems; five growth stages and separate harvest.',species={
 'Sunflower':'Tall thick stem; broad heart-shaped serrated leaves; two yellow ray rings around brown phyllotactic florets; striped seed.',
 'Chrysanthemum':'Branched compact shrub; deeply lobed leaves; dense golden incurving spoon petals; spherical buds.',
 'Dahlia':'Opposite compound serrated leaves; coral pink pointed cupped petals in overlapping concentric rings; round green buds.',
 'Marigold':'Pinnate foliage of narrow serrated leaflets; short branched shrub; orange ruffled flower heads; elongated ribbed calyx and black tipped seed.'},existingGameplay='Preserved; prepared art has separate Unity visual prefabs for review.'),indent=2))
print('G10_PREPARED',len(json.loads(baseline.read_text())['protected']),'protected files',flush=True)
