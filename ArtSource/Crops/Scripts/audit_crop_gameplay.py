"""Cross-file audit of applied crop art, seed shop, CSV identity and exported sprites."""
from pathlib import Path
import sys,json,re,csv,hashlib,math
ROOT=Path(__file__).resolve().parents[3];A=ROOT/'Assets/Nature  Paradaise';OUT=ROOT/'ArtSource/Crops/Integration'
if __name__=='__main__' and (OUT/'G06_G09_Baseline.json').is_file() and len(json.loads((OUT/'Manifest.json').read_text())['plants'])>12:
    import subprocess
    result=subprocess.run([r'C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe','-b','--threads','4','--python',str(Path(__file__).with_name('audit_crop_extension.py'))],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,encoding='utf-8',errors='replace')
    (OUT/'G06_G09_Audit.log').write_text(result.stdout,encoding='utf-8')
    passed='CROP_EXTENSION_AUDIT_PASS' in result.stdout and 'Traceback' not in result.stdout
    print('\n'.join(line for line in result.stdout.splitlines() if 'CROP_EXTENSION_AUDIT_PASS' in line) if passed else result.stdout)
    raise SystemExit(0 if passed else 1)
from PIL import Image
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def text(p):return p.read_text(encoding='utf-8-sig')
def guid(p):return re.search(r'^guid: (\w+)',text(Path(str(p)+'.meta')),re.M)[1]
def values(t,key):return re.findall(r'(?m)^  '+re.escape(key)+r': ([^\r\n]*)',t)
def refs(t,key):return [re.search(r'guid: (\w+)',v)[1] for v in values(t,key) if 'guid:' in v]
checks=[]
def check(ok,label):
    assert ok,label
    checks.append(label)
def rows(p,key):
    with p.open(encoding='utf-8-sig',newline='') as f:
        reader=csv.DictReader(f);data=list(reader)
        check(all(None not in r and all(v is not None for v in r.values()) for r in data),'Rectangular CSV '+p.name)
        check(len({r[key] for r in data})==len(data),'Unique IDs '+p.name)
        return {r[key]:r for r in data}
m=load(OUT/'Manifest.json');before=load(OUT/'BeforeApply.json')
revision_path=ROOT/'ArtSource/Crops/G04/Review/R02_AssetChanges.json'
revision=load(revision_path) if revision_path.exists() else {}
revised=revision.get('authorizedChanges',{})
framing_path=OUT/'OutdoorFraming.json'
framing=load(framing_path) if framing_path.exists() else {}
framing_changes=framing.get('authorizedChanges',{})
phase_pattern=re.compile(r'^Assets/Nature  Paradaise/(?:mesh/Crops/G04/NP_(?:GreenBean|ChiliPepper|Eggplant)_(?:Young|Flowering|Ready)(?:_LOD1)?\.(?:json|fbx|asset)|Prefabs/Crops/G04/NP_(?:GreenBean|ChiliPepper|Eggplant)_(?:Young|Flowering|Ready)\.prefab)$')
for name,h in before['protected'].items():
    change=revised.get(name)
    framing_change=framing_changes.get(name)
    if framing_change:check(name=='Assets/Nature  Paradaise/Map/Scenes/World/Map.unity' and framing.get('status')=='PASS' and framing_change['before']==h and digest(ROOT/name)==framing_change['after'],'Explicit outdoor field/camera revision '+name)
    elif change:check(bool(phase_pattern.fullmatch(name)) and change['before']==h and digest(ROOT/name)==change['after'],'Explicit G04 volume revision '+name)
    else:check(digest(ROOT/name)==h,'Existing art/scene retained '+name)
if framing:
    check(all(digest(ROOT/p)==h for p,h in framing['sourceHashes'].items()),'Outdoor framing report matches current source')
    check(all(digest(ROOT/p)==h for p,h in framing['interiorsUnchanged'].items()),'Interior scenes retained during outdoor revision')
