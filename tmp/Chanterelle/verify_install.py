import json
import re
import struct
from pathlib import Path

work = Path(__file__).resolve().parent
root = work.parents[1]
assets = root/'Assets/Nature  Paradaise'
name = 'NP_Chanterelle_01'


def guid(path):
    return re.search(r'(?m)^guid: (\w+)$',Path(str(path)+'.meta').read_text())[1]


material = assets/f'Material/Shared/Mushrooms/{name}.mat'
text = material.read_text()
original = assets/'Pack/Angry Mesh/Stylized Pack - Meadow Environment/Sources/Materials/Mushrooms/M_Mushroom_Brown_01_Summer.mat'
assert re.search(r'  m_Shader: (.+)',text)[1] == re.search(r'  m_Shader: (.+)',original.read_text())[1]
assert '_EnableWind: 0' in text
assert '_BaseAlbedoColor: {r: 0.5, g: 0.5, b: 0.5, a: 0}' in text
for suffix in ('BaseColor','Normal','SMAE'):
    texture = assets/f'Textures/Shared/Mushrooms/{name}_{suffix}.png'
    assert guid(texture) in text
    assert struct.unpack('>II',texture.read_bytes()[16:24]) == (1024,1024)
    assert texture.read_bytes() == (root/f'ArtSource/Mushrooms/Textures/{texture.name}').read_bytes()
normal_meta = (assets/f'Textures/Shared/Mushrooms/{name}_Normal.png.meta').read_text()
assert 'textureType: 1' in normal_meta and 'sRGBTexture: 0' in normal_meta
assert 'sRGBTexture: 0' in (assets/f'Textures/Shared/Mushrooms/{name}_SMAE.png.meta').read_text()
prefab = assets/f'Prefabs/Environment/Mushrooms/{name}.prefab'
assert guid(material) in prefab.read_text()
for lod in (0,1):
    mesh = assets/f'mesh/World/Mushrooms/{name}_LOD{lod}.asset'
    spec = json.loads((work/f'Mesh/{name}_LOD{lod}.json').read_text())
    assert guid(mesh) in prefab.read_text()
    buffer = bytes.fromhex(re.search(r'(?m)^    _typelessdata: (.+)$',mesh.read_text())[1])
    assert len(buffer)==len(spec['vertices'])*52
    assert all(buffer[i+48:i+52]==bytes((0,0,0,255)) for i in range(0,len(buffer),52))
    assert abs(max(p[1] for p in spec['vertices'])-.30)<.00001
    print(mesh.name,spec['triangles'],'triangles')
assert 'screenRelativeHeight: 0.08' in prefab.read_text()
assert (root/f'ArtSource/Mushrooms/Blender/{name}.blend').stat().st_size>100000
assert (root/f'ArtSource/Mushrooms/Textures/{name}.xcf').stat().st_size>100000
print('CHANTERELLE_INSTALLED_REFERENCES_AND_VERTEX_FORMAT_PASS')
