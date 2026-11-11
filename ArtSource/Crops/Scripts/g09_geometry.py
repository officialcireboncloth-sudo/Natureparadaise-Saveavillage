"""Sculpted roses, hibiscus funnels and double jasmine on radial leafy shrubs."""
import math,random
from mathutils import Vector,Matrix
import plant_blender_common as C
from g08_geometry import path_tube,grain
TAU=math.tau

class Frame:
 def __init__(self,center,normal):
  self.c=Vector(center);self.n=Vector(normal).normalized();self.x=Vector((1,0,0))-self.n*self.n.x
  if self.x.length<.01:self.x=Vector((0,1,0))-self.n*self.n.y
  self.x.normalize();self.y=self.n.cross(self.x).normalized()
 def p(self,x,y,z):return self.c+self.x*x+self.y*y+self.n*z

def direction(angle,elevation):
 a,e=map(math.radians,[angle,elevation]);return Vector((math.cos(a)*math.cos(e),math.sin(a)*math.cos(e),math.sin(e)))

def blade(kind,origin,d,length,width,slot=None,roll=0):
 origin=Vector(origin);d=Vector(d).normalized();n=Vector((d.x*.25,-.70+d.y*.18,.73));n-=d*n.dot(d)
 if n.length<.01:n=Vector((1,0,0))-d*d.x
 n.normalize();u=d.cross(n).normalized();u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
 serrated=kind in ['Rose','Hibiscus'] and C.parent.get('Phase')!='Sprout';rows,cols=(29,5) if serrated else (21,7);verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);profile=max(.008,math.sin(math.pi*t)**(.62 if kind=='Hibiscus' else .68))*(1-.12*t)
  if serrated:
   tooth=abs((t*7%1)*2-1);profile*=.90+.10*tooth
  for j in range(cols):
   q=j/(cols-1)*2-1;w=width*.5*profile
   bow=.14*math.sin(math.pi*t)-.12*t*t+.040*q*q*math.sin(math.pi*t)
   bow-=.018*math.exp(-q*q/.055)*math.sin(math.pi*t)
   bow+=.010*(math.sin(q*math.pi)**2)*(math.sin(t*math.pi*7)**2)*math.sin(math.pi*t)
   verts.append(origin+d*(length*t)+u*(w*q)+n*(length*bow));uv.append(((q+1)/2,t))
 for i in range(rows-1):
  for j in range(cols-1):k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 o=C.mesh(kind+' cupped '+('serrated' if serrated else 'oval')+' leaf',verts,faces,slot or kind+'Leaf',uv);solid=o.modifiers.new('Living leaf edge','SOLIDIFY');solid.thickness=.0017;solid.offset=0;o['ReferenceLeaf']=kind;o['SerratedLeaf']=serrated;return o

def leaf_at(kind,p,angle,elevation,length,width,slot=None,petiole=.028,roll=0):
 p=Vector(p);d=direction(angle,elevation);start=p+d*petiole
 stem=path_tube(kind+' leaf petiole',[p,p+d*(petiole*.5)+Vector((0,0,.003)),start],.0028,'StemGreen',sides=7,taper=.25)
 leaf=blade(kind,start,d,length,width,slot,roll);leaf['PetioleName']=stem.name;return leaf

def petal(frame,angle,length,width,base_z,rise,edge,cup,twist,slot,kind,scale=1):
 rows,cols=17,7;verts=[];uv=[];faces=[];a=angle
 for i in range(rows):
  t=i/(rows-1);profile=.009+.991*math.sin(math.pi*t)**(.34 if kind=='Hibiscus' else .40)
  for j in range(cols):
   q=j/(cols-1)*2-1;x=width*.5*profile*q+twist*length*t*t;y=.005+length*t
   if kind in ['Rose','Hibiscus']:
    fan=.012+.988*math.sin(math.pi*t*.5)**.68
    if kind=='Hibiscus':fan*=1-.10*t**4
    x=width*.5*fan*q+twist*(.55 if kind=='Rose' else .16)*length*t*t
    y=.005+length*(t-(.29 if kind=='Rose' else .18)*q*q*t**4)
   z=base_z+rise*math.sin(math.pi*t*.75)+edge*t**3+cup*q*q*math.sin(math.pi*t)
   if kind=='Rose':z+=.014*math.sin(q*math.pi)*t*t-.018*q*q*t**4
   z+=length*.020*math.sin(q*math.pi*3+a)*t**3
   verts.append(frame.p((x*math.cos(a)-y*math.sin(a))*scale,(x*math.sin(a)+y*math.cos(a))*scale,z*scale));uv.append(((q+1)/2,t))
 for i in range(rows-1):
  for j in range(cols-1):k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
 o=C.mesh(kind+' sculpted overlapping petal',verts,faces,slot,uv);solid=o.modifiers.new('Soft petal edge','SOLIDIFY');solid.thickness=.0013*scale;solid.offset=0;o['PetalKind']=kind;return o

