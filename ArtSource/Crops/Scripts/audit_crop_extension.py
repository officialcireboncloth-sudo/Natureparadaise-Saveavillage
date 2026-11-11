"""Validate the live 21-crop chain and exact UI sprites; run with Blender Python."""
import sys,re,json,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from crop_extension_config import *
import bpy,numpy as np
checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)

def same_binding(old,current):
 if old==current:return True
 # Each visual revision records the exact before/after crop row. Follow those
 # explicit transforms across epochs; no other binding differences are accepted.
 records=[]
 for revision in ['Leaf_R03','Bush_R04','Leaf_R05']:
  proof=ROOT/('ArtSource/Crops/G01/Review/'+revision+'/BindingChanges.json')
  if proof.exists():
   record=load(proof).get('plants',{}).get(old['key'])
   if record:records.append(record)
 reachable=[old]
 for _ in records:
  for record in records:
   if record['before'] in reachable and record['after'] not in reachable:reachable.append(record['after'])
 return current in reachable
def val(t,k):return re.findall(r'(?m)^  '+re.escape(k)+r': ([^\r\n]*)',t)
def refs(t,k):return [re.search(r'guid: (\w+)',v)[1] for v in val(t,k) if 'guid:' in v]
def text(p):return p.read_text(encoding='utf-8-sig')
baseline=load(OUT/'G06_G09_Baseline.json');m=load(OUT/'Manifest.json');icons=load(ART/'G06_G09_IconAudit.json')
hasG10=any(r['group']=='G10' for r in m['plants']);hasG11=any(r['group']=='G11' for r in m['plants']);activeGroup='G11' if hasG11 else 'G10';expected=26 if hasG11 else 25 if hasG10 else 21
hasG12=any(r['group']=='G12' for r in m['plants'])
if hasG12:expected=27
hasG13=any(r['group']=='G13' for r in m['plants'])
if hasG13:expected=28
hasG01=any(r['group']=='G01' for r in m['plants'])
if hasG01:expected=28+len([r for r in m['plants'] if r['group']=='G01'])
g10=load(OUT/(activeGroup+'_Baseline.json')) if hasG10 else None
revisionPath=next((ROOT/('ArtSource/Crops/G11/Review/'+n) for n in ['R03_AssetChanges.json','R02_AssetChanges.json'] if (ROOT/('ArtSource/Crops/G11/Review/'+n)).is_file()),None);revision=load(revisionPath) if hasG11 and revisionPath else {};revisionChanges=revision.get('authorizedChanges',{})
unifiedPath=ROOT/'ArtSource/Crops/Unified/Review/AssetChanges.json';unified=load(unifiedPath) if unifiedPath.exists() else {};unifiedChanges=unified.get('authorizedChanges',{});unifiedBefore=load(unifiedPath.parent/'BeforeMerge.json') if unified else {}
def checkUnified(name,previous):
 change=unifiedChanges[name];prior=revisionChanges.get(name,{})
 check((change['before']==previous or (prior.get('before')==previous and prior.get('after')==change['before'])) and digest(ROOT/name)==change['after'],'Explicit shared atlas migration '+name)
if g10:
 for name,sha in g10['protected'].items():
  if name in unifiedChanges and not name.endswith('/NP_Plants_Catalog.json'):checkUnified(name,sha)
  elif name in revisionChanges:
   change=revisionChanges[name];check(change['before']==sha and digest(ROOT/name)==change['after'],'Explicit G11 art revision '+name)
  elif name.endswith('/NP_Plants_Catalog.json'):
   currentCatalog=unifiedBefore['catalogBefore'] if unified else load(ROOT/name);oldCatalog=g10['catalogBefore'];check(len(currentCatalog['plants'])==len(oldCatalog['plants'])==31,'Catalog species count retained')
   for now,old in zip(currentCatalog['plants'],oldCatalog['plants']):
    if now['group']==activeGroup:check({k:v for k,v in now.items() if k not in ['artStatus','gameplayStatus']}=={k:v for k,v in old.items() if k not in ['artStatus','gameplayStatus']} and now.get('gameplayStatus')=='APPLIED',activeGroup+' art identity and applied catalog status '+now['displayName'])
    else:check(now==old,'Prior art catalog record retained '+now['displayName'])
  else:check(digest(ROOT/name)==sha,'G10 retains '+name)
 for row in g10['manifestBefore']['plants']:check(row==next(r for r in m['plants'] if r['cropId']==row['cropId']),'G10 retains previous binding '+row['species'])
