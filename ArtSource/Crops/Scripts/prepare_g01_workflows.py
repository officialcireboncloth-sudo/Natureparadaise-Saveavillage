"""Adapt accepted export and binding workflows for the two supplied berry species."""
from g01_config import *
S=SOURCE/'Scripts'
def adapted(src,dst,changes=()):
 t=src.read_text().replace('g13','g01').replace('G13','G01')
 for a,b in changes:t=t.replace(a,b)
 dst.write_text(t,encoding='utf-8')
adapted(S/'build_g13_blender.py',S/'build_g01_blender.py',[("R01 reference lavender, spatial sage foliage and tiered purple florets","R01 supplied Blueberry and Blackberry; Raspberry awaits reference"),("u,v=loop.uv;s=C.slots_by_index[max(0,min(7,int((1-v)*8)))*8+max(0,min(7,int(u*8)))];a,b,c,d=s['uvSafe'];loop.uv=(max(a,min(c,u)),max(b,min(d,v)))","u,v=loop.uv;s=min(C.slots.values(),key=lambda s:sum((max(s['uvSafe'][k]-z,0,z-s['uvSafe'][k+2]))**2 for k,z in enumerate([u,v])));a,b,c,d=s['uvSafe'];loop.uv=(max(a,min(c,u)),max(b,min(d,v)))")])
adapted(S/'capture_g13_exports.py',S/'capture_g01_exports.py')
adapted(S/'prepare_g13_crop_integration.py',S/'prepare_g01_crop_integration.py',[("27","28"),("if not raw.exists():raw.write_bytes(base64.b64decode((ROOT/'tmp/G01_IconSource.b64').read_text()))","assert raw.exists()")])
adapted(S/'apply_g13_crop_integration.py',S/'apply_g01_crop_integration.py',[("Append Lavender without rewriting the preceding 27 crop bindings.","Append two referenced berry crops; preserve the 28 accepted crops."),("scope='G02-G01: 28 crop profiles and seed-store products; accepted prefab revisions'","scope='G02-G13 and G01 Blueberry/Blackberry: 30 crop profiles; Raspberry pending reference'"),("Prior 27 crop balances retained; Lavender use existing 5-day/5-G seed/12-G harvest integration defaults, nonfood.","Prior 28 crop balances retained; two berries inherit existing edible crop defaults: 5 days, seed 5 G, harvest 12 G."),("addedCrops=1,totalCrops=28,seedShopProducts=28,totalUiSprites=84","addedCrops=2,totalCrops=30,seedShopProducts=30,totalUiSprites=90")])
art=SOURCE.parent/'UIIcons/Seeds'
adapted(art/'extend_g13_seed_packets_gimp.py',art/'extend_g01_seed_packets_gimp.py',[(";assert w==h",""),("2016,1152","2016,1440"),("G06-G01","G01/G06-G13"),("newPackets=1,newProduceSprites=1,newMutationSprites=1,seedMastersReopened=1,produceMastersReopened=1","newPackets=len(NEW),newProduceSprites=len(NEW),newMutationSprites=len(NEW),seedMastersReopened=len(NEW),produceMastersReopened=len(NEW)"),("28 packets, one new editable source pair","30 packets, two new editable source pairs")])
print('G01_EXPORT_AND_ICON_WORKFLOWS_READY',flush=True)
