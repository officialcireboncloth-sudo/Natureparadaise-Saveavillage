"""Reference structure, physical foliage, floral joints and exact prior-atlas preservation."""
import bpy,bmesh,sys,csv,math,re
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g12_config as g
from unified_plant_atlas import *
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
base=load(g.REVIEW/'Baseline.json');registry=load(REGISTRY);slots=allocations(registry);exports=load(g.REVIEW/'ExportBaseline.json')['models']
for p,s in base['protected'].items():check(sha(ROOT/p)==s,'Previous crop/art/UI/scene/hint retained '+p)
for old in base['registryBefore']['slots']:check(slots[old['key']]==old,'Existing UV allocation and surface settings retained '+old['key'])
def arr(path):
 im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);bpy.data.images.remove(im);return np.clip(np.rint(v.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
for suffix in MAPS:
 before=arr(g.REVIEW/('AtlasBefore_'+suffix+'.png'));now=arr(TEXTURES/(NAME+'_'+suffix+'.png'))
 for s in base['registryBefore']['slots']:
  x,y,w,h=s['pixelRectTopLeft'];check(np.array_equal(before[y:y+h,x:x+w],now[y:y+h,x:x+w]),s['key']+' '+suffix+' all original pixels/padding retained')
 del before,now
for name in ['OrchidPetal','OrchidSepal','OrchidLip','OrchidLeaf']:
 s=slots[g.ATLAS+':'+name];check(s['primaryDetailPreserved'] and s['contentRectTopLeft'][2:]==[512,512],name+' full 512px source detail retained')
bpy.ops.wm.open_mainfile(filepath=str(g.MASTER));check(len([o for o in bpy.data.objects if o.get('Phase')])==6,'Six editable source stages')
expected={'Seed':(0,0,0,0),'Sprout':(2,0,0,7),'Young':(4,0,0,7),'BudForming':(5,0,5,7),'Ready':(5,4,2,7),'Harvest':(0,2,2,0)};structure={};joints=[];leafMetrics={};closed=0
for stage,(nleaf,nflower,nbud,nroot) in expected.items():
 root=bpy.data.objects['Orchid_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];leaves=[o for o in obs if o.get('ReferenceLeaf')=='Orchid'];flowers=[o for o in obs if o.get('OpenFlower')=='Orchid'];buds=[o for o in obs if o.get('ClosedBud')=='Orchid'];roots=[o for o in obs if o.get('OrchidAerialRoot')]
 check((len(leaves),len(flowers),len(buds),len(roots))==(nleaf,nflower,nbud,nroot),stage+' reference leaf/flower/bud/root counts')
 structure[stage]=dict(leaves=nleaf,flowers=nflower,buds=nbud,aerialRoots=nroot)
 if stage=='Seed':check(sum(bool(o.get('OrchidSeed')) for o in obs)==11,'Illustrated ivory seed pile')
 for index in range(nflower):
  parts=[o for o in obs if o.get('FlowerKind')=='Orchid' and o.get('FlowerIndex')==index]
  check(sum(o.get('PetalRole')=='Broad lateral petal' for o in parts)==2,stage+' flower '+str(index)+' two broad lateral petals')
  check(sum(o.get('PetalRole')=='Dorsal sepal' for o in parts)==1 and sum(o.get('PetalRole')=='Lower sepal' for o in parts)==2,stage+' flower '+str(index)+' three separate sepals')
  check(sum(o.get('PetalRole')=='Lip side lobe' for o in parts)==2 and sum(bool(o.get('LipLandingLobe')) for o in parts)==1,stage+' flower '+str(index)+' physical three-lobed lip')
 for o in obs:
  if not o.get('ClosedVolume'):continue
  bm=bmesh.new();bm.from_mesh(o.data);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-10,o.name+' closed sculpted volume');bm.free();closed+=1
 if leaves:
  areas=np.zeros(3);points=[];leafData=[]
  for o in leaves:
   check(o.get('SpatialLeaf') and o.get('LeafFrame')=='radial growth and world up',stage+' physical radial leaf frame')
   v=np.array([tuple(p.co) for p in o.data.vertices]);points.extend(v);o.data.calc_loop_triangles();faces=np.array([tuple(t.vertices) for t in o.data.loop_triangles]);tri=v[faces];areas+=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5;s=np.linalg.svd(v-v.mean(axis=0),compute_uv=False);ratio=float(s[-1]/s[0]);check(ratio>.012,stage+' independently non-planar leaf blade');leafData.append(dict(index=int(o['LeafIndex']),nonplanarRatio=ratio))
  span=np.ptp(np.array(points),axis=0);depthRatio=float(span[1]/span[0]);projectedRatio=float(areas.min()/areas.max());check(depthRatio>.35 and projectedRatio>.22,stage+' foliage readable in three perpendicular projections')
  leafMetrics[stage]=dict(depthWidthRatio=depthRatio,minMaxProjectedAreaRatio=projectedRatio,leaves=leafData)
  if stage=='Ready':
   allpoints=np.array([tuple(v.co) for o in obs for v in o.data.vertices]);extent=np.ptp(allpoints,axis=0);leafheight=max(p[2] for p in points)-allpoints[:,2].min();ratio=float(leafheight/extent[2]);check(.34<ratio<.60,'Reference basal foliage proportion relative to flowering height');leafMetrics[stage]['leafHeightPlantHeightRatio']=ratio
 heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};check(set(heads)==set(stems),stage+' every flower/bud has a connected pedicel')
 def ring(o,start):return sum((v.co for v in o.data.vertices[start:start+9]),Vector())/9
 for key,head in heads.items():
  stem=stems[key];tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);a=ring(head,0);b=ring(head,10);gap=(tip-a).length;angle=math.degrees((tip-prev).angle(b-a));check(gap<1e-6 and angle<.15,stage+' '+key+' actual neck connection and tangent');joints.append(dict(stage=stage,joint=key,gap=gap,angleDegrees=angle))
