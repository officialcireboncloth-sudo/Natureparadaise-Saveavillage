"""Apply reviewed G02-G05 art to real crop/item/shop assets; keep existing IDs.

Writes a CSV edit plan for the accompanying artifact-tool editor. New balance rows
inherit the existing Cabbage defaults rather than inventing final crop balancing.
"""
from pathlib import Path
import sys,re,json,csv,hashlib,shutil,uuid
sys.path.insert(0,str(Path(__file__).resolve().parent))
from unity_art_io import meta

ROOT=Path(__file__).resolve().parents[3]
A=ROOT/'Assets/Nature  Paradaise'
OUT=ROOT/'ArtSource/Crops/Integration'
ICONS=A/'Textures/UI/Icons'
PLANTS=[('G02','Watermelon','watermelon','Semangka'),('G02','Melon','melon','Melon'),('G02','Zucchini','zucchini','Zucchini'),
        ('G03','Cucumber','cucumber','Timun'),('G03','Pea','pea','Ercis'),('G03','Kiwi','kiwi','Kiwi'),
        ('G04','GreenBean','green_bean','Buncis'),('G04','ChiliPepper','chili_pepper','Cabai'),('G04','Eggplant','eggplant','Terong'),
        ('G05','Cabbage','cabbage','Kubis'),('G05','Broccoli','broccoli','Brokoli'),('G05','Cauliflower','cauliflower','Kembang Kol')]

def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def rel(p):return str(p.relative_to(ROOT)).replace('\\','/')
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
def reference(g,file=11400000,kind=2):return '{fileID: '+str(file)+', guid: '+g+', type: '+str(kind)+'}'
def setval(t,k,v):
    pattern=r'(?m)^  '+re.escape(k)+r':[^\r\n]*'
    line='  '+k+': '+str(v)
    return re.sub(pattern,lambda _:line,t) if re.search(pattern,t) else t.rstrip()+'\n'+line+'\n'
def save(p,t,template=None):
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(t,encoding='utf-8',newline='\n')
    if template:meta(p,template=template)
    else:meta(p,main_id=11400000)
    return guid(p)
def csvread(p):
    with p.open(encoding='utf-8-sig',newline='') as f:
        reader=csv.DictReader(f);rows=list(reader);return reader.fieldnames,rows

OUT.mkdir(parents=True,exist_ok=True)
if (OUT/'Manifest.json').exists() and len(load(OUT/'Manifest.json')['plants'])>len(PLANTS):
    raise SystemExit('G02-G09 integration is already active. Use apply_crop_extension.py; the legacy 12-crop batch cannot replace the expanded store.')
snapshot=OUT/'BeforeApply.json'
if not snapshot.exists():
    paths=[A/'Resources/Profiles/Farming/Cabbage Crop.asset',A/'Resources/Items/Farming/Seed.asset',A/'Resources/Items/Animal/Cabbage.asset',
           A/'Resources/Items/Crops/Mutations/Cabbage Mutation.asset',A/'Resources/Shops/Crops Catalog.asset',
           A/'Data/Balance/Crops.csv',A/'Data/Balance/Items.csv',A/'Script/World/Field/FieldCropView.cs']
    protected={rel(p):digest(p) for folder in ['Prefabs/Crops','mesh/Crops','Textures/Shared/Plants','Material/Shared/Plants'] for p in (A/folder).rglob('*') if p.is_file()}
    protected[rel(A/'Map/Scenes/World/Map.unity')]=digest(A/'Map/Scenes/World/Map.unity')
    originals={}
    for p in paths:
        dest=OUT/'Before'/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
        originals[rel(p)]=digest(p)
    snapshot.write_text(json.dumps(dict(before=originals,protected=protected),indent=2))
before=load(snapshot)

seed_template=(OUT/'Before'/Path(rel(A/'Resources/Items/Farming/Seed.asset'))).read_text()
produce_template=(OUT/'Before'/Path(rel(A/'Resources/Items/Animal/Cabbage.asset'))).read_text()
mutation_template=(OUT/'Before'/Path(rel(A/'Resources/Items/Crops/Mutations/Cabbage Mutation.asset'))).read_text()
crop_template=(OUT/'Before'/Path(rel(A/'Resources/Profiles/Farming/Cabbage Crop.asset'))).read_text()
shop_path=A/'Resources/Shops/Crops Catalog.asset'
shop_text=(OUT/'Before'/Path(rel(shop_path))).read_text()
item_header,item_rows=csvread(A/'Data/Balance/Items.csv');crop_header,crop_rows=csvread(A/'Data/Balance/Crops.csv')
item_by_id={r['item_id']:r for r in item_rows};crop_by_id={r['crop_id']:r for r in crop_rows}
base_seed=dict(item_by_id['item.seed']);base_produce=dict(item_by_id['item.cabbage']);base_mutation=dict(item_by_id['item.cabbage_mutation']);base_crop=dict(crop_by_id['crop.cabbage'])

