"""Audit all runtime meshes, preserved source data, texture mappings and future capacity."""
import bpy,sys,os,copy,array,zipfile
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from unified_plant_atlas import *
from io_scene_fbx import parse_fbx
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
base=load(BASELINE);registry=load(REGISTRY);packing=load(REVIEW/'Packing.json');changes=load(REVIEW/'AssetChanges.json');pages=source_pages();known=allocations(registry)
for name,s in base['protected'].items():check(sha(ROOT/name)==s or authorized_extension(name,s),'Protected '+name)
for name,c in changes['authorizedChanges'].items():check(c['before']==base['editableBefore'][name] and sha(ROOT/name)==c['after'],'Recorded migration '+name)
for name,s in packing['sourceHashes'].items():check(sha(ROOT/name)==s,'Original source texture '+name)
currentPlants=load(SOURCE/'Integration/Manifest.json')['plants'];currentByKey={r['key']:r for r in currentPlants};extensions=applied_extensions();extensionGroups={r['group'] for r in extensions};expectedNewKeys={k for r in extensions for k in r.get('newPlantKeys',[])}
check(all(currentByKey.get(row['key'])==row for row in base['integrationBefore']['plants']),'All prior crop identities/stages/scales/depths and item bindings retained')
check(set(currentByKey)-{r['key'] for r in base['integrationBefore']['plants']}==expectedNewKeys,'Only explicitly integrated crop species appended')
check(guid(MATERIAL)==registry['materialGuid'],'One shared material identity')
mat=MATERIAL.read_text();check('m_EnableInstancingVariants: 1' in mat and 'guid: '+guid(SHADER) in mat,'Plant-only instanced material')
shader=SHADER.read_text();check('UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)' in shader and 'UNITY_DEFINE_INSTANCED_PROP(half, _PresentationFade)' in shader,'Tint/fade are instanced properties')
check(shader.count('#pragma multi_compile_instancing')==4,'Instancing enabled in forward/shadow/depth/depth-normal passes')
check('_BaseColor' not in shader.split('CBUFFER_START(UnityPerMaterial)')[1].split('CBUFFER_END')[0],'Lifecycle tint does not become a non-instanced material override')
rects=[]
for s in registry['slots']:
 x,y,w,h=s['pixelRectTopLeft'];check(0<=x<x+w<=SIZE and 0<=y<y+h<=SIZE,s['key']+' valid allocation')
 check(all(x+w<=a or a+c<=x or y+h<=b or b+d<=y for a,b,c,d in rects),s['key']+' disjoint UV rectangle');rects.append((x,y,w,h))
 a,b,c,d=s['uvSafe'];gutter=min(a*SIZE-x,(1-d)*SIZE-y,x+w-c*SIZE,y+h-(1-b)*SIZE);check(gutter>=7.99,s['key']+' mip padding')
future=copy.deepcopy(registry);before={s['key']:s['pixelRectTopLeft'][:] for s in future['slots']}
# Remaining groups: orchid leaf + two petal surfaces, lavender leaf + florets,
# three berry fruits and supporting leaves/stems. This is capacity, not new art.
remainingPrimary=0 if 'lavender' in currentByKey else 2 if 'orchid' in currentByKey else 5
berryMedium,berrySmall=(0,0) if all(k in currentByKey for k in ['blueberry','blackberry','raspberry']) else (4,4) if all(k in currentByKey for k in ['blueberry','blackberry']) else (8,12)
for i,size in enumerate([512]*remainingPrimary+[256]*berryMedium+[128]*berrySmall):
 rect=allocate(future,'capacity-test-'+str(i),size);future['slots'].append(dict(key='capacity-test-'+str(i),pixelRectTopLeft=rect))
check(all(allocations(future)[k]['pixelRectTopLeft']==v for k,v in before.items()),'Remaining-group capacity simulation preserves every current UV allocation')
check(len(load(TEXTURES/'NP_Plants_Catalog.json')['plants'])==len(base['catalogBefore']['plants'])==31,'31-species catalog identity retained')
for old,now in zip(base['catalogBefore']['plants'],load(TEXTURES/'NP_Plants_Catalog.json')['plants']):
 excluded=['atlas','runtimeMaterial','authoringAtlas','atlasPolicy']+(['slots','artStatus','gameplayStatus'] if old['group'] in extensionGroups else [])
 check({k:v for k,v in now.items() if k not in excluded}=={k:v for k,v in old.items() if k not in excluded},old['displayName']+' catalog identity/non-authorized fields retained')

