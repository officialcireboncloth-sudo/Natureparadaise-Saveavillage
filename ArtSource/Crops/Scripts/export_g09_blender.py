"""Export visually reviewed G09 source geometry through the common pipeline."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g09_config import *
import plant_blender_common as C
bpy.ops.wm.open_mainfile(filepath=str(MASTER));configure(C,bpy.data.materials[ATLAS]);exports=[]
for root in [o for o in bpy.data.objects if o.get('Phase')]:
 kind,stage=root['Plant'],root['Phase'];name='NP_'+kind+'_'+stage
 exports.append(C.export(dict(plant=kind,stage=stage,root=root,objects=[o for o in root.children_recursive if o.type=='MESH']),name));print('G09_EXPORTED',name,flush=True)
p=MASTER.parent/'G09_Manifest.json';data=json.loads(p.read_text());data['exports']=exports;p.write_text(json.dumps(data,indent=2));print('G09_EXPORT_PASS',len(exports),flush=True)
