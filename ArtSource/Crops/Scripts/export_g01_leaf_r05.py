"""Preserve the measured leaf outlines in the near mesh; reduce other surfaces to budget."""
import bpy
import plant_blender_common as C
from g01_config import MODELS

def export(root,name,mat):
 scene=bpy.context.scene;bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get();inverse=root.matrix_world.inverted();groups={True:[],False:[]}
 for src in root.children_recursive:
  if src.type!='MESH':continue
  ev=src.evaluated_get(graph);me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=graph);me.transform(inverse@src.matrix_world);ob=bpy.data.objects.new('R05 export part',me);scene.collection.objects.link(ob);groups[bool(src.get('ReferenceLeaf'))].append(ob)
 def join(parts):
  bpy.ops.object.select_all(action='DESELECT')
  for ob in parts:ob.select_set(True)
  bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=parts[0];tri=ob.modifiers.new('Exact surface triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name);C.clean_export_triangles(ob);return ob
 leaf=join(groups[True]);other=join(groups[False]);leafTriangles=len(leaf.data.polygons);budget=26000-leafTriangles;assert budget>=2500,(name,leafTriangles,'traced leaves must fit the established budget')
 if len(other.data.polygons)>budget:
  dec=other.modifiers.new('Non-leaf near budget','DECIMATE');dec.ratio=(budget-8)/len(other.data.polygons);dec.use_collapse_triangulate=True;bpy.context.view_layer.objects.active=other;bpy.ops.object.modifier_apply(modifier=dec.name);C.clean_export_triangles(other)
 ob=join([leaf,other]);ob.name=name;ob.data.materials.clear();ob.data.materials.append(mat)
 for face in ob.data.polygons:face.material_index=0
 def clamp_uv():
  for loop in ob.data.uv_layers.active.data:
   u,v=loop.uv;s=min(C.slots.values(),key=lambda s:sum((max(s['uvSafe'][k]-z,0,z-s['uvSafe'][k+2]))**2 for k,z in enumerate([u,v])));a,b,c,d=s['uvSafe'];loop.uv=(max(a,min(c,u)),max(b,min(d,v)))
 clamp_uv();spec=C.write_mesh_spec(ob,name);assert spec['triangles']<=26000
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 dec=ob.modifiers.new('Distance mesh','DECIMATE');dec.ratio=min(.4,2792/max(spec['triangles'],1));dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name);C.clean_export_triangles(ob);clamp_uv();lod=C.write_mesh_spec(ob,name+'_LOD1');bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'_LOD1.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False);bpy.data.objects.remove(ob,do_unlink=True)
 return dict(name=name,plant=root['Plant'],stage=root['Phase'],triangles=spec['triangles'],lodTriangles=lod['triangles'],bounds=[spec['min'],spec['max']],exactLeafTriangles=leafTriangles)
