"""Bind the two G14 flowers through existing assets, CSVs, and the Map seed store."""
import csv
import io
import re
from pathlib import Path
import uuid

from crop_extension_config import A, DEST, OUT, ROOT, csvread, guid, reference, rel, setval
from unified_plant_atlas import NAME, REGISTRY, SOURCE, TEXTURES, load, migration_report, sha, write
from unity_art_io import meta


GROUP = 'G14'
KINDS = ('Daisy', 'Moonflower')
PAGE = 'NP_Plants_DaisyMoonflower_Source_01'
SHOP = A / 'Resources/Shops/Crops Catalog.asset'
EDITOR = A / 'Script/Editor/CropGameplayValidation.cs'
ITEMS = A / 'Data/Balance/Items.csv'
CROPS = A / 'Data/Balance/Crops.csv'
BALANCE = 'Lavender defaults: all seasons, five growth days, 5 G seed, 12 G harvest, nonfood'


def save_asset(path, text, **fields):
    for key, value in fields.items():
        text = setval(text, key, value)
    path.write_text(text, encoding='utf-8', newline='\n')


def append_csv(path, key, additions):
    header, rows = csvread(path)
    existing = {row[key] for row in rows}
    new = [row for row in additions if row[key] not in existing]
    if new:
        raw = path.read_bytes()
        newline = '\r\n' if b'\r\n' in raw else '\n'
        buffer = io.StringIO(newline='')
        csv.DictWriter(buffer, header, lineterminator=newline).writerows(new)
        path.write_bytes(raw + (b'' if raw.endswith(b'\n') else newline.encode()) + buffer.getvalue().encode('utf-8'))


def paths(kind):
    return (A / f'Resources/Profiles/Farming/{kind} Crop.asset',
            A / f'Resources/Items/Farming/Seeds/{kind} Seed.asset',
            A / f'Resources/Items/Crops/{kind}.asset',
            A / f'Resources/Items/Crops/Mutations/{kind} Mutation.asset')


def sprite_reference(path, template):
    metadata = Path(str(path) + '.meta')
    exists = metadata.exists()
    asset_guid = meta(path, template)
    if not exists:
        text = metadata.read_text()
        sprite_id = uuid.uuid5(uuid.NAMESPACE_URL, rel(path)).hex
        metadata.write_text(re.sub(r'(?m)^(\s*spriteID:) \w+', lambda match: match[1]+' '+sprite_id, text))
    return reference(asset_guid, 21300000, 3)


