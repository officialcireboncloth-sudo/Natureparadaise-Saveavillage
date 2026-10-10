"""Dimensioned proposal drawings only. Does not build/import a Blender or Unity model."""
from pathlib import Path
import math, json, hashlib
from PIL import Image, ImageDraw, ImageFont

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
ATLAS=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd'
atlas=Image.open(ATLAS).convert('RGB')
font_path='C:/Windows/Fonts/arial.ttf'
bold_path='C:/Windows/Fonts/arialbd.ttf'
def font(n,b=False):return ImageFont.truetype(bold_path if b else font_path,n)
PAPER='#faf7ef';INK='#302e2b';MUTED='#766e62';LINE='#d7cfbf';ACCENT='#236b60'
targets={'red':('#bf3824','Cat papan merah'),'wood':('#eed8b4','Kayu krem / trim'),
         'roof':('#514c43','Atap charcoal hangat'),'stone':('#aaa08b','Batu fondasi'),
         'metal':('#383634','Besi gelap'),'glow':('#ffd355','Lampu hangat'),
         'red_dark':('#8f251d','Celah papan merah'),'wood_dark':('#ac7b44','Sisi kayu'),
         'roof_dark':('#343530','Celah atap'),'stone_light':('#c2b59a','Variasi batu')}
def rgb(h):return tuple(int(h[k:k+2],16) for k in (1,3,5))
swatches={}
for name,(hexcolor,label) in targets.items():
    target=rgb(hexcolor)
    cells=[(sum((atlas.getpixel((c*32+16,1024+r*64+32))[k]-target[k])**2 for k in range(3)),c,r)
           for r in range(16) for c in range(32)]
    _,c,r=min(cells);x,y=c*32,1024+r*64;color=atlas.getpixel((x+16,y+32))
    swatches[name]={'label':label,'pixelRectTopLeft':[x,y,32,64],
       'samplePixel':[x+16,y+32],'rgb':list(color),'hex':'#'+''.join(f'{v:02x}' for v in color),
       'uvSafeRectBottomLeft':[(x+4)/2048,1-(y+60)/2048,(x+28)/2048,1-(y+4)/2048]}
C={name:s['hex'] for name,s in swatches.items()}
levels=[]
for lv,s in enumerate([.75,.875,1,1.125,1.25],1):
    levels.append({'level':lv,'uniformScale':s,'bodySizeXYZ':[round(v*s,5) for v in [8,4.2,10]],
      'visualEnvelopeXYZ':[round(v*s,5) for v in [8.9,6.8,10.9]],
      'mainDoorClearWidthHeight':[round(v*s,5) for v in [3.4,3.2]],
      'entranceMarkerXYZ':[0,0,round(-5.9*s,5)]})
spec={'revision':'barn-exterior-A-2026-10-10','status':'PROPOSED_NOT_MODELLED_NOT_APPLIED',
 'scope':'Exterior only; same mesh, topology, materials, details and UVs for all five levels. Uniform scale only.',
 'units':'1 Unity unit = 1 m proposal; values are design choices, not measurements inferred from reference.',
 'coordinates':{'unity':'X right, Y up, Z rear. Front faces -Z. Origin ground center (0,0,0).',
  'blender':'X right, Y rear, Z up. Front -Y; ground-center origin. Export unit scale 1; apply mesh transforms before FBX.'},
 'master':{'baseLevel':3,'bodyWidth':8,'bodyDepth':10,'eaveY':4.2,'gableRidgeY':6.6,
  'maximumY':6.8,'foundationTopY':.5,'roofOverhangX':.45,'roofOverhangZ':.45,
  'roofThickness':.12,'roofSlopeDegrees':round(math.degrees(math.atan2(2.4,4)),4),
  'roofEaveOuterY':3.93,'visualBoundsMinXYZ':[-4.45,0,-5.45],'visualBoundsMaxXYZ':[4.45,6.8,5.45]},
 'levels':levels,'materials':{'opaque':'Reuse TFP_Atlas_1A.mat; one atlas, per-face UV palette islands.',
  'emission':'Reuse TFP_Atlas_Lights_1A.mat for lantern core only; same atlas texture.',
  'newRuntimeTextures':0,'newNormalAOMaps':0,'vertexColorRequired':False,
  'atlasPath':str(ATLAS.relative_to(ROOT)).replace('\\','/'),'atlasGuid':'522fcb2b5717c7640b460ee9ff73a78a',
  'atlasSize':[2048,2048],'sourceBytes':ATLAS.stat().st_size,'sha256':hashlib.sha256(ATLAS.read_bytes()).hexdigest(),
  'uvConvention':'Rects refer to PSD composite. Pixel origin top-left; UV origin bottom-left. Inset 4 px. No material-wide tint/repeat.',
  'swatches':swatches},
 'parts':[],
 'budget':{'estimatedLOD0Triangles':3200,'maximumLOD0Triangles':4500,'targetLOD1Triangles':1800,
  'targetLOD2Triangles':450,'opaqueMaterials':1,'optionalEmissionMaterials':1,
  'note':'Budgets, not measured mesh counts: no final 3D mesh has been made.'},
 'integration':{'masterMesh':'Barn_A_Master_LOD0.fbx','prefabs':'Five wrappers reference one shared master mesh and material set.',
  'level5ReservedWorldRectangle':[12,16],'placementGridRule':'ceil(reservedWorldSize / FieldArea.CellSize), not assumed 4x4.',
  'colliders':'Simple shell box, main-door approach trigger; no detailed stone/tile MeshCollider.',
  'portal':'Main front door only; side door and loft shutters decorative in this scope.',
  'existingLimitation':'Barn Building.asset and BarnInteriorSceneController currently define Lv1-4. Add actual Lv5 data at implementation; capacity/cost are outside this exterior sketch.'}}

