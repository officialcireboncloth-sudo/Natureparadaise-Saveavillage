"""Refresh comparison labels/render without rebuilding or modifying production meshes."""
from pathlib import Path
import bpy

art=Path(__file__).resolve().parents[1]
path=art/'Blender/Barn_A_Master.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
mat=bpy.data.materials.get('PREVIEW ONLY - level labels')
if mat is None:
    mat=bpy.data.materials.new('PREVIEW ONLY - level labels')
mat.use_nodes=True
node=mat.node_tree.nodes.get('Principled BSDF')
node.inputs['Base Color'].default_value=(.08,.055,.035,1)
node.inputs['Roughness'].default_value=1
levels=bpy.data.scenes['Barn A - Levels 1 to 5 - shared mesh']
for obj in levels.objects:
    if obj.type=='FONT':
        obj.data.materials.clear()
        obj.data.materials.append(mat)
levels.render.filepath=str(art/'Previews/Barn_A_Levels_1_to_5.png')
bpy.ops.render.render(write_still=True,scene=levels.name)
bpy.context.window.scene=bpy.data.scenes['Barn A - Editable Master Lv3']
bpy.ops.wm.save_as_mainfile(filepath=str(path))
print('COMPARISON_RENDER_COMPLETE')