def arr(p):
 im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;data=np.empty(w*h*4,np.float32);im.pixels.foreach_get(data);bpy.data.images.remove(im);return np.clip(np.rint(data.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)
textureMetrics={}
for suffix in MAPS:
 p=TEXTURES/(NAME+'_'+suffix+'.png');a=arr(p);check(a.shape==(SIZE,SIZE,4),suffix+' full 4K atlas');check(sha(p)==packing['maps'][suffix],suffix+' packed bytes recorded')
 meta=Path(str(p)+'.meta').read_text();check('isReadable: 0' in meta and 'streamingMipmaps: 1' in meta and 'enableMipMap: 1' in meta,suffix+' runtime memory/import settings')
 check(all(v=='4096' for v in re.findall(r'maxTextureSize: (\d+)',meta)),suffix+' mobile platforms retain 4K layout')
 for platform in ['Android','iOS']:
  block=meta.split('    buildTarget: '+platform+'\n')[1].split('  - serializedVersion:',1)[0];check('overridden: 1' in block and 'textureFormat: 50' in block,suffix+' '+platform+' ASTC 6x6')
 check(('sRGBTexture: 1' in meta)==(suffix=='BaseColor'),suffix+' correct colour space')
 for page in sorted({s['sourceAtlas'] for s in registry['slots']}):
  original=arr(source_texture(page,suffix))
  for s in [s for s in registry['slots'] if s['sourceAtlas']==page]:
   x,y,w,h=s['pixelRectTopLeft'];tile=a[y:y+h,x:x+w];check(hashlib.sha256(tile.tobytes()).hexdigest()==packing['tilePixelHashes'][s['key']+':'+suffix],s['key']+' '+suffix+' correct packed pixels')
   if s['primaryDetailPreserved']:
    sx,sy,sw,sh=s['sourceRect'];old=original[sy:sy+sh,sx:sx+sw]
    channels=[0,3] if suffix=='SurfaceMask' else [0,1,2,3];check(np.array_equal(tile[:,:,channels],old[:,:,channels]),s['key']+' '+suffix+' source detail retained exactly')
   if suffix=='SurfaceMask':
    check(abs(float(tile[0,0,1])/255-s['normalStrength'])<=.002,s['key']+' source normal relief retained')
    check(abs(float(tile[0,0,2])*2/255-s['saturation'])<=.004,s['key']+' source saturation retained')
  del original
 textureMetrics[suffix]=dict(sha256=sha(p),size=[SIZE,SIZE]);del a

def fbx_geometry(tree):
 result=[]
 def walk(node):
  if node.id==b'LayerElementUV':return
  props=[v.tobytes().hex() if isinstance(v,array.array) else v.hex() if isinstance(v,bytes) else v for v in node.props]
  return [node.id.decode(),props,[x for n in node.elems if (x:=walk(n)) is not None]]
 for node in tree.elems:
  if node.id==b'Objects':result=[walk(n) for n in node.elems if n.id==b'Geometry']
 return digest(result)
def fbx_uv(tree):
 result=[]
 def walk(n):
  if n.id==b'UV':result.extend(zip(n.props[0][::2],n.props[0][1::2]))
  for child in n.elems:walk(child)
 walk(tree);return result
def fbx_materials(tree):
 return [n.props[1].split(b'\x00')[0].decode() for node in tree.elems if node.id==b'Objects' for n in node.elems if n.id==b'Material']
temporary=Path(os.environ['TEMP'])/'NP_UnifiedPlantAtlasAudit';temporary.mkdir(parents=True,exist_ok=True);count=0;uv_cache={}
def mapped(page,uv):
 key=(page,*uv)
 if key not in uv_cache:uv_cache[key]=map_uv(page,uv,registry,pages)
 return uv_cache[key]
with zipfile.ZipFile(BACKUP) as z:
 for name,before in base['models'].items():
  p=ROOT/name;s=load(p);old=json.loads(z.read(name));check(geometry_hash(s)==before['geometrySha256'],p.stem+' exact geometry/bounds/LOD retained')
  check(s['materials']==[NAME] and len(s['submeshes'])==1,p.stem+' single shared material')
  expected=np.array([mapped(before['material'],uv) for uv in old['uv']]);check(np.max(np.abs(np.array(s['uv'])-expected))<2e-7,p.stem+' exact UV mapping')
  native=p.with_suffix('.asset').read_text();raw=bytes.fromhex(re.search(r'(?m)^    _typelessdata: (.*)$',native)[1]);v=np.frombuffer(raw,dtype='<f4').reshape(-1,12);check(len(v)==len(s['vertices']) and np.max(np.abs(v[:,10:12]-expected))<2e-7,p.stem+' native mesh UV buffer')
  oldraw=bytes.fromhex(re.search(r'(?m)^    _typelessdata: (.*)$',z.read(str(Path(name).with_suffix('.asset')).replace('\\','/')).decode())[1]);oldv=np.frombuffer(oldraw,dtype='<f4').reshape(-1,12)
  check(np.array_equal(v[:,:10],oldv[:,:10]),p.stem+' positions/normals/tangents bitwise retained')
  check(bytes.fromhex(re.search(r'(?m)^  m_IndexBuffer: (.*)$',native)[1])==bytes.fromhex(re.search(r'(?m)^  m_IndexBuffer: (.*)$',z.read(str(Path(name).with_suffix('.asset')).replace('\\','/')).decode())[1]),p.stem+' index buffer retained')
  f=p.with_suffix('.fbx');temp=temporary/'before.fbx';temp.write_bytes(z.read(str(Path(name).with_suffix('.fbx')).replace('\\','/')));oldtree,_=parse_fbx.parse(str(temp));tree,_=parse_fbx.parse(str(f));check(fbx_geometry(tree)==fbx_geometry(oldtree),p.stem+' FBX geometry/normal/topology retained')
  expectedfbx=np.array([mapped(before['material'],uv) for uv in fbx_uv(oldtree)]);check(np.max(np.abs(np.array(fbx_uv(tree))-expectedfbx))<2e-7,p.stem+' FBX UV mapping')
  check(fbx_materials(tree)==[NAME],p.stem+' FBX shared material name')
  metadata=Path(str(f)+'.meta').read_text();check('name: '+NAME in metadata and 'guid: '+guid(MATERIAL) in metadata,p.stem+' importer remaps shared material')
  count+=1
  if count%64==0:print('UNIFIED_VERIFIED_MESHES',count,flush=True)
for p in model_paths():
 if rel(p) in base['models']:continue
 proof=SOURCE/p.parent.name/('Review/Raspberry/ExportBaseline.json' if p.stem.startswith('NP_Raspberry_') else 'Review/ExportBaseline.json')
 leafRevision=SOURCE/p.parent.name/'Review/Leaf_R03/ExportBaseline.json'
 if leafRevision.exists() and p.stem in load(leafRevision)['models']:proof=leafRevision
 bushRevision=SOURCE/p.parent.name/'Review/Bush_R04/ExportBaseline.json'
 if bushRevision.exists() and p.stem in load(bushRevision)['models']:proof=bushRevision
 tracedRevision=SOURCE/p.parent.name/'Review/Leaf_R05/ExportBaseline.json'
 if tracedRevision.exists() and p.stem in load(tracedRevision)['models']:proof=tracedRevision
 sourceRecord=load(proof)['models'][p.stem];old=load(ROOT/sourceRecord['sourceSpec']);s=load(p)
 check(geometry_hash(s)==sourceRecord['geometrySha256'],p.stem+' new crop source geometry retained')
 check(s['materials']==[NAME] and len(s['submeshes'])==1,p.stem+' new crop shared material')
 expected=np.array([mapped(old['materials'][0],uv) for uv in old['uv']]);check(np.max(np.abs(np.array(s['uv'])-expected))<2e-7,p.stem+' new crop shared UVs')
 native=p.with_suffix('.asset').read_text();raw=bytes.fromhex(re.search(r'(?m)^    _typelessdata: (.*)$',native)[1]);v=np.frombuffer(raw,dtype='<f4').reshape(-1,12)
 check(len(v)==len(s['vertices']) and np.max(np.abs(v[:,:3]-np.array(s['vertices'])))<1e-6 and np.max(np.abs(v[:,3:6]-np.array(s['normals'])))<1e-6 and np.max(np.abs(v[:,10:12]-expected))<2e-7,p.stem+' new crop native geometry/normal/UV channels')
 check(list(struct.unpack('<'+'I'*(s['triangles']*3),bytes.fromhex(re.search(r'(?m)^  m_IndexBuffer: (.*)$',native)[1])))==s['submeshes'][0],p.stem+' new crop native topology')
 tree,_=parse_fbx.parse(str(p.with_suffix('.fbx')));oldtree,_=parse_fbx.parse(str(ROOT/sourceRecord['sourceFbx']));check(fbx_geometry(tree)==fbx_geometry(oldtree),p.stem+' new crop FBX geometry retained')
 check(np.max(np.abs(np.array(fbx_uv(tree))-np.array([mapped(old['materials'][0],uv) for uv in fbx_uv(oldtree)])))<2e-7 and fbx_materials(tree)==[NAME],p.stem+' new crop FBX UV/material')
 count+=1
check(count==len(model_paths()),'Every current near/distance mesh audited')
prefabs=list((A/'Prefabs/Crops').rglob('NP_*.prefab'));matguids={guid(p) for p in MATERIAL.parent.glob('NP_Plants_*.mat')};shared=guid(MATERIAL)
for p in prefabs:
 refs=[g for g in re.findall(r'guid: (\w+)',p.read_text()) if g in matguids];check(refs and set(refs)=={shared},p.stem+' all renderers/LODs share material')
prefabPaths={rel(p) for p in prefabs}
for row in load(SOURCE/'Integration/Manifest.json')['plants']:
 check(all(s['prefab'] in prefabPaths for s in row['stages']) and row['harvest'] in prefabPaths,row['species']+' every gameplay phase/harvest uses an authored shared-atlas prefab')
for p in (A/'Prefabs/Crops').glob('*.prefab'):check(sha(p)==base['editableBefore'][rel(p)],p.stem+' unused legacy wrapper retained')
gallery=WORK/'Blender/NP_Plants_Unified_Runtime.blend'
if gallery.exists():
 bpy.ops.wm.open_mainfile(filepath=str(gallery));crops=[o for o in bpy.data.objects if o.get('CropId')];check(len(crops)==len(currentPlants),'Unified Blender gallery contains every current runtime crop')
 check(all(len(o.data.materials)==1 and o.data.materials[0].name==NAME for o in crops),'Gallery crops use one shared material')
 images=[im for im in bpy.data.images if im.name.startswith(NAME)];check(len(images)==3,'Gallery contains three shared maps')
 for im in images:check(im.packed_file and hashlib.sha256(im.packed_file.data).hexdigest()==sha(TEXTURES/im.name),im.name+' current packed atlas')
scripts=list((SOURCE/'Scripts').glob('*unified*plant*atlas*.py'))+[SOURCE/'Scripts'/n for n in ['prepare_plant_unity.py','plant_library.py','audit_crop_extension.py','compile_crop_integration.py']]
report=dict(status='PASS',checks=len(checks),meshes=count,prefabs=len(prefabs),runtimeMaterials=1,atlasSize=[SIZE,SIZE],surfaces=len(registry['slots']),freePercent=registry['freePercent'],protectedFiles=len(base['protected']),primaryTexturePixelsRetained=True,geometryAndGuidsRetained=True,textureMetrics=textureMetrics,futureCapacity='Remaining groups simulated without moving existing allocations',unityPlayMode='NOT_RUN_BY_AGENT',mobileProfiling='NOT_RUN',sourceHashes={rel(p):sha(p) for p in scripts})
write(REVIEW/'Audit.json',report);print('UNIFIED_ATLAS_AUDIT_PASS',len(checks),'checks;',count,'meshes;',len(prefabs),'prefabs; one material',flush=True)
