"""Pack existing pixels and remap UVs/material references; never rebuild plant shapes."""
import bpy,sys,array,math
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from unified_plant_atlas import *
from io_scene_fbx import parse_fbx,encode_bin
group=sys.argv[sys.argv.index('--group')+1] if '--group' in sys.argv else None
assert BASELINE.exists(),'Run prepare_unified_plant_atlas.py first'
registry=plan(group);pages=source_pages();uv_cache={};index=allocations(registry)

def mapped(page,uv):
 key=(page,*uv)
 if key not in uv_cache:uv_cache[key]=map_uv(page,uv,registry,pages)
 return uv_cache[key]

def image_array(p):
 im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color';w,h=im.size;data=np.empty(w*h*4,np.float32);im.pixels.foreach_get(data);bpy.data.images.remove(im)
 return np.clip(np.rint(data.reshape(h,w,4)[::-1]*255),0,255).astype(np.uint8)

def save_png(p,rgba):
 h,w=rgba.shape[:2];im=bpy.data.images.new(p.stem,width=w,height=h,alpha=True,float_buffer=False);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set((rgba[::-1].astype(np.float32)/255).ravel());im.filepath_raw=str(p);im.file_format='PNG';im.save();bpy.data.images.remove(im)

def resized(tile,size,suffix):
 h,w=tile.shape[:2]
 if (h,w)==(size,size):return tile.copy()
 assert h==w and w%size==0,(w,size)
 factor=w//size;f=tile.astype(np.float32)/255
 if suffix=='BaseColor':f[:,:,:3]=np.where(f[:,:,:3]<=.04045,f[:,:,:3]/12.92,((f[:,:,:3]+.055)/1.055)**2.4)
 f=f.reshape(size,factor,size,factor,4).mean(axis=(1,3))
 if suffix=='BaseColor':f[:,:,:3]=np.where(f[:,:,:3]<=.0031308,f[:,:,:3]*12.92,1.055*np.maximum(f[:,:,:3],0)**(1/2.4)-.055)
 if suffix=='Normal':
  n=f[:,:,:3]*2-1;n/=np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-6);f[:,:,:3]=n*.5+.5
 return np.clip(np.rint(f*255),0,255).astype(np.uint8)

previous=load(REVIEW/'Packing.json') if (REVIEW/'Packing.json').exists() else {}
sourceHashes={rel(source_texture(page,suffix)):sha(source_texture(page,suffix)) for page in {s['sourceAtlas'] for s in registry['slots']} for suffix in MAPS}
mapsCurrent=previous.get('sourceHashes')==sourceHashes and previous.get('registrySha256')==sha(REGISTRY) and all((TEXTURES/(NAME+'_'+suffix+'.png')).exists() and sha(TEXTURES/(NAME+'_'+suffix+'.png'))==previous.get('maps',{}).get(suffix) for suffix in MAPS)
surfaceAudit=dict(previous.get('tilePixelHashes',{})) if mapsCurrent else {}
if mapsCurrent:print('UNIFIED_MAPS_UNCHANGED',flush=True)
for suffix in ([] if mapsCurrent else MAPS):
 result=np.empty((SIZE,SIZE,4),np.uint8);result[:]=[128,128,255,255] if suffix=='Normal' else [0,255,128,255] if suffix=='SurfaceMask' else [64,105,45,255]
 for page in sorted({s['sourceAtlas'] for s in registry['slots']}):
  p=source_texture(page,suffix);sourceHashes[rel(p)]=sha(p);source=image_array(p)
  for s in [s for s in registry['slots'] if s['sourceAtlas']==page]:
   sx,sy,w,h=s['sourceRect'];x,y,tw,th=s['pixelRectTopLeft'];cx,cy,cw,ch=s['contentRectTopLeft'];tile=resized(source[sy:sy+h,sx:sx+w],cw,suffix)
   if suffix=='SurfaceMask':
    tile[:,:,1]=round(s['normalStrength']*255);tile[:,:,2]=round(s['saturation']/2*255);tile[:,:,3]=np.clip(np.rint(tile[:,:,3].astype(float)*s['smoothnessScale']),0,255)
   pad=cx-x
   if pad:tile=np.pad(tile,((pad,pad),(pad,pad),(0,0)),mode='edge')
   result[y:y+th,x:x+tw]=tile;surfaceAudit[s['key']+':'+suffix]=hashlib.sha256(tile.tobytes()).hexdigest()
  del source
 save_png(TEXTURES/(NAME+'_'+suffix+'.png'),result);del result;print('UNIFIED_MAP',suffix,flush=True)

methods={ord(k):v for k,v in {'B':'bool','C':'char','Z':'int8','Y':'int16','I':'int32','L':'int64','F':'float32','D':'float64','R':'bytes','S':'string','i':'int32_array','l':'int64_array','f':'float32_array','d':'float64_array','b':'bool_array','c':'byte_array'}.items()}
def convert_fbx(node,page,uv_count):
 result=encode_bin.FBXElem(node.id)
 for i,(kind,value) in enumerate(zip(node.props_type,node.props)):
  if node.id==b'UV' and kind in [ord('d'),ord('f')]:
   uv_count[0]+=len(value)//2;value=array.array(value.typecode,(x for uv in zip(value[::2],value[1::2]) for x in mapped(page,uv)))
  elif kind==ord('S') and isinstance(value,bytes):
   match=re.search(re.escape(page.encode())+b'_(BaseColor|Normal|SurfaceMask)\\.png',value)
   if match and node.id.lower() in [b'filename',b'relativefilename']:
    filename=NAME+'_'+match[1].decode()+'.png';value=(str(TEXTURES/filename) if node.id.lower()==b'filename' else '../../../Textures/Shared/Plants/'+filename).encode()
   else:value=value.replace(page.encode(),NAME.encode())
  getattr(result,'add_'+methods[kind])(value)
 result.elems=[convert_fbx(n,page,uv_count) for n in node.elems];return result

migrated=[]
for p in model_paths(group):
 spec=load(p);page=spec['materials'][0]
 if page==NAME:continue
 before=geometry_hash(spec);spec['uv']=[mapped(page,uv) for uv in spec['uv']];spec['materials']=[NAME];spec['sourceAtlas']=page;spec['unifiedAtlasVersion']=1
 assert geometry_hash(spec)==before,p
 fbx=p.with_suffix('.fbx');tree,version=parse_fbx.parse(str(fbx));counter=[0];converted=convert_fbx(tree,page,counter);assert counter[0]>0,fbx;encode_bin.write(str(fbx),converted,version)
 p.write_text(json.dumps(spec,separators=(',',':')));patch_native(p,spec['uv']);migrated.append(rel(p))
 if len(migrated)%32==0:print('UNIFIED_MESHES',len(migrated),flush=True)
material_metadata();patch_bindings(group);update_catalog();registry['materialGuid']=guid(MATERIAL);write(REGISTRY,registry)
write(REVIEW/'Packing.json',dict(status='PASS',sourceHashes=sourceHashes,tilePixelHashes=surfaceAudit,maps={k:sha(TEXTURES/(NAME+'_'+k+'.png')) for k in MAPS},registrySha256=sha(REGISTRY),resampling='Primary leaves/petals retain original pixels and resolution; only small stem/seed/pollen surfaces use area downsampling, normals renormalized',surfaceChannels={'R':'metallic','G':'source normal strength','B':'source saturation / 2','A':'smoothness'},migratedMeshes=migrated))
migration_report();print('UNIFIED_PUBLISH_PASS',len(migrated),'UV-only mesh migrations;',len(registry['slots']),'surfaces;',registry['freePercent'],'percent free',flush=True)
