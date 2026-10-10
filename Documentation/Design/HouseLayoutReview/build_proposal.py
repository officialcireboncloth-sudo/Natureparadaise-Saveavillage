"""Generate review drawings and a placement manifest; DOES NOT edit Unity assets."""
import json, math, hashlib
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
SOURCE=json.loads((HERE/'current-scene-snapshot.json').read_text())
SCENE=ROOT/'Assets/Nature  Paradaise/Map/Scenes/Interiors/HouseInterior.unity'
FONT='C:/Windows/Fonts/arial.ttf'
FONTS={n:ImageFont.truetype(FONT,n) for n in (16,18,20,24,28,34,42)}
plans=[]
for lv,w,d in ((3,18,13),(4,22,16),(5,25,18)):
    existing={x['path']:x for x in SOURCE[str(lv)]}
    plan=dict(level=lv,width=w,depth=d,revision='proposal-A-2026-10-10',outline=[[0,0],[w,0],[w,d]]+([[12,d],[12,d+2.2],[6.2,d+2.2],[6.2,d+1.4]] if lv>=4 else [[6.2,d],[6.2,d+1.4]])+[[0,d+1.4]],items=[],routes=[],walls=[])
    def item(key,label,path,x,z,width,depth,face=None,kind='furniture',y=None,height=None,layer='floor'):
        source=existing.get(path)
        if y is None:y=source['center']['y'] if source else (height or 1)/2
        if height is None:height=source['size']['y'] if source else 1
        plan['items'].append(dict(id=key,label=label,sourcePath=path,center=[round(x,4),round(y,4),round(z,4)],boundsSize=[width,height,depth],facing=face,kind=kind,layer=layer))
    base='LivingBedroom_Editable/'
    item('bed','Kasur jumbo' if lv>=4 else 'Kasur',base+'Bed_MeshSlot',2.2,d-1,2.8 if lv>=4 else 1.9,3.5,[0,-1],kind='bed')
    nightx=4.45 if lv>=4 else 4
    item('night','Save',base+'SaveNightstand_Editable',nightx,d+.1,1.05,.9,[0,-1],height=1.05)
    item('book','Buku save',base+'SaveBook_Editable',nightx,d+.1,.65,.5,height=.18,y=1.14,layer='on-nightstand')
    item('cabinet','Lemari alat',base+'ToolCabinet_Editable',5.5,d-2.1,.9,1.5,[-1,0],kind='storage')
    item('chest','Peti',base+'StorageChest_Editable',w-1.5,1.6,1.7,1.1,[0,1],kind='storage')
    lounge='Lounge_Editable/'
    loungez={3:3.2,4:5.1,5:6.6}[lv]
    item('rug','Karpet',lounge+'LoungeRug_Editable',3.2,loungez-.1,6,4.8,height=.025,y=.0225,layer='rug')
    item('sofa','Sofa',lounge+'Sofa_Editable',5.2,loungez,1.25,3.7,[-1,0],kind='sofa')
    item('armchair','Kursi santai',lounge+'Armchair_Editable',2.75,loungez-2.05,1.35,1.35,[0,1],kind='chair')
    item('coffee','Meja tamu',lounge+'CoffeeTable_Editable',3.1,loungez,1,2,None,kind='table')
    item('tv-stand','Rak TV',base+'TVStand_Editable',.75,loungez+.3,.95,3.2 if lv==5 else 2.5,[1,0],kind='tv')
    item('tv','TV max' if lv==5 else 'TV',base+('TV_Max_Editable' if lv==5 else 'TV_MeshSlot'),.75,loungez+.3,1.0089 if lv==5 else .6984,2.6 if lv==5 else 1.8,[1,0],kind='tv',layer='on-tv-stand')
    tankz=6.8 if lv==3 else 7 if lv==4 else 8
    item('tank-stand','Rak aquarium',base+'AquariumStand_Editable',w-.75,tankz,1.1,3.6 if lv==5 else 2.1,[-1,0],kind='aquarium')
    item('tank','Aquarium jumbo' if lv==5 else 'Aquarium',base+('Aquarium_Jumbo_Editable' if lv==5 else 'Aquarium_Editable'),w-.75,tankz,1,3.6 if lv==5 else 2.1,[-1,0],kind='aquarium',layer='on-tank-stand')
    kitchen='Kitchen_Editable/'
    rowz=d-1.15
    for key,label,path,x,width in (
        ('stove','Kompor','Kitchen_MeshSlot',w-8.3,2),
        ('prep-1','Prep A','KitchenReturnCounter_Editable',w-6.6,1.3513),
        ('sink','Sink','KitchenSink_Editable',w-4.9,2),
        ('prep-2','Prep B','KitchenCounter_Editable',w-3.2,1.2)):
        item(key,label,kitchen+path,x,rowz,width,1.25 if key=='prep-1' else 1.3,[0,-1],kind='kitchen')
    fridgewidth=1.65 if lv==5 else 1.35
    item('fridge','Kulkas max' if lv==5 else 'Kulkas',base+('Refrigerator_Max_Editable' if lv==5 else 'Refrigerator_MeshSlot'),w-1.3,rowz,fridgewidth,1.9139 if lv==5 else 1.5659,[0,-1],kind='kitchen')
    item('island','Island',kitchen+'KitchenIsland_Editable',w-5.2,d-4.6,3.5 if lv==3 else 4.4,1.3,[0,1],kind='kitchen')
    tx={3:12.4,4:15.7,5:18.3}[lv];tz={3:3.6,4:4.2,5:4.8}[lv]
    dining='Dining_Editable/'
    item('dining','Meja makan',dining+'DiningTable_Editable',tx,tz,2.3,1.8,None,kind='table')
    for n,x,z,bx,bz,face in ((1,tx-1.8,tz,.85,.8,[1,0]),(2,tx+1.8,tz,.85,.8,[-1,0]),(3,tx,tz-1.6,.8,.85,[0,1]),(4,tx,tz+1.6,.8,.85,[0,-1])):
        item('chair-'+str(n),'K'+str(n),dining+f'DiningChair_{n}_Editable',x,z,bx,bz,face,kind='chair')
    item('flowers','Bunga meja','DiningFlowers_Editable',tx,tz,.45,.45,height=.65,y=1.475,layer='on-dining')
    item('plant','Tanaman','CornerPlant_Editable',w-1.1,3.7,.8,.8,height=1.4)
    if lv>=4:
        child='ChildRoom_Preparation_Editable/'
        item('child-bed','Kasur anak',child+'ChildBed_Preparation_Editable',8.4,d-.8,1.45,2.9,[0,-1],kind='bed')
        item('desk','Meja belajar',child+'ChildDesk_Editable',10.4,d+.8,1.4,.85,[0,-1],kind='table')
        item('child-chair','Kursi',child+'ChildChair_Editable',10.4,d-.4,.65,.7,[0,1],kind='chair')
        item('child-rug','Karpet anak',child+'ChildRug_Editable',9.2,d-1.8,3.3,2.5,height=.025,y=.0225,layer='rug')
    # Preserve the actual authored perimeter and partition coordinates.
    for s in SOURCE[str(lv)]:
        if 'Wall' in s['path'] or 'FrontLeft' in s['path'] or 'FrontRight' in s['path'] or 'WingStep' in s['path'] or 'OuterExtension' in s['path']:
            plan['walls'].append(dict(sourcePath=s['path'],center=s['center'],size=s['size']))
    corridor=d-6.7
    plan['routes']=[dict(id='entry',width=1.5,points=[[w/2,0],[w/2,corridor]]),dict(id='bedroom',width=1.5,points=[[w/2,corridor],[2.2,corridor],[2.2,d-4.1]])]
    kx=w-8.5
    plan['routes'].append(dict(id='kitchen',width=1.5,points=[[w/2,corridor],[kx,corridor],[kx,d-2.9]]))
    if lv>=4:plan['routes'].append(dict(id='child',width=1.5,points=[[9.4,corridor],[9.4,d-3.8]]))
    plan['entryPoint']=[w/2,1.15,1.4];plan['exitPoint']=[w/2,1.2,0]
    plan['wakePoint']=[2.2,1.15,d-4.1]
    plan['kitchenAisle']=2.15
    plans.append(plan)

