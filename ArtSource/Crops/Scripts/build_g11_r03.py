"""Replace only leaf source geometry; preserve accepted cup, bud, bulb and stem meshes."""
import bpy,sys,json,ast,hashlib
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_config import *
import plant_blender_common as C
import g11_r03_geometry as G
baseline=json.loads((REVIEW/'R03_Baseline.json').read_text())
original=ROOT/baseline['snapshot']/'ArtSource/Crops/G11/Blender/G11_Tulip_Master.blend'
bpy.ops.wm.open_mainfile(filepath=str(original));scene=bpy.context.scene;mat=bpy.data.materials[ATLAS];configure(C,mat)
src=SOURCE/'Scripts/build_g11_blender.py';tree=ast.parse(src.read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='export');exec(compile(ast.Module(body=[fn],type_ignores=[]),str(src),'exec'),globals())
manifestPath=MASTER.parent/'G11_Manifest.json';data=json.loads(manifestPath.read_text());updates={}
for stage in ['Sprout','Young','BudForming','Ready','Harvest']:
 root=bpy.data.objects['Tulip_'+stage];C.parent=root;C.current=root.users_collection[0];oldOffset=float(root.get('GroundOffset',0));leaves=[ob for ob in root.children_recursive if ob.get('ReferenceLeaf')=='Tulip']
 definitions=[(list(map(list,ob['CurveControlPoints'])),float(ob['LeafRollRadians'][0]),float(ob['LeafRollRadians'][1]),int(ob['LeafIndex'])) for ob in leaves] if stage!='Sprout' else []
 for ob in leaves:bpy.data.objects.remove(ob,do_unlink=True)
 before=set(root.children_recursive)
 if stage=='Sprout':G.shoots()
 else:
  for pts,r0,r1,index in definitions:
   width=(.155 if index in [0,2] else .156) if stage=='Young' else .133 if stage=='Harvest' else .171 if index==0 else .186
   G.leaf(pts,width,index,r0,r1,stage=='Harvest')
 for ob in set(root.children_recursive)-before:
  ob.data.transform(ob.matrix_basis);ob.matrix_basis=Matrix.Identity(4)
  for v in ob.data.vertices:v.co.z+=oldOffset
 objects=[o for o in root.children_recursive if o.type=='MESH'];delta=.001-min(v.co.z for ob in objects for v in ob.data.vertices)
 for ob in objects:
  for v in ob.data.vertices:v.co.z+=delta
  if ob.get('JointPoint'):ob['JointPoint']=[ob['JointPoint'][0],ob['JointPoint'][1],ob['JointPoint'][2]+delta]
 root['GroundOffset']=oldOffset+delta;root['Revision']='R03 radial leaf frames and spatial growth'
 updates['NP_Tulip_'+stage]=export(root,'NP_Tulip_'+stage);data['counts']['Tulip'][stage]['leaves']=sum(o.get('ReferenceLeaf')=='Tulip' for o in objects);print('G11_R03_BUILT',stage,flush=True)
data['exports']=[updates.get(r['name'],r) for r in data['exports']];data['revision']='R03: leaf frames from radial growth/world up, depth distribution, rolled cross-sections';manifestPath.write_text(json.dumps(data,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G11_R03_MASTER_PASS',flush=True)
