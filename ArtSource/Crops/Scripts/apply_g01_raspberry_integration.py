"""Append Raspberry via the established crop/item/store bindings and preserve prior rows."""
import sys,csv,io
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from g01_raspberry_integration_config import *
from unified_plant_atlas import write,sha,migration_report,REGISTRY
R=ROOT/'ArtSource/Crops/G01/Review/Raspberry';before=load(R/'Baseline.json')
if not BASELINE.exists():write(BASELINE,dict(protected=before['protected'],plantsBefore=30,manifestBefore=before['manifestBefore'],sourceCheckpoint=rel(R/'Baseline.json')))
source=Path(__file__).with_name('apply_g01_crop_integration.py')
t=source.read_text().replace('from g01_integration_config import *','from g01_raspberry_integration_config import *').replace('G01_CsvEditPlan.json','G01_Raspberry_CsvEditPlan.json').replace('G02-G13 and G01 Blueberry/Blackberry: 30 crop profiles; Raspberry pending reference','G01-G13: all 31 referenced crops including Blueberry, Raspberry and Blackberry').replace('Prior 28 crop balances retained; two berries inherit existing edible crop defaults: 5 days, seed 5 G, harvest 12 G.','Prior 30 crop balances retained; Raspberry uses the existing edible crop defaults: 5 days, seed 5 G, harvest 12 G.').replace('addedCrops=2,totalCrops=30,seedShopProducts=30,totalUiSprites=90','addedCrops=1,totalCrops=31,seedShopProducts=31,totalUiSprites=93')
exec(compile(t,str(source),'exec'),dict(__file__=str(source),__name__='__main__'))
# These code-generated balance files have no formulas. Append only newly authored rows,
# retaining the exact existing bytes, header order and every prior data value.
for plan in load(OUT/'G01_Raspberry_CsvEditPlan.json'):
 p=ROOT/plan['path'];raw=p.read_bytes();text=raw.decode('utf-8-sig');reader=csv.reader(io.StringIO(text));header=next(reader);old=list(reader);assert header==plan['header'];assert plan['rows'][:len(old)]==old
 output=io.StringIO(newline='');writer=csv.writer(output,lineterminator='\r\n');writer.writerows(plan['rows'][len(old):]);addition=output.getvalue().encode('utf-8');p.write_bytes(raw+(b'' if raw.endswith(b'\n') else b'\r\n')+addition)
editor=A/'Script/Editor/CropGameplayValidation.cs';t=editor.read_text();old='"Blueberry", "Blackberry" };';new='"Blueberry", "Blackberry", "Raspberry" };'
assert old in t or new in t
if old in t:editor.write_text(t.replace(old,new))
manifest=load(OUT/'Manifest.json');manifest.update(pendingReference=[],extensionBaseline=rel(BASELINE));manifest['runtimeAtlas']['registrySha256']=sha(REGISTRY);write(OUT/'Manifest.json',manifest)
catalogPath=A/'Textures/Shared/Plants/NP_Plants_Catalog.json';catalog=load(catalogPath)
for row in catalog['plants']:
 if row['displayName']=='Raspberry':row.update(authoringAtlas='NP_Plants_Raspberry_Source_01',slots=['RaspberryLeaf','RaspberryDrupe','BarkRed','FlowerIvory','BerryGreen','SeedTan','PollenGold','StemGreen'],artStatus='G01 supplied Raspberry reference complete',gameplayStatus='APPLIED')
write(catalogPath,catalog)
changes={name:dict(before=h,after=sha(ROOT/name)) for name,h in before['mutableBefore'].items() if sha(ROOT/name)!=h}
write(OUT/'G01_Raspberry_BindingChanges.json',dict(status='APPLIED_SCOPED_EXTENSION',group='G01',newPlantKeys=['raspberry'],authorizedChanges=changes,baseline=rel(R/'Baseline.json'),priorPlantsRetained=30,priorCsvRowsRetained=True,editorChanges='Append Raspberry to the species fixture list only',catalogAllowedGroups=['G01'],pendingReference=[]))
migration_report();print('RASPBERRY_INTEGRATION_PASS: 31 crops, 93 sprites; prior 30 retained',flush=True)
