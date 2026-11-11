"""Publish only Tulip's prepared atlas mapping; preserve the other thirty art records."""
import json
from g11_config import *
p=TEXTURES/'NP_Plants_Catalog.json';data=json.loads(p.read_text());before=json.loads((REVIEW/'Baseline.json').read_text())['catalog']
for row,old in zip(data['plants'],before['plants']):
 if row['group']!='G11':assert row==old
 else:row.update(atlas=ATLAS,slots=[s[0] for s in SURFACES],artStatus='G11: six reference-matched bulb, leaf, bud, cup and cut-flower visuals with two-LOD Unity prefabs')
p.write_text(json.dumps(data,indent=2));print('G11_ART_CATALOG_READY',flush=True)