parts=[]
def box(name,center,size,mat='wood',axis=(0,0,0),note=''):
    parts.append({'name':name,'type':'box','centerXYZ':list(center),'sizeXYZ':list(size),'rotationXYZDegrees':list(axis),'material':mat,'note':note})
def beam(name,a,b,width=.18,depth=.12,mat='wood'):
    parts.append({'name':name,'type':'beam','fromXYZ':list(a),'toXYZ':list(b),'width':width,'depth':depth,'material':mat})
def panel(name,verts,mat):parts.append({'name':name,'type':'surface','verticesXYZ':[list(p) for p in verts],'material':mat})
# Foundation remains closed visually; flat low threshold at front and side.
box('Foundation',(0,.25,0),(8,.5,10),'stone')
box('WallBody',(0,2.35,0),(8,3.7,10),'red')
panel('GableFront',[(-4,4.2,-5),(4,4.2,-5),(0,6.6,-5)],'red')
panel('GableRear',[(4,4.2,5),(-4,4.2,5),(0,6.6,5)],'red')
panel('RoofRight',[(0,6.6,-5.45),(4.45,3.93,-5.45),(4.45,3.93,5.45),(0,6.6,5.45)],'roof')
panel('RoofLeft',[(-4.45,3.93,-5.45),(0,6.6,-5.45),(0,6.6,5.45),(-4.45,3.93,5.45)],'roof')
box('RidgeCap',(0,6.7,0),(.28,.2,10.9),'roof')
for x in (-3.87,3.87):
 for z in (-4.87,4.87):box(f'CornerPost_{x}_{z}',(x,2.35,z),(.26,3.7,.26))
for z in (-5.07,5.07):
 box(f'HorizontalGableTrim_{z}',(0,4.14,z),(8.05,.22,.16))
 # Inset the fascia centerline by half its width, so its outer face stays
 # inside the same roof envelope; the dimensions include the trim geometry.
 nx=.6/math.sqrt(1+.6**2)*.11;ny=1/math.sqrt(1+.6**2)*.11
 for side in (-1,1):beam(f'GableFascia_{z}_{side}',(side*(4.45-nx),3.93-ny,z),(-side*nx,6.6-ny,z),.22,.20)
 beam(f'GableKingPost_{z}',(0,4.2,z),(0,6.48,z),.20,.18)
for x in (-4.05,4.05):
 box(f'EaveFascia_{x}',(x,4.12,0),(.18,.22,10.3))
 for n,z in enumerate((-4,-2,0,2,4)):
  beam(f'EaveBrace_{x}_{n}',(x,3.45,z),(x+(1 if x>0 else -1)*.28,3.91,z),.15,.15)
