"""Re-export current editable geometry after a common export cleanup fix."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
import plant_blender_common as C
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);exports=[]
for root in [o for o in bpy.data.objects if o.get('Phase') or o.name in EXTRAS.values()]:
 kind=root['Plant'];stage=root.get('Phase',root.get('Extra'));name='NP_'+kind+'_'+stage if root.get('Phase') else root.name
 exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),name));print('G08_REEXPORTED',name,flush=True)
p=MASTER.parent/'G08_Manifest.json';manifest=json.loads(p.read_text());manifest['exports']=exports;p.write_text(json.dumps(manifest,indent=2))
print('G08_REEXPORT_PASS',len(exports),flush=True)
