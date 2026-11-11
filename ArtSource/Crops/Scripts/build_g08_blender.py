"""Build editable cereal stages and export meshes through the established pipeline."""
import bpy,sys,json,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
import plant_blender_common as C
import g08_geometry as G
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new(ATLAS);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
for suffix in ['BaseColor','Normal','SurfaceMask']:
 im=bpy.data.images.load(str(TEXTURES/(ATLAS+'_'+suffix+'.png')));im.pack();tex=nodes.new('ShaderNodeTexImage');tex.image=im;tex.label=suffix
 if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 elif suffix=='Normal':
  im.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.50;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
 else:
  im.colorspace_settings.name='Non-Color';im.alpha_mode='CHANNEL_PACKED';n=nodes.new('ShaderNodeMath');n.operation='SUBTRACT';n.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],n.inputs[1]);links.new(n.outputs[0],bs.inputs['Roughness'])
configure(C,mat);exports=[];plan={};counts={}
for row,(kind,stages) in enumerate(PHASES.items()):
 plan[kind]=[];counts[kind]={}
 for col,stage in enumerate(stages):
  coll=bpy.data.collections.new(kind+' / '+stage);scene.collection.children.link(coll);C.current=coll
  root=bpy.data.objects.new(kind+'_'+stage,None);coll.objects.link(root);root.location=((col-2.5)*1.65,(.5-row)*2.1,0);root['Plant']=kind;root['Phase']=stage;root['Reference']='Two supplied cereal growth charts'
  C.parent=root;G.build(kind,stage);objects=[o for o in root.children_recursive if o.type=='MESH'];name='NP_'+kind+'_'+stage
  counts[kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),heads=sum(o.get('Panicle')==kind for o in objects),grains=sum(o.get('CerealGrain')==kind for o in objects),awns=sum(bool(o.get('WheatAwn')) for o in objects))
  exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=objects),name));plan[kind].append(dict(stage=stage,model=name,source_group='G08',scale=[1,1,1]))
for i,(key,name) in enumerate(EXTRAS.items()):
 coll=bpy.data.collections.new('EXTRAS / '+key);scene.collection.children.link(coll);C.current=coll
 root=bpy.data.objects.new(name,None);coll.objects.link(root);root.location=((i-1)*.45,-2.8,0);root['Plant']='Wheat' if key.startswith('Wheat') else 'Rice';root['Extra']=key;C.parent=root
 G.pile(root['Plant'],white=key=='RiceWhitePile')
 exports.append(C.export(dict(plant=root['Plant'],stage=key,root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),name))
with bpy.data.libraries.load(str(SOURCE/'G07/Blender/G07_LeafyVegetables_Master.blend'),link=False) as (src,dst):dst.collections=['STUDIO - excluded from exports']
studio=dst.collections[0];scene.collection.children.link(studio)
for o in list(studio.objects):
 if o.type=='FONT':bpy.data.objects.remove(o,do_unlink=True)
scene.camera=next(o for o in studio.objects if o.type=='CAMERA');scene.camera.data.ortho_scale=10.8
scene.world=bpy.data.worlds.new('G08 neutral white world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.15
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-.05;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=(0,0,.6);area.spaces.active.region_3d.view_distance=11;area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL'
# Keep the vendor wheat candidate for comparison, hidden and excluded from exports.
source=bpy.data.collections.new('SOURCE - vendor comparison, excluded from exports');scene.collection.children.link(source);audit=[]
for filename in ['TFP_Wheat_Strand_01.fbx','TFP_Wheat_Patch_01.fbx']:
 candidates=list((A/'Pack/Toon Series/Toon Farm Pack/Models').rglob(filename))
 if not candidates:continue
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(candidates[0]));objects=set(bpy.data.objects)-before
 audit.append(dict(file=str(candidates[0].relative_to(ROOT)),decision='Reference only; new sculpted kernels, ears, curved leaves and phase-specific clumps follow the supplied chart'))
 for o in objects:C.move(o,source)
source.hide_render=True;source.hide_viewport=True
(REVIEW/'SourceAudit.json').write_text(json.dumps(audit,indent=2))
(MASTER.parent/'G08_Manifest.json').write_text(json.dumps(dict(group='G08',revision='R01 detailed vivid cereals',plants=list(PHASES),material=ATLAS,atlas=ATLAS,newAtlasPages=1,phaseAppearances=12,uniqueNewExports=len(exports),counts=counts,exports=exports,extras=EXTRAS,integration='Prepared visual assets; no gameplay profile, balance, item or store changes'),indent=2))
(MASTER.parent/'G08_VisualPlan.json').write_text(json.dumps(dict(group='G08',plants=plan),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G08_MASTER_EXPORT_PASS',len(exports),'base exports',json.dumps(counts),flush=True)
