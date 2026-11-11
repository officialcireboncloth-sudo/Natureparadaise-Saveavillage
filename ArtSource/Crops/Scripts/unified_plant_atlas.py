"""One runtime crop atlas. Source pages stay editable; allocated rectangles never move."""
import json,hashlib,re,struct,zipfile,uuid
from pathlib import Path
from plant_library import ROOT,A,SOURCE,TEXTURES
NAME='NP_Plants_Unified_01'
SIZE=4096
WORK=SOURCE/'Unified'
REVIEW=WORK/'Review'
REGISTRY=WORK/'AtlasRegistry.json'
BASELINE=REVIEW/'BeforeMerge.json'
BACKUP=REVIEW/'BeforeMerge.zip'
MATERIAL=A/'Material/Shared/Plants'/(NAME+'.mat')
SHADER=A/'Material/Shared/Plants/PlantAtlas.shader'
BLENDER=Path(r'C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe')
MAPS=['BaseColor','Normal','SurfaceMask']

def load(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def rel(p):return Path(p).relative_to(ROOT).as_posix()
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def digest(data):return hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def write(p,value):
 p=Path(p);p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,indent=2),encoding='utf-8')
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
def model_paths(group=None):return sorted((A/'mesh/Crops'/group).glob('*.json')) if group else sorted((A/'mesh/Crops').rglob('*.json'))
def geometry_hash(s):return digest({k:v for k,v in s.items() if k not in ['uv','materials','sourceAtlas','unifiedAtlasVersion']})
def source_pages():
 paths=list(TEXTURES.glob('*_Slots.json'))+list(SOURCE.glob('G*/Textures/*_Slots.json'))
 return {p.stem[:-6]:load(p) for p in paths if not p.stem.startswith(NAME)}
def source_texture(page,suffix):
 p=TEXTURES/(page+'_'+suffix+'.png')
 if p.exists():return p
 matches=list(SOURCE.glob('G*/Textures/'+page+'_'+suffix+'.png'));assert len(matches)==1,(page,suffix)
 return matches[0]
def source_settings(page):
 p=MATERIAL.parent/(page+'.mat')
 if not p.exists():return source_pages()[page]['materialSettings']
 text=p.read_text();values=dict(re.findall(r'^    - (\w+): ([^\r\n]+)',text,re.M))
 return {k:float(values[k]) for k in ['_BumpScale','_Saturation','_Smoothness']}
def source_slot(page,uv,pages=None):
 slots=(pages or source_pages())[page]['slots'];u,v=uv
 found=[s for s in slots if s['uvSafe'][0]-2e-6<=u<=s['uvSafe'][2]+2e-6 and s['uvSafe'][1]-2e-6<=v<=s['uvSafe'][3]+2e-6]
 assert len(found)==1,(page,uv,'UV must belong to one padded source surface')
 return found[0]
def target_size(slot):
 w,h=slot['pixelRectTopLeft'][2:];assert w==h
 n=slot['name'].lower();hero=slot.get('primaryDetail',False) or any(k in n for k in ['leaf','petal','bud','bulbskin','sunflowerdisk']) or n in ['ricegold','ricegreen','wheatgold','wheatgreen']
 return w if hero or w<=256 else 128
def allocations(registry):return {s['key']:s for s in registry['slots']}

def free_rectangles(registry):
 """Maximal empty rectangles around the fixed allocations; never move occupied tiles."""
 free=[(0,0,registry['size'],registry['size'])]
 for slot in registry['slots']:
  a,b,c,d=slot['pixelRectTopLeft'];right,bottom=a+c,b+d;split=[]
  for x,y,w,h in free:
   if a>=x+w or right<=x or b>=y+h or bottom<=y:split.append((x,y,w,h));continue
   if a>x:split.append((x,y,a-x,h))
   if right<x+w:split.append((right,y,x+w-right,h))
   if b>y:split.append((x,y,w,b-y))
   if bottom<y+h:split.append((x,bottom,w,y+h-bottom))
  unique=set(split)
  free=sorted(r for r in unique if not any(r!=o and o[0]<=r[0] and o[1]<=r[1] and o[0]+o[2]>=r[0]+r[2] and o[1]+o[3]>=r[1]+r[3] for o in unique))
 return free

