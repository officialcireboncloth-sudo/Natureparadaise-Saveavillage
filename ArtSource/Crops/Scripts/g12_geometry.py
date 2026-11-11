"""Spatial fleshy Phalaenopsis, not camera-facing foliage cards."""
import math,random
from mathutils import Vector
import plant_blender_common as C
from g09_geometry import Frame
from g08_geometry import path_tube
TAU=math.tau
def bezier(points,t):
 a,b,c,d=map(Vector,points);return a*(1-t)**3+3*b*(1-t)**2*t+3*c*(1-t)*t*t+d*t**3
def sample(points,n=25):return [bezier(points,i/(n-1)) for i in range(n)]

def closed_grid(name,verts,uv,rows,cols,slot):
 faces=[];nv=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;f=(k,k+1,k+cols+1,k+cols);faces.extend([f,tuple(v+nv for v in reversed(f))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,a in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((a,a+nv,b+nv,b))
 ob=C.mesh(name,verts,faces,slot,uv);ob['ClosedVolume']=True;return ob

def leaf(points,width,index,roll0,roll1):
 pts=list(map(Vector,points));p0,p1,p2,p3=pts;radial=Vector((p3.x-p0.x,p3.y-p0.y,0)).normalized();across=Vector((radial.y,-radial.x,0));rows,cols=29,11;verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;center=bezier(pts,t);tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized();u=(across-tangent*across.dot(tangent)).normalized();n=u.cross(tangent).normalized();roll=roll0+(roll1-roll0)*t;u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
   profile=.001+math.sin(math.pi*t)**.49*(1.12-.08*t)+.10*(1-t)**10
   for j in range(cols):
    q=j/(cols-1)*2-1;edge=math.sin(math.pi*t);fold=width*(.14*q*q-.028*math.exp(-q*q/.05))*edge;half=.00025+.0065*edge**.65*max(0,1-q*q)**.55
    verts.append(center+u*width*.5*profile*q+n*(fold+side*half))
    # Follow the diagonally painted main vein, rather than rotating geometry toward a camera.
    uv.append((.10+.80*t-.25*q*edge,.08+.84*t+.25*q*edge))
 ob=closed_grid('Orchid fleshy rolled leaf',verts,uv,rows,cols,'OrchidLeaf');ob['ReferenceLeaf']='Orchid';ob['LeafIndex']=index;ob['SpatialLeaf']=True;ob['LeafFrame']='radial growth and world up';ob['CurveControlPoints']=[list(p) for p in pts];ob['LeafRollRadians']=[roll0,roll1];return ob

LEAVES=[
 ([(-.012,.016,.10),(-.03,.075,.22),(-.21,.20,.42),(-.34,.22,.42)],.21,.15,-.10),
 ([(.01,.025,.10),(.05,.07,.25),(.22,.18,.43),(.40,.17,.40)],.22,-.20,.10),
 ([(-.02,-.01,.09),(-.09,-.10,.25),(-.30,-.14,.32),(-.39,-.13,.20)],.265,.15,.75),
 ([(.018,-.012,.09),(.07,-.10,.24),(.27,-.15,.30),(.40,-.18,.15)],.26,-.20,-.50),
 ([(-.015,.02,.09),(-.10,.0,.16),(-.24,.035,.12),(-.24,-.01,.045)],.17,.50,.10)]
def foliage(stage):
 if stage=='Sprout':
  leaf([(-.008,.004,.065),(-.04,.035,.13),(-.14,.07,.20),(-.16,.08,.17)],.105,0,.20,.65)
  leaf([(.009,-.003,.066),(.035,-.02,.16),(.105,-.04,.23),(.16,-.05,.215)],.11,1,-.40,-.05)
 else:
  factor=.72 if stage=='Young' else .94 if stage=='BudForming' else 1
  for i,(pts,w,r0,r1) in enumerate(LEAVES[:4 if stage=='Young' else 5]):
   scaled=[(x*factor,y*factor,.07+(z-.07)*factor) for x,y,z in pts];leaf(scaled,w*factor,i,r0,r1)

def roots(stage):
 factor=.42 if stage=='Sprout' else .78 if stage=='Young' else .93 if stage=='BudForming' else 1
 C.sphere('Orchid basal crown',(0,0,.071),(.043*factor,.036*factor,.039*factor),'SeedCream',2)['OrchidCrown']=True
 for i,a in enumerate([195,235,278,325,20,72,135]):
  a=math.radians(a);d=Vector((math.cos(a),math.sin(a),0));side=Vector((-d.y,d.x,0));points=[]
  for j in range(23):
   t=j/22;r=(.018+.20*t)*factor;p=d*r+side*(math.sin(t*math.pi*2.7+i*.8)*.019*factor);p.z=.009+(.066*(1-t)**1.8+.006*math.sin(t*math.pi*3+i)**2)*factor;points.append(p)
  ob=path_tube('Orchid rounded aerial root',points,.016*factor*(1+.11*math.sin(i)), 'RootVelamen',sides=9,taper=.57);ob['OrchidAerialRoot']=True;ob['RootIndex']=i

def tepal(frame,angle,length,width,offset,slot,role,index,scale=1,cup=.020,twist=0):
 d=Vector((math.sin(angle),math.cos(angle)));u=Vector((d.y,-d.x));rows,cols=21,9;verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;profile=.001+math.sin(math.pi*t)**.49
   for j in range(cols):
    q=j/(cols-1)*2-1;xx=width*.5*profile*q;yy=length*t;z=.010*math.sin(math.pi*t)-.025*t**3+cup*q*q*math.sin(math.pi*t)+twist*q*t*t
    half=.00025+.0024*math.sin(math.pi*t)**.6*max(0,1-q*q)**.55;xy=u*xx+d*yy+Vector(offset[:2]);verts.append(frame.p(xy.x*scale,xy.y*scale,(z+offset[2]+side*half)*scale));uv.append(((q+1)/2,.015+.97*t))
 ob=closed_grid('Orchid '+role,verts,uv,rows,cols,slot);ob['PetalKind']='Orchid';ob['PetalRole']=role;ob['FlowerIndex']=index;return ob

def lip(frame,index,scale):
 # Two raised lateral walls and a curled lower landing lobe form a real three-dimensional throat.
 for j,sign in enumerate([-1,1]):
  tepal(frame,sign*math.radians(38),.090,.055,(sign*.009,-.045,.026),'OrchidLip','Lip side lobe',index,scale,cup=.038,twist=sign*.020)
 rows,cols=23,11;verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=i/(rows-1);profile=.006+math.sin(math.pi*t)**.43
   for j in range(cols):
    q=j/(cols-1)*2-1;x=.071*profile*q;y=-.025-.115*t+.012*q*q*t*t;z=.036+.039*math.sin(math.pi*t)-.012*t+.021*q*q*math.sin(math.pi*t)+.032*t**5;half=.0004+.0024*math.sin(math.pi*t)*max(0,1-q*q)**.5
    verts.append(frame.p(x*scale,y*scale,(z+side*half)*scale));uv.append(((q+1)/2,t))
 ob=closed_grid('Orchid curled three-dimensional lower lip',verts,uv,rows,cols,'OrchidLip');ob['LipLandingLobe']=True;ob['FlowerIndex']=index
 for x in [-.017,.017]:
  C.sphere('Orchid golden orange throat',frame.p(x*scale,-.028*scale,.051*scale),(.015*scale,.018*scale,.022*scale),'ColumnAmber',2)['FlowerIndex']=index
 C.sphere('Orchid pearl column',frame.p(0,.003*scale,.058*scale),(.020*scale,.017*scale,.026*scale),'SeedCream',3)['FlowerIndex']=index
 # Sculpted split at the tip of the column; no painted-on stamen substitute.
 path_tube('Orchid column tip',[frame.p(0,-.010*scale,.058*scale),frame.p(0,-.021*scale,.044*scale)],.008*scale,'SeedCream',sides=8,taper=.35)['FlowerIndex']=index

def flower(center,normal,index,scale):
 frame=Frame(center,normal);before=set(C.current.objects);attach=frame.p(0,0,-.017*scale)
 neck=path_tube('Orchid connected flower neck',[attach,frame.p(0,0,.006*scale)],.009*scale,'StemGreen',sides=9,taper=.12);neck['OpenFlower']='Orchid';neck['JointId']='Flower_'+str(index);neck['JointRole']='head';neck['JointPoint']=list(attach)
 tepal(frame,0,.245,.120,(0,.003,-.014),'OrchidSepal','Dorsal sepal',index,scale,cup=.014)
 for sign in [-1,1]:
  tepal(frame,sign*math.radians(143),.185,.112,(sign*.006,-.006,-.018),'OrchidSepal','Lower sepal',index,scale,cup=.018,twist=sign*.006)
  tepal(frame,sign*math.radians(58),.242,.215,(sign*.012,.009,-.001),'OrchidPetal','Broad lateral petal',index,scale,cup=.028,twist=sign*.007)
 C.sphere('Orchid closed basal receptacle',frame.p(0,0,-.003*scale),(.024*scale,.025*scale,.017*scale),'OrchidSepal',2)
 lip(frame,index,scale)
 for ob in set(C.current.objects)-before:ob['FlowerKind']='Orchid';ob['FlowerIndex']=index
 return attach

def branch(start,end,normal,key,radius=.006):
 start,end,n=Vector(start),Vector(end),Vector(normal).normalized();points=sample([start,start.lerp(end,.4)+Vector((0,0,.015)),end-n*.025,end],17);points[-2]=end-n*.008
 ob=path_tube('Orchid continuous floral pedicel',points,radius,'StemGreen',sides=9,taper=.22);ob['JointId']=key;ob['JointRole']='stem';ob['JointPoint']=list(end);return ob

def bud(center,axis,index,scale=1):
 f=Frame(center,axis);rows,sides=17,16;verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);r=(.0007+.038*math.sin(math.pi*t)**.63)*(1-.10*t)*scale
  for j in range(sides+1):
   a=TAU*j/sides;ridge=1+.035*math.cos(a*3);verts.append(f.p(r*math.cos(a)*ridge,r*math.sin(a)*ridge,(-.018+.105*t)*scale));uv.append((j/sides,.08+.85*t))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((rows-1)*(sides+1)+j for j in range(sides))]);ob=C.mesh('Orchid tapered lavender closed bud',verts,faces,'OrchidSepal',uv);ob['ClosedBud']='Orchid';ob['BudIndex']=index
 # Small green neck hides the floral attachment within the bud base.
 attach=f.p(0,0,-.028*scale);neck=path_tube('Orchid bud neck',[attach,f.p(0,0,-.005*scale)],.009*scale,'StemGreen',sides=9,taper=-.15);neck['JointRole']='head';neck['JointId']='Bud_'+str(index);neck['JointPoint']=list(attach)
 return attach

