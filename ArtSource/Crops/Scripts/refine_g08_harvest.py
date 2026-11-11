"""Lay the harvest bundles diagonally with the splayed rice branches close to ground."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
import plant_blender_common as C
import g08_geometry as G
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);changed={}
for kind in PHASES:
 root=bpy.data.objects[kind+'_Harvest'];C.parent=root;C.current=root.users_collection[0]
 for o in list(root.children_recursive):
  if o.type=='MESH':
   mesh=o.data;bpy.data.objects.remove(o,do_unlink=True)
   if not mesh.users:bpy.data.meshes.remove(mesh)
 G.harvest(kind);name='NP_'+kind+'_Harvest';changed[name]=C.export(dict(plant=kind,stage='Harvest',root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),name)
p=MASTER.parent/'G08_Manifest.json';data=json.loads(p.read_text());data['exports']=[changed.get(r['name'],r) for r in data['exports']];p.write_text(json.dumps(data,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G08_HARVEST_REFINED',list(changed),flush=True)
