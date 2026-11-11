"""Checkpoint the accepted 27-crop runtime and shared atlas before adding Orchid."""
import shutil
from g13_config import *
from unified_plant_atlas import load,write,sha,rel,REGISTRY,NAME
for p in [REVIEW,MASTER.parent,TEXTURES,GENERATED,MODELS]:p.mkdir(parents=True,exist_ok=True)
baseline=REVIEW/'Baseline.json'
if not baseline.exists():
 protected={}
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Material/Shared/Plants','Textures/Shared/Plants','Map/Scenes','Script']:
  for p in (A/folder).rglob('*'):
   if p.is_file() and 'G13' not in p.parts and p.name!='NP_Plants_Catalog.json' and p.name not in [NAME+'_'+s+'.png' for s in ['BaseColor','Normal','SurfaceMask']] and p.name!='CropGameplayValidation.cs':protected[rel(p)]=sha(p)
 before=dict(protected=protected,manifestBefore=load(SOURCE/'Integration/Manifest.json'),catalogBefore=load(RUNTIME_TEXTURES/'NP_Plants_Catalog.json'),registryBefore=load(REGISTRY),packingBefore=load(SOURCE/'Unified/Review/Packing.json'),iconsBefore=load(SOURCE.parent/'UIIcons/Seeds/Manifest.json'),shopBefore=(A/'Resources/Shops/Crops Catalog.asset').read_text(),editorValidationBefore=(A/'Script/Editor/CropGameplayValidation.cs').read_text(),mutableBefore={rel(p):sha(p) for p in [A/'Resources/Shops/Crops Catalog.asset',A/'Data/Balance/Items.csv',A/'Data/Balance/Crops.csv',A/'Script/Editor/CropGameplayValidation.cs']})
 for suffix in ['BaseColor','Normal','SurfaceMask']:shutil.copy2(RUNTIME_TEXTURES/(NAME+'_'+suffix+'.png'),REVIEW/('AtlasBefore_'+suffix+'.png'))
 for name in ['Items','Crops']:
  p=A/f'Data/Balance/{name}.csv';shutil.copy2(p,REVIEW/(name+'_Before.csv'))
 write(baseline,before)
print('G13_BASELINE_READY',len(load(baseline)['protected']),'protected files',flush=True)
