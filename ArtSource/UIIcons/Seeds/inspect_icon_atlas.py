"""Read transparent gutters before slicing the generated atlas; no image editing."""
import bpy,numpy as np,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'ArtSource/Crops/Scripts'))
from crop_extension_config import *
p=ART/'Generated/G06_G09_Illustrations.png';im=bpy.data.images.load(str(p));w,h=im.size;data=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(data);alpha=data.reshape(h,w,4)[::-1,:,3];mask=alpha>.015
def cuts(profile,size):
 result=[0]
 for i in [1,2]:
  lo,hi=int(size*(i/3-.075)),int(size*(i/3+.075));empty=np.flatnonzero(profile[lo:hi]==0)+lo
  assert len(empty)>0,'No transparent gutter near '+str(i/3)
  runs=np.split(empty,np.flatnonzero(np.diff(empty)>1)+1);run=max(runs,key=len);result.append(int((run[0]+run[-1])//2))
 return result+[size]
xs=cuts(mask.sum(axis=0),w);ys=cuts(mask.sum(axis=1),h);cells=[]
for i,(_,kind,key,label) in enumerate(NEW):
 x0,x1=xs[i%3:i%3+2];y0,y1=ys[i//3:i//3+2];cy,cx=np.nonzero(mask[y0:y1,x0:x1]);left,top=max(0,int(cx.min())-2),max(0,int(cy.min())-2);right,bottom=min(x1-x0,int(cx.max())+3),min(y1-y0,int(cy.max())+3);cells.append(dict(key=key,pixelRectTopLeft=[x0,y0,x1-x0,y1-y0],contentRectInCell=[left,top,right,bottom]))
(ART/'Generated/AtlasCells.json').write_text(json.dumps(dict(size=[w,h],verticalGutters=xs,horizontalGutters=ys,cells=cells),indent=2));print('ICON_ATLAS_CELLS_PASS',xs,ys,flush=True)
