"""Render actual G09 growth, volume, flower detail and cut-flower poses."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
if '--r02-draft' in sys.argv:MASTER=REVIEW/'G09_R02_Draft.blend'
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera;roots=[o for o in bpy.data.objects if o.get('Phase')];out=SOURCE/'Previews';out.mkdir(parents=True,exist_ok=True);scene.cycles.samples=16
if '--output-review' in sys.argv:out=REVIEW/'R02_FinalRenders';out.mkdir(parents=True,exist_ok=True)
if '--r02-draft' in sys.argv:out=REVIEW/'R02_DraftRenders';out.mkdir(parents=True,exist_ok=True)
for o in bpy.data.collections['STUDIO - excluded from exports'].objects:
 if o.type=='FONT':o.hide_render=True
def visible(root,show):
 for o in root.children_recursive:
  if o.type=='MESH':o.hide_render=not show
def reset():
 for r in roots:visible(r,False)
def aim(target,angle=0,elevation=20,scale=1.5):
 a,e=map(math.radians,[angle,elevation]);target=Vector(target);camera.location=target+Vector((4*math.cos(e)*math.sin(a),-4*math.cos(e)*math.cos(a),4*math.sin(e)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
def frame(w,h,elevation=22,angle=0):
 bpy.context.view_layer.update();points=[o.matrix_world@Vector(p) for r in roots for o in r.children_recursive if o.type=='MESH' and not o.hide_render for p in o.bound_box]
 aim((0,0,.5),angle,elevation);rot=camera.rotation_euler.to_matrix();right,up,forward=rot.col[0],rot.col[1],-rot.col[2];xx,yy,zz=([p.dot(v) for p in points] for v in [right,up,forward]);center=right*((min(xx)+max(xx))*.5)+up*((min(yy)+max(yy))*.5)+forward*((min(zz)+max(zz))*.5)
 width,height=max(xx)-min(xx),max(yy)-min(yy);aspect=w/h;scale=max(width,height*aspect) if w>=h else max(height,width/aspect);aim(center,angle,elevation,scale*1.17)
def render(name,w,h):
 if '--r02-draft' in sys.argv:name=name.replace('G09_','G09_R02_',1)
 if '--clay' in sys.argv:name=name.replace('.png','_Clay.png')
 scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/name);bpy.ops.render.render(write_still=True);print('G09_RENDER',name,flush=True)
bpy.context.view_layer.update();bounds={}
for kind in PHASES:
 root=bpy.data.objects[kind+'_Ready'];points=[root.matrix_world.inverted()@o.matrix_world@Vector(p) for o in root.children_recursive if o.type=='MESH' for p in o.bound_box];dims=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)];bounds[kind]=dims
if '--flower-only' in sys.argv:
 if '--clay' in sys.argv:
  mat=bpy.data.materials.new('R02 neutral clay');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.54,.54,.54,1);bs.inputs['Roughness'].default_value=.48;scene.view_layers[0].material_override=mat
 for kind in PHASES:
  reset();root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0)
  for o in root.children_recursive:
   if o.type=='MESH':o.hide_render=not(o.get('FlowerKind')==kind and o.get('FlowerIndex')==0)
  for title,angle in [('Front',0),('Oblique',43)]:frame(800,800,elevation=22,angle=angle);render('G09_'+kind+'_Flower'+title+'.png',800,800)
 print('G09_FLOWER_FORM_RENDERS_PASS',flush=True);sys.exit(0)
if '--harvest-only' in sys.argv:
 reset();root=bpy.data.objects['Jasmine_Harvest'];root.location=(0,0,0);visible(root,True);frame(430,600,elevation=42);render('G09_Jasmine_Harvest_Phase.png',430,600)
 reset()
 for i,kind in enumerate(PHASES):root=bpy.data.objects[kind+'_Harvest'];root.location=((i-1)*.78,0,0);visible(root,True)
 frame(2100,1000,elevation=48);render('G09_HarvestStudy.png',2100,1000);print('G09_HARVEST_RENDER_PASS',flush=True);sys.exit(0)
reset()
for kind in PHASES:
 root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0);visible(root,True);frame(920,1080,elevation=20)
 if '--skip-ready' not in sys.argv:render('G09_'+kind+'_Ready.png',920,1080)
 visible(root,False)
if '--quick' in sys.argv:sys.exit(0)
for kind in PHASES:
 root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0)
 for title,angle,e in [('Front',0,20),('Side',90,20),('Rear',180,20),('Top',0,88)]:
  visible(root,True);frame(650,760,e,angle);render('G09_'+kind+'_'+title+'.png',650,760);visible(root,False)
reset()
for i,kind in enumerate(PHASES):root=bpy.data.objects[kind+'_Ready'];root.location=((i-1)*1.05,0,0);visible(root,True)
frame(2100,1050,elevation=20);render('G09_Closeup.png',2100,1050)
for kind,stages in PHASES.items():
 for stage in stages:
  reset();root=bpy.data.objects[kind+'_'+stage];root.location=(0,0,0);visible(root,True)
  if stage=='Harvest':frame(430,600,elevation=42)
  else:
   dims=bounds[kind];horizontal=max(dims[0],dims[1]);height=dims[2];aim((0,0,height*.48),0,20,max(height,horizontal/(430/600))*1.20)
  render('G09_'+kind+'_'+stage+'_Phase.png',430,600)
reset()
for i,kind in enumerate(PHASES):root=bpy.data.objects[kind+'_Harvest'];root.location=((i-1)*.78,0,0);visible(root,True)
frame(2100,1000,elevation=48);render('G09_HarvestStudy.png',2100,1000)
for kind in PHASES:
 reset();root=bpy.data.objects[kind+'_Ready'];root.location=(0,0,0)
 for o in root.children_recursive:
  if o.type=='MESH':o.hide_render=not(o.get('FlowerKind')==kind and o.get('FlowerIndex')==0)
 frame(900,900,elevation=20);render('G09_'+kind+'_FlowerDetail.png',900,900)
print('G09_RENDERS_PASS',flush=True)
