"""Render individual leaf components from the published native geometry, not the authoring prototype."""
import bpy,sys,ast,math
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import unified_plant_atlas as U
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));scene=bpy.context.scene;camera=scene.camera;camera.data.sensor_fit='HORIZONTAL';scene.cycles.samples=16;scene.render.threads=4
for ob in bpy.data.objects:
 if ob.type=='MESH' and any(p.get('Phase') for p in [ob.parent] if p):ob.hide_render=True
source=Path(__file__).with_name('render_unified_plant_atlas.py');tree=ast.parse(source.read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='material');ctx=dict(globals(),TEXTURES=G.RUNTIME_TEXTURES,MAPS=U.MAPS,source_texture=U.source_texture,source_settings=U.source_settings);exec(compile(ast.Module(body=[fn],type_ignores=[]),str(source),'exec'),ctx);mat=ctx['material'](U.NAME,True);registry=U.load(U.REGISTRY);shown=None
for kind in ['Blueberry','Raspberry','Blackberry']:
 if '--rubus-only' in sys.argv and kind=='Blueberry':continue
 if shown:bpy.data.objects.remove(shown,do_unlink=True)
 root=bpy.data.objects[kind+'_Ready'];leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')==kind];reference=max(leaves,key=lambda o:float(o['LeafLength']));target=np.array([tuple(v.co) for v in reference.data.vertices]).mean(axis=0);spec=U.load(G.MODELS/('NP_'+kind+'_Ready.json'));page='NP_Plants_Raspberry_Source_01' if kind=='Raspberry' else G.ATLAS;s=next(s for s in registry['slots'] if s['key']==page+':'+kind+'Leaf');a,b,c,d=s['uvSafe'];uv=np.array(spec['uv']);inside=(uv[:,0]>=a-1e-6)&(uv[:,0]<=c+1e-6)&(uv[:,1]>=b-1e-6)&(uv[:,1]<=d+1e-6);faces=np.array(spec['submeshes'][0]).reshape(-1,3);faces=faces[np.all(inside[faces],axis=1)];parent={}
 def find(i):
  parent.setdefault(i,i)
  if parent[i]!=i:parent[i]=find(parent[i])
  return parent[i]
 for face in faces:
  x,y,z=map(int,face);r=find(x);parent[find(y)]=r;parent[find(z)]=r
 groups={}
 for face in faces:groups.setdefault(find(int(face[0])),[]).append(face)
 v=np.array([(x,z,y) for x,y,z in spec['vertices']]);group=min(groups.values(),key=lambda f:float(np.linalg.norm(v[np.unique(np.array(f))].mean(axis=0)-target)));indices=sorted(set(int(i) for f in group for i in f));remap={old:new for new,old in enumerate(indices)};me=bpy.data.meshes.new(kind+' actual near leaf');me.from_pydata([v[i] for i in indices],[],[(remap[int(f[0])],remap[int(f[2])],remap[int(f[1])]) for f in group]);me.update();layer=me.uv_layers.new(name='AtlasUV')
 for face in me.polygons:
  face.use_smooth=True
  for li in face.loop_indices:layer.data[li].uv=spec['uv'][indices[me.loops[li].vertex_index]]
 me.normals_split_custom_set_from_vertices([(spec['normals'][i][0],spec['normals'][i][2],spec['normals'][i][1]) for i in indices]);me.materials.append(mat);shown=bpy.data.objects.new(kind+' actual runtime leaf detail',me);scene.collection.objects.link(shown)
 aa=math.radians(float(reference['LeafAzimuth']));ee=math.radians(float(reference['LeafElevation']));rr=float(reference['LeafRoll']);radial=Vector((math.cos(aa),math.sin(aa),0));tangent=(radial*math.cos(ee)+Vector((0,0,math.sin(ee)))).normalized();across=Vector((radial.y,-radial.x,0));normal=across.cross(tangent).normalized();right=across*math.cos(rr)+normal*math.sin(rr);back=-across*math.sin(rr)+normal*math.cos(rr);up=tangent;points=[Vector(v[i]) for i in indices];xx=[p.dot(right) for p in points];yy=[p.dot(up) for p in points];zz=[p.dot(back) for p in points];center=right*((min(xx)+max(xx))*.5)+up*((min(yy)+max(yy))*.5)+back*((min(zz)+max(zz))*.5);camera.location=center+back*3;camera.rotation_euler=Matrix((right,up,back)).transposed().to_euler();camera.data.ortho_scale=max(max(xx)-min(xx),(max(yy)-min(yy))*650/850)*1.18;scene.render.resolution_x=650;scene.render.resolution_y=850;scene.render.filepath=str(G.SOURCE/'Previews'/('G01_'+kind+'_LeafDetail_R05.png'));bpy.ops.render.render(write_still=True);print('R05_NATIVE_LEAF_DETAIL',kind,'triangles',len(group),flush=True)
