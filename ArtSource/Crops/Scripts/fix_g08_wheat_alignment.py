"""Rebuild only Wheat Heading/Harvest, keeping reviewed colours and other phases."""
import bpy,sys,json,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
import plant_blender_common as C
import g08_geometry as G
targets={'Wheat_Heading','Wheat_Harvest'}
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS])
def fingerprint(root):
 h=hashlib.sha256()
 h.update(str(list(root.location)).encode())
 for o in sorted(root.children_recursive,key=lambda o:o.name):
  if o.type!='MESH':continue
  h.update(o.name.encode());h.update(str([list(v.co) for v in o.data.vertices]).encode());h.update(str([list(f.vertices) for f in o.data.polygons]).encode());h.update(str([list(u.uv) for u in o.data.uv_layers.active.data]).encode())
 return h.hexdigest()
retained=[o for o in bpy.data.objects if (o.get('Phase') or o.name in EXTRAS.values()) and o.name not in targets];prior={o.name:fingerprint(o) for o in retained};exports={}
for stage in ['Heading','Harvest']:
 root=bpy.data.objects['Wheat_'+stage];C.parent=root;C.current=root.users_collection[0]
 for o in list(root.children_recursive):
  if o.type=='MESH':
   mesh=o.data;bpy.data.objects.remove(o,do_unlink=True)
   if not mesh.users:bpy.data.meshes.remove(mesh)
 G.build('Wheat',stage);root['Revision']='R02 culm-aligned tapered wheat ears'
 objects=[o for o in root.children_recursive if o.type=='MESH'];name='NP_Wheat_'+stage
 exports[name]=C.export(dict(plant='Wheat',stage=stage,root=root,objects=objects),name)
for root in retained:assert fingerprint(root)==prior[root.name],root.name
p=MASTER.parent/'G08_Manifest.json';data=json.loads(p.read_text());data['revision']='R02 wheat heading and harvest attachment correction';data['exports']=[exports.get(r['name'],r) for r in data['exports']];data['revisionScope']=sorted(targets);p.write_text(json.dumps(data,indent=2))
(REVIEW/'R02_EditablePreservation.json').write_text(json.dumps(dict(status='PASS',retainedRoots=prior,changedRoots=sorted(targets)),indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G08_WHEAT_ALIGNMENT_FIX_PASS',list(exports),len(prior),'other editable roots retained',flush=True)
