"""Barn Store reference rework; player barn and livestock buildings stay separate."""
from pathlib import Path
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
library=(ROOT/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py').read_text().split('\nfor name,builder in ')[0]
library=library.replace('PoultryCozy','BarnStoreCozy').replace("name=='ChickenStore_Cozy'","name in ('ChickenStore_Cozy','BarnStore_Cozy')")
library=library.replace('Shop and companion coop only; player-build coop untouched','Barn Store only; player barn untouched')
exec(compile(library,__file__,'exec'),globals())

def timber(name,c,size,slot='WoodHoney',tier=0):return box(name,c,size,slot,.045,tier)

def wheat(c,size=.25):
 x,y,z=c
 beam('Wheat emblem stem',(x,y-size*.5,z),(x,y+size*.5,z),size*.055,size*.055,'LeafGreen',1)
 for i in range(3):
  yy=y-size*.28+i*size*.23
  for sign in [-1,1]:
   panel('Wheat emblem leaf',[(x,yy),(x+sign*size*.27,yy+size*.12),(x+sign*size*.32,yy+size*.29),(x+sign*size*.10,yy+size*.23)],z,'LeafGreen')
   lodtag[parts[-1].name]=1

def feedbag(c,w=.50,h=.82,d=.37,tier=1):
 x,y,z=c
 o=sphere('Feed bag',(x,y+h*.43,z),(w*.51,h*.46,d*.52),'FabricCream',tier)
 # Gentle folds around a rolled top; the front emblem is actual atlas-coloured geometry.
 timber('Feed bag folded top',(x,y+h*.88,z),(w*.65,h*.10,d*.85),'Burlap',tier)
 wheat((x,y+h*.48,z+d*.53+.009),w*.45)

def seedbox(c,w=.40,h=.33):
 x,y,z=c
 timber('Seed package',(x,y+h*.5,z),(w,h,.28),'Paper',1)
 timber('Seed package lower colour',(x,y+h*.12,z+.15),(w*.92,h*.18,.025),'FoodOrange',1)
 wheat((x,y+h*.56,z+.155),w*.48)

def flowerbarrel(c,r=.38,h=.56):
 x,y,z=c;barrel(c,r,h);plants((x,y+h,z),18,r*.95)

def pitchfork(c):
 x,y,z=c
 beam('Display pitchfork wooden handle',(x,y,z),(x-.06,y+1.55,z-.06),.052,.052,'WoodHoney',1)
 beam('Pitchfork shoulder',(x-.20,y+1.60,z-.06),(x+.12,y+1.60,z-.06),.045,.045,'Iron',1)
 for dx in [-.20,-.095,.01,.12]:
  beam('Pitchfork tine',(x+dx,y+1.60,z-.06),(x+dx,y+1.96,z-.02),.036,.036,'Iron',1)

def shovel(c):
 x,y,z=c
 beam('Display shovel handle',(x,y+.32,z),(x+.05,y+1.35,z-.06),.05,.05,'WoodHoney',1)
 panel('Shovel blade',[(x-.17,y+.38),(x+.17,y+.38),(x+.15,y+.12),(x,y),(x-.15,y+.12)],z+.01,'Aluminium')
 lodtag[parts[-1].name]=1

def barn_lamp(c):
 x,y,z=c
 pts=[(x,y+.26,z-.25),(x,y+.35,z-.08),(x,y+.33,z+.15),(x,y+.17,z+.22)]
 for a,b in zip(pts,pts[1:]):beam('Curved lamp iron bracket',a,b,.045,.045,'Iron',1)
 bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=.22,radius2=.07,depth=.15,location=cv((x,y+.09,z+.22)))
 assign(bpy.context.object,'Iron');link(bpy.context.object,'Black bell lamp shade',1)
 sphere('Warm lamp bulb',(x,y-.025,z+.22),(.085,.08,.085),'GlowWarm',1);assign(parts[-1],'GlowWarm',2)

