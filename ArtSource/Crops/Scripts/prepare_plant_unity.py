"""Prepare plant materials, native Mesh assets and visual-only LOD prefabs.
Existing crop profiles and balance remain intact until art integration is requested.
"""
import json,re,struct,sys,argparse,subprocess
from pathlib import Path
from plant_library import *
from unity_art_io import meta,v3,bounds,tangents

parser=argparse.ArgumentParser();parser.add_argument('--group',default='G05');args=parser.parse_args()
GROUP=args.group
assert re.fullmatch(r'G\d{2}',GROUP),'Expected a crop group such as G12'
group_manifest=SOURCE/GROUP/'Blender'/f'{GROUP}_Manifest.json'
MAIN_MATERIAL=json.loads(group_manifest.read_text()).get('material','NP_Plants_Produce_01') if group_manifest.is_file() else 'NP_Plants_Produce_01'
from unified_plant_atlas import REGISTRY,NAME as SHARED_PLANT_MATERIAL,BLENDER
if REGISTRY.exists():
 # Publish every future crop group through the same stable UV registry.
 result=subprocess.run([str(BLENDER),'-b','--threads','4','--python',str(SOURCE/'Scripts/publish_unified_plant_atlas.py'),'--','--group',GROUP],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
 (SOURCE/'Unified/Review'/('Prepare_'+GROUP+'.log')).write_text(result.stdout,encoding='utf-8')
 assert result.returncode==0 and 'UNIFIED_PUBLISH_PASS' in result.stdout and 'Traceback' not in result.stdout,'Shared atlas publication failed; see Unified/Review/Prepare_'+GROUP+'.log'
 MAIN_MATERIAL=SHARED_PLANT_MATERIAL
MODELS=A/('mesh/Crops/'+GROUP)
MATS=A/'Material/Shared/Plants'
PREFABS=A/('Prefabs/Crops/'+GROUP)
for p in [MODELS,MATS,PREFABS]:p.mkdir(parents=True,exist_ok=True)
for p in [TEXTURES,TEXTURES.parent,MATS,MODELS,MODELS.parent,PREFABS]:
 if not Path(str(p)+'.meta').exists():meta(p,folder=True)

material_ids={}
for page in PAGES:
 name='NP_Plants_'+page+'_01';guids={}
 existing=MATS/(name+'.mat')
 if existing.is_file():
  material_ids[name]=meta(existing,main_id=2100000)
  continue
 for suffix in ['BaseColor','Normal','SurfaceMask']:
  dest=TEXTURES/(name+'_'+suffix+'.png');assert dest.is_file(),dest
  guids[suffix]=meta(dest,A/f'Textures/Shared/Cozy/NP_CozySurfaces_04_{suffix}.png')
  p=Path(str(dest)+'.meta');s=p.read_text()
  s=re.sub(r'maxTextureSize: \d+','maxTextureSize: 1024',s)
  if suffix!='BaseColor':s=s.replace('sRGBTexture: 1','sRGBTexture: 0')
  p.write_text(s)
 base=A/'Material/Shared/NP_CozySurfaces_04.mat';s=base.read_text().replace('NP_CozySurfaces_04',name)
 for suffix,guid in guids.items():
  old=re.search(r'^guid: (\w+)',(A/f'Textures/Shared/Cozy/NP_CozySurfaces_04_{suffix}.png.meta').read_text(),re.M)[1]
  s=s.replace(old,guid)
 s=re.sub(r'(- _MainTex:\s*\n\s*m_Texture:) \{[^}]+\}',lambda m:m[1]+' {fileID: 2800000, guid: '+guids['BaseColor']+', type: 3}',s)
 s=s.replace('_BumpScale: 0.55','_BumpScale: 0.3').replace('_Wrap: 0.12','_Wrap: 0.25')
 dest=MATS/(name+'.mat');dest.write_text(s);material_ids[name]=meta(dest,main_id=2100000)
for p in TEXTURES.glob('*.json'):
 if not Path(str(p)+'.meta').exists():
  guid=meta(p,TEXTURES.parent/'Cozy/NP_CozyAtlasSlots_04.json')

if MAIN_MATERIAL not in material_ids:
 dest=MATS/(MAIN_MATERIAL+'.mat');assert dest.is_file(),dest
 material_ids[MAIN_MATERIAL]=meta(dest,main_id=2100000)

template=(A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
mesh_guids={};specs={}
for p in sorted(MODELS.glob('*.json')):
 spec=json.loads(p.read_text());name=spec['name'];specs[name]=spec
 assert len(spec['submeshes'])==1
 assert len(spec['vertices'])==len(spec['normals'])==len(spec['uv'])
 assert sum(len(v) for v in spec['submeshes'])==spec['triangles']*3
 text=template.replace('SoilScanner_Cozy_LOD2',name)
 centre,extent=bounds(spec['vertices']);indices=spec['submeshes'][0]
 subs='  m_SubMeshes:\n  - serializedVersion: 2\n    firstByte: 0\n    indexCount: '+str(len(indices))+'\n    topology: 0\n    baseVertex: 0\n    firstVertex: 0\n    vertexCount: '+str(len(spec['vertices']))+'\n    localAABB:\n      m_Center: '+v3(centre)+'\n      m_Extent: '+v3(extent)+'\n'
 text=re.sub(r'(?s)  m_SubMeshes:\n.*?(?=  m_Shapes:)',lambda _:subs,text)
 text=re.sub(r'(?m)^  m_IndexBuffer:.*$',lambda _:'  m_IndexBuffer: '+struct.pack('<'+'I'*len(indices),*indices).hex(),text)
 data=bytearray()
 for point,normal,tangent,uv in zip(spec['vertices'],spec['normals'],tangents(spec),spec['uv']):data.extend(struct.pack('<12f',*point,*normal,*tangent,*uv))
 text=re.sub(r'm_VertexCount: \d+','m_VertexCount: '+str(len(spec['vertices'])),text)
 text=re.sub(r'm_DataSize: \d+','m_DataSize: '+str(len(data)),text)
 text=re.sub(r'(?m)^    _typelessdata:.*$',lambda _:'    _typelessdata: '+data.hex(),text)
 text=re.sub(r'(?s)  m_LocalAABB:\n.*?(?=  m_MeshUsageFlags:)',lambda _:'  m_LocalAABB:\n    m_Center: '+v3(centre)+'\n    m_Extent: '+v3(extent)+'\n',text)
 dest=MODELS/(name+'.asset');dest.write_text(text);mesh_guids[name]=meta(dest,main_id=4300000)
 meta(p,A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD0.json')
 fbx=MODELS/(name+'.fbx');meta(fbx,A/'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD0.fbx')
 f=Path(str(fbx)+'.meta');s=f.read_text()
 remap='  externalObjects:\n  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: '+MAIN_MATERIAL+'\n    second: {fileID: 2100000, guid: '+material_ids[MAIN_MATERIAL]+', type: 2}\n'
 # A sequence entry also starts with two spaces. Match the next mapping key,
 # otherwise repeated preparation prepends another copy of the remap each time.
 if remap not in s:
  s=re.sub(r'(?ms)^  externalObjects:.*?(?=^  [A-Za-z_]\w*:|\Z)',lambda _:remap,s,count=1)
  f.write_text(s)

renderer_source=(A/'Pack/Toon Series/Toon Farm Pack/Prefabs/Vegetation/Agricultural Plants/TFP_Cabbage_Plant_03A.prefab').read_text()
renderer_template=re.search(r'(?ms)^--- !u!23 &[^\n]+\n(.*?)(?=^--- !u!|\Z)',renderer_source)[1]

def go(name,ids):
 return 'GameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n'+''.join('  - component: {fileID: '+str(i)+'}\n' for i in ids)+'  m_Layer: 0\n  m_Name: '+name+'\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'
def transform(go_id,parent,children):
 return 'Transform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+str(go_id)+'}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children:'+ ('\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in children) if children else ' []\n')+'  m_Father: {fileID: '+str(parent)+'}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'