items=rows(A/'Data/Balance/Items.csv','item_id');crops=rows(A/'Data/Balance/Crops.csv','crop_id')
for name,key,current in [('Items.csv','item_id',items),('Crops.csv','crop_id',crops)]:
    old=rows(OUT/'Before'/'Assets/Nature  Paradaise/Data/Balance'/name,key)
    for k,row in old.items():
        unchanged={f:v for f,v in row.items() if f!='display_name'}
        check(all(current[k].get(f)==v for f,v in unchanged.items()),'Existing CSV balance retained '+k)
check(len(m['plants'])==12,'Exactly twelve integrated crops')
shop=text(ROOT/m['seedShop']);oldshop=text(OUT/'Before'/m['seedShop'])
seed_section=shop.split('  - title: Benih\n',1)[1].split('    animals:',1)[0]
seeds=re.findall(r'guid: (\w+)',seed_section)
check(len(seeds)==12 and len(set(seeds))==12,'Twelve unique seeds in Benih')
check(shop[shop.index('  - title: Pupuk'):]==oldshop[oldshop.index('  - title: Pupuk'):],'Non-seed tabs and tool price overrides retained')
store=A/'Prefabs/Shops/Testing/01_Crops_Shop.prefab'
check(m['seedShopGuid'] in text(store),'Existing shop prefab references updated catalog')
check(guid(store) in text(A/'Map/Scenes/World/Map.unity'),'Existing Map uses that shop prefab')
all_ids=[]
for p in (A/'Resources').rglob('*.asset'):
    ids=values(text(p),'itemId')
    if ids:all_ids.extend(ids)
check(len(all_ids)==len(set(all_ids)),'No duplicate item IDs across all Resources')
icon_count=0
for r in m['plants']:
    c,s,p,mut=[text(ROOT/r[k]) for k in ['profile','seed','produce','mutation']]
    check(values(c,'cropId')==[r['cropId']] and values(s,'itemId')==[r['seedId']] and values(p,'itemId')==[r['produceId']],'Stable identities '+r['species'])
    check(refs(c,'seedItem')==[r['seedGuid']] and refs(s,'seedCrop')==[r['profileGuid']],'Seed/crop reverse binding '+r['species'])
    check(refs(c,'produceItem')==[r['produceGuid']] and refs(c,'mutationProduceItem')==[r['mutationGuid']],'Harvest item binding '+r['species'])
    check(refs(mut,'mutationSourceItem')==[r['produceGuid']] and values(mut,'isMutatedCrop')==['1'],'Mutation binding '+r['species'])
    check(r['seedGuid'] in seeds,'Seed sold '+r['species'])
    check(items[r['seedId']]['seed_crop_id']==r['cropId'] and crops[r['cropId']]['seed_item_id']==r['seedId'] and crops[r['cropId']]['produce_item_id']==r['produceId'],'CSV identity joins '+r['species'])
    check(int(values(c,'daysUntilFirstHarvest')[0])==int(crops[r['cropId']]['days_until_first_harvest']) and int(values(s,'buyPrice')[0])==int(items[r['seedId']]['buy_price']) and int(values(p,'sellPrice')[0])==int(items[r['produceId']]['sell_price']),'CSV economy and growth match applied assets '+r['species'])
    check(refs(p,'worldPrefab')==[r['harvestGuid']] and refs(mut,'worldPrefab')==[r['harvestGuid']],'Detached harvest model '+r['species'])
    stage_guids=re.findall(r'    modelPrefab: \{fileID: 100, guid: (\w+), type: 3\}',c)
    check(stage_guids==[v['guid'] for v in r['stages']],'All reference phase prefab links '+r['species'])
    check(stage_guids[-1]!=r['harvestGuid'] and '_Ready.prefab' in r['stages'][-1]['prefab'],'Final field phase is a ready plant '+r['species'])
    check(len(stage_guids)==(6 if r['key']=='cabbage' else 5),'Legacy-compatible stage count '+r['species'])
    for phase in r['stages']:
        spec=load(ROOT/phase['prefab'].replace('/Prefabs/','/mesh/').replace('.prefab','.json'))
        for axis in [0,2]:
            lo=(spec['min'][axis]+phase['offset'][axis])*phase['scale'];hi=(spec['max'][axis]+phase['offset'][axis])*phase['scale']
            check(lo>=-.42501 and hi<=.42501,'Field footprint '+r['species']+' '+str(phase['index'])+' '+str(axis))
        check((spec['min'][1]+phase['offset'][1])*phase['scale']>=-1e-5,'Ground contact '+r['species']+' '+str(phase['index']))
    for key,icon_name in [('seed','icon_seed_'+r['key']),('produce','icon_crop_'+r['key']),('mutation','icon_crop_'+r['key']+'_mutation')]:
        icon=A/'Textures/UI/Icons'/('Seeds' if key=='seed' else 'Crops')/(icon_name+'.png')
        image=Image.open(icon);metadata=text(Path(str(icon)+'.meta'));asset=text(ROOT/r[key])
        check(image.size==(256,256) and image.mode=='RGBA','256px RGBA '+icon_name)
        alpha=image.getchannel('A');bounds=alpha.getbbox()
        check(bounds is not None and alpha.getextrema()==(0,255) and bounds[0]>0 and bounds[1]>0 and bounds[2]<256 and bounds[3]<256,'Transparent safe margin '+icon_name)
        check('textureType: 8' in metadata and 'spriteMode: 1' in metadata and 'enableMipMap: 0' in metadata and 'alphaIsTransparency: 1' in metadata,'Sprite importer '+icon_name)
        check(all(v=='256' for v in re.findall(r'maxTextureSize: (\d+)',metadata)),'Platform import size '+icon_name)
        check(refs(asset,'icon')==[guid(icon)] and refs(asset,'inventoryIllustration')==[guid(icon)],'Icon resolves in item/shop '+icon_name)
        icon_count+=1
