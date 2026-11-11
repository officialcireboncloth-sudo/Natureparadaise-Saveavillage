"""R02: closed fleshy petals and branching shrubs, authored from the large forms."""
import bpy,math,random
from mathutils import Vector,Matrix
import plant_blender_common as C
import g09_geometry as B
from g08_geometry import path_tube
TAU=math.tau
BACK={'Rose':.074,'Hibiscus':.095,'Jasmine':.034}
base_bud=B.bud
base_harvest=B.harvested

def smooth(t):
 t=max(0,min(1,t));return t*t*(3-2*t)

def fleshy_petal(f,kind,angle,length,width,r0,z0,height,curl,cup,twist,thickness,scale=1,phase=0):
 """A closed lens with a rounded outline, cupped faces and a rolled distal lip."""
 rows,cols=(27,9) if kind=='Hibiscus' else (25,7)
 verts=[];uv=[];faces=[]
 def centre(t):
  r=r0+length*(t+.065*math.sin(math.pi*t)-.075*smooth((t-.73)/.27))
  if kind=='Hibiscus':z=z0+height*(.25*t+.75*t*t)
  else:z=z0+height*(.15*t+.85*math.sin(math.pi*.5*t))-curl*t**6
  return r,z
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5
   profile=.001+math.sin(math.pi*t)**(.46 if kind=='Jasmine' else .50)*(.55+.45*t**.35)
   r,z=centre(t);ta=max(0,t-.0001);tb=min(1,t+.0001);ra,za=centre(ta);rb,zb=centre(tb);dr,dz=rb-ra,zb-za;norm=Vector((0,-dz,dr)).normalized()
   for j in range(cols):
    q=j/(cols-1)*2-1
    x=width*.5*profile*q+twist*length*t*t
    y=r-.035*length*q*q*math.sin(math.pi*t)
    zz=z+cup*q*q*math.sin(math.pi*t)
    zz+=.012*length*math.sin(q*math.pi*2.5+phase)*t**4
    zz+=.025*length*q*math.sin(math.pi*t+phase)*t
    half=.00012+thickness*(max(0,math.sin(math.pi*t))**.60)*(max(0,1-q*q)**.65)
    local=Vector((x,y,zz))+norm*(side*half)
    a=angle+twist*.18*t
    verts.append(f.p((local.x*math.cos(a)-local.y*math.sin(a))*scale,(local.x*math.sin(a)+local.y*math.cos(a))*scale,local.z*scale));uv.append(((q+1)/2,t))
 n=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.append(face);faces.append(tuple(v+n for v in reversed(face)))
 boundary=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,a in enumerate(boundary):
  b=boundary[(i+1)%len(boundary)];faces.append((a+n,b+n,b,a))
 o=C.mesh(kind+' R02 rounded closed cupped petal',verts,faces,kind+'Petal',uv);o['PetalKind']=kind;o['ClosedPetal']=True;o['PetalHalfThickness']=thickness*scale;o['RoundedOutline']=True;return o

def calyx(kind,f,index,scale=1,bud=False):
 back=0 if bud else BACK[kind]*scale;attach=f.p(0,0,-back)
 ob=path_tube(kind+' R02 calyx collar',[attach,attach+f.n*.010*scale],.012*scale,'SepalGreen',sides=8,taper=.08)
 ob['JointId']=('Bud' if bud else 'Flower')+'_'+str(index);ob['JointRole']='head';ob['PlantKind']=kind
 sphere=C.sphere(kind+' R02 calyx base',attach-f.n*.008*scale,(.018*scale,.018*scale,.011*scale),'SepalGreen',2);sphere.rotation_euler=Vector((0,0,1)).rotation_difference(f.n).to_euler()
 for j in range(5):
  a=TAU*j/5;p=f.p(.008*math.cos(a)*scale,.008*math.sin(a)*scale,-back+.004*scale);d=(f.x*math.cos(a)+f.y*math.sin(a))*(.42 if bud else .90)+f.n*(.90 if bud else -.42)
  B.blade('Sepal',p,d,(.049 if kind!='Jasmine' else .026)*scale,.010*scale,'SepalGreen')
 return attach

