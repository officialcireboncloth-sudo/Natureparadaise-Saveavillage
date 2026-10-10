"""Independent saved-source, combined-file and exported-FBX audit. No source saves."""
from pathlib import Path
import bpy,json,math,hashlib,struct
from mathutils import Vector
from mathutils.kdtree import KDTree
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
S=json.loads((ART/'References/Coop_A_Approved.json').read_text());M=S['master']
assert hashlib.sha256((ROOT/S['materials']['atlasPath']).read_bytes()).hexdigest()==S['materials']['sha256']
loExpected=[M['visualBoundsMinXYZ'][i] for i in [0,2,1]];hiExpected=[M['visualBoundsMaxXYZ'][i] for i in [0,2,1]]
rects=[sw['uvSafeRectBottomLeft'] for sw in S['materials']['swatches'].values()]
def fingerprint(obj):
 h=hashlib.sha256()
 for v in obj.data.vertices:h.update(struct.pack('<3f',*v.co))
 for p in obj.data.polygons:h.update(struct.pack('<4I',*p.vertices,p.material_index))
 for loop in obj.data.uv_layers.active.data:h.update(struct.pack('<2f',*loop.uv))
 return h.hexdigest()
def check_same_positions(a,b):
 for source,target in [(a,b),(b,a)]:
  kd=KDTree(len(target))
  for i,v in enumerate(target):kd.insert(v,i)
  kd.balance()
  for v in source:assert kd.find(v)[2]<1e-5,('FBX geometry differs from final saved mesh',v)
def extrema(obj,matrix=None):
 transform=obj.matrix_world if matrix is None else matrix;pts=[transform@v.co for v in obj.data.vertices]
 return [[min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]]
def check_mesh(obj,lod):
 lo,hi=extrema(obj)
 assert all(abs(lo[i]-loExpected[i])<1e-4 and abs(hi[i]-hiExpected[i])<1e-4 for i in range(3)),(obj.name,lo,hi)
 count=sum(len(p.vertices)-2 for p in obj.data.polygons);assert count<=[4200,1600,420][lod],count
 assert len(obj.data.materials)==2 and {m.name for m in obj.data.materials}=={'TFP_Atlas_1A','TFP_Atlas_Lights_1A'}
 assert {p.material_index for p in obj.data.polygons}=={0,1}
 assert len(obj.data.uv_layers)==1
 for loop in obj.data.uv_layers.active.data:
  u,v=loop.uv;assert any(a-1e-6<=u<=c+1e-6 and b-1e-6<=v<=d+1e-6 for a,b,c,d in rects),(u,v)
 assert all(math.isfinite(v) for vert in obj.data.vertices for v in vert.co)
 assert all(p.area>1e-10 for p in obj.data.polygons),'degenerate face'
 assert all(abs(x)<1e-5 for x in obj.location),'nonzero origin'
 return dict(lod=lod,triangles=count,boundsBlenderXYZ=[lo,hi],dimensionsBlenderXYZ=[hi[i]-lo[i] for i in range(3)],materials='PASS',uvSwatches='PASS',noDegenerateFaces='PASS',groundCentreOrigin='PASS')
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Coop_A_Master.blend'))
bpy.context.window.scene=bpy.data.scenes['Coop A - Editable Master Lv3'];bpy.context.view_layer.update()
source=bpy.data.collections['01 EDITABLE - master dimensions Lv3'];objects=list(source.all_objects)
parts={p['name']:p for p in S['parts'] if p['type']!='surface' and p['name'] not in ['Foundation','WallBody']}
authored=[o for o in objects if o.get('ApprovedPart')];assert {o['ApprovedPart'] for o in authored if o['ApprovedPart'] in parts}==set(parts)
placements=[]
for obj in authored:
 p=next(p for p in S['parts'] if p['name']==obj['ApprovedPart'])
 if p['type']=='box':
  lo,hi=extrema(obj);c=p['centerXYZ'];sz=p['sizeXYZ']
  assert all(abs(lo[j]-(c[i]-sz[i]/2))<1e-5 and abs(hi[j]-(c[i]+sz[i]/2))<1e-5 for j,i in enumerate([0,2,1])),(obj.name,lo,hi)
 if p['type']=='surface':
  # BMesh merging does not change approved surface endpoints.
  actual=[tuple(v.co) for v in obj.data.vertices]
  for v in p['verticesXYZ']:assert any((Vector((v[0],v[2],v[1]))-Vector(a)).length<1e-5 for a in actual),obj.name
 placements.append(obj['ApprovedPart'])