manifest=dict(schemaVersion=1,status='PROPOSED_NOT_APPLIED',revision='proposal-A-2026-10-10',scenePath=str(SCENE.relative_to(ROOT)).replace('\\','/'),sourceSceneSha256=hashlib.sha256(SCENE.read_bytes()).hexdigest(),coordinates='Layout local units; +X right, +Z rear/top of drawing; center is renderer bounds center, not imported FBX pivot.',facing='Semantic visual front vector [X,Z]; resolve prefab front once when applying, never assume local +Z. Do not change coordinates to compensate for prefab pivot.',placementContract='Apply exact centers, footprints and facing shown; preserve mesh proportions, source materials and interaction components. If a native model cannot meet the proposed footprint with uniform scale, flag it before changing the accepted design.',levels=plans)
(HERE/'proposed-layouts.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf-8')

# Analytic footprint checks; this is NOT native Unity collision testing.
def box(it):
    x,_,z=it['center'];w,_,d=it['boundsSize'];return x-w/2,z-d/2,x+w/2,z+d/2
issues=[];counts=[]
for p in plans:
    solids=[it for it in p['items'] if it['layer']=='floor']
    for j,a in enumerate(solids):
        aa=box(a)
        for b in solids[j+1:]:
            bb=box(b)
            if min(aa[2],bb[2])-max(aa[0],bb[0])>.015 and min(aa[3],bb[3])-max(aa[1],bb[1])>.015:issues.append(f"Lv{p['level']}: overlap {a['id']} / {b['id']}")
        for wall in p['walls']:
            c=wall['center'];s=wall['size'];bb=(c['x']-s['x']/2,c['z']-s['z']/2,c['x']+s['x']/2,c['z']+s['z']/2)
            if min(aa[2],bb[2])-max(aa[0],bb[0])>.015 and min(aa[3],bb[3])-max(aa[1],bb[1])>.015:issues.append(f"Lv{p['level']}: wall overlap {a['id']} / {wall['sourcePath']}")
    samples=0
    for route in p['routes']:
        radius=route['width']/2
        for a,b in zip(route['points'],route['points'][1:]):
            n=max(1,math.ceil(math.dist(a,b)*20))
            for j in range(n+1):
                x=a[0]+(b[0]-a[0])*j/n;z=a[1]+(b[1]-a[1])*j/n;samples+=1
                for it in solids:
                    xx=box(it);dx=max(xx[0]-x,0,x-xx[2]);dz=max(xx[1]-z,0,z-xx[3])
                    if math.hypot(dx,dz)<radius-.015:issues.append(f"Lv{p['level']}: route {route['id']} blocked by {it['id']}");break
                for wall in p['walls']:
                    c=wall['center'];sz=wall['size'];xx=(c['x']-sz['x']/2,c['z']-sz['z']/2,c['x']+sz['x']/2,c['z']+sz['z']/2)
                    dx=max(xx[0]-x,0,x-xx[2]);dz=max(xx[1]-z,0,z-xx[3])
                    if math.hypot(dx,dz)<radius-.015:issues.append(f"Lv{p['level']}: route {route['id']} blocked by wall {wall['sourcePath']}")
    counts.append(f"Lv{p['level']}: {len(solids)} floor footprints, {samples} route samples")
