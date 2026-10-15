"""One-time authoring migration to the 1.5 m player reference (no map-position compression).

Run from repository root with --apply. An existing manifest prevents repeat scaling.
Backups retain the exact pre-migration bytes, including uncommitted authoring work.
"""
from pathlib import Path
import argparse, json, re, shutil

ROOT = Path('Assets/Nature  Paradaise')
OUT = Path('Documentation/WorldProportions-2026-10-15')
DOC = re.compile(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^---|\Z)', re.M | re.S)
NUM = r'[-\d.eE+]+'
changes, factors, offsets, overrides = {}, {}, {}, {}

def guid(path):
    return re.search(r'guid: (\w+)', Path(str(path)+'.meta').read_text())[1]

def vector(block, field, factor=None, value=None):
    def replace(m):
        if value is not None:
            return m[1] + '{x: '+str(value)+', y: '+str(value)+', z: '+str(value)+'}'
        return m[1]+re.sub(NUM, lambda n: format(float(n[0])*factor, '.9g'), m[2])
    return re.sub(r'(^  '+re.escape(field)+r': )(\{[^\n]+\})', replace, block, flags=re.M)

def scalar(block, field, value):
    return re.sub(r'(^  '+re.escape(field)+r': )'+NUM, lambda m: m[1]+format(value,'.9g'), block, flags=re.M)

def load(path):
    return changes.get(path, path.read_text(encoding='utf-8-sig'))

def save(path, text):
    if text != path.read_text(encoding='utf-8-sig'):
        changes[path] = text

def scale_prefab(path, factor):
    s=load(path)
    roots=[m for m in DOC.finditer(s) if m[1]=='4' and 'm_Father: {fileID: 0}' in m[3]]
    if len(roots)!=1: raise ValueError('Expected single native root: '+str(path))
    m=roots[0]; b=vector(m[0], 'm_LocalScale', factor=factor)
    save(path,s[:m.start()]+b+s[m.end():])
    factors[(guid(path),int(m[2]))]=factor

def animal_stages(name, stage_sizes, fallback_scales, height, radius):
    path=ROOT/'Prefabs/Animals'/f'{name}.prefab'; s=load(path); asset_guid=guid(path)
    # Growth-stage children are prefab instances. Correct their scales, leaving shared raw visuals intact.
    for m in reversed(list(DOC.finditer(s))):
        b=m[0]
        if m[1]=='1001':
            label=re.search(r'propertyPath: m_Name\n      value: (\w+)',b)
            if label and label[1] in stage_sizes:
                target=stage_sizes[label[1]]
                b=re.sub(r'(propertyPath: m_LocalScale\.[xyz]\n      value: )'+NUM,lambda q:q[1]+str(target),b)
                # Scene overrides target stripped transforms inside the actor prefab.
                instance_id=int(m[2])
                for stripped in DOC.finditer(s):
                    if stripped[1]=='4' and f'm_PrefabInstance: {{fileID: {instance_id}}}' in stripped[3]:
                        for axis in 'xyz':overrides[(asset_guid,int(stripped[2]),'m_LocalScale.'+axis)]=target
        elif m[1]=='114':
            for field,value in fallback_scales.items():b=vector(b,field,value=value)
        elif m[1]=='136':
            b=scalar(b,'m_Height',height);b=scalar(b,'m_Radius',radius)
            b=re.sub(r'(^  m_Center: )\{[^\n]+\}',lambda q:q[1]+f'{{x: 0, y: {height/2}, z: 0}}',b,flags=re.M)
        s=s[:m.start()]+b+s[m.end():]
    save(path,s)

