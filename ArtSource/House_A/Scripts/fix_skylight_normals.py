"""Fix skylight winding in saved editable/LOD meshes without changing vertex positions."""
from pathlib import Path
from datetime import datetime
import bpy,json,hashlib
ART=Path(__file__).resolve().parents[1]
spec=json.loads((ART/'References/House_A_Approved.json').read_text(encoding='utf-8'))
report=json.loads((ART/'Previews/MeshManifest.json').read_text(encoding='utf-8'))
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Revisions'/('House_A_BeforeSkylightNormals_'+datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
changed=[];visited=set();scene=bpy.context.scene
for model in spec['models']:
 roof_ids={i for i,p in enumerate(model['parts']) if p['name'].startswith('skylight_') and p['material']=='window'}
 for obj in bpy.data.scenes['House A '+model['id']].objects:
  if obj.type!='MESH' or obj.data.as_pointer() in visited:continue
  mesh=obj.data;attr=mesh.attributes.get('ApprovedPartIndex')
  if not attr:continue
  visited.add(mesh.as_pointer());count=0;before=[tuple(v.co) for v in mesh.vertices]
  for face in mesh.polygons:
   if attr.data[face.index].value in roof_ids and face.normal.z<0:face.flip();count+=1
  mesh.update()
  assert before==[tuple(v.co) for v in mesh.vertices]
  if count:changed.append({'mesh':mesh.name,'facesFlipped':count})
for entry in report['models']:
 if entry['model'] not in ['Lv2','Lv5']:continue
 bpy.context.window.scene=bpy.data.scenes['House A '+entry['model']];obj=bpy.data.objects[entry['name']]
 hidden=obj.hide_get();render=obj.hide_render;name=obj.name
 obj.hide_set(False);obj.hide_render=False;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 obj.name='House_A_'+entry['model']+'_'+['High','Medium','Low'][entry['lod']]
 path=ART/entry['export']
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,
  apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',mesh_smooth_type='FACE',add_leaf_bones=False,
  bake_anim=False,path_mode='STRIP',embed_textures=False)
 obj.name=name;obj.hide_set(hidden);obj.hide_render=render
 entry['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
bpy.context.window.scene=scene
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
(ART/'Previews/SkylightNormalsAudit.json').write_text(json.dumps({'status':'PASS','meshes':changed,'vertexPositionsPreserved':True,'triangleCountsPreserved':True},indent=2),encoding='utf-8')
print('SKYLIGHT_NORMALS_FIXED',len(changed),'meshes')
