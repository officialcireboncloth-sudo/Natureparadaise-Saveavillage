"""Raspberry authoring surfaces; runtime remains the existing unified plant atlas."""
from g01_config import *
ATLAS='NP_Plants_Raspberry_Source_01'
def manifest():
 slots=[]
 specs=[('RaspberryLeaf',0,0,256,.25),('RaspberryDrupe',256,0,256,.38)]+[(s[0],i*128,256,128,s[-1]) for i,s in enumerate(SURFACES[4:])]
 for i,(name,x,y,size,smooth) in enumerate(specs):
  pad=12 if size==256 else 8
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,size,size],uvSafe=[(x+pad)/1024,1-(y+size-pad)/512,(x+size-pad)/1024,1-(y+pad)/512],smoothness=smooth,primaryDetail=size==256))
 return dict(name=ATLAS,colorSize=[1024,512],normalSize=[1024,512],maskSize=[1024,512],paddingPixels=8,materialSettings={'_BumpScale':.28,'_Saturation':1.03,'_Smoothness':1.},slots=slots,runtimeAtlas=RUNTIME_ATLAS)
def configure(c,mat):
 c.configure(mat,MODELS);c.slots={s['name']:s for s in manifest()['slots']};c.slots_by_index=tuple(c.slots.values())
