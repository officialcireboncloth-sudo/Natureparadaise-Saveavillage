"""Measure the transparent lavender icon before normalization; no pixel edits."""
import bpy,numpy as np,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g13_integration_config import *
im=bpy.data.images.load(str(ART/'Generated/G13_Illustrations.png'));w,h=im.size;p=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(p);mask=p.reshape(h,w,4)[::-1,:,3]>.015;ys,xs=np.nonzero(mask);assert len(xs)
cell=dict(key='lavender',pixelRectTopLeft=[0,0,w,h],contentRectInCell=[max(0,int(xs.min())-2),max(0,int(ys.min())-2),min(w,int(xs.max())+3),min(h,int(ys.max())+3)])
(ART/'Generated/G13_AtlasCells.json').write_text(json.dumps(dict(size=[w,h],cells=[cell]),indent=2));print('G13_ICON_BOUNDS_PASS',cell,flush=True)
runtime=[]
for folder,name in [('Seeds','icon_seed_lavender'),('Crops','icon_crop_lavender'),('Crops','icon_crop_lavender_mutation')]:
 path=DEST/folder/(name+'.png')
 if not path.exists():continue
 image=bpy.data.images.load(str(path),check_existing=False);width,height=image.size;values=np.empty(width*height*4,np.float32);image.pixels.foreach_get(values);alpha=values.reshape(height,width,4)[:,:,3];yy,xx=np.nonzero(alpha>.001)
 row=dict(name=name,size=[width,height],minimumAlpha=float(alpha.min()),maximumAlpha=float(alpha.max()),bounds=[int(xx.min()),int(yy.min()),int(xx.max()),int(yy.max())]);runtime.append(row);print('G13_RUNTIME_ICON_ALPHA',row,flush=True)
(ROOT/'ArtSource/Crops/G13/Review/IconAlpha.json').write_text(json.dumps(runtime,indent=2))
