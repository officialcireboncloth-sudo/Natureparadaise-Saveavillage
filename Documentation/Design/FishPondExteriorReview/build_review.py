"""Coordinate-based pond sketches only; no Blender execution or Unity asset edits."""
from pathlib import Path
import math,json,hashlib,shutil,itertools
import numpy as np
from PIL import Image,ImageDraw
import drawing as draw

OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
PACK=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack'
ATLAS=PACK/'Textures/TFP_Atlas_1A.psd'
atlas=Image.open(ATLAS).convert('RGB')
targets={'stone':(166,161,145),'stone_light':(204,193,164),'stone_dark':(124,126,116),
         'wood':(164,106,52),'wood_light':(197,144,75),'metal':(49,54,55),
         'machine':(82,140,126),'machine_light':(139,177,161),'grain':(237,161,60),
         'red':(214,84,46),'leaf':(97,141,43),'leaf_light':(142,175,64),'roof':(88,110,104)}
swatches={}
for name,target in targets.items():
    candidates=[(sum((int(atlas.getpixel((c*32+16,1024+r*64+32))[i])-target[i])**2 for i in range(3)),c,r) for r in range(16) for c in range(32)]
    _,c,r=min(candidates);x,y=c*32,1024+r*64
    swatches[name]={'rgb':list(atlas.getpixel((x+16,y+32))), 'pixelRectTopLeft':[x,y,32,64],
                    'safeUvBottomLeft':[(x+4)/2048,1-(y+60)/2048,(x+28)/2048,1-(y+4)/2048]}
draw.COLORS={k:np.array(v['rgb'],float) for k,v in swatches.items()}
draw.COLORS['water']=np.array([71,153,164],float) # schematic only, never atlas UV
draw.COLORS['water_light']=np.array([136,198,198],float)
parts=[]
def box(name,c,s,mat,group='structure'):
    parts.append(dict(name=name,type='box',centerXYZ=list(c),sizeXYZ=list(s),material=mat,group=group))
def surface(name,vs,mat,group='structure'):
    parts.append(dict(name=name,type='surface',verticesXYZ=[list(v) for v in vs],material=mat,group=group))
def beam(name,a,b,w,mat,group='structure',depth=None):
    parts.append(dict(name=name,type='beam',fromXYZ=list(a),toXYZ=list(b),width=w,depth=depth or w,material=mat,group=group))
def rounded(w,d,r,cx=1.5):
    pts=[]
    for x,z,start in [(w/2-r,d/2-r,0),(-w/2+r,d/2-r,90),(-w/2+r,-d/2+r,180),(w/2-r,-d/2+r,270)]:
        for t in [0,30,60,90]:
            a=math.radians(start+t);pts.append([cx+x+r*math.cos(a),z+r*math.sin(a)])
    return pts
def area(poly):
    return abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(poly,poly[1:]+poly[:1])))/2
def cylinder(name,c,r,h,mat,n=8,group='machines',axis='Y',r_top=None):
    rt=r if r_top is None else r_top;x,y,z=c
    if axis=='Y':lo=[(x+r*math.cos(i*math.tau/n),y-h/2,z+r*math.sin(i*math.tau/n)) for i in range(n)];hi=[(x+rt*math.cos(i*math.tau/n),y+h/2,z+rt*math.sin(i*math.tau/n)) for i in range(n)]
    elif axis=='Z':lo=[(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),z-h/2) for i in range(n)];hi=[(x+rt*math.cos(i*math.tau/n),y+rt*math.sin(i*math.tau/n),z+h/2) for i in range(n)]
    else:lo=[(x-h/2,y+r*math.cos(i*math.tau/n),z+r*math.sin(i*math.tau/n)) for i in range(n)];hi=[(x+h/2,y+rt*math.cos(i*math.tau/n),z+rt*math.sin(i*math.tau/n)) for i in range(n)]
    surface(name+'_bottom',lo,mat,group);surface(name+'_top',hi,mat,group)
    for i in range(n):surface(name+f'_side{i}',[lo[i],lo[(i+1)%n],hi[(i+1)%n],hi[i]],mat,group)
def stone(name,c,w,d,h,mat,group='rim'):
    x,y,z=c;outline=[(-.5,-.3),(-.3,-.5),(.3,-.5),(.5,-.3),(.5,.3),(.3,.5),(-.3,.5),(-.5,.3)]
    low=[(x+a*w,y-h/2,z+b*d) for a,b in outline]
    middle=[(x+a*w,y+h*.20,z+b*d) for a,b in outline]
    top=[(x+a*w*.72,y+h/2,z+b*d*.72) for a,b in outline]
    surface(name+'_cap',top,mat,group)
    for i in range(8):
        j=(i+1)%8;surface(name+f'_face{i}',[low[i],low[j],middle[j],middle[i]],mat,group)
        surface(name+f'_bevel{i}',[middle[i],middle[j],top[j],top[i]],'stone_light' if i%3==0 else mat,group)
