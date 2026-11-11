"""Prove the Orchid extension against the accepted 26 crops without resetting any baseline."""
import re,csv
import g12_config as g
from g12_integration_config import *
from unified_plant_atlas import write,sha
base=load(g.REVIEW/'Baseline.json');manifest=load(OUT/'Manifest.json');assert len(manifest['plants'])==27
for old in base['manifestBefore']['plants']:assert old==next(r for r in manifest['plants'] if r['key']==old['key'])
for name,key in [('Items','item_id'),('Crops','crop_id')]:
 with (g.REVIEW/(name+'_Before.csv')).open(newline='',encoding='utf-8-sig') as f:old={r[key]:r for r in csv.DictReader(f)}
 _,rows=csvread(A/f'Data/Balance/{name}.csv');now={r[key]:r for r in rows}
 assert all(now[k]==r for k,r in old.items()),name+' earlier balance rows changed'
 assert len(now)-len(old)==(3 if name=='Items' else 1)
editor=A/'Script/Editor/CropGameplayValidation.cs';expected=base['editorValidationBefore'].replace('"Tulip" };','"Tulip", "Orchid" };');assert editor.read_text()==expected
catalogPath=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';catalog=load(catalogPath)
for row,old in zip(catalog['plants'],base['catalogBefore']['plants']):
 if row['group']!='G12':assert row==old,row['displayName']
 else:
  row.update(authoringAtlas=g.ATLAS,slots=[s[0] for s in g.SURFACES],artStatus='G12 reference model and spatial leaves complete',gameplayStatus='APPLIED')
write(catalogPath,catalog)
shop=(A/'Resources/Shops/Crops Catalog.asset').read_text();oldshop=base['shopBefore'];assert shop.split('  - title: Pupuk',1)[1]==oldshop.split('  - title: Pupuk',1)[1]
changes={name:dict(before=h,after=sha(ROOT/name)) for name,h in base['mutableBefore'].items() if sha(ROOT/name)!=h}
write(OUT/'G12_BindingChanges.json',dict(status='APPLIED_SCOPED_EXTENSION',group='G12',newPlantKeys=['orchid'],authorizedChanges=changes,baseline=rel(g.REVIEW/'Baseline.json'),manifestBeforeSha256=__import__('hashlib').sha256(json.dumps(base['manifestBefore'],sort_keys=True).encode()).hexdigest(),priorPlantsRetained=26,priorCsvRowsRetained=True,editorChanges='Append Orchid to the Editor fixture list only',catalogAllowedGroups=['G12']))
print('G12_INTEGRATION_PRESERVATION_PASS',len(changes),'scoped game-data changes; 26 prior crops unchanged',flush=True)
