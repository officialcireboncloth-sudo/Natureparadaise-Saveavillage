"""Render the exported tulip meshes, including close cup and multi-angle reviews."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera;out=SOURCE/'Previews';out.mkdir(parents=True,exist_ok=True);scene.cycles.samples=12 if '--flower-update' in sys.argv else 16
roots=[o for o in bpy.data.objects if o.get('Phase')]
for root in roots:
 s=json.loads((MODELS/('NP_Tulip_'+root['Phase']+'.json')).read_text());me=bpy.data.meshes.new('Tulip runtime '+root['Phase'])
 me.from_pydata([(p[0],p[2],p[1]) for p in s['vertices']],[],[(a,c,b) for a,b,c in zip(*[iter(s['submeshes'][0])]*3)]);me.update();uv=me.uv_layers.new(name='AtlasUV')
 for face in me.polygons:
  face.use_smooth=True
  for li in face.loop_indices:uv.data[li].uv=s['uv'][me.loops[li].vertex_index]
 me.normals_split_custom_set_from_vertices([(n[0],n[2],n[1]) for n in s['normals']]);me.materials.append(bpy.data.materials[ATLAS]);ob=bpy.data.objects.new('Tulip runtime '+root['Phase'],me);scene.collection.objects.link(ob);ob.parent=root;ob['RuntimeGeometry']=True
def show(stage):
 for r in roots:
  for o in r.children_recursive:
   if o.type=='MESH':o.hide_render=not(r['Phase']==stage and o.get('RuntimeGeometry'))
def aim(target,angle=0,elevation=18,scale=1.4):
 a,e=map(math.radians,[angle,elevation]);target=Vector(target);camera.location=target+Vector((4*math.cos(e)*math.sin(a),-4*math.cos(e)*math.cos(a),4*math.sin(e)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
def frame(w,h,angle=0,elevation=18):
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(p) for r in roots for o in r.children_recursive if o.type=='MESH' and not o.hide_render for p in o.bound_box];aim((0,0,.5),angle,elevation);rot=camera.rotation_euler.to_matrix();right,up,forward=rot.col[0],rot.col[1],-rot.col[2];xx,yy,zz=([p.dot(v) for p in pts] for v in [right,up,forward]);center=right*(min(xx)+max(xx))*.5+up*(min(yy)+max(yy))*.5+forward*(min(zz)+max(zz))*.5;aspect=w/h;ww,hh=max(xx)-min(xx),max(yy)-min(yy);scale=max(ww,hh*aspect) if w>=h else max(hh,ww/aspect);aim(center,angle,elevation,scale*1.15)
def render(name,w,h):
 scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/('G11_Tulip_'+name+'.png'));bpy.ops.render.render(write_still=True);print('G11_RENDER',name,flush=True)
if '--spatial-only' in sys.argv:
 for stage in ['Young','Ready']:
  show(stage);bpy.data.objects['Tulip_'+stage].location=(0,0,0)
  for title,angle,elevation in [('Top',0,90),('Side',90,0),('Oblique',40,38)]:
   frame(600,760,angle,elevation);render(stage+'_R03_'+title,600,760)
 print('G11_SPATIAL_RENDERS_PASS',flush=True);sys.exit(0)
show('Ready');r=bpy.data.objects['Tulip_Ready'];r.location=(0,0,0);frame(750,1000);render('Ready',750,1000)
if '--quick' in sys.argv:sys.exit(0)
for stage in PHASES['Tulip']:
 if '--flower-update' in sys.argv and stage not in ['Ready','Harvest']:continue
 show(stage);r=bpy.data.objects['Tulip_'+stage];r.location=(0,0,0)
 if stage=='Harvest':frame(480,660,elevation=35)
 else:aim((0,0,.49),0,18,1.20)
 render(stage+'_Phase',480,660)
if '--phases-only' in sys.argv:sys.exit(0)
show('Ready')
for title,angle,e in [('Side',90,20),('Rear',180,20),('Top',0,83),('Field',0,52),('Oblique',40,38)]:frame(600,760,angle,e);render(title,600,760)
for r in roots:
 for o in r.children_recursive:
  if o.type=='MESH':o.hide_render=not(r['Phase']=='Ready' and o.get('FlowerKind')=='Tulip')
frame(800,800,elevation=28);render('FlowerDetail',800,800)
for r in roots:
 for o in r.children_recursive:
  if o.type=='MESH':o.hide_render=not o.get('RuntimeGeometry')
for i,stage in enumerate(PHASES['Tulip']):bpy.data.objects['Tulip_'+stage].location=((i-2.5)*.85,0,0)
frame(2100,680,elevation=20);render('Growth',2100,680)
print('G11_RENDERS_PASS',flush=True)
