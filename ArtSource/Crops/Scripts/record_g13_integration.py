"""Prove the Lavender extension against the accepted 27 crops without resetting any baseline."""
import re,csv
import g13_config as g
from g13_integration_config import *
from unified_plant_atlas import write,sha
base=load(g.REVIEW/'Baseline.json');manifest=load(OUT/'Manifest.json');assert len(manifest['plants'])==28
for old in base['manifestBefore']['plants']:assert old==next(r for r in manifest['plants'] if r['key']==old['key'])
for name,key in [('Items','item_id'),('Crops','crop_id')]:
 with (g.REVIEW/(name+'_Before.csv')).open(newline='',encoding='utf-8-sig') as f:old={r[key]:r for r in csv.DictReader(f)}
 _,rows=csvread(A/f'Data/Balance/{name}.csv');now={r[key]:r for r in rows}
 assert all(now[k]==r for k,r in old.items()),name+' earlier balance rows changed'
 assert len(now)-len(old)==(3 if name=='Items' else 1)
editor=A/'Script/Editor/CropGameplayValidation.cs';expected=base['editorValidationBefore'].replace('"Orchid" };','"Orchid", "Lavender" };');assert editor.read_text()==expected
catalogPath=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';catalog=load(catalogPath)
for row,old in zip(catalog['plants'],base['catalogBefore']['plants']):
 if row['group']!='G13':assert row==old,row['displayName']
 else:
  row.update(authoringAtlas=g.ATLAS,slots=[s[0] for s in g.SURFACES],artStatus='G13 reference model and spatial leaves complete',gameplayStatus='APPLIED')
write(catalogPath,catalog)
shop=(A/'Resources/Shops/Crops Catalog.asset').read_text();oldshop=base['shopBefore'];assert shop.split('  - title: Pupuk',1)[1]==oldshop.split('  - title: Pupuk',1)[1]
changes={name:dict(before=h,after=sha(ROOT/name)) for name,h in base['mutableBefore'].items() if sha(ROOT/name)!=h}
write(OUT/'G13_BindingChanges.json',dict(status='APPLIED_SCOPED_EXTENSION',group='G13',newPlantKeys=['lavender'],authorizedChanges=changes,baseline=rel(g.REVIEW/'Baseline.json'),manifestBeforeSha256=__import__('hashlib').sha256(json.dumps(base['manifestBefore'],sort_keys=True).encode()).hexdigest(),priorPlantsRetained=27,priorCsvRowsRetained=True,editorChanges='Append Lavender to the Editor fixture list only',catalogAllowedGroups=['G13']))
print('G13_INTEGRATION_PRESERVATION_PASS',len(changes),'scoped game-data changes; 27 prior crops unchanged',flush=True)
