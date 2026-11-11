"""Locate the actual transparent gutter and content bounds without editing pixels."""
import bpy,numpy as np,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g10_integration_config import *
im=bpy.data.images.load(str(ART/'Generated/G10_Illustrations.png'));w,h=im.size;data=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(data);mask=data.reshape(h,w,4)[::-1,:,3]>.015
def cuts(profile,size):
 lo,hi=int(size*.43),int(size*.57);empty=np.flatnonzero(profile[lo:hi]==0)+lo;assert len(empty),'Transparent central gutter missing'
 runs=np.split(empty,np.flatnonzero(np.diff(empty)>1)+1);run=max(runs,key=len);return [0,int((run[0]+run[-1])//2),size]
xs=cuts(mask.sum(axis=0),w);ys=cuts(mask.sum(axis=1),h);cells=[]
for i,(_,kind,key,label) in enumerate(NEW):
 x0,x1=xs[i%2:i%2+2];y0,y1=ys[i//2:i//2+2];cy,cx=np.nonzero(mask[y0:y1,x0:x1]);assert len(cx)
 cells.append(dict(key=key,pixelRectTopLeft=[x0,y0,x1-x0,y1-y0],contentRectInCell=[max(0,int(cx.min())-2),max(0,int(cy.min())-2),min(x1-x0,int(cx.max())+3),min(y1-y0,int(cy.max())+3)]))
(ART/'Generated/G10_AtlasCells.json').write_text(json.dumps(dict(size=[w,h],verticalGutters=xs,horizontalGutters=ys,cells=cells),indent=2));print('G10_ICON_ATLAS_PASS',xs,ys,flush=True)
