"""Retain the pre-G07 boundary before any atlas edits or new exports."""
import json,hashlib,shutil
from g07_config import *
for p in [REVIEW,MASTER.parent,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 for folder in [A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Data/Balance',A/'Textures/Shared/Plants',A/'Material/Shared/Plants']:
  for p in folder.rglob('*'):
   if p.is_file() and 'G07' not in p.parts:protected[str(p.relative_to(ROOT))]=digest(p)
 for group in ['G02','G03','G04','G05','G06']:
  for p in (SOURCE/group/'Blender').glob('*.blend'):protected[str(p.relative_to(ROOT))]=digest(p)
 for p in (SOURCE/'Textures').glob('*.xcf'):protected[str(p.relative_to(ROOT))]=digest(p)
 backup=SOURCE/'Revisions/Before_G07'
 for p in [*TEXTURES.glob(ATLAS+'_*'),*(SOURCE/'Textures').glob(ATLAS+'_*.xcf'),TEXTURES/'NP_Plants_Catalog.json']:
  if p.is_file():
   target=backup/p.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
 baseline.write_text(json.dumps(dict(protected=protected,snapshot=str(backup.relative_to(ROOT))),indent=2))
spec=json.loads((TEXTURES/(ATLAS+'_Slots.json')).read_text());used=set()
for group in ['G03','G04','G06']:
 for p in (A/'mesh/Crops'/group).glob('*.json'):
  for u,v in json.loads(p.read_text())['uv']:used.add(spec['slots'][min(7,int((1-v)*8))*8+min(7,int(u*8))]['name'])
assert not used.intersection(CHANGED),used.intersection(CHANGED)
(REVIEW/'ReferenceStudy.json').write_text(json.dumps(dict(source='Two user images attached directly in this conversation, visually inspected before modeling',
 spinach=dict(seedCount=3,phaseLeafCounts=[0,2,4,6,13,8],features=['low dense radial rosette','broad rounded oval leaves with smooth outline','cupped blades and branching veins','fresh dark green with gentle waxy highlights','cut leaf-stalk bunch plus a separate leaf']),
 waterSpinach=dict(seedCount=5,phaseLeafCounts=[0,2,5,16,49,27],features=['taller clump of upright branching stems','visible internodes and swollen node collars','pointed lanceolate leaves','curved leaves spread through the full crown','harvest bunch with real hollow cut ends plus a two-leaf sprig']),
 priorCorrections=['match the distinct silhouette and proportions of each phase','curved leaves with real side depth','vivid source albedo under neutral lighting','registered secondary/tertiary veins','stable atlas UVs and prior group assets','complete unclipped previews'],
 previousUsedTiles=sorted(used),changedPreviouslyUnusedTiles=CHANGED,scope='Prepared visual assets; gameplay integration is separate'),indent=2))
print('G07_PREPARATION_PASS',len(json.loads(baseline.read_text())['protected']),'protected files',flush=True)