for name,sha in baseline['protected'].items():
 if hasG10 and name.endswith('/NP_Plants_Catalog.json'):
  beforeArt=load(ROOT/('ArtSource/Crops/'+activeGroup+'/Review/Baseline.json'))['catalog'];now=unifiedBefore['catalogBefore'] if unified else load(ROOT/name)
  for a,b in zip(beforeArt['plants'],now['plants']):
   check(a==b if a['group']!=activeGroup else all(a[k]==b[k] for k in ['plantId','displayName','group']),activeGroup+' catalog extension boundary '+a['displayName'])
 elif name in unifiedChanges:checkUnified(name,sha)
 else:check(digest(ROOT/name)==sha,'Retained '+name)
for name,old in baseline['reusedItemBefore'].items():
 p=ROOT/name;check(digest(Path(str(p)+'.meta'))==old['metaSha256'],'Reused item GUID metadata retained '+name)
 strip=lambda t:re.sub(r'(?m)^  (?:icon|inventoryIllustration|inventoryDescription|worldPrefab):[^\n]*\n?','',t).strip()
 check(strip(text(p))==strip(old['text']),'Only artwork references changed on reused item '+name)
for p,key in [(A/'Data/Balance/Items.csv','item_id'),(A/'Data/Balance/Crops.csv','crop_id')]:
 header,rows=csvread(p);check(len({r[key] for r in rows})==len(rows),'Unique CSV IDs '+p.name);check(all(None not in r and all(v is not None for v in r.values()) for r in rows),'Rectangular CSV '+p.name)
 current={r[key]:r for r in rows}
 for k,row in baseline['csvRows'][p.name].items():check(current.get(k)==row,'Existing balance row retained '+k)
 if g10:
  for k,row in g10['csvRows'][p.name].items():check(current.get(k)==row,'G10 retains prior balance '+k)
 if p.name=='Items.csv':items=current
 else:crops=current
