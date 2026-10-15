"""Apply explicit dimensions; leave vendor UI owners and unrelated scene data intact."""
from pathlib import Path
import re, json, shutil

ROOT = Path('Assets/Nature  Paradaise')
OUT = Path('Documentation/Field-Dummy-Scale-2026-10-15')
DOC = re.compile(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^---|\Z)', re.M | re.S)
NUM = r'[-\d.eE+]+'
changes = {}
targets = {}

def scalar(s, key, value):
    return re.sub(r'(^  '+key+r': )'+NUM, lambda m:m[1]+str(value), s, flags=re.M)

def vec(s, key, value):
    return re.sub(r'(^  '+key+r': )\{[^\n]+\}', lambda m:m[1]+value, s, flags=re.M)

def save(path, original, revised):
    if original == revised: return
    backup = OUT/'Before'/path
    if not backup.exists():
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, backup)
    path.write_text(revised, encoding='utf-8', newline='\n')
    changes[str(path)] = 'Explicit spatial dimensions; UI and gameplay data preserved.'

for path in sorted((ROOT/'Prefabs/Shops/Testing').glob('*.prefab')):
    s = original = path.read_text(encoding='utf-8-sig')
    guid = re.search(r'guid: (\w+)', Path(str(path)+'.meta').read_text())[1]
    names = {m[2]: re.search(r'^  m_Name: (.*)', m[0], re.M)[1]
             for m in DOC.finditer(s) if m[1]=='1' and 'm_Name:' in m[0]}
    for m in reversed(list(DOC.finditer(s))):
        if m[1] not in ('4','224'): continue
        go = re.search(r'm_GameObject: \{fileID: (\d+)',m[0])
        name = names.get(go[1] if go else '')
        b = m[0]
        if name == 'Seller_Capsule_Editable':
            b = vec(vec(b,'m_LocalScale','{x: 0.4, y: 0.6, z: 0.4}'),
                    'm_LocalPosition','{x: 0, y: 0.6, z: 0}')
            for axis, value in zip('xyz', (.4,.6,.4)):
                targets[(guid,m[2],'m_LocalScale.'+axis)] = value
            targets[(guid,m[2],'m_LocalPosition.y')] = .6
        elif name == 'StoreLabel_Editable':
            # Keep label typography and UI scale; move only the world-space anchor.
            b = re.sub(r'(m_LocalPosition: \{x: '+NUM+r', y: )'+NUM,
                       lambda q:q[1]+'1.5',b)
            targets[(guid,m[2],'m_LocalPosition.y')] = 1.5
        s = s[:m.start()]+b+s[m.end():]
    save(path, original, s)

for path in (ROOT/'Map/Scenes').rglob('*.unity'):
    if path.stem not in {'Map','Map_GameplayPreview','TestingScene','FishingTestScene'}: continue
    s = original = path.read_text(encoding='utf-8-sig')
    replacements = {}
    for m in DOC.finditer(s):
        b = m[0]
        if 'fieldId: player-home-field' not in b: continue
        cols = int(re.search(r'^  columns: (\d+)',b,re.M)[1])
        rows = int(re.search(r'^  rows: (\d+)',b,re.M)[1])
        collider = re.search(r'areaCollider: \{fileID: (\d+)',b)[1]
        surface = re.search(r'fieldSurface: \{fileID: (\d+)',b)[1]
        b = scalar(scalar(scalar(b,'cellSize',.8),'fieldEdgeFeather',.12),'tileEdgeFeather',.05)
        replacements[m[2]] = b
        for q in DOC.finditer(s):
            if q[2] == collider:
                replacements[q[2]] = vec(q[0],'m_Size',f'{{x: {cols*.8:g}, y: 0.2, z: {rows*.8:g}}}')
            elif q[2] == surface:
                replacements[q[2]] = vec(q[0],'m_LocalScale',f'{{x: {cols*.08:g}, y: 1, z: {rows*.08:g}}}')
    for m in reversed(list(DOC.finditer(s))):
        b = replacements.get(m[2],m[0])
        if m[1]=='1001':
            pattern=r'(target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n      propertyPath: ([^\n]+)\n      value: )('+NUM+r')'
            b = re.sub(pattern,lambda q:q[1]+str(targets[(q[3],q[2],q[4])])
                       if (q[3],q[2],q[4]) in targets else q[0], b)
        s=s[:m.start()]+b+s[m.end():]
    save(path, original, s)

for name in ['SeparatedShopSetup','UpgradeShopsSetup','CalendarCafeSetup']:
    path = ROOT/'Script/Editor'/f'{name}.cs'
    s=original=path.read_text(encoding='utf-8-sig')
    s=s.replace('new Vector3(0,1f,0)','new Vector3(0,.6f,0)').replace('new Vector3(.7f,1f,.7f)','new Vector3(.4f,.6f,.4f)')
    s=s.replace('capsule.localScale=seller.transform.lossyScale','capsule.localScale=new Vector3(.4f,.6f,.4f)')
    s=s.replace('seller!=null?seller.transform.lossyScale:new Vector3(.7f,1,.7f)','new Vector3(.4f,.6f,.4f)')
    s=s.replace('new Vector3(0,2.4f,0)','new Vector3(0,1.5f,0)').replace('new Vector3(0,2.65f,0)','new Vector3(0,1.5f,0)')
    save(path,original,s)

OUT.mkdir(parents=True,exist_ok=True)
(OUT/'manifest.json').write_text(json.dumps(changes,indent=2),encoding='utf-8')
print(json.dumps(changes,indent=2))
