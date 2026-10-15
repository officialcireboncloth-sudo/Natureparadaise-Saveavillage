"""Read-only source-FBX inspection; writes previews and measurements, no game/model edits."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent/'ReusePreviews';OUT.mkdir(exist_ok=True)
MODELS=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=6;sc.cycles.use_denoising=True
sc.render.resolution_x=560;sc.render.resolution_y=420;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
sc.world=bpy.data.worlds.new('PreviewWorld');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.69,.65,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.7
sc.view_settings.view_transform='Standard'
atlas=bpy.data.images.load(str(ROOT/'ArtSource/BarnInterior_A/References/TFP_Atlas_1A_BlenderPreview.png')) if (ROOT/'ArtSource/BarnInterior_A/References/TFP_Atlas_1A_BlenderPreview.png').exists() else bpy.data.images.load(str(ROOT/'ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png'))
mat=bpy.data.materials.new('OriginalAtlasPreview');mat.use_nodes=True;tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.8;mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
cam=bpy.data.objects.new('AuditCamera',bpy.data.cameras.new('AuditCamera'));sc.collection.objects.link(cam);sc.camera=cam;cam.data.type='ORTHO';cam.data.clip_start=.001;cam.data.clip_end=100
light=bpy.data.objects.new('AuditKey',bpy.data.lights.new('AuditKey','AREA'));sc.collection.objects.link(light);light.data.energy=100;light.data.size=3
records=[]
for name in ['TFP_Raw_Egg_01A','TFP_Egg_01A','TFP_Egg_02A','TFP_Jar_01A','TFP_Jar_01B','TFP_Honey_Jar_01A','TFP_Kitchen_Bowl_01A','TFP_Kitchen_Bowl_02A']:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_anim=False)
 imported=list(set(bpy.data.objects)-before);obs=[o for o in imported if o.type=='MESH'];bpy.context.view_layer.update()
 points=[o.matrix_world@v.co for o in obs for v in o.data.vertices];lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)]);centre=(lo+hi)/2;size=max(hi-lo)
 materials=sorted({m.name for o in obs for m in o.data.materials if m})
 for o in obs:
  o.data.materials.clear();o.data.materials.append(mat)
  for p in o.data.polygons:p.material_index=0
 cam.location=centre+Vector((1.4,-1.8,1.25))*size;cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size*1.8
 light.location=centre+Vector((-.8,-1.2,1.8))*size;light.rotation_euler=(centre-light.location).to_track_quat('-Z','Y').to_euler();light.data.energy=max(.2,size*size*100);light.data.size=size*2
 sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
 records.append({'name':name,'source':str((MODELS/(name+'.fbx')).relative_to(ROOT)),'meshObjects':len(obs),'triangles':sum(len(p.vertices)-2 for o in obs for p in o.data.polygons),'meshVertices':sum(len(o.data.vertices) for o in obs),'sizeMetres':list(hi-lo),'originalFBXMaterials':materials,'preview':name+'.png'})
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)
(OUT/'source-audit.json').write_text(json.dumps(records,indent=2));print('COOP_REUSE_SOURCE_AUDIT_COMPLETE')