def throat_and_sepals(kind,frame,index,scale=1,bud=False):
 back={'Rose':.043,'Hibiscus':.090,'Jasmine':.030}[kind]*scale
 if bud:back=0
 attachment=frame.p(0,0,-back)
 o=path_tube(kind+' calyx throat',[attachment,attachment+frame.n*.010*scale],.012*scale,'SepalGreen',sides=8,taper=.08);o['JointId']=('Bud' if bud else 'Flower')+'_'+str(index);o['JointRole']='head';o['PlantKind']=kind
 C.sphere(kind+' swollen green calyx',attachment-frame.n*.007*scale,(.020*scale,.020*scale,.012*scale),'SepalGreen',2)
 for j in range(5):
  a=TAU*j/5;start=frame.p(.010*math.cos(a)*scale,.010*math.sin(a)*scale,-back+.010*scale)
  d=(frame.x*math.cos(a)+frame.y*math.sin(a))*(.55 if bud else .90)+frame.n*(.85 if bud else -.35)
  blade('Sepal',start,d,.048*scale if kind!='Jasmine' else .027*scale,.011*scale,'SepalGreen')
 return attachment

def branch_to(kind,start,attachment,normal,joint_id,radius=.007):
 start=Vector(start);attachment=Vector(attachment);n=Vector(normal).normalized()
 a,b,c,d=start,start.lerp(attachment,.44)+Vector((0,0,.025)),attachment-n*.035,attachment
 points=[a*(1-t)**3+b*(3*(1-t)**2*t)+c*(3*(1-t)*t*t)+d*t**3 for t in [i/12 for i in range(13)]];points[-2]=attachment-n*.010
 o=path_tube(kind+' continuous floral branch',points,radius,'StemGreen',sides=8,taper=.34);o['JointId']=joint_id;o['JointRole']='stem';o['PlantKind']=kind;return o

def rose_core(f,scale):
 verts=[];uv=[];faces=[];rows=49
 for i in range(rows):
  t=i/(rows-1);a=t*TAU*1.55;r=.003+.022*t
  for j in range(3):
   q=j/2;verts.append(f.p(r*math.cos(a)*scale,r*math.sin(a)*scale,(.058-.010*t+(q-.5)*.020)*scale));uv.append((q,t))
 for i in range(rows-1):
  for j in range(2):k=i*3+j;faces.append((k,k+1,k+4,k+3))
 o=C.mesh('Rose rolled spiral heart',verts,faces,'RosePetal',uv);solid=o.modifiers.new('Rolled petal thickness','SOLIDIFY');solid.thickness=.0011*scale;solid.offset=0;o['RoseSpiral']=True

