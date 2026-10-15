"""Saved .blend and all FBX roundtrips; no regeneration or Unity changes."""
from pathlib import Path
import bpy,json,hashlib,math
from mathutils import Vector
from mathutils.kdtree import KDTree
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
S=json.loads((ART/'References/House_A_Approved.json').read_text())
R=json.loads((ART/'Previews/MeshManifest.json').read_text())
checks=[]
def check(condition,message):
 assert condition,message
 checks.append(message)
def extrema(obj):
 vs=[obj.matrix_world@v.co for v in obj.data.vertices]
 return [[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]]
def audit(obj,m,lod):
 mesh=obj.data;count=sum(len(p.vertices)-2 for p in mesh.polygons)
 limit=m['budgetTriangles']['LOD0Limit'] if lod==0 else m['budgetTriangles']['LOD'+str(lod)+'Target']
 check(count<=limit,obj.name+': triangle budget')
 check(len(mesh.materials)==2,obj.name+': shared material count')
 check({q.name for q in mesh.materials}==set(['TFP_Atlas_1A','TFP_Atlas_Lights_1A']),obj.name+': exact pack material names')
 check(len(mesh.uv_layers)==1,obj.name+': exactly one atlas UV map')
 rects=[q['safeUvBottomLeft'] for q in S['materials']['swatches'].values()]
 for p in mesh.polygons:
  slot=mesh.materials[p.material_index].name
  for k in p.loop_indices:
   u,v=mesh.uv_layers.active.data[k].uv
   assert any(a-1e-6<=u<=c+1e-6 and b-1e-6<=v<=d+1e-6 for a,b,c,d in rects),(obj.name,'UV outside palette',u,v)
 check(all(p.area>1e-10 for p in mesh.polygons),obj.name+': no degenerate faces')
 check(all(math.isfinite(x) for v in mesh.vertices for x in v.co),obj.name+': finite vertices')
 lo,hi=extrema(obj);unitylo=[lo[0],lo[2],-hi[1]];unityhi=[hi[0],hi[2],-lo[1]]
 tol=.051 if lod==0 else .13
 check(all(abs(unitylo[i]-m['visualBounds']['minXYZ'][i])<tol and abs(unityhi[i]-m['visualBounds']['maxXYZ'][i])<tol for i in range(3)),obj.name+': approved bounds tolerance')
 check(all(abs(x)<1e-5 for x in obj.location),obj.name+': common door-ground origin')
 return count

bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
check(hashlib.sha256((ROOT/S['materials']['atlasPath']).read_bytes()).hexdigest()==S['materials']['atlasSha256'],'Original atlas PSD unchanged')
check(len([im for im in bpy.data.images if im.packed_file and im.get('UnityGUID')])==1,'One packed Blender image; reuse existing atlas PSD')
opaque=bpy.data.materials['TFP_Atlas_1A']
check(opaque.node_tree.nodes.get('Principled BSDF').inputs['Alpha'].default_value==1,'Blue sunroom uses opaque atlas material')
saved={};sourcechecks=[]
for m in S['models']:
 scene=bpy.data.scenes['House A '+m['id']];bpy.context.window.scene=scene;bpy.context.view_layer.update()
 visible=[o for o in scene.objects if o.name.startswith('House_A_') and o.type=='MESH' and not o.hide_render and not o.hide_get()]
 check(len(visible)==1 and visible[0].get('LOD')==0,m['id']+': one combined visible building')
 for lod in [0,1,2]:
  name='House_A_'+m['id'].replace('-','_')+'_LOD'+str(lod);obj=bpy.data.objects[name];count=audit(obj,m,lod)
  check(tuple(obj.scale)==(1,1,1) and all(abs(v)<1e-6 for v in obj.rotation_euler),name+': identity transform')
  saved[name]=[obj.matrix_world@v.co for v in obj.data.vertices]
  roof_ids={i for i,p in enumerate(m['parts']) if p['name'].startswith('skylight_') and p['material']=='window'}
  if roof_ids:
   att=obj.data.attributes['ApprovedPartIndex']
   check(all(p.normal.z>.7 for p in obj.data.polygons if att.data[p.index].value in roof_ids),name+': skylight normals outward/up for Unity backface culling')
  if lod==0:
   attr=obj.data.attributes.get('ApprovedPartIndex');up=Vector((0,0,1))
   for p in obj.data.polygons:
    idx=attr.data[p.index].value
    if idx>=0 and idx<len(m['parts']) and '_slate_' in m['parts'][idx]['name']:
     assert p.normal.dot(up)>.70,(name,'inward roof',p.index)
   check(True,name+': slate normals outward/up')
  if m['id']=='Lv5':
   att=obj.data.attributes.get('PaletteIndex');blue=list(S['materials']['swatches']).index('glass')
   panes=[p for p in obj.data.polygons if att.data[p.index].value==blue]
   check(len(panes)==15 and all(p.material_index==0 for p in panes),name+': five facade + five roof panels use opaque blue atlas')
   a,b,c,d=S['materials']['swatches']['glass']['safeUvBottomLeft']
   check(all(abs(obj.data.uv_layers.active.data[k].uv.x-(a+c)/2)<1e-6 and abs(obj.data.uv_layers.active.data[k].uv.y-(b+d)/2)<1e-6 for p in panes for k in p.loop_indices),name+': blue pane UV samples correct atlas swatch')
  sourcechecks.append({'model':m['id'],'lod':lod,'triangles':count})
bpy.context.window.scene=bpy.data.scenes['House A Levels 1-5'];instances=[o for o in bpy.context.scene.objects if o.get('Level')]
check(len(instances)==5,'Five level comparison instances')
lv3=next(o for o in instances if o['Level']==3);lv4=next(o for o in instances if o['Level']==4)
check(lv3.data==lv4.data and tuple(lv3.scale)==tuple(lv4.scale)==(1,1,1),'Lv3 and Lv4 share actual mesh and scale')
check(len({o.data.as_pointer() for o in instances})==4,'Exactly four unique meshes in comparison')

fbx=[]
for entry in R['models']:
 path=ART/entry['export'];name=entry['name'];m=next(m for m in S['models'] if m['id']==entry['model'])
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 check(len(meshes)==1,name+': FBX contains one mesh')
 check(not any(o.type in ['CAMERA','LIGHT','FONT','ARMATURE'] for o in bpy.context.scene.objects),name+': no studio/camera/rig exported')
 obj=meshes[0];count=audit(obj,m,entry['lod']);check(count==entry['triangles'],name+': FBX triangles match saved source')
 actual=[obj.matrix_world@v.co for v in obj.data.vertices]
 for a,b in [(saved[name],actual),(actual,saved[name])]:
  tree=KDTree(len(b))
  for i,v in enumerate(b):tree.insert(v,i)
  tree.balance();check(all(tree.find(v)[2]<1e-4 for v in a),name+': FBX vertex positions match saved source')
 check(hashlib.sha256(path.read_bytes()).hexdigest()==entry['sha256'],name+': exported file checksum')
 fbx.append({'name':name,'triangles':count,'vertexRoundtrip':'PASS','singleMesh':'PASS','UV':'PASS','filename':str(path.relative_to(ART))})
report={'status':'PASS','scope':'Saved Blender source and FBX reimport; no Unity/Play Mode claims.',
        'checksPassed':len(checks),'checks':checks,'savedSource':sourcechecks,'fbxRoundtrip':fbx,'newRuntimeTextures':0}
(ART/'Previews/BlenderSourceAudit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('HOUSE_VERIFICATION_PASS '+str(len(checks))+' checks; 12 single-mesh FBX files; atlas reused.')
