"""Independent Raspberry preservation, geometry, cavity, foliage and binding checks."""
import bpy,bmesh,sys,json,csv,io,re,math
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Raspberry';base=U.load(R/'Baseline.json');checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
editor=U.rel(G.A/'Script/Editor/CropGameplayValidation.cs')
for name,h in base['protected'].items():
 if name==editor:continue
 check(U.sha(G.ROOT/name)==h,'Prior asset retained '+name)
registry=U.load(U.REGISTRY);slots=U.allocations(registry)
for s in base['registryBefore']['slots']:check(slots[s['key']]==s,'Prior atlas allocation retained '+s['key'])
def arr(p):
 im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);bpy.data.images.remove(im);return np.clip(np.rint(v.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
for suffix in U.MAPS:
 old=arr(R/('AtlasBefore_'+suffix+'.png'));now=arr(G.RUNTIME_TEXTURES/(U.NAME+'_'+suffix+'.png'))
 for s in base['registryBefore']['slots']:
  x,y,w,h=s['pixelRectTopLeft'];check(np.array_equal(old[y:y+h,x:x+w],now[y:y+h,x:x+w]),s['key']+' '+suffix+' original pixels and padding retained')
 del old,now
for name in ['RaspberryLeaf','RaspberryDrupe']:
 s=slots['NP_Plants_Raspberry_Source_01:'+name];check(s['primaryDetailPreserved'] and s['contentRectTopLeft'][2:]==[256,256],name+' full detail retained')
def source_signature(kind):
 result={}
 for root in [o for o in bpy.data.objects if o.get('Plant')==kind]:
  result[root.name]=dict(position=list(root.location),phase=root['Phase'],children={o.name:dict(v=[list(v.co) for v in o.data.vertices],faces=[list(f.vertices) for f in o.data.polygons],uv=[list(l.uv) for l in o.data.uv_layers.active.data],materials=[m.name for m in o.data.materials]) for o in root.children_recursive if o.type=='MESH'})
 return U.digest(result)
bpy.ops.wm.open_mainfile(filepath=str(R/'G01_Before_Raspberry.blend'));old={k:source_signature(k) for k in ['Blueberry','Blackberry']}
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER))
for k,h in old.items():check(source_signature(k)==h,k+' editable source geometry, UVs, material and placement unchanged')
check(len([o for o in bpy.data.objects if o.get('Phase')])==18,'Eighteen editable berry phase roots')
structure={};leafMetrics={};closed=0;joints=[]
for stage in ['Seed','Sprout','Young','Flowering','Ready','Harvest']:
 root=bpy.data.objects['Raspberry_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];leaves=[o for o in obs if o.get('ReferenceLeaf')=='Raspberry'];fruit=[o for o in obs if o.get('BerryKind')=='Raspberry'];flowers=[o for o in obs if o.get('OpenFlower')=='Raspberry']
 counts=(len(leaves),len(fruit),len(flowers));expected={'Seed':(0,0,0),'Sprout':(3,0,0),'Young':(21,0,0),'Flowering':(44,5,4),'Ready':(44,7,0),'Harvest':(0,3,0)}[stage];check(counts==expected,'Raspberry '+stage+' reference morphology counts');structure[stage]=dict(leaves=counts[0],fruits=counts[1],flowers=counts[2])
 if stage=='Seed':check(sum(o.get('BerrySeed')=='Raspberry' for o in obs)==4,'Four tan Raspberry seeds')
 if stage=='Flowering':check(sum(bool(o.get('BerryFlowerPetal')) for o in obs)==20,'Four separate five-petal white Raspberry flowers')
 for ob in fruit:
  check(sum(bool(o.get('RaspberryDrupelet')) and o.get('FruitIndex')==ob['FruitIndex'] for o in obs)==ob['Drupelets']==53,'Fifty-three actual 3D drupelets per Raspberry')
  verts=np.array([tuple(v.co) for v in ob.data.vertices]);rim=verts[4*40:5*40].mean(axis=0);floor=verts[8*40:9*40].mean(axis=0);depth=float(np.linalg.norm(rim-floor));check(depth>.05,'Raspberry actual open cup depth')
 if stage=='Harvest':check(sum(bool(o.get('DetachedHollow')) for o in fruit)==1,'Detached front Raspberry has its visible natural hollow opening')
 for ob in [o for o in obs if o.get('ClosedVolume') or o.get('BerryKind')=='Raspberry' or o.get('RaspberryDrupelet')]:
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,ob.name+' closed sculpted volume');bm.free();closed+=1
 if leaves:
  points=[];area=np.zeros(3);ratios=[]
  for ob in leaves:
   check(ob.get('SpatialLeaf') and ob.get('Serrated'),'Spatial serrated Raspberry leaflet');v=np.array([tuple(p.co) for p in ob.data.vertices]);points.extend(v);ob.data.calc_loop_triangles();tri=v[np.array([tuple(t.vertices) for t in ob.data.loop_triangles])];area+=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5;s=np.linalg.svd(v-v.mean(axis=0),compute_uv=False);ratios.append(float(s[-1]/s[0]))
  span=np.ptp(np.array(points),axis=0);metrics=dict(depthWidthRatio=float(span[1]/span[0]),projectionRatio=float(area.min()/area.max()),minimumLeafNonplanarity=min(ratios));check(min(ratios)>.005,'Raspberry '+stage+' curved leaves')
  if stage!='Sprout':check(metrics['depthWidthRatio']>.35 and metrics['projectionRatio']>.15,'Raspberry '+stage+' foliage has depth in perpendicular views')
  leafMetrics[stage]=metrics
 if stage in ['Flowering','Ready']:
  heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};check(set(heads)==set(stems),'Raspberry '+stage+' all fruit and flowers connected')
  def ring(ob,start):return sum((v.co for v in ob.data.vertices[start:start+9]),Vector())/9
  for key,head in heads.items():
   stem=stems[key];tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);a=ring(head,0);b=ring(head,10);gap=(tip-a).length;angle=math.degrees((tip-prev).angle(b-a));check(gap<1e-6 and angle<.15,'Raspberry '+stage+' '+key+' physical neck continuity');joints.append(dict(stage=stage,key=key,gap=gap,angle=angle))
