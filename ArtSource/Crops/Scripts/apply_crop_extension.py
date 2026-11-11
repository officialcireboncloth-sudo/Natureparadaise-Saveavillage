"""Append G06-G09 to live crop/item/shop assets without rewriting the first twelve."""
from crop_extension_config import *
from unity_art_io import meta
import uuid

baseline=load(OUT/'G06_G09_Baseline.json');manifest=load(OUT/'Manifest.json')
crop_template=(A/'Resources/Profiles/Farming/Cabbage Crop.asset').read_text();crop_template=crop_template[:crop_template.index('  visualStages:')]
seed_template=(A/'Resources/Items/Farming/Seed.asset').read_text()
produce_template=(A/'Resources/Items/Crops/Broccoli.asset').read_text()
mutant_template=(A/'Resources/Items/Crops/Mutations/Broccoli Mutation.asset').read_text()
item_header,item_rows=csvread(A/'Data/Balance/Items.csv');crop_header,crop_rows=csvread(A/'Data/Balance/Crops.csv')
items={r['item_id']:r for r in item_rows};crops={r['crop_id']:r for r in crop_rows}
base_seed=dict(items['item.seed']);base_produce=dict(items['item.broccoli']);base_mutant=dict(items['item.broccoli_mutation']);base_crop=dict(crops['crop.cabbage'])

def save(p,t):
 p.parent.mkdir(parents=True,exist_ok=True);p.write_text(t,encoding='utf-8',newline='\n');return meta(p,main_id=11400000)

def sprite(p):
 metadata=Path(str(p)+'.meta')
 if not metadata.exists():
  t=Path(str(DEST/'Seeds/icon_seed_cabbage.png')+'.meta').read_text();g=uuid.uuid4().hex
  t=re.sub(r'(?m)^guid: \w+','guid: '+g,t);t=re.sub(r'(?m)^(\s*spriteID:) \w+',lambda m:m[1]+' '+uuid.uuid5(uuid.NAMESPACE_URL,rel(p)).hex,t)
  metadata.write_text(t)
 return guid(p)

