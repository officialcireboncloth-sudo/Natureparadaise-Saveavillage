"""Shared plant surface vocabulary. UV slots are stable once models use them."""
from pathlib import Path
import json, hashlib, re

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'
SOURCE=ROOT/'ArtSource/Crops'
TEXTURES=A/'Textures/Shared/Plants'
SIZE=1024
TILE=128
PAD=10
# name, base, highlight, shadow, painter pattern, smoothness
COMMON=[
 ('LeafBrassica','#28744b','#58a066','#164b35','leaf',.36),
 ('LeafCabbage','#428829','#79af40','#24591e','leaf',.34),
 ('LeafCabbageInner','#91b93d','#bdd45e','#628c29','leaf',.36),
 ('LeafOval','#588d38','#93b950','#386629','leaf',.19),
 ('LeafSerrated','#3e7d37','#7eac4a','#285b2c','serrated',.17),
 ('LeafLobed','#638c3c','#9bb55a','#3f672e','lobed',.16),
 ('LeafVineRound','#72a23c','#abd060','#47722c','roundleaf',.19),
 ('LeafVineHeart','#4b8c38','#95bb51','#2e612c','leaf',.19),
 ('LeafStrap','#719b35','#b6c45f','#45692a','strap',.16),
 ('LeafNarrow','#75906b','#a9b6a0','#4f715c','strap',.12),
 ('LeafOrchid','#568841','#a4b967','#345f35','leaf',.25),
 ('LeafBean','#62993b','#a2bc59','#3d712b','leaf',.18),
 ('LeafYoung','#65a42c','#a1c94a','#3b7821','leaf',.38),
 ('LeafDark','#397137','#75a04c','#214f2d','leaf',.2),
 ('LeafSage','#829578','#b2bf9c','#536e59','strap',.12),
 ('LeafWaterSpinach','#629c40','#a5c65c','#3c752f','leaf',.2),
 ('StemGreen','#609d2c','#97c447','#387322','stem',.30),
 ('StemPurple','#655446','#a18368','#423e36','stem',.15),
 ('RootCream','#c6b88d','#eee1b7','#9b8a66','stem',.12),
 ('SepalGreen','#527b2d','#9aad4b','#325823','leaf',.16),
 ('SeedTan','#ba8547','#dfb870','#80562f','grain',.12),
 ('SeedBrown','#765438','#b18b58','#4b382b','grain',.12),
 ('SeedBlack','#343630','#716b51','#232720','grain',.12),
 ('SeedCream','#d7c18b','#f2e4b6','#a58c59','grain',.12),
]
CORE=COMMON+[
 ('LeafUnderside','#8ba474','#bac994','#5b7d52','leaf',.12),
 ('LeafPurpleVein','#66804e','#a5ad6c','#4b6440','leaf',.17),
 ('LeafSunflower','#689143','#a6bb63','#42672f','serrated',.15),
 ('LeafHibiscus','#39813f','#84ac59','#235b32','serrated',.2),
 ('LeafRose','#426c3a','#85a25a','#2b4c2d','serrated',.2),
 ('LeafMarigold','#538940','#96af5d','#346630','strap',.14),
 ('LeafChrysanthemum','#648649','#a3b96a','#415f31','lobed',.16),
 ('LeafDahlia','#587940','#9cad61','#36542e','serrated',.16),
 ('StemPale','#80b638','#b4d25a','#508b27','stem',.32),
 ('BarkYoung','#8a6a3d','#c8a16b','#5f472d','bark',.12),
 ('BarkBrown','#725037','#ac8156','#483727','bark',.12),
 ('BarkBerryRed','#855247','#b98566','#563c36','bark',.12),
 ('RootGray','#a5ac8e','#d0d3b2','#747f68','stem',.15),
 ('RootTip','#d2c8a5','#f2e8c6','#ab9c75','stem',.13),
 ('Tendril','#72963d','#aabd5a','#4b702e','stem',.17),
 ('Thorn','#7a6341','#ac976a','#51452f','grain',.13),
 ('SeedGreen','#92a154','#c3cd80','#65773b','grain',.12),
 ('SeedGrain','#c2a355','#ead083','#90753c','grain',.12),
 ('SeedStripe','#3b3b30','#c9bb91','#252b25','seedstripe',.13),
 ('BulbSkin','#bd894b','#e3bf7c','#8a622f','onion',.15),
 ('SoilDark','#604931','#896a47','#3b3024','speckle',.08),
 ('SoilLight','#997345','#b99664','#735638','speckle',.08),
 ('BudGreen','#809b3e','#bec469','#506e2d','petal',.17),
 ('BudPale','#acc06d','#d6dc9f','#7b954a','petal',.18),
 ('CutStem','#bfd17d','#e1e8ae','#8da35d','rings',.14),
 ('HollowStem','#94b85b','#c4d885','#547c37','rings',.17),
 ('LeafDry','#ad9e55','#d0bd79','#817541','strap',.1),
 ('StemGold','#bfaa55','#e4cd82','#907b3c','stem',.12),
 ('LeafWarm','#7f9b41','#b9c264','#526f30','leaf',.16),
 ('LeafCool','#4e8474','#90b4a1','#315b57','leaf',.17),
 ('VeinCream','#c1cf92','#e2e8b6','#8da46a','stem',.16),
 ('VeinGreen','#a1bd74','#cbda96','#6e954c','stem',.17),
 ('FlowerWhite','#e9e0b9','#fff3d7','#c7c798','petal',.18),
 ('FlowerGold','#dcae36','#f6d56b','#b78228','petal',.2),
 ('FlowerPurple','#9c70ad','#c59bce','#6d4d84','petal',.2),
 ('PollenGold','#d5a33b','#f3cb64','#a77728','speckle',.13),
 ('FruitUnripe','#8ba743','#c2cd6d','#597b30','fruit',.25),
 ('LeafBud','#719946','#b0c368','#456c32','leaf',.17),
 ('SeedPink','#ac765f','#d6a680','#784f42','grain',.12),
 ('SeedWhite','#ded9b3','#f8f1d5','#b7b38b','grain',.12),
]
PRODUCE=COMMON+[
 ('BerryBlue','#49689f','#8a9ec4','#2d4377','fruit',.28),
 ('BerryRed','#b93f53','#e77d7d','#7b2d41','fruit',.23),
 ('BerryDark','#403649','#796080','#252536','fruit',.24),
 ('FruitUnripe','#94ac4c','#c5d37c','#628334','fruit',.24),
 ('WatermelonStripe','#356442','#86a251','#234d36','watermelon',.24),
 ('MelonNet','#c4b76b','#e3d497','#97904f','melon',.16),
 ('KiwiSkin','#8f612f','#b88b4e','#684626','kiwi',.12),
 ('OnionSkin','#ca914a','#eed091','#9c6b36','onion',.2),
 ('RadishGradient','#c44652','#f4dfbb','#95374b','radish',.23),
 ('EggplantPurple','#5f3c74','#a378ad','#3c2c50','fruit',.32),
 ('ChiliRed','#bd4535','#e48250','#86302a','fruit',.29),
 ('ChiliGreen','#56873b','#9bb54f','#365d2d','fruit',.28),
 ('PodGreen','#83a743','#b8cb71','#567e33','fruit',.24),
 ('CucumberSkin','#24552c','#659747','#193e23','cucumber',.42),
 ('ZucchiniSkin','#315843','#708a53','#224334','zucchini',.27),
 ('BroccoliHead','#286b27','#558c32','#17431c','cluster',.22),
 ('CauliflowerHead','#e4d9b5','#fbf1d2','#c1b48f','cluster',.24),
 ('CabbageHead','#94bc3e','#c0d96b','#628f28','leaf',.36),
 ('GrainGreen','#91a843','#c6d171','#617e31','grain',.14),
 ('GrainGold','#c9a448','#eed485','#967735','grain',.14),
 ('WatermelonFlesh','#d26158','#f4967d','#a94445','flesh',.2),
 ('MelonFlesh','#cad781','#e6e6ae','#a4b96b','flesh',.2),
 ('KiwiFlesh','#93b64c','#d0d985','#648f35','kiwicut',.23),
 ('OnionCut','#eadcbb','#fff0d7','#c4b995','rings',.18),
 ('RadishCut','#e9e0bf','#fff5da','#c6c29f','rings',.2),
 ('EggplantFlesh','#ded3a6','#f2e7c1','#b5aa7e','flesh',.19),
 ('SeedStripe','#3e3d31','#cfbf91','#292c26','seedstripe',.13),
 ('FlowerWhite','#e9e1c7','#fff3df','#c6c7a3','petal',.18),
 ('FlowerGold','#e3b33f','#f8d97b','#b6812d','petal',.19),
 ('FlowerPurple','#a06fb3','#d1a7dc','#6f4988','petal',.19),
 ('PollenGold','#d5a43d','#f2cf72','#a8792b','speckle',.12),
 ('BerryCrown','#324660','#5e7195','#24364d','fruit',.22),
 ('StemPale','#80b638','#b4d25a','#508b27','stem',.32),
 ('RootGray','#a5ac8e','#d0d3b2','#747f68','stem',.13),
 ('BarkBrown','#806040','#b58f62','#56422f','bark',.12),
 ('CutStem','#c1ce80','#e1e7b0','#8caa5b','rings',.14),
 ('SeedGreen','#9aaa5b','#c7d386','#6b8040','grain',.12),
 ('HollowStem','#93b65c','#c7d98b','#567e39','rings',.17),
 ('BulbSkin','#be8b4b','#e5c082','#896135','onion',.17),
 ('LeafUnderside','#8ea981','#c0cda1','#637f5b','leaf',.13),
]
# G02 R02: three Produce slots refined against the supplied zucchini reference.
# Coordinates and every other page/slot retain their established values.
_G02_REFERENCE_COLORS={
 'LeafLobed':('#4b792b','#70993e','#325a22'),
 'ZucchiniSkin':('#24451f','#547032','#16331a'),
 'FlowerGold':('#f6b900','#ffda43','#d88a03'),
}
PRODUCE=[(n,*_G02_REFERENCE_COLORS[n],p,s) if n in _G02_REFERENCE_COLORS else (n,b,h,l,p,s)
         for n,b,h,l,p,s in PRODUCE]
