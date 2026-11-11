"""Distinct radial flower forms with closed petals and continuous stem/head joints."""
import math,random
from mathutils import Vector
import plant_blender_common as C
from g08_geometry import path_tube
from g09_geometry import Frame,direction
TAU=math.tau
KINDS=['Sunflower','Chrysanthemum','Dahlia','Marigold']

def blade(kind,origin,d,length,width,slot=None,roll=0,seedling=False):
 origin=Vector(origin);d=Vector(d).normalized();n=Vector((d.x*.18,-.50+d.y*.12,.86));n-=d*n.dot(d)
 if n.length<.01:n=Vector((1,0,0))-d*d.x
 n.normalize();u=d.cross(n).normalized();u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
 rows,cols=(13,3) if kind=='Marigold' else (21,5);verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);profile=max(.003,math.sin(math.pi*t)**.68)
  if not seedling:
   if kind=='Sunflower':profile=max(.003,math.sin(math.pi*t)**.54)*(1.25-.62*t)*(1-.045*math.cos(t*TAU*6))
   elif kind=='Chrysanthemum':profile*=.65+.35*math.cos((t-.12)*TAU*3);profile*=1-.08*math.cos(t*TAU*12)
   elif kind in ['Dahlia','Marigold']:profile*=.92+.08*math.cos(t*TAU*10)
  for j in range(cols):
   q=j/(cols-1)*2-1;bow=.11*math.sin(math.pi*t)-.12*t*t+.07*q*q*math.sin(math.pi*t)-.022*math.exp(-q*q/.06)*math.sin(math.pi*t)
   bow+=.010*math.sin(t*math.pi*7)*q*q*math.sin(math.pi*t)
   verts.append(origin+d*(length*t)+u*(width*.5*profile*q)+n*(length*bow));uv.append(((q+1)/2,t))
 for i in range(rows-1):
  for j in range(cols-1):k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 ob=C.mesh(kind+' sculpted '+('cotyledon' if seedling else 'leaf'),verts,faces,slot or kind+'Leaf',uv)
 m=ob.modifiers.new('Living leaf thickness','SOLIDIFY');m.thickness=.0015;m.offset=0;ob['ReferenceLeaf']=kind;ob['LeafForm']='cotyledon' if seedling else kind
 return ob

def leaf(kind,p,angle,elevation,length,width,compound=True):
 p=Vector(p);d=direction(angle,elevation)
 if kind=='Marigold' and compound:
  tip=p+d*length*.82;path_tube('Marigold pinnate rachis',[p,p.lerp(tip,.5)+Vector((0,0,.006)),tip],.0018,'StemGreen',sides=6,taper=.35)
  for i in range(4):
   t=.15+i*.19;anchor=p.lerp(tip,t)
   for sign in [-1,1]:blade(kind,anchor,direction(angle+sign*57,elevation+8),length*(.47-.055*i),width*.55,roll=sign*.10)
  blade(kind,tip,d,length*.31,width*.48);return
 start=p+d*.025;path_tube(kind+' leaf petiole',[p,start],.0025 if kind!='Sunflower' else .004,'StemGreen',sides=7,taper=.25)
 blade(kind,start,d,length,width,roll=.08*math.sin(angle))
 if kind=='Dahlia' and compound:
  for sign in [-1,1]:blade(kind,p+d*.015,direction(angle+sign*52,elevation),length*.68,width*.68,roll=sign*.12)

