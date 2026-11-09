"""Apply the current native-imported prefab visual to the existing Map station.

Preserves the scene station's transform, component IDs, processor/save data and
all unrelated scene objects. Only recognized placeholder/model children change.
"""
from pathlib import Path
import re, json, hashlib
from datetime import datetime, timezone

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'
SCENE=A/'Map/Scenes/World/Map.unity'
PREFAB=A/'Resources/World/FertilizerMaker_Dummy.prefab'
REVIEW=ROOT/'ArtSource/FertilizerMaker/Review'
PROCESSOR='52c4875d0d7943e0b3d5c90010000003'

def parse(text):
    headers=list(re.finditer(r'(?m)^--- !u!(\d+) &(-?\d+)([^\n]*)\n',text))
    result={}
    for i,match in enumerate(headers):
        key=int(match[2]);result[key]=dict(kind=int(match[1]),extra=match[3],
            body=text[match.end():headers[i+1].start() if i+1<len(headers) else len(text)])
    return text[:headers[0].start()],result

def game_object(body):
    match=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',body)
    return int(match[1]) if match else None
def name(body):return re.search(r'^  m_Name: (.*)',body,re.M)[1]
def transform_for(blocks,go):return next(i for i,b in blocks.items() if b['kind']==4 and game_object(b['body'])==go)
def children(body):
    value=re.search(r'(?s)  m_Children:.*?(?=  m_Father:)',body)[0]
    return [int(v) for v in re.findall(r'fileID: (-?\d+)',value)]
def subtree(blocks,transform):
    result=set();todo=[transform]
    while todo:
        current=todo.pop();go=game_object(blocks[current]['body'])
        result.add(go);result.add(current)
        result.update(int(i) for i in re.findall(r'component: \{fileID: (-?\d+)\}',blocks[go]['body']))
        todo.extend(children(blocks[current]['body']))
    return result

original_bytes=SCENE.read_bytes();newline='\r\n' if b'\r\n' in original_bytes else '\n'
header,scene=parse(original_bytes.decode('utf-8').replace('\r\n','\n'))
_,source=parse(PREFAB.read_text())
old={i:dict(b) for i,b in scene.items()}
processor=next(i for i,b in scene.items() if b['kind']==114 and 'guid: '+PROCESSOR in b['body'])
root_go=game_object(scene[processor]['body']);root_transform=transform_for(scene,root_go)
root_before=scene[root_transform]['body'];processor_before=scene[processor]['body']
visual_go=next(i for i,b in source.items() if b['kind']==1 and name(b['body'])=='Visual_FertilizerMaker_Cozy')
visual_transform=transform_for(source,visual_go);model_ids=subtree(source,visual_transform)
removed=set();retained_children=[]
for child in children(scene[root_transform]['body']):
    go=game_object(scene[child]['body'])
    if name(scene[go]['body']) in ('Visual_Dummy_ReplaceLater','ProcessorVisual_ReplaceMe','Visual_FertilizerMaker_Cozy'):
        removed.update(subtree(scene,child))
    else:retained_children.append(child)
for i in removed:scene.pop(i)
new_id=930100000;mapping={}
for identifier in sorted(model_ids):
    while new_id in scene:new_id+=1
    mapping[identifier]=new_id;new_id+=1
for identifier in sorted(model_ids):
    block=dict(source[identifier]);body=block['body']
    body=re.sub(r'\{fileID: (-?\d+)\}',lambda m:'{fileID: '+str(mapping.get(int(m[1]),int(m[1])))+'}',body)
    if identifier==visual_transform:
        body=re.sub(r'(?m)^  m_Father:.*$','  m_Father: {fileID: '+str(root_transform)+'}',body)
    block['body']=body;scene[mapping[identifier]]=block
attached=retained_children+[mapping[visual_transform]]
scene[root_transform]['body']=re.sub(r'(?s)  m_Children:.*?(?=  m_Father:)',
    lambda _:'  m_Children:\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in attached),root_before)
scene[root_go]['body']=re.sub(r'(?m)^  m_Name:.*$','  m_Name: Pembuat Pupuk',scene[root_go]['body'])
collider=next(i for i,b in scene.items() if b['kind']==65 and game_object(b['body'])==root_go)
source_collider=next(b['body'] for b in source.values() if b['kind']==65)
for field in ['m_Size','m_Center']:
    value=re.search(r'^  '+field+r':.*$',source_collider,re.M)[0]
    scene[collider]['body']=re.sub(r'(?m)^  '+field+r':.*$',lambda _:value,scene[collider]['body'])

changed={root_go,root_transform,collider}
assert scene[processor]['body']==processor_before
for field in ['m_LocalRotation','m_LocalPosition','m_LocalScale','m_Father']:
    assert re.search(r'^  '+field+r':.*$',root_before,re.M)[0]==re.search(r'^  '+field+r':.*$',scene[root_transform]['body'],re.M)[0]
for identifier,block in old.items():
    if identifier not in removed|changed:assert scene[identifier]==block,identifier
assert sum(scene[i]['kind']==23 for i in mapping.values())==3
assert sum(scene[i]['kind']==205 for i in mapping.values())==1
assert sum(b['kind']==114 and 'guid: '+PROCESSOR in b['body'] for b in scene.values())==1
for identifier in mapping.values():
    for ref in re.findall(r'\{fileID: (-?\d+)\}',scene[identifier]['body']):assert int(ref)==0 or int(ref) in scene

REVIEW.mkdir(parents=True,exist_ok=True)
backup=REVIEW/('Map_BeforeApplyModel_'+datetime.now(timezone.utc).strftime('%Y%m%d_%H%M%S')+'.unity')
backup.write_bytes(original_bytes)
result=header+''.join('--- !u!'+str(b['kind'])+' &'+str(i)+b['extra']+'\n'+b['body'] for i,b in scene.items())
SCENE.write_bytes(result.replace('\n',newline).encode('utf-8'))
report=dict(status='PASS',scene=str(SCENE.relative_to(ROOT)),root='Pembuat Pupuk',
    processorIdUnchanged=True,transformUnchanged=True,unrelatedBlocksUnchanged=True,
    removedPlaceholderComponents=len(removed),installedModelComponents=len(mapping),
    lodRenderers=3,colliders=1,rootGameObjectId=root_go,backup=str(backup.relative_to(ROOT)),
    sourcePrefabSHA256=hashlib.sha256(PREFAB.read_bytes()).hexdigest())
(REVIEW/'SceneApplyAudit.json').write_text(json.dumps(report,indent=2))
print('FERTILIZER_MODEL_APPLIED_TO_MAP '+json.dumps(report),flush=True)
