"""Apply the final living-tip pass to an already built R02 master."""
import bpy,sys,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C
import g09_r02_geometry as G
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);p=MASTER.parent/'G09_Manifest.json';data=json.loads(p.read_text())
for kind in PHASES:
 for stage in ['BudForming','Ready']:
  root=bpy.data.objects[kind+'_'+stage];C.parent=root;C.current=root.users_collection[0]
  for ob in list(root.children_recursive):
   if ob.get('TerminalLeaf'):
    stem=bpy.data.objects.get(ob.get('PetioleName',''))
    if stem:bpy.data.objects.remove(stem,do_unlink=True)
    bpy.data.objects.remove(ob,do_unlink=True)
  G.cap_leaf_canes(kind)
  for ob in root.children_recursive:
   if ob.type=='MESH' and ob.matrix_basis!=Matrix.Identity(4):ob.data.transform(ob.matrix_basis);ob.matrix_basis=Matrix.Identity(4)
  data['counts'][kind][stage]['leaves']=sum(o.get('ReferenceLeaf')==kind for o in root.children_recursive)
p.write_text(json.dumps(data,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G09_R02_LIVING_TIPS_PASS',flush=True)
