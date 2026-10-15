import bpy,sys
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/FarmBuildingsFence_A.blend'))
for name,scale in ([] if '--overview' in sys.argv else [('Farm_Lumbung_A',12),('Farm_BarnStore_B',14)]):
 sc=bpy.data.scenes[name+' Editable'];bpy.context.window.scene=sc;cam=sc.camera
 sc.cycles.samples=8;sc.render.resolution_x=1100;sc.render.resolution_y=825
 for label,pos in [('FrontRight',(12,-16,11)),('RearLeft',(-12,16,11)),('Front',(0,-20,3)),('Right',(20,0,3))]:
  cam.location=pos;cam.rotation_euler=(Vector((0,0,2.65))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
  sc.render.filepath=str(ART/'Previews'/f'{name}_{label}.png');bpy.ops.render.render(write_still=True)
sc=bpy.data.scenes['00 Farm + Fence Overview'];bpy.context.window.scene=sc;cam=sc.camera
sc.cycles.samples=10;sc.render.resolution_x=1400;sc.render.resolution_y=1000
cam.location=(24,-33,24);cam.rotation_euler=(Vector((0,-2,2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=27
sc.render.filepath=str(ART/'Previews/FarmKit_Overview.png');bpy.ops.render.render(write_still=True)
cam.location=(5,-20,12);cam.rotation_euler=(Vector((0,-7,.6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=23
sc.render.filepath=str(ART/'Previews/FenceKit_Modules.png');bpy.ops.render.render(write_still=True)
print('FINAL_RENDER_COMPLETE')
