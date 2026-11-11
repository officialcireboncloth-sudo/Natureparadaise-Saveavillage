"""Spatial berry shrubs with recessed calyx crowns and individually sculpted drupelets."""
import math
import bpy,bmesh
from mathutils import Vector
import plant_blender_common as C
from g12_geometry import bezier,sample,closed_grid
from g09_geometry import Frame
from g08_geometry import path_tube
TAU=math.tau

def leaf(origin,angle,elevation,length,width,index,kind,roll=0):
 origin=Vector(origin);a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));across=Vector((radial.y,-radial.x,0));e=math.radians(elevation)
 points=[origin,origin+radial*length*.30*math.cos(e)+Vector((0,0,length*.44*math.sin(e))),origin+radial*length*.78*math.cos(e)+Vector((0,0,length*.92*math.sin(e)+.022)),origin+radial*length*math.cos(e)+Vector((0,0,length*math.sin(e)-.009))]
 rows,cols=(23,5) if kind=='Blackberry' else (17,5);verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=i/(rows-1);center=bezier(points,t);p0,p1,p2,p3=points;tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized();u=(across-tangent*across.dot(tangent)).normalized();n=u.cross(tangent).normalized();r=roll+.20*math.sin(index*1.73)*t;u,n=u*math.cos(r)+n*math.sin(r),-u*math.sin(r)+n*math.cos(r)
   profile=.001+math.sin(math.pi*t)**(.63 if kind=='Blackberry' else .66)*(1.03-.15*t)
   if kind=='Blackberry':profile*=1.08 if i%2 else .88
   for j in range(cols):
    q=j/(cols-1)*2-1;edge=math.sin(math.pi*t);rib=.003*math.exp(-q*q/.07)*edge;cup=width*.10*q*q*edge;half=.00025+.0025*edge**.7*max(0,1-q*q)**.6
    verts.append(center+u*width*.5*profile*q+n*(rib+cup+side*half));uv.append(((q+1)/2,.015+.97*t))
 ob=closed_grid(kind+' curved '+('serrated' if kind=='Blackberry' else 'oval')+' leaf',verts,uv,rows,cols,kind+'Leaf');ob['ReferenceLeaf']=kind;ob['LeafIndex']=index;ob['SpatialLeaf']=True;ob['LeafFrame']='radial growth and world up';ob['Serrated']=kind=='Blackberry';return ob

def surface_faces(ob,first,slot):
 a,b,c,d=C.slots[slot]['uvSafe'];uv=ob.data.uv_layers.active
 for poly in ob.data.polygons[first:]:
  for li in poly.loop_indices:
   p=ob.data.vertices[ob.data.loops[li].vertex_index].co;u=.5+.35*math.sin(p.x*19+p.z*9);v=.5+.35*math.cos(p.y*17-p.z*7);uv.data[li].uv=(a+(c-a)*u,b+(d-b)*v)

def blueberry(center,axis,radius,index,green=False):
 f=Frame(center,axis);rows,sides=16,30;verts=[];uv=[];faces=[]
 for i in range(rows):
  theta=math.pi-(math.pi-.40)*i/(rows-1);rr=radius*max(.001,math.sin(theta));z=radius*.90*math.cos(theta)
  for j in range(sides+1):
   a=TAU*j/sides;verts.append(f.p(rr*math.cos(a),rr*math.sin(a),z));uv.append((j/sides,.02+.88*i/(rows-1)))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.append(tuple(range(sides-1,-1,-1)));crownStart=len(faces)
 # The star rim rises above a genuinely recessed bowl; it is part of the same closed mesh.
 for rr,z in [(0,0),(0,0)]:
  ring=len(verts)//(sides+1)
  for j in range(sides+1):
   a=TAU*j/sides;wave=math.cos(5*a);r=radius*((.29+.085*wave) if ring==rows else .16);zz=radius*((.91+.10*wave) if ring==rows else .65);verts.append(f.p(r*math.cos(a),r*math.sin(a),zz));uv.append((j/sides,.94))
  for j in range(sides):k=(ring-1)*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.append(tuple((rows+1)*(sides+1)+j for j in range(sides)));ob=C.mesh('Blueberry recessed five-point calyx fruit',verts,faces,'BerryGreen' if green else 'BlueberrySkin',uv);surface_faces(ob,crownStart,'StemGreen' if green else 'BerryCrown');ob['BerryKind']='Blueberry';ob['FruitIndex']=index;ob['RecessedCalyx']=True;ob['Unripe']=green;ob['CalyxLobes']=5
 attach=f.p(0,0,-radius*.96);neck=path_tube('Blueberry attached receptacle',[attach,f.p(0,0,-radius*.85)],radius*.12,'StemGreen',sides=9,taper=.1);neck['JointRole']='head';neck['JointId']='Fruit_'+str(index);neck['JointPoint']=list(attach);return attach,f.n

