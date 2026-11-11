"""Bind the reviewed G09 renders and editable sources to the audited model version."""
from g09_config import *
import json,hashlib,struct,base64,xml.etree.ElementTree as ET
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
audit=json.loads((REVIEW/'ArtAudit.json').read_text());gimp=json.loads((REVIEW/'GimpReopenAudit.json').read_text());assert audit['status']==gimp['status']=='PASS';assert digest(MASTER)==audit['masterSha256']
for suffix,entry in audit['atlasPixels'].items():assert digest(TEXTURES/(ATLAS+'_'+suffix+'.png'))==entry['sha256']
for entry in gimp['files']:assert digest(ROOT/entry['file'])==entry['sha256']
previews={}
for name in ['G09_Closeup.png','G09_GrowthStages.png','G09_VolumeReview.png','G09_HarvestStudy.png','G09_FlowerDetail.png','G09_SurfaceDetail.png']+(['G09_R02_Comparison.png','G09_R02_FlowerComparison.png'] if (REVIEW/'R02_FormAudit.json').is_file() else []):
 p=SOURCE/'Previews'/name;data=p.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';previews[name]=dict(size=list(struct.unpack('>II',data[16:24])),sha256=digest(p))
assert len(list((SOURCE/'Previews').glob('G09_*_Phase.png')))==18
revision=json.loads((MASTER.parent/'G09_Manifest.json').read_text())['revision']
report=dict(status='PASS',group='G09',revision=revision,baseModels=18,assetChecks=audit['checks'],fbxRoundtrips=audit['verifiedFbx'],floralJoints=len(audit['floralJoints']),masterSha256=audit['masterSha256'],atlas=ATLAS,previousAssetsRetained=audit['previousAssetsRetained'],olderFilesChecked=audit['protectedFiles'],gimpSources=len(gimp['files']),visualReview=['eighteen growth and harvest appearances','front side rear top with radial foliage','rolled spiral rose petals and bronze thorns','five cupped red hibiscus petals with yellow anthers and five red stigma pads','double ivory jasmine rosettes and pointed buds','complete cut-flower stems and aligned calyces','registered petal fibres and branching leaf veins'],previews=previews,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual visual comparison with user references; technical checks do not establish artistic similarity.')
if (REVIEW/'R02_FormAudit.json').is_file():
 form=json.loads((REVIEW/'R02_FormAudit.json').read_text());assert form['status']=='PASS' and form['masterSha256']==digest(MASTER);report['formRevision']=form;report['visualReview'].append('R02 closed fleshy petals, rounded outlines, varied branch skeletons and before/after renders')
 baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());before=ROOT/baseline['snapshot']/SOURCE.relative_to(ROOT)/'Previews';current=SOURCE/'Previews';matched=0
 for title,suffix in [('Comparison','Ready'),('FlowerComparison','FlowerDetail')]:
  svg=ET.fromstring((current/('G09_R02_'+title+'.svg')).read_text());images=svg.findall('{http://www.w3.org/2000/svg}image');expected=[folder/('G09_'+kind+'_'+suffix+'.png') for kind in PHASES for folder in [before,current]];assert len(images)==len(expected)==6
  for node,p in zip(images,expected):
   embedded=base64.b64decode(node.attrib['{http://www.w3.org/1999/xlink}href'].split(',',1)[1]);assert hashlib.sha256(embedded).hexdigest()==digest(p),str(p)+' stale comparison input';matched+=1
 report['comparisonSourceImagesVerified']=matched
(REVIEW/'FinalReview.json').write_text(json.dumps(report,indent=2)+'\n');print('G09_FINAL_REVIEW_PASS',len(previews),'previews;',audit['verifiedFbx'],'FBX roundtrips',flush=True)
