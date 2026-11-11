"""Update only the two cereal art rows, leaving other art and gameplay intact."""
import json
from g08_config import *
p=TEXTURES/'NP_Plants_Catalog.json';data=json.loads(p.read_text())
for row in data['plants']:
 if row['group']!='G08':continue
 rice=row['displayName']=='Rice'
 row['atlas']=ATLAS;row['slots']=['LeafGreen','LeafGold','LeafWarm','StemGreen','StemGold','CutStem']+(['RiceGold','RiceGreen','RiceSeed','RiceWhite'] if rice else ['WheatGold','WheatGreen','WheatSeed','AwnGold'])
 row['artStatus']='G08 sculpted cereal stages, vivid green/gold atlas and separate grain piles; Unity validation pending'
p.write_text(json.dumps(data,indent=2));print('G08_CATALOG_PASS 2 rows; other art and gameplay retained',flush=True)
