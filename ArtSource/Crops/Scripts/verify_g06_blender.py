"""Verify source volume, exact protected pixels, native buffers and FBX roundtrips."""
import sys,json,math,hashlib,re,struct
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g06_config import *

checks=[]
def check(ok,label):
    assert ok,label
    checks.append(label)
def read(p):return json.loads(p.read_text())
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]

baseline=read(REVIEW/'Baseline.json');backup=ROOT/baseline['snapshot']
mutable={str((TEXTURES/(ATLAS+'_'+s)).relative_to(ROOT)) for s in ['BaseColor.png','Normal.png','SurfaceMask.png','Slots.json']}
mutable.update(str((SOURCE/'Textures'/(ATLAS+'_'+s+'.xcf')).relative_to(ROOT)) for s in ['BaseColor','Normal','SurfaceMask'])
mutable.add(str((TEXTURES/'NP_Plants_Catalog.json').relative_to(ROOT)))
for name,sha in baseline['protected'].items():
    if name not in mutable:check(digest(ROOT/name)==sha,'Preserved '+name)

slots=read(TEXTURES/(ATLAS+'_Slots.json'));before=read(backup/TEXTURES.relative_to(ROOT)/(ATLAS+'_Slots.json'))
used=read(REVIEW/'ReferenceStudy.json')['previousUsedTiles']
check(not set(used).intersection(CHANGED),'All edited tiles were unused by previous Trellis meshes')
for a,b in zip(before['slots'],slots['slots']):
    if a['name'] in CHANGED:
        check(all(a[k]==b[k] for k in ['name','index','pixelRectTopLeft','uvSafe']),a['name']+' stable UV rectangle')
    else:check(a==b,a['name']+' unchanged metadata')

def pixels(p):
    im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color'
    a=np.empty(im.size[0]*im.size[1]*4,dtype=np.float32);im.pixels.foreach_get(a)
    check(tuple(im.size)==(2048,2048),p.name+' 2048 square');bpy.data.images.remove(im)
    return a.reshape(2048,2048,4)[::-1].copy()

allowed=np.zeros((2048,2048),dtype=bool)
for s in slots['slots']:
    if s['name'] in CHANGED:
        x,y,w,h=s['pixelRectTopLeft'];allowed[y:y+h,x:x+w]=True