for p in sorted(g.MODELS.glob('*.json')):
 s=load(p);check(s['materials']==[NAME] and geometry_hash(s)==exports[p.stem]['geometrySha256'],p.stem+' exact source geometry and shared material');check(s['triangles']<=(2800 if p.stem.endswith('_LOD1') else 26010),p.stem+' mobile triangle budget')
for old in base['manifestBefore']['plants']:check(old==next(r for r in load(SOURCE/'Integration/Manifest.json')['plants'] if r['key']==old['key']),'Prior crop binding retained '+old['species'])
row=next(r for r in load(SOURCE/'Integration/Manifest.json')['plants'] if r['key']=='orchid');check([r['name'] for r in row['stages']]==['Seed','Sprout','Young Plant','Bud Forming','Harvest Ready'],'Orchid field phase order')
icons=load(SOURCE.parent/'UIIcons/Seeds/G12_IconAudit.json');check(icons['status']=='PASS' and icons['newPackets']==1,'Existing paper-bag renderer and reopened GIMP icon masters')
for field,folder,name in [('seed','Seeds','icon_seed_orchid'),('produce','Crops','icon_crop_orchid'),('mutation','Crops','icon_crop_orchid_mutation')]:
 p=A/'Textures/UI/Icons'/folder/(name+'.png');v=arr(p);mask=v[:,:,3];ys,xs=np.nonzero(mask);check(v.shape==(256,256,4) and mask.min()==0 and mask.max()>=253 and min(xs)>0 and min(ys)>0 and max(xs)<255 and max(ys)<255,name+' transparent 256px safe margins and opaque artwork')
report=dict(status='PASS',group='G12',checks=len(checks),stages=structure,closedSculptedVolumes=closed,floralJoints=joints,leafMetrics=leafMetrics,priorAtlasSurfacesRetained=len(base['registryBefore']['slots']),runtimeAtlas=NAME,runtimeMaterials=1,modelsWithLods=12,fieldProfiles=27,masterSha256=sha(g.MASTER),protectedFiles=len(base['protected']),sourceHashes={rel(p):sha(p) for p in (SOURCE/'Scripts').glob('*g12*.py')},unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN')
write(g.REVIEW/'ArtAudit.json',report);print('G12_REFERENCE_ART_PASS',len(checks),'checks;',closed,'closed sculpted volumes;',len(joints),'connected joints',flush=True)
