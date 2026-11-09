"""Prepare deterministic Unity assets from Blender's explicit mesh export.

Uses the checked-in Unity 6 scanner Mesh serialization layout and the same shared
material/texture importer settings. The native editor import command can rebuild
all meshes and the prefab from the JSON without relying on this serializer.
"""
from pathlib import Path
import json, math, re, struct, uuid, hashlib

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'
MODELS=A/'mesh/Props/FertilizerMaker'
ATLAS=A/'Textures/Shared/Cozy'
REVIEW=ROOT/'ArtSource/FertilizerMaker/Review'
PREFAB=A/'Resources/World/FertilizerMaker_Dummy.prefab'
REVIEW.mkdir(parents=True,exist_ok=True)

def meta(path,template=None,main_id=None,folder=False):
    destination=Path(str(path)+'.meta')
    if destination.exists():return re.search(r'^guid: (\w+)',destination.read_text(),re.M)[1]
    guid=uuid.uuid4().hex
    if template:
        text=Path(str(template)+'.meta').read_text()
        text=re.sub(r'^guid: \w+','guid: '+guid,text,flags=re.M)
    elif folder:
        text='fileFormatVersion: 2\nguid: '+guid+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif main_id:
        text='fileFormatVersion: 2\nguid: '+guid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: '+str(main_id)+'\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        text='fileFormatVersion: 2\nguid: '+guid+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    destination.write_text(text,encoding='utf-8')
    return guid

def v3(values):return '{x: '+format(values[0],'.9g')+', y: '+format(values[1],'.9g')+', z: '+format(values[2],'.9g')+'}'
def bounds(vertices):
    lo=[min(v[i] for v in vertices) for i in range(3)];hi=[max(v[i] for v in vertices) for i in range(3)]
    return [(a+b)/2 for a,b in zip(lo,hi)],[(b-a)/2 for a,b in zip(lo,hi)]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def tangents(spec):
    vertices=spec['vertices'];uv=spec['uv'];tan=[[0.,0.,0.] for _ in vertices];bit=[[0.,0.,0.] for _ in vertices]
    for sub in spec['submeshes']:
        for at in range(0,len(sub),3):
            ids=sub[at:at+3];p,q,r=[vertices[i] for i in ids];u,v,w=[uv[i] for i in ids]
            e1=[q[i]-p[i] for i in range(3)];e2=[r[i]-p[i] for i in range(3)]
            a,b=v[0]-u[0],v[1]-u[1];c,d=w[0]-u[0],w[1]-u[1];det=a*d-b*c
            if abs(det)<1e-12:continue
            t=[(d*e1[i]-b*e2[i])/det for i in range(3)];bt=[(a*e2[i]-c*e1[i])/det for i in range(3)]
            for index in ids:
                for i in range(3):tan[index][i]+=t[i];bit[index][i]+=bt[i]
    result=[]
    for n,t,b in zip(spec['normals'],tan,bit):
        projection=dot(n,t);t=[t[i]-n[i]*projection for i in range(3)];length=math.sqrt(dot(t,t))
        if length<1e-8:
            t=cross([0,1,0] if abs(n[1])<.9 else [1,0,0],n);length=math.sqrt(dot(t,t))
        t=[x/max(length,1e-8) for x in t]
        result.append(t+[-1 if dot(cross(n,t),b)<0 else 1])
    return result

# Only new atlas 05 is created. Material 01 and all earlier atlas bytes are untouched.
texture_ids={}
for suffix in ['BaseColor','Normal','SurfaceMask']:
    texture_ids[suffix]=meta(ATLAS/f'NP_CozySurfaces_05_{suffix}.png',ATLAS/f'NP_CozySurfaces_04_{suffix}.png')
meta(ATLAS/'NP_CozyAtlasSlots_05.json',ATLAS/'NP_CozyAtlasSlots_04.json')
material_path=A/'Material/Shared/NP_CozySurfaces_05.mat'
material=(A/'Material/Shared/NP_CozySurfaces_04.mat').read_text().replace('NP_CozySurfaces_04','NP_CozySurfaces_05')
for suffix,guid in texture_ids.items():
    old=re.search(r'^guid: (\w+)',Path(str(ATLAS/f'NP_CozySurfaces_04_{suffix}.png')+'.meta').read_text(),re.M)[1]
    material=material.replace(old,guid)
