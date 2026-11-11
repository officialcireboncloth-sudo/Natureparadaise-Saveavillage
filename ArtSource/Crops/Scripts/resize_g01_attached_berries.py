"""Scale attached fruit around its pedicel joint, preserving branches and leaf meshes."""
from pathlib import Path
import ast
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import g01_config as berries
import g01_raspberry_config as raspberry
import plant_blender_common as art
from export_g01_leaf_r05 import export
from unified_plant_atlas import load, write


SCALE = {'Blueberry': .70, 'Raspberry': .78, 'Blackberry': .80}


def resize(root, factor):
    meshes = [obj for obj in root.children_recursive if obj.type == 'MESH']
    heads = {int(obj['JointId'].split('_')[1]): obj for obj in meshes
             if obj.get('JointRole') == 'head' and obj.get('JointId','').startswith('Fruit_')}
    assert heads, root.name
    groups = {index: [head] for index,head in heads.items()}
    for obj in meshes:
        index = obj.get('FruitIndex')
        if index in groups:
            groups[index].append(obj)
    for index, parts in groups.items():
        assert len(parts) > 1, (root.name,index)
        point = Vector(heads[index]['JointPoint'])
        transform = Matrix.Translation(point) @ Matrix.Scale(factor,4) @ Matrix.Translation(-point)
        for part in parts:
            basis = part.matrix_basis.copy()
            part.data.transform(basis.inverted() @ transform @ basis)
    return len(groups)


def main():
    bpy.ops.wm.open_mainfile(filepath=str(berries.MASTER))
    bpy.context.preferences.filepaths.save_version = 0
    manifest = berries.MASTER.parent / 'G01_Manifest.json'
    data = load(manifest)
    exports = {entry['name']:entry for entry in data['exports']}
    builder = Path(__file__).with_name('build_g01_blender.py')
    original = next(node for node in ast.parse(builder.read_text()).body
                    if isinstance(node,ast.FunctionDef) and node.name=='export')
    legacy = dict(bpy=bpy, scene=bpy.context.scene, C=art, MODELS=berries.MODELS)
    exec(compile(ast.Module(body=[original],type_ignores=[]),str(builder),'exec'),legacy)
    for kind,factor in SCALE.items():
        config = raspberry if kind == 'Raspberry' else berries
        material = bpy.data.materials[config.ATLAS]
        config.configure(art, material)
        for phase in ('Flowering','Ready'):
            root = bpy.data.objects[f'{kind}_{phase}']
            old_factor = float(root.get('AttachedBerryScale',1))
            name = f'NP_{kind}_{phase}'
            if abs(factor-old_factor) < 1e-6:
                continue
            count = resize(root, factor/old_factor)
            root['AttachedBerryScale'] = factor
            if kind=='Blueberry':
                legacy['mat'] = material
                exports[name] = legacy['export'](root,name)
            else:
                exports[name] = export(root,name,material)
            print('ATTACHED_BERRIES', kind, phase, count, factor, flush=True)
    data.update(exports=list(exports.values()), attachedBerryScale=SCALE)
    write(manifest,data)
    bpy.ops.wm.save_as_mainfile(filepath=str(berries.MASTER))


if __name__ == '__main__':
    main()
