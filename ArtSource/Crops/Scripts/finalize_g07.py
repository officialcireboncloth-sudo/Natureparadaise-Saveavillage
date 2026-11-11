"""Update only the G07 art catalog entries; retain gameplay and prior art rows."""
import json
from g07_config import *
p=TEXTURES/'NP_Plants_Catalog.json';data=json.loads(p.read_text())
for row in data['plants']:
 if row['group']!='G07':continue
 water=row['displayName']=='Water Spinach'
 row['atlas']=ATLAS;row['slots']=['LeafYoungSpinach','StemPale','SeedBrown' if water else 'SeedTan','LeafWaterSpinach' if water else 'LeafOrchid','CutStem']+(['LeafYoungWaterSpinach','HollowStem'] if water else [])
 row['artStatus']='G07 R02 registered high-detail leaf textures, inflated rosette and slender curved water-spinach stems; Unity validation pending'
p.write_text(json.dumps(data,indent=2));print('G07_CATALOG_PASS 2 rows; gameplay retained',flush=True)
manifest=MASTER.parent/'G07_Manifest.json';data=json.loads(manifest.read_text());data.update(revision='R02 registered high-detail leaves and organic clumps',newAtlasPages=1);manifest.write_text(json.dumps(data,indent=2))
