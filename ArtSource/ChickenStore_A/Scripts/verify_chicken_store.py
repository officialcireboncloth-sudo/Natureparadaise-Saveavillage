"""Read-only .blend and FBX verification, using actual geometry and UV coordinates."""
import bpy,json,hashlib,math
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/ChickenStore_A.blend'))
audit=json.loads((ART/'References/atlas-audit.json').read_text(encoding='utf-8-sig'))
manifest=json.loads((ART/'Previews/MeshManifest.json').read_text())
rects=[v['safeUvBottomLeft'] for v in audit['swatches'].values()]
checks=[]
def check(label,condition):
 checks.append({'check':label,'pass':bool(condition)})
 assert condition,label
def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
def bounds(obs):
 points=[world(o)@v.co for o in obs for v in o.data.vertices]
 return [[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]]
def near(a,b,tol=1e-4):return max(abs(x-y) for aa,bb in zip(a,b) for x,y in zip(aa,bb))<tol
check('Original Toonpack PSD unchanged',hashlib.sha256((ROOT/audit['sourcePSD']).read_bytes()).hexdigest()==audit['sourceSha256'])
meshes=[]
for record in manifest['assets']:
 sc=bpy.data.scenes[record['editableScene']];bpy.context.window.scene=sc
 source=next(c for c in sc.collection.children if c.name.startswith('01 EDITABLE'))
 export=next(c for c in sc.collection.children if c.name.startswith('02 EXPORT'))
 obs=[o for o in export.objects if o.type=='MESH'];meshes.extend(o for o in source.objects if o.type=='MESH')
 check(record['name']+' export bounds match authored mesh',near(bounds(obs),record['bounds']))
 check(record['name']+' rests on ground',abs(record['bounds'][0][2])<1e-5)
 height=5.6 if record['name']=='ChickenStore_A' else 3.1
 check(record['name']+' approved height',abs(record['bounds'][1][2]-height)<1e-4)
 meshes.extend(obs)
 if record['name']=='ChickenStore_A':
  hinge=bpy.data.objects['ChickenStore_A_DoorHinge'];leaf=bpy.data.objects['ChickenStore_A_DoorLeaf'];body=bpy.data.objects['ChickenStore_A_Body']
  static_before=bounds([body]);verts_before=[world(leaf)@v.co for v in leaf.data.vertices];pivot=Vector(hinge.location)
  hinge.rotation_euler.z=.65
  check('Door rotates around hinge while static building stays fixed',near(static_before,bounds([body])) and max(abs((world(leaf)@v.co-pivot).length-(p-pivot).length) for v,p in zip(leaf.data.vertices,verts_before))<1e-5 and (world(leaf)@leaf.data.vertices[0].co-verts_before[0]).length>.01)
  hinge.rotation_euler.z=0
  check('Main side gable reaches roof ridge',max(v.co.z for v in bpy.data.objects['ChickenStore_A_Plaster'].data.vertices if abs(v.co.x)>3.9)>4.59)
vane=bpy.data.objects['Coop_Optional_WeatherVane'];meshes.append(vane)
check('Optional vane fits 3.45 m total height',bounds([vane])[1][2]<=3.45)
for o in meshes:
 check(o.name+' finite coordinates',all(math.isfinite(c) for v in o.data.vertices for c in v.co))
 check(o.name+' one shared atlas material',len(o.data.materials)==1 and o.data.materials[0].name=='TFP_Atlas_1A')
 uv=o.data.uv_layers.active
 check(o.name+' safe atlas UVs',uv is not None and all(any(a-1e-6<=u.uv.x<=c+1e-6 and b-1e-6<=u.uv.y<=d+1e-6 for a,b,c,d in rects) for u in uv.data))
 o.data.calc_loop_triangles()
 check(o.name+' no degenerate triangles',all((o.data.vertices[t.vertices[1]].co-o.data.vertices[t.vertices[0]].co).cross(o.data.vertices[t.vertices[2]].co-o.data.vertices[t.vertices[0]].co).length>1e-10 for t in o.data.loop_triangles))
check('Atlas image packed for editable Blender file',any(i.packed_file and i.get('UnityGUID')==audit['sourceGUID'] for i in bpy.data.images))
# Read the exported files back through Blender's independent FBX importer.
roundtrips=[]
for record in manifest['assets']+[dict(manifest['optionalOrnament'],name='OptionalWeatherVane')]:
 sc=bpy.data.scenes.new('AUDIT '+record['name']);bpy.context.window.scene=sc
 bpy.ops.import_scene.fbx(filepath=str(ART/'Exports'/record['export']),use_anim=False)
 obs=[o for o in sc.objects if o.type=='MESH'];actual=bounds(obs)
 check(record['name']+' FBX roundtrip bounds',near(actual,record['bounds'],.0005))
 count=sum(len(p.vertices)-2 for o in obs for p in o.data.polygons)
 check(record['name']+' FBX triangle count preserved',count==record['triangles'])
 check(record['name']+' FBX excludes studio and cameras',not any(o.type in {'CAMERA','LIGHT'} or 'Studio' in o.name for o in sc.objects))
 roundtrips.append({'name':record['name'],'bounds':actual,'triangles':count})
report={'status':'PASS','checksPassed':len(checks),'checks':checks,'fbxRoundTrips':roundtrips}
(ART/'Previews/Verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CHICKEN_VERIFICATION_PASS',len(checks))
