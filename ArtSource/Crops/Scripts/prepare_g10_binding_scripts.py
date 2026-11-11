"""Extend the proven GIMP and binding workflows for G10 only."""
from g10_integration_config import *
def replace(t,a,b):
 assert a in t,a
 return t.replace(a,b)
t=(ART/'extend_seed_packets_gimp.py').read_text()
t=replace(t,'from crop_extension_config import *','from g10_integration_config import *')
t=t.replace('G06_G09_Illustrations.png','G10_Illustrations.png').replace('Generated/AtlasCells.json','Generated/G10_AtlasCells.json')
t=t.replace('2016,864','2016,1152').replace('G06_G09_IconAudit.json','G10_IconAudit.json')
t=t.replace('newPackets=9,newProduceSprites=9,newMutationSprites=9,seedMastersReopened=9,produceMastersReopened=9','newPackets=4,newProduceSprites=4,newMutationSprites=4,seedMastersReopened=4,produceMastersReopened=4')
t=t.replace('G06-G09 generated illustrations','G06-G10 generated illustrations').replace('GIMP_SEED_EXTENSION_PASS 21 packets, nine new editable sources pairs','GIMP_G10_SEED_PASS 25 packets, four new editable source pairs')
(ART/'extend_g10_seed_packets_gimp.py').write_text(t)
t=(ART/'review_seed_extension_gimp.py').read_text().replace('from crop_extension_config import *','from g10_integration_config import *').replace('864,864','576,576').replace('i%3*288','i%2*288').replace('i//3*288','i//2*288').replace('G06_G09_SeedPackets_Preview.png','G10_SeedPackets_Preview.png')
(ART/'review_g10_seed_packets_gimp.py').write_text(t)
t=(ROOT/'ArtSource/Crops/Scripts/apply_crop_extension.py').read_text()
t=t.replace('Append G06-G09 to live crop/item/shop assets without rewriting the first twelve.','Append four G10 flowers without rewriting the preceding 21 crop bindings.')
t=replace(t,'from crop_extension_config import *','from g10_integration_config import *')
t=t.replace("load(OUT/'G06_G09_Baseline.json')","load(BASELINE)")
start=t.index('for p,header,rows in [(A/\'Data/Balance/Items.csv\'')
end=t.index('oldByKey=',start)
t=t[:start]+"plans=[dict(path=rel(p),header=h,rows=[[r[k] for k in h] for r in rows]) for p,h,rows in [(A/'Data/Balance/Items.csv',item_header,item_rows),(A/'Data/Balance/Crops.csv',crop_header,crop_rows)]]\n(OUT/'G10_CsvEditPlan.json').write_text(json.dumps(plans,indent=2))\n"+t[end:]
t=t.replace('G02-G09: 21 crop profiles and seed-store products; latest accepted prefab revisions','G02-G10: 25 crop profiles and seed-store products; accepted prefab revisions')
t=t.replace("rel(OUT/'G06_G09_Baseline.json')","rel(BASELINE)")
t=t.replace('Prior 12 rows retained; nine added with Cabbage defaults. Flower produce is nonfood.','Prior 21 crop balances retained; four G10 flowers use existing 5-day/5-G seed/12-G harvest integration defaults, nonfood.')
t=t.replace("(OUT/'Manifest.json').write_text", "manifest['extensionSummary']=dict(addedCrops=4,totalCrops=25,seedShopProducts=25,totalUiSprites=75,acceptedModelsPreserved=True,legacyWheatIngredientReused=True)\n(OUT/'Manifest.json').write_text")
t=t.replace('APPLIED_CROP_EXTENSION_PASS','APPLIED_G10_CROPS_PASS')
(ROOT/'ArtSource/Crops/Scripts/apply_g10_crop_integration.py').write_text(t)
print('G10_BINDING_WORKFLOWS_READY',flush=True)
