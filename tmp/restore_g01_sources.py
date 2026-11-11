import sys,shutil
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'ArtSource/Crops/Scripts'))
import g01_config as g
from unified_plant_atlas import load,geometry_hash
src=g.REVIEW/'SourceExports';models=sorted(g.MODELS.glob('NP_Blackberry_*.json'));assert len(models)==12
for p in models:
 original=load(src/p.name);assert original['materials']==[g.ATLAS];assert geometry_hash(original)==geometry_hash(load(p)),p.name
for p in models:
 for name in [p.name,p.with_suffix('.fbx').name]:shutil.copy2(src/name,g.MODELS/name)
print('G01_BLACKBERRY_SOURCE_UVS_RESTORED; 12 current geometries verified')