# Sprite importer settings keep the explicit 256px size on every platform.
sprite_template=next((ICONS/'Companions').glob('*.png.meta')).read_text()
icon_guids={}
for folder in ['Seeds','Crops']:
    for p in (ICONS/folder).glob('*.png'):
        m=Path(str(p)+'.meta');g=guid(p) if m.exists() else uuid.uuid4().hex
        t=re.sub(r'(?m)^guid: \w+','guid: '+g,sprite_template)
        t=re.sub(r'(?m)^(\s*maxTextureSize:) \d+',r'\g<1> 256',t)
        t=re.sub(r'(?m)^(\s*textureCompression:) \d+',r'\g<1> 0',t)
        t=re.sub(r'(?m)^(\s*spriteID:) \w+',lambda m:m[1]+' '+uuid.uuid5(uuid.NAMESPACE_URL,rel(p)).hex,t)
        m.write_text(t);icon_guids[p.stem]=g

records=[];new_csv_items=[];new_csv_crops=[]
for group,kind,key,label in PLANTS:
    cabbage=key=='cabbage';crop_id='crop.'+key
    crop_path=A/'Resources/Profiles/Farming'/(kind+' Crop.asset')
    seed_path=A/'Resources/Items/Farming/Seed.asset' if cabbage else A/'Resources/Items/Farming/Seeds'/(kind+' Seed.asset')
    produce_path=A/'Resources/Items/Animal/Cabbage.asset' if cabbage else A/'Resources/Items/Crops'/(kind+'.asset')
    mutant_path=A/'Resources/Items/Crops/Mutations'/(kind+' Mutation.asset')
    for p in [crop_path,seed_path,produce_path,mutant_path]:
        p.parent.mkdir(parents=True,exist_ok=True);meta(p,main_id=11400000)
    crop_guid,seed_guid,produce_guid,mutant_guid=[guid(p) for p in [crop_path,seed_path,produce_path,mutant_path]]
    seed_id='item.seed' if cabbage else 'item.'+key+'_seed'
    produce_id='item.cabbage' if cabbage else 'item.'+key
    mutant_id='item.'+key+'_mutation'
    bindings=load(ROOT/f'ArtSource/Crops/{group}/Blender/UnityVisualBindings.json')['plants'][kind]
    phases=bindings[:5];harvest=bindings[5]
    sizes=[];specs=[]
    for b in phases:
        json_path=ROOT/(b['prefab'].replace('\\','/').replace('/Prefabs/','/mesh/').replace('.prefab','.json'))
        spec=load(json_path);specs.append(spec)
        sizes.extend([spec['max'][0]-spec['min'][0],spec['max'][2]-spec['min'][2]])
    scale=round(.85/max(sizes),6)
    profile=crop_template if cabbage else crop_template[:crop_template.index('  visualStages:')]
    if cabbage:profile=profile[:profile.index('  visualStages:')]
    for k,v in dict(m_Name=kind+' Crop',cropId=crop_id,seedItem=reference(seed_guid),produceItem=reference(produce_guid),mutationProduceItem=reference(mutant_guid)).items():profile=setval(profile,k,v)
    profile+='  visualStages:\n'
    # Keep the legacy Cabbage six-slot schedule/save indices; mature and ready share the ready model.
    indices=[0,1,2,3,4,4] if cabbage else list(range(5))
    stage_records=[]
    for i,n in enumerate(indices):
        b,s=phases[n],specs[n]
        stage_name=['Seed','Sprout','Young',b['stage'],'Mature','Harvest Ready'][i] if cabbage else ['Seed','Sprout','Young',b['stage'],'Harvest Ready'][i]
        offset=[round(-(s['min'][0]+s['max'][0])/2,6),round(max(0,-s['min'][1]),6),round(-(s['min'][2]+s['max'][2])/2,6)]
        profile+=f'  - stageName: {stage_name}\n    startsOnDay: {i}\n    modelPrefab: {reference(b["guid"],b["fileID"],3)}\n    scale: {{x: {scale}, y: {scale}, z: {scale}}}\n    localOffset: {{x: {offset[0]}, y: {offset[1]}, z: {offset[2]}}}\n    localEulerAngles: {{x: 0, y: 0, z: 0}}\n'
        stage_records.append(dict(index=i,name=stage_name,prefab=b['prefab'].replace('\\','/'),guid=b['guid'],scale=scale,offset=offset))
    save(crop_path,profile)
    seed=seed_template
    description=f'Benih {label}. Tanam di petak yang sudah dicangkul dan rawat dengan menyiramnya.'
    fields=dict(m_Name='Seed' if cabbage else kind+' Seed',itemId=seed_id,itemName='Benih '+label,icon=reference(icon_guids['icon_seed_'+key],21300000,3),inventoryIllustration=reference(icon_guids['icon_seed_'+key],21300000,3),inventoryDescription=description,seedCrop=reference(crop_guid))
    for k,v in fields.items():seed=setval(seed,k,v)
    save(seed_path,seed)
    produce=produce_template;mutant=mutation_template
    world_ref=reference(harvest['guid'],harvest['fileID'],3)
    for k,v in dict(m_Name='Cabbage' if cabbage else kind,itemId=produce_id,itemName=label,icon=reference(icon_guids['icon_crop_'+key],21300000,3),inventoryIllustration=reference(icon_guids['icon_crop_'+key],21300000,3),inventoryDescription='Hasil panen '+label+'.',worldPrefab=world_ref).items():produce=setval(produce,k,v)
    save(produce_path,produce)
    for k,v in dict(m_Name=kind+' Mutation',itemId=mutant_id,itemName=label+' Mutasi',icon=reference(icon_guids['icon_crop_'+key+'_mutation'],21300000,3),inventoryIllustration=reference(icon_guids['icon_crop_'+key+'_mutation'],21300000,3),mutationSourceItem=reference(produce_guid),worldPrefab=world_ref).items():mutant=setval(mutant,k,v)
    save(mutant_path,mutant)
    for base,item_id,p,name,extra in [(base_seed,seed_id,seed_path,'Benih '+label,dict(seed_crop_id=crop_id)),(base_produce,produce_id,produce_path,label,{}),(base_mutation,mutant_id,mutant_path,label+' Mutasi',dict(mutation_source_item_id=produce_id))]:
        row=dict(base);row.update(item_id=item_id,asset_path=rel(p),display_name=name,**extra)
        if item_id in item_by_id:
            # Preserve existing economy/care fields; names and icon links are intentional art integration.
            item_by_id[item_id]['display_name']=name
        else:item_by_id[item_id]=row;item_rows.append(row);new_csv_items.append(item_id)
    if crop_id not in crop_by_id:
        row=dict(base_crop);row.update(crop_id=crop_id,asset_path=rel(crop_path),seed_item_id=seed_id,produce_item_id=produce_id,mutation_produce_item_id=mutant_id)
        crop_rows.append(row);crop_by_id[crop_id]=row;new_csv_crops.append(crop_id)
    records.append(dict(group=group,species=kind,key=key,label=label,cropId=crop_id,seedId=seed_id,produceId=produce_id,mutationId=mutant_id,profile=rel(crop_path),profileGuid=crop_guid,seed=rel(seed_path),seedGuid=seed_guid,produce=rel(produce_path),produceGuid=produce_guid,mutation=rel(mutant_path),mutationGuid=mutant_guid,stages=stage_records,harvest=harvest['prefab'].replace('\\','/'),harvestGuid=harvest['guid'],footprintInCells=.85,initialBalance='Existing Cabbage defaults' if not cabbage else 'Existing values retained'))

