"""Keep the delivered G09 baseline for a visible shape-first revision."""
import json,hashlib,shutil
from g09_config import *
record=REVIEW/'R02_Baseline.json';snapshot=SOURCE/'Revisions/Before_G09_R02'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if not record.exists():
 protected={};metas={}
 for folder in [MODELS,A/'Prefabs/Crops/G09',TEXTURES,A/'Material/Shared/Plants',SOURCE/'Textures']:
  for p in folder.rglob('*'):
   if not p.is_file() or (folder==SOURCE/'Textures' and p.suffix!='.xcf'):continue
   if folder in [MODELS,A/'Prefabs/Crops/G09']:
    if p.suffix=='.meta':metas[str(p.relative_to(ROOT))]=sha(p)
    if '_Seed' not in p.name and '_Sprout' not in p.name:continue
   protected[str(p.relative_to(ROOT))]=sha(p)
 targets=[MASTER,SOURCE/'Scripts/g09_geometry.py',MASTER.parent/'G09_Manifest.json',REVIEW/'ArtAudit.json',REVIEW/'FinalReview.json']
 targets.extend((SOURCE/'Previews').glob('G09*.png'))
 for p in targets:
  if p.is_file():
   dest=snapshot/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 record.write_text(json.dumps(dict(snapshot=str(snapshot.relative_to(ROOT)),protected=protected,metas=metas,retainedPhases=['Seed','Sprout'],scope=['Young','BudForming','Ready','Harvest']),indent=2))
print('G09_R02_BASELINE_PASS',len(json.loads(record.read_text())['protected']),'protected files',flush=True)
