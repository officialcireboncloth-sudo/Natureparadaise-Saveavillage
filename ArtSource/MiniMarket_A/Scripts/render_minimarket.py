import bpy
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/MiniMarket_A.blend'))
sc=bpy.data.scenes['MiniMarket A - EDITABLE'];bpy.context.window.scene=sc;cam=sc.camera
for name,pos,target,scale in [('FrontRight',(14,-19,12),(0,-.3,2),15.7),('RearLeft',(-14,19,12),(0,0,2),15.7),('Front',(0,-25,5),(0,-.3,2.3),13.1)]:
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 sc.render.engine='CYCLES';sc.cycles.samples=12;sc.render.resolution_x=1200;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.filepath=str(ART/'Previews'/f'{name}.png')
 bpy.ops.render.render(write_still=True)
print('MINIMARKET_RENDER_COMPLETE')
