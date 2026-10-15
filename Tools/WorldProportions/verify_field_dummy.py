"""Check effective shop instance dimensions and authored field geometry without editing scenes."""
from pathlib import Path
import re
ROOT=Path('Assets/Nature  Paradaise')
DOC=re.compile(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^---|\Z)',re.M|re.S)
def docs(s):return {m[2]:(m[1],m[0]) for m in DOC.finditer(s)}
def vector(s,key):return [float(v) for v in re.findall(r': ([-\d.eE+]+)',re.search(r'^  '+key+r': (\{[^\n]+\})',s,re.M)[1])]
def scalar(s,key):return float(re.search(r'^  '+key+r': ([-\d.eE+]+)',s,re.M)[1])
def ref(s,key):return re.search(key+r': \{fileID: (-?\d+)',s)[1]
vendors={}
for path in (ROOT/'Prefabs/Shops/Testing').glob('*.prefab'):
    records=docs(path.read_text(encoding='utf-8-sig'))
    body=next(i for i,(t,b) in records.items() if t=='1' and 'm_Name: Seller_Capsule_Editable\n' in b)
    body_id=next(i for i,(t,b) in records.items() if t=='4' and ref(b,'m_GameObject')==body)
    root_id=next(i for i,(t,b) in records.items() if t=='4' and ref(b,'m_Father')=='0')
    guid=re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text())[1]
    vendors[guid]=(path,body_id,root_id,vector(records[body_id][1],'m_LocalScale'))

for name in ('World/Map','World/Map_GameplayPreview','Testing/TestingScene'):
    records=docs((ROOT/'Map/Scenes'/f'{name}.unity').read_text(encoding='utf-8-sig'))
    def parent_scale(i):
        scale=[1,1,1]
        while i!='0':
            t,b=records[i]
            assert t=='4' and 'm_LocalScale:' in b, ('Unexpected parent',i)
            v=vector(b,'m_LocalScale');scale=[a*c for a,c in zip(scale,v)];i=ref(b,'m_Father')
        return scale
    count=0
    for i,(t,b) in records.items():
        if t!='1001':continue
        guid=re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)',b)[1]
        if guid not in vendors:continue
        path,body_id,root_id,body_scale=vendors[guid]
        overrides={(q[1],q[2]):q[3] for q in re.finditer(r'target: \{fileID: (-?\d+), guid: \w+, type: \d+\}\n      propertyPath: ([^\n]+)\n      value: ([^\n]*)',b)}
        size=[];ps=parent_scale(ref(b,'m_TransformParent'))
        for axis,default,p in zip('xyz',body_scale,ps):
            local=float(overrides.get((body_id,'m_LocalScale.'+axis),default))
            root=float(overrides.get((root_id,'m_LocalScale.'+axis),1))
            size.append(local*root*p*(2 if axis=='y' else 1))
        assert all(abs(a-c)<.001 for a,c in zip(size,(.4,1.2,.4))),(name,path.name,size)
        print('PASS',name,path.name,'effective capsule',size);count+=1
    for i,(t,b) in records.items():
        if 'fieldId: player-home-field' not in b:continue
        cols=scalar(b,'columns');rows=scalar(b,'rows');assert scalar(b,'cellSize')==.8
        transform=next(j for j,(tt,bb) in records.items() if tt=='4' and 'm_GameObject:' in bb and ref(bb,'m_GameObject')==ref(b,'m_GameObject'))
        assert parent_scale(transform)==[1,1,1],name+' field ancestor scale'
        assert vector(records[ref(b,'areaCollider')][1],'m_Size')==[cols*.8,.2,rows*.8]
        assert vector(records[ref(b,'fieldSurface')][1],'m_LocalScale')==[cols*.08,1,rows*.08]
        print('PASS',name,'field',int(cols),'x',int(rows),'cells at 0.8m; collider and source plane synchronized')
    if name=='World/Map':assert count>=5,'Expected vendor instances in actual Map'
print('SCENE INSTANCE AND FIELD VALIDATION PASSED')