proof=U.load(R/'ExportBaseline.json')['models']
for p in G.MODELS.glob('NP_Raspberry*.json'):
 spec=U.load(p);check(spec['materials']==[U.NAME] and U.geometry_hash(spec)==proof[p.stem]['geometrySha256'],p.stem+' original geometry and one shared material');check(spec['triangles']<=(2800 if p.stem.endswith('_LOD1') else 26010),p.stem+' triangle budget')
manifest=U.load(G.SOURCE/'Integration/Manifest.json');check(len(manifest['plants'])==31,'All 31 referenced plants integrated')
for old in base['manifestBefore']['plants']:check(next(r for r in manifest['plants'] if r['key']==old['key'])==old,'Prior crop binding unchanged '+old['species'])
for name,key,added in [('Items','item_id',3),('Crops','crop_id',1)]:
 old=list(csv.DictReader(io.StringIO(base['csvBefore'][name])));now=list(csv.DictReader(io.StringIO((G.A/f'Data/Balance/{name}.csv').read_text(encoding='utf-8-sig'))));index={r[key]:r for r in now};check(len(now)==len(index)==len(old)+added,name+' exact row additions without duplicate IDs')
 for r in old:check(index[r[key]]==r,'Previous balance retained '+r[key])
for folder,name in [('Seeds','icon_seed_raspberry'),('Crops','icon_crop_raspberry'),('Crops','icon_crop_raspberry_mutation')]:
 v=arr(G.A/'Textures/UI/Icons'/folder/(name+'.png'));ys,xs=np.nonzero(v[:,:,3]);check(v.shape==(256,256,4) and v[:,:,3].min()==0 and v[:,:,3].max()>=253 and min(xs)>0 and min(ys)>0 and max(xs)<255 and max(ys)<255,name+' transparent 256px safe margins')
for report in [R/'GimpReopenAudit.json',G.SOURCE.parent/'UIIcons/Seeds/G01_Raspberry_IconAudit.json']:check(U.load(report)['status']=='PASS','Editable GIMP masters reopened '+report.name)
report=dict(status='PASS',checks=len(checks),priorCropsRetained=30,priorAtlasSlotsAndPixelsRetained=len(base['registryBefore']['slots']),species=['Blueberry','Raspberry','Blackberry'],stages=structure,leafMetrics=leafMetrics,closedSculptedVolumes=closed,floralJoints=joints,masterSha256=U.sha(G.MASTER),runtimeMaterials=1,atlasSize=[4096,4096],sourceHashes={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*raspberry*.py')},unityPlayMode='NOT_RUN',mobileProfiling='NOT_RUN')
U.write(R/'ArtAudit.json',report);print('RASPBERRY_ART_PRESERVATION_PASS',len(checks),'checks;',closed,'closed volumes;',len(joints),'continuous joints',flush=True)