def rose_heart(f,scale):
 rows,cols=65,5;verts=[];uv=[];faces=[]
 for side in [1,-1]:
  for i in range(rows):
   t=i/(rows-1);a=.5+t*TAU*1.8;r=.003+.021*t
   for j in range(cols):
    q=j/(cols-1);rad=r+side*(.00045+.0019*math.sin(math.pi*q)**.4)
    verts.append(f.p(rad*math.cos(a)*scale,rad*math.sin(a)*scale,(.074-.022*t+(q-.5)*.025)*scale));uv.append((q,t))
 n=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.append(face);faces.append(tuple(v+n for v in reversed(face)))
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,a in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((a,a+n,b+n,b))
 o=C.mesh('Rose R02 thick rolled inner spiral',verts,faces,'RosePetal',uv);o['RoseSpiral']=True

def open_flower(kind,center,normal,index,scale=1):
 f=B.Frame(center,normal);before=set(C.current.objects);attachment=calyx(kind,f,index,scale);rng=random.Random(9200+index*29+['Rose','Hibiscus','Jasmine'].index(kind)*100)
 if kind=='Rose':
  core=C.sphere('Rose R02 recessed red receptacle',f.p(0,0,-.045*scale),(.032*scale,.032*scale,.016*scale),'RosePetal',2);core.rotation_euler=Vector((0,0,1)).rotation_difference(f.n).to_euler()
  # Outer petals turn outward; successively steeper inner cups form a full rose heart.
  rings=[(7,.124,.142,.025,-.054,.086,.016,.003,.045,.0034),
         (7,.094,.119,.018,-.057,.112,.012,.004,.065,.0038),
         (6,.064,.093,.010,-.059,.138,.007,.005,.085,.0041),
         (6,.037,.060,.005,-.043,.113,.003,.004,.10,.0033)]
  for layer,(count,l,w,r,z,h,curl,cup,twist,thick) in enumerate(rings):
   for j in range(count):
    a=TAU*j/count+layer*.49+index*.13+rng.uniform(-.055,.055)
    fleshy_petal(f,kind,a,l*rng.uniform(.95,1.05),w*rng.uniform(.94,1.06),r,z,h*rng.uniform(.95,1.04),curl,cup,twist,thick,scale,phase=j*.71+layer)
  rose_heart(f,scale)
 elif kind=='Hibiscus':
  for j in range(5):
   fleshy_petal(f,kind,TAU*j/5+.20+[0,-.04,.045,-.025,.025][j],.182*[1,.97,1.02,.98,1][j],.163*[1,1.02,.97,1.04,.99][j],.003,-.078,.119,0,.010,.025,.0018,scale,phase=j*1.2+index)
  # A recessed bowl has upward-facing inside normals and no exposed green support.
  rings=[(.001,-.081),(.008,-.078),(.019,-.065),(.029,-.052)];verts=[];uv=[];faces=[];sides=24
  for r,z in rings:
   for j in range(sides+1):a=TAU*j/sides;verts.append(f.p(r*math.cos(a)*scale,r*math.sin(a)*scale,z*scale));uv.append((.5+.1*math.cos(a),.06+.10*r/.029))
  for row in range(3):
   for j in range(sides):k=row*(sides+1)+j;faces.append((k,k+sides+1,k+sides+2,k+1))
  faces.append(tuple(range(sides)));C.mesh('Hibiscus R02 deep red throat',verts,faces,'HibiscusPetal',uv)
  style=path_tube('Hibiscus R02 prominent curved style',[f.p(0,0,-.077*scale),f.p(.004*scale,.058*scale,.07*scale),f.p(.008*scale,.125*scale,.19*scale)],.0041*scale,'StigmaRed',sides=9,taper=.28);style['HibiscusStyle']=True
  for j in range(36):
   a=j*2.399;t=j/35;z=.085+.095*t;y=.058+(.125-.058)*(z-.07)/.12;p=f.p((.006+.010*math.cos(a))*scale,(y+.010*math.sin(a))*scale,z*scale)
   C.sphere('Hibiscus R02 gold anther',p,(.0043*scale,.0043*scale,.0048*scale),'PollenGold',1)['HibiscusAnther']=True
  for j in range(5):
   a=TAU*j/5;p=f.p((.008+.020*math.cos(a))*scale,(.136+.020*math.sin(a))*scale,.212*scale)
   path_tube('Hibiscus R02 stigma fork',[f.p(.008*scale,.125*scale,.188*scale),p],.0018*scale,'StigmaRed',sides=6,taper=.2)
   C.sphere('Hibiscus R02 stigma pad',p,(.007*scale,.007*scale,.006*scale),'StigmaRed',2)['HibiscusStigma']=True
 else:
  for layer,(count,l,w,r,z,h,cup,thick) in enumerate([(7,.071,.062,.003,-.014,.033,.0015,.0034),(6,.050,.047,.002,-.007,.036,.002,.0032),(5,.030,.032,.001,.003,.030,.0015,.0027)]):
   for j in range(count):
    fleshy_petal(f,kind,TAU*j/count+layer*.45+index*.22+rng.uniform(-.035,.035),l*rng.uniform(.96,1.04),w*rng.uniform(.96,1.04),r,z,h,.002,cup,.015,thick,scale,phase=j*.8)
  heart=C.sphere('Jasmine R02 small warm heart',f.p(0,0,.027*scale),(.007*scale,.007*scale,.004*scale),'BudCream',2);heart.rotation_euler=Vector((0,0,1)).rotation_difference(f.n).to_euler()
  for j in range(3):a=TAU*j/3;C.sphere('Jasmine R02 delicate centre',f.p(.004*math.cos(a)*scale,.004*math.sin(a)*scale,.032*scale),(.0024*scale,.0024*scale,.0028*scale),'BudCream',1)
 for o in set(C.current.objects)-before:o['FlowerKind']=kind;o['FlowerIndex']=index
 marker=next(o for o in set(C.current.objects)-before if o.get('JointRole')=='head');marker['OpenFlower']=kind;marker['CorollaCenter']=list(center);marker['CorollaNormal']=list(f.n);marker['CorollaRadius']={'Rose':.155,'Hibiscus':.180,'Jasmine':.081}[kind]*scale
 return attachment