def petal(f,kind,a,r0,length,width,z,rise,curl,cup,index):
 rows,cols=(15,5) if kind=='Sunflower' else (17,7) if kind=='Marigold' else (13,5);verts=[];uv=[];faces=[]
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))/2
   if kind=='Dahlia':profile=max(.001,math.sin(math.pi*t)**.86)*(.55+.45*t)
   elif kind=='Chrysanthemum':profile=max(.001,math.sin(math.pi*t)**.32)*(.35+.65*t**.45)
   elif kind=='Marigold':profile=max(.001,math.sin(math.pi*t)**.28)*(.40+.60*t**.5)
   else:profile=max(.001,math.sin(math.pi*t)**.66)
   for j in range(cols):
    q=j/(cols-1)*2-1;rr=r0+length*(t-.06*q*q*t*t);xx=width*.5*profile*q
    if kind=='Chrysanthemum':rr=r0+length*(t+.04*math.sin(math.pi*t)-.07*t**6-.035*q*q*t*t)
    zz=z+rise*math.sin(math.pi*t*.65)+curl*t**4+cup*q*q*math.sin(math.pi*t)
    if kind=='Marigold':
     zz+=length*.045*math.sin(q*math.pi*2.7+index*.7)*t**2
     rr+=length*.025*math.cos(q*math.pi*2+index)*t**4
    elif kind=='Dahlia':zz+=length*.055*abs(q)**1.6*math.sin(math.pi*t)
    else:zz+=length*.012*math.sin(q*math.pi*3+index)*t*t
    half=.00008+.0012*math.sin(math.pi*t)**.6*max(0,1-q*q)**.6
    verts.append(f.p(rr*math.cos(a)-xx*math.sin(a),rr*math.sin(a)+xx*math.cos(a),zz+side*half));uv.append(((q+1)/2,t))
 n=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+cols,k+cols+1,k+1);faces.extend([face,tuple(v+n for v in reversed(face))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,k in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((k,b,b+n,k+n))
 ob=C.mesh(kind+' closed cupped petal',verts,faces,kind+'Petal',uv);ob['PetalKind']=kind;ob['ClosedPetal']=True
 return ob

def oriented_sphere(name,f,z,scale,slot,sub=2):
 ob=C.sphere(name,f.p(0,0,z),scale,slot,sub);ob.rotation_euler=Vector((0,0,1)).rotation_difference(f.n).to_euler();return ob

def calyx(kind,f,index,scale,bud=False):
 back=.032*scale;attach=f.p(0,0,-back)
 ob=path_tube(kind+' calyx neck',[attach,f.p(0,0,-.012*scale)],.014*scale,'SepalGreen',sides=10,taper=-.35)
 ob['JointRole']='head';ob['JointId']=('Bud' if bud else 'Flower')+str(index);ob['JointPoint']=list(attach);ob['JointNormal']=list(f.n)
 r=(.070 if kind=='Sunflower' else .043)*scale
 oriented_sphere(kind+' green receptacle',f,-.014*scale,(r,r,.022*scale),'SepalGreen')
 count=10 if kind=='Sunflower' else 6
 for j in range(count):
  a=TAU*j/count;origin=f.p(r*.50*math.cos(a),r*.50*math.sin(a),-.017*scale)
  d=(f.x*math.cos(a)+f.y*math.sin(a))*(.70 if bud else .95)+f.n*(.8 if bud else -.15)
  blade('Sepal',origin,d,(.058 if kind=='Sunflower' else .035)*scale,.015*scale,'SepalGreen',seedling=True)
 return attach

def florets(f,scale):
 # Low-cost raised florets laid in a phyllotactic spiral, one mesh for the whole disk.
 verts=[];uv=[];faces=[];count=200
 for j in range(count):
  r=.111*scale*math.sqrt((j+.5)/count);a=j*2.399963;z=.014*scale+.018*scale*(1-(r/(.115*scale))**2)
  center=f.p(r*math.cos(a),r*math.sin(a),z);rad=.0045*scale;start=len(verts)
  for zoff,s in [(0,1),(.0032*scale,.56)]:
   for k in range(5):
    b=TAU*k/5;verts.append(center+f.x*(rad*s*math.cos(b))+f.y*(rad*s*math.sin(b))+f.n*zoff);uv.append((.5+.45*(math.cos(a)*r+rad*s*math.cos(b))/(.115*scale),.5+.45*(math.sin(a)*r+rad*s*math.sin(b))/(.115*scale)))
  for k in range(5):faces.append((start+k,start+(k+1)%5,start+5+(k+1)%5,start+5+k))
  faces.append(tuple(start+5+k for k in range(5)))
 ob=C.mesh('Sunflower raised spiral disk florets',verts,faces,'SunflowerDisk',uv);ob['DiskFlorets']=count

def flower(kind,center,normal,index,scale=1):
 f=Frame(center,normal);before=set(C.current.objects);attach=calyx(kind,f,index,scale)
 if kind=='Sunflower':
  oriented_sphere('Sunflower domed brown disk',f,.003*scale,(.119*scale,.119*scale,.027*scale),'SunflowerDisk',3);florets(f,scale)
  rings=[(23,.096,.139,.048,-.007,.018,-.025,.003),(20,.088,.128,.050,.002,.026,-.018,.004)]
 elif kind=='Chrysanthemum':
  oriented_sphere(kind+' enclosed golden heart',f,.025*scale,(.052*scale,.052*scale,.034*scale),kind+'Petal')
  rings=[(26,.020,.124,.053,-.022,.031,.013,.004),(24,.017,.106,.051,-.003,.045,.018,.005),(22,.013,.087,.047,.016,.053,.020,.006),(20,.008,.066,.042,.034,.052,.021,.007),(16,.004,.046,.035,.050,.045,.017,.007),(12,.001,.029,.026,.064,.032,.012,.007)]
 elif kind=='Dahlia':
  oriented_sphere(kind+' pink petal heart',f,.036*scale,(.035*scale,.035*scale,.025*scale),kind+'Petal')
  rings=[(14,.024,.133,.066,-.025,.032,-.010,.015),(13,.020,.112,.062,-.006,.047,.002,.015),(12,.013,.091,.056,.014,.053,.005,.014),(10,.007,.065,.047,.032,.049,.006,.013),(8,.003,.040,.032,.046,.040,.004,.010)]
 else:
  oriented_sphere(kind+' orange heart',f,.041*scale,(.044*scale,.044*scale,.036*scale),kind+'Petal')
  rings=[(15,.020,.099,.060,-.018,.028,-.008,.010),(13,.015,.084,.056,.002,.043,.004,.012),(12,.010,.067,.050,.020,.047,.006,.013),(10,.005,.048,.041,.038,.042,.005,.012),(8,.002,.028,.028,.050,.031,.003,.008)]
 rng=random.Random(10400+index*19+KINDS.index(kind)*71)
 for layer,(count,r,l,w,z,rise,curl,cup) in enumerate(rings):
  for j in range(count):
   a=TAU*j/count+layer*.39+index*.19+rng.uniform(-.025,.025)
   petal(f,kind,a,r*scale,l*scale*rng.uniform(.96,1.04),w*scale*rng.uniform(.94,1.06),z*scale,rise*scale,curl*scale,cup*scale,j+layer*3)
 for ob in set(C.current.objects)-before:ob['FlowerKind']=kind;ob['FlowerIndex']=index
 marker=next(ob for ob in set(C.current.objects)-before if ob.get('JointRole')=='head');marker['OpenFlower']=kind
 return attach

def bud(kind,center,normal,index,scale=1):
 f=Frame(center,normal);attach=calyx(kind,f,index,scale,True)
 r={'Sunflower':.065,'Chrysanthemum':.046,'Dahlia':.050,'Marigold':.037}[kind]*scale
 length=(.073 if kind!='Marigold' else .101)*scale
 rows,sides=13,16;verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);rr=max(.001,r*math.sin(math.pi*t)**.58)*(1+.055*math.cos(t*TAU*2))
  for j in range(sides+1):
   a=TAU*j/sides;rrr=rr*(1+.065*math.cos(a*(8 if kind=='Marigold' else 7)))
   verts.append(f.p(rrr*math.cos(a),rrr*math.sin(a),length*t));uv.append((j/sides,t))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((rows-1)*(sides+1)+j for j in range(sides))])
 ob=C.mesh(kind+' ribbed closed bud',verts,faces,'BudGreen',uv);ob['ClosedBud']=kind
 if kind!='Sunflower':oriented_sphere(kind+' coloured bud tip',f,length*.83,(r*.45,r*.45,length*.18),kind+'Petal')
 return attach

