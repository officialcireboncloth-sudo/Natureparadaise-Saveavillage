"""Coop A: reproducible design drawings, not a Blender model or Unity mutation."""
from pathlib import Path
import json, math, hashlib, itertools, shutil
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
ATLAS = ROOT / 'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd'
atlas = Image.open(ATLAS).convert('RGB')
targets = {
 'wall': ('#eee3c5', 'Papan krem'), 'wall_side': ('#d6c9a6', 'Sisi papan'),
 'trim': ('#81949a', 'Trim biru abu'), 'trim_dark': ('#52676e', 'Sisi trim'),
 'roof': ('#554d43', 'Shingle coklat charcoal'), 'roof_dark': ('#39372f', 'Celah atap'),
 'stone': ('#b0a094', 'Batu fondasi'), 'stone_light': ('#c5b8a1', 'Variasi batu'),
 'metal': ('#353532', 'Engsel / rongga gelap'), 'wire': ('#93886b', 'Kawat sederhana'),
 'glow': ('#ffd355', 'Core lampu hangat')}
swatches = {}
for key, (h, label) in targets.items():
 target = tuple(int(h[i:i+2],16) for i in (1,3,5))
 cells = [(sum((atlas.getpixel((c*32+16,1024+r*64+32))[k]-target[k])**2 for k in range(3)),c,r) for r in range(16) for c in range(32)]
 _,c,r = min(cells); x,y=c*32,1024+r*64; color=atlas.getpixel((x+16,y+32))
 swatches[key] = dict(label=label,pixelRectTopLeft=[x,y,32,64],samplePixel=[x+16,y+32],rgb=list(color),hex='#'+''.join(f'{v:02x}' for v in color),uvSafeRectBottomLeft=[(x+4)/2048,1-(y+60)/2048,(x+28)/2048,1-(y+4)/2048])
C = {k:v['hex'] for k,v in swatches.items()}
parts=[]
def box(name, center, size, mat, facade='all', note=''):
 parts.append(dict(name=name,type='box',centerXYZ=list(center),sizeXYZ=list(size),material=mat,facade=facade,note=note))
def panel(name, vertices, mat, facade='all', note=''):
 parts.append(dict(name=name,type='surface',verticesXYZ=vertices,material=mat,facade=facade,note=note))
def beam(name,a,b,width,depth,mat='trim',facade='all'):
 parts.append(dict(name=name,type='beam',fromXYZ=list(a),toXYZ=list(b),width=width,depth=depth,material=mat,facade=facade))

box('Foundation',(0,.225,0),(6,.45,7),'stone')
for f,half in [('front',3),('rear',3),('left',3.5),('right',3.5)]:
 for row in range(2):
  for k in range(math.ceil(2*half/.8)):
   a=-half+k*.8+(row%2)*.4;b=min(half,a+.77)
   if a>=half:continue
   y0=row*.225+.008;y1=(row+1)*.225-.008
   if f in ['front','rear']:
    z=-3.509 if f=='front' else 3.509;vs=[[a,y0,z],[b,y0,z],[b,y1,z],[a,y1,z]]
   else:
    x=-3.009 if f=='left' else 3.009;vs=[[x,y0,a],[x,y0,b],[x,y1,b],[x,y1,a]]
   panel('StoneBlock_'+f+str(row)+'_'+str(k),vs,'stone_light' if k%3==0 else 'stone',f)
box('WallBody',(0,2.125,0),(6,3.35,7),'wall')
for f,z in [('front',-3.5),('rear',3.5)]:
 panel('Gable_'+f,[[-3,3.8,z],[3,3.8,z],[0,5.6,z]],'wall',f)
 box('GableRail_'+f,(0,3.72,z+(-.04 if z<0 else .04)),(6,.16,.12),'trim',f)
 beam('KingPost_'+f,(0,3.8,z),(0,5.5,z),.16,.12,'trim',f)
 for side in [-1,1]:
  # Fascia centreline inset from roof edge by half width along slope normal.
  nx=.6/math.sqrt(1.36)*.08; ny=1/math.sqrt(1.36)*.08
  zf=-3.82 if z<0 else 3.82
  beam('Fascia_'+f+str(side),(side*(3.4-nx),3.56-ny,zf),(side*(-nx),5.6-ny,zf),.16,.16,'trim',f)