def compact_spike(p,factor=.74):
 p=Vector(p);p.z=.10+(p.z-.10)*factor;return p

def ready():
 a=list(map(compact_spike,[(0,.03,.10),(-.03,.06,.50),(.01,.06,.89),(.18,.06,1.04)]));b=list(map(compact_spike,[(.18,.06,1.04),(.38,.06,1.11),(.66,.05,.99),(.75,.04,.72)]));points=sample(a,33)+sample(b,33)[1:];path_tube('Orchid unbroken arching spike',points,.012,'StemGreen',sides=10,taper=.36)['OrchidSpike']=True
 specs=[(bezier(a,.70),(-.12,-.03,.82),(-.38,-.92,.12),.78),(bezier(b,.15),(.25,-.035,1.105),(-.08,-.96,.25),1.),(bezier(b,.58),(.58,-.04,.99),(.32,-.93,.13),.80),(bezier(b,.83),(.76,-.04,.85),(.62,-.77,.13),.56)]
 for i,(start,center,normal,scale) in enumerate(specs):end=flower(compact_spike(center),normal,i,scale);branch(start,end,normal,'Flower_'+str(i))
 for i,(center,axis,scale,t) in enumerate([((.765,.025,.697),(.15,-.1,-1),.65,.97),((.855,.05,.705),(.6,-.12,-.8),.76,.99)]):
  end=bud(compact_spike(center),axis,i,scale);branch(bezier(b,t),end,axis,'Bud_'+str(i),.0045)

