"""Reference lavender authoring surfaces; one shared runtime atlas."""
from plant_library import ROOT,A,SOURCE,TEXTURES as RUNTIME_TEXTURES
GROUP=SOURCE/'G13';REVIEW=GROUP/'Review';MASTER=GROUP/'Blender/G13_Lavender_Master.blend';GENERATED=SOURCE/'Textures/Generated/G13';MODELS=A/'mesh/Crops/G13';TEXTURES=GROUP/'Textures'
ATLAS='NP_Plants_Lavender_Source_01';RUNTIME_ATLAS='NP_Plants_Unified_01'
PHASES={'Lavender':['Seed','Sprout','Young','BudForming','Ready','Harvest']}
SURFACES=[('LavenderPetal','#8c54bc','#c799e6','#593580','petal',.34),('LavenderLeaf','#8d9f81','#c1ccb1','#617a65','leaf',.25),('StemSage','#8d9d71','#bbc797','#677b57','stem',.27),('SeedBrown','#866348','#c0996b','#593f30','seed',.23)]
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  x,y=i%2*512,i//2*512;p=16;slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,512,512],uvSafe=[(x+p)/1024,1-(y+512-p)/1024,(x+512-p)/1024,1-(y+p)/1024],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth,primaryDetail=i<2))
 return dict(name=ATLAS,colorSize=[1024,1024],normalSize=[1024,1024],maskSize=[1024,1024],paddingPixels=16,normalConvention='Tangent +Y',materialSettings={'_BumpScale':.30,'_Saturation':1.05,'_Smoothness':1.},surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots,runtimeAtlas=RUNTIME_ATLAS)
def configure(c,mat):
 c.configure(mat,MODELS);s=manifest()['slots'];c.slots={x['name']:x for x in s};c.slots_by_index=tuple(s[(row//4)*2+col//4] for row in range(8) for col in range(8))