for side in [-1,1]:
 x=side*3.4
 panel('Roof_'+str(side),[[0,5.6,-3.9],[x,3.56,-3.9],[x,3.56,3.9],[0,5.6,3.9]],'roof')
 box('Eave_'+str(side),(side*3.29,3.49,0),(.18,.14,7.8),'trim')
 for n,z in enumerate([-2.85,-.95,.95,2.85]):
  beam('Brace_'+str(side)+'_'+str(n),(side*3.04,3.1,z),(side*3.31,3.46,z),.12,.12)
box('RidgeCap',(0,5.67,0),(.20,.14,7.8),'roof')
for x,z in itertools.product([-2.89,2.89],[-3.39,3.39]):
 box('Post_'+str(x)+'_'+str(z),(x,2.125,z),(.22,3.35,.22),'trim')
# Thin wall seams are draft marks; production uses subdivided faces, not solid boards.
for f,z in [('front',-3.507),('rear',3.507)]:
 for n in range(1,24):
  x=-3+n*.25; high=3.8+1.8*(1-abs(x)/3)
  panel('BoardLine_'+f+str(n),[[x-.004,.45,z],[x+.004,.45,z],[x+.004,high,z],[x-.004,high,z]],'wall_side',f)
for f,x in [('left',-3.007),('right',3.007)]:
 for n in range(1,28):
  z=-3.5+n*.25
  panel('BoardLine_'+f+str(n),[[x,.45,z-.004],[x,.45,z+.004],[x,3.8,z+.004],[x,3.8,z-.004]],'wall_side',f)
# Roof grid/lips in drawings: same count and placement as the proposed final mesh.
for side in [-1,1]:
 for row in range(1,8):
  x=side*3.4*row/8; y=5.6-.6*abs(x)+.009
  panel('ShingleRow_'+str(side)+'_'+str(row),[[x,y,-3.9],[x+side*.012,y-.0072,-3.9],[x+side*.012,y-.0072,3.9],[x,y,3.9]],'roof_dark')
 for col in range(1,12):
  z=-3.9+7.8*col/12
  panel('ShingleColumn_'+str(side)+'_'+str(col),[[0,5.609,z-.005],[side*3.4,3.569,z-.005],[side*3.4,3.569,z+.005],[0,5.609,z+.005]],'roof_dark')

# Main door: one leaf, true clear width/height inside frame.
box('MainDoor',(-1.25,1.775,-3.56),(1.5,2.95,.10),'trim','front')
for x in [-2.09,-.41]:box('DoorJamb_'+str(x),(x,1.79,-3.65),(.18,3.16,.14),'trim','front')
for y in [.30,3.34]:box('DoorRail_'+str(y),(-1.25,y,-3.65),(1.86,.18,.14),'trim','front')
beam('DoorDiagonal',(-1.94,.52,-3.74),(-.56,3.06,-3.74),.14,.10,'trim','front')
box('DoorCentreRail',(-1.25,1.45,-3.74),(1.46,.14,.10),'trim','front')
for y in [.68,2.85]:box('DoorHinge_'+str(y),(-1.94,y,-3.80),(.32,.10,.05),'metal','front')
box('DoorHandle',(-.65,1.65,-3.8),(.07,.30,.10),'metal','front')
box('StepLower',(-1.25,.075,-3.95),(1.86,.15,.70),'stone','front')
box('StepUpper',(-1.25,.225,-3.77),(1.86,.15,.34),'stone','front')

