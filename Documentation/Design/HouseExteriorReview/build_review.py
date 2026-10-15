"""Exact-coordinate planning sketches only. Does not export meshes or edit Unity assets."""
from pathlib import Path
import json, math, hashlib, shutil, itertools
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
PACK = ROOT / 'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack'
ATLAS = PACK / 'Textures/TFP_Atlas_1A.psd'
BG = (248, 245, 235)
INK = '#263b41'
FONT = Path('C:/Windows/Fonts')

def font(n=24, bold=False):
    return ImageFont.truetype(str(FONT / ('arialbd.ttf' if bold else 'arial.ttf')), n)

def label(d, xy, s, n=24, bold=False, fill=INK):
    d.text(xy, s, font=font(n, bold), fill=fill)

targets = {'wall':(234,216,174), 'trim':(112,134,139), 'roof':(79,86,101),
           'roof_light':(104,107,120), 'wood':(164,106,52), 'stone':(198,183,146),
           'stone_light':(225,211,176), 'brick':(192,99,40), 'metal':(50,54,55),
           'window':(252,179,65), 'leaf':(97,141,43), 'flower':(246,239,209),
           'purple':(167,127,184), 'pot':(170,94,46), 'glass':(131,183,187)}
atlas = Image.open(ATLAS).convert('RGB')
assert atlas.size == (2048,2048)
swatches = {}
for name, target in targets.items():
    samples = [(sum((int(atlas.getpixel((c*32+16,1024+r*64+32))[i])-target[i])**2 for i in range(3)), c, r)
               for r in range(16) for c in range(32)]
    _, c, r = min(samples)
    x,y = c*32,1024+r*64
    rgb = list(atlas.getpixel((x+16,y+32)))
    swatches[name] = {'rgb':rgb, 'pixelRectTopLeft':[x,y,32,64],
                      'safeUvBottomLeft':[(x+4)/2048,1-(y+60)/2048,(x+28)/2048,1-(y+4)/2048]}
COLORS = {k:np.array(v['rgb'],float) for k,v in swatches.items()}
# Glass color is a drawing convention; actual pack glass is transparent, not a palette UV.
COLORS['glass'] = np.array([167,196,190],float)

MODELS = []
parts = []
def box(name,c,s,mat,group='shell'):
    parts.append({'name':name,'type':'box','centerXYZ':list(c),'sizeXYZ':list(s),'material':mat,'group':group})
def surface(name,vs,mat,group='shell'):
    parts.append({'name':name,'type':'surface','verticesXYZ':[list(v) for v in vs],'material':mat,'group':group})
def beam(name,a,b,w,mat,group='trim',depth=None):
    parts.append({'name':name,'type':'beam','fromXYZ':list(a),'toXYZ':list(b),
                  'width':w,'depth':depth or w,'material':mat,'group':group})
def cylinder(name,c,r,h,mat,n=8,group='decor'):
    x,y,z=c
    lo=[(x+r*math.cos(i*math.tau/n),y-h/2,z+r*math.sin(i*math.tau/n)) for i in range(n)]
    hi=[(a,y+h/2,b) for a,_,b in lo]
    surface(name+'_top',hi,mat,group)
    for i in range(n): surface(name+f'_{i}',[lo[i],lo[(i+1)%n],hi[(i+1)%n],hi[i]],mat,group)
def bud(name,c,s,mat):
    x,y,z=c;rx,ry,rz=s
    vertices=[(x-rx,y,z),(x+rx,y,z),(x,y-ry,z),(x,y+ry,z),(x,y,z-rz),(x,y,z+rz)]
    for a,b,k in [(0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)]:
        surface(name+f'_{a}{b}{k}',[vertices[a],vertices[b],vertices[k]],mat,'decor')
def foliage(name,c,w=1.7):
    x,y,z=c
    for i in range(5):
        xx=x+(i-2)*w/5
        bud(name+f'_leaf{i}',(xx,y,z),(w/5,.2,.26),'leaf')
        bud(name+f'_flower{i}',(xx,y+.19,z+.03),(.12,.10,.12),'flower' if i%3 else 'purple')
def planter(name,c,w=1.9,side=False):
    x,y,z=c
    box(name+'_box',c,(.45,.4,w) if side else (w,.4,.45),'wood','decor')
    if side:
        for i in range(4): foliage(name+str(i),(x,y+.32,z+(i-1.5)*w/4),.4)
    else: foliage(name,(x,y+.32,z),w)
def pot(name,c):
    x,y,z=c
    cylinder(name,(x,y+.28,z),.32,.56,'pot')
    foliage(name+'_plant',(x,y+.65,z),.7)

