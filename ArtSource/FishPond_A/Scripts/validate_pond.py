"""Audit saved meshes and real FBX reimport against approved placements."""
from pathlib import Path
import bpy,json,hashlib,math
from mathutils import Vector
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
S=json.loads((ART/'References/FishPond_A_Approved.json').read_text(encoding='utf-8'))
manifest=json.loads((ART/'Previews/MeshManifest.json').read_text(encoding='utf-8'))
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/FishPond_A_Masters.blend'))
checks=[]
def check(label,value):
 checks.append(dict(check=label,passed=bool(value)));assert value,label
def bounds(o):
 vs=[o.matrix_world@v.co for v in o.data.vertices]
 return [[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]]
check('Atlas unchanged',hashlib.sha256((ROOT/S['materials']['atlasPath']).read_bytes()).hexdigest()==S['materials']['atlasSha256'])
check('Unity pack water unchanged',hashlib.sha256((ROOT/S['materials']['waterMaterial']).read_bytes()).hexdigest()==S['materials']['waterMaterialSha256'])
check('One packed atlas, no external preview texture required',len(bpy.data.images)==1 and bpy.data.images[0].packed_file is not None)
snapshot={}
for a in manifest['models']:
 o=bpy.data.objects[a['name']];m=next(m for m in S['models'] if m['id']==a['model']);k=o.name
 check(k+' identity transform',o.location.length<1e-6 and sum(abs(x) for x in o.rotation_euler)<1e-6 and (o.scale-Vector((1,1,1))).length<1e-6)
 check(k+' one material',len(o.data.materials)==1)
 check(k+' triangulated clean geometry',all(len(p.vertices)==3 and p.area>1e-9 for p in o.data.polygons))
 check(k+' count matches export manifest',len(o.data.polygons)==a['triangles'])
 if a['kind']=='water':
  check(k+' flat and upward',all(abs(v.co.z-.65)<1e-6 for v in o.data.vertices) and all(p.normal.z>.99999 for p in o.data.polygons))
  check(k+' accurate approved boundary',all(any((v.co-Vector((x,z,.65))).length<1e-5 for v in o.data.vertices) for x,z in m['waterPolygonXZ']))
  check(k+' metric UV with nonzero area',all(abs(sum(o.data.uv_layers.active.data[t].uv.x*o.data.uv_layers.active.data[u].uv.y-o.data.uv_layers.active.data[u].uv.x*o.data.uv_layers.active.data[t].uv.y for t,u in zip(p.loop_indices,list(p.loop_indices)[1:]+list(p.loop_indices)[:1])))>1e-6 for p in o.data.polygons))
 else:
  safe=[v['safeUvBottomLeft'] for v in S['materials']['swatches'].values()]
  check(k+' UV stays within palette safe rectangles',all(any(x0-1e-6<=q.uv.x<=x1+1e-6 and y0-1e-6<=q.uv.y<=y1+1e-6 for x0,y0,x1,y1 in safe) for q in o.data.uv_layers.active.data))
  if a['lod']==0:
   bb=bounds(o);converted=[[bb[0][0],bb[0][2],bb[0][1]],[bb[1][0],bb[1][2],bb[1][1]]]
   check(k+' approved full bounds',all(abs(converted[j][i]-m['visualBounds'][['minXYZ','maxXYZ'][j]][i])<1e-5 for j in range(2) for i in range(3)))
   ids=o.data.attributes['ApprovedPartIndex'];polys=o.data.polygons
   # Open floor/wall normals cannot be tested with global recalculation.
   for prefix in ['basin_floor','basin_innerwall','basin_outerwall','basin_coping']:
    subset=[p for p in polys if m['parts'][ids.data[p.index].value]['name'].startswith(prefix)]
    check(k+' '+prefix+' exists',bool(subset))
    if prefix in ['basin_floor','basin_coping']:check(k+' '+prefix+' normals up',all(p.normal.z>.999 for p in subset))
    else:
     sign=-1 if 'inner' in prefix else 1
     check(k+' '+prefix+' correct facing',all(p.normal.dot(Vector((p.center.x-1.5,p.center.y,0)))*sign>0 for p in subset))
   for station in ['maker','storage']+(['autoFeeder'] if m['level']>=2 else []):
    p=m['stations'][station]['centerXYZ'];scene=bpy.data.scenes['Fish Pond A '+m['id']]
    name={'maker':'Maker station','storage':'Feed barrel','autoFeeder':'Auto feeder station'}[station]
    marker=next(v for v in scene.objects if v.type=='EMPTY' and v.name.startswith(name))
    check(k+' '+station+' approved marker',(marker.location-Vector((p[0],p[2],p[1]))).length<1e-5)
   lane=m['dock']['clearLaneWidth'];cx=m['dock']['centerXYZ'][0]
   ranges=[]
   for p in polys:
    part=m['parts'][ids.data[p.index].value]
    if part['group']=='autoFeeder':ranges.extend(o.data.vertices[v].co.x for v in p.vertices)
   if ranges:check(k+' feeder clear of central lane',max(ranges)<cx-lane/2)
  limit=m['budgetTriangles']['LOD0Limit' if a['lod']==0 else 'LOD'+str(a['lod'])+'Target']
  check(k+' within LOD triangle budget',a['triangles']+14<=limit)
 snapshot[k]=dict(bounds=bounds(o),triangles=len(o.data.polygons),uvs=sorted((round(q.uv.x,6),round(q.uv.y,6)) for q in o.data.uv_layers.active.data),volume=sum((o.data.vertices[p.vertices[0]].co.dot(o.data.vertices[p.vertices[1]].co.cross(o.data.vertices[p.vertices[2]].co)))/6 for p in o.data.polygons))
