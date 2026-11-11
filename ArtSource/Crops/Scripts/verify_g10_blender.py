"""Reopen production assets; verify closed petals, physical joints, runtime buffers and FBX."""
import bpy,bmesh,sys,json,math,hashlib,re,struct
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g10_config import *
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
baseline=read(REVIEW/'Baseline.json')
for name,digest in baseline['protected'].items():check((ROOT/name).is_file() and sha(ROOT/name)==digest,'Preserved '+name)
slots=manifest()['slots'];pixels={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';check(tuple(im.size)==(2048,2048),suffix+' resolution')
 a=np.empty(2048*2048*4,dtype=np.float32);im.pixels.foreach_get(a);a=a.reshape(2048,2048,4)[::-1].copy();bpy.data.images.remove(im)
 if suffix=='BaseColor':
  check(float(a[:,:,3].min())>.999,'Opaque albedo; geometry defines silhouettes')
  for s in slots[:8]:
   x,y,w,h=s['pixelRectTopLeft'];tile=a[y+32:y+h-32,x+32:x+w-32,:3];mean=tile.mean(axis=(0,1));sat=(tile.max(axis=2)-tile.min(axis=2))/np.maximum(tile.max(axis=2),1e-6)
   check(float(sat.mean())>.45,s['name']+' saturated surface');check(float(tile.std(axis=(0,1)).max())>.02,s['name']+' visible painted variation')
   if s['name'].endswith('Leaf'):check(mean[1]>mean[0]*1.1 and mean[1]>mean[2]*1.25,s['name']+' healthy green')
 elif suffix=='Normal':
  n=a[:,:,:3]*2-1;length=np.linalg.norm(n,axis=2);check(length.min()>.94 and length.max()<1.06,'Normalized tangent normals')
 else:
  check(float(a[:,:,0].max())==0,'Nonmetallic surface mask')
  for s in slots:
   x,y,w,h=s['pixelRectTopLeft'];check(abs(float(a[y+256,x+256,3])-s['smoothness'])<.008,s['name']+' smoothness alpha')
 pixels[suffix]=dict(sha256=sha(p),size=[2048,2048]);del a
bpy.ops.wm.open_mainfile(filepath=str(MASTER));roots=[ob for ob in bpy.data.objects if ob.get('Phase')];check(len(roots)==24,'24 editable growth/harvest roots')
mat=bpy.data.materials[ATLAS];images={n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image};check(len(images)==3,'Three packed maps')
for im in images:check(im.packed_file and hashlib.sha256(im.packed_file.data).hexdigest()==sha(TEXTURES/im.name),im.name+' packed current bytes')
structure={};closed=0;joints=[];expected={'Sunflower':2,'Chrysanthemum':3,'Dahlia':3,'Marigold':4}
for kind,stages in PHASES.items():
 for stage in stages:
  root=bpy.data.objects[kind+'_'+stage];obs=[ob for ob in root.children_recursive if ob.type=='MESH'];flowers=[ob for ob in obs if ob.get('OpenFlower')==kind];buds=[ob for ob in obs if ob.get('ClosedBud')==kind]
  check(len(flowers)==(expected[kind] if stage=='Ready' else 1 if stage=='Harvest' else 0),kind+' '+stage+' phase flower count')
  check(len(buds)==(expected[kind] if stage=='BudForming' else 1 if stage=='Ready' else 0),kind+' '+stage+' phase bud count')
  leaves=[ob for ob in obs if ob.get('ReferenceLeaf')==kind];structure[kind+'_'+stage]=dict(flowers=len(flowers),buds=len(buds),leaves=len(leaves))
  if stage=='Sprout':check(len(leaves)==2,kind+' two cotyledons')
  stems={ob['JointId']:ob for ob in obs if ob.get('JointRole')=='stem'};heads={ob['JointId']:ob for ob in obs if ob.get('JointRole')=='head'};check(set(stems)==set(heads),kind+' '+stage+' every head has a stem')
  def ring(ob,start,sides):return sum((v.co for v in ob.data.vertices[start:start+sides]),Vector())/sides
  for key,stem in stems.items():
   head=heads[key];tip=ring(stem,len(stem.data.vertices)-10,9);previous=ring(stem,len(stem.data.vertices)-20,9);base=ring(head,0,10);end=ring(head,11,10)
   gap=(tip-base).length;angle=math.degrees((tip-previous).angle(end-base));check(gap<1e-6,key+' actual neck gap');check(angle<.10,key+' aligned final stem segment');joints.append(dict(plant=kind,stage=stage,joint=key,gap=gap,angleDegrees=angle))
  for ob in obs:
   if not ob.get('ClosedPetal'):continue
   bm=bmesh.new();bm.from_mesh(ob.data);watertight=all(len(e.link_faces)==2 for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free()
   check(watertight and volume>1e-11,ob.name+' closed petal with volume');closed+=1
  if stage=='Ready':
   coords=np.array([tuple(v.co) for ob in leaves for v in ob.data.vertices]);span=np.ptp(coords,axis=0);check(span[1]/span[0]>.40,kind+' foliage has real depth')
specs={p.stem:read(p) for p in MODELS.glob('*.json')};check(len(specs)==48,'48 near/distance exports')
material=guid(A/'Material/Shared/Plants'/(ATLAS+'.mat'));safe=[s['uvSafe'] for s in slots]
for name,s in specs.items():
 check(s['materials']==[ATLAS] and len(s['submeshes'])==1,name+' single material')
 check(len(s['vertices'])==len(s['normals'])==len(s['uv'])>0,name+' complete channels')
 check(all(math.isfinite(v) for key in ['vertices','normals','uv'] for row in s[key] for v in row),name+' finite channels')
 indices=s['submeshes'][0];check(len(indices)==3*s['triangles'] and min(indices)>=0 and max(indices)<len(s['vertices']),name+' valid indices')
 check(all(any(a-1e-6<=u<=c+1e-6 and b-1e-6<=v<=d+1e-6 for a,b,c,d in safe) for u,v in s['uv']),name+' padded UVs')
 check(s['min'][1]>-.012,name+' ground pivot')
 body=(MODELS/(name+'.asset')).read_text();raw=bytes.fromhex(re.search(r'^  m_IndexBuffer: (.*)$',body,re.M)[1]);check(list(struct.unpack('<'+'I'*(len(raw)//4),raw))==indices,name+' native index buffer')
 raw=bytes.fromhex(re.search(r'^    _typelessdata: (.*)$',body,re.M)[1]);check(len(raw)==48*len(s['vertices']),name+' vertex stride')
 for i in [0,len(s['vertices'])//2,len(s['vertices'])-1]:
  values=struct.unpack_from('<12f',raw,48*i);check(max(abs(a-b) for a,b in zip(values[:3],s['vertices'][i]))<1e-5 and max(abs(a-b) for a,b in zip(values[-2:],s['uv'][i]))<1e-5,name+' native samples')
 for ext in ['.asset','.json','.fbx']:check(Path(str(MODELS/(name+ext))+'.meta').is_file(),name+ext+' meta')
 if name.endswith('_LOD1'):continue
 check(s['triangles']<=26010,name+' near geometry budget');check(specs[name+'_LOD1']['triangles']<s['triangles'] and specs[name+'_LOD1']['triangles']<=2800,name+' distance budget')
 body=(A/'Prefabs/Crops/G10'/(name+'.prefab')).read_text();check(body.count('guid: '+material)==2 and body.count('MeshRenderer:')==2 and 'm_LODs:' in body,name+' two-LOD material links')
 check(all(guid(MODELS/(n+'.asset')) in body for n in [name,name+'_LOD1']),name+' native mesh links')
roundtrip=[]
for p in sorted(MODELS.glob('*.fbx')):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(p));new=set(bpy.data.objects)-before;meshes=[o for o in new if o.type=='MESH'];check(len(meshes)==1,p.name+' imported mesh');ob=meshes[0];s=specs[p.stem]
 check(sum(len(f.vertices)-2 for f in ob.data.polygons)==s['triangles'],p.name+' exact triangle roundtrip');check(len(ob.data.uv_layers)==len(ob.data.materials)==1,p.name+' material/UV roundtrip')
 check(max(abs(a-b) for a,b in zip(sorted(ob.dimensions),sorted(y-x for x,y in zip(s['min'],s['max']))))<.001,p.name+' scale roundtrip');roundtrip.append(p.name)
 for ob in new:bpy.data.objects.remove(ob,do_unlink=True)
bindings=read(MASTER.parent/'UnityVisualBindings.json')
for kind,rows in bindings['plants'].items():
 check([r['stage'] for r in rows]==PHASES[kind],kind+' stage order')
 for r in rows:check(guid(ROOT/r['prefab'])==r['guid'],kind+' '+r['stage']+' prefab resolves')
report=dict(status='PASS',group='G10',checks=len(checks),phaseAppearances=24,meshesWithLods=48,verifiedFbx=len(roundtrip),closedPetals=closed,protectedFiles=len(baseline['protected']),atlas=ATLAS,atlasPixels=pixels,structure=structure,floralJoints=joints,baseTriangleCounts={n:s['triangles'] for n,s in specs.items() if not n.endswith('_LOD1')},masterSha256=sha(MASTER),unityEditorImport='NOT_RUN_BY_AGENT',unityPlayMode='NOT_RUN_BY_AGENT')
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2));print('G10_AUDIT_PASS',len(checks),'checks;',closed,'closed petals;',len(joints),'physical joints;',len(roundtrip),'FBX roundtrips',flush=True)
