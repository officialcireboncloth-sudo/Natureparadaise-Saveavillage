"""Capture existing art and gameplay boundaries before writing G08."""
import json,hashlib
from g08_config import *
for p in [REVIEW,MASTER.parent,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 folders=[A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Data/Balance',TEXTURES,A/'Material/Shared/Plants',SOURCE/'Textures']
 for folder in folders:
  for p in folder.rglob('*'):
   if not p.is_file() or 'G08' in p.parts or ATLAS in p.name:continue
   if folder==SOURCE/'Textures' and p.suffix!='.xcf':continue
   protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 for group in ['G02','G03','G04','G05','G06','G07']:
  for p in (SOURCE/group/'Blender').glob('*.blend'):protected[str(p.relative_to(ROOT))]=hashlib.sha256(p.read_bytes()).hexdigest()
 baseline.write_text(json.dumps(dict(protected=protected,catalog=json.loads((TEXTURES/'NP_Plants_Catalog.json').read_text())),indent=2))
study=dict(source='Two current user attachments, visually inspected in the conversation',artDirection='Distinct sculpted forms, visible fine surface detail, saturated fresh greens and warm golds; neutral studio review',
 rice=dict(seeds=3,phases=['elongated ridged husks','two narrow green leaves attached to one seed','dense green tillering clump without heads','green branched panicles','five heavy golden drooping panicles with mixed green and gold leaves','three cut panicles plus separate husk and polished-rice piles'],features=['narrow pleated strap leaves','thin branching panicle rachis','individual pointed rice husks on fine pedicels','longitudinal raised husk ridges','panicles bend downward with ripening']),
 wheat=dict(seeds=5,phases=['plump kernels with central groove','two strap leaves attached to one kernel','green leafy clump without heads','three green upright ears','six golden upright alternating spikelet ears with fine awns','three cut ears and a separate kernel pile'],features=['compact alternating spikelets','plump kernels with central seam','short fine awns','curved strap foliage','golden culms and mixed golden green lower leaves']),
 priorCorrections=['radial volume in front side rear and top views','crisp high-resolution atlas details','sculpted ridges and leaf folds','retain vivid greens and golds under neutral light','render every stage and harvest','preserve all earlier art and gameplay assets'])
(REVIEW/'ReferenceStudy.json').write_text(json.dumps(study,indent=2))
print('G08_PREPARATION_PASS',len(json.loads(baseline.read_text())['protected']),'protected files',flush=True)
