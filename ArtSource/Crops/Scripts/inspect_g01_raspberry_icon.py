"""Find actual transparent Raspberry illustration bounds for consistent 256px margins."""
import bpy,sys,json
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_raspberry_integration_config import *
p=ART/'Generated/G01_Raspberry_Illustration.png';im=bpy.data.images.load(str(p),check_existing=False);w,h=im.size;v=np.empty(w*h*4,np.float32);im.pixels.foreach_get(v);ys,xs=np.nonzero(v.reshape(h,w,4)[::-1,:,3]>.015)
(ART/'Generated/G01_Raspberry_AtlasCells.json').write_text(json.dumps(dict(source=rel(p),size=[w,h],cells=[dict(key='raspberry',pixelRectTopLeft=[0,0,w,h],contentRectInCell=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)])]),indent=2));print('RASPBERRY_ICON_BOUNDS_PASS',flush=True)
