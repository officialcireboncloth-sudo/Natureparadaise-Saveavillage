"""Render the real combined building, not editable components or schematic projections."""
from pathlib import Path
import bpy,sys
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Coop_A_Combined.blend'))
scene=bpy.data.scenes['Coop A - Combined Lv3'];bpy.context.window.scene=scene
camera=scene.camera
scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.cycles.samples=24
def aim(pos,target=(.2,-.2,2.5),scale=14):
 camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
views=[('FrontRight',(12,-16,12),(.2,-.2,2.5),14),('RearLeft',(-12,16,12),(.2,-.2,2.5),14),('Front',(0,-20,3),(0,0,2.9),10.4),('Right',(20,0,3),(0,-.4,2.9),11.5),('Rear',(0,20,3),(0,0,2.9),10.4),('Left',(-20,0,3),(0,-.4,2.9),11.5)]
if '--first-only' in sys.argv:views=views[:1]
if '--lod-only' in sys.argv or '--lod-and-comparison' in sys.argv:views=[]
for name,pos,target,scale in views:
 aim(pos,target,scale);scene.render.filepath=str(ART/'Previews'/f'Coop_A_{name}.png');bpy.ops.render.render(write_still=True)
if '--first-only' not in sys.argv:
 aim((12,-16,12))
 for lod in [1,2]:
  for obj in scene.objects:
   if obj.name.startswith('Coop_A_Master_LOD'):obj.hide_render=int(obj.name[-1])!=lod
  scene.render.filepath=str(ART/'Previews'/f'Coop_A_LOD{lod}.png');bpy.ops.render.render(write_still=True)
 if '--lod-only' not in sys.argv:
  levels=bpy.data.scenes['Coop A - Levels 1 to 5 - shared mesh'];levels.render.filepath=str(ART/'Previews/Coop_A_Levels_1_to_5.png');bpy.ops.render.render(write_still=True,scene=levels.name)
print('COOP_PREVIEWS_PASS: real combined mesh renders complete; saved production files unchanged.')
