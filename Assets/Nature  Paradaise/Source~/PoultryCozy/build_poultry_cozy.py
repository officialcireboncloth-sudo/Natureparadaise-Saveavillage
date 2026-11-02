"""Editable Chicken Store + shop companion coop. Player-built coop is out of scope."""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise';OUT=ROOT/'ArtSource/PoultryCozy'
HELPERS=(A/'Source~/HouseCozy/build_house_cozy.py').read_text().split('# Ground, body and readable exposed framing.')[0]
HELPERS=HELPERS.replace("OUT=ROOT/'ArtSource/HouseCozy'","OUT=ROOT/'ArtSource/PoultryCozy'").replace("A/'Source~/HouseCozy'","A/'Source~/PoultryCozy'").replace("A/'mesh/Buildings/HouseCozy'","A/'mesh/Buildings/PoultryCozy'")

def begin(label):
 exec(HELPERS,globals());scene.name=label+' - Editable Atlas Master';random.seed(1101)

def sphere(name,c,radii,slot='Ceramic',detail=1):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,location=cv(c));o=bpy.context.object;o.scale=(radii[0],radii[2],radii[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if 'egg' in name.lower():
  for v in o.data.vertices:
   taper=1-.15*max(0,v.co.z/max(.001,radii[1]));v.co.x*=taper;v.co.y*=taper
 for p in o.data.polygons:p.use_smooth=True
 assign(o,slot);return link(o,name,detail)

def bird(name,c,size,slot='WoodDark',detail=0):
 # Rooster emblem / weather vane, not a live chicken or a nest occupant.
 outline=[(-.05,0),(-.22,.04),(-.32,.13),(-.39,.26),(-.46,.35),(-.43,.42),(-.39,.40),(-.35,.30),(-.35,.43),(-.30,.49),(-.26,.43),(-.25,.28),(-.17,.22),(-.03,.23),(.09,.31),(.14,.40),(.14,.50),(.16,.55),(.16,.62),(.19,.65),(.22,.60),(.25,.65),(.28,.60),(.31,.62),(.33,.56),(.36,.52),(.43,.50),(.35,.46),(.33,.37),(.28,.24),(.22,.12),(.12,.06),(.08,.02),(.07,-.10),(.18,-.12),(.18,-.16),(.03,-.16),(.02,.02),(-.04,.01),(-.07,-.11),(0,-.13),(-.01,-.17),(-.16,-.16),(-.14,-.12)]
 x,y,z=c;n=len(outline);vv=[(x+u*size,y+v*size,z+d) for d in [-.02,.02] for u,v in outline]
 return mesh(name,vv,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],slot,detail)

def letters(name,text,c,size,width,slot='WoodDark',detail=1):
 curve=bpy.data.curves.new(name,'FONT');curve.body=text;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=size;curve.extrude=.003;curve.resolution_u=2
 font=Path('C:/Windows/Fonts/georgiab.ttf')
 if font.exists():curve.font=bpy.data.fonts.load(str(font))
 o=bpy.data.objects.new(name,curve);source.objects.link(o);o.location=cv(c);o.rotation_euler=(math.pi/2,0,0)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o=bpy.context.object
 bpy.context.view_layer.update()
 if o.dimensions.x>width:o.scale.x*=width/o.dimensions.x
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);assign(o,slot);return link(o,name,detail)

def frame(name,x,y,z,w,h,th=.12,slot='WoodHoney',detail=0):
 for dx in [-w/2,w/2]:box(name+' upright',(x+dx,y,z),(th,h+th,th*1.5),slot,.025,detail)
 for dy in [-h/2,h/2]:box(name+' rail',(x,y+dy,z),(w-th,th,th*1.5),slot,.025,detail)

def panel(name,outline,z,slot,side_x=None):
 n=len(outline)
 vv=[(x,y,z+d) if side_x is None else (side_x+d,y,x) for d in [-.035,.035] for x,y in outline]
 return mesh(name,vv,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],slot)