# Window helper: U axis runs rightward on the chosen facade. Four wire windows total.
def window(name,facade,u,y,w,h):
 def xyz(a,b,c):
  if facade=='front':return(a,b,-3.5-c)
  if facade=='right':return(3+c,b,a)
  return(-3-c,b,a)
 def cb(n,a,b,c,su,sy,st,mat):
  size=(su,sy,st) if facade=='front' else (st,sy,su)
  box(name+n,xyz(a,b,c),size,mat,facade)
 cb('_Inset',u,y,.023,w,h,.025,'metal')
 for du in [-w/2-.07,w/2+.07]:cb('_Jamb'+str(du),u+du,y,.09,.14,h+.28,.12,'trim')
 for dy in [-h/2-.07,h/2+.07]:cb('_Rail'+str(dy),u,y+dy,.09,w+.28,.14,.12,'trim')
 cb('_HalfShutter',u-w/4,y,.14,w/2-.03,h-.04,.08,'trim')
 beam(name+'_Diagonal',xyz(u-w/2+.06,y-h/2+.06,.20),xyz(u-.07,y+h/2-.06,.20),.08,.06,'trim',facade)
 for dy in [-h*.32,h*.32]:cb('_Hinge'+str(dy),u-w/2+.08,y+dy,.205,.22,.07,.04,'metal')
 # Readable sparse diamond wire; four strips each direction, clipped to exposed half.
 left=u+.03;right=u+w/2-.02;bottom=y-h/2+.02;top=y+h/2-.02
 for orient in [-1,1]:
  for k in range(4):
   intercept=bottom-.42+k*.37
   candidates=[]
   for a in [left,right]:
    b=intercept+orient*(a-left)
    if bottom<=b<=top:candidates.append((a,b))
   for b in [bottom,top]:
    a=left+(b-intercept)/orient
    if left<=a<=right:candidates.append((a,b))
   if len(candidates)>=2:
    a,b=candidates[:2];pa=xyz(*a,.07);pb=xyz(*b,.07)
    # Flat 2-triangle ribbon; no solid cylinders per wire.
    eps=.009
    panel(name+'_Wire'+str(orient)+'_'+str(k),[xyz(a[0]-eps,a[1],.07),xyz(a[0]+eps,a[1],.07),xyz(b[0]+eps,b[1],.07),xyz(b[0]-eps,b[1],.07)],'wire',facade)
window('FrontWindow','front',1.45,2.50,1.45,1.20)
window('RightFrontWindow','right',-1.4,2.50,1.30,1.20)
window('RightRearWindow','right',1.55,2.50,1.30,1.20)
window('LeftWindow','left',0,2.50,1.45,1.20)

box('ChickenHatch',(1.45,.90,-3.56),(.78,.9,.10),'trim','front')
for x in [1.,1.9]:box('HatchJamb_'+str(x),(x,.9,-3.65),(.12,1.10,.14),'trim','front')
for y in [.39,1.41]:box('HatchRail_'+str(y),(1.45,y,-3.65),(1.02,.12,.14),'trim','front')
for y in [.63,1.15]:box('HatchHinge_'+str(y),(1.12,y,-3.76),(.18,.07,.04),'metal','front')
panel('ChickenRamp',[[.95,.45,-3.73],[1.95,.45,-3.73],[1.95,0,-4.8],[.95,0,-4.8]],'trim','front')
for n in range(1,6):
 t=n/6;z=-3.73-1.07*t;y=.45*(1-t)+.012
 panel('RampCleat_'+str(n),[[.98,y,z-.025],[1.92,y,z-.025],[1.92,y+.018,z+.025],[.98,y+.018,z+.025]],'trim_dark','front')

box('NestBox',(3.48,1.025,-1.4),(.84,.85,1.8),'trim','right')
panel('NestLid',[[3.05,1.63,-2.35],[3.95,1.35,-2.35],[3.95,1.35,-.45],[3.05,1.63,-.45]],'trim','right')
for z in [-2.05,-.75]:
 beam('NestSupport_'+str(z),(3.06,.50,z),(3.8,.88,z),.10,.10,'trim_dark','right')
 box('NestHinge_'+str(z),(3.08,1.64,z),(.14,.04,.20),'metal','right')
box('NestHandle',(3.72,1.44,-1.4),(.12,.05,.18),'metal','right')
box('RearVent',(0,4.35,3.54),(1.1,.65,.07),'metal','rear')
for y in [4.13,4.28,4.43,4.58]:box('VentSlat_'+str(y),(0,y,3.6),(1.1,.08,.08),'trim','rear')
for x in [-.62,.62]:box('VentJamb_'+str(x),(x,4.35,3.61),(.14,.93,.10),'trim','rear')
for y in [3.96,4.74]:box('VentRail_'+str(y),(0,y,3.61),(1.38,.14,.10),'trim','rear')
# Compact lantern fits between door header and gable rail.
box('LanternBracket',(-1.25,3.60,-3.66),(.08,.16,.23),'metal','front')
box('LanternFrame',(-1.25,3.55,-3.78),(.26,.30,.20),'metal','front')
box('LanternCore',(-1.25,3.55,-3.885),(.16,.20,.012),'glow','front')

