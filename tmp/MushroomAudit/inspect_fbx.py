import importlib
import sys
import types
from pathlib import Path

root = Path('C:/Program Files (x86)/Steam/steamapps/common/Blender/4.5/scripts/addons_core/io_scene_fbx')
package = types.ModuleType('local_fbx')
package.__path__ = [str(root)]
sys.modules['local_fbx'] = package
parser = importlib.import_module('local_fbx.parse_fbx')
project = Path(__file__).resolve().parents[2]
files = sorted(project.glob('Assets/**/Models/TFP_Mushroom_0*A.fbx'))
files += sorted(project.glob('Assets/**/Sources/Meshes/Mushrooms/SM_Mushroom_01.fbx'))
for path in files:
    tree, version = parser.parse(str(path))
    objects = next(e for e in tree.elems if e.id == b'Objects')
    print(path.name, version)
    for node in objects.elems:
        if node.id == b'Geometry':
            print('  geometry', node.props[:3])
            for sub in node.elems:
                print('   ', sub.id, [str(p)[:100] for p in sub.props][:2],
                      [(x.id, [str(p)[:70] for p in x.props]) for x in sub.elems[:6]])
            break
        if node.id in (b'Model', b'GlobalSettings'):
            print('  ', node.id, node.props)