def petal(frame,angle,length,width,index):
 rows,cols=9,5;verts=[];uv=[];a=angle;d=Vector((math.cos(a),math.sin(a)));u=Vector((-d.y,d.x))
 for side in [1,-1]:
  for i in range(rows):
   t=i/(rows-1);profile=.001+math.sin(math.pi*t)**.55
   for j in range(cols):
    q=j/(cols-1)*2-1;xy=d*(.008+length*t)+u*(width*.5*profile*q);z=.016*math.sin(math.pi*t)+.009*t*t+.006*q*q*math.sin(math.pi*t);half=.00025+.0016*math.sin(math.pi*t)*max(0,1-q*q)
    verts.append(frame.p(xy.x,xy.y,z+side*half));uv.append(((q+1)/2,t))
 ob=closed_grid('Blackberry five-petal flower lobe',verts,uv,rows,cols,'FlowerIvory');ob['BerryFlowerPetal']=True;ob['FlowerIndex']=index

def sepals(frame,radius,kind,index):
 for j in range(5):
  a=TAU*j/5;d=frame.x*math.cos(a)+frame.y*math.sin(a);start=frame.c+d*radius*.25;end=start+d*radius*.9+frame.n*radius*.25
  # A thick pointed sepal is a closed little tapered lens in its local frame.
  f=Frame(start,end-start);v=[f.p(-radius*.17,0,0),f.p(radius*.17,0,0),f.p(0,-radius*.08,0),f.p(0,radius*.08,0),end];faces=[(0,3,1),(0,1,2),(0,4,3),(3,4,1),(1,4,2),(2,4,0)];ob=C.mesh(kind+' green calyx sepal',v,faces,'StemGreen',[(0,0),(1,0),(.5,0),(.5,.1),(.5,1)]);ob['BerrySepal']=True;ob['FruitIndex']=index

def flower(center,axis,index,kind,size=1):
 f=Frame(center,axis)
 if kind=='Blueberry':
  # Urn/bell hangs downward; the opening has five physical folded lobes and an inner wall.
  rows,sides=13,30;verts=[];uv=[];faces=[]
  for layer in [0,1]:
   for i in range(rows):
    t=i/(rows-1);r=(.011+.025*math.sin(math.pi*t*.89)**.70)*size
    if layer:r=max(.004,r-.0025*size)
    for j in range(sides+1):
     a=TAU*j/sides;lip=t**6;rr=r+lip*.006*math.cos(5*a)*size;z=(.072*t+.009*lip*math.cos(5*a))*size;verts.append(f.p(rr*math.cos(a),rr*math.sin(a),z));uv.append((j/sides,t))
  nv=rows*(sides+1)
  for i in range(rows-1):
   for j in range(sides):
    k=i*(sides+1)+j;face=(k,k+1,k+sides+2,k+sides+1);faces.extend([face,tuple(v+nv for v in reversed(face))])
  for j in range(sides):
   k=(rows-1)*(sides+1)+j;faces.append((k,k+nv,k+nv+1,k+1));faces.append((j,j+1,j+nv+1,j+nv))
  ob=C.mesh('Blueberry hollow cream five-lobed bell',verts,faces,'FlowerIvory',uv);ob['BellFlower']=True;ob['FlowerIndex']=index
  sepals(Frame(center,-f.n),.024*size,kind,index)
 else:
  for j in range(5):petal(f,TAU*j/5,.050*size,.039*size,index)
  C.sphere('Blackberry pollen center',f.p(0,0,.016),(.016*size,.016*size,.010*size),'PollenGold',2)
  for j in range(12):
   a=TAU*j/12;C.sphere('Blackberry golden anther',f.p(.022*math.cos(a)*size,.022*math.sin(a)*size,.022*size),(.0045*size,)*3,'PollenGold',1)
  sepals(Frame(center,-f.n),.028*size,kind,index)
 attach=f.p(0,0,-.015*size);ob=path_tube(kind+' flower connected neck',[attach,center],.006*size,'StemGreen',sides=9,taper=.1);ob['OpenFlower']=kind;ob['JointRole']='head';ob['JointId']='Flower_'+str(index);ob['JointPoint']=list(attach);return attach,f.n