FLOWERS=COMMON+[
 ('PetalWhite','#ece4cc','#fff5e4','#c8c7a5','petal',.2),
 ('PetalCream','#e4d9b6','#fff1d3','#bcb994','petal',.19),
 ('PetalGold','#dfab39','#f8d373','#b48028','petal',.21),
 ('PetalYellow','#edc34b','#ffe398','#c59732','petal',.2),
 ('PetalPink','#cf7a99','#f3b9c4','#aa5177','petal',.21),
 ('PetalCoral','#db897d','#f6b6a2','#b45b60','petal',.21),
 ('PetalRed','#b9494b','#e27d70','#842c3c','petal',.21),
 ('PetalPurple','#ac7ac1','#d7b4e5','#784b98','petal',.22),
 ('PetalLavender','#9275ad','#c0a5cd','#674e84','petal',.19),
 ('PetalOrange','#df963d','#f8cb77','#b76c27','petal',.2),
 ('OrchidLip','#a14986','#e59fbd','#6c3463','orchid',.23),
 ('FlowerCenter','#caa143','#edcc78','#9d742c','speckle',.14),
 ('SunflowerDisk','#795332','#b38b48','#513a28','sunflower',.11),
 ('PollenGold','#d6a439','#f7cf69','#a57728','speckle',.13),
 ('HibiscusFilament','#dfb16a','#f4d39b','#b87a48','stem',.17),
 ('BudPink','#c28291','#e7b4b9','#9e5d75','petal',.21),
 ('BudRed','#aa4542','#d58265','#783132','petal',.2),
 ('BudPurple','#93729f','#c2a0c6','#6b527c','petal',.21),
 ('BudWhite','#d8d6ab','#f0eac9','#adb48c','petal',.19),
 ('SeedStripe','#3d3c32','#d1bf92','#272c26','seedstripe',.13),
 ('LavenderFloret','#9576af','#c3a1ce','#6b4e87','petal',.18),
 ('RoseInner','#a33947','#d16c70','#762837','petal',.22),
 ('MarigoldRuffle','#d88c35','#f5bf63','#ad6327','petal',.19),
 ('DahliaPetal','#d58d91','#f3bbb6','#aa586d','petal',.21),
 ('LeafRose','#426c3a','#85a25a','#2b4c2d','serrated',.19),
 ('LeafHibiscus','#39813f','#84ac59','#235b32','serrated',.2),
 ('LeafJasmine','#4e7f39','#93ad57','#315a2c','leaf',.23),
 ('LeafSunflower','#689143','#a6bb63','#42672f','serrated',.16),
 ('LeafChrysanthemum','#648649','#a3b96a','#415f31','lobed',.16),
 ('LeafDahlia','#587940','#9cad61','#36542e','serrated',.16),
 ('LeafMarigold','#538940','#96af5d','#346630','strap',.15),
 ('LeafLavender','#85967b','#b3c0a5','#59705f','strap',.13),
 ('RootGray','#a6ac90','#d0d6b5','#757f6b','stem',.14),
 ('BulbSkin','#c18e51','#e3bf84','#906439','onion',.15),
 ('BarkBrown','#79603f','#b08c5d','#51412d','bark',.12),
 ('Thorn','#7d6644','#b1986b','#51462e','grain',.12),
 ('FlowerGreen','#8ba44d','#bdca7c','#5e7f38','petal',.17),
 ('PetalBack','#b98f91','#dec0b1','#916a76','petal',.18),
 ('LeafUnderside','#91a47d','#becba0','#647c56','leaf',.13),
 ('CutStem','#c1ce80','#e1e7b0','#8caa5b','rings',.14),
]
PAGES={'Core':CORE,'Produce':PRODUCE,'Flowers':FLOWERS}
assert all(len(v)==64 and len({s[0] for s in v})==64 for v in PAGES.values()),{k:len(v) for k,v in PAGES.items()}
GROUPS={
 'G01':['Blueberry','Raspberry','Blackberry'], 'G02':['Watermelon','Melon','Zucchini'],
 'G03':['Kiwi','Pea','Cucumber'], 'G04':['Eggplant','Chili Pepper','Green Bean'],
 'G05':['Cabbage','Broccoli','Cauliflower'], 'G06':['Radish','Onion'],
 'G07':['Spinach','Water Spinach'], 'G08':['Rice','Wheat'],
 'G09':['Rose','Hibiscus','Jasmine'], 'G10':['Sunflower','Chrysanthemum','Dahlia','Marigold'],
 'G11':['Tulip'],'G12':['Orchid'],'G13':['Lavender']}
