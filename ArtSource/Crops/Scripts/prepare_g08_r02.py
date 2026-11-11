"""Preserve the reviewed G08 state before the wheat attachment correction."""
import json,hashlib,shutil
from g08_config import *
record=REVIEW/'R02_Baseline.json';snapshot=SOURCE/'Revisions/Before_G08_R02'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if not record.exists():
 protected={}
 for folder in [MODELS,A/'Prefabs/Crops/G08',TEXTURES,A/'Material/Shared/Plants',SOURCE/'Textures']:
  for p in folder.rglob('*'):
   if not p.is_file() or (folder==SOURCE/'Textures' and p.suffix!='.xcf'):continue
   if p.name.startswith(('NP_Wheat_Heading','NP_Wheat_Harvest')):continue
   protected[str(p.relative_to(ROOT))]=sha(p)
 targets=[MASTER,MASTER.parent/'G08_Manifest.json',SOURCE/'Scripts/g08_geometry.py',REVIEW/'ArtAudit.json',REVIEW/'FinalReview.json']
 targets.extend(p for p in MODELS.glob('NP_Wheat_*') if p.name.startswith(('NP_Wheat_Heading','NP_Wheat_Harvest')))
 targets.extend((SOURCE/'Previews').glob('G08*.png'))
 for p in targets:
  if p.is_file():
   dest=snapshot/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 record.write_text(json.dumps(dict(snapshot=str(snapshot.relative_to(ROOT)),protected=protected,scope=['Wheat_Heading','Wheat_Harvest'],targetMeta={str(p.relative_to(ROOT)):sha(p) for p in MODELS.glob('*.meta') if p.name.startswith(('NP_Wheat_Heading','NP_Wheat_Harvest'))}),indent=2))
print('G08_R02_BASELINE_PASS',len(json.loads(record.read_text())['protected']),'protected files',flush=True)
