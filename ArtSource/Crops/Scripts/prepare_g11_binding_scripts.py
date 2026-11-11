"""Reuse accepted paper packet and binding workflows for the single tulip."""
from g11_integration_config import *
scripts=ROOT/'ArtSource/Crops/Scripts'
t=(scripts/'prepare_g10_crop_integration.py').read_text().replace('g10_integration_config','g11_integration_config').replace('G10','G11').replace('21','25')
(scripts/'prepare_g11_crop_integration.py').write_text(t)
t=(scripts/'apply_g10_crop_integration.py').read_text().replace('g10_integration_config','g11_integration_config').replace('G10','G11').replace('four G11 flowers','Tulip').replace('preceding 21','preceding 25').replace('25 crop profiles','26 crop profiles').replace('Prior 21 crop','Prior 25 crop').replace('four G11 flowers','Tulip').replace('addedCrops=4,totalCrops=25,seedShopProducts=25,totalUiSprites=75','addedCrops=1,totalCrops=26,seedShopProducts=26,totalUiSprites=78')
(scripts/'apply_g11_crop_integration.py').write_text(t)
t=(ART/'extend_g10_seed_packets_gimp.py').read_text().replace('g10_integration_config','g11_integration_config').replace('G10','G11').replace('newPackets=4,newProduceSprites=4,newMutationSprites=4,seedMastersReopened=4,produceMastersReopened=4','newPackets=1,newProduceSprites=1,newMutationSprites=1,seedMastersReopened=1,produceMastersReopened=1').replace('25 packets, four new editable source pairs','26 packets, one new editable source pair')
(ART/'extend_g11_seed_packets_gimp.py').write_text(t)
t=(ART/'review_g10_seed_packets_gimp.py').read_text().replace('g10_integration_config','g11_integration_config').replace('G10','G11').replace('576,576','288,288')
(ART/'review_g11_seed_packets_gimp.py').write_text(t)
print('G11_BINDING_WORKFLOWS_READY',flush=True)
