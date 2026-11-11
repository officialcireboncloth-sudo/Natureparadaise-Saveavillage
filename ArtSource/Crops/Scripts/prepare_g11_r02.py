"""Snapshot the delivered Tulip and protect gameplay, IDs and the other 25 species."""
import re,shutil,base64
from g11_integration_config import *
review=ROOT/'ArtSource/Crops/G11/Review';record=review/'R02_Baseline.json';snapshot=ROOT/'ArtSource/Crops/Revisions/Before_G11_R02'
pattern=re.compile(r'^Assets/Nature  Paradaise/(?:mesh/Crops/G11/NP_Tulip_(?:Young|BudForming|Ready|Harvest)(?:_LOD1)?\.(?:json|fbx|asset)|Prefabs/Crops/G11/NP_Tulip_(?:Young|BudForming|Ready|Harvest)\.prefab|Textures/Shared/Plants/NP_Plants_Tulip_01_(?:BaseColor|Normal|SurfaceMask)\.png|Material/Shared/Plants/NP_Plants_Tulip_01\.mat|Resources/Profiles/Farming/Tulip Crop\.asset)$')
if not record.exists():
 targets=[]
 for directory in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/Shared/Plants','Material/Shared/Plants','Textures/UI/Icons','Data/Balance','Resources/Shops','Map/Scenes']:
  targets += [p for p in (A/directory).rglob('*') if p.is_file()]
 for name in ['Core/WorldInteractionVisuals.cs','Core/CollectibleClueRules.cs','Core/WorldInteractionPrompt.cs','Player/Item/HoeTool.cs','World/Field/FieldCropView.cs']:targets.append(A/'Script'/name)
 protected={};editable={}
 for p in targets:
  key=rel(p);(editable if pattern.fullmatch(key) else protected)[key]=digest(p)
 profile=A/'Resources/Profiles/Farming/Tulip Crop.asset'
 record.write_text(json.dumps(dict(protected=protected,editableBefore=editable,profileBefore=profile.read_text(),manifestBefore=load(OUT/'Manifest.json'),artManifestBefore=load(ROOT/'ArtSource/Crops/G11/Blender/G11_Manifest.json'),snapshot=rel(snapshot),scope=['Young','BudForming','Ready','Harvest'],retainedMeshPhases=['Bulb','Sprout']),indent=2))
 for p in [ROOT/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend',ROOT/'ArtSource/Crops/Scripts/g11_geometry.py',ROOT/'ArtSource/Crops/Scripts/paint_g11_gimp.py',profile]+list((ROOT/'ArtSource/Crops/Previews').glob('G11*.png'))+list((ROOT/'ArtSource/Crops/Textures').glob('NP_Plants_Tulip_01_*.xcf')):
  dest=snapshot/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
raw=ROOT/'ArtSource/Crops/Textures/Generated/G11/Tulip_R02_Surfaces.png'
if not raw.exists():raw.write_bytes(base64.b64decode((ROOT/'tmp/G11_R02_TextureSource.b64').read_text()))
print('G11_R02_BASELINE_READY',len(load(record)['protected']),'protected files;',len(load(record)['editableBefore']),'scoped art targets',flush=True)
