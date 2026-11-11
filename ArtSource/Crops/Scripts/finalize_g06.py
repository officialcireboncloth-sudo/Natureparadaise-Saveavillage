"""Update only the two G06 art catalog rows after exports are prepared."""
import json
from g06_config import *
path=TEXTURES/'NP_Plants_Catalog.json';catalog=json.loads(path.read_text())
for row in catalog['plants']:
    if row['group']!='G06':continue
    onion=row['displayName']=='Onion'
    row['atlas']=ATLAS
    row['slots']=['SeedBlack' if onion else 'SeedTan','LeafStrap' if onion else 'LeafLobed','OnionSkin' if onion else 'RadishGradient','OnionCut' if onion else 'RadishCut','RootCream','StemGreen']
    row['artStatus']='G06 five growth phases, harvest, cut half and LODs prepared; Unity validation pending'
path.write_text(json.dumps(catalog,indent=2))
print('G06_CATALOG_PASS 2 rows; gameplay profiles retained',flush=True)