lo=[-3.4,0,-4.8];hi=[3.95,5.74,3.9];dims=[hi[i]-lo[i] for i in range(3)]
levels=[]
for level,s in enumerate([.75,.875,1,1.125,1.25],1):
 levels.append(dict(level=level,uniformScale=s,bodyWidthDepth=[6*s,7*s],visualWidthDepthHeight=[round(dims[0]*s,5),round(dims[2]*s,5),round(dims[1]*s,5)],doorWidthHeight=[1.5*s,2.95*s],entranceMarkerXYZ=[-1.25*s,0,-4.65*s]))
budgetRows=[('Panel dinding dan gable',418),('Atap 2 x 8 x 12 sel + lip',768),('Fondasi 66 blok sederhana x 12',792),('Tiang, trim, frame, brace',520),('Pintu, shutter, hatch',240),('Kawat window dan vent',96),('Kotak sarang, ramp, tangga',160),('Lampu dan hardware',80)]
spec=dict(revision='coop-exterior-A-2026-10-11',status='PROPOSAL_ONLY_AWAITING_USER_REVIEW',units='metres; design decisions, not dimensions measured from reference',scope='Exterior Lv1-5 same shape/material/details; uniform scale only, as Barn precedent',coordinates=dict(unity='X right; Y up; Z rear; front -Z; origin ground centre',blender='X right; Y rear; Z up; front -Y; apply transforms before export'),master=dict(baseLevel=3,bodyWidth=6,bodyDepth=7,foundationTopY=.45,eaveY=3.8,ridgeY=5.6,maximumY=5.74,roofOverhang=.4,roofEaveOuterY=3.56,roofSlopeDegrees=math.degrees(math.atan(.6)),roofSlopeLength=math.hypot(3.4,2.04),roofSurfaceArea=2*7.8*math.hypot(3.4,2.04),visualBoundsMinXYZ=lo,visualBoundsMaxXYZ=hi,rampSlopeDegrees=math.degrees(math.atan2(.45,1.07)),rampLength=math.hypot(.45,1.07),nestLidSlopeDegrees=math.degrees(math.atan2(.28,.9))),levels=levels,materials=dict(atlasPath=str(ATLAS.relative_to(ROOT)).replace('\\','/'),atlasGuid='522fcb2b5717c7640b460ee9ff73a78a',atlasSize=list(atlas.size),sha256=hashlib.sha256(ATLAS.read_bytes()).hexdigest(),swatches=swatches,newRuntimeTextures=0,opaqueMaterial='TFP_Atlas_1A.mat',emissionMaterial='TFP_Atlas_Lights_1A.mat',maximumMaterialSlots=2,uvInsetPixels=4),parts=parts,detailRules=dict(wallBoardPitch=.25,wallGrooveDepth=.008,foundationCourses=2,foundationBlocksPerCourse=33,foundationBevelSegments=1,stoneJitterMaximum=.02,roofRowsPerSlope=8,roofColumnsAlongDepth=12,roofLipHeight=.02,roofMethod='subdivided planes and low-poly row lips, no solid tile per shingle',trimBevel=.02,wireMethod='sparse flat diamond lattice behind half shutter, not dense hex tubes or new alpha texture',windows=dict(front=1,right=2,left=1),rear='one gable louver, no rear door',doorLeafCount=1,levelDetailChanges=False),budget=dict(estimatedLOD0Triangles=sum(t for _,t in budgetRows),maximumLOD0Triangles=4200,targetLOD1Triangles=1600,targetLOD2Triangles=420,breakdown=[dict(component=n,triangles=t) for n,t in budgetRows],note='planning allowance, not measured final mesh count; final bevel/hidden-face removal must be audited'),integration=dict(mergedMeshPerLOD=True,lodCreation='Build authored simplifications in Blender; Unity LODGroup selects them, does not generate their mesh geometry',collider=dict(centerXYZ=[0,1.9,0],sizeXYZ=[6,3.8,7],nestBoxAdditionalBox=True,stepsRampsNonBlocking=True),humanPortalOnly=True,chickenHatch='decorative closed exterior in this scope; animal travel remains separate runtime concern',reservedLevel5WorldBoundsXZ=[-4.25,5.75,-8.125,4.875],reservedWidthDepth=[10,13],gridRule='ceil(world width or depth / actual FieldArea.CellSize)',currentGameplayLevels=[1,2,3,4],existingFootprintCells=[3,3],level5Balance='not specified: no invented price, days, capacity or interior changes'),reference=dict(image='reference-coop.png',frontRight='image-derived design language',leftRear='explicit proposal: left one central window, rear gable vent; same cream panels and foundation'),productionFiles=dict(source='ArtSource/Coop_A/Blender/Coop_A_Combined.blend',editableArchive='ArtSource/Coop_A/Blender/Coop_A_Master.blend',export='Assets/Nature  Paradaise/mesh/Buildings/Coop_A/Coop_A_High.fbx / Medium / Low',prefabs='Assets/Nature  Paradaise/Prefabs/Coop/Exterior/CoopExterior_Lv1..5.prefab'))
spec['master'].update(roofThickness=.06,wallShellThicknessInward=.16)
spec['detailRules'].update(lanternSides=8,stoneBevelInward=True)
spec['integration'].update(reservationCenterOffsetWorldXYZ=[.75,0,-1.625],placementRootRule='root = reservation anchor - yaw-rotated reservation-center offset')
(OUT/'coop-proposal-A.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')
ref=Path('C:/Users/huxel/AppData/Local/Temp/codex-clipboard-8f83ed78-6028-4fc7-a534-4336a2e9f4d4.png')
if ref.exists() and not (OUT/'reference-coop.png').exists():shutil.copyfile(ref,OUT/'reference-coop.png')

