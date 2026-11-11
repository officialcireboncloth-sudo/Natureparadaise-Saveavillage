"""Change Rubus leaf blades only; keep the accepted shrub, fruit and flower arrangement."""
import bpy,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import g01_raspberry_config as R
import g01_leaf_r05_geometry as L
import plant_blender_common as C
import unified_plant_atlas as U
from export_g01_leaf_r05 import export
W=G.REVIEW/'Leaf_R05';draft='--draft' in sys.argv;bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));exports=[];stats={}
# Refresh only the Blueberry authoring page image nodes; its geometry remains exact.
mat=bpy.data.materials[G.ATLAS]
for node in mat.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image:
  suffix=next((s for s in ['BaseColor','Normal'] if s in node.image.name),None)
  if suffix:
   im=bpy.data.images.load(str(G.TEXTURES/(G.ATLAS+'_'+suffix+'.png')),check_existing=False)
   if suffix=='Normal':im.colorspace_settings.name='Non-Color'
   im.pack();node.image=im
for root in [o for o in bpy.data.objects if o.get('Phase') and o['Plant'] in ['Raspberry','Blackberry'] and o['Phase'] not in ['Seed','Harvest']]:
 kind,stage=root['Plant'],root['Phase']
 if draft and stage!='Ready':continue
 config=R if kind=='Raspberry' else G;mat=bpy.data.materials[config.ATLAS];config.configure(C,mat);C.parent=root;oldLeaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')==kind];C.current=oldLeaves[0].users_collection[0]
 for old in oldLeaves:
  assert old.get('LeafRevision')!='R05','R05 already applied; rebuild from its saved before-master'
  origin=Vector(old['LeafOrigin'])
  if not old.get('LeafOriginIncludesGroundOffset'):origin.z+=float(root['GroundOffset'])
  ob=L.create(old,origin);name=old.name;bpy.data.objects.remove(old,do_unlink=True);ob.name=name
 root['LeafShapeRevision']='R05';stats[root.name]=dict(leaves=len(oldLeaves))
 if not draft:exports.append(export(root,'NP_'+kind+'_'+stage,mat))
 print('LEAF_R05_BUILT',kind,stage,len(oldLeaves),flush=True)
if draft:bpy.ops.wm.save_as_mainfile(filepath=str(W/'Draft.blend'))
else:
 bpy.ops.wm.save_as_mainfile(filepath=str(G.MASTER));data=U.load(G.MASTER.parent/'G01_Manifest.json');current={r['name']:r for r in data['exports']};current.update({r['name']:r for r in exports});data.update(revision='R05 reference-traced Rubus blades and restored detailed Blueberry leaf surface',exports=list(current.values()));U.write(G.MASTER.parent/'G01_Manifest.json',data);U.write(W/'Build.json',dict(status='SOURCE_EXPORTED',roots=stats,exports=exports,masterSha256=U.sha(G.MASTER)))
print('LEAF_R05_BUILD_PASS',len(exports),'phase exports',flush=True)
