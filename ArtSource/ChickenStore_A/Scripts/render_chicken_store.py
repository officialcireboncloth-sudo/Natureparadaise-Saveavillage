import bpy
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/ChickenStore_A.blend'))
views=[
 ('00 Chicken Store + Coop Overview','Overview',(13,-20,12),(0,0,2.2),17),
 ('01 Chicken Store - Editable','Store_FrontLeft',(-13,-18,11),(0,-.3,2.4),12.2),
 ('01 Chicken Store - Editable','Store_RearRight',(13,18,11),(0,0,2.4),12.2),
 ('02 Coop Companion - Editable','Coop_FrontRight',(6,-8,5),(0,-.2,1.5),5.7),
 ('02 Coop Companion - Editable','Coop_RearLeft',(-6,8,5),(0,0,1.5),5.7),
]
for scene,name,pos,target,scale in views:
 sc=bpy.data.scenes[scene];bpy.context.window.scene=sc;cam=sc.camera
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 sc.render.engine='CYCLES';sc.cycles.samples=12;sc.cycles.use_denoising=True
 sc.render.resolution_x=1200;sc.render.resolution_y=900;sc.render.resolution_percentage=100
 sc.render.image_settings.file_format='PNG';sc.render.filepath=str(ART/'Previews'/f'{name}.png')
 bpy.ops.render.render(write_still=True)
print('CHICKEN_RENDER_COMPLETE')
