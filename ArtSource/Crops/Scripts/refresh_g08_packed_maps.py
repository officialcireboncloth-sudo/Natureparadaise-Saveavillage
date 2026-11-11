"""Refresh packed production maps after source texture authoring, without remeshing."""
import bpy,sys,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g08_config import *
bpy.ops.wm.open_mainfile(filepath=str(MASTER));mat=bpy.data.materials[ATLAS];count=0
for node in mat.node_tree.nodes:
 if node.type!='TEX_IMAGE' or not node.image:continue
 old=node.image;suffix=node.label;path=TEXTURES/(ATLAS+'_'+suffix+'.png');name=path.name
 im=bpy.data.images.load(str(path),check_existing=False)
 if suffix!='BaseColor':im.colorspace_settings.name='Non-Color'
 if suffix=='SurfaceMask':im.alpha_mode='CHANNEL_PACKED'
 im.pack();node.image=im
 if not old.users:bpy.data.images.remove(old)
 im.name=name
 assert hashlib.sha256(im.packed_file.data).hexdigest()==hashlib.sha256(path.read_bytes()).hexdigest()
 count+=1
assert count==3
bpy.ops.wm.save_as_mainfile(filepath=str(MASTER));print('G08_CURRENT_MAPS_PACKED',count,flush=True)