newRecords=[]
for group,kind,key,label in NEW:
 crop_path=A/'Resources/Profiles/Farming'/(kind+' Crop.asset');seed_path=A/'Resources/Items/Farming/Seeds'/(kind+' Seed.asset');produce_path=A/'Resources/Items/Materials/Wheat.asset' if key=='wheat' else A/'Resources/Items/Crops'/(kind+'.asset');mutant_path=A/'Resources/Items/Crops/Mutations'/(kind+' Mutation.asset')
 for p in [crop_path,seed_path,produce_path,mutant_path]:p.parent.mkdir(parents=True,exist_ok=True);meta(p,main_id=11400000)
 cg,sg,pg,mg=[guid(p) for p in [crop_path,seed_path,produce_path,mutant_path]]
 crop_id='crop.'+key;seed_id='item.'+key+'_seed';produce_id='item.'+key;mutant_id=produce_id+'_mutation'
 bindings=load(ROOT/f'ArtSource/Crops/{group}/Blender/UnityVisualBindings.json')['plants'][kind];phases=bindings[:5];harvest=next(b for b in bindings if b['stage']=='Harvest')
 specs=[load(ROOT/b['prefab'].replace('\\','/').replace('/Prefabs/','/mesh/').replace('.prefab','.json')) for b in phases]
 scale=round(.85/max(s['max'][axis]-s['min'][axis] for s in specs for axis in [0,2]),6)
 profile=crop_template
 for k,v in dict(m_Name=kind+' Crop',cropId=crop_id,seedItem=reference(sg),produceItem=reference(pg),mutationProduceItem=reference(mg)).items():profile=setval(profile,k,v)
 profile+='  visualStages:\n';stages=[]
 for i,(b,s) in enumerate(zip(phases,specs)):
  offset=[round(-(s['min'][0]+s['max'][0])/2,6),round(max(0,-s['min'][1]),6),round(-(s['min'][2]+s['max'][2])/2,6)]
  title={'Young':'Young Plant','Ready':'Harvest Ready','BudForming':'Bud Forming','RootForming':'Root Forming','BulbForming':'Bulb Forming','LeafGrowth':'Leaf Growth'}.get(b['stage'],b['stage'])
  profile+=f'  - stageName: {title}\n    startsOnDay: {i}\n    modelPrefab: {reference(b["guid"],b["fileID"],3)}\n    scale: {{x: {scale}, y: {scale}, z: {scale}}}\n    localOffset: {{x: {offset[0]}, y: {offset[1]}, z: {offset[2]}}}\n    localEulerAngles: {{x: 0, y: 0, z: 0}}\n'
  stages.append(dict(index=i,name=title,prefab=b['prefab'].replace('\\','/'),guid=b['guid'],scale=scale,offset=offset))
  depth=SOIL_BURIAL.get((kind,b['stage']),0)
  if depth:
   profile=profile.replace(f'  - stageName: {title}\n',f'  - stageName: {title}\n    soilBurialDepth: {depth}\n');stages[-1]['soilBurialDepth']=depth
 save(crop_path,profile)
 iconGuids={}
 for itemKind,folder,iconName in [('seed','Seeds','icon_seed_'+key),('produce','Crops','icon_crop_'+key),('mutation','Crops','icon_crop_'+key+'_mutation')]:
  p=DEST/folder/(iconName+'.png');assert p.is_file(),str(p);iconGuids[itemKind]=sprite(p)
 seed=seed_template
 for k,v in dict(m_Name=kind+' Seed',itemId=seed_id,itemName='Benih '+label,icon=reference(iconGuids['seed'],21300000,3),inventoryIllustration=reference(iconGuids['seed'],21300000,3),seedCrop=reference(cg),inventoryDescription='Benih '+label+'. Tanam di petak yang sudah dicangkul dan rawat dengan menyiramnya.').items():seed=setval(seed,k,v)
 save(seed_path,seed)
 world=reference(harvest['guid'],harvest['fileID'],3)
 for mutation,path,template,item_id,name in [(False,produce_path,produce_template,produce_id,label),(True,mutant_path,mutant_template,mutant_id,label+' Mutasi')]:
  if key=='wheat':template=baseline['reusedItemBefore'][rel(produce_path)]['text']
  t=template;ig=iconGuids['mutation' if mutation else 'produce']
  values=dict(m_Name=kind+(' Mutation' if mutation else ''),itemId=item_id,itemName=name,icon=reference(ig,21300000,3),inventoryIllustration=reference(ig,21300000,3),worldPrefab=world,inventoryDescription='Hasil panen '+label+('. Mutasi langka; harga mengikuti aturan mutasi.' if mutation else '.'))
  if mutation:values.update(mutationSourceItem=reference(pg),isMutatedCrop=1)
  if key=='wheat':
   if mutation:values.update(buyPrice=0,sellPrice=items[produce_id]['sell_price'])
   else:values['itemName']=re.search(r'(?m)^  itemName: (.*)$',template)[1]
  if key in FLOWERS:values.update(isEdible=0,foodPreparation=0,healthRestore=0,staminaRestore=0,hungerRestore=0,refrigeratorCategory=0)
  for k,v in values.items():t=setval(t,k,v)
  save(path,t)
 for base,item_id,path,name,extra in [(base_seed,seed_id,seed_path,'Benih '+label,dict(seed_crop_id=crop_id)),(base_produce,produce_id,produce_path,label,{}),(base_mutant,mutant_id,mutant_path,label+' Mutasi',dict(mutation_source_item_id=produce_id))]:
  if item_id in items and not (key=='wheat' and item_id==mutant_id and item_id not in baseline['csvRows']['Items.csv']):continue
  if key=='wheat' and item_id==mutant_id:base=dict(items[produce_id]);extra.update(buy_price='0',is_mutated_crop='TRUE')
  row=dict(base);row.update(item_id=item_id,asset_path=rel(path),display_name=name,**extra)
  if key in FLOWERS and item_id!=seed_id:row.update(is_edible='FALSE',food_preparation='NotFood',health_restore='0',stamina_restore='0',hunger_restore='0',refrigerator_category='None')
  if item_id in items:items[item_id].update(row)
  else:item_rows.append(row);items[item_id]=row
 if crop_id not in crops:
  row=dict(base_crop);row.update(crop_id=crop_id,asset_path=rel(crop_path),seed_item_id=seed_id,produce_item_id=produce_id,mutation_produce_item_id=mutant_id);crop_rows.append(row);crops[crop_id]=row
 newRecords.append(dict(group=group,species=kind,key=key,label=label,cropId=crop_id,seedId=seed_id,produceId=produce_id,mutationId=mutant_id,profile=rel(crop_path),profileGuid=cg,seed=rel(seed_path),seedGuid=sg,produce=rel(produce_path),produceGuid=pg,mutation=rel(mutant_path),mutationGuid=mg,stages=stages,harvest=harvest['prefab'].replace('\\','/'),harvestGuid=harvest['guid'],footprintInCells=.85,initialBalance='Existing Cabbage integration defaults; flowers are nonfood'))

shop_path=A/'Resources/Shops/Crops Catalog.asset';shop=shop_path.read_text();pattern=r'(  - title: Benih\n.*?    items:\n)(.*?)(    animals: \[\])';match=re.search(pattern,shop,re.S);assert match
section=match[2]
for r in newRecords:
 if r['seedGuid'] not in section:section+='    - '+reference(r['seedGuid'])+'\n'
shop=shop[:match.start(2)]+section+shop[match.end(2):];save(shop_path,shop)
for p,header,rows in [(A/'Data/Balance/Items.csv',item_header,item_rows),(A/'Data/Balance/Crops.csv',crop_header,crop_rows)]:
 with p.open('w',encoding='utf-8',newline='') as f:
  writer=csv.DictWriter(f,fieldnames=header,lineterminator='\n');writer.writeheader();writer.writerows(rows)
oldByKey={r['key']:r for r in manifest['plants']}
for r in newRecords:oldByKey[r['key']]=r
manifest.update(status='APPLIED_ASSET_BINDINGS',scope='G02-G09: 21 crop profiles and seed-store products; latest accepted prefab revisions',plants=list(oldByKey.values()),runtimeValidation='PENDING_CURRENT_EDITOR_VALIDATION',extensionBaseline=rel(OUT/'G06_G09_Baseline.json'),initialBalance='Prior 12 rows retained; nine added with Cabbage defaults. Flower produce is nonfood.')
(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
print('APPLIED_CROP_EXTENSION_PASS',len(newRecords),'new;',len(manifest['plants']),'total crops',flush=True)
