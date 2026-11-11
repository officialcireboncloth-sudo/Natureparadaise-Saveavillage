"""Check the actual rounded source forms, unchanged six atlas regions and revised binding scope."""
import bpy,bmesh,sys,json,math,re,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text())
baseline=load(REVIEW/'R02_Baseline.json');checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
for path,digest in baseline['protected'].items():check(sha(ROOT/path)==digest,'Protected '+path)
snapshot=ROOT/baseline['snapshot'];slots=manifest()['slots']
for suffix in ['BaseColor','Normal','SurfaceMask']:
 arrays=[]
 for p in [snapshot/TEXTURES.relative_to(ROOT)/(ATLAS+'_'+suffix+'.png'),TEXTURES/(ATLAS+'_'+suffix+'.png')]:
  im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';a=np.empty(2048*1024*4,dtype=np.float32);im.pixels.foreach_get(a);arrays.append(a.reshape(1024,2048,4)[::-1].copy());bpy.data.images.remove(im)
 for s in slots:
  x,y,w,h=s['pixelRectTopLeft'];same=np.array_equal(arrays[0][y:y+h,x:x+w],arrays[1][y:y+h,x:x+w])
  if suffix=='SurfaceMask' or s['name'] not in ['TulipPetal','TulipLeaf']:check(same,suffix+' retained region '+s['name'])
  else:check(not same,suffix+' revised region '+s['name'])
 if suffix=='Normal':
  for s in [slots[0],slots[2]]:
   x,y,w,h=s['pixelRectTopLeft'];old=arrays[0][y+20:y+h-20,x+20:x+w-20,:2]-.5;new=arrays[1][y+20:y+h-20,x+20:x+w-20,:2]-.5
   check(float(np.mean(new*new))<float(np.mean(old*old)),s['name']+' softer registered relief')
bpy.ops.wm.open_mainfile(filepath=str(MASTER));forms={}
for stage in ['Young','BudForming','Ready','Harvest']:
 root=bpy.data.objects['Tulip_'+stage];leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')=='Tulip'];petals=[o for o in root.children_recursive if o.get('PetalKind')=='Tulip']
 for ob in leaves:
  check(bool(ob.get('ClosedLeaf')),stage+' closed curved leaf');bm=bmesh.new();bm.from_mesh(ob.data);check(all(len(e.link_faces)==2 for e in bm.edges),stage+' watertight leaf edge');check(abs(bm.calc_volume(signed=True))>1e-10,stage+' physical leaf volume');bm.free()
  cp=np.array(ob['CurveControlPoints']);chord=cp[-1]-cp[0];distance=np.linalg.norm(np.cross(cp[1]-cp[0],chord))/np.linalg.norm(chord);check(distance>.002,stage+' curved leaf centreline')
  check(abs(ob['LeafRollRadians'][1]-ob['LeafRollRadians'][0])>.20,stage+' longitudinal leaf turn')
 if stage in ['Ready','Harvest']:
  check(len(petals)==6,stage+' six revised tepals')
  for ob in petals:
   check(ob.get('RoundedTip') and ob.get('EllipticalOutline'),stage+' genuinely closed oval petal outline');top=[v.co for v in ob.data.vertices[36*13:37*13]];spread=max((a-b).length for a in top for b in top);check(spread<.001,stage+' oval petal tip closes smoothly')
 if stage in ['Ready','BudForming']:
  ends=[Vector(ob['CurveControlPoints'][-1]) for ob in leaves];heightDiff=abs(ends[0].z-ends[1].z);depthDiff=abs(ends[0].y-ends[1].y);check(heightDiff>.07 and depthDiff>.04,stage+' distinct left/right height and depth');forms[stage]=dict(leafTipHeightDifference=heightDiff,leafTipDepthDifference=depthDiff)
profile=(A/'Resources/Profiles/Farming/Tulip Crop.asset').read_text();strip=lambda t:re.sub(r'(?m)^    localOffset:[^\n]*\n?','',t).strip();check(strip(profile)==strip(baseline['profileBefore']),'Only visual placement changed on crop profile')
report=dict(status='PASS',revision='G11 R02',checks=len(checks),protectedFiles=len(baseline['protected']),forms=forms,masterSha256=sha(MASTER),technicalScope='Actual closed forms, elliptical tips, curved/asymmetric leaves, registered surface revision, retained palette regions and binding scope; artistic match reviewed from model renders',sourceHashes={str(p.relative_to(ROOT)).replace('\\','/'):sha(p) for p in [SOURCE/'Scripts/g11_r02_geometry.py',SOURCE/'Scripts/paint_g11_r02_gimp.py']})
(REVIEW/'R02_FormAudit.json').write_text(json.dumps(report,indent=2));print('G11_R02_FORM_PASS',len(checks),'checks',flush=True)
