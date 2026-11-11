"""G10 radial flowers; new stable page, all existing plant pages retained."""
from plant_library import ROOT,A,SOURCE,TEXTURES
ATLAS='NP_Plants_RadialFlowers_01'
GROUP=SOURCE/'G10'; REVIEW=GROUP/'Review'
MASTER=GROUP/'Blender/G10_RadialFlowers_Master.blend'
GENERATED=SOURCE/'Textures/Generated/G10'; MODELS=A/'mesh/Crops/G10'
PHASES={k:['Seed','Sprout','Young','BudForming','Ready','Harvest'] for k in ['Sunflower','Chrysanthemum','Dahlia','Marigold']}
SURFACES=[
 ('SunflowerPetal','#ffcb09','#fff074','#d98609','petal',.34),
 ('ChrysanthemumPetal','#ffd52e','#fff79a','#d8940c','petal',.34),
 ('DahliaPetal','#f23888','#ff8fbb','#b51b5a','petal',.40),
 ('MarigoldPetal','#ff9710','#ffd342','#dc4f06','petal',.34),
 ('SunflowerLeaf','#4e9b20','#98cf42','#286b1c','leaf',.32),
 ('ChrysanthemumLeaf','#318b28','#83c840','#1b5f27','leaf',.31),
 ('DahliaLeaf','#27862b','#78be43','#145426','leaf',.41),
 ('MarigoldLeaf','#5da51d','#a3d63e','#2d7517','leaf',.32),
 ('StemGreen','#56992c','#9ac74f','#2d6e23','stem',.29),
 ('SepalGreen','#4c8c24','#92be45','#28601d','leaf',.30),
 ('BudGreen','#71a628','#c0d65e','#3d7b23','bud',.32),
 ('SunflowerDisk','#754222','#b68034','#402819','disk',.20),
 ('SeedStripe','#393b30','#c5bb8f','#1e251e','stripe',.23),
 ('SeedBrown','#a17438','#deb76a','#60491f','seed',.22),
 ('SeedBlack','#343326','#81774d','#1c221b','seed',.23),
 ('CutStem','#c5d98b','#eef0ba','#85b24c','rings',.24),
]
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  x,y=i%4*512,i//4*512;p=16
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,512,512],uvSafe=[(x+p)/2048,1-(y+512-p)/2048,(x+512-p)/2048,1-(y+p)/2048],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth))
 return dict(name=ATLAS,colorSize=[2048,2048],normalSize=[2048,2048],maskSize=[2048,2048],paddingPixels=16,normalConvention='Tangent +Y',surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots)
def configure(c,material):
 c.configure(material,MODELS);s=manifest()['slots'];c.slots={x['name']:x for x in s}
 c.slots_by_index=tuple(s[(row//2)*4+col//2] for row in range(8) for col in range(8))
