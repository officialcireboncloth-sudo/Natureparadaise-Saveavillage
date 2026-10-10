"""Export combined LODs with neutral node names; Unity prefabs own LODGroup settings."""
from pathlib import Path
import bpy
art=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(art/'Blender/Barn_A_Combined.blend'))
bpy.context.window.scene=bpy.data.scenes['Barn A - Combined Lv3']
for lod,name in enumerate(('Barn_A_High','Barn_A_Medium','Barn_A_Low')):
    obj=bpy.data.objects[f'Barn_A_Master_LOD{lod}']
    bpy.ops.object.select_all(action='DESELECT')
    obj.hide_set(False);obj.select_set(True);bpy.context.view_layer.objects.active=obj
    obj.name=name;obj.data.name=name+'Mesh'
    bpy.ops.export_scene.fbx(filepath=str(art/'Exports'/f'Barn_A_Master_LOD{lod}.fbx'),use_selection=True,
        object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',
        add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
print('UNITY_LOD_EXPORT_PASS: three standalone meshes, no incomplete automatic LOD naming')
