"""Unity 6 mesh serialization helpers, reused from the established machine art pipeline."""
from pathlib import Path
import re,struct,math,uuid

def meta(path, template=None, main_id=None, folder=False):
    destination = Path(str(path) + '.meta')
    if destination.exists():
        return re.search('^guid: (\\w+)', destination.read_text(), re.M)[1]
    guid = uuid.uuid4().hex
    if template:
        text = Path(str(template) + '.meta').read_text()
        text = re.sub('^guid: \\w+', 'guid: ' + guid, text, flags=re.M)
    elif folder:
        text = 'fileFormatVersion: 2\nguid: ' + guid + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif main_id:
        text = 'fileFormatVersion: 2\nguid: ' + guid + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: ' + str(main_id) + '\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:
        text = 'fileFormatVersion: 2\nguid: ' + guid + '\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    destination.write_text(text, encoding='utf-8')
    return guid

def v3(values):
    return '{x: ' + format(values[0], '.9g') + ', y: ' + format(values[1], '.9g') + ', z: ' + format(values[2], '.9g') + '}'

def bounds(vertices):
    lo = [min((v[i] for v in vertices)) for i in range(3)]
    hi = [max((v[i] for v in vertices)) for i in range(3)]
    return ([(a + b) / 2 for a, b in zip(lo, hi)], [(b - a) / 2 for a, b in zip(lo, hi)])

def dot(a, b):
    return sum((x * y for x, y in zip(a, b)))

def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]

def tangents(spec):
    vertices = spec['vertices']
    uv = spec['uv']
    tan = [[0.0, 0.0, 0.0] for _ in vertices]
    bit = [[0.0, 0.0, 0.0] for _ in vertices]
    for sub in spec['submeshes']:
        for at in range(0, len(sub), 3):
            ids = sub[at:at + 3]
            p, q, r = [vertices[i] for i in ids]
            u, v, w = [uv[i] for i in ids]
            e1 = [q[i] - p[i] for i in range(3)]
            e2 = [r[i] - p[i] for i in range(3)]
            a, b = (v[0] - u[0], v[1] - u[1])
            c, d = (w[0] - u[0], w[1] - u[1])
            det = a * d - b * c
            if abs(det) < 1e-12:
                continue
            t = [(d * e1[i] - b * e2[i]) / det for i in range(3)]
            bt = [(a * e2[i] - c * e1[i]) / det for i in range(3)]
            for index in ids:
                for i in range(3):
                    tan[index][i] += t[i]
                    bit[index][i] += bt[i]
    result = []
    for n, t, b in zip(spec['normals'], tan, bit):
        projection = dot(n, t)
        t = [t[i] - n[i] * projection for i in range(3)]
        length = math.sqrt(dot(t, t))
        if length < 1e-08:
            t = cross([0, 1, 0] if abs(n[1]) < 0.9 else [1, 0, 0], n)
            length = math.sqrt(dot(t, t))
        t = [x / max(length, 1e-08) for x in t]
        result.append(t + [-1 if dot(cross(n, t), b) < 0 else 1])
    return result