def window(name,center,w=1.8,h=1.9,side=False,flowers=True,rear=False):
    x,y,z=center
    # The planning shell is closed. Window panes project 3 cm outside it.
    def b(suffix,cc,ss,mat): box(name+suffix,cc,ss,mat,'windows')
    if not side:
        sign=-1 if rear else 1
        b('_pane',(x,y,z),(w,h,.055),'window')
        for xx in [x-w/2-.09,x+w/2+.09,x]: b('_vertical',(xx,y,z+sign*.06),(.14,h+.3,.14),'trim')
        for yy in [y-h/2-.09,y+h/2+.09,y]: b('_horizontal',(x,yy,z+sign*.06),(w+.32,.14,.14),'trim')
        if flowers: planter(name+'_flowers',(x,y-h/2-.4,z+sign*.27),w+.35)
    else:
        b('_pane',(x,y,z),(.055,h,w),'window')
        sign = 1 if x>0 else -1
        for zz in [z-w/2-.09,z+w/2+.09,z]: b('_vertical',(x+sign*.06,y,zz),(.14,h+.3,.14),'trim')
        for yy in [y-h/2-.09,y+h/2+.09,y]: b('_horizontal',(x+sign*.06,yy,z),(.14,.14,w+.32),'trim')
        if flowers: planter(name+'_flowers',(x+sign*.27,y-h/2-.4,z),w+.35,True)

def roof(name,cx,zfront,w,d,eave,ridge,axis='X',overhang=.4,rows=7):
    # Axis X: ridge left-right, slopes towards front/rear. Axis Z: porch/dormer gable.
    zmid=zfront-d/2
    if axis=='X':
        a0,a1=cx-w/2-overhang,cx+w/2+overhang
        half=d/2; center=zmid
        def point(a,b,lift=0): return (a,eave+(ridge-eave)*(1-abs(b-center)/half)+lift,b)
        b0,b1=zfront+overhang,zfront-d-overhang
        for x in [cx-w/2,cx+w/2]:
            surface(name+'_gable',[(x,eave,zfront),(x,ridge,zmid),(x,eave,zfront-d)],'wall')
    else:
        a0,a1=zfront-d-overhang,zfront+overhang
        half=w/2;center=cx
        def point(a,b,lift=0): return (b,eave+(ridge-eave)*(1-abs(b-center)/half)+lift,a)
        b0,b1=cx+w/2+overhang,cx-w/2-overhang
        for z in [zfront,zfront-d]:
            surface(name+'_gable',[(cx-w/2,eave,z),(cx,ridge,z),(cx+w/2,eave,z)],'wall')
    columns=max(3,round((a1-a0)/.82))
    for sign,end in [(1,b0),(-1,b1)]:
        for row in range(rows):
            b=center+(end-center)*row/rows;bb=center+(end-center)*(row+1)/rows
            for col in range(columns):
                a=a0+(a1-a0)*col/columns;aa=a0+(a1-a0)*(col+1)/columns
                # Flat subdivided roof face and a narrow raised lip; not individual solid roof tiles.
                lift=.035+row*.006
                surface(name+f'_slate_{sign}_{row}_{col}',[point(a,b,lift),point(aa,b,lift),point(aa,bb,lift),point(a,bb,lift)],
                        'roof_light' if (row*7+col*3)%11<3 else 'roof','roof')
            beam(name+'_rowlip',point(a0,bb,.038),point(a1,bb,.038),.035,'roof','roof')
        beam(name+'_fascia',point(a0,end),point(a1,end),.18,'trim')
        for a in [a0,a1]: beam(name+'_barge',point(a,center),point(a,end),.19,'trim')
    beam(name+'_ridge',point(a0,center,.12),point(a1,center,.12),.22,'roof','roof')
    return point

def shell(name,cx,zfront,w,d,eave=4.8,ridge=7.5):
    box(name+'_wall',(cx,(.6+eave)/2,zfront-d/2),(w,eave-.6,d),'wall')
    box(name+'_foundation',(cx,.3,zfront-d/2),(w+.16,.6,d+.16),'stone')
    # Broad face blocks only; final production removes unseen faces.
    for y in [.16,.46]:
        for x in np.arange(cx-w/2+.42,cx+w/2,.85):
            box(name+'_stone_front',(float(x),y,zfront+.065),(.80,.27,.12),'stone_light','foundation')
            box(name+'_stone_rear',(float(x),y,zfront-d-.065),(.80,.27,.12),'stone_light','foundation')
    for x in [cx-w/2,cx+w/2]:
        for z in [zfront,zfront-d]: box(name+'_corner',(x,(eave+.6)/2,z),(.22,eave-.6,.22),'trim','trim')
    return roof(name,cx,zfront,w,d,eave,ridge)