def cane(kind,start,end,radius=.009):
 start,end=Vector(start),Vector(end);offset=end-start;points=[]
 for i in range(13):
  t=i/12;points.append(start+offset*t+Vector((-.025*math.sin(t*math.pi),.018*math.sin(t*math.pi*1.2),.025*math.sin(t*math.pi))))
 ob=path_tube(kind+' R02 curved woody side cane',points,radius,'StemGreen',sides=8,taper=.50);ob['ShrubBranch']=kind;return points

def compound(p,a,e,length=.15,rng=None):
 rng=rng or random.Random(9201+int(a*11));p=Vector(p);d=B.direction(a,e);tip=p+d*.055
 path_tube('Rose R02 compound rachis',[p,p+d*.03+Vector((0,0,.006)),tip],.0027,'StemGreen',sides=7,taper=.30)
 B.leaf_at('Rose',tip,a+rng.uniform(-8,8),e+rng.uniform(0,8),length*rng.uniform(.96,1.05),.090,petiole=.008,roll=rng.uniform(-.22,.22))
 for sign in [-1,1]:B.leaf_at('Rose',p+d*.026,a+sign*rng.uniform(47,61),e-rng.uniform(3,11),length*rng.uniform(.76,.87),.070,petiole=.010,roll=sign*rng.uniform(.10,.25))

def leaves_on_cane(kind,points,angle,rng,pairs=3,length=.18,width=.12):
 for k in range(pairs):
  idx=[3,6,9,11][k];p=points[idx]
  if kind=='Rose':compound(p,angle+(-1 if k%2 else 1)*rng.uniform(30,62),rng.uniform(15,34),length,rng)
  else:
   for sign in [-1,1]:B.leaf_at(kind,p,angle+sign*rng.uniform(62,87),rng.uniform(15,39),length*rng.uniform(.85,1.10)*(1-.06*k),width*rng.uniform(.92,1.07),petiole=.024,roll=rng.uniform(-.25,.25))

