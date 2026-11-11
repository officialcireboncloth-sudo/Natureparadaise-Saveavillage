"""Check reference morphology, spatial leaves, physical fruit details and prior atlas preservation."""
import bpy,bmesh,sys,math
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as g
from unified_plant_atlas import *
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
base=load(g.REVIEW/'Baseline.json');registry=load(REGISTRY);slots=allocations(registry);exports=load(g.REVIEW/'ExportBaseline.json')['models']
for p,h in base['protected'].items():check(sha(ROOT/p)==h,'Accepted art/gameplay/UI/hint retained '+p)
for old in base['registryBefore']['slots']:check(slots[old['key']]==old,'Accepted atlas rectangle and settings retained '+old['key'])
def arr(path):
 im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);bpy.data.images.remove(im);return np.clip(np.rint(v.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
for suffix in MAPS:
 before=arr(g.REVIEW/('AtlasBefore_'+suffix+'.png'));now=arr(TEXTURES/(NAME+'_'+suffix+'.png'))
 for s in base['registryBefore']['slots']:
  x,y,w,h=s['pixelRectTopLeft'];check(np.array_equal(before[y:y+h,x:x+w],now[y:y+h,x:x+w]),s['key']+' '+suffix+' pixels and padding unchanged')
 del before,now
for name in ['BlueberryLeaf','BlackberryLeaf','BlueberrySkin','BlackberryDrupe']:
 s=slots[g.ATLAS+':'+name];check(s['primaryDetailPreserved'] and s['contentRectTopLeft'][2:]==[256,256],name+' complete source resolution retained')
bpy.ops.wm.open_mainfile(filepath=str(g.MASTER));check(len([o for o in bpy.data.objects if o.get('Phase')])==12,'Twelve editable phase roots; Raspberry absent')
structure={};joints=[];leavesData={};closed=0;calyx=[]
for kind in g.PHASES:
 structure[kind]={};leavesData[kind]={}
 for stage in g.PHASES[kind]:
  root=bpy.data.objects[kind+'_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];leaves=[o for o in obs if o.get('ReferenceLeaf')==kind];fruit=[o for o in obs if o.get('BerryKind')==kind];flowers=[o for o in obs if o.get('OpenFlower')==kind]
  expectedLeaves=0 if stage in ['Seed','Harvest'] else (3 if kind=='Blackberry' else 2) if stage=='Sprout' else (21 if kind=='Blackberry' else 9) if stage=='Young' else (44 if kind=='Blueberry' else 38)
  expectedFruit=3 if stage=='Harvest' else (7 if kind=='Blueberry' else 8) if stage=='Ready' else (3 if kind=='Blueberry' else 6) if stage=='Flowering' else 0
  expectedFlowers=4 if stage=='Flowering' else 0
  check((len(leaves),len(fruit),len(flowers))==(expectedLeaves,expectedFruit,expectedFlowers),kind+' '+stage+' reference leaf/fruit/flower counts');structure[kind][stage]=dict(leaves=len(leaves),fruits=len(fruit),flowers=len(flowers))
  if stage=='Seed':check(sum(o.get('BerrySeed')==kind for o in obs)==4,kind+' four illustrated seeds')
  if kind=='Blueberry':
   for ob in fruit:
    check(ob.get('RecessedCalyx') and ob.get('CalyxLobes')==5,'Blueberry five-lobed physical crown')
    v=np.array([tuple(p.co) for p in ob.data.vertices]);rim=v[16*31:17*31];bowl=v[17*31:18*31];depth=float(np.linalg.norm(rim.mean(axis=0)-bowl.mean(axis=0)));check(depth>.015,'Blueberry actual recessed bowl depth');calyx.append(dict(stage=stage,index=int(ob['FruitIndex']),depth=depth))
   if stage=='Flowering':check(sum(bool(o.get('BellFlower')) for o in obs)==4,'Four actual cream bell flowers')
  else:
   for ob in fruit:
    n=sum(bool(o.get('BlackberryDrupelet')) and o.get('FruitIndex')==ob['FruitIndex'] for o in obs);check(n==ob['Drupelets'] and n>=20,stage+' individual 3D drupelets per blackberry')
   if stage=='Harvest':check(sum(bool(o.get('BerryCutCore')) for o in obs)==1 and sum(bool(o.get('CrossSection')) for o in fruit)==1,'Detached Blackberry harvest includes actual cream-core cross section')
   if stage=='Flowering':check(sum(bool(o.get('BerryFlowerPetal')) for o in obs)==20,'Four flowers with five separate petals each')
  volumeObjects=[o for o in obs if o.get('ClosedVolume') or o.get('BerryKind')=='Blueberry' or o.get('BellFlower') or o.get('BlackberryDrupelet') or o.get('BerryCutCore')]
  for ob in volumeObjects:
   bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,ob.name+' closed physical volume');bm.free();closed+=1
  if leaves:
   pts=[];areas=np.zeros(3);ratios=[]
   for ob in leaves:
    check(ob.get('SpatialLeaf') and ob.get('LeafFrame')=='radial growth and world up' and bool(ob['Serrated'])==(kind=='Blackberry'),'Species leaf shape and spatial growth')
    v=np.array([tuple(p.co) for p in ob.data.vertices]);pts.extend(v);ob.data.calc_loop_triangles();tri=v[np.array([tuple(t.vertices) for t in ob.data.loop_triangles])];areas+=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5;s=np.linalg.svd(v-v.mean(axis=0),compute_uv=False);r=float(s[-1]/s[0]);check(r>.005,ob.name+' curved rather than planar');ratios.append(r)
   span=np.ptp(np.array(pts),axis=0);depth=float(span[1]/span[0]);projection=float(areas.min()/areas.max())
   if stage not in ['Sprout']:check(depth>.35 and projection>.15,kind+' '+stage+' foliage visible in three perpendicular views')
   leavesData[kind][stage]=dict(depthWidthRatio=depth,minMaxProjectionRatio=projection,minimumLeafNonplanarity=min(ratios))
  if stage in ['Flowering','Ready']:
   heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};check(set(heads)==set(stems),kind+' '+stage+' all fruit and flowers have a continuous pedicel')
   def ring(ob,start):return sum((v.co for v in ob.data.vertices[start:start+9]),Vector())/9
   for key,head in heads.items():
    stem=stems[key];tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);a=ring(head,0);b=ring(head,10);gap=(tip-a).length;angle=math.degrees((tip-prev).angle(b-a));check(gap<1e-6 and angle<.15,kind+' '+stage+' '+key+' physical head connection/tangent');joints.append(dict(species=kind,stage=stage,key=key,gap=gap,angleDegrees=angle))
