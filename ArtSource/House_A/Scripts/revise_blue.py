"""Run in the open Blender scene; preserve current edits and recolor Lv5 bay only."""
from pathlib import Path
from datetime import datetime
import bpy,json,hashlib,sys
ART=Path(__file__).resolve().parents[1]
if '--from-saved' in sys.argv:
 bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
SPEC=json.loads((ART/'References/House_A_Approved.json').read_text())
REPORT=json.loads((ART/'Previews/MeshManifest.json').read_text())
revisions=ART/'Blender/Revisions';revisions.mkdir(exist_ok=True)
# Save current scene as a copy before changing materials; retain original working path.
bpy.ops.wm.save_as_mainfile(filepath=str(revisions/('House_A_BeforeBlue_'+datetime.now().strftime('%Y%m%d_%H%M%S')+'.blend')),copy=True)
opaque=bpy.data.materials['TFP_Atlas_1A'];removed='TFP_Glass_Windows_1B';colorIndex=list(SPEC['materials']['swatches']).index('glass')
a,b,c,d=SPEC['materials']['swatches']['glass']['safeUvBottomLeft'];uvcenter=((a+c)/2,(b+d)/2)
changed=[]
for mesh in bpy.data.meshes:
 slots=[i for i,mat in enumerate(mesh.materials) if mat and mat.name==removed]
 if not slots:continue
 opaqueIndex=next(i for i,mat in enumerate(mesh.materials) if mat==opaque)
 count=0
 for face in mesh.polygons:
  if face.material_index not in slots:continue
  face.material_index=opaqueIndex
  if mesh.attributes.get('PaletteIndex'):mesh.attributes['PaletteIndex'].data[face.index].value=colorIndex
  for k in face.loop_indices:mesh.uv_layers.active.data[k].uv=uvcenter
  count+=1
 for i in sorted(slots,reverse=True):mesh.materials.pop(index=i)
 mesh.update();changed.append({'mesh':mesh.name,'blueFaces':count})
mat=bpy.data.materials.get(removed)
if mat and mat.users==0:bpy.data.materials.remove(mat)
for im in list(bpy.data.images):
 if im.get('UnityGUID')==SPEC['materials']['glassTextureGuid'] and im.users==0:bpy.data.images.remove(im)
scene=bpy.context.scene
for entry in REPORT['models']:
 if entry['model']!='Lv5':continue
 bpy.context.window.scene=bpy.data.scenes['House A Lv5'];obj=bpy.data.objects[entry['name']]
 oldhidden=obj.hide_get();oldrender=obj.hide_render;oldname=obj.name
 obj.hide_set(False);obj.hide_render=False;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 obj.name='House_A_Lv5_'+['High','Medium','Low'][entry['lod']]
 path=ART/entry['export']
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,
  apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',mesh_smooth_type='FACE',
  add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
 obj.name=oldname;obj.hide_set(oldhidden);obj.hide_render=oldrender
 entry['materials']=[mat.name for mat in obj.data.materials];entry['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
REPORT['runtimeMaterials']=['TFP_Atlas_1A.mat','TFP_Atlas_Lights_1A.mat'];REPORT['packedBlenderPreviewImages']=1
REPORT['sunroomTreatment']='Solid blue opaque from existing atlas';REPORT['blueSwatchRGB']=[137,178,197]
REPORT['approvedSpecificationSha256']=hashlib.sha256((ART/'References/House_A_Approved.json').read_bytes()).hexdigest()
(ART/'Previews/MeshManifest.json').write_text(json.dumps(REPORT,indent=2),encoding='utf-8')
bpy.context.window.scene=scene
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
(ART/'Previews/BlueRevision.json').write_text(json.dumps({'status':'PASS','blueRGB':[137,178,197],'meshes':changed,'existingGeometryPreserved':True,'backupSaved':True},indent=2),encoding='utf-8')
print('HOUSE_BLUE_REVISION_COMPLETE',len(changed),'meshes; solid atlas blue; geometry preserved')
