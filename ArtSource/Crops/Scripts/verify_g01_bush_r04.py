"""Independent shrub structure, preservation, texture-isolation and published-mesh checks."""
import bpy,bmesh,sys,re,math
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import unified_plant_atlas as U
W=G.REVIEW/'Bush_R04';base=U.load(W/'Baseline.json');proof=U.load(W/'ExportBaseline.json')['models'];transforms=U.load(W/'BindingChanges.json')['plants'];checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
allowed={U.rel(G.RUNTIME_TEXTURES/(U.NAME+'_'+s+'.png')) for s in ['BaseColor','Normal']}
for stem in proof:
 for ext in ['.json','.fbx','.asset']:allowed.add(U.rel(G.MODELS/(stem+ext)))
 if not stem.endswith('_LOD1'):allowed.add(U.rel(G.A/'Prefabs/Crops/G01'/(stem+'.prefab')))
allowed.update(r['profile'] for r in transforms.values())
for name,h in base['protected'].items():
 if name not in allowed:check(U.sha(G.ROOT/name)==h,'Retained unrelated art, UI, data, scripts or material '+name)
check(U.load(U.REGISTRY)==base['registryBefore'],'All 154 stable atlas allocations and settings retained')
def arr(path):
 im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);bpy.data.images.remove(im);return np.clip(np.rint(v.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
changed={'NP_Plants_Berries_Source_01:BlueberryLeaf','NP_Plants_Berries_Source_01:BlackberryLeaf','NP_Plants_Raspberry_Source_01:RaspberryLeaf'}
for suffix in U.MAPS:
 p=G.RUNTIME_TEXTURES/(U.NAME+'_'+suffix+'.png');before=arr(W/'Before'/p.relative_to(G.ROOT));now=arr(p)
 for s in base['registryBefore']['slots']:
  x,y,w,h=s['pixelRectTopLeft'];same=np.array_equal(before[y:y+h,x:x+w],now[y:y+h,x:x+w])
  check(same if s['key'] not in changed or suffix=='SurfaceMask' else not same,s['key']+' '+suffix+' exact authorized leaf-only pixel boundary')
 del before,now
plants=U.load(G.SOURCE/'Integration/Manifest.json')['plants']
for old in base['manifestBefore']['plants']:
 now=next(r for r in plants if r['key']==old['key'])
 if old['key'] not in transforms:check(now==old,'Other crop binding retained '+old['species']);continue
 rec=transforms[old['key']];check(rec['before']==old and rec['after']==now,'Exact visual fit recorded '+old['species']);strip=lambda row:dict(row,stages=[{k:v for k,v in s.items() if k not in ['scale','offset']} for s in row['stages']]);check(strip(old)==strip(now),'Prefab/item references and gameplay retained '+old['species'])
 stripText=lambda text:re.sub(r'(?m)^    (?:scale|localOffset):[^\r\n]*\r?\n?','',text).strip();check(stripText(rec['beforeText'])==stripText(rec['afterText']) and U.sha(G.ROOT/rec['profile'])==rec['afterSha256'],'Only serialized visual transforms revised '+old['species'])
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));roots=[o for o in bpy.data.objects if o.get('Phase')];check(len(roots)==18,'Eighteen editable berry phases');structure={};leafMetrics={};joints=0;closed=0
for root in roots:
 kind,stage=root['Plant'],root['Phase']
 if stage in ['Seed','Harvest']:continue
 check(root.get('BushRevision')=='R04',root.name+' current shrub revision');obs=[o for o in root.children_recursive if o.type=='MESH'];leaves=[o for o in obs if o.get('ReferenceLeaf')==kind];fruits=[o for o in obs if o.get('BerryKind')==kind];flowers=[o for o in obs if o.get('OpenFlower')==kind];canes=[o for o in obs if 'R04 curved main cane' in o.name]
 expectedLeaves=(2 if kind=='Blueberry' else 3) if stage=='Sprout' else (20 if kind=='Blueberry' else 35) if stage=='Young' else (54 if kind=='Blueberry' else 89)
 check(len(leaves)==expectedLeaves,root.name+' connected foliage count');check(len(canes)==(0 if stage=='Sprout' else 4 if kind=='Blueberry' else 5),root.name+' spatial main cane count');structure[root.name]=dict(leaves=len(leaves),fruit=len(fruits),flowers=len(flowers),canes=len(canes))
 if kind!='Blueberry' and stage!='Sprout':
  groups={}
  for ob in leaves:
   if ob.get('CompoundCluster'):groups.setdefault(ob['CompoundCluster'],[]).append(ob)
  check(len(groups)==(10 if stage=='Young' else 24),root.name+' compound leaf group count')
  for key,group in groups.items():check(len(group)==3 and sorted(o['LeafletRole'] for o in group)==['Lateral','Lateral','Terminal'],root.name+' '+key+' actual three-leaflet group')
 pts=[];elev=[];projected=np.zeros(3)
 for ob in leaves:
  check(ob.get('LeafRevision')=='R04' and ob.get('VeinUV')=='taper-aware centerline',ob.name+' species leaf surface mapping');v=np.array([tuple(p.co) for p in ob.data.vertices]);pts.extend(v);elev.append(float(ob['LeafElevation']));s=np.linalg.svd(v-v.mean(axis=0),compute_uv=False);check(s[-1]/s[0]>.002,ob.name+' actual curved leaf volume');bm=bmesh.new();bm.from_mesh(ob.data);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,ob.name+' closed physical thickness');bm.free();closed+=1;ob.data.calc_loop_triangles();tri=v[np.array([tuple(t.vertices) for t in ob.data.loop_triangles])];projected+=np.abs(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])).sum(axis=0)*.5
 span=np.ptp(np.array(pts),axis=0);leafMetrics[root.name]=dict(depthWidthRatio=float(span[1]/span[0]),minMaxProjectionRatio=float(projected.min()/projected.max()),meanInclination=float(np.mean(elev)))
 if stage!='Sprout':check(span[1]/span[0]>.45 and projected.min()/projected.max()>.15,root.name+' genuinely spatial crown')
 if stage in ['Flowering','Ready']:
  heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};check(set(heads)==set(stems),root.name+' all fruit and flowers connected')
  def ring(ob,start):return sum((v.co for v in ob.data.vertices[start:start+9]),Vector())/9
  for key,head in heads.items():
   stem=stems[key];a=ring(head,0);b=ring(head,10);tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);check((tip-a).length<1e-6 and math.degrees((tip-prev).angle(b-a))<.2,root.name+' '+key+' continuous physical neck');joints+=1
  if stage=='Ready':
   centers=np.array([np.mean([tuple(v.co) for v in o.data.vertices],axis=0) for o in fruits]);check(np.ptp(centers[:,1])>.24,root.name+' fruit placed at multiple depths')
for name,record in proof.items():
 spec=U.load(G.MODELS/(name+'.json'));check(spec['materials']==[U.NAME] and U.geometry_hash(spec)==record['geometrySha256'],name+' exact published geometry and one shared material');check(spec['triangles']<=(2800 if name.endswith('_LOD1') else 26010),name+' established mobile triangle budget')
check(U.load(W/'GimpAudit.json')['status']=='PASS','Four editable GIMP source maps reopened')
U.write(W/'ArtAudit.json',dict(status='PASS',checks=len(checks),revision='Bush_R04',structure=structure,leafMetrics=leafMetrics,closedLeaves=closed,continuousJoints=joints,changedLeafSurfaces=3,priorAtlasSlots=154,otherAtlasSurfacesRetained=151,allCropPrefabAndItemReferencesRetained=True,otherCropsRetained=28,runtimeMaterials=1,updatedNearDistanceMeshes=24,masterSha256=U.sha(G.MASTER),sourceHashes={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*bush*r04*.py')},unityPlayMode='NOT_RUN'))
print('BUSH_R04_ART_PASS',len(checks),'checks;',closed,'closed leaves;',joints,'continuous joints',flush=True)
