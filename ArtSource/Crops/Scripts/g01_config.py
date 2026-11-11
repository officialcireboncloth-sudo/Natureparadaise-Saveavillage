"""Two supplied berry references. Raspberry remains pending its own reference."""
from plant_library import ROOT,A,SOURCE,TEXTURES as RUNTIME_TEXTURES
GROUP=SOURCE/'G01';REVIEW=GROUP/'Review';MASTER=GROUP/'Blender/G01_Berries_Master.blend';GENERATED=SOURCE/'Textures/Generated/G01';MODELS=A/'mesh/Crops/G01';TEXTURES=GROUP/'Textures'
ATLAS='NP_Plants_Berries_Source_01';RUNTIME_ATLAS='NP_Plants_Unified_01'
PHASES={k:['Seed','Sprout','Young','Flowering','Ready','Harvest'] for k in ['Blueberry','Blackberry']}
SURFACES=[('BlueberryLeaf','#71ad32','#a7d85b','#356e2b','leaf',.25),('BlackberryLeaf','#38782e','#7daa44','#204e26','leaf',.26),('BlueberrySkin','#4773ce','#9bb3e8','#294b9b','fruit',.28),('BlackberryDrupe','#29163f','#674a79','#120c22','fruit',.53),('BarkRed','#95583e','#c48c57','#69422d','bark',.24),('FlowerIvory','#fff0dc','#fffdf1','#e8cdbb','petal',.28),('BerryGreen','#b6ca56','#dce890','#7d9c3d','fruit',.26),('BerryCrown','#203c77','#4868a0','#111c40','fruit',.26),('SeedTan','#d9a150','#f1c57b','#986732','seed',.27),('PollenGold','#dcb52f','#f4da73','#a88528','seed',.32),('StemGreen','#76a142','#a4be68','#496d32','stem',.27),('BerryRed','#c05543','#e38563','#7e3036','fruit',.45)]
def manifest():
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(SURFACES):
  size=256 if i<4 else 128;x=i*256 if i<4 else (i-4)*128;y=0 if i<4 else 256;p=12 if i<4 else 8
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,size,size],uvSafe=[(x+p)/1024,1-(y+size-p)/512,(x+size-p)/1024,1-(y+p)/512],base=base,highlight=hi,shadow=lo,pattern=pattern,smoothness=smooth,primaryDetail=i<4))
 return dict(name=ATLAS,colorSize=[1024,512],normalSize=[1024,512],maskSize=[1024,512],paddingPixels=8,normalConvention='Tangent +Y',materialSettings={'_BumpScale':.28,'_Saturation':1.03,'_Smoothness':1.},surfaceMaskChannels={'R':'metallic zero','A':'smoothness'},slots=slots,runtimeAtlas=RUNTIME_ATLAS,pendingReference=['Raspberry'])
def configure(c,mat):
 c.configure(mat,MODELS);c.slots={s['name']:s for s in manifest()['slots']};c.slots_by_index=tuple(c.slots.values())
