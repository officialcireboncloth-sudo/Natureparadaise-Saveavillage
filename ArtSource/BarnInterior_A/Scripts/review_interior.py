import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/BarnInterior_A.blend'))
report=[]
for sc in [s for s in bpy.data.scenes if 'BARN INTERIOR' in s.name]:
 bpy.context.window.scene=sc;bpy.context.view_layer.update()
 obs=[o for o in sc.objects if o.type=='MESH'];bad=[];uvbad=0;estimate=0
 for o in obs:
  uv=o.data.uv_layers.active;seen=set()
  for p in o.data.polygons:
   if p.area<1e-10:bad.append(o.name)
   for li in p.loop_indices:
    if uv:
     point=uv.data[li].uv
     if any(v<0 or v>1 or not math.isfinite(v) for v in point):uvbad+=1
     seen.add((o.data.loops[li].vertex_index,*[round(v,6) for v in p.normal],*[round(v,6) for v in point]))
  estimate+=len(seen)
 report.append({'scene':sc.name,'degenerateFaces':len(bad),'invalidUVs':uvbad,'estimatedVerticesAfterUVNormalSplits':estimate,'triangles':sum(len(p.vertices)-2 for o in obs for p in o.data.polygons),'bounds':[[min((o.matrix_world@v.co)[i] for o in obs for v in o.data.vertices) for i in range(3)],[max((o.matrix_world@v.co)[i] for o in obs for v in o.data.vertices) for i in range(3)]]})
 if '--render' in sys.argv:
  sc.cycles.samples=10;sc.render.resolution_x=1280;sc.render.resolution_y=960
  bpy.ops.render.render(write_still=True)
(ART/'Previews/Verification.json').write_text(json.dumps(report,indent=2))
print('VERIFICATION',json.dumps(report))
