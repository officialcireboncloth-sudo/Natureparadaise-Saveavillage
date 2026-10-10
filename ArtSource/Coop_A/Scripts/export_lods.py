"""Export one building mesh per FBX. Never copies into Assets or changes Unity."""
from pathlib import Path
import bpy,json,hashlib
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Coop_A_Combined.blend'))
bpy.context.window.scene=bpy.data.scenes['Coop A - Combined Lv3']
report=json.loads((ART/'Previews/MeshManifest.json').read_text())
for lod,name in enumerate(['Coop_A_High','Coop_A_Medium','Coop_A_Low']):
 obj=bpy.data.objects[f'Coop_A_Master_LOD{lod}'];bpy.ops.object.select_all(action='DESELECT');obj.hide_set(False);obj.hide_render=False;obj.select_set(True);bpy.context.view_layer.objects.active=obj
 original=obj.name;original_mesh=obj.data.name;obj.name=name;obj.data.name=name+'Mesh';path=ART/'Exports'/f'{name}.fbx'
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
 obj.name=original;obj.data.name=original_mesh
 report['lodMeshes'][lod]['export']='Exports/'+path.name;report['lodMeshes'][lod]['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
report['status']='MODEL_BUILT_EXPORTED_NOT_APPLIED_TO_UNITY'
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2)+'\n')
print('COOP_EXPORT_PASS: 3 neutral-named FBX files, one mesh each, no studio objects or new game textures.')