def perimeter_samples(poly,count):
    seg=[math.dist(a,b) for a,b in zip(poly,poly[1:]+poly[:1])];total=sum(seg);result=[]
    for k in range(count):
        t=(k+.5)*total/count
        for i,length in enumerate(seg):
            if t<=length:
                a=poly[i];b=poly[(i+1)%len(poly)];u=t/length
                result.append((a[0]+u*(b[0]-a[0]),a[1]+u*(b[1]-a[1])));break
            t-=length
    return result,total
def feed_maker(x,z):
    for dx in [-.53,.53]:
        for dz in [-.4,.4]:box('maker_leg',(x+dx,.38,z+dz),(.15,.65,.15),'metal','maker')
    box('maker_body',(x,1.22,z),(1.45,1.45,1.1),'machine','maker')
    lo=[(x+a*.68,1.94,z+b*.62) for a,b in [(-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5)]]
    hi=[(x+a*1.60,2.78,z+b*1.34) for a,b in [(-.5,-.5),(.5,-.5),(.5,.5),(-.5,.5)]]
    for i in range(4):surface('maker_hopper'+str(i),[lo[i],lo[(i+1)%4],hi[(i+1)%4],hi[i]],'machine_light','maker')
    surface('maker_grain_top',[(a,2.72,b) for a,_,b in hi],'grain','maker')
    cylinder('maker_motor',(x-1.00,1.16,z),.43,.55,'metal',12,'maker','X')
    for k in range(5):cylinder('motor_ring'+str(k),(x-1.21+k*.105,1.16,z),.45,.026,'roof',12,'maker','X')
    cylinder('maker_outlet',(x,.89,z-.72),.24,.42,'metal',8,'maker','Z')
    box('maker_tray_floor',(x,.12,z-1.15),(1.00,.10,.76),'wood','maker')
    for dx in [-.47,.47]:box('maker_tray_side',(x+dx,.30,z-1.15),(.10,.38,.76),'wood_light','maker')
    for dz in [-.35,.35]:box('maker_tray_end',(x,.30,z-1.15+dz),(.95,.38,.10),'wood','maker')
    box('maker_pellets',(x,.22,z-1.15),(.81,.10,.60),'grain','maker')
    for dx,mat in [(-.32,'red'),(0,'grain'),(.32,'leaf')]:cylinder('maker_button',(x+dx,1.62,z-.57),.085,.055,mat,6,'maker','Z')
    cylinder('feed_sack',(x-1.15,.48,z-1.12),.37,.80,'wood_light',8,'maker',r_top=.29)
def barrel(x,z):
    cylinder('feed_barrel',(x,1.36,z),.40,.85,'wood',10,'storage')
    for y in [1.02,1.64]:cylinder('barrel_hoop',(x,y,z),.42,.08,'metal',10,'storage')
    cylinder('barrel_lid',(x,1.82,z),.43,.12,'machine',10,'storage',r_top=.34)
    cylinder('barrel_handle',(x,1.92,z),.065,.09,'wood_light',6,'storage')
def feeder(x,z,large=False):
    r=.47 if large else .42
    for dx in [-.34,.34]:
        for dz in [-.30,.30]:box('feeder_leg',(x+dx,1.23,z+dz),(.10,.68,.10),'wood','autoFeeder')
    box('feeder_base',(x,1.54,z),(1.06,.16,.96),'wood_light','autoFeeder')
    cylinder('feeder_body',(x,2.15,z),r,1.05,'machine_light',10,'autoFeeder')
    cylinder('feeder_bottom',(x,1.69,z),r+.015,.18,'machine',10,'autoFeeder')
    cylinder('feeder_lid',(x,2.79,z),r+.065,.16,'machine',10,'autoFeeder',r_top=r*.78)
    cylinder('feeder_knob',(x,2.93,z),.07,.10,'wood_light',6,'autoFeeder')
    cylinder('feeder_spout',(x,1.90,z+r+.19),.11,.48,'machine',8,'autoFeeder','Z')
    box('feeder_controls',(x,1.90,z-.50),(.32,.35,.08),'metal','autoFeeder')
    cylinder('feeder_button',(x,1.94,z-.55),.06,.04,'grain',6,'autoFeeder','Z')