# All sketches are projections of the same part coordinates.
PAPER='#faf7ef';INK='#302e2b';MUTED='#766e62';ACCENT='#236b60'
def font(n,b=False):return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if b else 'arial.ttf'),n)
def text(d,xy,s,n=24,color=INK,b=False,anchor=None):d.text(xy,s,font=font(n,b),fill=color,anchor=anchor)
def faces(part):
 if part['type']=='surface':return [part['verticesXYZ']]
 if part['type']=='box':
  c=part['centerXYZ'];s=part['sizeXYZ'];v=[[c[i]+signs[i]*s[i]/2 for i in range(3)] for signs in itertools.product([-1,1],repeat=3)]
 else:
  a=part['fromXYZ'];b=part['toXYZ'];delta=[b[i]-a[i] for i in range(3)];length=math.sqrt(sum(v*v for v in delta));w=part['width']/2;t=part['depth']/2
  if abs(delta[2])<1e-9:normal=[-delta[1]/length,delta[0]/length,0];depth=[0,0,1]
  else:normal=[0,-delta[2]/length,delta[1]/length];depth=[1,0,0]
  v=[[end[i]+sn*w*normal[i]+sd*t*depth[i] for i in range(3)] for end in [a,b] for sn,sd in itertools.product([-1,1],repeat=2)]
 return [[v[i] for i in indices] for indices in [[0,1,3,2],[4,6,7,5],[0,4,5,1],[2,3,7,6],[0,2,6,4],[1,5,7,3]]]
def projection(v,view):
 x,y,z=v
 return dict(front=(x,-y),rear=(-x,-y),right=(-z,-y),left=(z,-y),top=(x,z),iso=(.82*x+.57*z,-y+.32*x-.46*z),back=(-.82*x-.57*z,-y-.32*x+.46*z))[view]
def depth(v,view):
 x,y,z=v
 return dict(front=z,rear=-z,right=-x,left=x,top=-y,iso=-x+z-y*.3,back=x-z-y*.3)[view]
