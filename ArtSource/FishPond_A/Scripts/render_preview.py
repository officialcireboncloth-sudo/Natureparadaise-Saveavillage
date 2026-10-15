"""Render saved production meshes; no regenerated geometry."""
from pathlib import Path
import bpy
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/FishPond_A_Masters.blend'))
for lv in range(1,4):
 s=bpy.data.scenes['Fish Pond A Lv'+str(lv)];bpy.context.window.scene=s
 cam=s.camera;initial=cam.location.copy();rot=cam.rotation_euler.copy()
 center=initial+cam.rotation_euler.to_quaternion()@Vector((0,0,-23*Vector((1,-1,1.15)).length))
 for side in ['FrontRight','RearLeft']:
  if side=='RearLeft':cam.location=center+Vector((-1,1,1.15))*23;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
  s.render.filepath=str(ART/'Previews'/f'FishPond_A_Lv{lv}_{side}.png');bpy.ops.render.render(write_still=True)
 cam.location=initial;cam.rotation_euler=rot
s=bpy.data.scenes['Fish Pond A Levels 1-3'];s.render.filepath=str(ART/'Previews/FishPond_A_Levels_1_to_3.png')
bpy.ops.render.render(write_still=True,scene=s.name)
print('POND_PREVIEWS_COMPLETE',flush=True)