def branch(kind,start,end,normal,joint,index,radius=.006):
 start,end,n=Vector(start),Vector(end),Vector(normal).normalized()
 a,b,c,d=start,start.lerp(end,.42)+Vector((0,0,.025)),end-n*.050,end
 points=[a*(1-t)**3+b*3*(1-t)**2*t+c*3*(1-t)*t*t+d*t**3 for t in [i/12 for i in range(13)]]
 points[-2]=end-n*.012
 ob=path_tube(kind+' continuous floral stem',points,radius,'StemGreen',sides=9,taper=.28)
 ob['JointRole']='stem';ob['JointId']=joint+str(index);ob['JointPoint']=list(end);ob['JointNormal']=list(n);ob['FlowerStem']=kind
 return points

def seed(kind):
 rng=random.Random(100+KINDS.index(kind))
 count=4 if kind=='Sunflower' else 5
 for i in range(count):
  p=Vector(((i%3-1)*.043,(i//3)*.041,.012));p.x+=rng.uniform(-.01,.01)
  if kind=='Sunflower':s=(.013,.027,.009);slot='SeedStripe'
  elif kind=='Chrysanthemum':s=(.005,.018,.004);slot='SeedBrown'
  elif kind=='Dahlia':s=(.009,.024,.0045);slot='SeedBlack'
  else:s=(.0035,.035,.003);slot='SeedBlack'
  ob=C.sphere(kind+' seed',p,s,slot,2);ob.rotation_euler.z=rng.uniform(-.6,.6);ob['SeedSpecies']=kind
  if kind=='Marigold':
   tip=p+Vector((0,-.027,0));path_tube('Marigold pale seed tip',[tip,tip+Vector((0,-.013,0))],.0028,'CutStem',sides=6,taper=.3)

def sprout(kind):
 path_tube(kind+' seedling stem',[(0,0,0),(0,0,.055),(.003,0,.105)],.006,'StemGreen',sides=9,taper=.25)
 for a in [20,200]:blade(kind,(.002,0,.088),direction(a,24),.087,.049,seedling=True)

def foliage(kind,stage):
 young=stage=='Young';h=(.40 if young else 1.02) if kind=='Sunflower' else (.24 if young else .45)
 points=[Vector((.018*math.sin(t*2),.012*t,h*t)) for t in [i/12 for i in range(13)]]
 path_tube(kind+' upright main stem',points,.014 if kind=='Sunflower' else .009,'StemGreen',sides=10,taper=.35)
 count=(6 if young else 11) if kind=='Sunflower' else (4 if young else 7)
 for i in range(count):
  a=195+i*137.5;z=h*(.12+.73*i/max(1,count-1));p=(.012*math.sin(z/h*2),.012*z/h,z)
  l=(.18 if young else .245)*(1-.26*i/count) if kind=='Sunflower' else (.11 if young else .15)
  w=l*(.72 if kind=='Sunflower' else .68 if kind=='Chrysanthemum' else .52 if kind=='Dahlia' else .23)
  leaf(kind,p,a,25+(i%3)*5,l,w)
 if young:return
 if kind!='Sunflower':
  for j,a in enumerate([20,150,265]):
   end=Vector((.105*math.cos(math.radians(a)),.105*math.sin(math.radians(a)),.32+j*.035))
   path_tube(kind+' leafy side branch',[(0,0,.06),end*.55+Vector((0,0,.035)),end],.005,'StemGreen',sides=8,taper=.35)
   for k in [0,1]:leaf(kind,end*.55+Vector((0,0,k*.06)),a+(-60 if k else 50),25,.142,.027 if kind=='Marigold' else .075)

def layout(kind):
 if kind=='Sunflower':return [((.015,.01,1.16),(-.10,-.88,.46),1.0),((-.22,.015,.87),(-.30,-.78,.56),.66)]
 if kind=='Chrysanthemum':return [((0,.045,.68),(-.08,-.70,.70),1.03),((-.22,-.08,.49),(-.24,-.85,.46),.91),((.22,-.015,.55),(.20,-.79,.58),.91)]
 if kind=='Dahlia':return [((0,.04,.75),(-.04,-.77,.64),1.03),((-.23,-.09,.54),(-.20,-.85,.48),.90),((.23,-.02,.58),(.22,-.82,.53),.90)]
 return [((-.07,.04,.64),(-.08,-.75,.66),1.04),((-.23,-.10,.45),(-.25,-.85,.46),.92),((.20,-.07,.48),(.22,-.83,.51),.98),((.20,.10,.66),(.32,-.60,.73),.77)]

def plant(kind,stage):
 foliage(kind,stage)
 if stage=='Young':return
 for i,(center,n,scale) in enumerate(layout(kind)):
  if stage=='Ready':end=flower(kind,center,n,i,scale);joint='Flower'
  else:end=bud(kind,center,(0,-.20,.98),i,scale);joint='Bud';n=(0,-.20,.98)
  h=1.02 if kind=='Sunflower' else .45
  start=(.018*math.sin(2),.012,h) if i==0 else (0,0,.36 if kind=='Sunflower' else .18)
  pts=branch(kind,start,end,n,joint,i,.009 if kind=='Sunflower' else .006)
  leaf(kind,pts[5],160 if i%2 else 20,30,.15 if kind=='Sunflower' else .105,.11 if kind=='Sunflower' else .025 if kind=='Marigold' else .062)
 if stage=='Ready':
  center=(.19,.11,.95) if kind=='Sunflower' else (-.15,.14,.65 if kind=='Dahlia' else .58)
  n=(.12,-.18,.98);end=bud(kind,center,n,9,.67);branch(kind,(0,0,.30 if kind=='Sunflower' else .22),end,n,'Bud',9,.005)

def harvest(kind):
 # A flower sprig lies naturally above the ground; stem ends exactly at the calyx.
 start=Vector((-.33,-.02,.018));normal=Vector((.46,-.22,.86)).normalized();center=Vector((.14,.035,.12 if kind=='Sunflower' else .105))
 end=flower(kind,center,normal,0,.80 if kind=='Sunflower' else 1.0)
 pts=branch(kind,start,end,normal,'Flower',0,.008 if kind=='Sunflower' else .006)
 for k,a in [(4,145),(7,40)]:leaf(kind,pts[k],a,8,.12 if kind!='Sunflower' else .16,.025 if kind=='Marigold' else .075)
 cut=C.sphere(kind+' cut stem end',start,(.008,.008,.004),'CutStem',2);cut['HarvestCut']=True

def build(kind,stage):
 if stage=='Seed':seed(kind)
 elif stage=='Sprout':sprout(kind)
 elif stage=='Harvest':harvest(kind)
 else:plant(kind,stage)
