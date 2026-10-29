"""Export ALL saved player Actions from the unified native master.

Run in Blender: blender --background --python export_player.py.
Horse reference objects and editor-only object movement are excluded.
This exporter does not generate or change any pose in the saved master.
"""
import bpy,json,sys,runpy
from pathlib import Path
HERE=Path(__file__).resolve().parent if '__file__' in globals() and Path(__file__).is_file() else Path(bpy.data.filepath).resolve().parent
ROOT=next(p for p in [HERE,*HERE.parents] if (p/'DEVELOPMENT.md').exists())
MASTER=HERE/'Player_Farming.blend'
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
OUT=Path(args[0]) if args else ROOT/'Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx'
OUT.parent.mkdir(parents=True,exist_ok=True)
if bpy.data.filepath and Path(bpy.data.filepath).resolve()==MASTER.resolve() and bpy.data.is_dirty:
 raise RuntimeError('Simpan Player_Farming.blend terlebih dahulu sebelum ekspor agar perubahan animasi tidak hilang.')
bpy.ops.wm.open_mainfile(filepath=str(MASTER),load_ui=False)
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
rig=bpy.data.objects['Armature'];mesh=bpy.data.objects['PlayerModel'];scene=bpy.context.scene
# Split the approved source body's turn from local bone motion, so the capsule
# can finish facing the horse without a snap when ordinary locomotion resumes.
runtime=runpy.run_path(str(HERE/'prepare_horse_runtime.py'))
runtime['prepare_horse_runtime'](rig,scene,HERE/'HorseTrajectory.json')
# Source-only pose assets and manual blocking timelines are not runtime takes.
for action in list(bpy.data.actions):
 if action.get('horse_blocking_only',False) or action.get('source_reference_only',False):bpy.data.actions.remove(action)
for a in bpy.data.actions:
 for c in list(a.fcurves):
  if not c.data_path.startswith('pose.bones['):a.fcurves.remove(c)
rig.location=(0,0,0);rig.scale=(1,1,1)
rig.animation_data.action=bpy.data.actions['Player | Idle'];rig.animation_data.action_slot=rig.animation_data.action.slots[0];scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
# Keep the native model's hidden control-shape meshes and visibility exactly as
# before. Remove ONLY the horse reference collection in this export process.
meshes=[o for o in scene.objects if o.type=='MESH' and not o.get('reference_only',False)]
for o in list(scene.objects):
 if o.get('reference_only',False) or any(c.name.startswith('Horse references') for c in o.users_collection):bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.update()
bpy.context.view_layer.objects.active=rig
scene.unit_settings.scale_length=1
bpy.ops.export_scene.fbx(filepath=str(OUT),use_selection=False,use_visible=False,object_types={'ARMATURE','MESH','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',add_leaf_bones=True,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='AUTO',mesh_smooth_type='FACE')
print('COMPLETE_PLAYER_EXPORTED',OUT,len(bpy.data.actions),'meshes',len(meshes))
# Return the editor to the intact saved master, with reference horse and preview
# movement still present. Never let an ordinary Save overwrite it with export cleanup.
bpy.ops.wm.open_mainfile(filepath=str(MASTER),load_ui=False)
