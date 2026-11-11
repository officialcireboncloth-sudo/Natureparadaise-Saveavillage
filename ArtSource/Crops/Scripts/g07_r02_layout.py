"""Two 1024 leaf regions and eight 512 utility regions in a single 2048 atlas."""
import json
from g07_config import *
REGIONS=[('LeafOrchid',0,0,1024),('LeafWaterSpinach',1024,0,1024),
 ('LeafYoungSpinach',0,1024,512),('LeafYoungWaterSpinach',512,1024,512),('StemPale',1024,1024,512),('HollowStem',1536,1024,512),
 ('SeedTan',0,1536,512),('SeedBrown',512,1536,512),('CutStem',1024,1536,512),('LeafUnderside',1536,1536,512)]
def manifest():
 slots=[]
 for i,(name,x,y,size) in enumerate(REGIONS):
  pad=24 if size==1024 else 12
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,size,size],uvSafe=[(x+pad)/2048,1-(y+size-pad)/2048,(x+size-pad)/2048,1-(y+pad)/2048],smoothness=SMOOTH[name]))
 return dict(name=ATLAS,colorSize=[2048,2048],normalSize=[2048,2048],maskSize=[2048,2048],revision='G07 R02',surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots)
def configure(common,material):
 common.configure(material,MODELS);spec=manifest();common.slots={s['name']:s for s in spec['slots']}
 grid=[]
 for row in range(8):
  for col in range(8):
   x,y=col*256+128,row*256+128
   grid.append(next(s for s in spec['slots'] if s['pixelRectTopLeft'][0]<=x<s['pixelRectTopLeft'][0]+s['pixelRectTopLeft'][2] and s['pixelRectTopLeft'][1]<=y<s['pixelRectTopLeft'][1]+s['pixelRectTopLeft'][3]))
 common.slots_by_index=tuple(grid)
