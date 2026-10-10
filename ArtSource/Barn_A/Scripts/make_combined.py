"""Deliver one visible building mesh; preserve the original parts file separately."""
from pathlib import Path
import bpy

art=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(art/'Blender/Barn_A_Master.blend'))
scene=bpy.data.scenes['Barn A - Editable Master Lv3']
bpy.context.window.scene=scene
scene.name='Barn A - Combined Lv3'
source=bpy.data.collections['01 EDITABLE - master dimensions Lv3']
# Unlink the original parts only in this new deliverable. The original .blend
# remains unchanged, including its editable Boolean operands.
scene.collection.children.unlink(source)
exports=bpy.data.collections['02 EXPORT MESHES - hidden in preview'] if '02 EXPORT MESHES - hidden in preview' in bpy.data.collections else next(c for c in scene.collection.children if c.name.startswith('02 EXPORT'))
exports.name='01 BARN - one mesh per LOD'
exports.hide_viewport=False;exports.hide_render=False
for obj in list(exports.all_objects):
    if obj.type!='MESH' or not obj.name.startswith('Barn_A_Master_LOD'):
        continue
    lod=int(obj.name[-1])
    obj.hide_viewport=False
    obj.hide_render=lod!=0
    bpy.context.view_layer.update()
    obj.hide_set(lod!=0)
    obj['CombinedBuilding']=True
    obj['LOD']=lod
    obj['UnityAutomaticLODGeneration']=False
    obj.select_set(lod==0)
    if lod==0:bpy.context.view_layer.objects.active=obj
scene['VisibleBuildingMeshCount']=1
scene['UnityLOD']='Authored meshes LOD0/LOD1/LOD2; Unity LODGroup selects and culls.'
bpy.ops.wm.save_as_mainfile(filepath=str(art/'Blender/Barn_A_Combined.blend'))
assert len([o for o in scene.objects if o.type=='MESH' and not o.hide_render and o.name.startswith('Barn_A_Master')])==1
print('COMBINED_BARN_PASS: one visible mesh; three authored LOD meshes; original parts preserved')