def lantern(c,scale=1):
 x,y,z=c;s=scale
 beam('Lantern iron bracket',(x,y+.40*s,z-.16*s),(x,y+.40*s,z+.12*s),.045*s,.045*s,'Iron',1)
 cyl('Lantern cap',(x,y+.18*s,z+.12*s),.16*s,.09*s,'Iron',8,1)
 box('Lantern warm panes',(x,y,z+.12*s),(.19*s,.32*s,.18*s),'GlowWarm',.01,1,2)
 for dx in [-.12,.12]:
  for dz in [-.11,.11]:beam('Lantern cage',(x+dx*s,y-.20*s,z+(.12+dz)*s),(x+dx*s,y+.18*s,z+(.12+dz)*s),.027*s,.027*s,'Iron',1)
 box('Lantern base',(x,y-.20*s,z+.12*s),(.29*s,.06*s,.28*s),'Iron',.015,1)
 beam('Lantern hanger',(x,y+.22*s,z+.12*s),(x,y+.42*s,z+.12*s),.035*s,.035*s,'Iron',1)

def crate(c,w=.8,d=.55,h=.45,eggs=False):
 x,y,z=c;box('Crate base',(x,y+.04,z),(w,.08,d),'WoodDark',.015,1)
 for dx in [-w/2+.04,w/2-.04]:
  for dz in [-d/2+.04,d/2-.04]:box('Crate corner',(x+dx,y+h/2,z+dz),(.08,h,.08),'WoodHoney',.012,1)
 for level in range(3):
  yy=y+(level+.5)*h/3
  for dz in [-d/2,d/2]:box('Crate slat',(x,yy,z+dz),(w,h/3-.018,.055),'WoodHoney',.012,1)
  for dx in [-w/2,w/2]:box('Crate end slat',(x+dx,yy,z),(.055,h/3-.018,d),'WoodHoney',.012,1)
 if eggs:
  for r in range(2):
   for col in range(5):sphere('Display egg',(x+(col-2)*w*.16,y+h+.045,z+(r-.5)*d*.48),(.085,.115,.082),'Ceramic',1)

def barrel(c,r=.35,h=.8):
 x,y,z=c;profile=[(r*.78,0),(r*.95,.08*h),(r,h*.5),(r*.95,h*.92),(r*.78,h)]
 vv=[(x+rad*math.cos(a*math.tau/12),y+yy,z+rad*math.sin(a*math.tau/12)) for rad,yy in profile for a in range(12)]
 faces=[tuple(range(11,-1,-1)),tuple(range(48,60))]+[(j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i) for j in range(4) for i in range(12)]
 mesh('Coopered wood barrel',vv,faces,'WoodHoney',1)
 for yy,rad in [(h*.15,r*.98),(h*.84,r*.98)]:
  for i in range(12):
   a=i*math.tau/12;b=(i+1)*math.tau/12
   beam('Barrel iron hoop',(x+rad*math.cos(a),y+yy,z+rad*math.sin(a)),(x+rad*math.cos(b),y+yy,z+rad*math.sin(b)),.045,.05,'Iron',1)

def bucket(c,r=.27,h=.4):
 x,y,z=c;n=12
 vv=[(x+rad*math.cos(i*math.tau/n),y+yy,z+rad*math.sin(i*math.tau/n)) for yy,rad in [(0,r*.85),(h,r),(h-.035,r-.04),(.05,r*.85-.04)] for i in range(n)]
 ff=[(ring*n+i,ring*n+(i+1)%n,(ring+1)*n+(i+1)%n,(ring+1)*n+i) for ring in range(3) for i in range(n)]+[tuple(range(3*n,4*n))]
 mesh('Open aluminium water pail',vv,ff,'Aluminium',1)
 cyl('Pail water',(x,y+h*.63,z),r*.85,.01,'WaterBlue',12,1)
 for i in range(10):
  aa=i*math.pi/10;bb=(i+1)*math.pi/10
  beam('Pail handle',(x+r*math.cos(aa),y+h+.22*math.sin(aa),z),(x+r*math.cos(bb),y+h+.22*math.sin(bb),z),.018,.018,'Iron',1)

def sack(c,r=.32,h=.7):
 x,y,z=c;sphere('Grain sack body',(x,y+h*.43,z),(r,h*.47,r*.7),'Burlap',1)
 cyl('Sack tied neck',(x,y+h*.91,z),r*.25,.16,'Burlap',8,1)
 beam('Sack rope',(x-r*.28,y+h*.93,z),(x+r*.28,y+h*.93,z),.045,.045,'Rope',1)
 card('Sack leaf mark',(x,y+h*.45,z+r*.73),.22,.25,1,0)