# Keep the old seed first and preserve all non-seed tabs and tool price overrides byte-for-byte.
seed_lines=''.join('    - '+reference(r['seedGuid'])+'\n' for r in sorted(records,key=lambda r:0 if r['key']=='cabbage' else 1))
shop_text=re.sub(r'(  - title: Benih\n.*?    items:\n).*?(    animals: \[\])',lambda m:m[1]+seed_lines+m[2],shop_text,count=1,flags=re.S)
save(shop_path,shop_text)

# New folders receive stable ordinary Unity metadata. Existing metadata is never regenerated.
for parent in [A/'Resources/Items/Farming/Seeds',A/'Resources/Items/Crops',ICONS/'Seeds',ICONS/'Crops']:
    p=parent
    while p!=A and p.is_relative_to(A):
        if not Path(str(p)+'.meta').exists():meta(p,folder=True)
        p=p.parent

plan=[]
for p,header,rows in [(A/'Data/Balance/Items.csv',item_header,item_rows),(A/'Data/Balance/Crops.csv',crop_header,crop_rows)]:
    plan.append(dict(path=rel(p),header=header,rows=[[r.get(k,'') for k in header] for r in rows]))
(OUT/'CsvEditPlan.json').write_text(json.dumps(plan,indent=2,ensure_ascii=False))
changes={}
for p,old_hash in before['before'].items():
    current=ROOT/p
    if p.endswith('.csv') or p.endswith('FieldCropView.cs'):continue
    if digest(current)!=old_hash:changes[p]=dict(before=old_hash,after=digest(current))
manifest=dict(status='APPLIED_ASSET_BINDINGS',scope='G02 G03 G04 G05 crop profiles, seed items, produce/mutations and existing seed-shop catalog',plants=records,authorizedChanges=changes,newCropRows=new_csv_crops,newItemRows=new_csv_items,seedShop=rel(shop_path),seedShopGuid=guid(shop_path),iconManifest='ArtSource/UIIcons/Seeds/Manifest.json',runtimeValidation='BLOCKED_BY_UNITY_LICENSING; file and compiler checks are separate')
(OUT/'Manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
print('APPLIED_CROP_BINDINGS',len(records),'CSV plan ready:',len(new_csv_crops),'new crops,',len(new_csv_items),'new items')
