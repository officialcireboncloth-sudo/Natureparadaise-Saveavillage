"""Finalize only the new G11 art after internal review; prior 25 assets stay fixed."""
from g11_integration_config import *
audit=load(ROOT/'ArtSource/Crops/G11/Review/ArtAudit.json');assert audit['status']=='PASS'
assert audit['masterSha256']==digest(ROOT/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend')
data=load(BASELINE);changes={}
for path,sha in data['protected'].items():
 actual=digest(ROOT/path)
 if actual==sha:continue
 assert path.startswith(('Assets/Nature  Paradaise/mesh/Crops/G11/','Assets/Nature  Paradaise/Prefabs/Crops/G11/')),path
 changes[path]=dict(before=sha,after=actual,reason='New G11 geometry finalized after reference review; the preceding 25 species are immutable')
 data['protected'][path]=actual
data['newArtReviewChanges']=changes;data['reviewedNewArtMasterSha256']=audit['masterSha256'];BASELINE.write_text(json.dumps(data,indent=2));print('G11_NEW_ART_REVIEW_RECORDED',len(changes),'new G11 files; prior species retained',flush=True)
