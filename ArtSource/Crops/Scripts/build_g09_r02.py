"""R02 model revision; --draft rebuilds only mature shrubs into a separate preview file."""
import bpy,sys,json
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C
import g09_r02_geometry as G
baseline=json.loads((REVIEW/'R02_Baseline.json').read_text());source=ROOT/baseline['snapshot']/MASTER.relative_to(ROOT)
bpy.ops.wm.open_mainfile(filepath=str(source));configure(C,bpy.data.materials[ATLAS]);draft='--draft' in sys.argv;data=json.loads((ROOT/baseline['snapshot']/(MASTER.parent/'G09_Manifest.json').relative_to(ROOT)).read_text())
for kind in PHASES:
 for stage in (['Ready'] if draft else ['Young','BudForming','Ready','Harvest']):
  root=bpy.data.objects[kind+'_'+stage];C.parent=root;C.current=root.users_collection[0]
  for o in list(root.children_recursive):
   if o.type=='MESH':
    mesh=o.data;bpy.data.objects.remove(o,do_unlink=True)
    if not mesh.users:bpy.data.meshes.remove(mesh)
  G.build(kind,stage);objects=[o for o in root.children_recursive if o.type=='MESH']
  for o in objects:o.data.transform(o.matrix_basis);o.matrix_basis=Matrix.Identity(4)
  offset=.001-min(v.co.z for o in objects for v in o.data.vertices)
  for o in objects:
   for v in o.data.vertices:v.co.z+=offset
  root['GroundOffset']=offset;root['Revision']='R02 closed fleshy petals and branching shrubs'
  data['counts'][kind][stage]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in objects),flowers=sum(o.get('OpenFlower')==kind for o in objects),buds=sum(o.get('ClosedBud')==kind for o in objects),petals=sum(o.get('PetalKind')==kind for o in objects),thorns=sum(bool(o.get('RoseThorn')) for o in objects),closedPetals=sum(bool(o.get('ClosedPetal')) for o in objects))
  print('G09_R02_BUILT',kind,stage,data['counts'][kind][stage],flush=True)
data.update(revision='R02 closed fleshy petals and branching shrubs',revisionScope=['Young','BudForming','Ready','Harvest'])
dest=REVIEW/'G09_R02_Draft.blend' if draft else MASTER
if not draft:(MASTER.parent/'G09_Manifest.json').write_text(json.dumps(data,indent=2))
else:(REVIEW/'R02_DraftManifest.json').write_text(json.dumps(data,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(dest));print('G09_R02_MASTER_PASS','draft' if draft else 'active',flush=True)