material_path.write_text(material)
material_ids=[meta(A/'Material/Shared/NP_CozySurfaces_01.mat',main_id=2100000),meta(material_path,main_id=2100000)]
meta(MODELS,folder=True)
meta(A/'Script/Editor/FertilizerMakerArtSetup.cs')

template=(A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
mesh_ids=[];specs=[]
for lod in range(3):
    name='FertilizerMaker_Cozy_LOD'+str(lod)
    spec=json.loads((MODELS/(name+'.json')).read_text());specs.append(spec)
    assert len(spec['vertices'])==len(spec['normals'])==len(spec['uv'])
    assert all(all(math.isfinite(x) for x in v) for v in spec['vertices']+spec['normals']+spec['uv'])
    assert all(0<x<1 for uv in spec['uv'] for x in uv)
    assert sum(len(s) for s in spec['submeshes'])==spec['triangles']*3
    text=template.replace('SoilScanner_Cozy_LOD2',name)
    submeshes='  m_SubMeshes:\n';offset=0;indices=[]
    for sub in spec['submeshes']:
        assert sub and min(sub)>=0 and max(sub)<len(spec['vertices'])
        centre,extent=bounds([spec['vertices'][index] for index in set(sub)])
        submeshes+=('  - serializedVersion: 2\n    firstByte: '+str(offset)+'\n    indexCount: '+str(len(sub))+\
            '\n    topology: 0\n    baseVertex: 0\n    firstVertex: '+str(min(sub))+'\n    vertexCount: '+str(max(sub)-min(sub)+1)+\
            '\n    localAABB:\n      m_Center: '+v3(centre)+'\n      m_Extent: '+v3(extent)+'\n')
        offset+=len(sub)*4;indices.extend(sub)
    text=re.sub(r'(?s)  m_SubMeshes:\n.*?(?=  m_Shapes:)',lambda _:submeshes,text)
    index_bytes=struct.pack('<'+'I'*len(indices),*indices)
    text=re.sub(r'(?m)^  m_IndexBuffer:.*$',lambda _:'  m_IndexBuffer: '+index_bytes.hex(),text)
    data=bytearray()
    for p,n,t,uv in zip(spec['vertices'],spec['normals'],tangents(spec),spec['uv']):data.extend(struct.pack('<12f',*p,*n,*t,*uv))
    text=re.sub(r'm_VertexCount: \d+','m_VertexCount: '+str(len(spec['vertices'])),text)
    text=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(len(data)),text)
    text=re.sub(r'(?m)^    _typelessdata:.*$',lambda _:'    _typelessdata: '+data.hex(),text)
    centre,extent=bounds(spec['vertices'])
    text=re.sub(r'(?s)  m_LocalAABB:\n.*?(?=  m_MeshUsageFlags:)',lambda _:'  m_LocalAABB:\n    m_Center: '+v3(centre)+'\n    m_Extent: '+v3(extent)+'\n',text)
    destination=MODELS/(name+'.asset');destination.write_text(text)
    mesh_ids.append(meta(destination,main_id=4300000))
    meta(MODELS/(name+'.json'),A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD0.json')
    meta(MODELS/(name+'.fbx'),A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD0.fbx')
    fbx_meta=Path(str(MODELS/(name+'.fbx'))+'.meta')
    remaps='  externalObjects:\n'
    for page,material_guid in zip(['01','05'],material_ids):
        remaps+='  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: NP_CozySurfaces_'+page+'\n    second: {fileID: 2100000, guid: '+material_guid+', type: 2}\n'
    fbx_meta.write_text(fbx_meta.read_text().replace('  externalObjects: {}\n',remaps))
    print('UNITY_MESH_PREPARED '+name+' triangles='+str(spec['triangles']),flush=True)

# Retain all original root component IDs, processor/save fields and prefab GUID.
before=PREFAB.read_text();guid_before=meta(PREFAB)
blocks={(int(kind),int(identifier)):body for kind,identifier,body in re.findall(r'(?ms)^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',before)}
root_go=4465165886248734535;root_transform=4118292089790204869
visual_go=1400122686715594423;visual_transform=1294710535316777146;lod_group=900100
body=blocks[(1,visual_go)]
body=re.sub(r'(?s)  m_Component:.*?(?=  m_Layer:)',lambda _:'  m_Component:\n  - component: {fileID: '+str(visual_transform)+'}\n  - component: {fileID: '+str(lod_group)+'}\n',body)
body=re.sub(r'(?m)^  m_Name:.*$','  m_Name: Visual_FertilizerMaker_Cozy',body);blocks[(1,visual_go)]=body
body=blocks[(4,visual_transform)]
body=re.sub(r'(?m)^  m_LocalPosition:.*$','  m_LocalPosition: {x: 0, y: 0, z: 0}',body)
body=re.sub(r'(?m)^  m_LocalScale:.*$','  m_LocalScale: {x: 1, y: 1, z: 1}',body)
body=re.sub(r'(?s)  m_Children:.*?(?=  m_Father:)',lambda _:'  m_Children:\n'+''.join('  - {fileID: '+str(910010+i)+'}\n' for i in range(3)),body)
blocks[(4,visual_transform)]=body
renderer_template=blocks.pop((23,7035662756761337974),None)
if renderer_template is None:renderer_template=blocks[(23,910030)]
blocks.pop((33,3057595517201214120),None)
centre,extent=bounds(specs[0]['vertices']);size=[x*2 for x in extent]
for lod,guid in enumerate(mesh_ids):
    go=910000+lod;transform=910010+lod;mesh_filter=910020+lod;renderer=910030+lod
    blocks[(1,go)]=('GameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n'+
        ''.join('  - component: {fileID: '+str(i)+'}\n' for i in [transform,mesh_filter,renderer])+\
        '  m_Layer: 0\n  m_Name: FertilizerMaker_Cozy_LOD'+str(lod)+'\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n')
    blocks[(4,transform)]=('Transform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+str(go)+'}\n  serializedVersion: 2\n'+
        '  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {fileID: '+str(visual_transform)+'}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n')
    blocks[(33,mesh_filter)]=('MeshFilter:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+str(go)+'}\n  m_Mesh: {fileID: 4300000, guid: '+guid+', type: 2}\n')
    renderer_body=re.sub(r'(?m)^  m_GameObject:.*$','  m_GameObject: {fileID: '+str(go)+'}',renderer_template)
    renderer_body=re.sub(r'(?s)  m_Materials:.*?(?=  m_StaticBatchInfo:)',lambda _:'  m_Materials:\n'+''.join('  - {fileID: 2100000, guid: '+m+', type: 2}\n' for m in material_ids),renderer_body)
    blocks[(23,renderer)]=renderer_body
group='LODGroup:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+str(visual_go)+'}\n  serializedVersion: 2\n  m_LocalReferencePoint: '+v3(centre)+'\n  m_Size: '+format(max(size),'.9g')+'\n  m_FadeMode: 0\n  m_AnimateCrossFading: 0\n  m_LastLODIsBillboard: 0\n  m_LODs:\n'
for lod,height in enumerate([.34,.14,.025]):group+='  - screenRelativeHeight: '+str(height)+'\n    fadeTransitionWidth: 0\n    renderers:\n    - renderer: {fileID: '+str(910030+lod)+'}\n'
blocks[(205,lod_group)]=group+'  m_Enabled: 1\n'
blocks[(1,root_go)]=re.sub(r'(?m)^  m_Name:.*$','  m_Name: FertilizerMaker',blocks[(1,root_go)])
collider=blocks[(65,3562064985455766512)]
collider=re.sub(r'(?m)^  m_Size:.*$','  m_Size: '+v3(size),collider)
collider=re.sub(r'(?m)^  m_Center:.*$','  m_Center: '+v3(centre),collider)
blocks[(65,3562064985455766512)]=collider
PREFAB.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join('--- !u!'+str(kind)+' &'+str(identifier)+'\n'+body for (kind,identifier),body in blocks.items()))
assert meta(PREFAB)==guid_before
assert 'processorId: farm-fertilizer-processor' in PREFAB.read_text()
assert len(re.findall(r'^--- !u!65 ',PREFAB.read_text(),re.M))==1
assert len(re.findall(r'^--- !u!23 ',PREFAB.read_text(),re.M))==3
(REVIEW/'PreparedArtAudit.json').write_text(json.dumps(dict(status='PASS',prefabGuid=guid_before,colliders=1,lodRenderers=3,
    triangles=[s['triangles'] for s in specs],bounds=dict(center=centre,size=size),atlasMaterials=['01','05'],
    validation='Serialized buffers, attributes, indices, UVs, bindings and prefab identity; native import is checked by the editor command.'),indent=2))
(ROOT/'ArtSource/FertilizerMaker/ApplyArt.request').write_text('apply')
print('FERTILIZER_UNITY_ASSETS_PREPARED prefab/save GUID retained; 3 LODs; 2 shared materials',flush=True)
