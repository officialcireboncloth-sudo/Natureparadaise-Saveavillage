"""Keep the editable Blender preview's ground UVs aligned with Unity's 18 m repeat."""
from pathlib import Path
import bpy

root = Path.cwd() / 'ArtSource' / 'StylizedMeadow'
source = root / 'Blender' / 'StylizedMeadow.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
ground = next(obj for obj in bpy.data.objects if obj.name.startswith('Preview Grass Ground'))
# Use local plane coordinates, not the previous UV multiplier, so rerunning is safe.
for loop in ground.data.loops:
    vertex = ground.data.vertices[loop.vertex_index].co
    ground.data.uv_layers.active.data[loop.index].uv = ((vertex.x + 6) / 18, (vertex.y + 6) / 18)
ground.name = 'Preview Grass Ground — 18m texture repeat'
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(source))
print('Ground UV updated: 12m plane / 18m texture repeat')