prefab_ids={}
for name,spec in specs.items():
 if name.endswith('_LOD1'):continue
 assert name+'_LOD1' in specs,name
 assert specs[name+'_LOD1']['triangles']<spec['triangles']
 blocks=[(1,100,go(name,[400,2050])),(4,400,transform(100,0,[410,411]))]
 for lod in range(2):
  n=name+('_LOD1' if lod else '');go_id=110+lod;tr=410+lod;mf=330+lod;mr=230+lod
  blocks.extend([(1,go_id,go(n,[tr,mf,mr])),(4,tr,transform(go_id,400,[]))])
  blocks.append((33,mf,'MeshFilter:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: '+str(go_id)+'}\n  m_Mesh: {fileID: 4300000, guid: '+mesh_guids[n]+', type: 2}\n'))
  r=re.sub(r'(?m)^  m_GameObject:.*$','  m_GameObject: {fileID: '+str(go_id)+'}',renderer_template)
  r=re.sub(r'(?s)  m_Materials:.*?(?=  m_StaticBatchInfo:)',lambda _:'  m_Materials:\n  - {fileID: 2100000, guid: '+material_ids[MAIN_MATERIAL]+', type: 2}\n',r)
  blocks.append((23,mr,r))
 c,e=bounds(spec['vertices']);size=max(e)*2
 group='LODGroup:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 100}\n  serializedVersion: 2\n  m_LocalReferencePoint: '+v3(c)+'\n  m_Size: '+str(size)+'\n  m_FadeMode: 0\n  m_AnimateCrossFading: 0\n  m_LastLODIsBillboard: 0\n  m_LODs:\n  - screenRelativeHeight: 0.13\n    fadeTransitionWidth: 0\n    renderers:\n    - renderer: {fileID: 230}\n  - screenRelativeHeight: 0.008\n    fadeTransitionWidth: 0\n    renderers:\n    - renderer: {fileID: 231}\n  m_Enabled: 1\n'
 blocks.append((205,2050,group));dest=PREFABS/(name+'.prefab')
 dest.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join('--- !u!'+str(k)+' &'+str(i)+'\n'+body for k,i,body in blocks))
 prefab_ids[name]=meta(dest,A/'Resources/World/FertilizerMaker_Dummy.prefab')