def skylight(name,x,w,d,eave,ridge,z=-1.65):
    mid=-d/2;slope=(ridge-eave)/(d/2)
    def p(xx,zz,lift=.12): return (xx,eave+(ridge-eave)*(1-abs(zz-mid)/(d/2))+lift,zz)
    ww=1.3; dd=1.65
    v=[p(x-ww/2,z-dd/2),p(x+ww/2,z-dd/2),p(x+ww/2,z+dd/2),p(x-ww/2,z+dd/2)]
    surface(name+'_pane',v,'window','windows')
    for i in range(4): beam(name+'_frame',v[i],v[(i+1)%4],.13,'trim','windows')
    beam(name+'_mullion',p(x,z-dd/2,.16),p(x,z+dd/2,.16),.08,'trim','windows')

def door_and_porch(pw,pd,pe,pr):
    box('door_panel',(0,2.6,.04),(2,4,.1),'wood','entry')
    window('door_glass',(0,3.35,.115),1.2,1.35,flowers=False)
    for x in [-1.12,1.12]: box('door_jamb',(x,2.6,.12),(.2,4.2,.2),'trim','entry')
    box('door_lintel',(0,4.69,.13),(2.45,.18,.22),'trim','entry')
    box('door_handle',(.72,2.3,.2),(.09,.23,.09),'metal','entry')
    box('porch_platform',(0,.3,pd/2),(pw,.6,pd),'stone_light','entry')
    for i in range(3):
        height=.6-.2*i
        box('step'+str(i),(0,height/2,pd+.25+.5*i),(pw-.35,height,.5),'stone_light','entry')
    for x in [-pw/2+.2,pw/2-.2]:
        box('porch_post',(x,(pe+.6)/2,pd-.18),(.24,pe-.6,.24),'trim','entry')
        box('post_base',(x,.88,pd-.18),(.56,.56,.56),'stone','entry')
        beam('porch_brace',(x,pe-.65,pd-.18),(x+(-.7 if x>0 else .7),pe,pd-.18),.17,'trim')
    roof('porch',0,pd,pw,pd+.8,pe,pr,'Z',.25,5)
    box('porch_front_beam',(0,pe-.08,pd),(pw,.2,.22),'trim','entry')
    beam('porch_gable_kingpost',(0,pe,pd+.1),(0,pr-.12,pd+.1),.16,'trim')
    for x in [-pw*.26,pw*.26]:
        beam('porch_gable_brace',(x,pe+.06,pd+.1),(0,pr-.22,pd+.1),.13,'trim')
    # Light to the left of the doorway, below porch beams.
    box('lantern_frame',(-1.55,3.75,.38),(.28,.45,.25),'metal','decor')
    box('lantern_core',(-1.55,3.75,.52),(.19,.28,.03),'window','decor')
    for x in [-pw/2+.17,pw/2-.17]: pot('porch_pot',(x,.6,.65))
    if pw>4:
        for x in [-pw/2+.25,pw/2-.25]:
            beam('hanging_chain',(x,pe-.2,pd-.28),(x,pe-1.05,pd-.28),.035,'metal','decor')
            cylinder('hanging_pot',(x,pe-1.18,pd-.28),.24,.3,'pot')
            foliage('hanging_flowers',(x,pe-1.0,pd-.28),.65)

def chimney(w,d,ridge):
    x=w*.21; z=-d*.52
    box('chimney_stack',(x,ridge+.55,z),(.9,1.6,.9),'brick','chimney')
    for yy in [ridge+.05,ridge+.4,ridge+.75,ridge+1.1]:
        box('brick_course',(x,yy,z+.455),(.94,.035,.035),'stone','chimney')
    box('chimney_cap',(x,ridge+1.38,z),(1.14,.22,1.14),'stone_light','chimney')
    box('chimney_flue',(x,ridge+1.66,z),(.55,.35,.55),'metal','chimney')

def glass_bay(w):
    # Shared wall at x=w/2; five external facets, no duplicate transparent overlapping panes.
    x=w/2
    poly=[(x,-.7),(x+2.1,-.7),(x+3.45,-1.8),(x+3.45,-5.4),(x+2.1,-6.5),(x,-6.5)]
    base,eave=.65,4.55
    # closed foundation top/facets
    surface('bay_floor',[(a,base,b) for a,b in poly],'stone_light','sunroom')
    peak=(x+.75,6.45,-3.6)
    for i in range(len(poly)-1):
        a,z=poly[i]; b,zz=poly[i+1]
        surface('bay_foundation',[(a,0,z),(b,0,zz),(b,base,zz),(a,base,z)],'stone','sunroom')
        surface('bay_glass',[(a,base,z),(b,base,zz),(b,eave,zz),(a,eave,z)],'glass','sunroom')
        surface('bay_glass_roof',[peak,(a,eave,z),(b,eave,zz)],'glass','sunroom')
        beam('bay_roof_hip',peak,(a,eave,z),.14,'trim','sunroom')
        beam('bay_eave',(a,eave,z),(b,eave,zz),.18,'trim','sunroom')
        for t in [0,.5,1]:
            xx=a+(b-a)*t; zzz=z+(zz-z)*t
            beam('bay_mullion',(xx,base,zzz),(xx,eave,zzz),.12,'trim','sunroom')
        for yy in [2.1,3.4]: beam('bay_transom',(a,yy,z),(b,yy,zz),.1,'trim','sunroom')
    return poly