def leafy_base(kind,stage,rng):
 young=stage=='Young'
 if young:
  height={'Rose':.38,'Hibiscus':.35,'Jasmine':.32}[kind];B.trunk(kind,height)
  if kind=='Rose':
   for p,a,e in [((0,0,.075),195,22),((0,0,.16),25,26),((0,0,.25),145,30)]:compound(p,a,e,.137,rng)
   for a in [70,250]:B.leaf_at(kind,(0,0,.34),a,58,.095,.059,'RoseLeaf',petiole=.009)
  else:
   count=8 if kind=='Hibiscus' else 7
   for i in range(count):
    a=190+i*137.5;p=(.012*math.cos(math.radians(a)),.012*math.sin(math.radians(a)),.045+i*.035)
    B.leaf_at(kind,p,a,23+rng.uniform(0,24),.163 if kind=='Hibiscus' else .145,.111 if kind=='Hibiscus' else .091,petiole=.015,roll=rng.uniform(-.2,.2))
  return
 B.trunk(kind,{'Rose':.45,'Hibiscus':.52,'Jasmine':.40}[kind])
 if kind=='Rose':
  for i,a in enumerate([205,335,95]):
   rad=.11+.014*i;points=cane(kind,(0,0,.025),(.11*math.cos(math.radians(a)),rad*math.sin(math.radians(a)),.27+i*.045),.0085);leaves_on_cane(kind,points,a,rng,2,.151,.09)
  compound((0,0,.06),190,20,.15,rng);compound((0,0,.14),15,26,.151,rng)
  for end,a in [((-.15,.10,.64),165),((.16,.12,.62),25)]:
   points=cane(kind,(0,.01,.04),end,.0075);leaves_on_cane(kind,points,a,rng,3,.146,.09)
 elif kind=='Hibiscus':
  for i,a in enumerate([215,300,15,85,155]):
   rad=.12+.015*(i%2);points=cane(kind,(0,0,.025), (rad*math.cos(math.radians(a)),rad*math.sin(math.radians(a)),[.30,.39,.51,.56,.42][i]),.0085);leaves_on_cane(kind,points,a,rng,2,.195,.131)
  for i,a in enumerate([190,20,125,285]):B.leaf_at(kind,(0,0,.045+.07*i),a,24,.19,.132,petiole=.025,roll=rng.uniform(-.15,.15))
  for end,a in [((-.15,.12,.66),160),((.15,.14,.69),25)]:
   points=cane(kind,(0,.01,.04),end,.0075);leaves_on_cane(kind,points,a,rng,3,.172,.116)
 else:
  for i,a in enumerate([210,290,20,90,150]):
   rad=[.12,.15,.13,.12,.16][i];points=cane(kind,(0,0,.018),(rad*math.cos(math.radians(a)),rad*math.sin(math.radians(a)),[.30,.39,.45,.54,.42][i]),.008);leaves_on_cane(kind,points,a,rng,4,.162,.098)
  for i,(z,a) in enumerate([(.045,190),(.14,235),(.26,295)]):
   for sign in [-1,1]:B.leaf_at(kind,(0,0,z),a+(0 if sign==1 else 180),24+rng.uniform(-7,7),.16,.097,petiole=.021,roll=rng.uniform(-.22,.22))

