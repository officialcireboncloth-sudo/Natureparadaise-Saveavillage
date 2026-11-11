"""Reference Phalaenopsis; authoring surfaces live outside Assets, runtime uses the shared atlas."""
from plant_library import ROOT,A,SOURCE,TEXTURES as RUNTIME_TEXTURES
GROUP=SOURCE/'G12';REVIEW=GROUP/'Review';MASTER=GROUP/'Blender/G12_Orchid_Master.blend';GENERATED=SOURCE/'Textures/Generated/G12';MODELS=A/'mesh/Crops/G12';TEXTURES=GROUP/'Textures'
ATLAS='NP_Plants_Orchid_Source_01';RUNTIME_ATLAS='NP_Plants_Unified_01'
PHASES={'Orchid':['Seed','Sprout','Young','BudForming','Ready','Harvest']}
SURFACES=[('OrchidPetal','#bb79e1','#e8baf9','#8b42af','petal',.39),('OrchidSepal','#ac65d0','#dfadf0','#753694','petal',.38),('OrchidLip','#a91a98','#df65bf','#661061','petal',.40),('OrchidLeaf','#4c902f','#a4c653','#2c632c','leaf',.43),('StemGreen','#619735','#b0c96b','#3f722e','stem',.31),('RootVelamen','#c9c9a1','#e7dfb8','#9baa87','root',.32),('SeedCream','#d4bb87','#fae9c5','#a98d5e','seed',.30),('ColumnAmber','#d48216','#ffc452','#ad4a12','column',.38)]
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  x,y=i%4*512,i//4*512;p=16;slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,512,512],uvSafe=[(x+p)/2048,1-(y+512-p)/1024,(x+512-p)/2048,1-(y+p)/1024],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth,primaryDetail=i<4))
 return dict(name=ATLAS,colorSize=[2048,1024],normalSize=[2048,1024],maskSize=[2048,1024],paddingPixels=16,normalConvention='Tangent +Y',materialSettings={'_BumpScale':.30,'_Saturation':1.05,'_Smoothness':1.},surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots,runtimeAtlas=RUNTIME_ATLAS)
def configure(c,mat):
 c.configure(mat,MODELS);s=manifest()['slots'];c.slots={x['name']:x for x in s};c.slots_by_index=tuple(s[(row//4)*4+col//2] for row in range(8) for col in range(8))