def projected_faces(view):
 result=[]
 for p in parts:
  # Hidden facades excluded for orthographic drawings, to avoid false projections.
  if view in ['front','rear','left','right'] and p['facade'] not in ['all',view]:continue
  for f in faces(p):
   pts=[projection(v,view) for v in f]
   area=abs(sum(pts[i][0]*pts[(i+1)%len(pts)][1]-pts[(i+1)%len(pts)][0]*pts[i][1] for i in range(len(pts))))/2
   if area<1e-7:continue
   result.append(dict(name=p['name'],mat=p['material'],points=pts,depth=sum(depth(v,view) for v in f)/len(f)))
 # Orthographic roof projection has coplanar depths. Base first, overlay marks later.
 if view in ['front','rear','right','left','top']:
  shell=[f for f in result if f['name'] in ['Foundation','WallBody','Gable_front','Gable_rear']]
  bases=[f for f in result if f['name'].startswith('Roof_')]
  result=[f for f in result if not f['name'].startswith('Roof_') and f['name'] not in ['Foundation','WallBody','Gable_front','Gable_rear']]
  if view in ['right','left']:
   for f in result:
    if f['name'].startswith('BoardLine'):f['points']=[(x,max(y,-3.56)) for x,y in f['points']]
  return sorted(shell,key=lambda f:f['depth'],reverse=True)+bases+sorted(result,key=lambda f:f['depth'],reverse=True)
 return sorted(result,key=lambda f:f['depth'],reverse=True)
def draw_view(view,title,path,w=1600,h=1150):
 im=Image.new('RGB',(w,h),PAPER);d=ImageDraw.Draw(im)
 text(d,(55,32),'COOP A / SKETSA MASTER Lv3',32,b=True);text(d,(55,82),title,25)
 ff=projected_faces(view);pts=[v for f in ff for v in f['points']];xmin=min(v[0] for v in pts);xmax=max(v[0] for v in pts);ymin=min(v[1] for v in pts);ymax=max(v[1] for v in pts)
 scale=min((w-310)/(xmax-xmin),(h-430)/(ymax-ymin));ox=(w-(xmax-xmin)*scale)/2-xmin*scale;oy=170-ymin*scale
 for f in ff:
  d.polygon([(ox+x*scale,oy+y*scale) for x,y in f['points']],fill=C[f['mat']],outline=C['trim_dark'] if not f['name'].startswith(('BoardLine','Shingle')) else None,width=1)
 if view in ['front','rear','left','right']:
  body=6 if view in ['front','rear'] else 7
  dimension(d,(ox-body/2*scale,h-185),(ox+body/2*scale,h-185),f'{body:.2f} m badan')
  dimension(d,(w-150,oy),(w-150,oy-5.74*scale),'5.74 m',True)
 label={'front':'Pintu 1.50 x 2.95 m | window 1.45 x 1.20 m | hatch 0.78 x 0.90 m',
 'right':'2 window 1.30 x 1.20 m | nest box 1.80 m panjang / menonjol 0.95 m',
 'rear':'USULAN sisi tidak terlihat: satu vent gable 1.10 x 0.65 m; tanpa pintu',
 'left':'USULAN sisi tidak terlihat: satu window tengah 1.45 x 1.20 m',
 'iso':'Papan krem / trim biru abu / atap charcoal / batu / hatch + ramp / nest box',
 'back':'Kiri dan belakang ditentukan di proposal; tidak diklaim terlihat pada referensi',
 'top':'Atap 6.80 x 7.80 m | envelope 7.35 x 8.70 m termasuk nest box + ramp'}[view]
 text(d,(55,h-105),label,23)
 text(d,(55,h-64),'Ukuran usulan desain, bukan hasil ukur foto. Belum model final / belum apply Unity.',22,MUTED)
 im.save(OUT/path)
 return im
def dimension(d,a,b,label,vertical=False):
 d.line((a,b),fill=ACCENT,width=2)
 for p in [a,b]:d.line((p[0]-5,p[1]-5,p[0]+5,p[1]+5),fill=ACCENT,width=2)
 mid=((a[0]+b[0])/2,(a[1]+b[1])/2)
 text(d,(mid[0]+10,mid[1]) if vertical else (mid[0],mid[1]-10),label,22,ACCENT,anchor='lm' if vertical else 'mb')