def bind(kind, visuals, templates, item_rows, crop_row):
    key = kind.lower()
    crop, seed, produce, mutation = paths(kind)
    crop_id = f'crop.{key}'
    item_ids = [f'item.{key}_seed', f'item.{key}', f'item.{key}_mutation']
    labels = [f'Benih {kind}', kind, f'{kind} Mutasi']
    crop_guid, seed_guid, produce_guid, mutation_guid = [meta(p, main_id=11400000) for p in (crop,seed,produce,mutation)]
    stages = visuals[:5]
    specs = [load(A / f'mesh/Crops/{GROUP}/NP_{kind}_{stage["stage"]}.json') for stage in stages]
    scale = round(min(1.12, .85 / max(s['max'][axis] - s['min'][axis] for s in specs for axis in (0,2))), 6)
    profile = templates[0].split('  visualStages:', 1)[0] + '  visualStages:\n'
    records = []
    names = ['Seed', 'Sprout', 'Young Plant', 'Bud Forming', 'Harvest Ready']
    for index, (stage, spec, name) in enumerate(zip(stages, specs, names)):
        x,z = [round(-(spec['min'][axis]+spec['max'][axis])*.5*scale, 6) for axis in (0,2)]
        profile += (f'  - stageName: {name}\n    startsOnDay: {index}\n'
                    f'    modelPrefab: {reference(stage["guid"], 100, 3)}\n'
                    f'    scale: {{x: {scale}, y: {scale}, z: {scale}}}\n'
                    f'    localOffset: {{x: {x}, y: 0, z: {z}}}\n'
                    '    localEulerAngles: {x: 0, y: 0, z: 0}\n')
        records.append(dict(index=index,name=name,prefab=stage['prefab'].replace('\\','/'),guid=stage['guid'],
                            scale=scale,offset=[x,0,z]))
    save_asset(crop, profile, m_Name=kind+' Crop', cropId=crop_id, seedItem=reference(seed_guid),
               produceItem=reference(produce_guid), mutationProduceItem=reference(mutation_guid))
    harvest = visuals[5]
    world = reference(harvest['guid'], 100, 3)
    icons = [DEST/f'Seeds/icon_seed_{key}.png', DEST/f'Crops/icon_crop_{key}.png',
             DEST/f'Crops/icon_crop_{key}_mutation.png']
    icon_templates = [DEST/'Seeds/icon_seed_lavender.png', DEST/'Crops/icon_crop_lavender.png',
                      DEST/'Crops/icon_crop_lavender_mutation.png']
    asset_names = [kind+' Seed', kind, kind+' Mutation']
    extras = [dict(seedCrop=reference(crop_guid)), dict(worldPrefab=world),
              dict(worldPrefab=world, mutationSourceItem=reference(produce_guid))]
    csv_extras = [dict(seed_crop_id=crop_id), {}, dict(mutation_source_item_id=item_ids[1])]
    descriptions = [f'Benih {kind}. Tanam di petak yang sudah dicangkul dan rawat dengan menyiramnya.',
                    f'Hasil panen {kind}.', f'Hasil panen {kind}. Mutasi langka; harga mengikuti aturan mutasi.']
    items = []
    for i, path in enumerate((seed, produce, mutation)):
        assert icons[i].is_file(), icons[i]
        icon = sprite_reference(icons[i], icon_templates[i])
        save_asset(path, templates[i+1], m_Name=asset_names[i], itemId=item_ids[i], itemName=labels[i],
                   icon=icon, inventoryIllustration=icon, inventoryDescription=descriptions[i], **extras[i])
        items.append(dict(item_rows[i], item_id=item_ids[i], asset_path=rel(path), display_name=labels[i], **csv_extras[i]))
    crop_data = dict(crop_row, crop_id=crop_id, asset_path=rel(crop), seed_item_id=item_ids[0],
                     produce_item_id=item_ids[1], mutation_produce_item_id=item_ids[2])
    record = dict(group=GROUP,species=kind,key=key,label=kind,cropId=crop_id,seedId=item_ids[0],
                  produceId=item_ids[1],mutationId=item_ids[2],profile=rel(crop),profileGuid=crop_guid,
                  seed=rel(seed),seedGuid=seed_guid,produce=rel(produce),produceGuid=produce_guid,
                  mutation=rel(mutation),mutationGuid=mutation_guid,stages=records,
                  harvest=harvest['prefab'].replace('\\','/'),harvestGuid=harvest['guid'],footprintInCells=.85,
                  initialBalance=BALANCE)
    return record, items, crop_data