mapping={}
if GROUP=='G05':
 for plant in ['Cabbage','Broccoli','Cauliflower']:
  mapping[plant]=[dict(stage=stage,prefab=str((PREFABS/(name+'.prefab')).relative_to(ROOT)),guid=prefab_ids[name],fileID=100,scale=[1,1,1]) for stage,name in [('Seed','NP_Shared_Seed'),('Sprout','NP_Shared_Sprout'),('Young','NP_'+plant+'_Young'),('Head Forming','NP_'+plant+'_HeadForming'),('Ready','NP_'+plant+'_Ready'),('Harvest','NP_'+plant+'_Harvest')]]
else:
 plan=json.loads((SOURCE/GROUP/'Blender'/f'{GROUP}_VisualPlan.json').read_text())
 for plant,stages in plan['plants'].items():
  mapping[plant]=[]
  for stage in stages:
   path=A/('Prefabs/Crops/'+stage['source_group'])/(stage['model']+'.prefab')
   assert path.is_file(),path
   guid=re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
   mapping[plant].append(dict(stage=stage['stage'],prefab=str(path.relative_to(ROOT)),guid=guid,fileID=100,scale=stage.get('scale',[1,1,1])))
binding_data=dict(group=GROUP,status='Prepared visual assets; existing crop profiles not modified',material=MAIN_MATERIAL,plants=mapping)
integration_path=SOURCE/'Integration/Manifest.json'
if integration_path.exists():
 integration=json.loads(integration_path.read_text())
 applied={r['species']:r['profile'] for r in integration['plants'] if r['group']==GROUP}
 if applied:
  binding_data.update(status='Applied to gameplay crop profiles and the existing seed store; Unity Play Mode blocked by licensing',gameplayIntegration='ArtSource/Crops/Integration/Manifest.json',profiles=applied)
(SOURCE/GROUP/'Blender/UnityVisualBindings.json').write_text(json.dumps(binding_data,indent=2))
if REGISTRY.exists():
 from unified_plant_atlas import migration_report
 migration_report()
print('UNITY_PLANT_ART_PREPARED',GROUP,len(mesh_guids),'native meshes,',len(prefab_ids),'visual prefabs, existing shared plant materials')
