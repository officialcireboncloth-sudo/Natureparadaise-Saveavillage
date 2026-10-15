import bpy,json
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
# Run with saved .blend as command-line input. This script never saves changes.
for sc in [s for s in bpy.data.scenes if 'COOP INTERIOR - LEVEL' in s.name]:
 bpy.context.window.scene=sc;sc.cycles.samples=16;bpy.ops.render.render(write_still=True)
sc=next(s for s in bpy.data.scenes if s.name.startswith('04 COOP INTERIOR'));bpy.context.window.scene=sc
cam=sc.camera;cam.location=(5.2,6.3,4.8);target=Vector((4.9,12.9,1.35));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=5.8
sc.render.filepath=str(ART/'Previews/Lv4_Incubator_Mayo_Detail.png');bpy.ops.render.render(write_still=True)
cam.location=(-3.5,8.3,4.5);target=Vector((-5.2,12.8,.8));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=5
sc.render.filepath=str(ART/'Previews/Lv4_FeedMaker_Detail.png');bpy.ops.render.render(write_still=True)
print('COOP_REVIEW_RENDER_COMPLETE',flush=True)
