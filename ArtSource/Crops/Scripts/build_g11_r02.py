"""Rebuild four Tulip phases, preserving the delivered bulb/sprout meshes and all GUIDs."""
import bpy,sys,json,ast
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
import plant_blender_common as C
import g11_r02_geometry as G
bpy.ops.wm.open_mainfile(filepath=str(MASTER));scene=bpy.context.scene;mat=bpy.data.materials[ATLAS]
for node in mat.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image:
  node.image.filepath=str(TEXTURES/node.image.name);node.image.reload();node.image.pack()
configure(C,mat)
source=SOURCE/'Scripts/build_g11_blender.py';tree=ast.parse(source.read_text());exportCode=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='export');exec(compile(ast.Module(body=[exportCode],type_ignores=[]),str(source),'exec'),globals())
manifestPath=MASTER.parent/'G11_Manifest.json';data=json.loads(manifestPath.read_text());updates={}
stages=sys.argv[sys.argv.index('--stages')+1].split(',') if '--stages' in sys.argv else [sys.argv[sys.argv.index('--stage')+1]] if '--stage' in sys.argv else ['Young','BudForming','Ready','Harvest']
for stage in stages:
 root=bpy.data.objects['Tulip_'+stage];coll=root.users_collection[0]
 for ob in list(root.children_recursive):bpy.data.objects.remove(ob,do_unlink=True)
 C.parent=root;C.current=coll;G.build('Tulip',stage);objects=[ob for ob in root.children_recursive if ob.type=='MESH']
 for ob in objects:ob.data.transform(ob.matrix_basis);ob.matrix_basis=Matrix.Identity(4)
 offset=.001-min(v.co.z for ob in objects for v in ob.data.vertices)
 for ob in objects:
  for v in ob.data.vertices:v.co.z+=offset
  if ob.get('JointPoint'):ob['JointPoint']=[ob['JointPoint'][0],ob['JointPoint'][1],ob['JointPoint'][2]+offset]
 root['GroundOffset']=offset;root['Revision']='R02 rounded oval tepals and curved leaves'
 updates['NP_Tulip_'+stage]=export(root,'NP_Tulip_'+stage)
 data['counts']['Tulip'][stage]=dict(leaves=sum(ob.get('ReferenceLeaf')=='Tulip' for ob in objects),flowers=sum(ob.get('OpenFlower')=='Tulip' for ob in objects),buds=sum(ob.get('ClosedBud')=='Tulip' for ob in objects),petals=sum(ob.get('PetalKind')=='Tulip' for ob in objects))
 print('G11_R02_BUILT',stage,flush=True)
data['exports']=[updates.get(r['name'],r) for r in data['exports']];data['revision']='R02 rounded elliptical cup petals; individually curved/asymmetric leaves; softer registered petal and leaf surfaces';manifestPath.write_text(json.dumps(data,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G11_R02_MASTER_PASS: four updated phases, retained bulb/sprout exports',flush=True)