def open_flower(kind,center,normal,index,scale=1):
 f=Frame(center,normal);before=set(C.current.objects);attachment=throat_and_sepals(kind,f,index,scale)
 if kind=='Rose':
  rings=[(8,.155,.135,-.024,.052,-.024,.35),(7,.125,.115,-.010,.060,-.003,.50),(6,.088,.085,.003,.060,.004,.65),(5,.052,.055,.014,.047,.010,.75)]
  for r,(count,l,w,z,rise,edge,twist) in enumerate(rings):
   for j in range(count):
    variation=math.sin(j*2.39+r*.81);petal(f,TAU*j/count+r*.47+.035*variation,l*(1+.055*variation),w*(1+.07*math.cos(j*1.7+r)),z,rise*1.24,edge+.006*variation,.021,twist,'RosePetal',kind,scale)
  rose_core(f,scale)
 elif kind=='Hibiscus':
  for j in range(5):petal(f,TAU*j/5+.12,.172,.230,-.075,.040,.078,.020,.035,'HibiscusPetal',kind,scale)
  verts=[];uv=[];faces=[];rings=[(.001,-.078),(.008,-.075),(.018,-.067),(.026,-.056)];sides=20
  for r,z in rings:
   for j in range(sides+1):
    a=TAU*j/sides;verts.append(f.p(r*math.cos(a)*scale,r*math.sin(a)*scale,z*scale));uv.append((.5+.10*math.cos(a),.06+.10*r/.026))
  for row in range(3):
   for j in range(sides):k=row*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
  faces.append(tuple(range(sides-1,-1,-1)));C.mesh('Hibiscus recessed red funnel throat',verts,faces,'HibiscusPetal',uv)
  style=path_tube('Hibiscus long red staminal column',[f.p(0,0,-.075*scale),f.p(.004*scale,.12*scale,.18*scale)],.0042*scale,'StigmaRed',sides=9,taper=.35);style['HibiscusStyle']=True
  for j in range(36):
   a=j*2.399;z=(.085+.080*j/35)*scale;r=.009*scale
   y=.12*scale*((z/scale+.075)/.255);p=f.p(r*math.cos(a),y+r*math.sin(a),z);C.sphere('Hibiscus golden anther',p,(.0038*scale,.0038*scale,.0045*scale),'PollenGold',1)['HibiscusAnther']=True
  for j in range(5):
   a=TAU*j/5;tip=f.p(.018*math.cos(a)*scale,(.13+.018*math.sin(a))*scale,.198*scale)
   path_tube('Hibiscus stigma fork',[f.p(.004*scale,.12*scale,.18*scale),tip],.0018*scale,'StigmaRed',sides=6,taper=.15)
   C.sphere('Hibiscus red stigma pad',tip,(.007*scale,.007*scale,.006*scale),'StigmaRed',2)['HibiscusStigma']=True
 else:
  for r,(count,l,w,z,rise) in enumerate([(7,.078,.060,-.011,.026),(6,.055,.047,.002,.030),(5,.035,.034,.009,.027)]):
   for j in range(count):petal(f,TAU*j/count+r*.43,l,w,z,rise,-.006,.009,.02,'JasminePetal',kind,scale)
  heart=C.sphere('Jasmine cream flower heart',f.p(0,0,.025*scale),(.012*scale,.012*scale,.007*scale),'BudCream',2);heart.rotation_euler=Vector((0,0,1)).rotation_difference(f.n).to_euler()
  for j in range(5):
   a=TAU*j/5;C.sphere('Jasmine warm ivory centre',f.p(.008*math.cos(a)*scale,.008*math.sin(a)*scale,.030*scale),(.004*scale,.004*scale,.004*scale),'BudCream',1)
 for o in set(C.current.objects)-before:o['FlowerKind']=kind;o['FlowerIndex']=index
 # One marker per open corolla; fine parts retain FlowerKind for isolated detail renders.
 marker=next(o for o in set(C.current.objects)-before if o.get('JointRole')=='head');marker['OpenFlower']=kind;marker['CorollaCenter']=list(center);marker['CorollaNormal']=list(Vector(normal).normalized());marker['CorollaRadius']=({'Rose':.16,'Hibiscus':.18,'Jasmine':.08}[kind])*scale
 return attachment

