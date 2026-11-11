"""Measure the transparent tulip icon before normalization; no pixel edits."""
import bpy,numpy as np,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g11_integration_config import *
im=bpy.data.images.load(str(ART/'Generated/G11_Illustrations.png'));w,h=im.size;p=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(p);mask=p.reshape(h,w,4)[::-1,:,3]>.015;ys,xs=np.nonzero(mask);assert len(xs)
cell=dict(key='tulip',pixelRectTopLeft=[0,0,w,h],contentRectInCell=[max(0,int(xs.min())-2),max(0,int(ys.min())-2),min(w,int(xs.max())+3),min(h,int(ys.max())+3)])
(ART/'Generated/G11_AtlasCells.json').write_text(json.dumps(dict(size=[w,h],cells=[cell]),indent=2));print('G11_ICON_BOUNDS_PASS',cell,flush=True)
