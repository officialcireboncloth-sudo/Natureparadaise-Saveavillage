"""Adapt existing paper packets and crop binding code for one new Orchid."""
from g12_integration_config import *
scripts=ROOT/'ArtSource/Crops/Scripts'
t=(scripts/'prepare_g11_crop_integration.py').read_text().replace('g11_integration_config','g12_integration_config').replace('G11','G12').replace('25','26')
(scripts/'prepare_g12_crop_integration.py').write_text(t)
t=(scripts/'apply_g11_crop_integration.py').read_text().replace('g11_integration_config','g12_integration_config').replace('G11','G12').replace('Tulip','Orchid').replace('preceding 25','preceding 26').replace('26 crop profiles','27 crop profiles').replace('Prior 25 crop','Prior 26 crop').replace('G02-G12: 26','G02-G12: 27').replace('totalCrops=26,seedShopProducts=26,totalUiSprites=78','totalCrops=27,seedShopProducts=27,totalUiSprites=81')
(scripts/'apply_g12_crop_integration.py').write_text(t)
t=(ART/'extend_g11_seed_packets_gimp.py').read_text().replace('g11_integration_config','g12_integration_config').replace('G11','G12').replace('26 packets','27 packets').replace('G06-G12 generated','G06-G12 generated')
(ART/'extend_g12_seed_packets_gimp.py').write_text(t)
t=(scripts/'inspect_g11_icon_atlas.py').read_text().replace('g11_integration_config','g12_integration_config').replace('G11','G12').replace("key='tulip'","key='orchid'").replace('tulip','orchid')
(scripts/'inspect_g12_icon_atlas.py').write_text(t)
print('G12_BINDING_WORKFLOWS_READY',flush=True)