check(len(m['plants'])==expected and len({r['cropId'] for r in m['plants']})==expected,str(expected)+' unique integrated plants')
shop=text(ROOT/m['seedShop']);oldshop=baseline['shopTextBefore'];section=shop.split('  - title: Benih\n',1)[1].split('    animals:',1)[0];sold=re.findall(r'guid: (\w+)',section)
check(len(sold)==len(set(sold))==expected,str(expected)+' distinct packets in Benih')
check(shop.split('  - title: Pupuk',1)[1]==oldshop.split('  - title: Pupuk',1)[1],'Other tabs and price overrides retained')
store=A/'Prefabs/Shops/Testing/01_Crops_Shop.prefab';check(m['seedShopGuid'] in text(store),'Map seed-store prefab uses the catalog');check(guid(store) in text(A/'Map/Scenes/World/Map.unity'),'Map uses the seed store')
all_ids=[v for p in (A/'Resources').rglob('*.asset') for v in val(text(p),'itemId')];check(len(set(all_ids))==len(all_ids),'Unique Resources item IDs')
phaseCount=0;iconReport=[]
for row in m['plants']:
 c,s,p,mut=[text(ROOT/row[k]) for k in ['profile','seed','produce','mutation']];kind=row['species'];key=row['key']
 check(val(c,'cropId')==[row['cropId']] and val(s,'itemId')==[row['seedId']] and val(p,'itemId')==[row['produceId']],'Stable crop/item IDs '+kind)
 check(refs(c,'seedItem')==[row['seedGuid']] and refs(s,'seedCrop')==[row['profileGuid']],'Reversible seed/crop binding '+kind)
 check(refs(c,'produceItem')==[row['produceGuid']] and refs(c,'mutationProduceItem')==[row['mutationGuid']],'Harvest items '+kind)
 check(refs(mut,'mutationSourceItem')==[row['produceGuid']] and val(mut,'isMutatedCrop')==['1'],'Mutation source '+kind)
 check(row['seedGuid'] in sold,'Sold packet '+kind)
 check(crops[row['cropId']]['seed_item_id']==row['seedId'] and items[row['seedId']]['seed_crop_id']==row['cropId'],'Balance IDs resolve '+kind)
 check(items[row['produceId']]['asset_path']==row['produce'] and int(val(p,'sellPrice')[0])==int(items[row['produceId']]['sell_price']),'Existing harvest identity and price resolve '+kind)
 check(int(val(c,'daysUntilFirstHarvest')[0])==int(crops[row['cropId']]['days_until_first_harvest']) and int(val(s,'buyPrice')[0])==int(items[row['seedId']]['buy_price']),'Applied growth and seed prices '+kind)
 if key in FLOWERS or row['group'] in ['G10','G11','G12','G13']:check(val(p,'isEdible')==val(mut,'isEdible')==['0'] and items[row['produceId']]['is_edible']=='FALSE','Flower produce is nonfood '+kind)
 check(refs(p,'worldPrefab')==refs(mut,'worldPrefab')==[row['harvestGuid']],'Detached harvest model '+kind)
 stageGuids=re.findall(r'    modelPrefab: \{fileID: 100, guid: (\w+), type: 3\}',c)
 check(stageGuids==[v['guid'] for v in row['stages']] and len(stageGuids)==(6 if key=='cabbage' else 5),'Ordered accepted phases '+kind)
 check(row['stages'][-1]['prefab'].endswith('_Ready.prefab') and row['harvest'].endswith('_Harvest.prefab'),'Ready stays planted; harvest is detached '+kind)
 for phase in row['stages']:
  prefab=ROOT/phase['prefab'];check(guid(prefab)==phase['guid'],'Stage prefab GUID '+kind+' '+str(phase['index']))
  check('m_LODs:' in text(prefab),'Two-LOD prefab '+kind+' '+str(phase['index']))
  spec=load(ROOT/phase['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json'))
  for axis in [0,2]:
   lo=(spec['min'][axis]+phase['offset'][axis])*phase['scale'];hi=(spec['max'][axis]+phase['offset'][axis])*phase['scale'];check(lo>=-.42501 and hi<=.42501,'Fits 85 percent of tile '+kind+' '+str(phase['index'])+' '+str(axis))
  check((spec['min'][1]+phase['offset'][1])*phase['scale']>=-1e-5,'Base pivot grounded before planting depth '+kind+' '+str(phase['index']));phaseCount+=1
 for field,folder,filename in [('seed','Seeds','icon_seed_'+key),('produce','Crops','icon_crop_'+key),('mutation','Crops','icon_crop_'+key+'_mutation')]:
  icon=DEST/folder/(filename+'.png');metadata=text(Path(str(icon)+'.meta'));asset={'seed':s,'produce':p,'mutation':mut}[field]
  check(refs(asset,'icon')==refs(asset,'inventoryIllustration')==[guid(icon)],'Shop/inventory sprite binding '+filename)
  check(all(v=='256' for v in re.findall(r'maxTextureSize: (\d+)',metadata)) and 'textureType: 8' in metadata and 'spriteMode: 1' in metadata and 'enableMipMap: 0' in metadata and 'alphaIsTransparency: 1' in metadata,'256px UI importer '+filename)
  image=bpy.data.images.load(str(icon),check_existing=False);check(tuple(image.size)==(256,256) and image.channels==4,'Actual RGBA PNG '+filename);pixels=np.empty(256*256*4,dtype=np.float32);image.pixels.foreach_get(pixels);alpha=pixels.reshape(256,256,4)[:,:,3];ys,xs=np.nonzero(alpha>.001)
  check(xs.size>0 and min(xs)>0 and min(ys)>0 and max(xs)<255 and max(ys)<255 and alpha.min()==0 and alpha.max()>.99,'Transparent safe margin '+filename);bpy.data.images.remove(image);iconReport.append(dict(file=rel(icon),sha256=digest(icon)))
