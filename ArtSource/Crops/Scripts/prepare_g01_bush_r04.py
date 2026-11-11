"""Preserve R03 before the requested fuller shrub and species-specific leaf revision."""
import shutil
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Bush_R04';R.mkdir(parents=True,exist_ok=True)
if not (R/'Baseline.json').exists():
 protected={}
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Textures/Shared/Plants','Material/Shared/Plants','Map/Scenes','Script','Data/Balance']:
  for p in (G.A/folder).rglob('*'):
   if p.is_file():protected[U.rel(p)]=U.sha(p)
 for p in [G.MASTER,G.MASTER.parent/'G01_Manifest.json',G.MASTER.parent/'G01_VisualPlan.json',G.SOURCE/'Integration/Manifest.json']+list(G.TEXTURES.glob('*.png'))+list(G.TEXTURES.glob('*.xcf'))+[G.RUNTIME_TEXTURES/(U.NAME+'_'+s+'.png') for s in U.MAPS]:
  dest=R/'Before'/p.relative_to(G.ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 for kind in ['Blueberry','Raspberry','Blackberry']:shutil.copy2(G.SOURCE/'Previews'/('G01_'+kind+'_Ready.png'),R/(kind+'_Before.png'))
 U.write(R/'Baseline.json',dict(protected=protected,manifestBefore=U.load(G.SOURCE/'Integration/Manifest.json'),registryBefore=U.load(U.REGISTRY),masterBefore=U.sha(G.MASTER),sourceMapHashes={U.rel(p):U.sha(p) for p in G.TEXTURES.glob('*.png')}))
print('BUSH_R04_BASELINE_READY',flush=True)