report='PROPOSAL GEOMETRY CHECK (not Unity Play Mode)\n'+'\n'.join(counts)+'\n'+('\n'.join(sorted(set(issues))) if issues else 'PASS: no floor furniture / wall overlap; 1.5-unit route ribbons clear of furniture.')+'\n'
(HERE/'proposal-checks.txt').write_text(report,encoding='utf-8')
print(report)

# Static review sketches: same positions and footprints as proposed-layouts.json.
COL=dict(bg='#f6f1e6',floor='#ead8b8',line='#5d5143',text='#302c26',muted='#726a5e',furniture='#d6c5a4',bed='#e8c4a6',sofa='#b3c29b',chair='#c9d3b5',table='#d2ac7c',tv='#acb7bd',aquarium='#9dcbd0',kitchen='#c2cdce',storage='#c3ae8a',route='#c6d8b9')
def draw(p):
    s=46;ox=82;oy=120;maxz=p['depth']+(2.2 if p['level']>=4 else 1.4)
    width=math.ceil(p['width']*s+164);height=math.ceil(maxz*s+240)
    im=Image.new('RGB',(width,height),COL['bg']);g=ImageDraw.Draw(im)
    xy=lambda x,z:(ox+x*s,oy+(maxz-z)*s)
    def textat(x,y,text,font=18,color='text',anchor='mm'):g.text((x,y),text,font=FONTS[font],fill=COL[color],anchor=anchor)
    textat(82,42,f"LEVEL {p['level']}  /  SKETSA A",34,anchor='lm')
    textat(82,78,f"{p['width']} × {p['depth']} unit + sayap kamar · Tampak atas",20,'muted',anchor='lm')
    g.polygon([xy(*v) for v in p['outline']],fill=COL['floor'])
    # quiet one-unit plan grid
    for x in range(p['width']+1):g.line([xy(x,0),xy(x,p['depth'])],fill='#ddcdae',width=1)
    for z in range(p['depth']+1):g.line([xy(0,z),xy(p['width'],z)],fill='#ddcdae',width=1)
    for it in p['items']:
        if it['layer']=='rug':
            b=box(it);g.rounded_rectangle([xy(b[0],b[3]),xy(b[2],b[1])],radius=14,fill='#d9c7b4')
    for r in p['routes']:
        g.line([xy(*v) for v in r['points']],fill=COL['route'],width=round(s*r['width']),joint='curve')
        g.line([xy(*v) for v in r['points']],fill='#879b72',width=2)
    for wall in p['walls']:
        c=wall['center'];size=wall['size'];a=xy(c['x']-size['x']/2,c['z']+size['z']/2);b=xy(c['x']+size['x']/2,c['z']-size['z']/2)
        g.rectangle([a,b],fill=COL['line'])
    # Low front boundary and entrance opening.
    g.line([xy(0,0),xy(p['width']/2-1.3,0)],fill=COL['line'],width=7)
    g.line([xy(p['width']/2+1.3,0),xy(p['width'],0)],fill=COL['line'],width=7)
    for it in p['items']:
        if it['layer']!='floor':continue
        b=box(it);a=xy(b[0],b[3]);bb=xy(b[2],b[1]);color=COL.get(it['kind'],COL['furniture'])
        g.rounded_rectangle([a,bb],radius=4,fill=color,outline=COL['line'],width=2)
        x,_,z=it['center'];face=it['facing']
        if face:
            scale=.4 if it['kind']=='chair' else .38
            ex=(b[2]-b[0])/2;ez=(b[3]-b[1])/2
            offset=(ex if face[0] else ez)-.25
            cx=x+face[0]*offset;cz=z+face[1]*offset
            start=xy(cx-face[0]*scale/2,cz-face[1]*scale/2);end=xy(cx+face[0]*scale/2,cz+face[1]*scale/2)
            g.line([start,end],fill=COL['line'],width=2)
            vx,vy=end[0]-start[0],end[1]-start[1];length=math.hypot(vx,vy);vx/=length;vy/=length
            g.polygon([end,(end[0]-vx*7-vy*4,end[1]-vy*7+vx*4),(end[0]-vx*7+vy*4,end[1]-vy*7-vx*4)],fill=COL['line'])
        label=it['label']
        if it['id']=='plant':label='Pot'
        if it['id']=='desk':label='Belajar'
        if it['id']=='fridge' and p['level']==5:label='Kulkas'
        if it['id']=='tv-stand':label='TV max' if p['level']==5 else 'TV'
        if it['id']=='tank-stand':label='Aquarium jumbo' if p['level']==5 else 'Aquarium'
        if it['kind']=='chair' and it['id'].startswith('chair'):label=it['label']
        # Vertical objects use rotated labels to keep all footprints legible.
        if (b[3]-b[1])>1.6*(b[2]-b[0]) and it['kind']!='chair':
            f=FONTS[18];bounds=f.getbbox(label);tile=Image.new('RGBA',(bounds[2]+12,32));ImageDraw.Draw(tile).text((6,0),label,font=f,fill=COL['text']);tile=tile.rotate(90,expand=True)
            cx,cy=xy(x,z);im.paste(tile,(round(cx-tile.width/2),round(cy-tile.height/2)),tile)
        elif it['kind']!='chair':
            textat(*xy(x,z),label,16)
    # Room names and key approach spaces.
    textat(*xy(3.1,p['depth']-3.65),'KAMAR UTAMA',18,'muted')
    if p['level']>=4:textat(*xy(9.1,p['depth']-3.75),'KAMAR ANAK',18,'muted')
    textat(*xy(p['width']-5.3,p['depth']-2.85),'Aisle dapur 2,15 u',18,'muted')
    textat(*xy(p['width']/2,-.6),'PINTU MASUK',20)
    textat(82,height-50,'Panah = arah hadap  ·  Hijau = jalur kosong 1,5 unit  ·  Belum diterapkan',18,'muted',anchor='lm')
    im.save(HERE/f'level-{p["level"]}-sketch.png')
    return im
images=[draw(p) for p in plans]
# Equal metric scale; Lv5 drawing is larger because its house actually is larger.
overview=Image.new('RGB',(sum(i.width for i in images)+48*2,max(i.height for i in images)),COL['bg']);x=0
for im in images:overview.paste(im,(x,0));x+=im.width+48
overview.save(HERE/'levels-3-4-5-overview.png')
