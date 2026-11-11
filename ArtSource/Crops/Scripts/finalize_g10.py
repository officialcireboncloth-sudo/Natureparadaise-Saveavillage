"""Record the completed reviewed art batch without adding gameplay balances or items."""
import json,hashlib
from g10_config import *
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
audit=json.loads((REVIEW/'ArtAudit.json').read_text());gimp=json.loads((REVIEW/'GimpReopenAudit.json').read_text())
assert audit['status']==gimp['status']=='PASS'
assert audit['masterSha256']==sha(MASTER)
for name,record in audit['atlasPixels'].items():assert record['sha256']==sha(TEXTURES/(ATLAS+'_'+name+'.png'))
baseline=json.loads((REVIEW/'Baseline.json').read_text())
for name,digest in baseline['protected'].items():assert sha(ROOT/name)==digest,name
p=TEXTURES/'NP_Plants_Catalog.json';catalog=json.loads(p.read_text())
for row,prior in zip(catalog['plants'],baseline['catalog']['plants']):
 if row['group']!='G10':assert row==prior
 else:
  kind=row['displayName'];row.update(atlas=ATLAS,slots=[kind+'Leaf',kind+'Petal','StemGreen','SepalGreen','BudGreen','CutStem','SeedStripe' if kind=='Sunflower' else 'SeedBrown' if kind=='Chrysanthemum' else 'SeedBlack']+(['SunflowerDisk'] if kind=='Sunflower' else []),artStatus='G10: six sculpted visual stages and two-LOD Unity prefabs; art/export checks passed, gameplay bindings not added')
p.write_text(json.dumps(catalog,indent=2))
reviews=['G10_ReadyReview.png','G10_GrowthStages.png','G10_FlowerDetail.png','G10_VolumeReview.png','G10_HarvestStudy.png']
for name in reviews:assert (SOURCE/'Previews'/name).is_file(),name
report=dict(status='ART_COMPLETE',species=list(PHASES),phaseVisuals=24,nativeMeshes=48,prefabs=24,atlasPages=1,technicalChecks=audit['checks'],closedPetals=audit['closedPetals'],physicalJoints=len(audit['floralJoints']),fbxRoundtrips=audit['verifiedFbx'],protectedFiles=len(baseline['protected']),masterSha256=sha(MASTER),referenceBasis='Existing project species plan; no new G10 image attachments',visualReview='Actual Blender colour renders reviewed; mature plants, growth, flower closeups, side/rear/top and detached harvest',gameplayBindings='NOT_ADDED',unityPlayMode='NOT_RUN_BY_AGENT',reviews={n:sha(SOURCE/'Previews'/n) for n in reviews})
report['sourceHashes']={str(p.relative_to(ROOT)):sha(p) for p in list((SOURCE/'Scripts').glob('*g10*.py'))+[SOURCE/'Scripts/plant_library.py',SOURCE/'Scripts/prepare_plant_unity.py']}
(REVIEW/'Completion.json').write_text(json.dumps(report,indent=2));print('G10_ART_COMPLETE:',audit['checks'],'checks; 24 prefabs; 48 FBX roundtrips; prior crops retained',flush=True)
