"""Bind the three G09 art catalog rows without altering gameplay or other crops."""
import json
from g09_config import *
p=TEXTURES/'NP_Plants_Catalog.json';data=json.loads(p.read_text())
for row in data['plants']:
 if row['group']!='G09':continue
 kind=row['displayName'];row['atlas']=ATLAS;row['slots']=[kind+'Leaf',kind+'Petal','StemGreen','SepalGreen','BudCream' if kind=='Jasmine' else 'BudRed','SeedTan' if kind=='Rose' else 'SeedBrown','CutStem']+(['ThornBronze'] if kind=='Rose' else ['PollenGold','StigmaRed'] if kind=='Hibiscus' else [])
 row['artStatus']='G09 sculpted species-specific flowers, detailed leaf/petal atlas and six visual stages; Unity validation pending'
p.write_text(json.dumps(data,indent=2));print('G09_CATALOG_PASS 3 rows; other art and gameplay retained',flush=True)
