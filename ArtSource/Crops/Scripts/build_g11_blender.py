"""Build and export G11, preserving editable high-detail components in the master."""
import bpy,sys,json
from pathlib import Path
from mathutils import Matrix,Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
import plant_blender_common as C
import g11_geometry as G
only=sys.argv[sys.argv.index('--only')+1] if '--only' in sys.argv else None
if only:bpy.ops.wm.open_mainfile(filepath=str(MASTER))
else:bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
mat=bpy.data.materials.get(ATLAS) if only else bpy.data.materials.new(ATLAS);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs.inputs['Specular IOR Level'].default_value=.28
for suffix in ([] if only else ['BaseColor','Normal','SurfaceMask']):
 im=bpy.data.images.load(str(TEXTURES/(ATLAS+'_'+suffix+'.png')));im.pack();tex=nodes.new('ShaderNodeTexImage');tex.image=im;tex.label=suffix
 if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 elif suffix=='Normal':
  im.colorspace_settings.name='Non-Color';n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.22;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],bs.inputs['Normal'])
 else:
  im.colorspace_settings.name='Non-Color';im.alpha_mode='CHANNEL_PACKED';n=nodes.new('ShaderNodeMath');n.operation='SUBTRACT';n.inputs[0].default_value=1;links.new(tex.outputs['Alpha'],n.inputs[1]);links.new(n.outputs[0],bs.inputs['Roughness'])
configure(C,mat)

def export(root,name):
 bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get();inverse=root.matrix_world.inverted();copies=[]
 for src in root.children_recursive:
  if src.type!='MESH':continue
  ev=src.evaluated_get(graph);me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=graph);me.transform(inverse@src.matrix_world)
  ob=bpy.data.objects.new('G11 export part',me);scene.collection.objects.link(ob);copies.append(ob)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in copies:ob.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();ob=copies[0];ob.name=name;ob.data.materials.clear();ob.data.materials.append(mat)
 for face in ob.data.polygons:face.material_index=0
 tri=ob.modifiers.new('Runtime triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name);C.clean_export_triangles(ob)
 if len(ob.data.polygons)>26000:
  dec=ob.modifiers.new('Near view budget','DECIMATE');dec.ratio=26000/len(ob.data.polygons);dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name);C.clean_export_triangles(ob)
 def clamp_uv():
  for loop in ob.data.uv_layers.active.data:
   u,v=loop.uv;s=C.slots_by_index[max(0,min(7,int((1-v)*8)))*8+max(0,min(7,int(u*8)))];a,b,c,d=s['uvSafe'];loop.uv=(max(a,min(c,u)),max(b,min(d,v)))
 clamp_uv();spec=C.write_mesh_spec(ob,name)
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 dec=ob.modifiers.new('Distance mesh','DECIMATE');dec.ratio=min(.40,2800/max(spec['triangles'],1));dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name);C.clean_export_triangles(ob);clamp_uv();lod=C.write_mesh_spec(ob,name+'_LOD1')
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'_LOD1.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 bpy.data.objects.remove(ob,do_unlink=True)
 return dict(name=name,plant=root['Plant'],stage=root['Phase'],triangles=spec['triangles'],lodTriangles=lod['triangles'],bounds=[spec['min'],spec['max']])

if only:
 old=json.loads((MASTER.parent/'G11_Manifest.json').read_text());plan=json.loads((MASTER.parent/'G11_VisualPlan.json').read_text())['plants'];counts=old['counts'];exports=[r for r in old['exports'] if r['plant']!=only]
 for root in [ob for ob in bpy.data.objects if ob.get('Plant')==only]:
  for ob in list(root.children_recursive):bpy.data.objects.remove(ob,do_unlink=True)
  bpy.data.objects.remove(root,do_unlink=True)
 for coll in [c for c in bpy.data.collections if c.name.startswith(only+' / ')]:bpy.data.collections.remove(coll)
else:plan={};counts={};exports=[]
for row,(kind,stages) in enumerate(PHASES.items()):
 if only and kind!=only:continue
 plan[kind]=[];counts[kind]={}
 for col,stage in enumerate(stages):
  coll=bpy.data.collections.new(kind+' / '+stage);scene.collection.children.link(coll);C.current=coll
  root=bpy.data.objects.new(kind+'_'+stage,None);coll.objects.link(root);root.location=((col-2.5)*1.65,0,0);root['Plant']=kind;root['Phase']=stage;C.parent=root
  G.build(kind,stage);objects=[o for o in root.children_recursive if o.type=='MESH'];name='NP_'+kind+'_'+stage
  for ob in objects:ob.data.transform(ob.matrix_basis);ob.matrix_basis=Matrix.Identity(4)
  offset=.001-min(v.co.z for ob in objects for v in ob.data.vertices)
  for ob in objects:
   for v in ob.data.vertices:v.co.z+=offset
   if ob.get('JointPoint'):ob['JointPoint']=[ob['JointPoint'][0],ob['JointPoint'][1],ob['JointPoint'][2]+offset]
  root['GroundOffset']=offset
  counts[kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),flowers=sum(o.get('OpenFlower')==kind for o in objects),buds=sum(o.get('ClosedBud')==kind for o in objects),petals=sum(o.get('PetalKind')==kind for o in objects))
  if '--draft' not in sys.argv:exports.append(export(root,name))
  plan[kind].append(dict(stage=stage,model=name,source_group='G11',scale=[1,1,1]));print('G11_BUILT',kind,stage,counts[kind][stage],flush=True)
if only:studio=bpy.data.collections['STUDIO - excluded from exports']
else:
 with bpy.data.libraries.load(str(SOURCE/'G09/Blender/G09_FloweringShrubs_Master.blend'),link=False) as (src,dst):dst.collections=['STUDIO - excluded from exports']
 studio=dst.collections[0];scene.collection.children.link(studio)
for ob in list(studio.objects):
 if ob.type=='FONT':bpy.data.objects.remove(ob,do_unlink=True)
scene.camera=next(ob for ob in studio.objects if ob.type=='CAMERA')
scene.world=bpy.data.worlds.new('G11 neutral world');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.8,.8,.8,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.20
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0.;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=(0,0,.5);area.spaces.active.region_3d.view_distance=12;area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL'
(MASTER.parent/'G11_Manifest.json').write_text(json.dumps(dict(group='G11',revision='R03 spatial radial leaves and rounded elliptical Tulip',plants=list(PHASES),atlas=ATLAS,material=ATLAS,phaseAppearances=6,uniqueNewExports=6,counts=counts,exports=exports,nearTriangleBudget=26000,distanceTriangleBudget=2800,integration='Visual assets; existing gameplay retained'),indent=2))
(MASTER.parent/'G11_VisualPlan.json').write_text(json.dumps(dict(group='G11',plants=plan),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G11_MASTER_PASS',len(exports),'exports',flush=True)
