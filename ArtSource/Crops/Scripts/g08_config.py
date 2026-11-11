"""G08 cereal art: an independent page with crisp green and golden surfaces."""
from plant_library import ROOT,A,SOURCE,TEXTURES
import json
ATLAS='NP_Plants_Cereal_01'
GROUP=SOURCE/'G08'
REVIEW=GROUP/'Review'
MASTER=GROUP/'Blender/G08_Cereals_Master.blend'
GENERATED=SOURCE/'Textures/Generated/G08'
MODELS=A/'mesh/Crops/G08'
PHASES={p:['Seed','Sprout','Young','Heading','Ready','Harvest'] for p in ['Rice','Wheat']}
EXTRAS={'RiceHuskPile':'NP_Rice_HuskPile','RiceWhitePile':'NP_Rice_WhitePile','WheatGrainPile':'NP_Wheat_GrainPile'}
# Each entry is a complete 512px surface. R is metallic, A is smoothness.
SURFACES=[
 ('RiceGold','#f9bd29','#ffeaa1','#cc8c1d','husk',.56),
 ('RiceGreen','#a8ca23','#e4f76c','#6e9a15','husk',.47),
 ('WheatGold','#ffc33b','#ffeaaa','#d09222','wheat',.58),
 ('WheatGreen','#b0d039','#edfa90','#7e9f1f','wheat',.49),
 ('LeafGreen','#78b319','#d2e651','#34720c','strap',.38),
 ('LeafGold','#dcb026','#ffe36c','#99831b','strap',.33),
 ('StemGreen','#6d9d17','#b9d441','#3c7213','stem',.34),
 ('StemGold','#dfa629','#ffda67','#a57b1c','stem',.32),
 ('RiceSeed','#f5b83a','#ffe7a5','#ca9228','husk',.50),
 ('WheatSeed','#ffc343','#ffe7a5','#d09a2e','wheat',.55),
 ('RiceWhite','#f0e3c8','#fff9e8','#d4bd92','rice',.31),
 ('LeafWarm','#a2ba20','#e8ed61','#5d8d13','strap',.34),
 ('CutStem','#e4ce71','#fff2ac','#ae9f45','rings',.26),
 ('AwnGold','#edb82f','#ffe19a','#ba891b','stem',.30),
 ('LeafUnderside','#84ae37','#c7d95f','#527e19','strap',.30),
 ('RootCream','#cbb881','#fff0be','#a28e55','stem',.22),
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
