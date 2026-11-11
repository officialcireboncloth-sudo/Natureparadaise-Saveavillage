"""Rebuild the Jasmine cut sprig after its flower-clearance review."""
import bpy,sys,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C
import g09_geometry as G
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);root=bpy.data.objects['Jasmine_Harvest'];C.parent=root;C.current=root.users_collection[0]
for o in list(root.children_recursive):
 if o.type=='MESH':
  mesh=o.data;bpy.data.objects.remove(o,do_unlink=True)
  if not mesh.users:bpy.data.meshes.remove(mesh)
G.build('Jasmine','Harvest');objects=[o for o in root.children_recursive if o.type=='MESH'];offset=.001-min(v.co.z for o in objects for v in o.data.vertices)
for o in objects:
 for v in o.data.vertices:v.co.z+=offset
result=C.export(dict(plant='Jasmine',stage='Harvest',root=root,objects=objects),'NP_Jasmine_Harvest')
p=MASTER.parent/'G09_Manifest.json';data=json.loads(p.read_text());data['exports']=[result if r['name']==result['name'] else r for r in data['exports']];data['counts']['Jasmine']['Harvest']['leaves']=sum(o.get('ReferenceLeaf')=='Jasmine' for o in objects);p.write_text(json.dumps(data,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G09_HARVEST_CLEARANCE_PASS',data['counts']['Jasmine']['Harvest'],flush=True)