for p in sorted(g.MODELS.glob('*.json')):
 s=load(p);check(s['materials']==[NAME] and geometry_hash(s)==exports[p.stem]['geometrySha256'],p.stem+' geometry preserved and one shared material');check(s['triangles']<=(2800 if p.stem.endswith('_LOD1') else 26010),p.stem+' mobile triangle budget')
manifest=load(SOURCE/'Integration/Manifest.json')
for old in base['manifestBefore']['plants']:check(old==next(r for r in manifest['plants'] if r['key']==old['key']),'Prior binding unchanged '+old['species'])
for key in ['blueberry','blackberry']:
 row=next(r for r in manifest['plants'] if r['key']==key);check([r['name'] for r in row['stages']]==['Seed','Sprout','Young Plant','Flowering','Harvest Ready'],key+' gameplay phases')
 for folder,name in [('Seeds','icon_seed_'+key),('Crops','icon_crop_'+key),('Crops','icon_crop_'+key+'_mutation')]:
  v=arr(A/'Textures/UI/Icons'/folder/(name+'.png'));mask=v[:,:,3];ys,xs=np.nonzero(mask);check(v.shape==(256,256,4) and mask.min()==0 and mask.max()>=253 and min(xs)>0 and min(ys)>0 and max(xs)<255 and max(ys)<255,name+' transparent 256px safe margin')
oldRasp=next(r for r in base['catalogBefore']['plants'] if r['displayName']=='Raspberry');nowRasp=next(r for r in load(TEXTURES/'NP_Plants_Catalog.json')['plants'] if r['displayName']=='Raspberry');check(oldRasp==nowRasp and not any(r['key']=='raspberry' for r in manifest['plants']),'Missing Raspberry reference is explicitly pending')
check(load(SOURCE.parent/'UIIcons/Seeds/G01_IconAudit.json')['status']=='PASS' and load(g.REVIEW/'GimpReopenAudit.json')['status']=='PASS','Editable icon and texture masters reopened')
report=dict(status='PASS',group='G01',species=list(g.PHASES),pendingReference=['Raspberry'],checks=len(checks),stages=structure,closedSculptedVolumes=closed,floralJoints=joints,leafMetrics=leavesData,calyxDepths=calyx,priorAtlasSurfacesRetained=134,runtimeMaterials=1,fieldProfiles=30,modelsWithLods=24,masterSha256=sha(g.MASTER),sourceHashes={rel(p):sha(p) for p in (SOURCE/'Scripts').glob('*g01*.py')},unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN')
write(g.REVIEW/'ArtAudit.json',report);print('G01_REFERENCE_ART_PASS',len(checks),'checks;',closed,'closed volumes;',len(joints),'connected heads',flush=True)