def clip(poly,fn):
 if not poly:return []
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da,db=fn(*a),fn(*b);ia,ib=da>=-1e-7,db>=-1e-7
  if ia:out.append(a)
  if ia!=ib:
   t=da/(da-db);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
 return out if len(out)>=3 else []

def subtract(poly,conditions):
 remaining=poly;out=[]
 for f in conditions:
  piece=clip(remaining,lambda x,z,f=f:-f(x,z))
  if piece:out.append(piece)
  remaining=clip(remaining,f)
  if not remaining:break
 return out

def roofs(planes,tile_w=.52,pitch=.44):
 for index,p in enumerate(planes):
  xmin,xmax,zmin,zmax=p['rect'];height=p['h'];ridge_x=p['along']=='x'
  def visible(poly):
   pieces=[poly]
   for j,q in enumerate(planes):
    if index==j:continue
    a,b,c,d=q['rect'];other=q['h']
    cond=[lambda x,z,a=a:x-a,lambda x,z,b=b:b-x,lambda x,z,c=c:z-c,lambda x,z,d=d:d-z,lambda x,z,other=other,height=height:other(x,z)-height(x,z)-.002]
    pieces=[piece for part in pieces for piece in subtract(part,cond)]
   return pieces
  def prism(poly,base_bounds,detail,slab=True):
   u0,u1,v0,v1=base_bounds;n=len(poly);top=[]
   ha=height((u0+u1)/2,v0) if ridge_x else height(v0,(u0+u1)/2)
   hb=height((u0+u1)/2,v1) if ridge_x else height(v1,(u0+u1)/2)
   for x,z in poly:
    t=((z if ridge_x else x)-v0)/max(.001,v1-v0);up=t if hb>ha else 1-t
    top.append((x,height(x,z)+(.026+.048*(1-up) if slab else -.025),z))
   vv=top+[(x,y-.035,z) for x,y,z in top];ff=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
   o=mesh('Slate shingle' if slab else 'Slate distant underlay',vv,ff,'RoofSlate',detail)
   rect=S['RoofSlate']['singleTileUvs'][(int((u0+7)*8)+int((v0+7)*6))%8] if slab else region('RoofSlate')
   for face in o.data.polygons:
    for li in face.loop_indices:
     co=o.data.vertices[o.data.loops[li].vertex_index].co;x,z=co.x,-co.y
     u,v=(x,z) if ridge_x else (z,x)
     o.data.uv_layers.active.data[li].uv=(rect[0]+(rect[2]-rect[0])*min(1,max(0,(u-u0)/max(.001,u1-u0))),rect[1]+(rect[3]-rect[1])*min(1,max(0,(v-v0)/max(.001,v1-v0))))
  ul,uh=(xmin,xmax) if ridge_x else (zmin,zmax);vl,vh=(zmin,zmax) if ridge_x else (xmin,xmax)
  # Tiled underlay remains on the two distant LODs.
  for u in range(3):
   for v in range(2):
    u0=ul+(uh-ul)*u/3;u1=ul+(uh-ul)*(u+1)/3;v0=vl+(vh-vl)*v/2;v1=vl+(vh-vl)*(v+1)/2
    poly=[(u0,v0),(u1,v0),(u1,v1),(u0,v1)] if ridge_x else [(v0,u0),(v1,u0),(v1,u1),(v0,u1)]
    for piece in visible(poly):prism(piece,(u0,u1,v0,v1),2,False)
  rows=math.ceil((vh-vl)/pitch)
  for row in range(rows):
   v0=vl+(vh-vl)*row/rows;v1=vl+(vh-vl)*(row+1)/rows+.014
   pos=ul-(tile_w*.5 if row%2 else 0)
   while pos<uh:
    u0=max(ul,pos)+.003;u1=min(uh,pos+tile_w)-.003;pos+=tile_w
    if u1-u0<.025:continue
    poly=[(u0,v0),(u1,v0),(u1,v1),(u0,v1)] if ridge_x else [(v0,u0),(v1,u0),(v1,u1),(v0,u1)]
    for piece in visible(poly):prism(piece,(u0,u1,v0,v1),0)

def roof_edge(name,a,b,w=.16):
 o=beam(name,a,b,w,w,'GlassBlue');assign(o,'RoofSlate',rect=S['RoofSlate']['singleTileUvs'][0]);return o