CROP_SLOTS={
 'Blueberry':['LeafOval','BarkBrown','FlowerWhite','BerryBlue','BerryCrown','FruitUnripe'],
 'Raspberry':['LeafSerrated','BarkBrown','FlowerWhite','BerryRed','FruitUnripe'],
 'Blackberry':['LeafSerrated','BarkBrown','FlowerWhite','BerryDark','FruitUnripe'],
 'Watermelon':['LeafVineHeart','SeedBlack','SepalGreen','WatermelonStripe','WatermelonFlesh','FlowerGold','PollenGold','FruitUnripe'],
 'Melon':['LeafVineRound','SeedCream','SepalGreen','MelonNet','MelonFlesh','FlowerGold','PollenGold','FruitUnripe'],
 'Zucchini':['LeafLobed','SeedCream','SepalGreen','ZucchiniSkin','FlowerGold','PollenGold'],
 'Kiwi':['LeafBean','BarkBrown','KiwiSkin','KiwiFlesh','FlowerWhite','SeedBlack','PollenGold','FruitUnripe','SepalGreen'],
 'Pea':['LeafOval','PodGreen','SeedGreen','SeedCream','FlowerWhite','SepalGreen','BarkBrown'],
 'Cucumber':['LeafVineHeart','CucumberSkin','FlowerGold','PollenGold','SepalGreen','SeedCream','BarkBrown','MelonFlesh'],
 'Eggplant':['LeafSerrated','StemPurple','EggplantPurple','EggplantFlesh','FlowerPurple','SeedCream','SeedTan','SepalGreen','PollenGold'],
 'Chili Pepper':['LeafNarrow','ChiliRed','ChiliGreen','BerryRed','FlowerWhite','SeedCream','SeedTan','SepalGreen','PollenGold','EggplantFlesh'],
 'Green Bean':['LeafDark','GrainGreen','SeedCream','SeedGreen','FlowerWhite','SepalGreen'],
 'Cabbage':['LeafCabbage','LeafCabbageInner','LeafBrassica','CabbageHead'],
 'Broccoli':['LeafBrassica','StemPale','BroccoliHead'],
 'Cauliflower':['LeafBrassica','LeafCabbage','CauliflowerHead'],
 'Radish':['LeafLobed','RadishGradient','RadishCut','RootCream'],
 'Onion':['LeafStrap','OnionSkin','OnionCut','RootCream','BulbSkin'],
 'Spinach':['LeafDark','LeafOval'], 'Water Spinach':['LeafWaterSpinach','HollowStem'],
 'Rice':['LeafStrap','GrainGreen','GrainGold'], 'Wheat':['LeafStrap','GrainGreen','GrainGold'],
 'Rose':['LeafRose','BarkBrown','Thorn','PetalRed','RoseInner','BudRed'],
 'Hibiscus':['LeafHibiscus','PetalRed','HibiscusFilament','PollenGold','BudRed'],
 'Jasmine':['LeafJasmine','PetalWhite','PetalCream','BudWhite'],
 'Sunflower':['LeafSunflower','PetalGold','PetalYellow','SunflowerDisk','SeedStripe'],
 'Chrysanthemum':['LeafChrysanthemum','PetalGold','PetalYellow','FlowerGreen'],
 'Dahlia':['LeafDahlia','PetalCoral','DahliaPetal','BudPink'],
 'Marigold':['LeafMarigold','PetalOrange','MarigoldRuffle','FlowerGreen'],
 'Tulip':['LeafStrap','BulbSkin','PetalPink','BudPink'],
 'Orchid':['LeafOrchid','RootGray','PetalPurple','OrchidLip','BudPurple'],
 'Lavender':['LeafLavender','PetalLavender','LavenderFloret','BudPurple'],
}