configs=[('Lv1',[1],9,7.5,7.2,3.8,2.2,4.45,5.9),
         ('Lv2',[2],13,8.5,7.5,5.8,2.8,4.55,6.35),
         ('Lv3-4',[3,4],9.6,8.5,7.5,4.4,2.4,4.5,6.1),
         ('Lv5',[5],15,8.5,7.5,6.6,3.0,4.6,6.5)]
for key,levels,w,d,ridge,pw,pd,pe,pr in configs:
    parts=[]
    shell('main',0,0,w,d,4.8,ridge)
    door_and_porch(pw,pd,pe,pr)
    chimney(w,d,ridge)
    frontxs=[-w*.31] if key=='Lv1' else [-w*.33,w*.33]
    if key=='Lv3-4': frontxs=[-3.2]
    for i,x in enumerate(frontxs): window('front_window'+str(i),(x,2.85,.045))
    # Left/rear not visible in references: explicitly proposed, no extra entry doors.
    for i,z in enumerate([-d*.3,-d*.73]): window('left_window'+str(i),(-w/2-.045,2.85,z),side=True,flowers=False)
    for i,x in enumerate([-w*.27,w*.27]): window('rear_window'+str(i),(x,2.85,-d-.045),flowers=False,rear=True)
    if key not in ['Lv3-4','Lv5']: window('right_window',(w/2+.045,2.85,-d*.53),side=True)
    # Small attic vent at each main end gable.
    for sign in [-1,1]:
        box('gable_vent',(sign*(w/2+.035),5.7,-d/2),(.07,.72,.9),'metal','windows')
        for yy in [5.45,5.65,5.85,6.05]: box('vent_louver',(sign*(w/2+.09),yy,-d/2),(.13,.06,1.0),'trim','windows')
    extra={}
    if key=='Lv2': skylight('skylight_right',4.0,w,d,4.8,ridge)
    if key=='Lv3-4':
        wingw,wingd=4.2,6.8; wingx=w/2+wingw/2-.1
        shell('right_wing',wingx,-.5,wingw,wingd,4.15,5.95)
        window('wing_front',(wingx,2.6,-.455),1.75,1.65)
        window('wing_right',(wingx+wingw/2+.045,2.6,-3.8),1.75,1.65,True)
        window('wing_rear',(wingx,2.6,-7.345),1.75,1.65,flowers=False,rear=True)
        box('dormer_wall',(0,6.27,-2.12),(2.6,1.5,2.24),'wall','dormer')
        roof('dormer',0,-1,2.6,2.24,7.02,8.05,'Z',.17,4)
        window('dormer_window',(0,6.3,-.945),1.5,1.1)
        extra={'wing':{'centerX':wingx,'width':wingw,'depth':wingd,'frontZ':-.5,'eaveY':4.15,'ridgeY':5.95},
               'dormer':{'width':2.6,'depth':2.24,'frontZ':-1,'eaveY':7.02,'ridgeY':8.05}}
    if key=='Lv5':
        skylight('skylight_left',-5,w,d,4.8,ridge)
        skylight('skylight_right',5,w,d,4.8,ridge)
        extra={'sunroom':{'footprintXZ':glass_bay(w),'floorY':.65,'eaveY':4.55,'peakXYZ':[w/2+.75,6.45,-3.6]}}
        # Compact static porch chair, left side with entry lane kept open.
        box('porch_chair_seat',(-2.0,1.15,1.25),(.9,.15,.9),'wood','decor')
        box('porch_chair_back',(-2.0,1.65,.8),(.9,.95,.12),'wood','decor')
        for x in [-2.36,-1.64]:
            for z in [.9,1.6]: box('chair_leg',(x,.85,z),(.1,.5,.1),'wood','decor')
    # Stable identifiers are required because repeated detail labels are convenience generator names.
    for i,p in enumerate(parts): p['id']=f'{key}_{i:04d}'
    MODELS.append({'id':key,'levels':levels,'uniformScale':1,'mainBodyWD':[w,d],
                   'wallFrontZ':0,'eaveY':4.8,'ridgeY':ridge,'roofSlopeDegrees':math.degrees(math.atan((ridge-4.8)/(d/2))),
                   'porch':{'width':pw,'depth':pd,'eaveY':pe,'ridgeY':pr},
                   'door':{'clearWH':[2,4],'bottomY':.6,'centerXYZ':[0,2.6,.04]},
                   'entry':{'thresholdXYZ':[0,.6,.2],'interactionXYZ':[0,.6,pd+.1],
                            'approachXYZ':[0,0,pd+2.4],'reservedLaneWidth':2.4},
                   'additionalFeatures':extra,'parts':parts})

