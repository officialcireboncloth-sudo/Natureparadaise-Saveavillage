"""Reference-matched tulip surfaces and visual phases on an independent page."""
from plant_library import ROOT,A,SOURCE,TEXTURES
ATLAS='NP_Plants_Tulip_01';GROUP=SOURCE/'G11';REVIEW=GROUP/'Review'
MASTER=GROUP/'Blender/G11_Tulip_Master.blend';GENERATED=SOURCE/'Textures/Generated/G11';MODELS=A/'mesh/Crops/G11'
PHASES={'Tulip':['Bulb','Sprout','Young','BudForming','Ready','Harvest']}
SURFACES=[
 ('TulipPetal','#ef5b93','#ffb4d0','#b62760','petal',.39),
 ('TulipBud','#f46d9d','#ffb7cd','#b83c6e','petal',.39),
 ('TulipLeaf','#519833','#93c75b','#2c6d29','leaf',.35),
 ('BulbSkin','#c48b4d','#f3d1a0','#906136','bulb',.23),
 ('StemGreen','#67aa30','#acd15c','#387e22','stem',.32),
 ('SepalGreen','#76ab3f','#b4cd69','#478529','stem',.33),
 ('RootCream','#c4a06e','#f3d5a2','#97714a','stem',.20),
 ('CutStem','#c9d38c','#f4ebbd','#91ab50','rings',.25),
]
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  x,y=i%4*512,i//4*512;p=16
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,512,512],uvSafe=[(x+p)/2048,1-(y+512-p)/1024,(x+512-p)/2048,1-(y+p)/1024],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth))
 return dict(name=ATLAS,colorSize=[2048,1024],normalSize=[2048,1024],maskSize=[2048,1024],paddingPixels=16,normalConvention='Tangent +Y',surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots)
def configure(c,material):
 c.configure(material,MODELS);s=manifest()['slots'];c.slots={x['name']:x for x in s};c.slots_by_index=tuple(s[(row//4)*4+col//2] for row in range(8) for col in range(8))
