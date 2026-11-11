"""Mark Tulip's catalog and visual binding as applied without changing other species."""
from g11_integration_config import *
p=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';data=load(p)
for row in data['plants']:
 if row['group']=='G11':row.update(artStatus='G11: six reference-matched bulb, leaf, bud, cup and cut-flower visuals with two-LOD Unity prefabs',gameplayStatus='APPLIED')
p.write_text(json.dumps(data,indent=2))
p=ROOT/'ArtSource/Crops/G11/Blender/UnityVisualBindings.json';data=load(p);data.update(status='Applied to 26-crop gameplay and seed-store integration; Editor/Play Mode validation recorded separately',gameplayIntegration='ArtSource/Crops/Integration/Manifest.json',profiles={r['species']:r['profile'] for r in load(OUT/'Manifest.json')['plants'] if r['group']=='G11'});p.write_text(json.dumps(data,indent=2));print('G11_APPLIED_CATALOG_PASS',flush=True)