def shop():
 begin('Chicken Store Cozy');
 box('Stone foundation',(0,.16,0),(8.1,.32,6.1),'StoneWarm',.04)
 box('Cream wall body',(0,1.86,0),(7.9,3.15,5.9),'Plaster',.035)
 for x in [-3.94,3.94]:
  for z in [-2.93,3.0]:box('Thick corner post',(x,1.9,z),(.25,3.36,.28),'WoodHoney',.035)
 for z in [-1.0,1.0]:
  for x in [-3.97,3.97]:box('Side structural timber',(x,1.9,z),(.20,3.3,.20))
 for y in [.45,3.32]:
  box('Front timber horizontal',(0,y,3.03),(8.1,.22,.27));box('Rear timber horizontal',(0,y,-3.03),(8.1,.22,.27))
 for x in [-4,4]:box('Side timber header',(x,3.32,0),(.22,.24,6.2));box('Side timber sill',(x,.45,0),(.21,.22,6.0))
 for x in [-3.96,3.96]:
  panel('Enclosed store side gable',[(-2.98,3.38),(-2.98,3.69),(0,5.20),(2.98,3.69),(2.98,3.38)],0,'Plaster',side_x=x)
  beam('Side gable upright',(x,3.42,0),(x,5.10,0),.17,.19)
 # Deep lit displays with actual shelves and eggs, not an empty flat window.
 for x,w in [(-2.30,2.55),(2.92,1.42)]:
  box('Display shadow interior',(x,1.92,3.02),(w,2.15,.12),'WoodDark',.02)
  box('Display warm back',(x,1.92,3.09),(w-.13,2.00,.03),'GlowWarm',.01,0,2)
  frame('Deep display surround',x,1.92,3.25,w,2.2,.16)
  for yy in [1.22,1.93,2.60]:box('Display shelf',(x,yy,3.38),(w-.10,.10,.45),'WoodDark',.015,1)
  cols=4 if w>2 else 2
  for col in range(1,cols):box('Display cubby division',(x-w/2+w*col/cols,1.93,3.35),(.065,2.07,.32),'WoodHoney',.012,1)
  for row in range(3):
   for col in range(cols):
    xx=x-w/2+w*(col+.5)/cols;yy=1.40+row*.67
    sphere('Egg display',(xx,yy,3.44),(.11,.15,.10),'Ceramic',1)
    if row==2:sphere('Golden egg display',(xx+.13,yy-.02,3.4),(.07,.11,.07),'GlowWarm',1)
  box('Display sill',(x,.82,3.34),(w+.30,.19,.56),'WoodHoney',.035)
 # Door stays a separate exported leaf with its hinge at the left jamb.
 dx=.65;dw=1.20
 box('Door recess',(dx,1.48,3.09),(1.45,2.55,.12),'WoodDark',.02)
 frame('Door heavy jamb',dx,1.48,3.23,dw+ .25,2.61,.17)
 before=len(parts)
 box('Door panel',(dx,1.49,3.27),(dw,2.45,.10),'WoodHoney',.025)
 box('Door lit glass',(dx,1.92,3.34),(.90,1.15,.045),'GlowWarm',.01,0,2)
 for yy in [1.37,1.92,2.48]:box('Door window rail',(dx,yy,3.39),(.98,.065,.06),'WoodDark',.01)
 for xx in [dx-.45,dx,dx+.45]:box('Door window bar',(xx,1.92,3.39),(.055,1.15,.06),'WoodDark',.01)
 bird('Door chicken emblem',(dx,.92,3.34),.35)
 box('Door handle',(dx+.40,1.20,3.43),(.06,.24,.10),'Iron',.012,1)
 door_parts=list(parts[before:])
 for o in door_parts:o['DoorLeaf']=True
 box('Doorstep',(dx,.13,3.55),(1.72,.26,.75),'Paving',.035)
 box('Door mat',(dx,.28,3.72),(1.12,.045,.55),'Burlap',.015,1)
 # Main roof, prominent forward gable and two brick chimney stacks.
 main=lambda x,z:3.45+(5.22-3.45)*(1-abs(z)/3.45)
 gable=lambda x,z:3.47+(5.78-3.47)*(1-abs(x)/2.35)
 roofs([dict(rect=(-4.42,4.42,0,3.45),h=main,along='x'),dict(rect=(-4.42,4.42,-3.45,0),h=main,along='x'),dict(rect=(-2.35,0,-.8,3.65),h=gable,along='z'),dict(rect=(0,2.35,-.8,3.65),h=gable,along='z')])
 for z in [-3.45,3.45]:beam('Main thick fascia',(-4.44,3.40,z),(4.44,3.40,z),.22,.24)
 for x in [-4.42,4.42]:
  for z in [-3.45,3.45]:roof_edge('Main roof verge',(x,3.48,z),(x,5.25,0),.16)
 roof_edge('Main ridge',(-4.5,5.29,0),(4.5,5.29,0),.20)
 # Front triangle is a solid cream field, overlaid by honey timber in the reference.
 panel('Cream front gable',[(-2.15,3.48),(2.15,3.48),(0,5.60)],3.48,'Plaster')
 panel('Closed rear of front gable',[(-2.15,3.48),(2.15,3.48),(0,5.60)],-.8,'Plaster')
 for sign in [-1,1]:
  beam('Prominent front gable frame',(sign*2.32,3.46,3.70),(0,5.80,3.70),.25,.28)
  roof_edge('Slate front verge',(sign*2.35,3.5,3.75),(0,5.84,3.75),.15)
  beam('Front decorative strut',(sign*1.50,4.0,3.57),(0,5.25,3.57),.15,.18)
 beam('Front king post',(0,4.05,3.59),(0,5.67,3.59),.19,.21)
 roof_edge('Front gable ridge',(0,5.86,-.8),(0,5.86,3.75),.17)
 for x in [-2.68,2.68]:
  box('Brick chimney stack',(x,4.95,-.95),(.44,1.5,.43),'Brick',.025)
  box('Chimney stone cap',(x,5.74,-.95),(.57,.13,.57),'StoneWarm',.025)
  box('Chimney dark flue',(x,5.815,-.95),(.34,.025,.33),'Iron',.005)
 # Large sign, curved crest, rooster and leaf ornaments.
 box('Sign honey frame',(0,3.94,3.90),(5.2,.91,.22),'WoodHoney',.055)
 box('Sign cream face',(0,3.94,4.03),(4.94,.67,.06),'Paper',.025)
 arch('Arched sign crest',(0,4.36,3.86),1.72,.18,.86,.17,'WoodHoney')
 arch('Cream sign crest',(0,4.40,3.97),1.49,.13,.745,.055,'Paper')
 bird('Sign rooster',(0,4.66,4.04),.75)
 letters('LIVE CHICKEN STORE lettering','LIVE CHICKEN STORE',(0,3.95,4.075),.39,4.44,detail=0)
 for sign in [-1,1]:card('Sign leaf flourish',(sign*2.23,3.94,4.08),.22,.35,0,sign*.3)
 lantern((-1.40,4.64,3.87),.85);lantern((1.40,4.64,3.87),.85)
 lantern((-.20,2.26,3.30),.9);lantern((1.69,2.26,3.30),.9);lantern((3.93,2.17,3.30),.9)
 # Red/cream cloth: curved top, rounded scalloped valance, no checkerboard UV.
 left,right=-4.10,-.67;back,front=3.10,4.30;n=8
 for stripe in range(n):
  x0=left+(right-left)*stripe/n;x1=left+(right-left)*(stripe+1)/n;slot='FabricRed' if stripe%2==0 else 'FabricCream'
  vv=[(x,3.23-.50*t-.08*math.sin(math.pi*t),back+(front-back)*t) for t in [i/6 for i in range(7)] for x in [x0,x1]]
  mesh('Curved canvas stripe',vv,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(6)],slot)
  hem=[(x0,2.73,front),(x1,2.73,front)]+[(x1-(x1-x0)*j/8,2.59-.11*math.sin(math.pi*j/8),front+.02) for j in range(9)]
  mesh('Scalloped canvas hem',hem,[tuple(range(len(hem)))],slot)
 # Counter, baskets, egg crates and reference pots.
 box('Display counter base',(-2.32,.79,3.37),(2.77,1.05,.77),'WoodHoney',.04)
 for xx in [-3.70,-.94]:box('Counter post',(xx,.80,3.75),(.16,1.15,.17),'WoodDark',.025)
 box('Counter top',(-2.32,1.34,3.55),(3.04,.16,1.00),'WoodHoney',.045)
 crate((-1.65,1.43,3.69),1.12,.68,.30,True)
 # Basket consists of a wicker bowl with a raised wood handle and eggs.
 sphere('Wicker egg basket',(-3.13,1.56,3.65),(.49,.28,.35),'Wicker',1)
 for i in range(12):
  t=i*math.pi/12;t2=(i+1)*math.pi/12
  beam('Basket handle',(-3.13+.47*math.cos(t),1.63+.54*math.sin(t),3.66),(-3.13+.47*math.cos(t2),1.63+.54*math.sin(t2),3.66),.047,.047,'WoodHoney',1)
 for zz in [3.50,3.75]:
  for xx in [-3.38,-3.13,-2.88]:sphere('Basket egg',(xx,1.73,zz),(.10,.14,.09),'Ceramic',1)
 for xx in [-2.94,-1.94]:crate((xx,.04,4.18),.93,.73,.40,True)
 box('Chalkboard plank',(-.83,.84,4.12),(.42,.60,.055),'WoodHoney',.02,1);box('Chalkboard face',(-.83,.84,4.16),(.33,.49,.025),'Iron',.005,1)
 bird('Chalk chicken',(-.83,.77,4.18),.25,'Paper',1)
 for c,r,h in [((-4.12,0,3.50),.31,.53),((-.22,0,3.97),.34,.62),((1.84,0,3.98),.32,.59),((3.97,0,3.43),.44,.69),((3.12,0,4.00),.30,.50)]:pot('Flower pot',c,r,h)
 sack((2.62,.05,3.58),.36,.86);bird('Sack chicken emblem',(2.62,.43,3.88),.34)
 # Planters below the two displays.
 for x,w in [(-2.30,2.24),(2.92,1.30)]:
  box('Window flower box',(x,.72,3.37),(w,.28,.45),'WoodDark',.025,1);plants((x,.94,3.40),18,w*.36)
 return door_parts,(dx-dw/2, .26,3.27)

