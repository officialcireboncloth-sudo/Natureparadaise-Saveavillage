"""G09 flowering shrubs with a separate high-detail colour atlas."""
from plant_library import ROOT,A,SOURCE,TEXTURES
ATLAS='NP_Plants_ShrubFlowers_01'
GROUP=SOURCE/'G09';REVIEW=GROUP/'Review';MASTER=GROUP/'Blender/G09_FloweringShrubs_Master.blend'
GENERATED=SOURCE/'Textures/Generated/G09';MODELS=A/'mesh/Crops/G09'
PHASES={kind:['Seed','Sprout','Young','BudForming','Ready','Harvest'] for kind in ['Rose','Hibiscus','Jasmine']}
SURFACES=[
 ('RosePetal','#ed2638','#ff6470','#9e102b','petal',.40),
 ('HibiscusPetal','#fa2836','#ff7472','#9d102b','petal',.43),
 ('JasminePetal','#fff5d9','#fffdf2','#e3ce9d','petal',.38),
 ('RoseLeaf','#377e30','#7ab645','#1c5228','leaf',.43),
 ('HibiscusLeaf','#4c9b23','#99cf43','#226620','leaf',.48),
 ('JasmineLeaf','#397f22','#84b437','#225e1b','leaf',.51),
 ('StemGreen','#569729','#99c149','#326d20','stem',.34),
 ('SepalGreen','#438326','#87b137','#245c1b','leaf',.36),
 ('BudRed','#f02b40','#ff7378','#a01430','petal',.42),
 ('BudCream','#fff2c7','#fffce6','#d9c585','petal',.41),
 ('PollenGold','#ffd51b','#fff591','#e5a018','pollen',.31),
 ('StigmaRed','#ec2539','#ff7b71','#a7132b','fruit',.48),
 ('SeedBrown','#98632f','#d49d55','#62421e','seed',.28),
 ('SeedTan','#d4a34c','#f5d183','#a36a2c','seed',.29),
 ('ThornBronze','#ad633c','#dc9c63','#704326','stem',.29),
 ('CutStem','#c8d981','#f2ecc0','#8fbd56','rings',.25),
]
SMOOTH={s[0]:s[-1] for s in SURFACES}
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  x,y=(i%4)*512,(i//4)*512;pad=16
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,512,512],uvSafe=[(x+pad)/2048,1-(y+512-pad)/2048,(x+512-pad)/2048,1-(y+pad)/2048],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth))
 return dict(name=ATLAS,colorSize=[2048,2048],normalSize=[2048,2048],maskSize=[2048,2048],paddingPixels=16,normalConvention='Tangent +Y',surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots)
def configure(common,material):
 common.configure(material,MODELS);spec=manifest();common.slots={s['name']:s for s in spec['slots']}
 common.slots_by_index=tuple(spec['slots'][(row//2)*4+col//2] for row in range(8) for col in range(8))