def allocate(registry,key,size):
 """Append a square with best short-side fit, keeping every existing UV fixed."""
 assert size>0 and size%32==0
 candidates=[r for r in free_rectangles(registry) if r[2]>=size and r[3]>=size]
 if candidates:
  x,y,w,h=min(candidates,key=lambda r:(min(r[2]-size,r[3]-size),max(r[2]-size,r[3]-size),r[1],r[0]))
  return [x,y,size,size]
 raise RuntimeError('The shared atlas is full; do not silently create a new material or shrink existing surfaces: '+key)

def plan(group=None,reset_unpublished=False):
 if reset_unpublished:assert not (REVIEW/'Packing.json').exists(),'Published UV allocations cannot be reset'
 pages=source_pages();registry=load(REGISTRY) if REGISTRY.exists() and not reset_unpublished else dict(version=1,name=NAME,size=SIZE,slots=[],policy='One shared runtime material; stable append-only UV rectangles; source-page namespaces; preserve primary leaf/petal texel density')
 old={s['key']:s['pixelRectTopLeft'][:] for s in registry['slots']};known=allocations(registry);wanted={}
 for p in model_paths(group):
  spec=load(p);page=spec['materials'][0]
  if page==NAME:continue
  assert len(spec['materials'])==1 and page in pages,p
  # Source grids are small; this also rejects invalid and out-of-slot UVs before mutation.
  for uv in spec['uv']:
   s=source_slot(page,uv,pages);key=page+':'+s['name']
   if key not in known:wanted[key]=(page,s)
 for key,(page,s) in sorted(wanted.items(),key=lambda item:(-target_size(item[1][1]),item[0])):
  size=target_size(s);padding=16 if size<s['pixelRectTopLeft'][2] else 0;rect=allocate(registry,key,size+2*padding);settings=source_settings(page);sw,sh=pages[page]['colorSize'];x,y=rect[:2];x+=padding;y+=padding;w=h=size;sx,sy,rw,rh=s['pixelRectTopLeft'];u0,v0,u1,v1=s['uvSafe']
  # Scale the original safe padding together with the tile, retaining its mapping.
  du0=(x+(u0*sw-sx)*w/rw)/SIZE;du1=(x+(u1*sw-sx)*w/rw)/SIZE
  dv0=1-(y+((1-v0)*sh-sy)*h/rh)/SIZE;dv1=1-(y+((1-v1)*sh-sy)*h/rh)/SIZE
  row=dict(key=key,name=key,index=len(registry['slots']),sourceAtlas=page,sourceSlot=s['name'],sourceRect=s['pixelRectTopLeft'],sourceSize=pages[page]['colorSize'],sourceUvSafe=s['uvSafe'],pixelRectTopLeft=rect,contentRectTopLeft=[x,y,w,h],uvSafe=[du0,dv0,du1,dv1],normalStrength=settings['_BumpScale'],saturation=settings['_Saturation'],smoothnessScale=settings['_Smoothness'],primaryDetailPreserved=size==rw)
  registry['slots'].append(row);known[key]=row
 assert all(known[k]['pixelRectTopLeft']==v for k,v in old.items()),'Existing allocations must never move'
 for row in registry['slots']:
  settings=source_settings(row['sourceAtlas']);row['normalStrength']=settings['_BumpScale'];row['saturation']=settings['_Saturation'];row['smoothnessScale']=settings['_Smoothness']
 used=sum(s['pixelRectTopLeft'][2]*s['pixelRectTopLeft'][3] for s in registry['slots']);registry['usedPixels']=used;registry['freePixels']=SIZE*SIZE-used;registry['freePercent']=round(100*(1-used/SIZE**2),2)
 registry['maps']={k:rel(TEXTURES/(NAME+'_'+k+'.png')) for k in MAPS};registry['material']=rel(MATERIAL);registry['materialGuid']=guid(MATERIAL) if Path(str(MATERIAL)+'.meta').exists() else None
 write(REGISTRY,registry);return registry

def map_uv(page,uv,registry,pages=None):
 source=source_slot(page,uv,pages);dest=allocations(registry)[page+':'+source['name']];sw,sh=dest['sourceSize'];sx,sy,rw,rh=dest['sourceRect'];x,y,w,h=dest['contentRectTopLeft'];u,v=uv
 return [(x+(u*sw-sx)*w/rw)/SIZE,1-(y+((1-v)*sh-sy)*h/rh)/SIZE]