for obj in objects:
 if obj.name.startswith('WallBoards_'):
  direction=dict(front=(0,-1,0),rear=(0,1,0),right=(1,0,0),left=(-1,0,0))[obj.name.split('_')[-1]]
  assert all(p.normal.dot(Vector(direction))>.99 for p in obj.data.polygons),obj.name
 if obj.name in ['RoofRight','RoofLeft']:
  for p in obj.data.polygons:
   verts=[obj.data.vertices[i].co for i in p.vertices]
   if abs(p.normal.z)>.7 and sum(v.z+.6*abs(v.x)-5.6 for v in verts)/len(verts)>-.01:assert p.normal.z>.7,(obj.name,p.index)
assert len([im for im in bpy.data.images if im.packed_file and im.get('UnityAtlasGUID')])==1
sourceHashes={i:fingerprint(bpy.data.objects['Coop_A_Master_LOD'+str(i)]) for i in [0,1,2]}
scene=bpy.data.scenes['Coop A - Levels 1 to 5 - shared mesh'];bpy.context.window.scene=scene;bpy.context.view_layer.update()
col=bpy.data.collections['Level instances - uniform size only'];instances=[o for o in col.objects if o.type=='MESH'];assert len(instances)==5 and len({o.data.as_pointer() for o in instances})==1
for lv in S['levels']:
 root=bpy.data.objects['Coop_A_Lv'+str(lv['level'])];assert all(abs(x-lv['uniformScale'])<1e-7 for x in root.scale)
 obj=next(o for o in instances if o.parent==root);matrix=root.matrix_basis@obj.matrix_parent_inverse@obj.matrix_basis;lo,hi=extrema(obj,matrix)
 expected=[lv['visualWidthDepthHeight'][i] for i in [0,1,2]]
 assert all(abs(hi[i]-lo[i]-expected[i])<1e-4 for i in range(3)),lv['level']
 marker=bpy.data.objects['Lv'+str(lv['level'])+' Entrance'];assert tuple(marker.location)==(-1.25,-4.650000095367432,0.0)
 sourceMarker=root.matrix_basis@marker.matrix_parent_inverse@marker.matrix_basis@Vector((0,0,0));expectedMarker=Vector(root.location)+Vector((lv['entranceMarkerXYZ'][0],lv['entranceMarkerXYZ'][2],0))
 assert (sourceMarker-expectedMarker).length<1e-5
sourceReport=dict(approvedPartPlacements=len(placements),boxDimensions='PASS',surfaceEndpoints='PASS',outwardWallAndRoofNormals='PASS',levelInstances=5,uniformScaleAndEntrance='PASS',sharedLevelMesh='PASS',originalAtlasUnchanged='PASS',packedBlenderPreviewImage=True)
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Coop_A_Combined.blend'))
bpy.context.window.scene=bpy.data.scenes['Coop A - Combined Lv3'];bpy.context.view_layer.update()
visible=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Coop_A_Master') and not o.hide_render and not o.hide_get()]
assert len(visible)==1 and visible[0].name=='Coop_A_Master_LOD0'
combinedChecks=[]
combinedPositions={}
for lod in [0,1,2]:
 obj=bpy.data.objects['Coop_A_Master_LOD'+str(lod)];assert tuple(obj.scale)==(1,1,1) and all(abs(x)<1e-7 for x in obj.rotation_euler)
 combinedChecks.append(check_mesh(obj,lod))
 assert fingerprint(obj)==sourceHashes[lod],'combined file is stale relative to source'
 combinedPositions[lod]=[v.co.copy() for v in obj.data.vertices]
 assert obj.hide_render==(lod!=0) and obj.hide_get()==(lod!=0)
fbxChecks=[]
for lod,name in enumerate(['Coop_A_High','Coop_A_Medium','Coop_A_Low']):
 bpy.ops.wm.read_factory_settings(use_empty=True);path=ART/'Exports'/f'{name}.fbx';assert path.stat().st_size>1000
 bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False);models=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(models)==1
 assert models[0].name==name;assert not any(o.type in ['LIGHT','CAMERA','FONT','ARMATURE'] for o in bpy.context.scene.objects)
 check_same_positions(combinedPositions[lod],[models[0].matrix_world@v.co for v in models[0].data.vertices])
 entry=check_mesh(models[0],lod);entry['filename']=path.name;entry['sha256']=hashlib.sha256(path.read_bytes()).hexdigest();entry['previewObjectsExcluded']='PASS';fbxChecks.append(entry)
report=dict(status='PASS',scope='Saved Blender source, one-mesh combined file and FBX reimport. No Unity integration or Play Mode claims.',sourceBlend=sourceReport,sourceCombinedFingerprints='PASS',fbxMatchesFinalVertexPositions='PASS',visibleBuildingMeshCount=1,combinedMeshes=combinedChecks,fbxRoundtrip=fbxChecks)
(ART/'Previews/BlenderSourceAudit.json').write_text(json.dumps(report,indent=2)+'\n')
print('COOP_VERIFICATION_PASS '+json.dumps(report))
