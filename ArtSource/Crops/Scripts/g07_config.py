"""G07 R02 uses its own Leafy atlas with registered high-resolution leaf regions."""
from plant_library import ROOT,A,SOURCE,TEXTURES
ATLAS='NP_Plants_Leafy_01'
GROUP=SOURCE/'G07'
REVIEW=GROUP/'Review'
MASTER=GROUP/'Blender/G07_LeafyVegetables_Master.blend'
GENERATED=SOURCE/'Textures/Generated/G07_R02'
MODELS=A/'mesh/Crops/G07'
CHANGED=['LeafOrchid','LeafWaterSpinach','StemPale','HollowStem','CutStem']
SMOOTH=dict(LeafOrchid=.52,LeafWaterSpinach=.46,LeafYoungSpinach=.45,LeafYoungWaterSpinach=.43,StemPale=.40,HollowStem=.24,CutStem=.36,SeedTan=.18,SeedBrown=.17,LeafUnderside=.30)
PALETTE={
 'LeafOrchid':('#46852d','#7aad48','#295e24'),
 'LeafWaterSpinach':('#588e40','#99ba60','#397130'),
 'StemPale':('#7fbd27','#c5df69','#4b9321'),
 'HollowStem':('#315d20','#608b37','#203e16'),
 'CutStem':('#b2ce6e','#e0eaa2','#6e9b3a'),
}
PHASES={kind:['Seed','Sprout','Young','LeafGrowth','Ready','Harvest'] for kind in ['Spinach','WaterSpinach']}
EXTRAS={'Spinach':'NP_Spinach_LooseLeaf','WaterSpinach':'NP_WaterSpinach_LooseSprig'}
