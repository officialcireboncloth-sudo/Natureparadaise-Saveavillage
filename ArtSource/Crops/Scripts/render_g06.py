"""Render actual G06 models, including four independent camera directions."""
import bpy, sys, math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g06_config import *

bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;camera=scene.camera
roots=[o for o in bpy.data.objects if o.get('Phase') or o.name in ['NP_Radish_CutHalf','NP_Onion_CutHalf']]
for o in bpy.data.collections['STUDIO - excluded from exports'].objects:
    if o.type=='FONT':o.hide_render=True
scene.cycles.samples=12
out=SOURCE/'Previews'

def visible(root,show):
    for o in root.children_recursive:
        if o.type=='MESH':o.hide_render=not show

def reset():
    for r in roots:visible(r,False)

def aim(target,angle=0,elevation=22,distance=4,scale=1.44):
    a,e=math.radians(angle),math.radians(elevation);target=Vector(target)
    camera.location=target+Vector((distance*math.cos(e)*math.sin(a),-distance*math.cos(e)*math.cos(a),distance*math.sin(e)))
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale

def render(name,w,h):
    scene.render.resolution_x=w;scene.render.resolution_y=h;scene.render.filepath=str(out/name);bpy.ops.render.render(write_still=True)
    print('G06_RENDER',name,flush=True)

def frame_visible(w,h,angle=0,elevation=18):
    """Fit visible crop bounds in camera space, including laid-down harvest foliage."""
    bpy.context.view_layer.update()
    points=[o.matrix_world@Vector(p) for r in roots for o in r.children_recursive if o.type=='MESH' and not o.hide_render for p in o.bound_box]
    aim((0,0,.4),angle,elevation)
    rotation=camera.rotation_euler.to_matrix();right=rotation.col[0];up=rotation.col[1];forward=-rotation.col[2]
    xx=[p.dot(right) for p in points];yy=[p.dot(up) for p in points];zz=[p.dot(forward) for p in points]
    center=right*((min(xx)+max(xx))*.5)+up*((min(yy)+max(yy))*.5)+forward*((min(zz)+max(zz))*.5)
    width=max(xx)-min(xx);height=max(yy)-min(yy);aspect=w/h
    scale=max(width,height*aspect) if w>=h else max(height,width/aspect)
    aim(center,angle,elevation,scale=scale*1.15)

def growth_stages(selected=None):
    for kind,stages in PHASES.items():
        for stage in stages:
            if selected is not None and (kind,stage) not in selected:continue
            reset();r=bpy.data.objects[kind+'_'+stage];r.location=(0,0,0);visible(r,True)
            if stage=='Harvest' and kind=='Radish':frame_visible(430,600,elevation=16)
            else:aim((0,0,.53),elevation=14,scale=1.40)
            render('G06_'+kind+'_'+stage+'_Phase.png',430,600)

def harvest_study():
    reset()
    for i,kind in enumerate(PHASES):
        r=bpy.data.objects[kind+'_Harvest'];r.location=((i-.5)*1.30,.09,0);r.rotation_euler.z=0;visible(r,True)
        cut=bpy.data.objects['NP_'+kind+'_CutHalf'];cut.location=((i-.5)*1.30+(.47 if kind=='Radish' else .29),-.22,0);visible(cut,True)
    frame_visible(1800,1000,elevation=25);render('G06_HarvestStudy.png',1800,1000)

if '--changed-phases-only' in sys.argv:
    growth_stages({('Radish','Sprout'),('Radish','Young'),('Onion','Harvest')});sys.exit(0)
if '--sheets-only' in sys.argv:
    growth_stages();harvest_study();print('G06_SHEET_RENDERS_PASS',flush=True);sys.exit(0)

reset()
for kind in PHASES:
    r=bpy.data.objects[kind+'_Ready'];r.location=(0,0,0);visible(r,True)
    aim((0,0,.50 if kind=='Radish' else .55),elevation=14,scale=1.35 if kind=='Radish' else 1.42)
    render('G06_'+kind+'_Ready.png',920,1080);visible(r,False)
    if '--quick' in sys.argv:continue
    for label,angle,elevation in [('Front',0,12),('Side',90,12),('Rear',180,12),('Top',0,88)]:
        visible(r,True);aim((0,0,.50 if label!='Top' else .40),angle,elevation,scale=1.45)
        render('G06_'+kind+'_'+label+'.png',650,760);visible(r,False)
if '--quick' in sys.argv:sys.exit(0)

reset()
for i,kind in enumerate(PHASES):
    r=bpy.data.objects[kind+'_Ready'];r.location=((i-.5)*1.12,0,0);visible(r,True)
aim((0,0,.52),elevation=22,scale=2.55);render('G06_Closeup.png',1800,1100)
growth_stages();harvest_study()
print('G06_RENDERS_PASS',flush=True)
