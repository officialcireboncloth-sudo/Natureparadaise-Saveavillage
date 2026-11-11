"""Preserve R01 and all previous groups before the G07 visual correction."""
import json,hashlib,shutil,base64
from pathlib import Path
from plant_library import ROOT,A,SOURCE,TEXTURES
REVIEW=SOURCE/'G07/Review';GEN=SOURCE/'Textures/Generated/G07_R02';GEN.mkdir(parents=True,exist_ok=True)
record=REVIEW/'R02_Baseline.json';backup=SOURCE/'Revisions/Before_G07_R02'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if not record.exists():
 protected={}
 for folder in [A/'mesh/Crops',A/'Prefabs/Crops',A/'Resources/Profiles/Farming',A/'Resources/Items',A/'Data/Balance',A/'Textures/Shared/Plants',A/'Material/Shared/Plants']:
  for p in folder.rglob('*'):
   if p.is_file() and 'G07' not in p.parts:protected[str(p.relative_to(ROOT))]=sha(p)
 for group in ['G02','G03','G04','G05','G06']:
  for p in (SOURCE/group/'Blender').glob('*.blend'):protected[str(p.relative_to(ROOT))]=sha(p)
 targets=[]
 for folder in [SOURCE/'G07',A/'mesh/Crops/G07',A/'Prefabs/Crops/G07']:
  targets.extend(p for p in folder.rglob('*') if p.is_file() and p.suffix not in ['.log','.blend1','.blend2'])
 targets.extend((SOURCE/'Previews').glob('G07*'))
 targets.extend((SOURCE/'Textures').glob('NP_Plants_Trellis_01_*.xcf'))
 targets.extend(TEXTURES.glob('NP_Plants_Trellis_01_*'))
 for p in targets:
  dest=backup/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 record.write_text(json.dumps(dict(snapshot=str(backup.relative_to(ROOT)),protected=protected,originalG07Meta={str(p.relative_to(ROOT)):sha(p) for folder in [A/'mesh/Crops/G07',A/'Prefabs/Crops/G07'] for p in folder.glob('*.meta')}),indent=2))
source=GEN/'Leaf_Source_Sheet.png'
if not source.exists():source.write_bytes(base64.b64decode((ROOT/'tmp/G07_R02_TextureSource.b64').read_text()))
(GEN/'SourceProvenance.json').write_text(json.dumps(dict(source='Generated two-leaf texture sheet for the requested G07 visual revision',file=source.name,sha256=sha(source),purpose='Registered leaf albedo, silhouette contour and surface relief in the editable Blender/GIMP deliverable'),indent=2))
print('G07_R02_BASELINE_PASS',len(json.loads(record.read_text())['protected']),'protected files',flush=True)
