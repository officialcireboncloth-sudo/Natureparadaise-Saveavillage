"""Check the measured blade volumes before any published asset is replaced."""
import bpy,bmesh,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
bpy.ops.wm.open_mainfile(filepath=str(G.REVIEW/'Leaf_R05/Draft.blend'));checked=0
for ob in bpy.data.objects:
 if ob.type=='MESH' and ob.get('LeafRevision')=='R05':
  bm=bmesh.new();bm.from_mesh(ob.data);bad=sum(len(e.link_faces)!=2 for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free();assert not bad and volume>1e-12,(ob.name,bad,volume);checked+=1
print('LEAF_R05_DRAFT_VOLUME_PASS',checked,flush=True)
