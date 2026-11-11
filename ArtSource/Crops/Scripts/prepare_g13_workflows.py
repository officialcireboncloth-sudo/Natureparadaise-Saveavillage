"""Adapt the established workflow to four lavender surfaces and one new species."""
from g13_config import *
s=SOURCE/'Scripts'
t=(s/'prepare_g12.py').read_text().replace('g12_config','g13_config').replace('G12','G13').replace('26','27')
t=t[:t.index("write(REVIEW/'ReferenceStudy.json'")]+"print('G13_BASELINE_READY',len(load(baseline)['protected']),'protected files',flush=True)\n"
(s/'prepare_g13.py').write_text(t)
t=(s/'paint_g12_gimp.py').read_text().replace('g12_config','g13_config').replace('G12','G13').replace('Orchid_Source_Sheet','Lavender_Source_Sheet').replace('2048,1024','1024,1024').replace('i%4*512,i//4*512','i%2*512,i//2*512')
(s/'paint_g13_gimp.py').write_text(t)
t=(s/'build_g12_blender.py').read_text().replace('g12_config','g13_config').replace('g12_geometry','g13_geometry').replace('G12','G13').replace("R01 reference Phalaenopsis, closed fleshy spatial leaves and articulated flowers","R01 reference lavender, spatial sage foliage and tiered purple florets")
(s/'build_g13_blender.py').write_text(t)
t=(s/'capture_g12_exports.py').read_text().replace('g12_config','g13_config').replace('G12','G13');(s/'capture_g13_exports.py').write_text(t)
t=(s/'prepare_g12_crop_integration.py').read_text().replace('g12_integration_config','g13_integration_config').replace('G12','G13').replace('27','28').replace('26','27')
(s/'prepare_g13_crop_integration.py').write_text(t)
t=(s/'apply_g12_crop_integration.py').read_text().replace('g12_integration_config','g13_integration_config').replace('G12','G13').replace('Orchid','Lavender').replace('preceding 26','preceding 27').replace('27 crop profiles','28 crop profiles').replace('Prior 26 crop','Prior 27 crop').replace('totalCrops=27,seedShopProducts=27,totalUiSprites=81','totalCrops=28,seedShopProducts=28,totalUiSprites=84')
(s/'apply_g13_crop_integration.py').write_text(t)
t=(SOURCE.parent/'UIIcons/Seeds/extend_g12_seed_packets_gimp.py').read_text().replace('g12_integration_config','g13_integration_config').replace('G12','G13').replace('27 packets','28 packets')
(SOURCE.parent/'UIIcons/Seeds/extend_g13_seed_packets_gimp.py').write_text(t)
t=(s/'inspect_g12_icon_atlas.py').read_text().replace('g12_integration_config','g13_integration_config').replace('G12','G13').replace('orchid','lavender');(s/'inspect_g13_icon_atlas.py').write_text(t)
t=(s/'render_g12.py').read_text().replace('g12_config','g13_config').replace('G12','G13').replace('Orchid','Lavender')
t=t.replace("and o.get('FlowerIndex')==1","and o.get('SpikeIndex')==3").replace("o.get('FlowerKind')=='Lavender'","o.get('LavenderFloret') and o.get('SpikeIndex')==3")
t=t.replace("aim((.20,0,.49),0,18,1.95)","aim((0,0,.49),0,18,1.55)")
(s/'render_g13.py').write_text(t)
print('G13_WORKFLOWS_READY',flush=True)
