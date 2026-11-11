"""Bind the visually reviewed delivery to current audited production files."""
from g08_config import *
import json,hashlib,struct
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
audit=json.loads((REVIEW/'ArtAudit.json').read_text());gimp=json.loads((REVIEW/'GimpReopenAudit.json').read_text())
assert audit['status']==gimp['status']=='PASS'
assert digest(MASTER)==audit['masterSha256']
for suffix,entry in audit['atlasPixels'].items():assert digest(TEXTURES/(ATLAS+'_'+suffix+'.png'))==entry['sha256']
for entry in gimp['files']:assert digest(ROOT/entry['file'])==entry['sha256']
previews={}
for name in ['G08_Closeup.png','G08_ReadyReview.png','G08_GrowthStages.png','G08_VolumeReview.png','G08_HarvestStudy.png','G08_SurfaceDetail.png','G08_GrainDetail.png']+([p.name for p in (SOURCE/'Previews').glob('G08_Wheat_R02_*.png') if p.name.endswith(('Comparison.png','Alignment.png'))] if (REVIEW/'R02_AlignmentAudit.json').is_file() else []):
 p=SOURCE/'Previews'/name;data=p.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';previews[name]=dict(size=list(struct.unpack('>II',data[16:24])),sha256=digest(p))
assert len(list((SOURCE/'Previews').glob('G08_*_Phase.png')))==12
revision=json.loads((MASTER.parent/'G08_Manifest.json').read_text())['revision']
report=dict(status='PASS',group='G08',revision=revision,assetChecks=audit['checks'],fbxRoundtrips=audit['verifiedFbx'],baseModels=audit['baseModels'],gimpSources=len(gimp['files']),masterSha256=audit['masterSha256'],atlas=ATLAS,surfaceResolution=512,previousAssetsRetained=audit['previousAssetsRetained'],olderFilesChecked=audit['protectedFiles'],orphanVerticesRemoved=True,visualReview=['five growth phases plus cut harvest for each cereal','radial foliage in front side rear and top views','green heading and vivid golden maturity','individual ridged rice husks and grooved wheat kernels','branched drooping rice panicles and compact upright wheat ears with awns','low diagonal harvest bundles and three separate grain piles','registered colour and normal-map fibre detail'],previews=previews,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual visual review against the supplied attachments; technical checks are not an artistic-fidelity score.')
if (REVIEW/'R02_AlignmentAudit.json').is_file():
 alignment=json.loads((REVIEW/'R02_AlignmentAudit.json').read_text());assert alignment['status']=='PASS' and alignment['masterSha256']==digest(MASTER);report['wheatAlignment']=alignment;report['visualReview'].append('R02 Heading and Harvest joint continuity, tapered neck/apex and three-angle renders')
(REVIEW/'FinalReview.json').write_text(json.dumps(report,indent=2)+'\n');print('G08_FINAL_REVIEW_PASS',len(previews),'previews;',audit['verifiedFbx'],'FBX roundtrips',flush=True)
