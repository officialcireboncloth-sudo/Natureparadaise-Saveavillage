"""Checkpoint 28 accepted crops and decode the new generated source images."""
import shutil,base64
from g01_config import *
from unified_plant_atlas import load,write,sha,rel,REGISTRY,NAME
for p in [REVIEW,MASTER.parent,TEXTURES,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Material/Shared/Plants','Textures/Shared/Plants','Map/Scenes','Script']:
  for p in (A/folder).rglob('*'):
   if p.is_file() and 'G01' not in p.parts and p.name!='NP_Plants_Catalog.json' and p.name not in [NAME+'_'+s+'.png' for s in ['BaseColor','Normal','SurfaceMask']] and p.name!='CropGameplayValidation.cs':protected[rel(p)]=sha(p)
 before=dict(protected=protected,manifestBefore=load(SOURCE/'Integration/Manifest.json'),catalogBefore=load(RUNTIME_TEXTURES/'NP_Plants_Catalog.json'),registryBefore=load(REGISTRY),packingBefore=load(SOURCE/'Unified/Review/Packing.json'),iconsBefore=load(SOURCE.parent/'UIIcons/Seeds/Manifest.json'),shopBefore=(A/'Resources/Shops/Crops Catalog.asset').read_text(),editorValidationBefore=(A/'Script/Editor/CropGameplayValidation.cs').read_text(),mutableBefore={rel(p):sha(p) for p in [A/'Resources/Shops/Crops Catalog.asset',A/'Data/Balance/Items.csv',A/'Data/Balance/Crops.csv',A/'Script/Editor/CropGameplayValidation.cs']})
 assert len(before['manifestBefore']['plants'])==28
 for suffix in ['BaseColor','Normal','SurfaceMask']:shutil.copy2(RUNTIME_TEXTURES/(NAME+'_'+suffix+'.png'),REVIEW/('AtlasBefore_'+suffix+'.png'))
 for name in ['Items','Crops']:shutil.copy2(A/f'Data/Balance/{name}.csv',REVIEW/(name+'_Before.csv'))
 write(baseline,before)
for src,dst in [('tmp/G01_TextureSource.b64',GENERATED/'Berries_Source_Sheet.png'),('tmp/G01_IconSource.b64',SOURCE.parent/'UIIcons/Seeds/Generated/G01_Illustrations.png')]:
 if not dst.exists():dst.write_bytes(base64.b64decode((ROOT/src).read_text()))
write(REVIEW/'ReferenceStudy.json',dict(species=['Blueberry','Blackberry'],pendingReference=['Raspberry'],duplicates='The second user attachment repeats Blueberry',blueberry='4 golden seeds, 2 oval sprout leaves, young woody bush, 4 cream hanging bells and 3 green berries, 7 cobalt berries with recessed 5-point calyx crowns; 3 detached harvest fruits',blackberry='4 golden seeds, serrated sprout leaves, red woody canes and thorns, 4 five-petal flowers and green/red young fruit, 7 ripe black-purple aggregates and 1 red fruit; two whole harvest fruits and one cream-core cross section',runtime='Append to NP_Plants_Unified_01 without changing the existing 134 slots or their pixels'))
print('G01_BASELINE_READY',len(load(baseline)['protected']),'protected files',flush=True)
