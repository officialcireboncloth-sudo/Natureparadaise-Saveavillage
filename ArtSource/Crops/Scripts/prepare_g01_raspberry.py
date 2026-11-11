"""Append-only checkpoint for the supplied Raspberry reference, after the two berries."""
import shutil
from g01_config import *
from unified_plant_atlas import load,write,sha,rel,REGISTRY,NAME,MAPS
R=REVIEW/'Raspberry';R.mkdir(parents=True,exist_ok=True)
checkpoint=R/'Baseline.json'
if not checkpoint.exists():
 protected={}
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Material/Shared/Plants','Map/Scenes','Script']:
  for p in (A/folder).rglob('*'):
   if p.is_file():protected[rel(p)]=sha(p)
 for suffix in MAPS:shutil.copy2(RUNTIME_TEXTURES/(NAME+'_'+suffix+'.png'),R/('AtlasBefore_'+suffix+'.png'))
 shutil.copy2(MASTER,R/'G01_Before_Raspberry.blend')
 write(checkpoint,dict(protected=protected,registryBefore=load(REGISTRY),manifestBefore=load(SOURCE/'Integration/Manifest.json'),catalogBefore=load(RUNTIME_TEXTURES/'NP_Plants_Catalog.json'),iconsBefore=load(SOURCE.parent/'UIIcons/Seeds/Manifest.json'),shopBefore=(A/'Resources/Shops/Crops Catalog.asset').read_text(),csvBefore={n:(A/f'Data/Balance/{n}.csv').read_text(encoding='utf-8-sig') for n in ['Items','Crops']},masterBefore=sha(MASTER),mutableBefore={rel(p):sha(p) for p in [A/'Resources/Shops/Crops Catalog.asset',A/'Data/Balance/Items.csv',A/'Data/Balance/Crops.csv',A/'Script/Editor/CropGameplayValidation.cs']}))
write(R/'ReferenceStudy.json',dict(species='Raspberry',reference='User supplied Raspberry five growth stages and hollow detached harvest on 2026-11-11',morphology='four tan seeds; three serrated sprout leaves; branching red-brown canes; spatial compound leaves; white five-petal flowers and green immature aggregates; seven pink-red aggregate fruits; two whole harvested fruits and a hollow front fruit',runtimeAtlas=NAME,previousCrops=30,policy='Keep both earlier berries, all prior atlas rectangles and allocated pixels intact'))
print('RASPBERRY_CHECKPOINT_READY',len(load(checkpoint)['protected']),flush=True)
