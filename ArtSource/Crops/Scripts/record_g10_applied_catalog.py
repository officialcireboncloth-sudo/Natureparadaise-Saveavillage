"""Publish G10's integration state without moving atlas slots or other catalog entries."""
from g10_integration_config import *
p=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';data=load(p)
for row in data['plants']:
 if row['group']=='G10':row.update(artStatus='G10: six sculpted visual stages and two-LOD Unity prefabs; art/export checks passed',gameplayStatus='APPLIED')
p.write_text(json.dumps(data,indent=2))
p=ROOT/'ArtSource/Crops/G10/Blender/UnityVisualBindings.json';data=load(p);data.update(status='Applied to 25-crop gameplay and seed-store integration; Editor/Play Mode validation recorded separately',gameplayIntegration='ArtSource/Crops/Integration/Manifest.json',profiles={r['species']:r['profile'] for r in load(OUT/'Manifest.json')['plants'] if r['group']=='G10'});p.write_text(json.dumps(data,indent=2))
print('G10_APPLIED_CATALOG_PASS',flush=True)