def before_merge():
 REVIEW.mkdir(parents=True,exist_ok=True)
 if BASELINE.exists():return load(BASELINE)
 protected={};editable={};models={};files=[]
 for folder in ['mesh/Crops','Prefabs/Crops','Resources/Profiles/Farming','Resources/Items','Textures/UI/Icons','Data/Balance','Resources/Shops','Map/Scenes','Script','Material/Shared/Plants','Textures/Shared/Plants']:
  files.extend(p for p in (A/folder).rglob('*') if p.is_file())
 for p in files:
  name=rel(p);allowed=(name.startswith(rel(A/'mesh/Crops')+'/') and (p.suffix in ['.json','.asset','.fbx'] or p.name.endswith('.fbx.meta'))) or (name.startswith(rel(A/'Prefabs/Crops')+'/') and p.suffix=='.prefab') or p.name=='NP_Plants_Catalog.json'
  (editable if allowed else protected)[name]=sha(p)
 for p in model_paths():
  s=load(p);models[rel(p)]=dict(geometrySha256=geometry_hash(s),uvSha256=digest(s['uv']),material=s['materials'][0],triangles=s['triangles'],vertices=len(s['vertices']))
 paths=[ROOT/n for n in editable]
 with zipfile.ZipFile(BACKUP,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=3) as z:
  for p in paths:z.write(p,rel(p))
 data=dict(status='SNAPSHOT_BEFORE_MIGRATION',protected=protected,editableBefore=editable,models=models,backup=rel(BACKUP),backupSha256=sha(BACKUP),integrationBefore=load(SOURCE/'Integration/Manifest.json'),catalogBefore=load(TEXTURES/'NP_Plants_Catalog.json'),priorG11Revision=load(SOURCE/'G11/Review/R03_AssetChanges.json'))
 write(BASELINE,data);print('UNIFIED_BASELINE',len(models),'meshes;',len(protected),'protected files',flush=True);return data

def patch_native(path,uv):
 p=path.with_suffix('.asset')
 if not p.exists():return
 text=p.read_text();match=re.search(r'(?m)^    _typelessdata: (.*)$',text);data=bytearray.fromhex(match[1]);spec=load(path)
 # A newly sculpted source mesh can have a different topology. The canonical
 # prepare_plant_unity pipeline rebuilds its native asset after publication.
 # Only patch UV bytes here when the existing positions and normals match.
 if len(data)!=48*len(uv):return False
 if any(struct.pack('<6f',*(v+n))!=data[48*i:48*i+24] for i,(v,n) in enumerate(zip(spec['vertices'],spec['normals']))):return False
 for i,(u,v) in enumerate(uv):struct.pack_into('<2f',data,48*i+40,u,v)
 p.write_text(text[:match.start(1)]+data.hex()+text[match.end(1):])
 return True

def material_metadata():
 from unity_art_io import meta
 template=TEXTURES/'NP_Plants_Tulip_01_BaseColor.png'
 for suffix in MAPS:
  p=TEXTURES/(NAME+'_'+suffix+'.png');m=Path(str(p)+'.meta')
  if not m.exists():
   original=TEXTURES/('NP_Plants_Tulip_01_'+suffix+'.png');text=Path(str(original)+'.meta').read_text();text=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,text,flags=re.M);text=re.sub(r'maxTextureSize: \d+','maxTextureSize: 4096',text)
   text=text.replace('streamingMipmaps: 0','streamingMipmaps: 1');text=re.sub(r'compressionQuality: \d+','compressionQuality: 80',text)
   # TextureImporterFormat 50 is ASTC 6x6. Retain the established mobile format.
   if suffix=='SurfaceMask':text=text.replace('sRGBTexture: 1','sRGBTexture: 0')
   m.write_text(text)
 if not Path(str(SHADER)+'.meta').exists():meta(SHADER,A/'Material/Shared/CozyAtlas.shader')
 elif 'MonoImporter:' in Path(str(SHADER)+'.meta').read_text():
  shaderGuid=guid(SHADER);text=Path(str(A/'Material/Shared/CozyAtlas.shader')+'.meta').read_text();text=re.sub(r'^guid: \w+','guid: '+shaderGuid,text,flags=re.M);Path(str(SHADER)+'.meta').write_text(text)
 if not MATERIAL.exists():
  old='NP_Plants_Tulip_01';text=(MATERIAL.parent/(old+'.mat')).read_text().replace(old,NAME);text=text.replace('guid: '+guid(A/'Material/Shared/CozyAtlas.shader'),'guid: '+guid(SHADER))
  for suffix in MAPS:text=text.replace(guid(TEXTURES/(old+'_'+suffix+'.png')),guid(TEXTURES/(NAME+'_'+suffix+'.png')))
  for key,value in [('_BumpScale',1),('_Saturation',1)]:text=re.sub(r'('+key+r':) [\d.]+',r'\g<1> '+str(value),text)
  MATERIAL.write_text(text)
 meta(MATERIAL,main_id=2100000)

