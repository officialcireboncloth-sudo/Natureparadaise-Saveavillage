"""Check flower structure, true attachments, texture registration and production files."""
import bpy,sys,json,math,hashlib,re,struct
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
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
prior=baseline['catalog'];current=read(TEXTURES/'NP_Plants_Catalog.json');check(len(prior['plants'])==len(current['plants'])==31,'31 catalog species')
for a,b in zip(prior['plants'],current['plants']):check(a==b if a['group']!='G09' else b['atlas']==ATLAS,a['displayName']+' art catalog boundary')
slots=read(TEXTURES/(ATLAS+'_Slots.json'));check(len(slots['slots'])==16,'16 registered 512px regions');pixel_reports={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
 p=TEXTURES/(ATLAS+'_'+suffix+'.png');im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';check(tuple(im.size)==(2048,2048),suffix+' resolution')
 a=np.empty(2048*2048*4,dtype=np.float32);im.pixels.foreach_get(a);a=a.reshape(2048,2048,4)[::-1].copy();bpy.data.images.remove(im);pixel_reports[suffix]=dict(size=[2048,2048],sha256=digest(p))
 if suffix=='BaseColor':
  check(float(a[:,:,3].min())>.999,'Opaque surfaces with real geometric silhouettes')
  for s in slots['slots'][:6]:
   x,y,w,h=s['pixelRectTopLeft'];tile=a[y+40:y+h-40,x+40:x+w-40,:3];mean=tile.mean(axis=(0,1));sat=(tile.max(axis=2)-tile.min(axis=2))/np.maximum(tile.max(axis=2),1e-6)
   if s['name']=='JasminePetal':check(float(mean.min())>.72 and float(sat.mean())<.25,'Luminous ivory jasmine petals')
   elif s['name'].endswith('Petal'):check(float(mean[0])>.55 and mean[0]>mean[1]*2 and float(sat.mean())>.60,s['name']+' vivid red source')
   else:check(mean[1]>mean[0]*1.15 and float(sat.mean())>.45,s['name']+' fresh detailed green source')
 elif suffix=='Normal':
  n=a[:,:,:3]*2-1;length=np.linalg.norm(n,axis=2);check(length.min()>.94 and length.max()<1.06,'Normalized tangent surface normals')
 else:
  check(float(a[:,:,0].max())==0,'All flower surfaces nonmetallic')
  for s in slots['slots']:
   x,y,w,h=s['pixelRectTopLeft'];check(abs(float(a[y+256,x+256,3])-SMOOTH[s['name']])<.008,s['name']+' smoothness alpha')
 del a
bpy.ops.wm.open_mainfile(filepath=str(MASTER));roots=[o for o in bpy.data.objects if o.get('Phase')];check(len(roots)==18,'18 independent editable phase roots')
mat=bpy.data.materials[ATLAS];images={n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image};check(len(images)==3,'Three packed production maps')
check(bool(re.search(r'_Cull: 0\b',(A/'Material/Shared/Plants'/(ATLAS+'.mat')).read_text())),'Two-sided petals and funnel interiors in the Unity material')
for im in images:check(im.packed_file and hashlib.sha256(im.packed_file.data).hexdigest()==digest(TEXTURES/im.name),im.name+' packed current bytes')
flower_counts={'Rose':[0,0,0,0,3,1],'Hibiscus':[0,0,0,0,3,1],'Jasmine':[0,0,0,0,5,2]};bud_counts={'Rose':[0,0,0,3,1,0],'Hibiscus':[0,0,0,4,3,0],'Jasmine':[0,0,0,14,10,3]};structure={};volumes={};joints=[]
def ring(o,start):return sum((v.co for v in o.data.vertices[start:start+8]),Vector())/8
for kind,phases in PHASES.items():
 for index,stage in enumerate(phases):
  root=bpy.data.objects[kind+'_'+stage];obs=[o for o in root.children_recursive if o.type=='MESH'];flowers=[o for o in obs if o.get('OpenFlower')==kind];buds=[o for o in obs if o.get('ClosedBud')==kind];leaves=[o for o in obs if o.get('ReferenceLeaf')==kind]
  check(len(flowers)==flower_counts[kind][index],kind+' '+stage+' open-flower count');check(len(buds)==bud_counts[kind][index],kind+' '+stage+' closed-bud count')
  structure[kind+'_'+stage]=dict(flowers=len(flowers),buds=len(buds),leaves=len(leaves),petals=sum(o.get('PetalKind')==kind for o in obs))
  if stage=='Seed':check(sum(o.get('SeedKind')==kind for o in obs)==(5 if kind=='Rose' else 1),kind+' seed count')
  if stage=='Sprout':check(len(leaves)==2 and not any(o.get('SerratedLeaf') for o in leaves),kind+' two smooth cotyledons')
  if kind=='Rose' and stage in ['Young','BudForming','Ready','Harvest']:check(sum(bool(o.get('RoseThorn')) for o in obs)>=5,stage+' real rose thorns')
  if flowers:
   expected_petals={'Rose':26,'Hibiscus':5,'Jasmine':18}[kind]*len(flowers);check(structure[kind+'_'+stage]['petals']==expected_petals,kind+' '+stage+' sculpted overlapping petals')
   if kind=='Rose':check(sum(bool(o.get('RoseSpiral')) for o in obs)==len(flowers),stage+' rolled rose hearts')
   if kind=='Hibiscus':
    check(sum(bool(o.get('HibiscusStyle')) for o in obs)==len(flowers),stage+' long staminal columns');check(sum(bool(o.get('HibiscusAnther')) for o in obs)==36*len(flowers),stage+' yellow anthers');check(sum(bool(o.get('HibiscusStigma')) for o in obs)==5*len(flowers),stage+' five red stigma pads per flower')
  stems={o['JointId']:o for o in obs if o.get('JointRole')=='stem'};heads={o['JointId']:o for o in obs if o.get('JointRole')=='head'};check(set(stems)==set(heads),kind+' '+stage+' complete floral attachments')
  for key,stem in stems.items():
   head=heads[key];tip=ring(stem,len(stem.data.vertices)-9);previous=ring(stem,len(stem.data.vertices)-18);base=ring(head,0);end=ring(head,9);gap=(tip-base).length;angle=math.degrees((tip-previous).angle(end-base))
   check(gap<1e-6,kind+' '+stage+' '+key+' actual joint surface');check(angle<.06,kind+' '+stage+' '+key+' continuous branch and calyx axis');joints.append(dict(plant=kind,stage=stage,joint=key,gapMetres=gap,angleDegrees=angle))
  if stage=='Ready':
   points=np.array([tuple(v.co) for o in leaves for v in o.data.vertices]);span=np.ptp(points,axis=0);ratio=float(span[1]/span[0]);check(.50<ratio<1.85,kind+' substantial radial foliage');check(len(leaves)>=18,kind+' dense mature foliage');volumes[kind]=dict(leaves=len(leaves),width=float(span[0]),depth=float(span[1]),depthToWidth=ratio)

# Shared native/FBX validation is appended from the established crop checker.
specs={p.stem:read(p) for p in MODELS.glob('*.json')};expected=36
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
 prefab=A/'Prefabs/Crops/G09'/(name+'.prefab');body=prefab.read_text();check(body.count('guid: '+material)==2 and len(re.findall(r'^--- !u!23 ',body,re.M))==2 and 'm_LODs:' in body,name+' two-LOD prefab')
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

report=dict(status='PASS',group='G09',checks=len(checks),phaseAppearances=18,baseModels=18,meshesWithLods=36,verifiedFbx=len(roundtrip),atlas=ATLAS,newAtlasPages=1,surfaceResolution=512,protectedFiles=len(baseline['protected'])-1,previousAssetsRetained=True,atlasPixels=pixel_reports,volumes=volumes,structure=structure,floralJoints=joints,baseTriangleCounts={n:s['triangles'] for n,s in specs.items() if not n.endswith('_LOD1')},masterSha256=digest(MASTER),roundtrip=roundtrip,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual comparison with the three user references; technical checks do not score artistic similarity.')
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2))
(REVIEW/'BlenderReopenAudit.json').write_text(json.dumps(dict(status='PASS',masterSha256=digest(MASTER),verifiedFbx=len(roundtrip),phaseRoots=18,packedMaps=3),indent=2))
(REVIEW/'BatchStatus.json').write_text(json.dumps(dict(status='PENDING_UNITY_VALIDATION',assetValidation='PASS',unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN'),indent=2))
print('G09_AUDIT_PASS',len(checks),'checks;',len(joints),'floral joints;',len(roundtrip),'FBX roundtrips',flush=True)
