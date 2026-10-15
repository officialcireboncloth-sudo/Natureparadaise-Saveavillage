import bpy
from pathlib import Path
ART=Path(__file__).resolve().parents[1]
target=ART/'Blender/ChickenStore_A.blend'
bpy.ops.wm.open_mainfile(filepath=str(target))
if 'Original Scene - preserved' not in bpy.data.scenes:
 with bpy.data.libraries.load(str(ART/'References/OriginalSceneSnapshot.blend'),link=False) as (src,dst):
  dst.scenes=['Scene.001']
 initial=dst.scenes[0]
 assert initial and {'Cube','Camera','Light'}<=set(o.name.split('.')[0] for o in initial.objects)
 initial.name='Original Scene - preserved'
 assert all(o.library is None for o in initial.objects)
 bpy.ops.wm.save_as_mainfile(filepath=str(target))
print('ORIGINAL_SCENE_PRESERVED')
