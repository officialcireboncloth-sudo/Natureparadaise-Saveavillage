"""Create editable flower shrubs; --draft defers exports until visual review."""
import bpy,sys,json
from mathutils import Matrix
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C
import g09_geometry as G
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new(ATLAS);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs.inputs['Specular IOR Level'].default_value=.30
for suffix in ['BaseColor','Normal','SurfaceMask']:
 im=bpy.data.images.load(str(TEXTURES/(ATLAS+'_'+suffix+'.png')));im.pack();tex=nodes.new('ShaderNodeTexImage');tex.image=im;tex.label=suffix
 if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 elif suffix=='Normal':
  im.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.25;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
 else:
  im.colorspace_settings.name='Non-Color';im.alpha_mode='CHANNEL_PACKED';n=nodes.new('ShaderNodeMath');n.operation='SUBTRACT';n.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],n.inputs[1]);links.new(n.outputs[0],bs.inputs['Roughness'])
configure(C,mat);plan={};counts={};exports=[]
for row,(kind,stages) in enumerate(PHASES.items()):
 plan[kind]=[];counts[kind]={}
 for col,stage in enumerate(stages):
  coll=bpy.data.collections.new(kind+' / '+stage);scene.collection.children.link(coll);C.current=coll
  root=bpy.data.objects.new(kind+'_'+stage,None);coll.objects.link(root);root.location=((col-2.5)*1.70,(1-row)*1.9,0);root['Plant']=kind;root['Phase']=stage;root['Reference']='Current user reference: '+kind;C.parent=root
  G.build(kind,stage);objects=[o for o in root.children_recursive if o.type=='MESH'];name='NP_'+kind+'_'+stage
  for o in objects:o.data.transform(o.matrix_basis);o.matrix_basis=Matrix.Identity(4)
  offset=.001-min(v.co.z for o in objects for v in o.data.vertices)
  for o in objects:
   for v in o.data.vertices:v.co.z+=offset
  root['GroundOffset']=offset
  counts[kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),flowers=sum(o.get('OpenFlower')==kind for o in objects),buds=sum(o.get('ClosedBud')==kind for o in objects),petals=sum(o.get('PetalKind')==kind for o in objects),thorns=sum(bool(o.get('RoseThorn')) for o in objects))
  if '--draft' not in sys.argv:exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=objects),name))
  plan[kind].append(dict(stage=stage,model=name,source_group='G09',scale=[1,1,1]));print('G09_BUILT',kind,stage,counts[kind][stage],flush=True)
with bpy.data.libraries.load(str(SOURCE/'G08/Blender/G08_Cereals_Master.blend'),link=False) as (src,dst):dst.collections=['STUDIO - excluded from exports']
studio=dst.collections[0];scene.collection.children.link(studio)
for o in list(studio.objects):
 if o.type=='FONT':bpy.data.objects.remove(o,do_unlink=True)
scene.camera=next(o for o in studio.objects if o.type=='CAMERA');scene.camera.data.ortho_scale=11
for o in studio.objects:
 if o.type=='LIGHT':o.data.energy*=1.40
scene.world=bpy.data.worlds.new('G09 neutral white world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.18
scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=.04;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=(0,0,.5);area.spaces.active.region_3d.view_distance=11;area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL'
(REVIEW/'SourceAudit.json').write_text(json.dumps(dict(search='Toon Farm model filenames Rose/Hibiscus/Jasmine',matchedModels=[],decision='New species-specific geometry from the three supplied growth references; existing crop atlases retained'),indent=2))
(MASTER.parent/'G09_Manifest.json').write_text(json.dumps(dict(group='G09',revision='R01 detailed colourful flowering shrubs',plants=list(PHASES),atlas=ATLAS,material=ATLAS,newAtlasPages=1,phaseAppearances=18,uniqueNewExports=18,counts=counts,exports=exports,integration='Prepared visual assets; crop profiles, items, stores, balance and scenes retained'),indent=2))
(MASTER.parent/'G09_VisualPlan.json').write_text(json.dumps(dict(group='G09',plants=plan),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G09_MASTER_PASS',len(exports),'exports; draft',('--draft' in sys.argv),flush=True)