MODELS=[]
for level,(w,d,r,capacity,site) in enumerate([(8,6,.95,50,(14,11)),(10,7.5,1.10,100,(16,13)),(14,10.5,1.30,250,(20,16))],1):
    parts=[];cx=1.5;outer=rounded(w,d,r);inner=rounded(w-1.2,d-1.2,max(.35,r-.6))
    dockw=4.2 if level==3 else 3.6;dockd=2.6 if level<3 else 3.0
    dockx=cx-w/2+.15+dockw/2;dockfront=-d/2;dockback=dockfront+dockd
    # Portable basin: bottom is above ground. Continuous lining seals water and hides terrain.
    surface('basin_floor',[(x,.10,z) for x,z in inner],'stone_dark','basin')
    for i,(x,z) in enumerate(outer):
        j=(i+1)%len(outer);xx,zz=outer[j];a,b=inner[i];aa,bb=inner[j]
        surface('basin_innerwall'+str(i),[(a,.10,b),(aa,.10,bb),(aa,.82,bb),(a,.82,b)],'stone_dark','basin')
        surface('basin_outerwall'+str(i),[(x,0,z),(xx,0,zz),(xx,.78,zz),(x,.78,z)],'stone','basin')
        surface('basin_coping'+str(i),[(a,.78,b),(aa,.78,bb),(xx,.78,zz),(x,.78,z)],'stone_light','basin')
    surface('water_surface',[(x,.65,z) for x,z in inner],'water','water')
    bottom_stones=0
    for k in range([6,9,12][level-1]):
        angle=k*math.tau/[6,9,12][level-1]
        x=cx+math.cos(angle)*(w/2-1.25);z=math.sin(angle)*(d/2-1.25)
        if dockx-dockw/2-.35<x<dockx+dockw/2+.35 and z<dockback+.35:continue
        stone('bottom_rock'+str(k),(x,.19,z),.70,.45,.14,'stone_dark','underwaterDecor');bottom_stones+=1
    centers,total=perimeter_samples(rounded(w-.62,d-.62,max(.3,r-.31)),32 if level==1 else 40 if level==2 else 56)
    for i,(x,z) in enumerate(centers):
        if z<-d/2+.45 and abs(x-dockx)<dockw/2-.03:continue
        h=.76+.10*math.sin(i*2.9);size=total/len(centers)*1.06
        stone('rim_boulder'+str(i),(x,.42,z),size,.82,h,['stone','stone_light','stone_dark'][i%3])
        if i%5==0:surface('moss'+str(i),[(x-.20,.42+h/2+.004,z-.13),(x+.19,.42+h/2+.004,z-.08),(x+.06,.42+h/2+.005,z+.21)],'leaf','decor')
    box('dock_beams',(dockx,.78,dockfront+dockd/2),(dockw,.20,dockd),'wood','dock')
    boards=math.ceil(dockd/.24)
    for i in range(boards):box('dock_board'+str(i),(dockx,.90,dockfront+(i+.5)*dockd/boards),(dockw,.10,dockd/boards-.015),'wood_light' if i%3 else 'wood','dock')
    for x in [dockx-dockw/2+.12,dockx+dockw/2-.12]:
        for z in [dockfront+.16,dockback-.16]:
            box('dock_post',(x,.83,z),(.22,1.66,.22),'wood','dock')
    beam('dock_rear_rail',(dockx-dockw/2,1.53,dockback),(dockx+dockw/2,1.53,dockback),.10,'wood','dock')
    # No front rail. Three 0.30 m steps and 2.0 m stair width.
    for i in range(3):box('dock_step'+str(i),(dockx,(.90-.30*i)/2,dockfront-.15-.30*i),(2.0,.90-.30*i,.30),'wood','dock')
    mx=cx-w/2-1.70;mz=dockfront+1.55
    feed_maker(mx,mz)
    bx=dockx+(1.40 if level==3 else 1.15);bz=dockback-.80;barrel(bx,bz)
    ax=dockx-(1.40 if level==3 else 1.25);az=dockback-.85
    if level>=2:feeder(ax,az,level==3)
    inlets=[(cx+w*.18,d/2+.08)] if level<3 else [(cx-w*.24,d/2+.08),(cx+w*.25,d/2+.08)]
    for k,(x,z) in enumerate(inlets):
        stone('inlet_base'+str(k),(x,.83,z),1.15,.93,1.65,'stone')
        box('inlet_spout'+str(k),(x,1.22,z-.55),(.46,.30,.76),'wood_light','inlet')
        box('inlet_mouth'+str(k),(x,1.17,z-.945),(.28,.17,.025),'metal','inlet')
        surface('inlet_stream'+str(k),[(x-.14,1.14,z-.96),(x+.14,1.14,z-.96),(x+.17,.66,z-1.08),(x-.17,.66,z-1.08)],'water_light','sketchWaterFx')
    outletx=cx+w/2-.80;outletz=-d/2+.55
    box('overflow_trough',(outletx,.49,outletz-.63),(.64,.16,.91),'stone_light','outlet')
    surface('overflow_water',[(outletx-.23,.61,outletz-.25),(outletx+.23,.61,outletz-.25),(outletx+.23,.18,outletz-1.05),(outletx-.23,.18,outletz-1.05)],'water_light','sketchWaterFx')
    # Sparse broad grass leaves, following the existing meadow style; no dense micro-detail.
    for k in range(12 if level<3 else 18):
        x,z=centers[(k*3+4)%len(centers)];n=np.array([x-cx,z]);n/=np.linalg.norm(n)
        x+=n[0]*.57;z+=n[1]*.57
        if z<dockfront+.2 and abs(x-dockx)<2:continue
        for j in [-1,0,1]:surface('leaf_patch'+str(k)+'_'+str(j),[(x-.12,0,z),(x+.12,0,z),(x+j*.27,.50+.1*(j==0),z+.08)],'leaf_light' if j==0 else 'leaf','decor')
    # Compact shed only at Lv3, independent silhouette from the pond rim.
    if level==3:
        sx=mx-.40;sz=mz+2.40
        box('supply_box',(sx,.65,sz),(1.40,1.25,1.35),'wood','storage')
        box('supply_door',(sx,.65,sz-.69),(.95,1.10,.05),'wood_light','storage')
        box('supply_roof',(sx,1.32,sz),(1.65,.16,1.58),'roof','storage')
    for i,p in enumerate(parts):p['id']=f'PondLv{level}_{i:04d}'
    model=dict(id=f'Lv{level}',level=level,capacity=capacity,uniformScale=1,pondCenterXYZ=[cx,0,0],outerWD=[w,d],
               outerCornerRadius=r,outerPolygonXZ=outer,waterPolygonXZ=inner,waterAreaM2=round(area(inner),3),
               waterLevelY=.65,basinFloorY=.10,waterDepth=.55,rimNominalTopY=.90,siteReservationWD=list(site),
               dock=dict(width=dockw,depth=dockd,centerXYZ=[dockx,.90,dockfront+dockd/2],frontZ=dockfront,backZ=dockback,
                         stairWidth=2,stairRise=.30,stairTread=.30,clearLaneWidth=1.40,steps=3),
               stations=dict(maker=dict(centerXYZ=[mx,0,mz],interactionXYZ=[mx,0,mz-2.05]),
                             pondInteractionXYZ=[dockx,.9,dockfront+1.0],pondApproachXYZ=[dockx,0,dockfront-1.60],
                             storage=dict(centerXYZ=[bx,.9,bz],interactionXYZ=[dockx,.9,bz-.55]),
                             autoFeeder=dict(enabled=level>=2,centerXYZ=[ax,.9,az] if level>=2 else None,
                                             emitterXYZ=[ax,1.9,az+.90] if level>=2 else None,directionXYZ=[0,0,1] if level>=2 else None)),
               inletCount=len(inlets),outletCount=1,maximumVisibleFish=[8,12,18][level-1],
               fishSwimBandY=[.38,.50],maximumFishVisualThickness=.18,bottomRockCount=bottom_stones,parts=parts)
    vs=np.concatenate([v for v,_ in draw.faces_for(model)]);lo=vs.min(axis=0);hi=vs.max(axis=0)
    model['visualBounds']=dict(minXYZ=lo.tolist(),maxXYZ=hi.tolist(),sizeWDH=[float(hi[0]-lo[0]),float(hi[2]-lo[2]),float(hi[1]-lo[1])])
    model['sketchPrimitiveTriangles']=sum(len(v)-2 for v,_ in draw.faces_for(model))
    model['budgetTriangles']=dict(LOD0Target=[3500,4500,6000][level-1],LOD0Limit=[5000,6500,8500][level-1],
                                  LOD1Target=[1800,2300,3200][level-1],LOD2Target=[650,850,1200][level-1])
    MODELS.append(model)