def bud_phase():
 pts=[compact_spike(p,.78) for p in [(0,.03,.1),(-.015,.04,.60),(.03,.05,.91),(.40,.06,1.01)]];path_tube('Orchid young curved spike',sample(pts,37),.010,'StemGreen',sides=10,taper=.42)['OrchidSpike']=True
 for i,(t,delta,axis,s) in enumerate([(.47,(-.048,-.02,.055),(-.18,-.05,1),.72),(.66,(-.045,-.025,.065),(-.1,-.03,1),.92),(.82,(.018,-.02,.07),(.75,-.10,.65),1.0),(.89,(.085,-.01,-.018),(.95,-.04,-.15),.75),(.98,(.070,.0,-.025),(.9,-.04,-.35),.85)]):
  start=bezier(pts,t);center=start+Vector(delta);end=bud(center,axis,i,s);branch(start,end,axis,'Bud_'+str(i),.0045)

def harvest():
 pts=[(-.43,.01,.027),(-.17,.05,.045),(.13,.06,.10),(.43,.03,.045)];path_tube('Orchid curved cut sprig',sample(pts,39),.009,'StemGreen',sides=10,taper=.35)['HarvestStem']=True
 for i,(t,center,normal,scale) in enumerate([(.38,(-.13,-.005,.105),(-.1,-.35,.93),.80),(.69,(.13,.10,.145),(.18,-.28,.94),.65)]):
  end=flower(center,normal,i,scale);branch(bezier(pts,t),end,normal,'Flower_'+str(i),.005)
 for i,(t,center,normal,scale) in enumerate([(.87,(.32,-.10,.09),(.85,-.35,.30),.72),(.98,(.46,.03,.06),(.9,.1,.28),.62)]):
  end=bud(center,normal,i,scale);branch(bezier(pts,t),end,normal,'Bud_'+str(i),.004)
 C.sphere('Orchid pale cut end',(-.43,.01,.027),(.009,.009,.004),'SeedCream',2)['HarvestCut']=True

def build(kind,stage):
 if stage=='Seed':
  rng=random.Random(1212)
  for i in range(11):
   a=i*2.4;r=.012+.047*math.sqrt((i+.5)/11);ob=C.sphere('Orchid illustrated ivory seed',(r*math.cos(a),r*math.sin(a)*.65,.008),(.020,.009,.006),'SeedCream',2);ob.rotation_euler=(0,rng.uniform(-.4,.4),a);ob['OrchidSeed']=True
 elif stage=='Harvest':harvest()
 else:
  roots(stage);foliage(stage)
  if stage=='BudForming':bud_phase()
  if stage=='Ready':ready()