view=text(A/'Script/World/Field/FieldCropView.cs')
check('stagePrefab != null ? Color.white' in view and 'propertyBlock.SetColor("_BaseColor", cropColor)' in view,'Authored atlas colour is retained and URP lifecycle tint supported')
compiled=load(OUT/'Compilation.json')
check(compiled['status']=='PASS' and len(compiled['assemblies'])==2,'Runtime and Editor source compiled')
check(all(digest(ROOT/p)==h for p,h in compiled['sourceHashes'].items()),'Compiler report matches current changed source')
gimp=load(ROOT/'ArtSource/UIIcons/Seeds/GimpSourceAudit.json')
check(gimp['status']=='PASS' and gimp['seedMastersReopenedDuringExport']==12 and len(gimp['produceMastersReopened'])==12,'Twenty-four editable GIMP masters reopened')
for record in gimp['produceMastersReopened']:check(digest(ROOT/record['source'])==record['sha256'],'Current GIMP source '+record['plant'])
if revision:
    volume=load(ROOT/'ArtSource/Crops/G04/Review/R02_VolumeAudit.json')
    check(revision['status']=='PASS' and volume['status']=='PASS' and len(volume['updatedModels'])==9,'Nine explicit G04 phase crowns revised')
    check(all(row['depthWidthRatio']>.6 and row['crownPlanarRatio']>.48 for row in volume['after'].values()),'G04 crown depth and two-dimensional footprint verified')
for name,h in before['before'].items():
    now=digest(ROOT/name)
    if now!=h:m['authorizedChanges'][name]=dict(before=h,after=now)
for name,change in framing_changes.items():m['authorizedChanges'][name]=change
(OUT/'Manifest.json').write_text(json.dumps(m,indent=2,ensure_ascii=False))
report=dict(status='PASS',checks=len(checks),crops=12,seedStoreProducts=12,icons=icon_count,stageBindings=sum(len(r['stages']) for r in m['plants']),protectedFiles=len(before['protected']),legacyCabbageIdsRetained=True,initialBalance='11 new crop rows inherit existing Cabbage defaults: All seasons, five days, seed price 5 G, base produce sale 12 G; editable through existing CSV import',unityPlayMode='BLOCKED_BY_LICENSING; not a runtime pass')
(OUT/'IntegrationAudit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