def blackberry(center,axis,size,index,color='BlackberryDrupe',half=False):
 f=Frame(center,axis);before=set(C.current.objects);counts=[5,7,8,8,7,5];beads=0
 for row,count in enumerate(counts):
  t=row/(len(counts)-1);z=(-.072+.144*t)*size;r=(.024+.029*math.sin(math.pi*t)**.7)*size
  for j in range(count):
   a=TAU*j/count+(row%2)*.30;pos=f.p(r*math.cos(a),r*math.sin(a),z)
   if half and z>.004:continue
   ob=C.sphere('Blackberry individual plump drupelet',pos,(.023*size,.023*size,.025*size),color,2);ob['BlackberryDrupelet']=True;ob['FruitIndex']=index;ob['DrupeRow']=row;beads+=1
 if half:
  # Cross section: opaque cream core with a scalloped ring of cut purple drupelets.
  verts=[];uv=[];n=40
  for z in [-.012*size,.010*size]:
   for j in range(n):
    a=TAU*j/n;r=(.038+.005*math.cos(a*8))*size;verts.append(f.p(r*math.cos(a),r*math.sin(a),z));uv.append((.5+.45*math.cos(a),.5+.45*math.sin(a)))
  faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)];C.mesh('Blackberry cream scalloped cut core',verts,faces,'FlowerIvory',uv)['BerryCutCore']=True
  for j in range(9):
   a=TAU*j/9;ob=C.sphere('Blackberry cross-section drupelet',f.p(.052*math.cos(a)*size,.052*math.sin(a)*size,-.005*size),(.023*size,.023*size,.013*size),color,2);ob.rotation_euler=f.n.to_track_quat('Z','Y').to_euler();ob['BlackberryDrupelet']=True;ob['FruitIndex']=index;ob['CutDrupelet']=True;beads+=1
 else:sepals(Frame(f.p(0,0,.089*size),f.n),.038*size,'Blackberry',index)
 ob=C.sphere('Blackberry embedded fruit core',f.p(0,0,(-.040 if half else -.025)*size),(.037*size,.037*size,(.034 if half else .049)*size),color,2);ob.rotation_euler=f.n.to_track_quat('Z','Y').to_euler();ob['BerryKind']='Blackberry';ob['FruitIndex']=index;ob['Drupelets']=beads;ob['CrossSection']=half;ob['Ripeness']=color
 if half:return None,None
 attach=f.p(0,0,.113*size);neck=path_tube('Blackberry attached calyx neck',[attach,f.p(0,0,.088*size)],.007*size,'StemGreen',sides=9,taper=.1);neck['JointRole']='head';neck['JointId']='Fruit_'+str(index);neck['JointPoint']=list(attach);return attach,-f.n