views=[]
for view,label in [('front','DEPAN -Z'),('right','KANAN +X'),('rear','BELAKANG +Z'),('left','KIRI -X')]:views.append(draw_view(view,label,'coop-'+view+'-elevation.png'))
sheet=Image.new('RGB',(3200,2300),PAPER)
for n,im in enumerate(views):sheet.paste(im,((n%2)*1600,(n//2)*1150))
sheet.save(OUT/'coop-four-views.png')
draw_view('iso','VOLUME DEPAN / KANAN','coop-volume-front-right.png')
draw_view('back','VOLUME BELAKANG / KIRI','coop-volume-rear-left.png')
draw_view('top','TAMPAK ATAS / atap, ramp dan nest box','coop-top.png')
# Measured site plan instead of a misleading cut-through roof projection.
im=Image.new('RGB',(1600,1250),PAPER);d=ImageDraw.Draw(im)
text(d,(55,35),'COOP A / TAMPAK ATAS MASTER Lv3',32,b=True)
text(d,(55,85),'Depan -Z di bawah / pivot pusat badan (0,0,0)',24)
q=79;ox=780;oy=615
# Plot convention maps -Z to bottom, consistent with front facade label.
def top(x,z):return(ox+x*q,oy-z*q)
def rect(x0,z0,x1,z1,color):d.rectangle((*top(x0,z1),*top(x1,z0)),fill=color,outline=INK,width=2)
rect(-3.4,-3.9,3.4,3.9,C['roof'])
d.line((top(0,-3.9),top(0,3.9)),fill=C['trim'],width=11)
for r in range(1,8):
 for sign in [-1,1]:
  x=sign*3.4*r/8;d.line((top(x,-3.9),top(x,3.9)),fill=C['roof_dark'],width=2)
for k in range(1,12):
 z=-3.9+7.8*k/12;d.line((top(-3.4,z),top(3.4,z)),fill=C['roof_dark'],width=2)
# Dashed body outline records the wall footprint hidden below the roof.
for a,b in [((-3,-3.5),(3,-3.5)),((-3,3.5),(3,3.5)),((-3,-3.5),(-3,3.5)),((3,-3.5),(3,3.5))]:
 for t in range(24):
  if t%2:continue
  p0=top(a[0]+(b[0]-a[0])*t/24,a[1]+(b[1]-a[1])*t/24);p1=top(a[0]+(b[0]-a[0])*(t+1)/24,a[1]+(b[1]-a[1])*(t+1)/24)
  d.line((p0,p1),fill=C['wall'],width=3)
rect(.95,-4.8,1.95,-3.9,C['trim']);rect(-2.18,-4.3,-.32,-3.9,C['stone']);rect(3.4,-2.35,3.95,-.45,C['trim'])
d.ellipse((ox-6,oy-6,ox+6,oy+6),fill=ACCENT)
text(d,(1050,270),'BELAKANG +Z',23)
text(d,(1080,535),'Nest box',23);text(d,(1080,570),'+0.95 m dari dinding',22,MUTED)
text(d,(540,1038),'Pintu manusia',23);text(d,(860,1038),'Ramp ayam',23)
dimension(d,(top(-3.4,0)[0],190),(top(3.4,0)[0],190),'6.80 m atap')
dimension(d,(top(-3.4,0)[0],1100),(top(3.95,0)[0],1100),'7.35 m envelope termasuk nest box')
dimension(d,(380,top(0,3.9)[1]),(380,top(0,-4.8)[1]),'8.70 m',True)
text(d,(55,1180),'Garis putus krem: footprint badan 6 x 7 m. Envelope penuh termasuk ramp dan sarang.',23)
im.save(OUT/'coop-top.png')
# Five front silhouettes with one shared scale, no perspective size changes.
im=Image.new('RGB',(1700,850),PAPER);d=ImageDraw.Draw(im)
text(d,(55,35),'COOP A / PERBANDINGAN UKURAN Lv1-5',32,b=True)
text(d,(55,87),'Bentuk dan detail tetap sama. Setiap silhouette memakai skala gambar yang sama.',24)
ff=projected_faces('front');q=31;base=550
for n,l in enumerate(levels):
 s=l['uniformScale'];cx=200+n*325
 for f in ff:d.polygon([(cx+x*q*s,base+y*q*s) for x,y in f['points']],fill=C[f['mat']])
 text(d,(cx,600),f"Lv{l['level']} / {s:.3f}",24,b=True,anchor='mt')
 w,de=l['bodyWidthDepth'];text(d,(cx,645),f'{w:.2f} x {de:.2f} m badan',22,anchor='mt')
d.line((55,550,1645,550),fill=ACCENT,width=2)
text(d,(55,746),'Pintu dan hatch ikut uniform scale / Lv1 pintu 1.125 x 2.2125 m / Lv5 pintu 1.875 x 3.6875 m',24)
im.save(OUT/'coop-level-comparison.png')
im=Image.new('RGB',(1600,1000),PAPER);d=ImageDraw.Draw(im)
text(d,(55,35),'COOP A / SCALE Lv1-5 DAN TAPAK',32,b=True)
text(d,(55,105),'Level    Scale XYZ    Badan W x D         Visual W x D x H              Pintu W x H',25,b=True)
for n,l in enumerate(levels):
 w,de,h=l['visualWidthDepthHeight'];bw,bd=l['bodyWidthDepth'];dw,dh=l['doorWidthHeight'];y=165+n*76
 text(d,(55,y),f"Lv{l['level']}       {l['uniformScale']:.3f}         {bw:.2f} x {bd:.2f}            {w:.3f} x {de:.3f} x {h:.3f}         {dw:.3f} x {dh:.3f} m",25)
for n,line in enumerate(['Master Lv3: badan 6 x 7 m / ridge 5.60 m / tinggi maksimum 5.74 m',
 'Fondasi 0.45 m; overhang 0.40 m; slope atap 30.96 derajat',
 f"Luas bidang atap {spec['master']['roofSurfaceArea']:.2f} m2; ramp {spec['master']['rampLength']:.3f} m / {spec['master']['rampSlopeDegrees']:.2f} derajat",
 'Envelope tidak simetris: X -3.40..+3.95 / Z -4.80..+3.90 m',
 'Cadangan Lv5: 10 x 13 m, termasuk akses; pivot tetap pusat badan.',
 'Data gameplay saat ini Lv1-4 / footprint 3 x 3 sel. Lv5 belum ditentukan balancingnya.',
 'Satu desain / satu set mesh LOD / semua komponen ikut uniform scale.']):text(d,(55,590+n*48),line,24,MUTED if n==5 else INK)
im.save(OUT/'coop-levels-and-measurements.png')
im=Image.new('RGB',(1700,1320),PAPER);d=ImageDraw.Draw(im)
text(d,(55,32),'COOP A / PALET ASLI TOON FARM PACK',32,b=True)
crop=atlas.crop((0,1024,1024,2048));crop.thumbnail((540,540));im.paste(crop,(55,135))
for n,(key,s) in enumerate(swatches.items()):
 y=120+n*81;d.rectangle((650,y,706,y+50),fill=s['hex'],outline=INK)
 text(d,(730,y),s['label']+' / '+s['hex'],23,b=True)
 x,yy,ww,hh=s['pixelRectTopLeft'];u=s['uvSafeRectBottomLeft'];text(d,(730,y+32),f"px {x},{yy} | UV [{u[0]:.4f},{u[1]:.4f}] - [{u[2]:.4f},{u[3]:.4f}]",21,MUTED)
for n,line in enumerate(['PSD asli 2048 x 2048; tidak diubah. 0 texture runtime baru / maksimal 2 material.',
 'Papan, shingle, batu dan kawat dibuat sebagai geometri sederhana + UV palet.',
 'Kawat disederhanakan diamond lattice, bukan tabung hex padat atau alpha map baru.',
 'UV inset 4 px; body TFP_Atlas_1A / core lampu TFP_Atlas_Lights_1A.',
 'Audit palette ini dokumentasi; shading akhir mengikuti lighting dan shader Unity.']):text(d,(55,1060+n*43),line,23)
im.save(OUT/'coop-atlas-and-materials.png')
print(json.dumps(dict(parts=len(parts),atlasColours=C,estimatedTriangles=spec['budget']['estimatedLOD0Triangles'],roofArea=spec['master']['roofSurfaceArea']),indent=2))