def scale_vendor(path):
    s=load(path);parts=list(DOC.finditer(s))
    names={int(m[2]):re.search(r'^  m_Name: (.*)',m[3],re.M)[1] for m in parts if m[1]=='1' and 'm_Name:' in m[3]}
    for m in reversed(parts):
        if m[1]!='4' or 'm_GameObject:' not in m[3]:continue
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)',m[3])[1])
        if names.get(go) not in ('Seller_Capsule_Editable','StoreLabel_Editable'):continue
        b=vector(vector(m[0],'m_LocalScale',factor=.75),'m_LocalPosition',factor=.75)
        factors[(guid(path),int(m[2]))]=.75
        offsets[(guid(path),int(m[2]))]=.75
        s=s[:m.start()]+b+s[m.end():]
    save(path,s)

def plan():
    animal_stages('Cow',{'Adult':.9,'Adolescent':.78,'Young':.8,'Baby':.65,'Newborn':.55,'Pregnancy':.9},
                  {'adultScale':.9,'adolescentScale':.78,'youngScale':.8,'babyScale':.65,'hatchlingOrNewbornScale':.55},1.26,.35)
    animal_stages('Chicken',{'Adult':.583333333,'Adolescent':.5,'Young':.55,'Baby':.45,'Hatchling':.35,'Egg':.16},
                  {'adultScale':.583333333,'adolescentScale':.5,'youngScale':.55,'babyScale':.45,'hatchlingOrNewbornScale':.35},.35,.16)
    horse=ROOT/'Prefabs/Animals/Companions/Horse Companion.prefab'
    horse_factor=1.7/3.3567138
    scale_prefab(horse,horse_factor)
    s=scalar(load(horse),'adultHorseScale',1.25*horse_factor)
    for field in ['riderSeatOffset']:s=vector(s,field,factor=horse_factor)
    save(horse,s)
    scale_prefab(ROOT/'Prefabs/Animals/Companions/Dog Companion.prefab',.55/1.0649936)
    # Interior layout prefabs retain their authoring units; their scene owner scales them once.
    for folder,factor in [('PlayerHouse/Exterior',.6),('Barn/Exterior',.6),('Coop/Exterior',.75)]:
        for path in sorted((ROOT/'Prefabs'/folder).glob('*.prefab')):scale_prefab(path,factor)
    for path in (ROOT/'Prefabs/Shops/Testing').glob('*.prefab'):scale_vendor(path)
    for path in (ROOT/'Prefabs/ShopsMobile/Modules').glob('*.prefab'):
        if 'Store' in path.name or 'Market' in path.name:scale_prefab(path,.8)

    # Shared terrain tiles and detail prototype inputs, without changing texture files or density grids.
    for path in [ROOT/'Resources/World/Demo Garden Grass.terrainlayer',ROOT/'Art/StylizedMeadow/Terrain/MeadowGrass.terrainlayer']:
        save(path,vector(load(path),'m_TileSize',factor=1.5/3.6))
    for path in [ROOT/'Resources/World/Terrain Vegetation.asset',ROOT/'Art/StylizedMeadow/Profiles/MeadowVegetation.asset']:
        s=load(path)
        s=re.sub(r'((?:widthScale|heightScale): )\{([^}]+)\}',lambda m:m[1]+'{'+re.sub(NUM,lambda n:format(float(n[0])*(1.5/3.6),'.8g'),m[2])+'}',s)
        save(path,s)

    camera_guid=guid(ROOT/'Script/Camera/TopDownCameraFollow.cs');horse_guid=guid(horse)
    for path in (ROOT/'Map/Scenes').rglob('*.unity'):
        if path.stem not in {'Map','Map_GameplayPreview','TestingScene','FishingTestScene','HouseInterior','BarnInterior','CoopInterior'}:continue
        s=load(path); parts=list(DOC.finditer(s)); names={int(m[2]):re.search(r'^  m_Name: (.*)',m[3],re.M)[1] for m in parts if m[1]=='1' and 'm_Name:' in m[3]}
        # Parent each spatially related fixture, collider and marker scales together.
        for m in reversed(parts):
            b=m[0]
            if m[1]=='1001':
                pattern=r'(target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n      propertyPath: ([^\n]+)\n      value: )('+NUM+r')'
                def modify(q):
                    key=(q[3],int(q[2]),q[4]);v=float(q[5])
                    if key in overrides:return q[1]+str(overrides[key])
                    f=factors.get((q[3],int(q[2])))
                    if f and q[4] in ['m_LocalScale.'+a for a in 'xyz']:return q[1]+format(v*f,'.9g')
                    f=offsets.get((q[3],int(q[2])))
                    if f and q[4] in ['m_LocalPosition.'+a for a in 'xyz']:return q[1]+format(v*f,'.9g')
                    if q[4]=='adultHorseScale' and q[3]==horse_guid:return q[1]+format(1.25*horse_factor,'.9g')
                    return q[0]
                b=re.sub(pattern,modify,b)
            elif m[1]=='4' and 'm_Father:' in b:
                go=int(re.search(r'm_GameObject: \{fileID: (\d+)',b)[1]);name=names.get(go,'')
                if name=='HouseInterior_Editable':b=vector(b,'m_LocalScale',factor=.75)
                elif name in ('BarnInterior_Editable','CoopInterior_Editable'):
                    f=.6 if name.startswith('Barn') else .75
                    # Keep the underground floor at y=-200 rather than raising it toward the world.
                    b=vector(b,'m_LocalScale',factor=f)
                    b=re.sub(r'(^  m_LocalPosition: )\{[^\n]+\}',lambda q:q[1]+f'{{x: 0, y: {-200*(1-f)}, z: 0}}',b,flags=re.M)
                elif name in ('PlayerHouseExitSpawn','HouseEntranceDoor_Editable'):
                    b=vector(b,'m_LocalPosition',factor=.6);b=vector(b,'m_LocalScale',factor=.6)
                elif name.startswith('NPC_') or name=='Carpenter_Editable':
                    b=vector(b,'m_LocalScale',factor=.75)
                    if name=='Carpenter_Editable':
                        b=re.sub(r'(m_LocalPosition: \{x: '+NUM+r', y: )('+NUM+r')',lambda q:q[1]+str(float(q[2])-.25),b)
                elif re.fullmatch(r'House(?: \(\d+\))?',name):
                    sy=float(re.search(r'm_LocalScale: \{x: '+NUM+r', y: ('+NUM+r')',b)[1])
                    b=vector(b,'m_LocalScale',factor=.6)
                    b=re.sub(r'(m_LocalPosition: \{x: '+NUM+r', y: )('+NUM+r')',lambda q:q[1]+str(float(q[2])-sy*.2),b)
            elif m[1]=='114' and camera_guid in b:
                b=scalar(b,'distance',15);b=scalar(b,'maximumOrthographicSize',9)
                b=re.sub(r'(^  focusOffset: )\{[^\n]+\}',lambda q:q[1]+'{x: 0, y: 0.75, z: 0}',b,flags=re.M)
                b=re.sub(r'  zoomLevels:\n(?:  - [^\n]+\n)+','  zoomLevels:\n  - 5\n  - 6.5\n  - 8\n',b)
            elif m[1]=='114' and 'cameraDistance:' in b and path.stem in ('BarnInterior','CoopInterior'):
                b=scalar(b,'cameraDistance',18 if path.stem=='BarnInterior' else 15)
            elif m[1]=='20' and 'orthographic: 1' in b:
                b=scalar(b,'orthographic size',6.5)
            s=s[:m.start()]+b+s[m.end():]
        save(path,s)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    if (OUT/'manifest.json').exists():raise SystemExit('Already applied; inspect manifest instead of multiplying scales again.')
    plan();print('Planned:',len(changes),'files')
    if not args.apply:
        print('\n'.join(str(p) for p in changes));return
    for path,text in changes.items():
        backup=OUT/'Before'/path;backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,backup)
        path.write_text(text,encoding='utf-8')
    (OUT/'manifest.json').write_text(json.dumps({'playerHeight':1.5,'mapPositionsCompressed':False,'files':[str(p) for p in changes]},indent=2),encoding='utf-8')
    print('Applied with exact backups:',OUT)

if __name__=='__main__':main()