def pedicel(start,end,axis,key,kind,radius=.007):
 start,end,axis=Vector(start),Vector(end),Vector(axis).normalized();pts=sample([start,start.lerp(end,.35)+Vector((0,0,.025)),end-axis*.040,end],17);pts[-2]=end-axis*.008
 ob=path_tube(kind+' continuous fruit/flower pedicel',pts,radius,'StemGreen',sides=9,taper=.15);ob['JointRole']='stem';ob['JointId']=key;ob['JointPoint']=list(end);ob['AnchorPoint']=list(start)

def shrub(kind,stage):
 if stage=='Sprout':
  path_tube(kind+' sprout trunk',[(0,0,.008),(0,0,.13),(0,0,.19)],.016,'BarkRed',sides=10,taper=.3)
  for i,(a,e) in enumerate([(188,32),(18,48)]):leaf((0,0,.16),a,e,.23,.13 if kind=='Blueberry' else .12,i,kind,.25*(-1 if i else 1))
  if kind=='Blackberry':leaf((0,0,.17),280,25,.15,.085,2,kind,-.4)
  return
 factor=.62 if stage=='Young' else .90 if stage=='Flowering' else 1.;idx=0;branches=[]
 canes=[(-.22,.04,.58),(0,.10,.70),(.22,-.015,.62)] if kind=='Blueberry' else [(-.22,.04,.64),(0,.10,.82),(.22,-.015,.70)]
 for j,(x,y,height) in enumerate(canes):
  base=Vector(((j-1)*.033,0,.009));end=Vector((x,y,height))*factor;curve=[base,base+Vector((0,0,.23))*factor,end-Vector((x*.4,y*.4,.18))*factor,end];path_tube(kind+' branching woody cane',sample(curve,25),.025*factor,'BarkRed',sides=10,taper=.68)
  nodes=([.35,.67] if kind=='Blueberry' else [.32,.57,.81]) if stage=='Young' else [.22,.40,.58,.75,.91]
  for node,t in enumerate(nodes):
   pos=bezier(curve,t);a=math.degrees(math.atan2(y,x))+(-1 if node%2 else 1)*(42+17*j);direction=Vector((math.cos(math.radians(a)),math.sin(math.radians(a)),.25)).normalized();tip=pos+direction*(.15+.035*(j%2))*factor
   branch=sample([pos,pos.lerp(tip,.4)+Vector((0,0,.02))*factor,tip-Vector((0,0,.025))*factor,tip],13);path_tube(kind+' leaf-bearing lateral',branch,.012*factor,'BarkRed',sides=8,taper=.65)
   angles=[a] if stage=='Young' and kind=='Blueberry' else [a-34,a+38]
   for k,angle in enumerate(angles):
    origin=tip if k==0 or kind=='Blackberry' else branch[len(branch)//2]
    leaf(origin,angle,48+15*math.sin(idx*1.91)+8*(node%2),((.25 if kind=='Blueberry' else .29)+.025*math.sin(idx))*factor,(.185 if kind=='Blueberry' else .140)*factor,idx,kind,.32*math.sin(idx*1.7));idx+=1
   branches.append(tip)
   if kind=='Blackberry' and node<4:
    start=pos+direction*.012;C.tube('Blackberry small cane thorn',start,start+direction*.030*factor+Vector((0,0,.013))*factor,.006*factor,'BarkRed',r2=.0006)['BerryThorn']=True
  leaf(end,[150,65,30][j],68,(.22 if kind=='Blueberry' else .25)*factor,(.17 if kind=='Blueberry' else .13)*factor,idx,kind,.18*math.sin(j));idx+=1
 # Crown leaves close the silhouette without forcing them toward the camera.
 if stage!='Young':
  for j in range(5):leaf((.07*math.cos(j*TAU/5),.07*math.sin(j*TAU/5),.08),j*72+15,25,.25,.14 if kind=='Blueberry' else .12,idx,kind,.4*math.sin(j));idx+=1
  if kind=='Blueberry':
   for z in [.20,.36,.51]:
    for sign in [-1,1]:
     start=Vector((0,.075,z))*factor;end=start+Vector((sign*.075,.025,.015))*factor;path_tube('Blueberry inner canopy twig',[start,end],.008*factor,'BarkRed',sides=8,taper=.45)
     leaf(end,155 if sign<0 else 25,42,.245*factor,.17*factor,idx,kind,.24*sign);idx+=1;branches.append(end)
 return branches

BLUE_FRUIT=[(-.27,-.35,.49,.105),(-.06,-.38,.65,.115),(.21,-.32,.68,.115),(.33,-.34,.49,.105),(.14,-.42,.36,.115),(-.19,-.40,.22,.100),(.33,-.37,.22,.092)]
BLACK_FRUIT=[(-.33,-.30,.67,.93),(.25,-.28,.60,1.0),(.39,-.30,.52,.91),(-.31,-.36,.35,.99),(-.13,-.40,.27,.97),(.20,-.37,.26,1.0),(.35,-.32,.22,.95),(.46,-.28,.20,.77)]

def build(kind,stage):
 if stage=='Seed':
  for i,(x,y,a) in enumerate([(-.040,.012,.2),(.020,.035,-.3),(.005,-.031,.5),(.056,-.013,-.6)]):
   ob=C.sphere(kind+' golden oblong seed',(x,y,.009),(.023,.014,.007),'SeedTan',2);ob.rotation_euler=(.1,.15,a);ob['BerrySeed']=kind
  return
 if stage=='Harvest':
  if kind=='Blueberry':
   for i,(center,r,a) in enumerate([((-.13,.04,.12),.14,(0,-.4,1)),((.13,.055,.10),.11,(.2,-.35,1)),((.015,-.16,.085),.10,(-.15,-.5,1))]):blueberry(center,a,r,i)
  else:
   blackberry((-.13,.065,.11),(0,-.1,1),1.3,0);blackberry((.15,.10,.10),(.1,-.15,1),1.15,1);blackberry((.025,-.16,.055),(0,-.70,.71),1.25,2,half=True)
  return
 anchors=shrub(kind,stage)
 if stage not in ['Flowering','Ready']:return
 def connect(end,n,key):
  start=min(anchors,key=lambda p:(Vector(p)-Vector(end)).length);pedicel(start,end,n,key,kind)
 if kind=='Blueberry':
  if stage=='Ready':
   for i,(x,y,z,r) in enumerate(BLUE_FRUIT):
    axis=(x*.65,-.65,.82);attach,n=blueberry((x,y,z),axis,r,i);connect(attach,n,'Fruit_'+str(i))
  else:
   for i,(x,y,z,r) in enumerate(BLUE_FRUIT[:4]):
    center=(x*.93,y,z*.90);axis=(x*.25,-.30,-.95);attach,n=flower(center,axis,i,kind,1.1);connect(attach,n,'Flower_'+str(i))
   for i,(x,y,z,r) in enumerate(BLUE_FRUIT[4:]):
    attach,n=blueberry((x*.93,y,z*.9),(x*.65,-.65,.82),r*.73,i,True);connect(attach,n,'Fruit_'+str(i))
 else:
  fruit=BLACK_FRUIT if stage=='Ready' else BLACK_FRUIT[:6]
  for i,(x,y,z,size) in enumerate(fruit):
   color=('BerryRed' if i==7 else 'BlackberryDrupe') if stage=='Ready' else ('BerryRed' if i==5 else 'BerryGreen');z*=1 if stage=='Ready' else .90;attach,n=blackberry((x,y,z),(x*.10,.10,1),size*(1 if stage=='Ready' else .72),i,color);connect(attach,n,'Fruit_'+str(i))
  if stage=='Flowering':
   for i,(x,y,z) in enumerate([(-.24,-.21,.72),(.12,-.20,.86),(.31,-.22,.52),(-.12,-.24,.45)]):
    attach,n=flower((x,y,z),(x*.2,-.8,.6),i,kind,.90);connect(attach,n,'Flower_'+str(i))