def coop():
 begin('Shop Companion Coop Cozy')
 # Same compact shop-yard scale; empty nest openings, no player-build connection.
 box('Coop foundation',(0,.10,0),(3.05,.20,2.85),'StoneWarm',.025)
 box('Coop rear red wall',(0,1.03,-1.33),(2.95,1.8,.12),'WoodRed',.02)
 for x in [-1.44,1.44]:
  for j in range(12):box('Coop vertical red side board',(x,1.03,-1.25+j*2.5/11),(.13,1.8,.22),'WoodRed',.012)
 # Front wall assembled around real open nest and entry rectangles.
 holes=[(-.96,.68,.66,.66),(0,.66,.22,1.12),(.96,.68,.66,.66)]
 cuts=sorted(set([-1.5,1.5]+[x+sign*w/2 for x,w,y,h in holes for sign in [-1,1]]))
 for x0,x1 in zip(cuts,cuts[1:]):
  intervals=[(.18,1.95)]
  for x,w,y,h in holes:
   if x-w/2<(x0+x1)/2<x+w/2:intervals=[part for a,b in intervals for part in [(a,min(b,y)),(max(a,y+h),b)] if part[1]>part[0]+.01]
  for y0,y1 in intervals:box('Red wall between openings',((x0+x1)/2,(y0+y1)/2,1.35),(x1-x0,y1-y0,.14),'WoodRed',.015)
 box('Nest inner shadow',(0,1.08,1.01),(2.85,1.7,.055),'WoodDark',.01)
 for x in [-.96,.96]:box('Empty nest floor',(x,.61,1.36),(.74,.08,.55),'Hay',.012,1)
 for x,w,y,h in holes:
  frame('Red opening trim',x,y+h/2,1.46,w,h,.09,'WoodRed')
  if x:box('Nest sill',(x,y-.02,1.56),(w+.16,.11,.55),'WoodHoney',.018)
 for x in [-1.48,1.48]:
  for z in [-1.37,1.39]:box('Coop corner timber',(x,1.02,z),(.16,1.94,.18),'WoodHoney',.025)
 box('Coop front header',(0,1.87,1.44),(3.16,.19,.22),'WoodHoney',.025)
 # Cream vertical upper boards and central loft with curved lintel.
 for z in [-1.35,1.35]:
  for j in range(14):
   x0=-1.47+j*2.94/14;x1=x0+2.94/14-.008
   top=lambda x:1.92+(3.13-1.92)*(1-abs(x)/1.76)-.02
   panel('Cream vertical gable board',[(x0,1.88),(x1,1.88),(x1,top(x1)),(x0,top(x0))],z,'WoodCream')
 arch('Loft dark opening',(0,1.97,1.45),.70,.39,.35,.05,'WoodDark')
 for x in [-.40,.40]:box('Loft red jamb',(x,2.23,1.51),(.10,.58,.10),'WoodRed',.015)
 for j in range(8):
  a=j*math.pi/8;b=(j+1)*math.pi/8
  beam('Loft arched trim',(.40*math.cos(a),2.38+.40*math.sin(a),1.52),(.40*math.cos(b),2.38+.40*math.sin(b),1.52),.10,.12,'WoodRed')
 box('Loft sill',(0,1.99,1.56),(.94,.12,.44),'WoodHoney',.02)
 lantern((0,2.36,1.53),.55)
 # Red gable trim and rectangular slate shingles.
 h=lambda x,z:1.92+(3.13-1.92)*(1-abs(x)/1.76)
 roofs([dict(rect=(-1.76,0,-1.64,1.67),h=h,along='z'),dict(rect=(0,1.76,-1.64,1.67),h=h,along='z')],.36,.35)
 for x in [-1.76,1.76]:beam('Red eave trim',(x,1.88,-1.66),(x,1.88,1.72),.16,.18,'WoodRed')
 for z in [-1.67,1.72]:
  for x in [-1.76,1.76]:beam('Red gable verge',(x,1.92,z),(0,3.17,z),.15,.19,'WoodRed')
 roof_edge('Coop slate ridge',(0,3.20,-1.70),(0,3.20,1.77),.13)
 # Weather vane: single authored ornament; old optional ornament is disabled on apply.
 beam('Vane iron mast',(0,3.16,0),(0,3.55,0),.035,.035,'Iron',1);beam('Vane arrow',(-.34,3.39,0),(.34,3.39,0),.026,.026,'Iron',1)
 bird('Roof rooster vane',(0,3.52,0),.43,'Iron',1)
 # Low ramp aligns to the central entry.
 mesh('Entry ramp',[(-.40,.20,1.42),(.40,.20,1.42),(.40,.025,2.15),(-.40,.025,2.15)],[(0,1,2,3)],'WoodHoney')
 for z in [1.53,1.70,1.87,2.04]:box('Ramp tread',(0,.20-(z-1.42)*.24,z),(.83,.045,.07),'WoodHoney',.008)
 barrel((-1.88,0,1.05),.35,.78);bucket((1.68,0,1.78),.28,.43)
 sack((2.01,0,.76),.35,.83)
 box('Hay bale rear',(1.92,.44,-.12),(.75,.65,.57),'Hay',.045,1);box('Hay bale upper',(1.92,.83,-.12),(.67,.20,.52),'Hay',.025,1)
 for c,r,hh in [((-1.36,0,1.94),.25,.39),((2.07,0,1.52),.25,.42)]:pot('Coop flower pot',c,r,hh)
 return [],(0,0,0)

