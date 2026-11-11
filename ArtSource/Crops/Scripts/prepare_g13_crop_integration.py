"""Preserve the current 27-crop setup, map scenes, art and interaction code."""
import base64,shutil
from g13_integration_config import *
if not BASELINE.exists():
 old=load(OUT/'Manifest.json');assert len(old['plants'])==27
 protected={}
 for directory in ['mesh/Crops','Prefabs/Crops','Textures/Shared/Plants','Material/Shared/Plants','Textures/UI/Icons/Seeds','Textures/UI/Icons/Crops','Map/Scenes']:
  for p in (A/directory).rglob('*'):
   if p.is_file():protected[rel(p)]=digest(p)
 for row in old['plants']:
  for key in ['profile','seed','produce','mutation']:
   p=ROOT/row[key];protected[rel(p)]=digest(p);protected[rel(Path(str(p)+'.meta'))]=digest(Path(str(p)+'.meta'))
 for name in ['Core/WorldInteractionVisuals.cs','Core/CollectibleClueRules.cs','Core/WorldInteractionPrompt.cs','Player/Item/HoeTool.cs','World/Field/FieldCropView.cs']:
  p=A/'Script'/name;protected[rel(p)]=digest(p)
 beforeRows={}
 for p,key in [(A/'Data/Balance/Items.csv','item_id'),(A/'Data/Balance/Crops.csv','crop_id')]:
  _,rows=csvread(p);beforeRows[p.name]={r[key]:r for r in rows}
 BASELINE.write_text(json.dumps(dict(protected=protected,plantsBefore=27,manifestBefore=old,csvRows=beforeRows,shopTextBefore=(A/'Resources/Shops/Crops Catalog.asset').read_text(),iconsBefore=load(ART/'Manifest.json')),indent=2))
 for p in [A/'Resources/Shops/Crops Catalog.asset',A/'Data/Balance/Items.csv',A/'Data/Balance/Crops.csv',OUT/'Manifest.json',ART/'Manifest.json']:
  target=OUT/'Before_G13'/p.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
data=load(BASELINE)
if 'catalogBefore' not in data:
 p=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';assert digest(p)==data['protected'][rel(p)]
 data['catalogBefore']=load(p);BASELINE.write_text(json.dumps(data,indent=2))
raw=ART/'Generated/G13_Illustrations.png';raw.parent.mkdir(parents=True,exist_ok=True)
if not raw.exists():raw.write_bytes(base64.b64decode((ROOT/'tmp/G13_IconSource.b64').read_text()))
print('G13_GAMEPLAY_BASELINE_PASS',len(load(BASELINE)['protected']),'protected files',flush=True)