# Main opening dimensions are the visible free rectangle inside the trim.
box('MainDoorLeft',(-.855,1.78,-5.05),(1.69,3.2,.10),'red')
box('MainDoorRight',(.855,1.78,-5.05),(1.69,3.2,.10),'red')
for x in (-1.81,1.81):box(f'MainDoorJamb_{x}',(x,1.81,-5.13),(.22,3.46,.18))
for y in (.17,3.49):box(f'MainDoorLintel_{y}',(0,y,-5.13),(3.84,.22,.18))
for side in (-1,1):
 x0,x1=(.07,1.62) if side>0 else (-1.62,-.07)
 beam(f'DoorX_{side}_A',(x0,.32,-5.13),(x1,3.22,-5.13),.14,.12)
 beam(f'DoorX_{side}_B',(x1,.32,-5.13),(x0,3.22,-5.13),.14,.12)
 for y in (.55,3.02):box(f'MainHinge_{side}_{y}',(side*1.58,y,-5.235),(.38,.11,.06),'metal')
 box(f'MainHandle_{side}',(side*.13,1.62,-5.23),(.075,.32,.10),'metal')
box('MainThreshold',(0,.09,-5.2),(3.84,.18,.4),'stone')
# Loft shutters above the horizontal gable trim; no interior behind them.
box('LoftShutter',(0,5.05,-5.055),(1.8,1.4,.10),'wood_dark')
for x in (-1.0,0,1.0):box(f'LoftFrame_{x}',(x,5.05,-5.13),(.16,1.66,.12))
for y in (4.28,5.82):box(f'LoftRail_{y}',(0,y,-5.13),(2.16,.16,.12))
for side in (-1,1):beam(f'LoftDiagonal_{side}',(side*.87,4.43,-5.18),(side*.13,5.67,-5.18),.10,.08)
# Right elevation: one side door at z=+1.8, no extra wing or side extension.
box('SideDoor',(4.06,1.405,1.8),(.10,2.45,1.3),'wood_dark')
for z in (1.06,2.54):box(f'SideJamb_{z}',(4.14,1.44,z),(.16,2.72,.18))
for y in (.17,2.70):box(f'SideLintel_{y}',(4.14,y,1.8),(.16,.18,1.66))
beam('SideDoorDiagonal',(4.2,.38,1.25),(4.2,2.48,2.35),.12,.10)
box('SideThreshold',(4.2,.09,1.8),(.4,.18,1.66),'stone')
for y in (.60,2.28):box(f'SideHinge_{y}',(4.235,y,1.26),(.065,.11,.28),'metal')
# Left: two closed shutters; rear: single loft vent. Same on all levels.
for z in (-2.5,2.5):
 box(f'LeftShutter_{z}',(-4.06,2.55,z),(.10,1.2,1.2),'wood_dark')
 for dz in (-.69,.69):box(f'LeftWindowJamb_{z}_{dz}',(-4.14,2.55,z+dz),(.14,1.5,.18))
 for y in (1.89,3.21):box(f'LeftWindowRail_{z}_{y}',(-4.14,y,z),(.14,.15,1.55))
box('RearLoftVent',(0,4.95,5.08),(1.2,1.0,.12),'metal')
for x in (-.7,.7):box(f'RearVentJamb_{x}',(x,4.95,5.14),(.16,1.3,.13))
for y in (4.38,5.52):box(f'RearVentRail_{y}',(0,y,5.14),(1.56,.16,.13))
for y in (4.64,4.86,5.08,5.30):box(f'RearVentLouver_{y}',(0,y,5.19),(1.16,.10,.10),'wood_dark')
# Small lantern: 6-8 sided frame in final modelling, simplified box in technical draft.
box('LanternBracket',(0,3.77,-5.22),(.14,.18,.30),'metal')
box('LanternFrame',(0,3.67,-5.30),(.34,.48,.22),'metal')
box('LanternGlow',(0,3.67,-5.435),(.23,.30,.012),'glow',note='Core sits inside roof envelope; frontmost z=-5.441.')
spec['parts']=parts
spec['detailRules']={'wallBoardPitch':.25,'wallGrooveDepth':.008,'trimBevel':.025,'doorBraceBevel':.015,
 'stoneCourses':2,'stoneBlockWidthRange':[.55,.90],'stoneJitterMax':.03,
 'roofRowsPerSlope':10,'roofColumnsAlongDepth':16,'roofSeamWidth':.018,
 'roofRowLipHeight':.025,'roofMethod':'Low-poly subdivided plane; palette variation and raised row edges, not individual solid roof tiles.',
 'bracesPerSide':5,'frontDoorLeaves':2,'sideDoorCount':1,'leftShutters':2,'rearLoftVents':1,
 'smallDetails':'Large readable hinges/handles; omit nail mesh and microscopic wear. Do not add new props by level.'}
