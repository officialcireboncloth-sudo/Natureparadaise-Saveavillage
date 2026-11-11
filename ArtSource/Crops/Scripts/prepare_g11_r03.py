"""Preserve the delivered R02 before repairing leaf orientation in actual 3D space."""
import json,shutil
from g11_integration_config import *
review=ROOT/'ArtSource/Crops/G11/Review';record=review/'R03_Baseline.json';snapshot=ROOT/'ArtSource/Crops/Revisions/Before_G11_R03'
if not record.exists():
 paths=[]
 for directory in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/Shared/Plants','Material/Shared/Plants','Textures/UI/Icons','Data/Balance','Resources/Shops','Map/Scenes']:
  paths += [p for p in (A/directory).rglob('*') if p.is_file()]
 for name in ['Core/WorldInteractionVisuals.cs','Core/CollectibleClueRules.cs','Core/WorldInteractionPrompt.cs','Player/Item/HoeTool.cs','World/Field/FieldCropView.cs']:paths.append(A/'Script'/name)
 allowed=lambda key: (key.startswith('Assets/Nature  Paradaise/mesh/Crops/G11/NP_Tulip_') and not key.endswith('.meta') and '_Bulb' not in key) or (key.startswith('Assets/Nature  Paradaise/Prefabs/Crops/G11/NP_Tulip_') and key.endswith('.prefab') and '_Bulb' not in key) or key=='Assets/Nature  Paradaise/Resources/Profiles/Farming/Tulip Crop.asset'
 protected={};editable={}
 for p in paths:(editable if allowed(rel(p)) else protected)[rel(p)]=digest(p)
 oldReport=load(review/'R02_AssetChanges.json')
 record.write_text(json.dumps(dict(protected=protected,editableBefore=editable,profileBefore=(A/'Resources/Profiles/Farming/Tulip Crop.asset').read_text(),manifestBefore=load(OUT/'Manifest.json'),previousRevision=oldReport,snapshot=rel(snapshot)),indent=2))
 for p in [ROOT/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend',ROOT/'ArtSource/Crops/Scripts/g11_r02_geometry.py',A/'Resources/Profiles/Farming/Tulip Crop.asset']+list((ROOT/'ArtSource/Crops/Previews').glob('G11*.png')):
  target=snapshot/p.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
print('G11_R03_BASELINE_READY',len(load(record)['protected']),'protected files',flush=True)
