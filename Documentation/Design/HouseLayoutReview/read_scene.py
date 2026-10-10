"""Read authored Unity text scene only; never opens or modifies the scene."""
import re, json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SCENE = ROOT / 'Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity'
text = SCENE.read_text(encoding='utf-8-sig')
docs = {}
for m in re.finditer(r'^--- !u!(\d+) &(\d+).*\n([\s\S]*?)(?=^--- !u!|\Z)', text, re.M):
    docs[int(m[2])] = (int(m[1]), m[3])

def ref(body, key):
    m = re.search(r'^  '+key+r': \{fileID: (\d+)', body, re.M)
    return int(m[1]) if m else 0
def vec(body, key):
    m = re.search(r'^  '+key+r': \{([^}]+)\}',body,re.M)
    return {k:float(v) for k,v in re.findall(r'([xyzw]): ([-\d.eE+]+)',m[1])} if m else {}

names = {i:m[1].strip() for i,(t,b) in docs.items() if t==1 and (m:=re.search(r'^  m_Name: (.*)$',b,re.M))}
transforms = {}
boxes = {}
for i,(t,b) in docs.items():
    if t==4 and 'm_LocalPosition:' in b:
        transforms[i] = dict(go=ref(b,'m_GameObject'),parent=ref(b,'m_Father'),position=vec(b,'m_LocalPosition'),rotation=vec(b,'m_LocalRotation'),scale=vec(b,'m_LocalScale'))
    if t==65:
        boxes[ref(b,'m_GameObject')] = dict(center=vec(b,'m_Center'),size=vec(b,'m_Size'))

def point(t,p):
    s=t['scale']; q=t['rotation']; v=[p[k]*s[k] for k in 'xyz']; u=[q[k] for k in 'xyz']; w=q['w']
    cross=lambda a,b:[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
    c=cross(u,v); cc=cross(u,c)
    return {k:v[j]+2*w*c[j]+2*cc[j]+t['position'][k] for j,k in enumerate('xyz')}
def to_layout(i,p,stop):
    while i!=stop and i in transforms:
        p=point(transforms[i],p);i=transforms[i]['parent']
    return p
def path(i,stop):
    a=[]
    while i!=stop and i in transforms:
        t=transforms[i];a.append(names.get(t['go'],'?'));i=t['parent']
    return '/'.join(reversed(a)) if i==stop else None

result = {}
for lv in (3,4,5):
    layout=next(i for i,t in transforms.items() if names.get(t['go'])==f'InteriorLayout_Lv{lv}_Editable')
    items=[]
    for i,t in transforms.items():
        p=path(i,layout)
        if not p or p.count('/')>1:continue
        go=t['go']; b=boxes.get(go)
        if not b:continue
        bounds=[]
        for sx in (-.5,.5):
            for sy in (-.5,.5):
                for sz in (-.5,.5):
                    bounds.append(to_layout(i,{k:b['center'][k]+s*b['size'][k] for k,s in zip('xyz',(sx,sy,sz))},layout))
        minv={k:min(v[k] for v in bounds) for k in 'xyz'};maxv={k:max(v[k] for v in bounds) for k in 'xyz'}
        items.append(dict(path=p,transform=t,center={k:round((minv[k]+maxv[k])/2,4) for k in 'xyz'},size={k:round(maxv[k]-minv[k],4) for k in 'xyz'}))
    result[str(lv)]=items
out=Path(__file__).with_name('current-scene-snapshot.json');out.write_text(json.dumps(result,indent=2),encoding='utf-8')
for lv,items in result.items():
    print('LEVEL',lv)
    for item in items:
        if any(key in item['path'] for key in ('Kitchen','Chair','Sofa','TV','Aquarium','Bed_Mesh','StorageChest','ToolCabinet')):
            print(item['path'],item['center'],item['size'])
