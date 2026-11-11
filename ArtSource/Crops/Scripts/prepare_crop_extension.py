"""Snapshot live integration before appending the next nine crops."""
from crop_extension_config import *
import shutil
OUT.mkdir(parents=True,exist_ok=True);record=OUT/'G06_G09_Baseline.json'
if not record.exists():
 protected={};old=load(OUT/'Manifest.json');snap=OUT/'Before_G06_G09'
 paths=[A/'Resources/Shops/Crops Catalog.asset',A/'Data/Balance/Items.csv',A/'Data/Balance/Crops.csv',OUT/'Manifest.json',ART/'Manifest.json']
 for p in paths:
  dest=snap/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 for folder in ['mesh/Crops','Prefabs/Crops','Textures/Shared/Plants','Material/Shared/Plants','Textures/UI/Icons/Seeds','Textures/UI/Icons/Crops','Map/Scenes']:
  for p in (A/folder).rglob('*'):
   if p.is_file():protected[rel(p)]=digest(p)
 for row in old['plants']:
  for key in ['profile','seed','produce','mutation']:
   p=ROOT/row[key];protected[rel(p)]=digest(p);protected[rel(Path(str(p)+'.meta'))]=digest(Path(str(p)+'.meta'))
 beforeRows={}
 for p,key in [(A/'Data/Balance/Items.csv','item_id'),(A/'Data/Balance/Crops.csv','crop_id')]:
  _,rows=csvread(p);beforeRows[p.name]={r[key]:r for r in rows}
 record.write_text(json.dumps(dict(protected=protected,snapshot=rel(snap),plantsBefore=len(old['plants']),csvRows=beforeRows,shopTextBefore=(A/'Resources/Shops/Crops Catalog.asset').read_text()),indent=2))
data=load(record)
if 'shopTextBefore' not in data:
 data['shopTextBefore']=(ROOT/data['snapshot']/'Assets/Nature  Paradaise/Resources/Shops/Crops Catalog.asset').read_text();record.write_text(json.dumps(data,indent=2))
if 'reusedItemBefore' not in data:
 p=A/'Resources/Items/Materials/Wheat.asset';data['reusedItemBefore']={rel(p):dict(text=p.read_text(),metaSha256=digest(Path(str(p)+'.meta')))};record.write_text(json.dumps(data,indent=2))
print('CROP_EXTENSION_BASELINE_PASS',len(load(record)['protected']),'protected files',flush=True)