check(icons['status']=='PASS' and icons['seedMastersReopened']==icons['produceMastersReopened']==9,'18 added GIMP masters reopened')
if hasG10:
 extra=load(ART/'G10_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==4,'Eight G10 GIMP masters reopened')
 check(len([r for r in m['plants'] if r['group']=='G10'])==4,'Four G10 species integrated')
 check(load(OUT/(activeGroup+'_CsvEditAudit.json'))['status']=='PASS',activeGroup+' CSV authoring and exact-cell verification')
if hasG11:
 extra=load(ART/'G11_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==1,'Two Tulip GIMP masters reopened')
 check(len([r for r in m['plants'] if r['group']=='G11'])==1,'One Tulip species integrated')
if hasG12:
 extra=load(ART/'G12_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==1,'Two Orchid GIMP masters reopened')
 check(len([r for r in m['plants'] if r['group']=='G12'])==1 and load(OUT/'G12_CsvEditAudit.json')['status']=='PASS','One Orchid species; CSV append verified')
 old=load(OUT/'G12_Baseline.json')
 for previous in old['manifestBefore']['plants']:check(previous==next(r for r in m['plants'] if r['key']==previous['key']),'Orchid retains previous crop '+previous['species'])
if hasG13:
 extra=load(ART/'G13_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==1,'Two Lavender GIMP masters reopened')
 check(len([r for r in m['plants'] if r['group']=='G13'])==1 and load(OUT/'G13_CsvEditAudit.json')['status']=='PASS','One Lavender species; CSV append verified')
 old=load(OUT/'G13_Baseline.json')
 for previous in old['manifestBefore']['plants']:check(previous==next(r for r in m['plants'] if r['key']==previous['key']),'Lavender retains previous crop '+previous['species'])
if hasG01:
 extra=load(ART/'G01_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==2,'Four berry GIMP masters reopened')
 berryCount=len([r for r in m['plants'] if r['group']=='G01'])
 check(berryCount in [2,3] and load(OUT/'G01_CsvEditAudit.json')['status']=='PASS','Supplied berry species; CSV append verified')
 if berryCount==3:
  extra=load(ART/'G01_Raspberry_IconAudit.json');check(extra['status']=='PASS' and extra['seedMastersReopened']==extra['produceMastersReopened']==1,'Raspberry GIMP source pair reopened')
  oldRaspberry=load(ROOT/'ArtSource/Crops/G01/Review/Raspberry/Baseline.json')
  for previous in oldRaspberry['manifestBefore']['plants']:check(same_binding(previous,next(r for r in m['plants'] if r['key']==previous['key'])),'Raspberry retains previous crop or exact authorized leaf art transforms '+previous['species'])
 old=load(OUT/'G01_Baseline.json')
 for previous in old['manifestBefore']['plants']:check(previous==next(r for r in m['plants'] if r['key']==previous['key']),'Berries retain previous crop '+previous['species'])
compiled=load(OUT/'Compilation.json');check(compiled['status']=='PASS','Runtime and Editor compile');check(all(digest(ROOT/p)==sha for p,sha in compiled['sourceHashes'].items()),'Compilation matches current source')
attempt=load(OUT/'UnityValidationAttempt.json') if (OUT/'UnityValidationAttempt.json').is_file() else {}
report=dict(status='PASS',crops=expected,seedStoreProducts=expected,icons=len(iconReport),stageBindings=phaseCount,checks=len(checks),protectedFiles=len(g10['protected']) if g10 else len(baseline['protected']),legacyCropsRetained=True,balance='Existing rows retained; new crops use integration defaults, legacy Wheat ingredient identity/price/stack retained, ornamental flowers nonfood',iconFiles=iconReport,unityEditorImport='NOT_RUN_FOR_G10' if hasG10 else attempt.get('status','PENDING_CURRENT_EDITOR_VALIDATION'),unityPlayMode='NOT_RUN',validationMethod='Asset/CSV/PNG/metadata references and cached compiler checks; separate from Editor/Play Mode')
(OUT/'IntegrationAudit.json').write_text(json.dumps(report,indent=2));print('CROP_EXTENSION_AUDIT_PASS',len(checks),'checks,',phaseCount,'stages,',len(iconReport),'sprites',flush=True)