def bud(kind,base,normal,index,scale=1):
 f=Frame(base,normal);before=set(C.current.objects);attachment=throat_and_sepals(kind,f,index,scale,bud=True)
 length=(.110 if kind=='Rose' else .100 if kind=='Hibiscus' else .070)*scale;radius=(.030 if kind=='Rose' else .028 if kind=='Hibiscus' else .020)*scale
 rows,sides=17,18;verts=[];uv=[];faces=[]
 for i in range(rows):
  t=i/(rows-1);r=radius*(.13+.87*math.sin(math.pi*t)**.65)*(1-.25*t)
  if i==rows-1:r=.0006*scale
  for j in range(sides+1):
   a=TAU*j/sides;crease=1-.07*math.cos(a*5-t*1.3)**10;verts.append(f.p(r*math.cos(a)*crease,r*math.sin(a)*crease,length*t));uv.append((j/sides,t))
 for i in range(rows-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((rows-1)*(sides+1)+j for j in range(sides))])
 o=C.mesh(kind+' pointed folded bud',verts,faces,'BudCream' if kind=='Jasmine' else 'BudRed',uv);o['ClosedBud']=kind;o['BudIndex']=index
 if kind=='Rose':
  for j in range(5):
   a=TAU*j/5;start=f.p(.015*math.cos(a)*scale,.015*math.sin(a)*scale,.003*scale);d=f.n*.94+(f.x*math.cos(a)+f.y*math.sin(a))*.34
   blade('Sepal',start,d,.075*scale,.013*scale,'SepalGreen')
 return attachment

def thorn(p,d):
 p=Vector(p);d=Vector(d).normalized();o=path_tube('Rose curved bronze thorn',[p,p+d*.017,p+d*.032+Vector((0,0,.007))],.0085,'ThornBronze',sides=7,taper=.98);o['RoseThorn']=True

def trunk(kind,h):
 path_tube(kind+' main living stem',[(0,0,.006),(.006,0,h*.35),(-.006,.012,h*.70),(0,.02,h)],.017 if kind!='Jasmine' else .019,'StemGreen',sides=10,taper=.63)
 if kind=='Rose':
  for i in range(10):
   a=i*2.399;p=(.012*math.cos(a),.012*math.sin(a),.035+i*h*.075);thorn(p,(math.cos(a),math.sin(a),.08))

def compound(p,a,e,length=.14):
 p=Vector(p);d=direction(a,e);tip=p+d*.062;path_tube('Rose compound leaf rachis',[p,tip],.0028,'StemGreen',sides=7,taper=.35)
 leaf_at('Rose',tip,a,e+4,length,.084,petiole=.008)
 for sign in [-1,1]:leaf_at('Rose',p+d*.033,a+sign*52,e-6,length*.81,.068,petiole=.010,roll=sign*.12)