def faces_for(model):
    faces=[]
    for p in model['parts']:
        mat=p['material']
        if p['type']=='surface': faces.append((np.array(p['verticesXYZ'],float),mat));continue
        if p['type']=='box':
            c=np.array(p['centerXYZ']);e=np.array(p['sizeXYZ'])/2
            v=[c+np.array(q)*e for q in itertools.product([-1,1],repeat=3)]
        else:
            a=np.array(p['fromXYZ']);b=np.array(p['toXYZ']);axis=(b-a)/np.linalg.norm(b-a)
            ref=np.array([0,1,0]) if abs(axis[1])<.9 else np.array([0,0,1])
            side=np.cross(axis,ref);side/=np.linalg.norm(side);up=np.cross(axis,side)
            v=[q+side*s*p['width']/2+up*t*p['depth']/2 for q in [a,b] for s,t in itertools.product([-1,1],repeat=2)]
        for idx in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:
            faces.append((np.array([v[i] for i in idx]),mat))
    return faces

for m in MODELS:
    vs=np.concatenate([f[0] for f in faces_for(m)])
    lo=vs.min(axis=0);hi=vs.max(axis=0)
    m['visualBounds']={'minXYZ':lo.tolist(),'maxXYZ':hi.tolist(),'sizeWDH':[float(hi[0]-lo[0]),float(hi[2]-lo[2]),float(hi[1]-lo[1])]}
    m['sketchPrimitiveTriangles']=sum(len(v)-2 for v,_ in faces_for(m))
    m['budgetTriangles']={'LOD0Target':{'Lv1':4800,'Lv2':6000,'Lv3-4':7600,'Lv5':8500}[m['id']],
                         'LOD0Limit':{'Lv1':6000,'Lv2':7500,'Lv3-4':9500,'Lv5':11000}[m['id']],
                         'LOD1Target':{'Lv1':2200,'Lv2':2800,'Lv3-4':3500,'Lv5':4000}[m['id']],
                         'LOD2Target':{'Lv1':650,'Lv2':800,'Lv3-4':1100,'Lv5':1300}[m['id']]}
    allocations={'Lv1':[900,1200,1100,1100,500,0], 'Lv2':[1100,1500,1350,1550,500,0],
                 'Lv3-4':[1500,2200,1500,1850,550,0], 'Lv5':[1200,1700,1700,1600,600,1700]}
    m['budgetTriangles']['LOD0Allocation']=dict(zip(['shellFoundation','roof','trimWindows','attachedDecor','chimneyLantern','sunroom'],allocations[m['id']]))
    m['roofConstruction']={'mainRowsPerSlope':7,'columnPitchApproxMeters':.82,'rowLipMeters':.035,
                           'mainOverhangMeters':.4,'porchOverhangMeters':.25,'bevelMeters':.025,'bevelSegments':1}
    m['sketchGroupCounts']={g:sum(p['group']==g for p in m['parts']) for g in sorted({p['group'] for p in m['parts']})}

spec={'status':'PROPOSED_NOT_MODELLED_NOT_APPLIED','date':'2026-10-12',
      'coordinateSystem':{'unit':'meter','right':'+X','up':'+Y','front':'+Z','origin':'main front wall center at ground',
                          'blenderConversion':'Blender (x,y,z) = Unity (x,-z,y); front Blender -Y. Verify FBX front +Z after import.'},
      'playerReferenceHeight':3.6,'levelToModel':{'1':'Lv1','2':'Lv2','3':'Lv3-4','4':'Lv3-4','5':'Lv5'},
      'siteReservation':{'minXZ':[-9.5,-10],'maxXZ':[12.5,7],'widthDepth':[22,17],
                         'centerXZ':[1.5,-1.5],'doorAnchorXZ':[0,0],'currentDataFootprintCells':[6,5],
                         'cellCalculation':'ceil(reservedWorldSize / actual FieldArea.CellSize); allow asymmetric origin offset'},
      'materials':{'atlasPath':str(ATLAS.relative_to(ROOT)).replace('\\','/'),'atlasSha256':hashlib.sha256(ATLAS.read_bytes()).hexdigest(),
                   'atlasDimensions':[2048,2048],'atlasGuid':'522fcb2b5717c7640b460ee9ff73a78a','swatches':swatches,
                   'opaque':'TFP_Atlas_1A.mat','emission':'TFP_Atlas_Lights_1A.mat','glass':'TFP_Glass_Windows_1B.mat',
                   'glassTexture':'TFP_Glass_Windows_1B.psd','glassTextureGuid':'d961c196f859dee448ef832d1c0da9e8',
                   'productionSlotByPalette':{k:(2 if k=='glass' else 1 if k=='window' else 0) for k in swatches},
                   'maximumMaterialSlots':3,'newRuntimeTextures':0},'models':MODELS}
(OUT/'house-proposal-A.json').write_text(json.dumps(spec,indent=2),encoding='utf-8')

