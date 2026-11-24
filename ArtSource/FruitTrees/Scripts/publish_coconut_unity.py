"""Publish the edited coast palm and bind the wild growth and pickup visuals."""

import json
import os
import re
import shutil
from pathlib import Path

from publish_apple_unity import ASSETS, MATERIALS, PROJECT, TEXTURES, TREE_DATA
from publish_apple_unity import blocks, meta, prefab, ref, serialize_mesh

SOURCE = Path(os.environ.get('NP_COCONUT_STAGE', PROJECT / 'tmp/Coconut'))
MODELS = ASSETS / 'mesh/Trees/Coconut'
PREFABS = ASSETS / 'Prefabs/Trees/Coconut'
STAGES = (
    ('Seedling', 1, 0, .26),
    ('Young', 3, 20, .52),
    ('Mature', 5, 60, 1),
    ('FruitForming', 6, 60, 1),
    ('HarvestReady', 7, 60, 1),
)
EXTRA_MODELS = ('Stump', 'Produce', 'Mutation')


def publish_material():
    material_path = MATERIALS / 'NP_Coconut.mat'
    material = (MATERIALS / 'NP_OilPalm_01.mat').read_text().replace('NP_OilPalm_01', 'NP_Coconut')
    palm_material = material
    for channel, property_name in [('BaseColor', '_BaseMap'), ('Normal', '_BumpMap'),
                                   ('SurfaceMask', '_MetallicGlossMap')]:
        file = TEXTURES / ('NP_Coconut_' + channel + '.png')
        shutil.copyfile(SOURCE / file.name, file)
        template = TEXTURES / ('NP_OilPalm_01_' + channel + '.png')
        new_guid = meta(file, template)
        old_guid = re.search(rf'{property_name}:\n\s*m_Texture: \{{fileID: 2800000, guid: (\w+)',
                             palm_material)[1]
        material = material.replace(old_guid, new_guid)
        if channel == 'BaseColor':
            importer = Path(str(file) + '.meta')
            settings = importer.read_text().replace('mipMapsPreserveCoverage: 0', 'mipMapsPreserveCoverage: 1')
            importer.write_text(settings.replace('alphaTestReferenceValue: 0.5', 'alphaTestReferenceValue: 0.42'))
    material_path.write_text(material)
    return meta(material_path, main_id=2100000)


def bind(prefabs):
    definition = TREE_DATA / 'Wild Coconut.asset'
    data = definition.read_text().split('  stages:\n', 1)[0]
    data += '  stages:\n'
    for name, stage, day, scale in STAGES:
        data += (f'  - stage: {stage}\n    fromGrowthDay: {day}\n    model: {ref(prefabs[name])}\n'
                 f'    scale: {{x: {scale}, y: {scale}, z: {scale}}}\n')
    definition.write_text(data + '  stumpModel: ' + ref(prefabs['Stump']) + '\n')

    path = TREE_DATA / 'Coconut Fruit.asset'
    data = re.sub(r'(?m)^  worldPrefab: .*\n', '', path.read_text())
    data, count = re.subn(r'(?m)^  worldScale: \{[^}]+}',
                          f'  worldPrefab: {ref(prefabs["Produce"])}\n'
                          '  worldScale: {x: 1, y: 1, z: 1}', data)
    assert count == 1, path
    path.write_text(data)


def main():
    for directory in (MODELS, PREFABS):
        directory.mkdir(parents=True, exist_ok=True)
        meta(directory, folder=True)

    material = publish_material()
    template_mesh = (ASSETS / 'mesh/Props/SoilScanner/SoilScanner_Cozy_LOD2.asset').read_text()
    template_prefab = blocks((ASSETS / 'Prefabs/Crops/G01/NP_Blueberry_Seed.prefab').read_text())
    prefabs = {}
    for variant in [stage[0] for stage in STAGES] + list(EXTRA_MODELS):
        name = 'NP_Coconut_' + variant
        spec = json.loads((SOURCE / 'Mesh' / (name + '.json')).read_text())
        mesh_id = serialize_mesh(spec, template_mesh, MODELS / (name + '.asset'))
        prefabs[variant] = prefab(name, [mesh_id], material, template_prefab, {}, directory=PREFABS)
    bind(prefabs)
    print('Wild coconut stages and fruit pickup published.')


if __name__ == '__main__':
    main()