def foliage(kind,stage):
 young=stage=='Young';ready=stage=='Ready'
 if kind=='Rose':
  h=.44 if young else .77 if not ready else .87;trunk(kind,h)
  for i in range(3 if young else 12 if ready else 10):
   a=190+i*137.5;z=(.09+i*.092) if young else (.07+i*.065);compound((.010*math.cos(math.radians(a)),.010*math.sin(math.radians(a)),z),a,22+(i%3)*9,.14 if young else .18)
  if young:
   for j in range(2):leaf_at('Rose',(0,.01,h-.025),95+j*80,65,.12,.068,'ThornBronze',petiole=.013)
  return h
 h=(.46 if young else .73 if kind=='Hibiscus' else .53)
 if ready:h=.83 if kind=='Hibiscus' else .61
 trunk(kind,h)
 count=(8 if kind=='Hibiscus' else 7) if young else ((36 if ready else 28) if kind=='Hibiscus' else (42 if ready else 32))
 for i in range(count):
  a=195+i*137.5;frac=i/max(1,count-1);tier=i%6;z=.035+(h*.78)*(i//(5 if kind=='Hibiscus' else 7))/max(1,(count-1)//(5 if kind=='Hibiscus' else 7))
  if young:z=.05+i*h*.095
  rad=(.020 if young else .05+.025*math.sin(frac*math.pi));p=Vector((rad*math.cos(math.radians(a)),rad*math.sin(math.radians(a)),z))
  path_tube(kind+' short leafy side branch',[(0,.006,max(.013,z-.025)),p],.004,'StemGreen',sides=7,taper=.35)
  length=(.22 if kind=='Hibiscus' else .205)*(1-.19*frac);width=(.145 if kind=='Hibiscus' else .125)*(1-.15*frac)
  leaf_at(kind,p,a,18+25*frac+8*(i%2),length,width,petiole=.035 if not young else .018,roll=.18*math.sin(i))
 return h

def flowers_on_shrub(kind,stage,h):
 ready=stage=='Ready'
 if kind=='Rose':
  specs=[((-.17,-.08,.72),(-.12,-.95,.30),.96),((.04,.015,1.02),(.02,-.78,.63),1.08),((.23,-.10,.62),(.15,-.96,.25),.98)]
  if ready:
   for i,(p,n,s) in enumerate(specs):
    attach=open_flower(kind,p,n,i,s);branch_to(kind,(0,.01,.34+i*.07),attach,n,'Flower_'+str(i),.009)
    for j in range(2):compound(Vector((0,.01,.34+i*.07)).lerp(attach,.44+j*.24)+Vector((0,.025,0)),(185 if p[0]<0 else 5)+j*65,23,.15)
   bspec=[((.27,.035,.94),(.3,-.2,1),1)]
  else:bspec=[((-.18,-.025,.73),(-.25,-.20,1),1),((.01,.015,.89),(0,-.05,1),1.1),((.23,.005,.69),(.30,-.15,1),1)]
 elif kind=='Hibiscus':
  if ready:
   for i,(p,n,s) in enumerate([((-.17,-.16,.55),(-.07,-.97,.24),1.0),((.17,.005,.91),(.10,-.85,.52),1.05),((.24,-.14,.36),(.35,-.90,.26),.87)]):
    attach=open_flower(kind,p,n,i,s);branch_to(kind,(0,.01,.27+i*.12),attach,n,'Flower_'+str(i),.008)
   bspec=[((-.21,.025,.83),(-.3,-.1,1),.90),((-.03,.055,1.00),(-.08,-.1,1),.82),((.34,.04,.64),(.55,-.1,.8),.83)]
  else:bspec=[((-.20,-.005,.65),(-.35,-.1,1),.9),((.015,.03,.82),(0,-.1,1),1),((.23,.015,.64),(.4,-.1,1),.92),((.02,-.14,.45),(0,-.55,.9),.88)]
 else:
  if ready:
   specs=[((-.13,-.07,.70),(-.10,-.85,.55),.94),((.10,-.10,.61),(.12,-.95,.35),1),((-.22,-.14,.39),(-.30,-.90,.35),.98),((.02,-.23,.27),(.05,-.97,.25),1.0),((.27,-.06,.42),(.4,-.85,.4),.97)]
   for i,(p,n,s) in enumerate(specs):
    attach=open_flower(kind,p,n,i,s);branch_to(kind,(0,.015,.18+i*.055),attach,n,'Flower_'+str(i),.006)
  count=10 if ready else 14;bspec=[]
  for i in range(count):
   a=i*2.399+.4;r=.16+.035*(i%3);p=(r*math.cos(a),r*math.sin(a),.33+.075*(i%5));n=(.3*math.cos(a),.3*math.sin(a),1);bspec.append((p,n,.84+.10*(i%2)))
 for i,(p,n,s) in enumerate(bspec):
  attach=bud(kind,p,n,i,s);branch_to(kind,(0,.015,max(.06,p[2]*.66)),attach,n,'Bud_'+str(i),.005)
  if kind=='Rose':compound(Vector((0,.015,max(.06,p[2]*.66))).lerp(attach,.55),190 if p[0]<0 else 15,20,.14)

def seeds(kind):
 count=5 if kind=='Rose' else 1
 for i in range(count):
  a=i*2.4;r=.045 if count>1 else 0;p=(r*math.cos(a),r*math.sin(a),.015)
  o=grain(kind+' seed',p,(math.cos(a+.4),math.sin(a+.4),.12),.055 if kind=='Rose' else .075,.034 if kind!='Hibiscus' else .042,.025,'SeedTan' if kind=='Rose' else 'SeedBrown','Rice');o['SeedKind']=kind
  if kind=='Jasmine':C.sphere('Jasmine pale seed hilum',(-.029,-.012,.011),(.007,.006,.005),'SeedTan',1)

def sprout(kind):
 path_tube(kind+' sprout stem',[(0,0,.005),(0,0,.075),(0,0,.13)],.008,'StemGreen',taper=.1)
 for j,(a,l) in enumerate([(202,.135),(22,.145)]):leaf_at(kind,(0,0,.105),a,35,l,.075,'JasmineLeaf',petiole=.014)

def clear_blooms(kind):
 blooms=[o for o in C.current.objects if o.get('OpenFlower')==kind]
 for leaf in list(C.current.objects):
  if leaf.get('ReferenceLeaf')!=kind or not leaf.get('PetioleName'):continue
  blocked=False
  for flower in blooms:
   f=Frame(flower['CorollaCenter'],flower['CorollaNormal']);radius=flower['CorollaRadius']*.76
   for v in leaf.data.vertices:
    d=v.co-f.c
    if (d.dot(f.x)**2+d.dot(f.y)**2)<radius*radius and -.035<d.dot(f.n)<.23:blocked=True;break
   if blocked:break
  if blocked:
   import bpy
   stem=bpy.data.objects.get(leaf['PetioleName'])
   if stem:bpy.data.objects.remove(stem,do_unlink=True)
   bpy.data.objects.remove(leaf,do_unlink=True)

def harvested(kind):
 before=set(C.current.objects)
 tip=Vector((0,0,.39 if kind=='Rose' else .24 if kind=='Hibiscus' else .29));n=Vector((0,-.90,.44));center=tip+n*({'Rose':.043,'Hibiscus':.090,'Jasmine':.030}[kind])
 if kind=='Jasmine':
  path_tube('Jasmine cut leafy sprig',[(0,0,.008),(0,0,.15),tip],.008,'StemGreen',taper=.35)['CutFlowerStem']=True
  for i,(p,normal,s) in enumerate([((-.055,-.015,.23),(-.2,-.92,.35),.92),((.07,-.02,.34),(.15,-.90,.4),.95)]):
   attach=open_flower(kind,p,normal,i,s);branch_to(kind,(0,0,.13+i*.06),attach,normal,'Flower_'+str(i),.005)
  for i,(p,norm) in enumerate([((-.07,.02,.38),(-.2,0,1)),((.06,.015,.43),(.2,0,1)),((.13,.02,.32),(.5,0,.9))]):
   attach=bud(kind,p,norm,i,.85);branch_to(kind,(0,0,.17),attach,norm,'Bud_'+str(i),.004)
  for i in range(5):leaf_at(kind,(0,0,.065+i*.04),180+(i%2)*180,25,.14,.09,petiole=.02)
 else:
  attach=open_flower(kind,center,n,0,1 if kind=='Rose' else .96);branch_to(kind,(0,0,.008),attach,n,'Flower_0',.008)['CutFlowerStem']=True
  if kind=='Rose':
   compound((0,0,.12),180,20,.13);compound((0,0,.23),0,25,.12)
   for i in range(5):thorn((.006,0,.035+i*.06),(1 if i%2 else -1,.1,.05))
  else:
   for i in range(2):leaf_at(kind,(0,0,.06+i*.08),180+i*180,22,.16,.10,petiole=.022)
 clear_blooms(kind)
 # Lay the cut flower on a side, with the corolla still facing up toward inspection.
 rot=Matrix.Rotation(math.radians(-64),4,'Y')
 new=set(C.current.objects)-before
 for o in new:
  if o.type=='MESH':
   matrix=o.matrix_basis.copy();o.data.transform(matrix);o.matrix_basis=Matrix.Identity(4);o.data.transform(rot)
 lo=min(v.co.z for o in new if o.type=='MESH' for v in o.data.vertices)
 for o in new:
  if o.type=='MESH':
   for v in o.data.vertices:v.co.z+=.008-lo

def build(kind,stage):
 if stage=='Seed':seeds(kind)
 elif stage=='Sprout':sprout(kind)
 elif stage=='Harvest':harvested(kind)
 else:
  h=foliage(kind,stage)
  if stage!='Young':flowers_on_shrub(kind,stage,h)
  if stage=='Ready':clear_blooms(kind)
  compression={'Rose':.90,'Hibiscus':.86,'Jasmine':.82}[kind]
  transform=Matrix.Diagonal((1,1,compression,1))
  for o in C.current.objects:
   if o.type=='MESH':
    o.data.transform(transform @ o.matrix_basis);o.matrix_basis=Matrix.Identity(4)