def build():
 begin('Barn Store Cozy')
 # Canonical source front +Z, matching the existing root/collider reservation.
 timber('Stone foundation',(-1.25,.12,0),(6.5,.24,6.02),'StoneWarm')
 timber('Left enclosed wing',(-3.75,1.27,0),(1.5,2.35,5.98),'WoodRed')
 timber('Rear shop wall',(-.50,1.30,-2.95),(5.0,2.36,.13),'WoodRed')
 timber('Left main side wall',(-3.0,1.30,0),(.13,2.36,5.98),'WoodRed')
 timber('Right main side wall',(2.0,1.30,0),(.13,2.36,5.98),'WoodRed')
 # Front red boards are separate panels around the double doorway.
 for xa,xb in [(-3.0,-1.45),(1.45,2.0)]:
  count=math.ceil((xb-xa)/.22)
  for i in range(count):
   w=(xb-xa)/count-.008;xx=xa+(i+.5)*(xb-xa)/count
   h=2.36 if abs(xx)>2.1 else 2.92
   timber('Front vertical red board',(xx,.11+h/2,3.0),(w,h,.16),'WoodRed')
 for xx in [-4.45,-3.0,-1.48,1.48,2.02]:
  h=2.42 if xx<-2.5 else 3.0
  timber('Thick front timber post',(xx,.06+h/2,3.13),(.24,h,.30))
 for xx in [-4.45,-3.0,2.02]:
  h=2.42
  timber('Rear corner timber',(xx,.06+h/2,-2.98),(.24,h,.28))
 for yy in [.35,2.40]:timber('Front long timber rail',(-1.20,yy,3.13),(6.72,.20,.28))
 timber('Right-side lower rail',(2.09,.34,0),(.22,.18,6.0))
 # Display stays open at the front; its outside flank is fully enclosed.
 for i in range(12):
  timber('Display rear vertical board',(2.12+(i+.5)*2.28/12,1.28,-2.97),(2.28/12-.006,2.35,.13),'WoodRed')
 for z in [-3,0,3]:timber('Display open-side column',(4.40,1.21,z),(.24,2.42,.24))
 for i in range(27):
  zz=-2.99+(i+.5)*5.98/27
  timber('Closed right display wall board',(4.37,1.27,zz),(.15,2.42,5.98/27+.006),'WoodRed')
 for yy in [.12,2.42]:timber('Right display wall rail',(4.45,yy,0),(.20,.18,6.04),'WoodHoney')
 for xx in [2.04,4.40]:timber('Display roof support',(xx,2.49,0),(.26,.25,6.25))
 timber('Display front header',(3.23,2.49,3.11),(2.60,.25,.28))
 beam('Open display front brace',(4.40,1.88,3.10),(3.65,2.48,3.10),.17,.20)
 beam('Open display rear brace',(4.40,1.88,-3.0),(3.65,2.48,-3.0),.17,.20)
 # Lower gambrel roof wings, steep central shoulder and compact ridge.
 xs=[0,1.45,2.10,4.90];ys=[5.18,4.46,3.12,2.57]
 def roofheight(x,z):
  t=abs(x)
  for i in range(3):
   if t<=xs[i+1]+.001:return ys[i]+(ys[i+1]-ys[i])*(t-xs[i])/(xs[i+1]-xs[i])
  return ys[-1]
 def mainheight(x,z):return 2.57+1.95*(1-abs(z)/3.40)
 planes=[dict(rect=(-4.9,4.9,-3.40,0),h=mainheight,along='x'),dict(rect=(-4.9,4.9,0,3.40),h=mainheight,along='x')]
 for sign in [-1,1]:
  for i in range(2):planes.append(dict(rect=(min(sign*xs[i],sign*xs[i+1]),max(sign*xs[i],sign*xs[i+1]),-.05,3.48),h=roofheight,along='z'))
 roofs(planes,.43,.33)
 for sign in [-1,1]:
  for i in range(2):roof_edge('Slate front gable verge',(sign*xs[i],ys[i]+.07,3.55),(sign*xs[i+1],ys[i+1]+.07,3.55),.15)
 for xx in [-4.90,4.90]:
  for z0,z1 in [(-3.4,0),(0,3.4)]:roof_edge('Slate side roof verge',(xx,mainheight(xx,z0)+.06,z0),(xx,mainheight(xx,z1)+.06,z1),.15)
 roof_edge('Slate front gable ridge',(0,5.28,-.05),(0,5.28,3.57),.18)
 roof_edge('Slate main ridge',(-4.90,4.60,0),(4.90,4.60,0),.17)
 for zz in [-3.4,3.4]:beam('Thick main eave timber',(-4.90,2.54,zz),(4.90,2.54,zz),.23,.27)
 for xx in [-4.43,4.40]:
  panel('Closed side roof gable',[(-2.99,2.40),(0,4.44),(2.99,2.40)],0,'WoodRed',side_x=xx)
 # Closed front upper gable, raised framing and X loft shutter.
 for z in [3.12]:
  for i in range(18):
   x0=-2.02+i*4.04/18;x1=x0+4.04/18-.006
   top0=roofheight(x0,z)-.06;top1=roofheight(x1,z)-.06
   panel('Vertical red loft board',[(x0,2.88),(x1,2.88),(x1,top1),(x0,top0)],z,'WoodRed')
 for sign in [-1,1]:
  beam('Prominent gable shoulder',(sign*2.11,3.12,3.48),(sign*1.45,4.46,3.48),.27,.30)
  beam('Prominent gable crown',(sign*1.45,4.46,3.48),(0,5.18,3.48),.27,.30)
  beam('Front porch brace',(sign*1.49,2.10,3.27),(sign*2.02,2.88,3.27),.20,.24)
 timber('Gable king post',(0,4.59,3.25),(.20,1.00,.26))
 timber('Loft shutter backing',(0,4.22,3.27),(1.10,.73,.08),'WoodHoney')
 frame('Loft shutter frame',0,4.22,3.34,1.12,.76,.12,'WoodCream')
 beam('Loft X brace',(-.49,3.89,3.43),(.49,4.55,3.43),.105,.10,'WoodCream')
 beam('Loft X brace',(.49,3.89,3.44),(-.49,4.55,3.44),.105,.10,'WoodCream')
 # Two leaf panels remain individually editable in the master.
 for side in [-1,1]:
  cx=side*.65
  timber('Door leaf left' if side<0 else 'Door leaf right',(cx,1.35,3.20),(1.28,2.35,.12),'WoodHoney')
  timber('Door glass inset',(cx,1.92,3.29),(.98,1.02,.045),'GlowWarm')
  assign(parts[-1],'GlowWarm',2)
  frame('Door glass frame',cx,1.92,3.34,1.00,1.06,.07)
  for xoff in [-.25,0,.25]:timber('Door glass mullion',(cx+xoff,1.92,3.38),(.042,1.02,.06),'WoodHoney',1)
  for yy in [1.75,2.08]:timber('Door glass crossbar',(cx,yy,3.38),(.99,.042,.06),'WoodHoney',1)
  timber('Door lower rail',(cx,.43,3.31),(1.22,.14,.15))
  timber('Door middle rail',(cx,1.30,3.31),(1.22,.13,.15))
  beam('Door diagonal brace',(cx-side*.52,.49,3.34),(cx+side*.52,1.23,3.34),.13,.13)
  for yy in [.55,1.19]:timber('Dark hinge strap',(cx+side*.39,yy,3.40),(.35,.075,.045),'Iron',1)
  timber('Door iron pull',(side*.12,1.32,3.45),(.06,.31,.09),'Iron',1)
 frame('Double door heavy jamb',0,1.40,3.23,2.83,2.70,.20)
 timber('Door threshold',(0,.105,3.50),(3.0,.21,.73),'Paving')
 # Sign board and stylized wheat ornament above the type.
 timber('Barn Store sign outer frame',(0,3.22,3.57),(4.38,.76,.20))
 timber('Barn Store sign face',(0,3.22,3.69),(4.12,.56,.055),'Paper')
 letters('BARN STORE lettering','BARN STORE',(0,3.19,3.73),.47,3.56,detail=0)
 for sign in [-1,1]:
  cyl('Sign corner bolt',(sign*2.04,3.23,3.75),.035,.025,'Iron',8,1)
 for xx in [-.22,0,.22]:
  wheat((xx,3.57,3.75),.24)
  for piece in parts[-7:]:assign(piece,'WoodHoney')
 barn_lamp((-1.22,3.89,3.50));barn_lamp((1.22,3.89,3.50))
 # Left window, flower box and hanging planters at the entrance.
 wx=-3.56
 timber('Left window recess',(wx,1.80,3.10),(1.42,1.37,.10),'WoodDark')
 timber('Left warm window',(wx,1.80,3.18),(1.20,1.15,.035),'GlowWarm');assign(parts[-1],'GlowWarm',2)
 frame('Left window deep surround',wx,1.80,3.24,1.45,1.40,.14)
 for xx in [wx-.30,wx,wx+.30]:timber('Left window bar',(xx,1.80,3.32),(.045,1.23,.08),'WoodHoney',1)
 for yy in [1.61,1.99]:timber('Left window crossbar',(wx,yy,3.32),(1.29,.05,.08),'WoodHoney',1)
 timber('Left flower box',(wx,1.04,3.28),(1.68,.28,.48),'WoodDark',1);plants((wx,1.25,3.31),22,.70)
 for xx in [-1.58,1.60]:
  timber('Hanging planter',(xx,1.73,3.43),(.59,.28,.42),'WoodDark',1);plants((xx,1.94,3.43),12,.28)
 # Right display: three rows of grain sacks and seed boxes with actual shelving.
 timber('Display shadow back',(3.24,1.37,2.20),(2.10,2.12,.12),'WoodDark')
 for yy in [.28,1.03,1.77,2.30]:timber('Open feed display shelf',(3.26,yy,2.70),(2.20,.12,.85),'WoodHoney',1)
 for xx in [2.17,4.35]:timber('Display shelf upright',(xx,1.31,2.67),(.12,2.20,.62))
 for yy in [.35,1.10,1.84]:
  for xx in [2.51,3.22,3.93]:feedbag((xx,yy,2.75),.43,.62,.35)
 for level in range(3):
  for col in range(3-level):seedbox((2.65+col*.43+level*.20,.02+level*.34,3.63),.40,.32)
 feedbag((3.67,.03,3.51),.61,.93,.49)
 # Seed display stack to the left of the main entrance, staying clear of the door.
 for level in range(3):
  for col in range(2):seedbox((-2.30+col*.43,.02+level*.34,3.60),.39,.32)
 for xx in [-3.38,-2.81]:feedbag((xx,.02,3.78),.51,.89,.40)
 flowerbarrel((-4.44,0,3.54),.36,.58)
 flowerbarrel((4.50,0,3.62),.37,.57)
 pitchfork((1.78,.16,3.47));shovel((2.09,.07,3.66));bucket((2.54,.02,3.92),.30,.45)
 # One rooster vane, authored as part of the store instead of a separate dummy.
 beam('Weather vane mast',(0,5.23,-.30),(0,5.74,-.30),.035,.035,'Iron',1)
 beam('Weather vane arrow',(-.42,5.53,-.30),(.42,5.53,-.30),.03,.03,'Iron',1)
 bird('Barn Store rooster vane',(0,5.69,-.30),.48,'Iron',1)
 # Readable rim on the right open canopy is geometry, not painted shading.
 beam('Right display outer roof brace',(4.40,2.40,3.12),(2.12,2.40,3.12),.16,.20)
 return [],(0,0,0)

door,hinge=build();export_and_render('BarnStore_Cozy',door,hinge)
