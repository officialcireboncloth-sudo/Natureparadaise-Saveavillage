"""Read source alpha silhouettes for UV-registered geometry; no image pixels are edited."""
import bpy,numpy as np,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g07_config import *
result={}
for kind in ['Spinach','WaterSpinach']:
 im=bpy.data.images.load(str(GENERATED/(kind+'_Leaf.png')));w,h=im.size;a=np.empty(w*h*4,dtype=np.float32);im.pixels.foreach_get(a);a=a.reshape(h,w,4)[::-1]
 mask=(a[:,:,3]>.9)&(a[:,:,1]>.1);spans=[]
 for y in range(h):
  xs=np.nonzero(mask[y])[0];spans.append((float(xs[0]),float(xs[-1])) if len(xs)>3 else None)
 largest=max(r-l for item in spans if item for l,r in [item]);valid=[i for i,item in enumerate(spans) if item and item[1]-item[0]>largest*(.07 if kind=='Spinach' else .11)]
 top=min(i for i,item in enumerate(spans) if item and item[1]-item[0]>5);bottom=max(valid);profile=[]
 for i in range(65):
  t=i/64;y=int(round(bottom-(bottom-top)*t));near=[spans[j] for j in range(max(0,y-2),min(h,y+3)) if spans[j]]
  l,r=np.median(np.array(near),axis=0)
  if i==64:l,r=(l+r)*.5-.15,(l+r)*.5+.15
  profile.append([t,float(l/w),float(r/w),float(1-y/h)])
 result[kind]=dict(width=float(largest/w),baseCenter=(profile[0][1]+profile[0][2])*.5,profile=profile)
 bpy.data.images.remove(im)
(GENERATED/'LeafContours.json').write_text(json.dumps(result,indent=2));print('G07_R02_CONTOURS_PASS',[(k,len(v['profile'])) for k,v in result.items()],flush=True)