water=PACK/'Models/Materials/TFP_Water_1A.mat'
spec=dict(status='PROPOSED_AWAITING_USER_ACC_NO_BLENDER_NO_UNITY_CHANGES',date='2026-10-13',
          coordinateSystem=dict(unit='meter',right='+X',up='+Y',front='-Z',origin='center of reserved site on flat ground',
                                pondCenterXOffset=1.5,blenderConversion='Blender (x,y,z)=(x,z,y), front -Y; orient face winding outward. Verify FBX handedness and apply expected yaw 180 before camera-facing wrapper.'),
          playerReferenceHeight=3.6,levelToModel={'1':'Lv1','2':'Lv2','3':'Lv3'},models=MODELS,
          materials=dict(atlasPath=ATLAS.relative_to(ROOT).as_posix(),atlasSha256=hashlib.sha256(ATLAS.read_bytes()).hexdigest(),
                         atlasGuid='522fcb2b5717c7640b460ee9ff73a78a',atlasDimensions=[2048,2048],swatches=swatches,
                         opaqueMaterial='TFP_Atlas_1A.mat',waterMaterial=water.relative_to(ROOT).as_posix(),
                         waterMaterialGuid='593ae163bc656234f924d34c6719b9c1',waterMaterialSha256=hashlib.sha256(water.read_bytes()).hexdigest(),
                         waterShader='Toon/CustomWaterSimple',maximumStaticMaterialSlots=2,newRuntimeTextures=0,
                         proposedPondWaterOverrides={'_Waves':0,'_WaveIntensity':.16,'_Opacity':.35,'_OpacityDepth':1.4,'_ShallowColorDepth':.85,'_FoamOpacity':.18,'_FoamDistance':.06,'_ReflectionsOpacity':.08},
                         waterPreview='Flat schematic blue and stream ribbons only; actual shader not rendered/tested in this proposal.'),
          foundation=dict(method='portable shallow raised basin',flatGroundY=0,floorY=.10,waterY=.65,
                          linerBelowWater=True,terrainCarving=False,placementSlope='level pad, maximum ground excursion 0.08m; reject/level unsuitable terrain before apply'),
          futureIntegration=dict(oldRuntimeCapacities=[99,300,999,999],newCapacities=[50,100,250],
                                 oldDefinitionCapacities=[4,8,12,16],oldMaximumLevel=4,newMaximumLevel=3,
                                 migration='Preserve existing saved fish and pond IDs. Legacy Lv4 migrates to Lv3; over-capacity ponds allow withdrawal but no further deposits until count <= capacity. No silent deletion.',
                                 autoFeeding='Separate auto feeder from production FeedMaker; recipe timing, hopper stock, daily consumption and save fields need implementation after approval.',
                                 footprint='Definition currently has one footprint. With current system reserve largest 20x16 m for all levels; per-level 14x11/16x13/20x16 requires upgrade-expansion checks.',
                                 swim='Replace fixed radius 0.32m / Y0.05 with the approved inner polygon, deck exclusion and Y0.38..0.50 band; normalize actual fish prefab bounds to length 0.45..0.90m and vertical thickness <=0.18m.'),
          performance=dict(staticMeshes='One combined opaque static mesh per LOD; one separate main water mesh, maximum two static renderers/materials at a time.',
                           machines='Station components and simple colliders on empty child markers; static casing stays in combined mesh.',
                           waterFx='Reuse existing pack particles at 1/2 inlet and one outlet; near-camera effect budget, not baked transparent geometry.',
                           fish='8/12/18 representative visible fish, independent from 50/100/250 logical fish capacity; pool and use actual ItemSO.worldPrefab.'),
          referenceNote='Images 1 and 2 have the same pictured form. Lv2 enlargement + auto feeder follows user text. Rear/hidden details are designed proposals.')
