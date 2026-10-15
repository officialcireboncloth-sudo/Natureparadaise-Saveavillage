import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/MiniMarket_A.blend'))
manifest=json.loads((ART/'Previews/MeshManifest.json').read_text());audit=json.loads((ART/'References/atlas-audit.json').read_text(encoding='utf-8-sig'))
sc=bpy.data.scenes[manifest['scene']];bpy.context.window.scene=sc
ex=next(c for c in sc.collection.children if c.name.startswith('MM 02'));obs=[o for o in ex.objects if o.type=='MESH'];checks=[]
def check(label,ok):
 checks.append({'check':label,'pass':bool(ok)})
 assert ok,label
def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
def bounds(objects):
 pts=[world(o)@v.co for o in objects for v in o.data.vertices]
 return [[min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]]
def near(a,b):return max(abs(x-y) for aa,bb in zip(a,b) for x,y in zip(aa,bb))<.0005
check('Existing atlas unchanged',hashlib.sha256((ROOT/audit['sourcePSD']).read_bytes()).hexdigest()==audit['sourceSha256'])
check('Triangle budget 5000',manifest['triangles']<=5000)
check('Three runtime meshes, two separate door leaves',len(obs)==3)
check('Actual height 4.85m and ground contact',abs(bounds(obs)[1][2]-4.85)<.0001 and abs(bounds(obs)[0][2])<.0001)
check('No runtime cameras or lights',not any(o.type in {'CAMERA','LIGHT'} for o in ex.objects))
rects=[v['safeUvBottomLeft'] for v in audit['swatches'].values()]
expanded=0
for o in obs:
 check(o.name+' finite geometry',all(math.isfinite(c) for v in o.data.vertices for c in v.co))
 check(o.name+' one opaque shared atlas material',len(o.data.materials)==1 and o.data.materials[0].name=='TFP_Atlas_1A')
 check(o.name+' all triangulated',all(len(p.vertices)==3 for p in o.data.polygons))
 uv=o.data.uv_layers.active
 check(o.name+' UV within audited swatches',uv is not None and all(any(a-1e-6<=u.uv.x<=c+1e-6 and b-1e-6<=u.uv.y<=d+1e-6 for a,b,c,d in rects) for u in uv.data))
 check(o.name+' no degenerate triangle',all((o.data.vertices[p.vertices[1]].co-o.data.vertices[p.vertices[0]].co).cross(o.data.vertices[p.vertices[2]].co-o.data.vertices[p.vertices[0]].co).length>1e-10 for p in o.data.polygons))
 # Engine vertices may split at flat normals and UV discontinuities.
 unique=set()
 for p in o.data.polygons:
  for li in p.loop_indices:
   unique.add((o.data.loops[li].vertex_index,*[round(c,6) for c in p.normal],*[round(c,6) for c in uv.data[li].uv]))
 expanded+=len(unique)
check('Atlas packed for Blender',any(n.type=='TEX_IMAGE' and n.image and n.image.packed_file for n in obs[0].data.materials[0].node_tree.nodes))
for suffix in ['Left','Right']:
 hinge=bpy.data.objects['MiniMarket_A_Door'+suffix+'_Hinge'];leaf=next(o for o in obs if o.parent==hinge);pivot=Vector(hinge.location)
 before=[world(leaf)@v.co for v in leaf.data.vertices];body=bounds([obs[0]])
 hinge.rotation_euler.z=.55
 check(suffix+' hinge rotates only its door',near(body,bounds([obs[0]])) and max(abs((world(leaf)@v.co-pivot).length-(p-pivot).length) for v,p in zip(leaf.data.vertices,before))<1e-4 and (world(leaf)@leaf.data.vertices[0].co-before[0]).length>.01)
 hinge.rotation_euler.z=0
expected=bounds(obs)
sc2=bpy.data.scenes.new('FBX AUDIT ONLY');bpy.context.window.scene=sc2
bpy.ops.import_scene.fbx(filepath=str(ART/'Exports/MiniMarket_A.fbx'),use_anim=False)
imported=[o for o in sc2.objects if o.type=='MESH']
check('FBX roundtrip exact bounds',near(expected,bounds(imported)))
check('FBX triangle count unchanged',sum(len(o.data.polygons) for o in imported)==manifest['triangles'])
check('FBX preserves both door hinge parents',sum(o.parent is not None for o in imported)==2)
report={'status':'PASS','checksPassed':len(checks),'triangles':manifest['triangles'],'meshVertices':manifest['meshVertices'],'estimatedVerticesAfterNormalAndUVSplits':expanded,'note':'Geometry audit only. Actual phone frame rate needs profiling in Unity on target hardware.','checks':checks}
(ART/'Previews/Verification.json').write_text(json.dumps(report,indent=2))
print('MINIMARKET_VERIFICATION_PASS',json.dumps({k:v for k,v in report.items() if k!='checks'}))