def manifest(page):
 slots=[]
 for i,(name,base,hi,lo,pattern,smooth) in enumerate(PAGES[page]):
  x,y=(i%8)*TILE,(i//8)*TILE
  slots.append(dict(name=name,index=i,pixelRectTopLeft=[x,y,TILE,TILE],uvSafe=[(x+PAD)/SIZE,1-(y+TILE-PAD)/SIZE,(x+TILE-PAD)/SIZE,1-(y+PAD)/SIZE],base=base,highlight=hi,shadow=lo,pattern=pattern,metallic=0,smoothness=smooth))
 return dict(version=1,name='NP_Plants_'+page+'_01',colorSize=[SIZE,SIZE],normalSize=[SIZE,SIZE],maskSize=[SIZE,SIZE],paddingPixels=PAD,normalConvention='Tangent +Y, GIMP GEGL normal-map',surfaceMaskChannels={'R':'metallic (zero)','A':'smoothness'},authoring='Native GIMP 3, editable per-slot layers from deterministic vector brush masters',slots=slots)

def catalog():
 result=[]
 for group,names in GROUPS.items():
  for name in names:
   page='Flowers' if group in ['G09','G10','G11','G12','G13'] else 'Produce'
   slots=['LeafYoung','StemGreen','SeedBrown']+CROP_SLOTS[name]
   assert set(slots)<={s[0] for s in PAGES[page]},(name,slots)
   prepared=(SOURCE/group/'Blender'/(group+'_Manifest.json')).is_file()
   if group=='G01' and prepared:prepared=name in json.loads((SOURCE/group/'Blender'/(group+'_Manifest.json')).read_text())['plants']
   atlas='NP_Plants_'+page+'_01'
   if group in ['G01','G03','G04','G06','G07','G08','G09','G10','G11','G12','G13'] and prepared:atlas=json.loads((SOURCE/group/'Blender'/(group+'_Manifest.json')).read_text()).get('atlas',atlas)
   if group=='G09' and prepared:
    slots=[name+'Leaf',name+'Petal','StemGreen','SepalGreen','BudCream' if name=='Jasmine' else 'BudRed','SeedTan' if name=='Rose' else 'SeedBrown','CutStem']+(['ThornBronze'] if name=='Rose' else ['PollenGold','StigmaRed'] if name=='Hibiscus' else [])
   if group=='G10' and prepared:
    slots=[name+'Leaf',name+'Petal','StemGreen','SepalGreen','BudGreen','CutStem','SeedStripe' if name=='Sunflower' else 'SeedBrown' if name=='Chrysanthemum' else 'SeedBlack']+(['SunflowerDisk'] if name=='Sunflower' else [])
   if group=='G11' and prepared:slots=['TulipPetal','TulipBud','TulipLeaf','BulbSkin','StemGreen','SepalGreen','RootCream','CutStem']
   if group=='G12' and prepared:slots=['OrchidPetal','OrchidSepal','OrchidLip','OrchidLeaf','StemGreen','RootVelamen','SeedCream','ColumnAmber']
   if group=='G13' and prepared:slots=['LavenderPetal','LavenderLeaf','StemSage','SeedBrown']
   if group=='G01' and prepared:slots=[name+'Leaf','BlueberrySkin' if name=='Blueberry' else 'BlackberryDrupe','BarkRed','FlowerIvory','BerryGreen','BerryCrown','SeedTan','PollenGold','StemGreen','BerryRed']
   result.append(dict(plantId='art.'+name.lower().replace(' ','_'),displayName=name,group=group,atlas=atlas,slots=list(dict.fromkeys(slots)),artStatus=group+' model batch' if prepared else 'textures prepared; model pending'))
 if (SOURCE/'Unified/AtlasRegistry.json').exists():
  for row in result:
   row['authoringAtlas']=row['atlas'];row['atlas']='NP_Plants_Unified_01';row['runtimeMaterial']='NP_Plants_Unified_01';row['atlasPolicy']='Append source surfaces to the shared registry; never introduce a per-group runtime material'
 g03_atlas=next(row['atlas'] for row in result if row['group']=='G03')
 return dict(schemaVersion=1,plants=result,missingReference=['Daisy','Moonflower'],sharedEarlyStages='Core atlas: reusable round/oval/flat seeds; broad two-leaf sprout. Single-leaf/strap/bulb sprouts remain distinct.',trellisMaterial='NP_CozySurfaces_01 (existing wood/rope)',trellisMaterialByGroup={'G03':g03_atlas+' / BarkBrown'})

if __name__=='__main__':
 if (SOURCE/'Unified/AtlasRegistry.json').exists():
  raise SystemExit('The shared runtime atlas is active. Use prepare_plant_unity.py --group Gxx; do not reset the accepted source-page layouts.')
 for p in [TEXTURES,SOURCE/'Textures',SOURCE/'Review',SOURCE/'Previews',SOURCE/'G05/Blender']:p.mkdir(parents=True,exist_ok=True)
 existing={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (A/'Textures/Shared/Cozy').glob('*') if p.is_file()}
 old=SOURCE/'Review/ExistingAtlasHashes.json'
 if not old.exists():old.write_text(json.dumps(existing,indent=2))
 for page in PAGES:(TEXTURES/('NP_Plants_'+page+'_01_Slots.json')).write_text(json.dumps(manifest(page),indent=2))
 (TEXTURES/'NP_Plants_Catalog.json').write_text(json.dumps(catalog(),indent=2))
 print('PLANT_LIBRARY: 3 pages, 192 named slots, 31 plants mapped, stable 10px UV gutters')