(OUT/'barn-proposal-A.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')

# Four measured elevations, all derived from the same master primitives.
def text(d,xy,s,n=24,color=INK,b=False,anchor=None):d.text(xy,s,font=font(n,b),fill=color,anchor=anchor)
def rotated_box(part):
 cx,cy,cz=part['centerXYZ'];sx,sy,sz=part['sizeXYZ'];return [(cx+a*sx/2,cy+b*sy/2,cz+c*sz/2) for a in (-1,1) for b in (-1,1) for c in (-1,1)]
def beam_poly(part):
 a,b=part['fromXYZ'],part['toXYZ'];dx,dy,dz=[b[k]-a[k] for k in range(3)];l=math.sqrt(dx*dx+dy*dy+dz*dz)
 if abs(dz)<1e-8:off=(-dy/l*part['width']/2,dx/l*part['width']/2,0)
 else:off=(0,-dz/l*part['width']/2,dy/l*part['width']/2)
 return [tuple(p[k]+m*off[k] for k in range(3)) for p,m in [(a,-1),(b,-1),(b,1),(a,1)]]
def project(p,view):
 x,y,z=p
 return {'front':(x,y),'rear':(-x,y),'right':(-z,y),'left':(z,y),'top':(x,z)}[view]
def dimension(d,a,b,label,vertical=False):
 d.line((a,b),fill=ACCENT,width=2)
 for p in (a,b):d.line((p[0]-6,p[1]-6,p[0]+6,p[1]+6),fill=ACCENT,width=2)
 mid=((a[0]+b[0])/2,(a[1]+b[1])/2)
 if vertical:text(d,(mid[0]+10,mid[1]),label,23,ACCENT,anchor='lm')
 else:text(d,(mid[0],mid[1]-13),label,23,ACCENT,anchor='mb')
