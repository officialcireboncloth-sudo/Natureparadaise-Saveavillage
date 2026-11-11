"""Review both supplied berry species from actual exported runtime geometry and UVs."""
import bpy,sys,math,json,ast
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_config import *
from unified_plant_atlas import MAPS,source_settings,source_texture
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera;camera.data.sensor_fit='HORIZONTAL';out=SOURCE/'Previews';out.mkdir(parents=True,exist_ok=True);scene.cycles.samples=8;scene.render.threads=4
roots=[o for o in bpy.data.objects if o.get('Phase')];runtime=bpy.data.materials[ATLAS]
if json.loads((MODELS/'NP_Blueberry_Ready.json').read_text())['materials']==['NP_Plants_Unified_01']:
 source=SOURCE/'Scripts/render_unified_plant_atlas.py';tree=ast.parse(source.read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='material');ctx=dict(globals(),TEXTURES=RUNTIME_TEXTURES);exec(compile(ast.Module(body=[fn],type_ignores=[]),str(source),'exec'),ctx);runtime=ctx['material']('NP_Plants_Unified_01',True)
for root in roots:
 s=json.loads((MODELS/('NP_'+root['Plant']+'_'+root['Phase']+'.json')).read_text());me=bpy.data.meshes.new(root.name+' runtime');me.from_pydata([(x,z,y) for x,y,z in s['vertices']],[],[(a,c,b) for a,b,c in zip(*[iter(s['submeshes'][0])]*3)]);me.update();uv=me.uv_layers.new(name='AtlasUV')
 for face in me.polygons:
  face.use_smooth=True
  for li in face.loop_indices:uv.data[li].uv=s['uv'][me.loops[li].vertex_index]
 me.normals_split_custom_set_from_vertices([(x,z,y) for x,y,z in s['normals']]);me.materials.append(runtime);ob=bpy.data.objects.new(root.name+' runtime',me);scene.collection.objects.link(ob);ob.parent=root;ob['RuntimeGeometry']=True
def show(kind,stage):
 for r in roots:
  for o in r.children_recursive:
   if o.type=='MESH':o.hide_render=not(r['Plant']==kind and r['Phase']==stage and o.get('RuntimeGeometry'))
 bpy.data.objects[kind+'_'+stage].location=(0,0,0)
def aim(target,angle=0,elevation=18,scale=1.4):
 a,e=map(math.radians,[angle,elevation]);target=Vector(target);camera.location=target+Vector((4*math.cos(e)*math.sin(a),-4*math.cos(e)*math.cos(a),4*math.sin(e)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
def frame(w,h,angle=0,elevation=18):
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(p) for r in roots for o in r.children_recursive if o.type=='MESH' and not o.hide_render for p in o.bound_box];aim((0,0,.5),angle,elevation);rot=camera.rotation_euler.to_matrix();right,up,forward=rot.col[0],rot.col[1],-rot.col[2];xx,yy,zz=([p.dot(v) for p in pts] for v in [right,up,forward]);center=right*(min(xx)+max(xx))*.5+up*(min(yy)+max(yy))*.5+forward*(min(zz)+max(zz))*.5;aspect=w/h;ww,hh=max(xx)-min(xx),max(yy)-min(yy);aim(center,angle,elevation,max(ww,hh*aspect)*1.15)
def render(kind,name,w=600,h=760):
 scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/('G01_'+kind+'_'+name+'.png'));bpy.ops.render.render(write_still=True);print('G01_RENDER',kind,name,flush=True)
if '--spatial-only' in sys.argv:
 for kind in PHASES:
  if '--only' in sys.argv and kind!=sys.argv[sys.argv.index('--only')+1]:continue
  for stage in ['Young','Ready']:
   show(kind,stage)
   for title,angle,e in [('Top',0,90),('Side',90,10),('Oblique',40,38)]:frame(540,680,angle,e);render(kind,stage+'_'+title,540,680)
 print('G01_SPATIAL_RENDERS_PASS',flush=True);sys.exit(0)
for kind in PHASES:
 if '--only' in sys.argv and kind!=sys.argv[sys.argv.index('--only')+1]:continue
 show(kind,'Ready');frame(650,850);render(kind,'Ready',650,850)
 if '--quick' in sys.argv:
  show(kind,'Flowering');frame(650,850);render(kind,'Flowering_Check',650,850);show(kind,'Harvest');frame(650,650,elevation=28);render(kind,'FruitDetail',650,650);continue
 for stage in PHASES[kind]:
  show(kind,stage)
  if stage=='Harvest':frame(480,660,elevation=28)
  else:aim((0,0,.56),0,18,1.75)
  render(kind,stage+'_Phase',480,660)
 show(kind,'Ready');frame(650,850,15,52);render(kind,'Field',650,850)
 show(kind,'Harvest');frame(800,800,elevation=28);render(kind,'FruitDetail',800,800)
 show(kind,'Flowering');frame(650,850,15,20);render(kind,'Flowering_Check',650,850)
print('G01_RENDERS_PASS',flush=True)
