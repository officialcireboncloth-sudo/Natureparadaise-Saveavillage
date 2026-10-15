"""Read-only design audit. Does not model/export or run/change Unity."""
from pathlib import Path
import json, hashlib, math, itertools
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
s=json.loads((OUT/'house-proposal-A.json').read_text(encoding='utf-8'))
checks=[]
def check(condition,message):
    if not condition: raise AssertionError(message)
    checks.append('PASS: '+message)

def vertices(p):
    if p['type']=='surface': return np.array(p['verticesXYZ'],float)
    if p['type']=='box':
        c=np.array(p['centerXYZ']);e=np.array(p['sizeXYZ'])/2
        return np.array([c+np.array(q)*e for q in itertools.product([-1,1],repeat=3)])
    a=np.array(p['fromXYZ']);b=np.array(p['toXYZ']);axis=b-a
    axis/=np.linalg.norm(axis)
    ref=np.array([0,1,0]) if abs(axis[1])<.9 else np.array([0,0,1])
    side=np.cross(axis,ref);side/=np.linalg.norm(side);up=np.cross(axis,side)
    return np.array([q+side*t*p['width']/2+up*u*p['depth']/2 for q in [a,b] for t,u in itertools.product([-1,1],repeat=2)])

check(s['status']=='PROPOSED_NOT_MODELLED_NOT_APPLIED','proposal status is sketch only')
check(s['levelToModel']=={'1':'Lv1','2':'Lv2','3':'Lv3-4','4':'Lv3-4','5':'Lv5'},'four designs; Lv3/Lv4 share model')
check(s['coordinateSystem']['front']=='+Z','front convention explicit +Z')
atlas=ROOT/s['materials']['atlasPath'];im=Image.open(atlas).convert('RGB')
check(list(im.size)==s['materials']['atlasDimensions'],'atlas dimensions match actual PSD')
check(hashlib.sha256(atlas.read_bytes()).hexdigest()==s['materials']['atlasSha256'],'source atlas hash unchanged')
for name,q in s['materials']['swatches'].items():
    x,y,w,h=q['pixelRectTopLeft']
    check(list(im.getpixel((x+w//2,y+h//2)))==q['rgb'],f'{name}: color sampled from source atlas')
    expected=[(x+4)/2048,1-(y+h-4)/2048,(x+w-4)/2048,1-(y+4)/2048]
    check(np.allclose(expected,q['safeUvBottomLeft']),f'{name}: UV conversion and margin 4 px')
matroot=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models/Materials'
for role in ['opaque','emission','glass']:
    check((matroot/s['materials'][role]).is_file(),role+' material exists')
glass=(matroot/s['materials']['glass']).read_text()
check(s['materials']['glassTextureGuid'] in glass and '_SURFACE_TYPE_TRANSPARENT' in glass,'glass uses actual pack texture + transparency')
glasspsd=matroot.parents[1]/'Textures'/s['materials']['glassTexture']
check(Image.open(glasspsd).size==(128,128),'existing glass PSD is 128 x 128')
check(s['materials']['newRuntimeTextures']==0,'no new runtime textures planned')

allids=[]
reserve=s['siteReservation']
for m in s['models']:
    key=m['id'];pp=m['parts'];vs=np.concatenate([vertices(p) for p in pp])
    lo=vs.min(axis=0);hi=vs.max(axis=0);q=m['visualBounds']
    check(np.isfinite(vs).all(),key+': all coordinates finite')
    check(np.allclose(lo,q['minXYZ']) and np.allclose(hi,q['maxXYZ']),key+': declared bounds match exact primitives')
    check(np.allclose([hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],q['sizeWDH']),key+': W/D/H correct axis mapping')
    check(lo[0]>=reserve['minXZ'][0] and hi[0]<=reserve['maxXZ'][0] and lo[2]>=reserve['minXZ'][1] and hi[2]<=reserve['maxXZ'][1],key+': 22 x 17 m reservation contains all visual parts')
    check(reserve['minXZ'][1]<=m['entry']['approachXYZ'][2]<=reserve['maxXZ'][1],key+': approach within reservation')
    check(m['door']['clearWH'][1]>=s['playerReferenceHeight']+.35,key+': door height accommodates 3.6 m player')
    check(m['uniformScale']==1 and m['wallFrontZ']==0,key+': stable scale and common front door plane')
    check(m['budgetTriangles']['LOD0Target']==sum(m['budgetTriangles']['LOD0Allocation'].values()),key+': component triangle allocation adds to target')
    check(m['budgetTriangles']['LOD2Target']<m['budgetTriangles']['LOD1Target']<m['budgetTriangles']['LOD0Target']<=m['budgetTriangles']['LOD0Limit'],key+': LOD budgets ordered')
    check(m['sketchPrimitiveTriangles']<m['budgetTriangles']['LOD0Limit'],key+': sketch complexity below final hard cap (not final mesh count)')
    w,dep=m['mainBodyWD']
    check(abs(math.degrees(math.atan((m['ridgeY']-m['eaveY'])/(dep/2)))-m['roofSlopeDegrees'])<1e-6,key+': roof slope derived from actual rise/run')
    slots={s['materials']['productionSlotByPalette'][p['material']] for p in pp}
    check(len(slots)==(3 if key=='Lv5' else 2),key+': production material slots <= 3')
    # The actual 2.4 m lane is ground/porch region Z>0.2. All decorative vertices stay outside it.
    obstacles=[]
    for p in pp:
        if p['group']!='decor':continue
        v=vertices(p);a=v.min(axis=0);b=v.max(axis=0)
        if b[2]>.2 and a[2]<m['entry']['approachXYZ'][2] and b[0]>-1.2 and a[0]<1.2:
            obstacles.append(p['id'])
    check(not obstacles,key+': attached decor clears 2.4 m approach lane')
    for name in ['front-right','rear-left','elevations','plan']:
        p=OUT/f'house-{key}-{name}.png'
        check(p.is_file() and Image.open(p).width>=1200,key+': '+name+' drawing present')
    allids.extend(p['id'] for p in pp)
check(len(allids)==len(set(allids)),'all component identifiers unique')
check(sum(p['name'].startswith('skylight_') and p['name'].endswith('_pane') for p in s['models'][1]['parts'])==1,'Lv2 has exactly one skylight')
check(sum(p['name'].startswith('skylight_') and p['name'].endswith('_pane') for p in s['models'][3]['parts'])==2,'Lv5 has exactly two skylights')
check('wing' in s['models'][2]['additionalFeatures'] and 'dormer' in s['models'][2]['additionalFeatures'],'Lv3-4 has wing + dormer')
check(len(s['models'][3]['additionalFeatures']['sunroom']['footprintXZ'])==6,'Lv5 glass bay has fixed six-point footprint / five exterior facets')
for key in ['Lv1','Lv2','Lv3-4','Lv5']:
    check((OUT/'References'/f'House-{key}-UserReference.png').exists(),key+': correct unique reference archived')
check(not list(OUT.rglob('*.fbx')) and not list(OUT.rglob('*.blend')),'review folder contains no production meshes')
report='\n'.join(checks)+f'\n\n{len(checks)} design checks passed. No Blender/Unity validation claimed.\n'
(OUT/'validation-report.txt').write_text(report,encoding='utf-8')
print(report)