def main():
    visual_path = SOURCE / GROUP / 'Blender/UnityVisualBindings.json'
    visuals = load(visual_path)
    manifest_path = OUT / 'Manifest.json'
    manifest = load(manifest_path)
    old = [p for p in manifest['plants'] if p['group'] != GROUP]
    assert len(old) == 31, len(old)
    changes_path = OUT / f'{GROUP}_BindingChanges.json'
    old_changes = load(changes_path)['authorizedChanges'] if changes_path.exists() else {}
    mutable = [SHOP, ITEMS, CROPS, EDITOR]
    before = {rel(p): old_changes.get(rel(p), {}).get('before', sha(p)) for p in mutable}
    templates = [p.read_text(encoding='utf-8') for p in paths('Lavender')]
    items = {row['item_id']:row for row in csvread(ITEMS)[1]}
    crops = {row['crop_id']:row for row in csvread(CROPS)[1]}
    item_templates = [items[key] for key in ('item.lavender_seed','item.lavender','item.lavender_mutation')]
    records, add_items, add_crops = [], [], []
    for kind in KINDS:
        row, new_items, new_crop = bind(kind, visuals['plants'][kind], templates, item_templates, crops['crop.lavender'])
        records.append(row)
        add_items.extend(new_items)
        add_crops.append(new_crop)
    append_csv(ITEMS,'item_id',add_items)
    append_csv(CROPS,'crop_id',add_crops)
    shop = SHOP.read_bytes()
    newline = '\r\n' if b'\r\n' in shop else '\n'
    section = re.search(rb'(  - title: Benih\r?\n.*?    items:\r?\n)(.*?)(    animals: \[\])',shop,re.S)
    assert section
    seeds = [reference(r['seedGuid']) for r in records if r['seedGuid'].encode() not in section[2]]
    if seeds:
        additions = ''.join('    - '+ref+newline for ref in seeds).encode()
        SHOP.write_bytes(shop[:section.end(2)] + additions + shop[section.end(2):])
    code = EDITOR.read_bytes()
    if b'"Raspberry", "Daisy", "Moonflower" };' not in code:
        assert code.count(b'"Raspberry" };') == 1
        EDITOR.write_bytes(code.replace(b'"Raspberry" };',b'"Raspberry", "Daisy", "Moonflower" };')
                           .replace(b'kind == "Tulip")',b'kind == "Tulip" || kind == "Daisy" || kind == "Moonflower")'))
    manifest.update(plants=old+records, scope='G01-G14: 33 crop profiles and seed-store products',
                    runtimeValidation='ASSET_REFERENCES_CHECKED; UNITY_PLAY_MODE_PENDING', initialBalance=BALANCE,
                    unityEditorFixture='NOT_RUN_FOR_G14', extensionSummary=dict(addedCrops=2,totalCrops=33,
                    seedShopProducts=33,totalUiSprites=99,acceptedModelsPreserved=True,legacyWheatIngredientReused=True))
    manifest['runtimeAtlas']['registrySha256'] = sha(REGISTRY)
    write(manifest_path,manifest)
    visuals.update(status='Applied to crop profiles and the existing seed store',
                   gameplayIntegration=rel(manifest_path), profiles={r['species']:r['profile'] for r in records})
    write(visual_path,visuals)
    catalog_path = TEXTURES/'NP_Plants_Catalog.json'
    catalog = load(catalog_path)
    catalog['plants'] = [plant for plant in catalog['plants'] if plant['group'] != GROUP]
    for kind in KINDS:
        slots = [s['sourceSlot'] for s in load(REGISTRY)['slots'] if s['sourceAtlas']==PAGE
                 and (s['sourceSlot'].startswith(kind) or s['sourceSlot']=='FlowerStem')]
        catalog['plants'].append(dict(plantId='art.'+kind.lower(),displayName=kind,group=GROUP,atlas=NAME,
            slots=slots,artStatus='Reference phases and detached harvest complete',authoringAtlas=PAGE,
            runtimeMaterial=NAME,atlasPolicy='Append source surfaces to the shared registry',gameplayStatus='APPLIED'))
    catalog['missingReference'] = [kind for kind in catalog['missingReference'] if kind not in KINDS]
    write(catalog_path,catalog)
    changes = {rel(p):dict(before=before[rel(p)],after=sha(p)) for p in mutable if before[rel(p)]!=sha(p)}
    write(changes_path,dict(status='APPLIED_SCOPED_EXTENSION',group=GROUP,newPlantKeys=[r['key'] for r in records],
                           authorizedChanges=changes,priorPlantsRetained=31,priorCsvRowsRetained=True))
    migration_report()
    print('G14_GAMEPLAY_APPLIED: 33 crops including Daisy and Moonflower')


if __name__ == '__main__':
    main()