(OUT/'pond-proposal-A.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')

def head(im,title,subtitle):
    d=ImageDraw.Draw(im);draw.label(d,(42,24),title,34,True);draw.label(d,(42,73),subtitle,22);return d
overview=Image.new('RGB',(2100,1120),draw.BG);od=head(overview,'KOLAM IKAN A / SKETSA Lv1-3','Batu lembut + kayu hangat + mesin hijau. Ketiga level memakai skala gambar yang sama.')
for i,m in enumerate(MODELS):
    tile=draw.render(m,[1,.85,-1],(685,640),same_scale=30);overview.paste(tile,(i*700+5,132))
    d=ImageDraw.Draw(overview);x=i*700+30
    draw.label(d,(x,129),f"{m['id']}  /  {m['capacity']} EKOR",28,True)
    for k,line in enumerate([f"Kolam luar {m['outerWD'][0]:g} x {m['outerWD'][1]:g} m",'Pembuat pakan + '+('penabur otomatis' if i else 'pakan manual'),
                            f"Air {m['waterAreaM2']:.1f} m2  /  kedalaman visual 0,55 m",f"Reservasi {m['siteReservationWD'][0]} x {m['siteReservationWD'][1]} m"]):draw.label(d,(x,784+k*47),line,23,k==0)
draw.label(od,(42,1050),'Ukuran usulan untuk ACC. Air pada sketsa hanya simbol warna; shader Toon Pack dipasang setelah model disetujui.',23)
overview.save(OUT/'pond-levels-overview.png')

