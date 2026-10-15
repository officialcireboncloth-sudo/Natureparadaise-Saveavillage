from pathlib import Path
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
code=(ROOT/'ArtSource/ChickenStore_A/Scripts/verify_chicken_store.py').read_text()
code=code.replace('ChickenStore_A','ChickenStore_Mobile_A').replace('ChickenCoop_Companion_A','ChickenCoop_Mobile_A').replace('Coop_Optional_WeatherVane','Coop_Mobile_Optional_WeatherVane')
exec(compile(code,__file__,'exec'),{'__file__':__file__})
import json
mobile=json.loads((ART/'Previews/MeshManifest.json').read_text())
baseline=json.loads((ROOT/'ArtSource/ChickenStore_A/Previews/MeshManifest.json').read_text())
assert sum(x['triangles'] for x in mobile['assets'])+mobile['optionalOrnament']['triangles']<=6000
for new,old in zip(mobile['assets'],baseline['assets']):
 assert max(abs(a-b) for aa,bb in zip(new['bounds'],old['bounds']) for a,b in zip(aa,bb))<1e-4
 assert new['triangles']<old['triangles']
print('MOBILE_BUDGET_AND_ORIGINAL_BOUNDS_PASS')
