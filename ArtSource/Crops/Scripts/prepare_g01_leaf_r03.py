"""Snapshot the completed three berries before the requested foliage correction."""
import shutil
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Leaf_R03';R.mkdir(parents=True,exist_ok=True)
if not (R/'Baseline.json').exists():
 protected={}
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Textures/Shared/Plants','Material/Shared/Plants','Map/Scenes','Script']:
  for p in (G.A/folder).rglob('*'):
   if p.is_file():protected[U.rel(p)]=U.sha(p)
 for p in [G.MASTER,G.MASTER.parent/'G01_Manifest.json',G.MASTER.parent/'G01_VisualPlan.json',G.SOURCE/'Integration/Manifest.json',G.SOURCE/'Previews/G01_AppliedPreview.png']:
  target=R/'Before'/p.relative_to(G.ROOT);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
 U.write(R/'Baseline.json',dict(protected=protected,registryBefore=U.load(U.REGISTRY),manifestBefore=U.load(G.SOURCE/'Integration/Manifest.json'),packingBefore=U.load(U.REVIEW/'Packing.json'),masterBefore=U.sha(G.MASTER)))
for kind in ['Blueberry','Raspberry','Blackberry']:
 p=G.SOURCE/'Previews'/('G01_'+kind+'_Ready.png');target=R/'Before'/(kind+'_Ready.png')
 if not target.exists():shutil.copy2(p,target)
print('LEAF_R03_CHECKPOINT_READY',flush=True)
