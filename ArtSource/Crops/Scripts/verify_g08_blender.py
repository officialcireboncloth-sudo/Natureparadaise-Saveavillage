"""Verify production exports and botanical structure without scoring artistic fidelity."""
import sys,json,math,hashlib,re,struct
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
def read(p):return json.loads(p.read_text())
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
baseline=read(REVIEW/'Baseline.json')
for name,sha in baseline['protected'].items():
 p=ROOT/name
 if p.name=='NP_Plants_Catalog.json':continue
 check(p.is_file() and digest(p)==sha,'Preserved '+name)
prior=baseline['catalog'];current=read(TEXTURES/'NP_Plants_Catalog.json')
check(len(prior['plants'])==len(current['plants'])==31,'31 art rows')
for a,b in zip(prior['plants'],current['plants']):check(a==b if a['group']!='G08' else b['atlas']==ATLAS,a['displayName']+' art catalog boundary')
slots=read(TEXTURES/(ATLAS+'_Slots.json'));check(len(slots['slots'])==16,'16 padded 512px surfaces')
pixel_reports={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color'
 check(tuple(im.size)==(2048,2048),suffix+' 2048 square')
 a=np.empty(2048*2048*4,dtype=np.float32);im.pixels.foreach_get(a);a=a.reshape(2048,2048,4)[::-1].copy();bpy.data.images.remove(im)
 pixel_reports[suffix]=dict(size=[2048,2048],sha256=digest(p))
 if suffix=='BaseColor':
  check(float(a[:,:,3].min())>.999,'Opaque atlas, all silhouettes are real geometry')
  for s in slots['slots'][:8]:
   x,y,w,h=s['pixelRectTopLeft'];tile=a[y+40:y+h-40,x+40:x+w-40,:3];sat=(tile.max(axis=2)-tile.min(axis=2))/np.maximum(tile.max(axis=2),1e-6)
   check(float(sat.mean())>.52,s['name']+' saturated source albedo')
 elif suffix=='Normal':
  n=a[:,:,:3]*2-1;l=np.linalg.norm(n,axis=2);check(l.min()>.94 and l.max()<1.06,'Normalized tangent normals')
 else:
  check(float(a[:,:,0].max())==0,'All cereal surfaces nonmetallic')
  for s in slots['slots']:
   x,y,w,h=s['pixelRectTopLeft'];check(abs(float(a[y+256,x+256,3])-SMOOTH[s['name']])<.008,s['name']+' smoothness alpha')
 del a
bpy.ops.wm.open_mainfile(filepath=str(MASTER));roots=[o for o in bpy.data.objects if o.get('Phase')]
check(len(roots)==12,'12 editable stage roots')
mat=bpy.data.materials[ATLAS];images={n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
check(len(images)==3,'Three packed production maps')
for im in images:check(im.packed_file and digest(TEXTURES/im.name)==hashlib.sha256(im.packed_file.data).hexdigest(),im.name+' current packed bytes')
volumes={};structure={}
for kind,phases in PHASES.items():
 check({r['Phase'] for r in roots if r['Plant']==kind}==set(phases),kind+' six exact phases')
 for stage in phases:
  root=bpy.data.objects[kind+'_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];heads=[o for o in obs if o.get('Panicle')==kind];grains=[o for o in obs if o.get('CerealGrain')==kind];leaves=[o for o in obs if o.get('ReferenceLeaf')==kind]
  structure[kind+'_'+stage]=dict(heads=len(heads),grains=len(grains),leaves=len(leaves))
  if stage=='Seed':check(len(grains)==(3 if kind=='Rice' else 5),kind+' seed count')
  elif stage=='Sprout':check(len(grains)==1 and len(leaves)==2,kind+' two strap leaves with attached seed')
  elif stage=='Young':check(not heads and len(leaves)>=20,kind+' foliage-only tillering stage')
  elif stage in ['Heading','Ready']:
   check(len(heads)==(5 if kind=='Rice' else 3 if stage=='Heading' else 6),kind+' '+stage+' separate heads')
   check(len(grains)>=50 and len(leaves)>=15,kind+' '+stage+' individual kernels and pleated foliage')
   expected=kind+('Gold' if stage=='Ready' else 'Green');check(all(o.get('AtlasSlot')==expected for o in grains),kind+' '+stage+' maturity surface')
  else:check(len(heads)==3 and sum(bool(o.get('CutCulm')) for o in obs)==3,kind+' three cut harvest heads')
  if kind=='Rice' and stage in ['Heading','Ready','Harvest']:
   check(sum(bool(o.get('PanicleBranch')) for o in obs)==len(heads)*6,stage+' branched rice rachises')
   if stage=='Ready':check(all(o.data.vertices[-1].co.z<o.data.vertices[0].co.z for o in heads),'Rice ripe panicles genuinely bend downward')
  if kind=='Wheat' and stage in ['Heading','Ready','Harvest']:
   check(sum(bool(o.get('WheatAwn')) for o in obs)==len(heads)*18,stage+' fine wheat awns')
   check(all(o.get('KernelGroove') for o in grains),stage+' sculpted wheat grooves')
  if stage=='Ready':
   points=np.array([tuple(v.co) for o in leaves for v in o.data.vertices]);span=np.ptp(points,axis=0);ratio=float(span[1]/span[0]);check(.60<ratio<1.65,kind+' radial leaf volume')
   volumes[kind]=dict(width=float(span[0]),depth=float(span[1]),depthToWidth=ratio)
for name in EXTRAS.values():check(len([o for o in bpy.data.objects[name].children_recursive if o.get('CerealGrain')])==30,name+' 30 separate pile grains')
specs={p.stem:read(p) for p in MODELS.glob('*.json')};expected=(12+len(EXTRAS))*2
check(len(specs)==expected,'Complete base and LOD meshes')
material=guid(A/'Material/Shared/Plants'/(ATLAS+'.mat'));safe=[s['uvSafe'] for s in slots['slots']]
for name,s in specs.items():
 check(s['materials']==[ATLAS] and len(s['submeshes'])==1,name+' one shared material')
 check(len(s['vertices'])==len(s['normals'])==len(s['uv'])>0,name+' complete channels')
 check(all(math.isfinite(v) for key in ['vertices','normals','uv'] for row in s[key] for v in row),name+' finite channels')
 indices=s['submeshes'][0];check(len(indices)==3*s['triangles'] and min(indices)>=0 and max(indices)<len(s['vertices']),name+' valid indices')
 check(all(any(a-1e-6<=u<=c+1e-6 and b-1e-6<=v<=d+1e-6 for a,b,c,d in safe) for u,v in s['uv']),name+' padded UVs')
 check(s['min'][1]>-.015,name+' ground pivot')
 asset=(MODELS/(name+'.asset')).read_text();raw=bytes.fromhex(re.search(r'^  m_IndexBuffer: (.*)$',asset,re.M)[1]);check(list(struct.unpack('<'+'I'*(len(raw)//4),raw))==indices,name+' native index buffer')
 raw=bytes.fromhex(re.search(r'^    _typelessdata: (.*)$',asset,re.M)[1]);check(len(raw)==48*len(s['vertices']),name+' native vertex stride')
 for i in [0,len(s['vertices'])//2,len(s['vertices'])-1]:
  values=struct.unpack_from('<12f',raw,48*i);check(max(abs(a-b) for a,b in zip(values[:3],s['vertices'][i]))<1e-5 and max(abs(a-b) for a,b in zip(values[-2:],s['uv'][i]))<1e-5,name+' native position/UV sample '+str(i))
 for ext in ['.asset','.json','.fbx']:check(Path(str(MODELS/(name+ext))+'.meta').is_file(),name+ext+' Unity metadata')
 if name.endswith('_LOD1'):continue
 low=specs[name+'_LOD1'];check(low['triangles']<s['triangles'] and low['triangles']<=2800,name+' distance budget')
 prefab=A/'Prefabs/Crops/G08'/(name+'.prefab');body=prefab.read_text();check(body.count('guid: '+material)==2 and len(re.findall(r'^--- !u!23 ',body,re.M))==2 and 'm_LODs:' in body,name+' two-LOD prefab')
 check(all(guid(MODELS/(n+'.asset')) in body for n in [name,name+'_LOD1']),name+' linked native meshes')
roundtrip=[]
for p in sorted(MODELS.glob('*.fbx')):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(p));new=set(bpy.data.objects)-before;meshes=[o for o in new if o.type=='MESH'];check(len(meshes)==1,p.name+' one imported mesh');o=meshes[0];s=specs[p.stem]
 check(len({v for f in o.data.polygons for v in f.vertices})==len(o.data.vertices),p.name+' no orphan vertices after thin-feature decimation')
 check(sum(len(f.vertices)-2 for f in o.data.polygons)==s['triangles'],p.name+' exact triangle roundtrip');check(len(o.data.uv_layers)==len(o.data.materials)==1,p.name+' material/UV roundtrip')
 check(max(abs(a-b) for a,b in zip(sorted(o.dimensions),sorted(y-x for x,y in zip(s['min'],s['max']))))<.001,p.name+' scale roundtrip');roundtrip.append(dict(file=p.name,triangles=s['triangles']))
 for o in new:bpy.data.objects.remove(o,do_unlink=True)
bindings=read(MASTER.parent/'UnityVisualBindings.json')
for kind,rows in bindings['plants'].items():
 check([r['stage'] for r in rows]==PHASES[kind],kind+' ordered stage bindings')
 for r in rows:check(guid(ROOT/r['prefab'])==r['guid'],kind+' '+r['stage']+' prefab resolves')
report=dict(status='PASS',group='G08',checks=len(checks),phaseAppearances=12,baseModels=12+len(EXTRAS),meshesWithLods=expected,verifiedFbx=len(roundtrip),atlas=ATLAS,newAtlasPages=1,surfaceResolution=512,protectedFiles=len(baseline['protected'])-1,previousAssetsRetained=True,atlasPixels=pixel_reports,volumes=volumes,structure=structure,baseTriangleCounts={n:s['triangles'] for n,s in specs.items() if not n.endswith('_LOD1')},masterSha256=digest(MASTER),roundtrip=roundtrip,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual visual comparison with the two user attachments; technical checks do not establish artistic fidelity.')
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2));(REVIEW/'BlenderReopenAudit.json').write_text(json.dumps(dict(status='PASS',masterSha256=digest(MASTER),verifiedFbx=len(roundtrip),phaseRoots=12,packedMaps=3),indent=2));(REVIEW/'BatchStatus.json').write_text(json.dumps(dict(status='PENDING_UNITY_VALIDATION',assetValidation='PASS',unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN'),indent=2))
print('G08_AUDIT_PASS',len(checks),'checks',json.dumps(structure),flush=True)
