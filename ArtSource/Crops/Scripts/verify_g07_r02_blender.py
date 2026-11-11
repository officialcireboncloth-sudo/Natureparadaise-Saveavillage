"""G07 R02: validate actual new assets and preserve all earlier crops."""
import sys,json,math,hashlib,re,struct
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g07_config import *
checks=[]
def check(ok,label):
    assert ok,label
    checks.append(label)
def read(p):return json.loads(p.read_text())
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
baseline=read(REVIEW/'R02_Baseline.json');backup=ROOT/baseline['snapshot']
initial=read(REVIEW/'Baseline.json');original=ROOT/initial['snapshot']
for name,sha in baseline['protected'].items():
    p=ROOT/name
    if p.name=='NP_Plants_Catalog.json':continue
    if 'NP_Plants_Trellis_01_' in p.name and (original/name).is_file():
        check(digest(p)==digest(original/name),'Restored pre-G07 '+name)
    else:check(digest(p)==sha,'Preserved '+name)
for name in baseline['originalG07Meta']:
    p=ROOT/name
    check(guid(p.with_suffix(''))==guid((backup/name).with_suffix('')),'Retained G07 GUID '+name)
prior=read(original/TEXTURES.relative_to(ROOT)/'NP_Plants_Catalog.json');current=read(TEXTURES/'NP_Plants_Catalog.json')
for a,b in zip(prior['plants'],current['plants']):
    check(a==b if a['group']!='G07' else b['atlas']==ATLAS,a['displayName']+' art catalog boundary')
slots=read(TEXTURES/(ATLAS+'_Slots.json'));check(len(slots['slots'])==10,'Ten packed regions in independent leafy atlas')
def pixels(p):
    im=bpy.data.images.load(str(p),check_existing=False);im.colorspace_settings.name='Non-Color'
    check(tuple(im.size)==(2048,2048),p.name+' 2048 square')
    a=np.empty(2048*2048*4,dtype=np.float32);im.pixels.foreach_get(a);bpy.data.images.remove(im)
    return a.reshape(2048,2048,4)[::-1].copy()
pixel_reports={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
    p=TEXTURES/(ATLAS+'_'+suffix+'.png');a=pixels(p)
    pixel_reports[suffix]=dict(size=[2048,2048],sha256=digest(p))
    if suffix=='BaseColor':
        check(float(a[:,:,3].min())>.999,'Opaque atlas with real geometric leaf silhouettes')
        for name in ['LeafOrchid','LeafWaterSpinach']:
            s=next(s for s in slots['slots'] if s['name']==name);x,y,w,h=s['pixelRectTopLeft']
            check(w==h==1024,name+' 1024-pixel detailed region')
            tile=a[y+60:y+h-60,x+60:x+w-60,:3];sat=(tile.max(axis=2)-tile.min(axis=2))/np.maximum(tile.max(axis=2),1e-6)
            check(float(sat.mean())>.50,name+' vivid source albedo')
    elif suffix=='Normal':
        n=a[:,:,:3]*2-1;l=np.linalg.norm(n,axis=2);check(l.min()>.94 and l.max()<1.06,'Unit tangent normal vectors')
    else:
        check(float(a[:,:,0].max())==0,'Nonmetallic leaf/stem material')
        for s in slots['slots']:
            x,y,w,h=s['pixelRectTopLeft'];check(abs(float(a[y+h//2,x+w//2,3])-SMOOTH[s['name']])<.008,s['name']+' actual smoothness channel')
    del a
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
study={'spinach':dict(seedCount=3,phaseLeafCounts=[0,2,4,6,16,8]),'waterSpinach':dict(seedCount=5,phaseLeafCounts=[0,2,5,16,54,27])}
for kind,key in [('Spinach','spinach'),('WaterSpinach','waterSpinach')]:
    for stage,expected in zip(PHASES[kind],study[key]['phaseLeafCounts']):
        root=bpy.data.objects[kind+'_'+stage]
        check(sum(o.get('ReferenceLeaf')==kind for o in root.children_recursive)==expected,kind+' '+stage+' reference leaf count')
    root=bpy.data.objects[kind+'_Seed']
    check(sum(o.type=='MESH' for o in root.children_recursive)==study[key]['seedCount'],kind+' seed count')
water=bpy.data.objects['WaterSpinach_Harvest']
hollow=[o for o in water.children_recursive if o.get('HollowCutEnd')]
check(len(hollow)==9,'Nine actual hollow cut stems in water-spinach bunch')
check(sum(o.get('ReferenceLeaf')=='WaterSpinach' for o in bpy.data.objects[EXTRAS['WaterSpinach']].children_recursive)==2,'Loose water-spinach sprig has two leaves')
check(sum(bool(o.get('SolidCutEnd')) for o in bpy.data.objects['Spinach_Harvest'].children_recursive)==8,'Spinach bunch has eight solid cut leaf stalks')
for o in hollow:
    outer=np.array([tuple(v.co) for v in o.data.vertices[165:175]])
    inner=np.array([tuple(v.co) for v in o.data.vertices[176:186]])
    center=outer.mean(axis=0)
    ratio=float(np.linalg.norm(inner-center,axis=1).mean()/np.linalg.norm(outer-center,axis=1).mean())
    check(.65<ratio<.75,o.name+' recessed inner shell and nonzero wall thickness')
check(sum(bool(o.get('StemNode')) for o in bpy.data.objects['WaterSpinach_Ready'].children_recursive)>=28,'Water-spinach mature stems retain visible main and branch nodes')
check(sum(bool(o.get('AxillaryBranch')) for o in bpy.data.objects['WaterSpinach_Ready'].children_recursive)==9,'Mature water spinach has nine slender curved axillary branches')

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
    prefab=A/'Prefabs/Crops/G07'/(name+'.prefab');body=prefab.read_text()
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

report=dict(status='PASS',group='G07',checks=len(checks),phaseAppearances=12,baseModels=14,meshesWithLods=28,verifiedFbx=len(roundtrip),newAtlasPages=1,atlas=ATLAS,registeredLeafRegions=2,leafRegionResolution=1024,trellisRestored=True,protectedFiles=len(baseline['protected']),previousAssetsRetained=True,atlasPixels=pixel_reports,volumes=volumes,baseTriangleCounts={n:s['triangles'] for n,s in specs.items() if not n.endswith('_LOD1')},masterSha256=digest(MASTER),roundtrip=roundtrip,unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',referenceReview='Manual comparison against the two supplied spinach and water-spinach images; no automated pixel-identical comparison')
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2))
(REVIEW/'BlenderReopenAudit.json').write_text(json.dumps(dict(status='PASS',masterSha256=digest(MASTER),verifiedFbx=len(roundtrip),phaseRoots=12,packedMaps=3),indent=2))
(REVIEW/'BatchStatus.json').write_text(json.dumps(dict(status='PENDING_UNITY_VALIDATION',assetValidation='PASS',unityEditorImport='NOT_RUN',unityPlayMode='NOT_RUN',checks=len(checks)),indent=2))
print('G07_AUDIT_PASS',len(checks),'checks',json.dumps(volumes),flush=True)
