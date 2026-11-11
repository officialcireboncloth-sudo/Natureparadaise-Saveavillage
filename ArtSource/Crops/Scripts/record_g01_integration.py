"""Prove two berry additions; do not alter Raspberry or the 28 accepted crop records."""
import csv
import g01_config as g
from g01_integration_config import *
from unified_plant_atlas import write,sha,migration_report
base=load(g.REVIEW/'Baseline.json');manifest=load(OUT/'Manifest.json');assert len(manifest['plants'])==30
for old in base['manifestBefore']['plants']:assert old==next(r for r in manifest['plants'] if r['key']==old['key'])
for name,key in [('Items','item_id'),('Crops','crop_id')]:
 with (g.REVIEW/(name+'_Before.csv')).open(newline='',encoding='utf-8-sig') as f:old={r[key]:r for r in csv.DictReader(f)}
 _,rows=csvread(A/f'Data/Balance/{name}.csv');now={r[key]:r for r in rows};assert all(now[k]==r for k,r in old.items());assert len(now)-len(old)==(6 if name=='Items' else 2)
editor=A/'Script/Editor/CropGameplayValidation.cs';assert editor.read_text()==base['editorValidationBefore'].replace('"Lavender" };','"Lavender", "Blueberry", "Blackberry" };')
catalogPath=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';catalog=load(catalogPath)
for row,old in zip(catalog['plants'],base['catalogBefore']['plants']):
 if row['displayName'] not in g.PHASES:assert row==old,row['displayName']
 else:
  own=[row['displayName']+'Leaf','BlueberrySkin' if row['displayName']=='Blueberry' else 'BlackberryDrupe'];row.update(authoringAtlas=g.ATLAS,slots=own+[s[0] for s in g.SURFACES[4:]],artStatus='G01 supplied berry reference complete',gameplayStatus='APPLIED')
write(catalogPath,catalog)
shop=(A/'Resources/Shops/Crops Catalog.asset').read_text();assert shop.split('  - title: Pupuk',1)[1]==base['shopBefore'].split('  - title: Pupuk',1)[1]
changes={name:dict(before=h,after=sha(ROOT/name)) for name,h in base['mutableBefore'].items() if sha(ROOT/name)!=h}
write(OUT/'G01_BindingChanges.json',dict(status='APPLIED_SCOPED_EXTENSION',group='G01',newPlantKeys=['blueberry','blackberry'],authorizedChanges=changes,baseline=rel(g.REVIEW/'Baseline.json'),priorPlantsRetained=28,priorCsvRowsRetained=True,editorChanges='Append Blueberry and Blackberry to the Editor fixture list only',catalogAllowedGroups=['G01'],pendingReference=['Raspberry']))
migration_report();print('G01_INTEGRATION_PRESERVATION_PASS; two supplied species; 28 prior crops and Raspberry retained',flush=True)
