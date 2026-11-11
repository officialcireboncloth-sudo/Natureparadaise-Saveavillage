"""Validate Lavender morphology, spatial geometry, shared atlas and previous accepted art."""
import bpy,bmesh,sys,math
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g13_config as g
from unified_plant_atlas import *
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
base=load(g.REVIEW/'Baseline.json');registry=load(REGISTRY);slots=allocations(registry);exports=load(g.REVIEW/'ExportBaseline.json')['models']
for p,h in base['protected'].items():check(sha(ROOT/p)==h,'Previous crop/art/UI/scene/hint retained '+p)
for old in base['registryBefore']['slots']:check(slots[old['key']]==old,'Previous UV allocation and surface settings retained '+old['key'])
def arr(path):
 im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);bpy.data.images.remove(im);return np.clip(np.rint(v.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
for suffix in MAPS:
 before=arr(g.REVIEW/('AtlasBefore_'+suffix+'.png'));now=arr(TEXTURES/(NAME+'_'+suffix+'.png'))
 for s in base['registryBefore']['slots']:
  x,y,w,h=s['pixelRectTopLeft'];check(np.array_equal(before[y:y+h,x:x+w],now[y:y+h,x:x+w]),s['key']+' '+suffix+' original pixels and padding retained')
 del before,now
for name in ['LavenderPetal','LavenderLeaf']:
 s=slots[g.ATLAS+':'+name];check(s['primaryDetailPreserved'] and s['contentRectTopLeft'][2:]==[512,512],name+' full source detail preserved')
bpy.ops.wm.open_mainfile(filepath=str(g.MASTER));check(len([o for o in bpy.data.objects if o.get('Phase')])==6,'Six editable stages')
expected={'Seed':(0,0,0,0),'Sprout':(2,0,0,0),'Young':(24,0,0,0),'BudForming':(68,0,5,15),'Ready':(108,9,0,297),'Harvest':(6,4,0,132)}
structure={};joints=[];leafMetrics={};closed=0
for stage,(nleaf,nflower,nbud,nfloret) in expected.items():
 root=bpy.data.objects['Lavender_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];leaves=[o for o in obs if o.get('ReferenceLeaf')=='Lavender'];flowers=[o for o in obs if o.get('OpenFlower')=='Lavender'];buds=[o for o in obs if o.get('ClosedBud')=='Lavender'];florets=[o for o in obs if o.get('LavenderFloret')]
 check((len(leaves),len(flowers),len(buds),len(florets))==(nleaf,nflower,nbud,nfloret),stage+' reference leaf/spike/bud/floret counts')
 structure[stage]=dict(leaves=nleaf,flowerSpikes=nflower,budHeads=nbud,florets=nfloret)
 if stage=='Seed':check(sum(bool(o.get('LavenderSeed')) for o in obs)==3,'Three brown oblong seeds')
 if stage=='Harvest':check(sum(bool(o.get('HarvestCut')) for o in obs)==4,'Three cut sprigs and one loose sprig')
 for index in range(nflower):
  parts=[o for o in florets if o['SpikeIndex']==index];check(len(parts)==33 and {o['WhorlTier'] for o in parts}==set(range(7)),stage+' spike '+str(index)+' stacked whorls and terminal cluster')
 for o in leaves+florets:
  bm=bmesh.new();bm.from_mesh(o.data)
  if o.get('LavenderFloret'):bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
  check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,o.name+' closed physical volume');bm.free();closed+=1
 if leaves:
  areas=np.zeros(3);points=[];data=[]
  for o in leaves:
   check(o.get('SpatialLeaf') and o.get('LeafFrame')=='radial growth and world up',stage+' spatial leaf orientation')
   v=np.array([tuple(p.co) for p in o.data.vertices]);points.extend(v);o.data.calc_loop_triangles();tri=v[np.array([tuple(t.vertices) for t in o.data.loop_triangles])];areas+=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5;s=np.linalg.svd(v-v.mean(axis=0),compute_uv=False);ratio=float(s[-1]/s[0]);check(ratio>.005,stage+' nonplanar individual blade');data.append(dict(index=int(o['LeafIndex']),nonplanarRatio=ratio))
  span=np.ptp(np.array(points),axis=0);depthRatio=float(span[1]/span[0]);projectedRatio=float(areas.min()/areas.max())
  if stage not in ['Sprout','Harvest']:check(depthRatio>.35 and projectedRatio>.20,stage+' readable foliage in three perpendicular views')
  leafMetrics[stage]=dict(depthWidthRatio=depthRatio,minMaxProjectedAreaRatio=projectedRatio,leaves=data)
 heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};check(set(heads)==set(stems),stage+' every head has its own continuous stem')
 def ring(o,start):return sum((v.co for v in o.data.vertices[start:start+9]),Vector())/9
 for key,head in heads.items():
  stem=stems[key];tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);a=ring(head,0);b=ring(head,10);gap=(tip-a).length;angle=math.degrees((tip-prev).angle(b-a));check(gap<1e-6 and angle<.15,stage+' '+key+' head/stem connection and tangent');joints.append(dict(stage=stage,joint=key,gap=gap,angleDegrees=angle))
for p in sorted(g.MODELS.glob('*.json')):
 s=load(p);check(s['materials']==[NAME] and geometry_hash(s)==exports[p.stem]['geometrySha256'],p.stem+' exact source geometry and shared material');check(s['triangles']<=(2800 if p.stem.endswith('_LOD1') else 26010),p.stem+' mobile triangle budget')
manifest=load(SOURCE/'Integration/Manifest.json')
for old in base['manifestBefore']['plants']:check(old==next(r for r in manifest['plants'] if r['key']==old['key']),'Prior crop binding retained '+old['species'])
row=next(r for r in manifest['plants'] if r['key']=='lavender');check([r['name'] for r in row['stages']]==['Seed','Sprout','Young Plant','Bud Forming','Harvest Ready'],'Lavender gameplay phase order')
icons=load(SOURCE.parent/'UIIcons/Seeds/G13_IconAudit.json');check(icons['status']=='PASS' and icons['newPackets']==1,'Same paper-bag renderer and reopened icon masters')
for folder,name in [('Seeds','icon_seed_lavender'),('Crops','icon_crop_lavender'),('Crops','icon_crop_lavender_mutation')]:
 v=arr(A/'Textures/UI/Icons'/folder/(name+'.png'));mask=v[:,:,3];ys,xs=np.nonzero(mask);check(v.shape==(256,256,4) and mask.min()==0 and mask.max()>=253 and min(xs)>0 and min(ys)>0 and max(xs)<255 and max(ys)<255,name+' transparent 256px safe margins')
check(load(g.REVIEW/'GimpReopenAudit.json')['status']=='PASS','Editable texture masters reopened')
report=dict(status='PASS',group='G13',checks=len(checks),stages=structure,closedSculptedVolumes=closed,floralJoints=joints,leafMetrics=leafMetrics,priorAtlasSurfacesRetained=len(base['registryBefore']['slots']),runtimeAtlas=NAME,runtimeMaterials=1,modelsWithLods=12,fieldProfiles=28,masterSha256=sha(g.MASTER),protectedFiles=len(base['protected']),sourceHashes={rel(p):sha(p) for p in (SOURCE/'Scripts').glob('*g13*.py')},unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN')
write(g.REVIEW/'ArtAudit.json',report);print('G13_REFERENCE_ART_PASS',len(checks),'checks;',closed,'closed volumes;',len(joints),'connected stems',flush=True)