def elevation(view,title):
 im=Image.new('RGB',(1500,1100),PAPER);d=ImageDraw.Draw(im);text(d,(65,40),'BARN A / MASTER Lv3',32,b=True);text(d,(65,85),title,26)
 scale=83 if view in ('right','left') else 96;ox=750;oy=850
 def p(pt):u,v=project(pt,view);return(ox+u*scale,oy-v*scale)
 # Profile base.
 half=4 if view in ('front','rear') else 5
 d.rectangle((ox-half*scale,oy-4.2*scale,ox+half*scale,oy),fill=C['red'])
 d.rectangle((ox-half*scale,oy-.5*scale,ox+half*scale,oy),fill=C['stone'])
 if view in ('front','rear'):d.polygon([p((-4,4.2,0)),p((0,6.6,0)),p((4,4.2,0))],fill=C['red'])
 # Board seams stop at gable slope.
 for k in range(int(half*8)+1):
  u=-half+k*.25;top=4.2+(2.4*(1-abs(u)/4) if view in ('front','rear') else 0)
  d.line((ox+u*scale,oy-.5*scale,ox+u*scale,oy-top*scale),fill=C['red_dark'],width=2)
 # Stone joint sketch.
 for row in range(2):
  y=.05+row*.225;d.line((ox-half*scale,oy-y*scale,ox+half*scale,oy-y*scale),fill=C['stone_light'],width=3)
  for k in range(int(half*2/.7)):
   u=-half+k*.7+(row%2)*.35;d.line((ox+u*scale,oy-(y+.21)*scale,ox+u*scale,oy-y*scale),fill=C['wood_dark'],width=2)
 # Side roofs show depth profile as a rectangle plus ridge.
 if view in ('right','left'):
  d.rectangle((ox-5.45*scale,oy-6.6*scale,ox+5.45*scale,oy-3.93*scale),fill=C['roof'])
  for r in range(1,10):
   y=3.93+(6.6-3.93)*r/10;d.line((ox-5.45*scale,oy-y*scale,ox+5.45*scale,oy-y*scale),fill=C['roof_dark'],width=2)
  for c in range(1,16):
   u=-5.45+10.9*c/16;d.line((ox+u*scale,oy-6.6*scale,ox+u*scale,oy-3.93*scale),fill=C['roof_dark'],width=1)
 for part in parts:
  name=part['name']
  if name in ('Foundation','WallBody') or name.startswith(('GableFront','GableRear','Roof')) or name=='RidgeCap':continue
  verts=rotated_box(part) if part['type']=='box' else beam_poly(part)
  # Show objects belonging to this facade. Corner posts are shared silhouette marks.
  if not name.startswith('CornerPost'):
   vals=[v[2] if view in ('front','rear') else v[0] for v in verts]
   if view=='front' and max(vals)>-4.95:continue
   if view=='rear' and min(vals)<4.95:continue
   if view=='right' and min(vals)<3.9:continue
   if view=='left' and max(vals)>-3.9:continue
  uv=[project(v,view) for v in verts]
  if part['type']=='box':
   x0=min(u for u,v in uv);x1=max(u for u,v in uv);y0=min(v for u,v in uv);y1=max(v for u,v in uv)
   d.rectangle((ox+x0*scale,oy-y1*scale,ox+x1*scale,oy-y0*scale),fill=C[part['material']],outline=C.get('wood_dark',INK),width=2)
  else:d.polygon([p(v) for v in verts],fill=C[part['material']],outline=C['wood_dark'])
 # Roof edge in gable elevation is not a flat wall polygon.
 if view in ('front','rear'):
  d.line([p((-4.45,3.93,0)),p((0,6.6,0)),p((4.45,3.93,0))],fill=C['roof'],width=17)
  d.rectangle((ox-.14*scale,oy-6.8*scale,ox+.14*scale,oy-6.6*scale),fill=C['roof'])
 else:d.rectangle((ox-5.45*scale,oy-6.8*scale,ox+5.45*scale,oy-6.6*scale),fill=C['roof'])
 dimension(d,(ox-half*scale,935),(ox+half*scale,935),f'{half*2:.2f} m badan')
 dimension(d,(ox-(4.45 if half==4 else 5.45)*scale,995),(ox+(4.45 if half==4 else 5.45)*scale,995),f'{8.9 if half==4 else 10.9:.2f} m termasuk atap')
 dimension(d,(1260,oy),(1260,oy-6.8*scale),'6.80 m',True)
 text(d,(65,1040),'PROPORSI DAN JUMLAH DETAIL SAMA UNTUK Lv1-5 / Scale XYZ seragam',22,MUTED)
 if view=='front':
  text(d,(65,170),'A  Pintu utama 3.40 x 3.20 m',23);text(d,(65,205),'B  Shutter loft 1.80 x 1.40 m',23)
 elif view=='right':text(d,(65,160),'Pintu samping 1.30 x 2.45 m / pusat Z = +1.80 m',23)
 elif view=='left':text(d,(65,160),'2 shutter tertutup 1.20 x 1.20 m / Z = -2.50 dan +2.50 m',23)
 else:text(d,(65,160),'Belakang: dinding merah + 1 vent loft 1.20 x 1.00 m; tanpa pintu tambahan',23)
 im.save(OUT/f'barn-{view}-elevation.png')
 return im
