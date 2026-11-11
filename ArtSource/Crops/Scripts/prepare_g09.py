"""Retain prior art/gameplay and record the current three flower references."""
import json,hashlib
from g09_config import *
for p in [REVIEW,MASTER.parent,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
record=REVIEW/'Baseline.json'
if not record.exists():
 protected={}
 for folder in [A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Data/Balance',TEXTURES,A/'Material/Shared/Plants',SOURCE/'Textures']:
  for p in folder.rglob('*'):
   if not p.is_file() or 'G09' in p.parts or ATLAS in p.name:continue
   if folder==SOURCE/'Textures' and p.suffix!='.xcf':continue
   protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 for group in ['G02','G03','G04','G05','G06','G07','G08']:
  for p in (SOURCE/group/'Blender').glob('*.blend'):protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 record.write_text(json.dumps(dict(protected=protected,catalog=json.loads((TEXTURES/'NP_Plants_Catalog.json').read_text())),indent=2))
study=dict(source='Three current user attachments, directly inspected in the conversation: Jasmine, Hibiscus, Rose',artDirection='Clear sculpted silhouettes, distinct high-detail textures, vivid healthy colours',
 Rose=dict(seedCount=5,young='upright thorny stem, compound serrated leaves and bronze new tips',buds=3,readyFlowers=3,readyBuds=1,harvestFlowers=1,features=['overlapping spiral red petals','rounded layered corolla with rolled inner petals','compound pointed serrated leaflets','bronze thorns','tapered red buds wrapped in green sepals','one long cut stem with leaves']),
 Hibiscus=dict(seedCount=1,young='upright branched plant with broad pointed serrated leaves',buds=4,readyFlowers=3,readyBuds=3,harvestFlowers=1,features=['five large overlapping red petals','deep funnel throat','prominent red staminal column','yellow anthers along the upper style','five red stigma lobes','long narrow sepals and epicalyx','short cut stem with two broad leaves']),
 Jasmine=dict(seedCount=1,young='seven broad glossy oval opposite leaves',buds=14,readyFlowers=5,readyBuds=10,harvestFlowers=2,harvestBuds=3,features=['low dense radial green shrub','smooth oval cupped leaves','pointed ivory buds','double white cream rosettes','warm pale-yellow flower centre','cut leafy sprig with two flowers and three buds']),
 priorCorrections=['dense radial foliage, not planar cards','physical petal depth and distinct silhouettes','registered leaf veins and petal fibres','vivid albedo under neutral lighting','continuous flower calyx and branch attachment','phase-specific bud/open-flower counts','complete unclipped previews','preserve earlier source and runtime assets'])
(REVIEW/'ReferenceStudy.json').write_text(json.dumps(study,indent=2));print('G09_PREPARATION_PASS',len(json.loads(record.read_text())['protected']),'protected files',flush=True)
