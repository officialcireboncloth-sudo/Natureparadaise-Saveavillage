"""Export R02 evaluated geometry in batches; preserve the six accepted early-stage meshes."""
import bpy,sys,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C

def export(root,name):
 bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get();inverse=root.matrix_world.inverted();copies=[]
 for src in root.children_recursive:
  if src.type!='MESH':continue
  evaluated=src.evaluated_get(graph);me=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=graph);me.transform(inverse@src.matrix_world)
  ob=bpy.data.objects.new('R02 export part',me);bpy.context.scene.collection.objects.link(ob);copies.append(ob)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in copies:ob.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();ob=copies[0];ob.name=name;ob.data.materials.clear();ob.data.materials.append(C.mat)
 for face in ob.data.polygons:face.material_index=0
 tri=ob.modifiers.new('Runtime triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name);C.clean_export_triangles(ob);spec=C.write_mesh_spec(ob,name)
 bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 dec=ob.modifiers.new('Distance mesh','DECIMATE');dec.ratio=min(.4,2800/max(spec['triangles'],1));dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name);C.clean_export_triangles(ob)
 for loop in ob.data.uv_layers.active.data:
  u,v=loop.uv;s=C.slots_by_index[max(0,min(7,int((1-v)*8)))*8+max(0,min(7,int(u*8)))];a,b,c,d=s['uvSafe'];loop.uv=(max(a,min(c,u)),max(b,min(d,v)))
 C.write_mesh_spec(ob,name+'_LOD1');bpy.ops.export_scene.fbx(filepath=str(MODELS/(name+'_LOD1.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 bpy.data.objects.remove(ob,do_unlink=True)
 return dict(name=name,plant=root['Plant'],stage=root['Phase'],triangles=spec['triangles'],bounds=[spec['min'],spec['max']])

if __name__=='__main__':
 bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);p=MASTER.parent/'G09_Manifest.json';data=json.loads(p.read_text());updates={}
 for root in [o for o in bpy.data.objects if o.get('Phase') in ['Young','BudForming','Ready','Harvest']]:
  name='NP_'+root['Plant']+'_'+root['Phase'];updates[name]=export(root,name);print('G09_R02_EXPORTED',name,flush=True)
 data['exports']=[updates.get(r['name'],r) for r in data['exports']];p.write_text(json.dumps(data,indent=2));print('G09_R02_EXPORT_PASS',len(updates),flush=True)
