"""Create primary one-building mesh file; preserve editable source as a separate file."""
from pathlib import Path
import bpy
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Coop_A_Master.blend'))
scene=bpy.data.scenes['Coop A - Editable Master Lv3'];bpy.context.window.scene=scene
scene.name='Coop A - Combined Lv3'
source=bpy.data.collections['01 EDITABLE - master dimensions Lv3'];scene.collection.children.unlink(source)
exports=bpy.data.collections['02 EXPORT MESHES - identity transforms'];exports.name='01 COOP - one mesh per LOD';exports.hide_viewport=False;exports.hide_render=False
bpy.ops.object.select_all(action='DESELECT')
for obj in exports.all_objects:
 if obj.type!='MESH' or not obj.name.startswith('Coop_A_Master_LOD'):continue
 lod=int(obj.name[-1]);obj.hide_viewport=False;obj.hide_render=lod!=0;obj.hide_set(lod!=0);obj.select_set(lod==0)
 obj['CombinedBuilding']=True;obj['LOD']=lod;obj['UnityAutomaticLODGeneration']=False
 if lod==0:bpy.context.view_layer.objects.active=obj
bpy.data.collections['04 PREVIEW STUDIO - never export'].hide_viewport=True
scene['VisibleBuildingMeshCount']=1;scene['UnityLOD']='Three authored meshes; Unity LODGroup selects/culls, no geometry generation'
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
bpy.context.view_layer.update()
assert len([o for o in scene.objects if o.type=='MESH' and not o.hide_render and o.name.startswith('Coop_A_Master')])==1
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Coop_A_Combined.blend'))
print('COOP_COMBINED_PASS: one visible building mesh; editable source preserved; LOD1/2 hidden.')
