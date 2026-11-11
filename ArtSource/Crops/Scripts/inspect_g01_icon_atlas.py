"""Find the separate blueberry and blackberry artwork bounds on the transparent source sheet."""
import bpy,sys,json
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_integration_config import *
p=ART/'Generated/G01_Illustrations.png';im=bpy.data.images.load(str(p),check_existing=False);w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);rgba=v.reshape(h,w,4)[::-1];cells=[]
for i,(_,kind,key,label) in enumerate(NEW):
 x0=i*(w//2);x1=(i+1)*(w//2) if i==0 else w;alpha=rgba[:,x0:x1,3];ys,xs=np.nonzero(alpha>.015);assert len(xs)>100
 cells.append(dict(key=key,pixelRectTopLeft=[x0,0,x1-x0,h],contentRectInCell=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]))
(ART/'Generated/G01_AtlasCells.json').write_text(json.dumps(dict(source=rel(p),size=[w,h],cells=cells),indent=2));print('G01_ICON_BOUNDS_PASS',cells,flush=True)
