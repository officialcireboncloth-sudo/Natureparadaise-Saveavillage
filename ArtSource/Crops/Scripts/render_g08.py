"""Unretouched neutral-studio renders for every cereal stage and view."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera
roots=[o for o in bpy.data.objects if o.get('Phase') or o.name in EXTRAS.values()]
for o in bpy.data.collections['STUDIO - excluded from exports'].objects:
 if o.type=='FONT':o.hide_render=True
scene.cycles.samples=18;out=SOURCE/'Previews';out.mkdir(parents=True,exist_ok=True)

def visible(root,show):
 for o in root.children_recursive:
  if o.type=='MESH':o.hide_render=not show

def reset():
 for r in roots:visible(r,False)

def aim(target,angle=0,elevation=22,scale=1.5):
 a,e=map(math.radians,[angle,elevation]);target=Vector(target)
 camera.location=target+Vector((4*math.cos(e)*math.sin(a),-4*math.cos(e)*math.cos(a),4*math.sin(e)))
 camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale

def render(name,w,h):
 scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/name);bpy.ops.render.render(write_still=True);print('G08_RENDER',name,flush=True)

def frame(w,h,elevation=24,angle=0):
 bpy.context.view_layer.update();points=[o.matrix_world@Vector(p) for r in roots for o in r.children_recursive if o.type=='MESH' and not o.hide_render for p in o.bound_box]
 aim((0,0,.5),angle,elevation);rot=camera.rotation_euler.to_matrix();right,up,forward=rot.col[0],rot.col[1],-rot.col[2]
 xx,yy,zz=([p.dot(v) for p in points] for v in [right,up,forward]);center=right*((min(xx)+max(xx))*.5)+up*((min(yy)+max(yy))*.5)+forward*((min(zz)+max(zz))*.5)
 width,height=max(xx)-min(xx),max(yy)-min(yy);aspect=w/h
 scale=max(width,height*aspect) if w>=h else max(height,width/aspect)
 aim(center,angle,elevation,scale*1.16)

def plant_camera(kind,w,h,angle=0,elevation=22):
 s=json.loads((MODELS/('NP_'+kind+'_Ready.json')).read_text());dims=[b-a for a,b in zip(s['min'],s['max'])];horizontal=max(dims[0],dims[2]);height=dims[1]
 scale=max(height,horizontal/(w/h))*1.18 if w<h else max(horizontal,height*(w/h))*1.18
 aim((0,0,height*.48),angle,elevation,scale)

if '--wheat-alignment-only' in sys.argv:
 for stage in ['Heading','Harvest']:
  reset();r=bpy.data.objects['Wheat_'+stage];r.location=(0,0,0);visible(r,True)
  if stage=='Heading':plant_camera('Wheat',430,600,elevation=18)
  else:frame(430,600,elevation=48)
  render('G08_Wheat_'+stage+'_Phase.png',430,600)
  for title,angle in [('Front',0),('Side',90),('Rear',180)]:
   frame(650,760,elevation=24 if stage=='Heading' else 48,angle=angle);render('G08_Wheat_'+stage+'_R02_'+title+'.png',650,760)
 reset()
 for i,kind in enumerate(PHASES):
  r=bpy.data.objects[kind+'_Harvest'];r.location=((i-.5)*1.20,.12,0);r.rotation_euler.z=math.radians(-12 if i==0 else 10);visible(r,True)
 for key,pos in [('RiceHuskPile',(-.79,-.40,0)),('RiceWhitePile',(-.35,-.40,0)),('WheatGrainPile',(.69,-.40,0))]:
  r=bpy.data.objects[EXTRAS[key]];r.location=pos;visible(r,True)
 frame(1800,1100,elevation=43);render('G08_HarvestStudy.png',1800,1100);print('G08_WHEAT_ALIGNMENT_RENDERS_PASS',flush=True);sys.exit(0)
if '--harvest-only' in sys.argv:
 for kind in PHASES:
  reset();r=bpy.data.objects[kind+'_Harvest'];r.location=(0,0,0);visible(r,True);frame(430,600,elevation=48);render('G08_'+kind+'_Harvest_Phase.png',430,600)
 reset()
 for i,kind in enumerate(PHASES):
  r=bpy.data.objects[kind+'_Harvest'];r.location=((i-.5)*1.20,.12,0);r.rotation_euler.z=math.radians(-12 if i==0 else 10);visible(r,True)
 for key,pos in [('RiceHuskPile',(-.79,-.40,0)),('RiceWhitePile',(-.35,-.40,0)),('WheatGrainPile',(.69,-.40,0))]:
  r=bpy.data.objects[EXTRAS[key]];r.location=pos;visible(r,True)
 frame(1800,1100,elevation=43);render('G08_HarvestStudy.png',1800,1100);print('G08_HARVEST_RENDERS_PASS',flush=True);sys.exit(0)
reset()
for kind in PHASES:
 r=bpy.data.objects[kind+'_Ready'];r.location=(0,0,0);visible(r,True);frame(920,1080,elevation=18)
 if '--skip-ready' not in sys.argv:render('G08_'+kind+'_Ready.png',920,1080)
 visible(r,False)
if '--quick' in sys.argv:sys.exit(0)
for kind in PHASES:
 r=bpy.data.objects[kind+'_Ready'];r.location=(0,0,0)
 for title,a,e in [('Front',0,18),('Side',90,18),('Rear',180,18),('Top',0,88)]:
  visible(r,True);frame(650,760,elevation=e,angle=a);render('G08_'+kind+'_'+title+'.png',650,760);visible(r,False)
reset()
for i,kind in enumerate(PHASES):r=bpy.data.objects[kind+'_Ready'];r.location=((i-.5)*1.18,0,0);visible(r,True)
frame(1800,1200,elevation=20);render('G08_Closeup.png',1800,1200)
for kind,stages in PHASES.items():
 for stage in stages:
  reset();r=bpy.data.objects[kind+'_'+stage];r.location=(0,0,0);visible(r,True)
  if stage=='Harvest':frame(430,600,elevation=38)
  else:plant_camera(kind,430,600,elevation=18)
  render('G08_'+kind+'_'+stage+'_Phase.png',430,600)
reset()
for i,kind in enumerate(PHASES):
 r=bpy.data.objects[kind+'_Harvest'];r.location=((i-.5)*1.20,.12,0);r.rotation_euler.z=math.radians(-12 if i==0 else 10);visible(r,True)
for key,pos in [('RiceHuskPile',(-.79,-.40,0)),('RiceWhitePile',(-.35,-.40,0)),('WheatGrainPile',(.69,-.40,0))]:
 r=bpy.data.objects[EXTRAS[key]];r.location=pos;visible(r,True)
frame(1800,1100,elevation=43);render('G08_HarvestStudy.png',1800,1100)
for kind in PHASES:
 reset();r=bpy.data.objects[kind+'_Seed'];r.location=(0,0,0);visible(r,True);frame(920,700,elevation=32,angle=-18);render('G08_'+kind+'_SeedDetail.png',920,700)
print('G08_RENDERS_PASS',flush=True)