def render(m,cam,size=(1200,1000),same_scale=None,top=False):
    w,h=size;look=np.array(cam,float);look/=np.linalg.norm(look)
    if top: right=np.array([1.,0,0]);up=np.array([0.,0,-1.])
    else:
        right=np.cross([0,1,0],look);right/=np.linalg.norm(right);up=np.cross(look,right)
    faces=faces_for(m);allvs=np.concatenate([v for v,_ in faces]);xs=allvs@right;ys=allvs@up
    vertical_margin=280 if h>=1000 else 180
    scale=same_scale or min((w-120)/(xs.max()-xs.min()),(h-vertical_margin)/(ys.max()-ys.min()))
    center=(xs.min()+xs.max())/2;centery=(ys.min()+ys.max())/2
    pixels=np.full((h,w,3),BG,dtype=np.uint8);depth=np.full((h,w),-1e20)
    for vs,mat in faces:
        n=np.cross(vs[1]-vs[0],vs[2]-vs[0]);n/=max(1e-10,np.linalg.norm(n))
        # Flat schematic illumination, not an in-game shader preview.
        col=np.clip(COLORS[mat]*(.76+.24*abs(n[1])),0,255).astype(np.uint8)
        coords=np.column_stack((w/2+(vs@right-center)*scale,h/2-(vs@up-centery)*scale,vs@look))
        for k in range(1,len(coords)-1):
            a,b,c=coords[[0,k,k+1]]
            x0=max(0,int(math.floor(min(a[0],b[0],c[0]))));x1=min(w-1,int(math.ceil(max(a[0],b[0],c[0]))))
            y0=max(0,int(math.floor(min(a[1],b[1],c[1]))));y1=min(h-1,int(math.ceil(max(a[1],b[1],c[1]))))
            den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
            if abs(den)<1e-9 or x1<x0 or y1<y0: continue
            xx,yy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
            aa=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
            bb=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;cc=1-aa-bb
            zz=aa*a[2]+bb*b[2]+cc*c[2];existing=depth[y0:y1+1,x0:x1+1]
            mask=(aa>=-1e-5)&(bb>=-1e-5)&(cc>=-1e-5)&(zz>existing+1e-7)
            existing[mask]=zz[mask];pixels[y0:y1+1,x0:x1+1][mask]=col
    im=Image.fromarray(pixels)
    # Thin shape edges from depth differences, improves legibility of palette surfaces.
    occupied=depth>-1e19;edges=np.zeros((h,w),bool)
    edges[:,1:] |= occupied[:,1:] & (~occupied[:,:-1] | ((abs(depth[:,1:]-depth[:,:-1])>.10)&occupied[:,:-1]))
    edges[1:,:] |= occupied[1:,:] & (~occupied[:-1,:] | ((abs(depth[1:,:]-depth[:-1,:])>.10)&occupied[:-1,:]))
    arr=np.array(im);arr[edges]=(arr[edges]*.68).astype(np.uint8)
    return Image.fromarray(arr)

def dims(d,xy1,xy2,s):
    d.line([xy1,xy2],fill='#71858a',width=2)
    for x,y in [xy1,xy2]: d.line([(x-6,y-6),(x+6,y+6)],fill='#71858a',width=2)
    x=(xy1[0]+xy2[0])/2;y=(xy1[1]+xy2[1])/2
    bb=d.textbbox((0,0),s,font=font(20));ww=bb[2]
    d.rectangle((x-ww/2-7,y-14,x+ww/2+7,y+13),fill=BG)
    label(d,(x-ww/2,y-12),s,20)

def metric(m): return ' x '.join(f'{v:.2f}' for v in m['visualBounds']['sizeWDH'])+' m'

overview=Image.new('RGB',(2000,1630),BG);od=ImageDraw.Draw(overview)
label(od,(60,35),'RUMAH A / SKETSA EXTERIOR Lv1-5',42,True)
label(od,(60,92),'4 model unik. Lv3 dan Lv4 memakai mesh + scale yang sama. Ukuran usulan untuk review.',25)
for i,m in enumerate(MODELS):
    col=i%2;row=i//2;x=col*990+15;y=row*710+155
    tile=render(m,[1,.82,1],(970,575),same_scale=42)
    overview.paste(tile,(x,y));d=ImageDraw.Draw(overview)
    label(d,(x+40,y+5),m['id'],32,True)
    label(d,(x+40,y+553),'Batas visual W x D x H: '+metric(m),23,True)
    features={'Lv1':'Cottage kecil / teras / cerobong', 'Lv2':'Lebih lebar / teras besar / 1 skylight',
              'Lv3-4':'Dormer depan / sayap kanan / 1 model dipakai dua level',
              'Lv5':'Teras paling lebar / 2 skylight / ruang kaca kanan'}
    label(d,(x+40,y+589),features[m['id']],22)
    label(d,(x+40,y+621),f"Badan utama: {m['mainBodyWD'][0]:g} x {m['mainBodyWD'][1]:g} m / pintu: 2 x 4 m",22)