for m in MODELS:
    for name,cam in [('front-right',[1,.85,-1]),('rear-left',[-1,.85,1])]:
        im=draw.render(m,cam,(1600,1200));d=head(im,f"KOLAM {m['id']} / "+('DEPAN + KANAN' if name=='front-right' else 'BELAKANG + KIRI'),
                                            f"Kapasitas {m['capacity']} / kolam {m['outerWD'][0]:g} x {m['outerWD'][1]:g} m / sketsa koordinat")
        draw.label(d,(42,1136),'Air / aliran digambar skematis. Posisi model dibuat dari pond-proposal-A.json.',23)
        im.save(OUT/f"pond-{m['id']}-{name}.png")
    sheet=Image.new('RGB',(2000,1350),draw.BG);sd=head(sheet,f"{m['id']} / EMPAT TAMPAK",'Depan -Z. Mesin tidak dibesarkan mengikuti kolam; scale tiap model tetap 1,1,1.')
    for i,(name,cam) in enumerate([('DEPAN',[0,0,-1]),('KANAN',[1,0,0]),('BELAKANG',[0,0,1]),('KIRI',[-1,0,0])]):
        xx=(i%2)*1000;yy=130+(i//2)*565
        sheet.paste(draw.render(m,cam,(980,460)),(xx+10,yy));draw.label(sd,(xx+35,yy+10),name,25,True)
        width=m['visualBounds']['sizeWDH'][0 if i%2==0 else 1]
        draw.label(sd,(xx+35,yy+474),f"Batas visual sisi: {width:.2f} m / tinggi {m['visualBounds']['sizeWDH'][2]:.2f} m",22)
    sheet.save(OUT/f"pond-{m['id']}-elevations.png")
    plan=Image.new('RGB',(1700,1330),draw.BG);pd=head(plan,f"{m['id']} / TAPAK + PLACING + INTERAKSI",'Grid 1 m. Angka, posisi dan arah ini menjadi acuan produksi setelah ACC.')
    sw,sh=m['siteReservationWD'];sc=min(980/sw,990/sh);ox=560;oy=640
    def pt(x,z):return (ox+x*sc,oy-z*sc)
    def bounds(a,b):return (min(a[0],b[0]),min(a[1],b[1]),max(a[0],b[0]),max(a[1],b[1]))
    def rect(x0,z0,x1,z1,fill,outline='#647b7c'):pd.rectangle(bounds(pt(x0,z0),pt(x1,z1)),fill=fill,outline=outline,width=2)
    rect(-sw/2,-sh/2,sw/2,sh/2,'#eee9da')
    for x in range(-sw//2,math.ceil(sw/2)+1):pd.line([pt(x,-sh/2),pt(x,sh/2)],fill='#ddd9cb')
    for z in range(math.ceil(-sh/2),math.floor(sh/2)+1):pd.line([pt(-sw/2,z),pt(sw/2,z)],fill='#ddd9cb')
    pd.polygon([pt(*p) for p in m['outerPolygonXZ']],fill=tuple(swatches['stone']['rgb']),outline='#647b7c',width=3)
    pd.polygon([pt(*p) for p in m['waterPolygonXZ']],fill='#7abac3',outline='#487e85',width=2)
    q=m['dock'];dx,_,dz=q['centerXYZ'];rect(dx-q['width']/2,q['frontZ'],dx+q['width']/2,q['backZ'],'#c79b62')
    rect(dx-1,q['frontZ']-.9,dx+1,q['frontZ'],'#d4b486')
    ms=m['stations']['maker']['centerXYZ'];rect(ms[0]-.8,ms[2]-.9,ms[0]+.8,ms[2]+.7,'#729d8b')
    pd.ellipse(bounds(pt(m['stations']['storage']['centerXYZ'][0]-.43,m['stations']['storage']['centerXYZ'][2]-.43),pt(m['stations']['storage']['centerXYZ'][0]+.43,m['stations']['storage']['centerXYZ'][2]+.43)),fill='#b98b4d',outline='#536866',width=2)
    if m['level']>=2:
        a=m['stations']['autoFeeder']['centerXYZ'];rect(a[0]-.52,a[2]-.50,a[0]+.52,a[2]+.5,'#a0c7ae')
    route=[m['stations']['pondApproachXYZ'],[dx,.9,q['frontZ']+.35],[dx,.9,q['backZ']-1.10]]
    pd.line([pt(v[0],v[2]) for v in route],fill='#277a64',width=max(4,int(q['clearLaneWidth']*sc)),joint='curve')
    for tag,point in [('P',m['stations']['pondInteractionXYZ']),('M',m['stations']['maker']['interactionXYZ']),('F',m['stations']['storage']['interactionXYZ'])]:
        x,y=pt(point[0],point[2]);pd.ellipse((x-16,y-16,x+16,y+16),fill='#284b50');draw.label(pd,(x-9,y-12),tag,21,True,'#ffffff')
    px,py=pt(0,0);pd.ellipse((px-6,py-6,px+6,py+6),fill='#d76b47')
    for i,pp in enumerate([p for p in m['parts'] if p['name'].startswith('inlet_spout')]):
        x,_,z=pp['centerXYZ'];rect(x-.25,z-.3,x+.25,z+.3,'#c79b62')
    draw.label(pd,(40,1160),'DEPAN -Z / KE ARAH KAMERA',24,True)
    draw.label(pd,(45,1210),f"Reservasi {sw} x {sh} m / origin merah (0,0,0) / kolam offset X +1,50 m",24,True)
    draw.label(pd,(45,1255),'P = UI kolam    M = pembuat pakan    F = pakan / penabur    Jalur hijau = lebar 1,40 m',22)
    lines=[f"{m['capacity']} ekor (data gameplay)",f"Kolam luar {m['outerWD'][0]:g} x {m['outerWD'][1]:g} m",f"Air {m['outerWD'][0]-1.2:g} x {m['outerWD'][1]-1.2:g} m",f"Luas air {m['waterAreaM2']:.2f} m2",
           f"Dock {q['width']:g} x {q['depth']:g} m",'Deck Y +0,90 m','Air Y +0,65 m','Dasar Y +0,10 m','Kedalaman air 0,55 m','Rim batu sekitar 0,90 m',
           '3 tangga @ rise / tread 0,30 m','Lebar tangga 2,00 m','Pembuat pakan tinggi 2,78 m',f"Penabur otomatis: {'YA' if m['level']>=2 else 'TIDAK'}",f"Inlet {m['inletCount']} / outlet 1",'Jalur akses depan 1,40 m']
    for i,line in enumerate(lines):draw.label(pd,(1150,165+i*53),line,23,i in [0,1])
    plan.save(OUT/f"pond-{m['id']}-plan.png")

section=Image.new('RGB',(1700,880),draw.BG);sd=head(section,'POTONGAN / BASIN PORTABLE + DOCK','Solusi untuk kolam yang bisa dipindah: dasar berada di atas tanah, tanpa menggali Terrain runtime.')
ox=140;oy=585;sc=135
def p(x,y):return(ox+x*sc,oy-y*sc)
sd.rectangle((60,585,1640,685),fill='#bbac86');draw.label(sd,(60,716),'Terrain Y = 0,00 m / landing pad rata',25,True)
sd.rectangle((*p(1,.10),*p(9,0)),fill='#8d8977')
sd.rectangle((*p(1,.65),*p(9,.10)),fill='#85bdc5')
sd.rectangle((*p(.5,.83),*p(1.1,0)),fill='#c3b69a');sd.rectangle((*p(8.9,.83),*p(9.5,0)),fill='#c3b69a')
sd.rectangle((*p(.3,.95),*p(3.4,.85)),fill='#c49a68')
for x in [.5,3.2]:sd.rectangle((*p(x,1.10),*p(x+.13,0)),fill='#a1784d')
for x,y,txt in [(4.0,.65,'Air +0,65 m'),(4.0,.10,'Dasar +0,10 m'),(9.65,.90,'Rim ~0,90 m')]:
    sd.line([p(x,y),p(x+.65,y)],fill='#3d6468',width=3);draw.label(sd,p(x+.73,y+.05),txt,24,True)
draw.label(sd,(110,165),'Dock +0,90 m / celah air -0,25 m di bawah deck',25,True)
draw.label(sd,(110,210),'Liner kontinu menutupi terrain. Dasar gelap + batu detail tipis di bawah air.',23)
draw.label(sd,(110,775),'Kontur terrain berbeda >0,08 m perlu diratakan / ditolak saat placement; jangan membuat air mengikuti slope.',22)
section.save(OUT/'pond-section-and-foundation.png')

machine=Image.new('RGB',(1800,1220),draw.BG);md=head(machine,'MESIN + DOCK / DETAIL PRODUKSI','Komponen mesin sama ukurannya di semua level; layout station mengikuti koordinat masing-masing kolam.')
for i,(group,title,lines) in enumerate([
    ('maker','PEMBUAT PAKAN',['Badan 1,45 x 1,10 x 1,45 m','Hopper atas 1,60 x 1,34 m / Y 2,78 m','Motor 12 sisi / radius 0,43 m','Outlet depan -Z / radius 0,24 m','Tray 1,00 x 0,76 m / tinggi 0,38 m']),
    ('autoFeeder','PENABUR OTOMATIS',['Tabung 10 sisi / radius 0,42 / 0,47 m','Tinggi puncak ~2,99 m dari tanah','Spout mengarah +Z, melewati rail belakang','Titik partikel di luar permukaan dock','Kontrol di sisi -Z / Y 1,90 m']),
    ('dock','DOCK / TANGGA',['Lv1-2: 3,60 x 2,60 m','Lv3: 4,20 x 3,00 m','Deck Y +0,90 m / tebal 0,10 m','Tangga lebar 2,00 m / 3 x 0,30 m','Tiang 0,22 m / rail 0,10 m'])]):
    sub=dict(parts=[p for p in MODELS[1]['parts'] if p['group']==group]);im=draw.render(sub,[1,.65,-1],(580,680));machine.paste(im,(i*600+10,125))
    draw.label(md,(i*600+30,130),title,25,True)
    for k,line in enumerate(lines):draw.label(md,(i*600+30,829+k*53),line,21)
draw.label(md,(40,1150),'Tidak ada ikan atau pakan animasi yang dibake ke mesh. Collider / input / output berada pada marker child.',22)
machine.save(OUT/'pond-machines-and-dock.png')

audit=Image.new('RGB',(1800,1330),draw.BG);ad=head(audit,'ATLAS + AIR / RENCANA MATERIAL','Satu atlas yang sama dengan barn, coop, rumah. Air memakai material Toon Pack tersendiri.')
audit.paste(atlas.resize((570,570)),(40,140));draw.label(ad,(40,743),'TFP_Atlas_1A.psd / 2048 x 2048',24,True)
draw.label(ad,(40,790),'Batu dan kayu dibentuk di mesh.',23);draw.label(ad,(40,825),'Tidak membuat texture baru per level.',23)
for i,(name,q) in enumerate(swatches.items()):
    x=660+(i%2)*550;y=135+(i//2)*116;ad.rectangle((x,y,x+76,y+59),fill=tuple(q['rgb']),outline='#71858a')
    draw.label(ad,(x+90,y),name,23,True);draw.label(ad,(x+90,y+34),f"RGB {q['rgb']} / px {q['pixelRectTopLeft'][:2]}",17)
for i,line in enumerate(['Opaque: TFP_Atlas_1A.mat / CustomToon, UV hanya swatch aman.','Air: TFP_Water_1A.mat / Toon/CustomWaterSimple, normal + noise pack yang sudah ada.',
                         'Buat variant air khusus pond; tidak mengubah material bersama milik pack.','PC_RPAsset: Depth Texture ON. Mobile_RPAsset: OFF; depth/foam perlu diaktifkan saat implementasi.',
                         'Gelombang kecil + foam tipis. Sketsa ini bukan pengujian shader / transparency di Unity.']):draw.label(ad,(40,990+i*57),line,23)
audit.save(OUT/'pond-atlas-and-materials.png')

(OUT/'References').mkdir(exist_ok=True)
for level,guid in [(1,'6b2e7fac-f3f6-4575-a5d7-09b49fcda9a8'),(2,'1d86ebd9-9c5d-4874-8a32-c72a54f18680'),(3,'e0669686-306f-42f7-9da7-93426c2930b4')]:
    src=Path('C:/Users/huxel/AppData/Local/Temp')/f'codex-clipboard-{guid}.png'
    if src.exists():shutil.copy2(src,OUT/'References'/f'Pond-Lv{level}-UserReference.png')
print(json.dumps([dict(level=m['level'],outer=m['outerWD'],bounds=m['visualBounds']['sizeWDH'],waterArea=m['waterAreaM2'],triangles=m['sketchPrimitiveTriangles']) for m in MODELS],indent=2))