check('Comparison uses actual shared mesh data',all(bpy.data.scenes['Fish Pond A Levels 1-3'].objects[a['model']+' '+('Water' if a['kind']=='water' else 'Opaque')].data is bpy.data.objects[a['name']].data for a in manifest['models'] if a['kind']=='water' or a['lod']==0))
audit=bpy.data.scenes.new('FBX verification');bpy.context.window.scene=audit
for a in manifest['models']:
 path=ART/a['export'];check(a['name']+' export SHA',hashlib.sha256(path.read_bytes()).hexdigest()==a['sha256'])
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),use_custom_normals=False)
 imported=[o for o in bpy.data.objects if o not in before];mesh=[o for o in imported if o.type=='MESH'];check(a['name']+' exactly one FBX mesh',len(mesh)==1)
 o=mesh[0];orig=snapshot[a['name']];bb=bounds(o)
 check(a['name']+' FBX metre bounds/pivot roundtrip',all(abs(bb[j][i]-orig['bounds'][j][i])<1e-4 for i in range(3) for j in range(2)))
 check(a['name']+' FBX triangle count',len(o.data.polygons)==orig['triangles'])
 check(a['name']+' FBX safe UV preserved',sorted((round(q.uv.x,6),round(q.uv.y,6)) for q in o.data.uv_layers.active.data)==orig['uvs'])
 check(a['name']+' FBX material preserved',o.data.materials[0].name.split('.')[0]==a['materials'][0])
 volume=sum(o.data.vertices[p.vertices[0]].co.dot(o.data.vertices[p.vertices[1]].co.cross(o.data.vertices[p.vertices[2]].co))/6 for p in o.data.polygons)
 check(a['name']+' FBX face winding preserved',abs(volume-orig['volume'])<1e-3)
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)
report=dict(status='PASS',checkCount=len(checks),checks=checks,scope='Saved Blender meshes and all 12 FBX reimports. Unity integration not tested.')
(ART/'Previews/Validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('POND_VALIDATION_PASS',len(checks),flush=True)