label(od,(60,1572),'Sketsa koordinat, bukan render Blender final. Semua level ditampilkan dengan skala gambar yang sama.',24)
overview.save(OUT/'house-levels-overview.png')

for m in MODELS:
    for name,cam in [('front-right',[1,.8,1]),('rear-left',[-1,.8,-1])]:
        im=render(m,cam,(1600,1250));d=ImageDraw.Draw(im)
        label(d,(45,25),f"RUMAH A / {m['id']} / "+('DEPAN + KANAN' if name=='front-right' else 'BELAKANG + KIRI (USULAN)'),34,True)
        label(d,(45,77),'Batas visual: '+metric(m)+' / depan Unity +Z / meter',24)
        label(d,(45,1182),'Acuan posisi: house-proposal-A.json. Material akhir mengikuti shader dan lighting game.',24)
        im.save(OUT/f"house-{m['id']}-{name}.png")
    sheet=Image.new('RGB',(2100,1660),BG);sd=ImageDraw.Draw(sheet)
    label(sd,(45,25),f"{m['id']} / EMPAT TAMPAK ORTHOGRAPHIC",36,True)
    label(sd,(45,75),'Kanan = sisi +X pada gambar depan. Dimensi W x D x H termasuk trim/dekor terpasang.',24)
    for i,(name,cam) in enumerate([('DEPAN +Z',[0,0,1]),('KANAN +X',[1,0,0]),('BELAKANG -Z',[0,0,-1]),('KIRI -X',[-1,0,0])]):
        xx=30+(i%2)*1040;yy=135+(i//2)*730
        sheet.paste(render(m,cam,(1000,630)),(xx,yy))
        label(sd,(xx+20,yy+4),name,26,True)
        ww=m['visualBounds']['sizeWDH'][0 if i%2==0 else 1]
        allv=np.concatenate([v for v,_ in faces_for(m)]);look=np.array(cam,float)
        right=np.cross([0,1,0],look);up=np.cross(look,right)
        vx=allv@right;vy=allv@up;sc=min(880/(vx.max()-vx.min()),450/(vy.max()-vy.min()))
        half=(vx.max()-vx.min())*sc/2
        dims(sd,(xx+500-half,yy+603),(xx+500+half,yy+603),f'{ww:.2f} m')
        hy=(vy.max()-vy.min())*sc/2
        dims(sd,(xx+500+half+45,yy+315-hy),(xx+500+half+45,yy+315+hy),f"{m['visualBounds']['sizeWDH'][2]:.2f} m")
        label(sd,(xx+20,yy+641),f"Tinggi keseluruhan: {m['visualBounds']['sizeWDH'][2]:.2f} m",22)
    label(sd,(45,1610),'Pintu depan saja untuk portal. Tidak ada pintu belakang/samping baru. Sisi tersembunyi adalah usulan.',23)
    sheet.save(OUT/f"house-{m['id']}-elevations.png")

    top=Image.new('RGB',(1500,1100),BG);td=ImageDraw.Draw(top)
    label(td,(40,25),f"{m['id']} / TAPAK, ENTRY & UKURAN KOMPONEN",32,True)
    # Plan uses exact wall polygons, no roof occlusion. Coordinate grid is production reference.
    xmin,xmax=m['visualBounds']['minXYZ'][0],m['visualBounds']['maxXYZ'][0]
    zmin=-m['mainBodyWD'][1]-.8;zmax=m['porch']['depth']+3
    sc=min(850/(xmax-xmin+2),780/(zmax-zmin));ox=500-(xmin+xmax)*sc/2;oy=130-zmin*sc
    def pt(x,z): return (ox+x*sc,oy+z*sc)
    def rect(x0,z0,x1,z1,fill):
        td.rectangle((*pt(x0,z0),*pt(x1,z1)),fill=fill,outline='#4c666b',width=3)
    w,dep=m['mainBodyWD'];pw=m['porch']['width'];pd=m['porch']['depth']
    for x in range(math.floor(xmin)-1,math.ceil(xmax)+2): td.line([pt(x,zmin),pt(x,zmax)],fill='#e5e3d9')
    for z in range(math.floor(zmin),math.ceil(zmax)+1): td.line([pt(xmin-1,z),pt(xmax+1,z)],fill='#e5e3d9')
    rect(-w/2,-dep,w/2,0,'#e5d5ad');label(td,pt(-w/2+.3,-dep+.35),f'UTAMA {w:g} x {dep:g} m',22,True)
    if 'wing' in m['additionalFeatures']:
        q=m['additionalFeatures']['wing'];cx=q['centerX'];ww=q['width'];zz=q['frontZ'];dd=q['depth']
        rect(cx-ww/2,zz-dd,cx+ww/2,zz,'#d7c89f');label(td,pt(cx-ww/2+.15,zz-dd+.4),'SAYAP',19,True)
    if 'sunroom' in m['additionalFeatures']:
        poly=m['additionalFeatures']['sunroom']['footprintXZ'];td.polygon([pt(*p) for p in poly],fill='#b7d1cb',outline='#4c666b',width=3)
        label(td,pt(w/2+.1,-4.1),'KACA',19,True)
    rect(-pw/2,0,pw/2,pd,'#d6d0bd');label(td,pt(-pw/2+.15,.25),'TERAS',20,True)
    rect(-pw/2+.175,pd,pw/2-.175,pd+1.5,'#e7deca')
    # Approach region clear of pots/chair/colliders, doorway fixed across all variants.
    a,b=pt(-1.2,.2),pt(1.2,pd+2.4)
    td.rectangle((*a,*b),outline='#459d79',width=3)
    td.ellipse((ox-6,oy-6,ox+6,oy+6),fill='#d95143')
    td.line([pt(0,pd+2.0),pt(0,.4)],fill='#459d79',width=4)
    label(td,pt(-1.0,pd+2.55),'DEPAN +Z',22,True)
    dims(td,pt(-w/2,-dep-.55),pt(w/2,-dep-.55),f'{w:g} m badan utama')
    lines=[f"Badan utama: {w:g} x {dep:g} m",f"Teras: {pw:g} x {pd:g} m",'Pintu clear: 2.00 x 4.00 m',
           'Lantai teras: Y +0.60 m','3 anak tangga @ tinggi 0.20 m','Depth tiap anak tangga: 0.50 m',
           f"Eave utama Y: {m['eaveY']:.2f} m",f"Ridge utama Y: {m['ridgeY']:.2f} m",f"Slope utama: {m['roofSlopeDegrees']:.2f} derajat",'Overhang utama: 0.40 m',
           'Origin merah: (0,0,0)','Lorong hijau: lebar 2.40 m',f"Approach: (0,0,{pd+2.4:g})",'Uniform scale: 1,1,1']
    for i,line in enumerate(lines): label(td,(1010,160+i*47),line,22,i==0)
    label(td,(40,1030),'Tapak ini + JSON menjadi acuan Blender. Atap/dekor dapat melewati batas dinding (lihat bounds visual).',23)
    top.save(OUT/f"house-{m['id']}-plan.png")

# Actual source atlas/material audit, not a speculative inventory.
audit=Image.new('RGB',(1800,1190),BG);ad=ImageDraw.Draw(audit)
label(ad,(40,25),'TOON FARM PACK / AUDIT PALET & MATERIAL RUMAH',34,True)
audit.paste(atlas.resize((650,650)),(45,130))
label(ad,(45,807),'TFP_Atlas_1A.psd / 2048 x 2048',24,True)
label(ad,(45,850),'Palet di kiri bawah. Tidak ada albedo shingle',21)
label(ad,(45,880),'atau stucco sesuai referensi; detail via geometri.',21)
for i,(name,s) in enumerate(swatches.items()):
    x=750+(i%2)*490;y=135+(i//2)*100
    ad.rectangle((x,y,x+78,y+58),fill=tuple(s['rgb']),outline='#71858a',width=1)
    label(ad,(x+92,y),name,23,True)
    label(ad,(x+92,y+32),f"RGB {s['rgb']} / px {s['pixelRectTopLeft'][:2]}",16)
label(ad,(45,1000),'Opaque: TFP_Atlas_1A.mat / CustomToon. Lamp core: TFP_Atlas_Lights_1A.mat / URP emission.',23)
label(ad,(45,1042),'Sunroom: TFP_Glass_Windows_1B.mat + existing glass PSD; alpha 0.188, transparent, ZWrite 0.',23)
label(ad,(45,1084),'Glass preview is schematic. UV glass uses its own pack texture. Max 3 slots / no new runtime texture.',23)
audit.save(OUT/'house-atlas-and-materials.png')

refs=[('Lv1','8d9dce2c-857e-4ecf-98cd-7bfd17cd293b'),('Lv2','eccd0a4b-5b53-4bc8-b6d8-f211eaf7e9a7'),
      ('Lv3-4','d81314cc-4df2-4521-ab8b-9b6f07035f56'),('Lv5','0c8488bb-40a9-4757-bd57-143079b05953')]
(OUT/'References').mkdir(exist_ok=True)
for name,guid in refs:
    src=Path('C:/Users/huxel/AppData/Local/Temp')/f'codex-clipboard-{guid}.png'
    dst=OUT/'References'/f'House-{name}-UserReference.png'
    if src.exists() and not dst.exists(): shutil.copy2(src,dst)
print(json.dumps([{'model':m['id'],'bounds':m['visualBounds']['sizeWDH'],'sketchTriangles':m['sketchPrimitiveTriangles'],
                   'parts':len(m['parts'])} for m in MODELS],indent=2))