def export_and_render(name,door_parts,hinge):
 for o in parts:
  if 'MasonryUV' in o:continue
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 bpy.context.view_layer.update();stats=[];combined=[]
 def combine(items,label,ratio=None,origin=None):
  bpy.ops.object.select_all(action='DESELECT');copies=[]
  for original in items:
   cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);cp.select_set(True);copies.append(cp)
  bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=label;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  if origin:
   delta=cv(origin)
   for v in o.data.vertices:v.co-=delta
  bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
  used=[]
  for m in M:
   if any(o.data.materials[p.material_index]==m for p in o.data.polygons):used.append(m)
  old=[o.data.materials[p.material_index] for p in o.data.polygons];o.data.materials.clear()
  for m in used:o.data.materials.append(m)
  for p,m in zip(o.data.polygons,old):p.material_index=used.index(m)
  if ratio:
   d=o.modifiers.new('Distant simplification','DECIMATE');d.ratio=ratio;d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
  # Explicit triangulation and degenerate removal make the authored count match
  # Unity's importer at clipped roof valleys and text contours.
  bm=bmesh.new();bm.from_mesh(o.data)
  bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
  bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
  bmesh.ops.triangulate(bm,faces=list(bm.faces))
  bad=[f for f in bm.faces if f.calc_area()<1e-10]
  if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
  bm.to_mesh(o.data);bm.free();o.data.update()
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.export_scene.fbx(filepath=str(A/f'mesh/Buildings/PoultryCozy/{label}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
  stats.append(dict(name=label,triangles=sum(len(p.vertices)-2 for p in o.data.polygons),vertices=len(o.data.vertices),materials=[m.name for m in used],boundsMin=[min(v.co[i] for v in o.data.vertices) for i in range(3)],boundsMax=[max(v.co[i] for v in o.data.vertices) for i in range(3)]))
  return o
 for lod in range(3):
  selected=[o for o in parts if o not in door_parts and (lod==0 or (lodtag[o.name]!=1 and 'shingle' not in o.name))]
  o=combine(selected,name+'_LOD'+str(lod),.48 if lod==2 else None);o.hide_render=lod>0;o.hide_set(lod>0);combined.append(o)
 if door_parts:
  o=combine(door_parts,name+'_Door',origin=hinge);o.location=cv(hinge);combined.append(o)
 source.hide_render=True;source.hide_viewport=True
 markers=bpy.data.collections.new('04 ANCHORS - NO EXPORT');scene.collection.children.link(markers)
 for label,p in [('DoorHinge',hinge),('Front',(0,0,3.7 if name=='ChickenStore_Cozy' else 2.15))]:
  o=bpy.data.objects.new(label,None);markers.objects.link(o);o.location=cv(p);o.empty_display_size=.14
 # Soft studio only; real lantern lighting remains owned by the game.
 def studio_obj(o):
  for c in list(o.users_collection):c.objects.unlink(o)
  studio.objects.link(o)
 bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;studio_obj(o);fm=bpy.data.materials.new('Preview floor');fm.diffuse_color=(.78,.73,.61,1);o.data.materials.append(fm)
 for label,pos,power,size in [('Key',(-7,-10,14),1800,8),('Fill',(8,-2,10),900,10),('Rim',(0,8,12),1200,8)]:
  bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;studio_obj(o);o.name=label;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,2))-o.location).to_track_quat('-Z','Y').to_euler()
 scene.world=bpy.data.worlds.new('Cozy studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.5,.5,.5,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
 bpy.ops.object.camera_add(location=(10,-22,9));camera=bpy.context.object;studio_obj(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=12 if name=='ChickenStore_Cozy' else 6.1
 target=Vector((0,-.2,2.7 if name=='ChickenStore_Cozy' else 1.6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
 scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1300;scene.render.resolution_y=1050;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
 bpy.ops.object.select_all(action='DESELECT');combined[0].select_set(True);bpy.context.view_layer.objects.active=combined[0]
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=12 if name=='ChickenStore_Cozy' else 6;area.spaces.active.region_3d.view_location=target
 bpy.ops.wm.save_as_mainfile(filepath=str(A/f'Source~/PoultryCozy/{name}_Master.blend'))
 report=json.dumps(dict(status='BUILT',scope='Shop and companion coop only; player-build coop untouched',chickensInNests=0,sourceModules=len(parts),doorHingeUnity=hinge,lods=stats),indent=2)
 (OUT/f'Previews/{name}_Manifest.json').write_text(report)
 (A/f'Source~/PoultryCozy/{name}_Manifest.json').write_text(report)
 if '--no-preview' in __import__('sys').argv:
  print('POULTRY_BUILD_PASS',name,json.dumps(stats),flush=True);return
 for label,pos in [('Front',(0,-24,6.6 if name=='ChickenStore_Cozy' else 6.4)),('FrontRight',(9,-22,8.5)),('RearLeft',(-12,16,9))]:
  camera.location=pos;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/f'Previews/{name}_{label}.png');bpy.ops.render.render(write_still=True)
 print('POULTRY_BUILD_PASS',name,json.dumps(stats),flush=True)

for name,builder in [('ChickenStore_Cozy',shop),('ChickenCoop_Companion_Cozy',coop)]:
 door,hinge=builder();export_and_render(name,door,hinge)