def floral_shrub(kind,stage,rng):
 ready=stage=='Ready';joint_specs=[]
 if kind=='Rose':
  flower_specs=[((-.22,-.12,.55),(-.15,-.88,.45),.93),((.015,.015,.74),(.03,-.70,.71),1.03),((.24,-.10,.51),(.22,-.90,.38),.95)]
  bud_specs=[((.29,.04,.69),(.30,-.05,1),.95)] if ready else [((-.22,-.045,.55),(-.2,-.08,1),.95),((.0,.03,.71),(0,-.06,1),1.05),((.24,.0,.55),(.3,-.04,1),.98)]
 elif kind=='Hibiscus':
  flower_specs=[((-.23,-.15,.45),(-.10,-.94,.34),1),((.13,.015,.70),(.1,-.78,.62),1.02),((.25,-.17,.31),(.3,-.88,.36),.88)]
  bud_specs=[((-.23,.02,.65),(-.3,0,1),.86),((-.025,.055,.79),(0,-.05,1),.88),((.34,.02,.52),(.45,-.1,.9),.84)] if ready else [((-.21,-.01,.52),(-.2,-.1,1),.93),((.015,.025,.66),(0,0,1),1),((.23,.015,.52),(.25,-.1,1),.94),((.01,-.11,.35),(0,-.45,1),.87)]
 else:
  flower_specs=[((-.14,.005,.62),(-.10,-.76,.64),1),((.11,-.08,.56),(.12,-.89,.44),1.01),((-.23,-.14,.35),(-.25,-.88,.40),.98),((.0,-.22,.25),(.05,-.93,.34),1),((.25,-.07,.37),(.36,-.82,.45),1.0)]
  bud_specs=[]
  for i in range(10 if ready else 14):
   a=.36+i*2.399;r=.14+.022*(i%3);bud_specs.append(((r*math.cos(a),r*math.sin(a),.34+.062*(i%5)),(.26*math.cos(a),.26*math.sin(a),1),.83+.08*(i%2)))
 if ready:
  for i,(p,n,s) in enumerate(flower_specs):
   attach=open_flower(kind,p,n,i,s);start=Vector((0,.01,.15+.065*(i%3)));stem=B.branch_to(kind,start,attach,n,'Flower_'+str(i),.0085 if kind=='Rose' else .007)
   stem['ShrubBranch']=kind
   if kind in ['Rose','Hibiscus']:
    points=[sum((v.co for v in stem.data.vertices[k*9:k*9+8]),Vector())/8 for k in range(13)]
    leaves_on_cane(kind,points,200 if p[0]<0 else 15,rng,3 if kind=='Rose' else 2,.153 if kind=='Rose' else .18,.12)
 for i,(p,n,s) in enumerate(bud_specs):
  attach=base_bud(kind,p,n,i,s);stem=B.branch_to(kind,(0,.01,max(.10,p[2]*.45)),attach,n,'Bud_'+str(i),.0055);stem['ShrubBranch']=kind
  if not ready and kind in ['Rose','Hibiscus']:
   points=[sum((v.co for v in stem.data.vertices[k*9:k*9+8]),Vector())/8 for k in range(13)]
   leaves_on_cane(kind,points,195 if p[0]<0 else 10,rng,3 if kind=='Rose' else 2,.15 if kind=='Rose' else .18,.12)
  elif ready and kind in ['Rose','Hibiscus']:
   points=[sum((v.co for v in stem.data.vertices[k*9:k*9+8]),Vector())/8 for k in range(13)]
   leaves_on_cane(kind,points,190 if p[0]<0 else 15,rng,3,.148 if kind=='Rose' else .172,.115)

# Reuse the established seed, bud, foliage and cut-stem helpers with the new corollas.
B.open_flower=open_flower;B.throat_and_sepals=calyx;B.compound=compound

def cap_leaf_canes(kind):
 """End vegetative canes with a live leaf pair directed behind the flower crown."""
 for cane_ob in list(C.current.objects):
  if cane_ob.get('ShrubBranch')!=kind or 'curved woody side cane' not in cane_ob.name:continue
  tip=sum((v.co for v in cane_ob.data.vertices[-9:-1]),Vector())/8
  for j,a in enumerate([55,125]):
   length={'Rose':.105,'Hibiscus':.123,'Jasmine':.104}[kind]
   leaf=B.leaf_at(kind,tip,a,36+j*8,length,length*.65,petiole=.010,roll=(j-.5)*.23)
   leaf['TerminalLeaf']=True

def build(kind,stage):
 if stage=='Seed':B.seeds(kind);return
 if stage=='Sprout':B.sprout(kind);return
 if stage=='Harvest':base_harvest(kind);return
 rng=random.Random(9300+['Rose','Hibiscus','Jasmine'].index(kind)*100+['Young','BudForming','Ready'].index(stage)*11)
 leafy_base(kind,stage,rng)
 if stage!='Young':floral_shrub(kind,stage,rng)
 if stage=='Ready':B.clear_blooms(kind)
 if stage in ['BudForming','Ready']:cap_leaf_canes(kind)