views=[elevation(v,t) for v,t in [('front','DEPAN / arah -Z'),('right','SAMPING KANAN / arah +X'),('rear','BELAKANG / arah +Z'),('left','SAMPING KIRI / arah -X')]]
sheet=Image.new('RGB',(3000,2200),PAPER)
for n,im in enumerate(views):sheet.paste(im,((n%2)*1500,(n//2)*1100))
sheet.save(OUT/'barn-four-views.png')

# Top plan + measurement summary.
im=Image.new('RGB',(1600,1250),PAPER);d=ImageDraw.Draw(im);text(d,(70,42),'BARN A / TAMPAK ATAS + SKALA LEVEL',34,b=True)
q=55;ox=430;oy=440
def top(x,z):return(ox+x*q,oy-z*q)
for w,de,col in [(8.9,10.9,C['roof']),(8,10,C['red'])]:d.rectangle((*top(-w/2,de/2),*top(w/2,-de/2)),fill=col,outline=INK,width=2)
d.line((*top(0,-5.45),*top(0,5.45)),fill=C['wood'],width=4)
d.rectangle((*top(-1.92,-5),*top(1.92,-5.4)),fill=C['stone'])
d.rectangle((*top(4,2.63),*top(4.4,.97)),fill=C['stone'])
text(d,(ox,870),'DEPAN -Z / pintu utama',24,anchor='mt')
dimension(d,(top(-4.45,0)[0],800),(top(4.45,0)[0],800),'8.90 m atap')
dimension(d,(80,top(0,5.45)[1]),(80,top(0,-5.45)[1]),'10.90 m',True)
text(d,(790,140),'1 MASTER MESH / 5 UNIFORM SCALES',27,b=True)
text(d,(790,195),'Level    Scale       Badan W x D           Atap W x D x H',23,b=True)
for n,l in enumerate(levels):
 s=l['uniformScale'];a=l['bodySizeXYZ'];b=l['visualEnvelopeXYZ'];y=248+n*68
 text(d,(790,y),f"Lv{l['level']}     {s:.3f}       {a[0]:.2f} x {a[2]:.2f} m",24)
 text(d,(790,y+29),f"                           {b[0]:.3f} x {b[2]:.3f} x {b[1]:.2f} m",22,MUTED)
text(d,(790,655),'Lv3 adalah ukuran master; pintu dan detail ikut scale.',23)
text(d,(790,690),'Tidak ada sayap, lantai, atau dekorasi baru per level.',23)
text(d,(790,755),'Sumbu / pivot untuk produksi',27,b=True)
for n,s in enumerate(['Unity: X kanan / Y atas / Z belakang.','Blender: X kanan / Y belakang / Z atas.','Origin: tengah tapak di tanah (0, 0, 0).','Penempatan: rencanakan area maksimum Lv5.','Cadangan site: 12 x 16 m, termasuk akses depan.']):text(d,(790,805+n*40),s,23)
text(d,(70,975),'DIMENSI MASTER Lv3',27,b=True)
for n,s in enumerate(['Badan 8.00 x 10.00 m | fondasi 0.50 m | lis eave 4.20 m | ridge struktur 6.60 m',
 'Batas visual 8.90 x 10.90 x 6.80 m | overhang 0.45 m pada setiap sisi | slope 30.96 derajat',
 'Portal depan (0, 0, -5.90) m; threshold 3.84 x 0.40 x 0.18 m; atap menentukan envelope.',
 'Ukuran ini USULAN, bukan ukuran yang diukur dari image; belum dibuat model atau di-apply.']):text(d,(70,1030+n*40),s,23,MUTED if n==3 else INK)
im.save(OUT/'barn-top-and-levels.png')

# Atlas audit: authentic existing palette crop and exact chosen UVs.
im=Image.new('RGB',(1700,1530),PAPER);d=ImageDraw.Draw(im);text(d,(65,42),'BARN A / AUDIT TEXTURE & MATERIAL',34,b=True)
text(d,(65,96),'TFP_Atlas_1A.psd / existing 2048 x 2048 / 0 texture runtime baru',24)
crop=atlas.crop((0,1024,1024,2048));crop.thumbnail((560,560));im.paste(crop,(65,170));text(d,(65,750),'Palet asli atlas (area X 0-1024, Y 1024-2048)',22,MUTED)
for n,(name,s) in enumerate(swatches.items()):
 y=170+n*84;d.rectangle((690,y,756,y+57),fill=s['hex'],outline=INK)
 text(d,(780,y),s['label']+' / '+s['hex'],23,b=True)
 x0,y0,w,h=s['pixelRectTopLeft'];uv=s['uvSafeRectBottomLeft']
 text(d,(780,y+32),f"px ({x0},{y0}) {w}x{h} | UV [{uv[0]:.4f},{uv[1]:.4f}] - [{uv[2]:.4f},{uv[3]:.4f}]",21,MUTED)
text(d,(65,1040),'TERSEDIA DI ATLAS',26,b=True)
for n,s in enumerate(['Warna merah, warna kayu krem/coklat, abu atap, batu, besi, kuning lampu.',
 'Material body: TFP_Atlas_1A.mat / shader CustomToon yang sudah dipakai pack.',
 'Material glow: TFP_Atlas_Lights_1A.mat / atlas yang sama. Maksimal 2 material.']):text(d,(65,1090+n*40),s,23)
text(d,(65,1240),'DETAIL YANG DIBUAT MELALUI GEOMETRI',26,b=True)
for n,s in enumerate(['Papan vertikal, frame, X-brace, sambungan batu, garis shingle, engsel dan handle.',
 'Atlas belum menyediakan gambar tileable khusus papan barn / shingles / dinding batu ini.',
 'Tidak perlu texture unik, normal map, AO map, atau duplikasi atlas pada setiap level.',
 'PNG audit ini hanya dokumentasi; material Unity tetap menunjuk PSD asli.']):text(d,(65,1290+n*38),s,23,MUTED if n==3 else INK)
im.save(OUT/'barn-atlas-and-materials.png')
print('Proposal generated:',len(parts),'draft components; five scale-only levels; four elevations + top plan + atlas audit.')