pixel_reports={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
    p=TEXTURES/(ATLAS+'_'+suffix+'.png');a=pixels(backup/p.relative_to(ROOT));b=pixels(p)
    delta=np.max(np.abs(a-b),axis=2)
    check(not np.any(delta[~allowed]>1e-7),suffix+' every pixel outside edited tiles unchanged')
    pixel_reports[suffix]=dict(outsideChangedPixels=int(np.sum(delta[~allowed]>1e-7)),insideChangedPixels=int(np.sum(delta[allowed]>1e-7)))
    if suffix=='BaseColor':
        check(np.min(b[:,:,3])>.999,'Opaque atlas; foliage silhouette is real mesh geometry')
        for name in ['LeafLobed','LeafStrap']:
            s=next(s for s in slots['slots'] if s['name']==name);x,y,w,h=s['pixelRectTopLeft'];tile=b[y+20:y+h-20,x+20:x+w-20,:3]
            sat=(tile.max(axis=2)-tile.min(axis=2))/np.maximum(tile.max(axis=2),1e-7)
            check(float(sat.mean())>.48,name+' vivid source albedo')
    if suffix=='SurfaceMask':
        check(float(np.max(b[:,:,0]))==0,'Nonmetallic plants')
        for name in CHANGED:
            s=next(s for s in slots['slots'] if s['name']==name);x,y,w,h=s['pixelRectTopLeft']
            check(abs(float(b[y+h//2,x+w//2,3])-SMOOTH[name])<.008,name+' real smoothness in alpha')
    if suffix=='Normal':
        n=b[allowed,:3]*2-1;length=np.linalg.norm(n,axis=1)
        check(float(length.min())>.94 and float(length.max())<1.06,'Registered tangent normals have unit length')
    del a,b

old=read(backup/TEXTURES.relative_to(ROOT)/'NP_Plants_Catalog.json');catalog=read(TEXTURES/'NP_Plants_Catalog.json')
check(len(catalog['plants'])==31,'All 31 crop catalog identities retained')
for a,b in zip(old['plants'],catalog['plants']):
    check(a==b if a['group']!='G06' else all(a[k]==b[k] for k in a if k not in ['atlas','slots','artStatus']),a['displayName']+' catalog change boundary')

bpy.ops.wm.open_mainfile(filepath=str(MASTER));roots=[o for o in bpy.data.objects if o.get('Phase')]
check(len(roots)==12,'12 independent editable growth/harvest roots')
mat=bpy.data.materials[ATLAS];images={n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
check(len(images)==3,'Three packed production maps')
for im in images:
    check(im.packed_file and digest(TEXTURES/im.name)==hashlib.sha256(im.packed_file.data).hexdigest(),im.name+' packed current bytes')
volumes={}
for kind,phases in PHASES.items():
    check({r['Phase'] for r in roots if r['Plant']==kind}==set(phases),kind+' exact six phases')
    for stage in phases[2:5]:
        root=bpy.data.objects[kind+'_'+stage];bpy.context.view_layer.update()
        leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')==kind]
        points=np.array([tuple(root.matrix_world.inverted()@o.matrix_world@v.co) for o in leaves for v in o.data.vertices])
        span=np.ptp(points,axis=0);xy=np.cov(points[:,:2].T);eig=np.linalg.eigvalsh(xy)
        report=dict(leaves=len(leaves),width=float(span[0]),depth=float(span[1]),depthToWidth=float(span[1]/span[0]),horizontalCovarianceRatio=float(math.sqrt(max(0,eig[0])/eig[1])))
        volumes[kind+'_'+stage]=report
        if stage=='Ready':
            check(report['depthToWidth']>.65,kind+' mature foliage has substantial side depth')
            check(report['horizontalCovarianceRatio']>.44,kind+' mature crown is not planar')
        check(not any('vein' in o.name.lower() for o in root.children_recursive if o.type=='MESH'),kind+' '+stage+' fine veins remain in maps')
check(sum(bool(o.get('HollowTip')) for o in bpy.data.objects['Onion_Ready'].children_recursive)==1,'Mature onion open tip includes a real recessed inner wall')
check(not any(o.get('ReferenceLeaf') for o in bpy.data.objects['Onion_Harvest'].children_recursive),'Harvest onion has dry neck and no green leaves')
check(sum(o.get('ReferenceLeaf')=='Radish' for o in bpy.data.objects['Radish_Harvest'].children_recursive)==6,'Whole harvested radish keeps its foliage')

specs={p.stem:read(p) for p in MODELS.glob('*.json')}
check(len(specs)==28,'14 base meshes and 14 distance meshes')
material=guid(A/'Material/Shared/Plants'/(ATLAS+'.mat'))
safe=[s['uvSafe'] for s in slots['slots']]
for name,s in specs.items():
    check(s['materials']==[ATLAS] and len(s['submeshes'])==1,name+' one shared material')
    check(len(s['vertices'])==len(s['normals'])==len(s['uv'])>0,name+' complete channels')
    check(all(math.isfinite(v) for key in ['vertices','normals','uv'] for row in s[key] for v in row),name+' finite channels')
    indices=s['submeshes'][0]
    check(len(indices)==3*s['triangles'] and min(indices)>=0 and max(indices)<len(s['vertices']),name+' valid indices')
    check(all(any(a-1e-6<=u<=c+1e-6 and b-1e-6<=v<=d+1e-6 for a,b,c,d in safe) for u,v in s['uv']),name+' padded UVs')
    check(s['min'][1]>-.015,name+' ground pivot')
    asset=(MODELS/(name+'.asset')).read_text()
    raw=bytes.fromhex(re.search(r'^  m_IndexBuffer: (.*)$',asset,re.M)[1]);check(list(struct.unpack('<'+'I'*(len(raw)//4),raw))==indices,name+' native triangle buffer')
    raw=bytes.fromhex(re.search(r'^    _typelessdata: (.*)$',asset,re.M)[1]);check(len(raw)==48*len(s['vertices']),name+' native vertex stride')
    for i in [0,len(s['vertices'])//2,len(s['vertices'])-1]:
        values=struct.unpack_from('<12f',raw,48*i)
        check(max(abs(a-b) for a,b in zip(values[:3],s['vertices'][i]))<1e-5 and max(abs(a-b) for a,b in zip(values[-2:],s['uv'][i]))<1e-5,name+' native sampled position/UV '+str(i))
    for ext in ['.asset','.json','.fbx']:check(Path(str(MODELS/(name+ext))+'.meta').is_file(),name+ext+' Unity metadata')
    if name.endswith('_LOD1'):continue
    low=specs[name+'_LOD1'];check(low['triangles']<s['triangles'] and low['triangles']<=2800,name+' distance mesh budget')
    prefab=A/'Prefabs/Crops/G06'/(name+'.prefab');body=prefab.read_text()
    check(body.count('guid: '+material)==2 and len(re.findall(r'^--- !u!23 ',body,re.M))==2 and 'm_LODs:' in body,name+' two-LOD prefab')
    check(all(guid(MODELS/(n+'.asset')) in body for n in [name,name+'_LOD1']),name+' native mesh links')

roundtrip=[]
for p in sorted(MODELS.glob('*.fbx')):
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(p));new=set(bpy.data.objects)-before;meshes=[o for o in new if o.type=='MESH']
    check(len(meshes)==1,p.name+' one imported mesh');o=meshes[0];s=specs[p.stem]
    check(sum(len(f.vertices)-2 for f in o.data.polygons)==s['triangles'],p.name+' triangle roundtrip')
    check(len(o.data.uv_layers)==len(o.data.materials)==1,p.name+' UV/material roundtrip')
    check(max(abs(a-b) for a,b in zip(sorted(o.dimensions),sorted(y-x for x,y in zip(s['min'],s['max']))))<.001,p.name+' scale roundtrip')
    roundtrip.append(dict(file=p.name,triangles=s['triangles']))
    for o in new:bpy.data.objects.remove(o,do_unlink=True)
bindings=read(MASTER.parent/'UnityVisualBindings.json')
for kind,rows in bindings['plants'].items():
    check([r['stage'] for r in rows]==PHASES[kind],kind+' phase binding order')
    for r in rows:check(guid(ROOT/r['prefab'])==r['guid'],kind+' '+r['stage']+' prefab resolves')

report=dict(status='PASS',group='G06',checks=len(checks),phaseAppearances=12,baseModels=14,meshesWithLods=28,verifiedFbx=len(roundtrip),newAtlasPages=0,atlas=ATLAS,changedPreviouslyUnusedSlots=CHANGED,protectedFiles=len(baseline['protected']),previousAssetsRetained=True,atlasPixels=pixel_reports,volumes=volumes,baseTriangleCounts={n:s['triangles'] for n,s in specs.items() if not n.endswith('_LOD1')},masterSha256=digest(MASTER),roundtrip=roundtrip,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual comparison against two visible user attachments; no automated pixel-identical comparison')
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2))
(REVIEW/'BlenderReopenAudit.json').write_text(json.dumps(dict(status='PASS',masterSha256=digest(MASTER),verifiedFbx=len(roundtrip),phaseRoots=12,packedMaps=3),indent=2))
(REVIEW/'BatchStatus.json').write_text(json.dumps(dict(status='PENDING_UNITY_VALIDATION',assetValidation='PASS',unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',checks=len(checks)),indent=2))
print('G06_AUDIT_PASS',len(checks),'checks',json.dumps(volumes),flush=True)
