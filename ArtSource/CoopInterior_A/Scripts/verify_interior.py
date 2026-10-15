"""Independent saved .blend audit. No repairs or source changes."""
import bpy,bmesh,json,math
from pathlib import Path
from collections import Counter
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/CoopInterior_A.blend'))
meshes={o.data.name:o.data for sc in bpy.data.scenes for o in sc.objects if o.type=='MESH'};records=[];fail=[]
for name,me in meshes.items():
 bm=bmesh.new();bm.from_mesh(me)
 rec={'mesh':name,'vertices':len(bm.verts),'triangles':len(bm.faces),'zeroAreaFaces':sum(f.calc_area()<1e-10 for f in bm.faces),'looseVertices':sum(not v.link_faces for v in bm.verts),'looseEdges':sum(not e.link_faces for e in bm.edges),'nonManifoldJunctionEdges':sum(len(e.link_faces)>2 for e in bm.edges),'boundaryEdges':sum(e.is_boundary for e in bm.edges),'inconsistentWindingEdges':sum(e.is_manifold and not e.is_contiguous for e in bm.edges),'nonTriangles':sum(len(f.verts)!=3 for f in bm.faces),'nonFiniteVertices':sum(not all(math.isfinite(x) for x in v.co) for v in bm.verts),'missingUV':not bool(me.uv_layers),'invalidUV':0}
 if me.uv_layers:rec['invalidUV']=sum(not all(math.isfinite(t) and -.001<=t<=1.001 for t in u.uv) for u in me.uv_layers.active.data)
 # Duplicate faces by welded vertex indices, independent of orientation.
 bm.verts.index_update();keys=[tuple(sorted(v.index for v in f.verts)) for f in bm.faces];rec['duplicateFaces']=sum(n-1 for n in Counter(keys).values() if n>1)
 for k in ['zeroAreaFaces','looseVertices','looseEdges','nonManifoldJunctionEdges','inconsistentWindingEdges','nonTriangles','nonFiniteVertices','missingUV','invalidUV','duplicateFaces']:
  if rec[k]:fail.append({'mesh':name,'check':k,'count':rec[k]})
 records.append(rec);bm.free()
levels=[]
for n,(cap,troughs,heads,eggs,budget) in enumerate([(6,2,0,1,5000),(9,3,0,1,6500),(12,4,4,1,9000),(15,5,5,3,10000)],1):
 sc=next(s for s in bpy.data.scenes if s.name.startswith(f'0{n} COOP INTERIOR'))
 obs=[o for o in sc.objects if o.type=='MESH' and o.parent is not None];tri=sum(len(o.data.polygons) for o in obs)
 count=lambda prefix:sum(o.name.startswith(prefix) for o in sc.objects)
 checks={'capacity':sc['Capacity']==cap,'troughs':count('Trough_')==troughs,'autoHeads':count('AutoHead_')==heads,'incubatorEggs':count('IncubatorEgg_')==eggs,'animalSockets':count('AnimalSlot_')==cap,'triangleBudget':tri<=budget,'positiveScale':all(all(s>0 for s in o.scale) for o in obs),'materialsAssigned':all(bool(o.data.materials) for o in obs)}
 levels.append({'level':n,'triangles':tri,'meshVerticesInstanced':sum(len(o.data.vertices) for o in obs),'budget':budget,'checks':checks})
 for k,ok in checks.items():
  if not ok:fail.append({'level':n,'check':k})
result={'status':'PASS' if not fail else 'FAIL','failures':fail,'levels':levels,'meshes':records,'notes':['Boundary edges are reported separately: Barn seam strips and sparse straw are intentional open surfaces.','Validation checks defects and winding; overlapping separate assembled solids are not Boolean-unioned or counted as non-manifold defects.','No Unity vertex count, phone FPS or native hood material validation yet.']}
(ART/'Previews/TopologyAudit.json').write_text(json.dumps(result,indent=2));print('COOP_TOPOLOGY_AUDIT',json.dumps({'status':result['status'],'failures':fail,'levels':levels}),flush=True)