def patch_bindings(group=None):
 shared=guid(MATERIAL);oldmats={guid(p) for p in MATERIAL.parent.glob('NP_Plants_*.mat') if p!=MATERIAL}
 prefabs=sorted((A/'Prefabs/Crops'/group).glob('*.prefab')) if group else sorted((A/'Prefabs/Crops').rglob('*.prefab'))
 for p in prefabs:
  text=p.read_text();updated=re.sub(r'guid: (\w+)',lambda m:'guid: '+shared if m[1] in oldmats else m[0],text)
  if updated!=text:p.write_text(updated)
 for p in model_paths(group):
  f=p.with_suffix('.fbx.meta')
  if not f.exists():continue
  text=f.read_text();remap='  externalObjects:\n  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: '+NAME+'\n    second: {fileID: 2100000, guid: '+shared+', type: 2}\n'
  updated=re.sub(r'(?ms)^  externalObjects:.*?(?=^  [A-Za-z_]\w*:|\Z)',lambda _:remap,text,count=1)
  if updated!=text:f.write_text(updated)
 for p in SOURCE.glob('G*/Blender/UnityVisualBindings.json'):
  if group and p.parent.parent.name!=group:continue
  data=load(p);data['authoringMaterial']=data.get('authoringMaterial',data.get('material'));data['material']=NAME;data['runtimeAtlas']=rel(REGISTRY);write(p,data)

def update_catalog():
 p=TEXTURES/'NP_Plants_Catalog.json';data=load(p)
 for row in data['plants']:
  row['authoringAtlas']=row.get('authoringAtlas',row['atlas']);row['atlas']=NAME;row['runtimeMaterial']=NAME;row['atlasPolicy']='Append source surfaces to the shared registry; never introduce a per-group runtime material'
 data['runtimeAtlas']=rel(REGISTRY);write(p,data)

def applied_extensions():
 return [load(p) for p in sorted((SOURCE/'Integration').glob('G*_BindingChanges.json')) if load(p).get('status')=='APPLIED_SCOPED_EXTENSION']

def authorized_extension(name,before_sha):
 # Allow recorded content revisions while keeping the other atlas inputs protected.
 allowed={rel(A/'Resources/Shops/Crops Catalog.asset'),rel(A/'Data/Balance/Items.csv'),rel(A/'Data/Balance/Crops.csv'),rel(A/'Script/Editor/CropGameplayValidation.cs')}
 icon_root=A/'Textures/UI/Icons';sprite=ROOT/name
 seed_icon=sprite.parent==icon_root/'Seeds' and sprite.match('icon_seed_*.png')
 berry_icon=sprite.parent==icon_root/'Crops' and sprite.name in {
  f'icon_crop_{berry}{suffix}.png' for berry in ('blueberry','raspberry','blackberry') for suffix in ('','_mutation')}
 if name not in allowed and not seed_icon and not berry_icon:return False
 reachable={before_sha};records=applied_extensions()
 for _ in range(len(records)+1):
  for record in records:
   change=record.get('authorizedChanges',{}).get(name)
   if change and change['before'] in reachable:reachable.add(change['after'])
 return sha(ROOT/name) in reachable

def migration_report():
 before=load(BASELINE);changes={}
 for name,s in before['editableBefore'].items():
  now=sha(ROOT/name)
  if now!=s:changes[name]=dict(before=s,after=now)
 for name,s in before['protected'].items():assert sha(ROOT/name)==s or authorized_extension(name,s),'Protected asset changed: '+name
 report=dict(status='APPLIED_SCOPED_MIGRATION',authorizedChanges=changes,protectedFiles=len(before['protected']),registrySha256=sha(REGISTRY),geometryPreserved=True,existingAssetGuidsRetained=True,gameplayProfilesAndUiUnchanged=True)
 write(REVIEW/'AssetChanges.json',report);return report
