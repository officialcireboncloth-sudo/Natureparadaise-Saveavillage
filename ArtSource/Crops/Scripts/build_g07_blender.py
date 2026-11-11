"""Editable G07 growth/harvest roots, the shared studio and LOD exports."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g07_config import *
import plant_blender_common as C
import g07_r02_geometry as G
from g07_r02_layout import configure
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new(ATLAS);mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
for suffix in ['BaseColor','Normal','SurfaceMask']:
 im=bpy.data.images.load(str(TEXTURES/(ATLAS+'_'+suffix+'.png')));im.pack();tex=nodes.new('ShaderNodeTexImage');tex.image=im;tex.label=suffix
 if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 elif suffix=='Normal':
  im.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.65;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
 else:
  im.colorspace_settings.name='Non-Color';im.alpha_mode='CHANNEL_PACKED';n=nodes.new('ShaderNodeMath');n.operation='SUBTRACT';n.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],n.inputs[1]);links.new(n.outputs[0],bs.inputs['Roughness'])
configure(C,mat);exports=[];plan={};counts={}
for row,(kind,stages) in enumerate(PHASES.items()):
 plan[kind]=[];counts[kind]={}
 for col,stage in enumerate(stages):
  coll=bpy.data.collections.new(kind+' / '+stage);scene.collection.children.link(coll);C.current=coll
  root=bpy.data.objects.new(kind+'_'+stage,None);coll.objects.link(root);root.location=((col-2.5)*1.40,(.5-row)*1.9,0);root['Plant']=kind;root['Phase']=stage;root['Reference']='Current user attachment: '+kind
  C.parent=root;{'Spinach':G.spinach,'WaterSpinach':G.water_spinach}[kind](stage)
  objects=[o for o in root.children_recursive if o.type=='MESH'];name='NP_'+kind+'_'+stage
  counts[kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),nodes=sum(bool(o.get('StemNode')) for o in objects),hollowCutEnds=sum(bool(o.get('HollowCutEnd')) for o in objects))
  exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=objects),name));plan[kind].append(dict(stage=stage,model=name,source_group='G07',scale=[1,1,1]))
for i,(kind,name) in enumerate(EXTRAS.items()):
 coll=bpy.data.collections.new('EXTRAS / '+kind+' loose harvest');scene.collection.children.link(coll);C.current=coll
 root=bpy.data.objects.new(name,None);coll.objects.link(root);root.location=((i-.5)*1.5,-2.4,0);root['Plant']=kind;C.parent=root
 (G.spinach_bunch if kind=='Spinach' else G.water_bunch)(single=True)
 exports.append(C.export(dict(plant=kind,stage='Extra',root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),name))

# Reuse the accepted neutral G06 studio without changing its source master.
with bpy.data.libraries.load(str(SOURCE/'G06/Blender/G06_RootVegetables_Master.blend'),link=False) as (src,dst):dst.collections=['STUDIO - excluded from exports']
studio=dst.collections[0];scene.collection.children.link(studio)
for o in list(studio.objects):
 if o.type=='FONT':bpy.data.objects.remove(o,do_unlink=True)
scene.camera=next(o for o in studio.objects if o.type=='CAMERA');scene.camera.data.ortho_scale=9.5
ink=bpy.data.materials.get('G06 label ink')
for row,(kind,stages) in enumerate(PHASES.items()):
 for col,stage in enumerate(stages):
  cu=bpy.data.curves.new(kind+' '+stage,'FONT');cu.body=kind+' | '+stage;cu.size=.10;cu.align_x='CENTER';ob=bpy.data.objects.new(cu.name,cu);studio.objects.link(ob);ob.location=((col-2.5)*1.40,(.5-row)*1.9-.62,.006)
  if ink:cu.materials.append(ink)
scene.world=bpy.data.worlds.new('G07 neutral white world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.15
scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=-.05;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=(0,0,.30);area.spaces.active.region_3d.view_distance=10;area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL'
source=bpy.data.collections.new('SOURCE - vendor comparison, excluded from exports');scene.collection.children.link(source);audit=[]
for filename in ['TFP_Spinach_Plant_01A.fbx','TFP_Spinach_Plant_02A.fbx','TFP_Spinach_Pile_01A.fbx']:
 candidates=list((A/'Pack/Toon Series/Toon Farm Pack/Models').rglob(filename))
 if not candidates:continue
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(candidates[0]));objects=set(bpy.data.objects)-before
 audit.append(dict(file=str(candidates[0].relative_to(ROOT)),triangles=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in objects if o.type=='MESH'),decision='Retained as comparison; authored curved rosette leaves, growth stages and registered UVs for the supplied reference'))
 for o in objects:C.move(o,source)
source.hide_render=True;source.hide_viewport=True
(REVIEW/'SourceAudit.json').write_text(json.dumps(audit,indent=2))
(MASTER.parent/'G07_Manifest.json').write_text(json.dumps(dict(group='G07',revision='R02 registered high-detail leaves and organic clumps',plants=list(PHASES),material=ATLAS,atlas=ATLAS,newAtlasPages=1,phaseAppearances=12,uniqueNewExports=len(exports),counts=counts,exports=exports,extras=EXTRAS,integration='Prepared visual assets; no gameplay profile, balance, item or store changes'),indent=2))
(MASTER.parent/'G07_VisualPlan.json').write_text(json.dumps(dict(group='G07',plants=plan),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G07_MASTER_EXPORT_PASS',len(exports),'base exports',counts,flush=True)
