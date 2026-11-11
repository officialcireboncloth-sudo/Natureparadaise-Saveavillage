"""Sculpted cereal clumps: pleated foliage, branched rice and compact wheat ears."""
import math,random
from mathutils import Vector
import plant_blender_common as C
TAU=math.tau

def path_tube(name,points,radius,slot,sides=8,taper=.55):
 pts=[Vector(p) for p in points];verts=[];uv=[];faces=[]
 for i,p in enumerate(pts):
  t=i/(len(pts)-1);tangent=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
  side=tangent.cross(Vector((0,1,0)))
  if side.length<.01:side=tangent.cross(Vector((1,0,0)))
  side.normalize();other=tangent.cross(side).normalized();r=radius*(1-taper*t)
  for j in range(sides+1):
   angle=TAU*j/sides;verts.append(p+r*(math.cos(angle)*side+math.sin(angle)*other));uv.append((j/sides,t))
 for i in range(len(pts)-1):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(pts)-1)*(sides+1)+j for j in range(sides))])
 return C.mesh(name,verts,faces,slot,uv)

def grain(name,center,direction,length,width,depth,slot,kind='Rice',detail=1):
 # A pointed spindle with raised longitudinal ribs; wheat has a ventral groove.
 center=Vector(center);direction=Vector(direction).normalized();rot=Vector((0,0,1)).rotation_difference(direction)
 rows=10 if detail else 7;sides=12 if detail else 8;verts=[];faces=[];uv=[]
 for i in range(rows+1):
  t=i/rows;s=math.sin(math.pi*t)
  profile=max(.008,s**(.80 if kind=='Rice' else .57));profile*=1.06-.12*t
  for j in range(sides+1):
   phi=TAU*j/sides;ridge=1+.085*math.cos(phi*6)*(s**.6)
   x=width*.5*profile*math.cos(phi)*ridge;y=depth*.5*profile*math.sin(phi)*ridge
   if kind=='Wheat':
    # A true shallow central crease on the underside, retained by the mesh.
    groove=math.exp(-((math.cos(phi))/.24)**2)*max(0,-math.sin(phi))
    y*=1-.23*groove*s
   p=rot@Vector((x,y,(t-.5)*length));verts.append(center+p);uv.append((j/sides,t))
 for i in range(rows):
  for j in range(sides):k=i*(sides+1)+j;faces.append((k,k+1,k+sides+2,k+sides+1))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple(rows*(sides+1)+j for j in range(sides))])
 o=C.mesh(name,verts,faces,slot,uv);o['CerealGrain']=kind;o['LongitudinalRidges']=True
 if kind=='Wheat':o['KernelGroove']=True
 return o

def blade(name,base,angle,length,width,slot,lift=1.2,spread=.5,droop=-.6,twist=.10,kind='Rice'):
 base=Vector(base);d=Vector((math.cos(angle),math.sin(angle),0));side=Vector((-d.y,d.x,0));verts=[];uv=[];faces=[]
 rows=25;cols=5
 for i in range(rows):
  t=i/(rows-1);profile=(math.sin(math.pi*t)**.64)*(.93-.23*t)+.012
  center=base+d*(length*spread*(t+.16*t*t));center.z+=length*(lift*t+droop*t*t)
  for j in range(cols):
   q=j/(cols-1)*2-1;half=width*.5*profile
   p=center+side*(q*half)
   # Raised central keel, folded edges, taper and gentle longitudinal twist.
   p.z+=width*(.16*(1-abs(q))+.15*abs(q)**1.4)*math.sin(math.pi*t)
   p.z+=twist*width*q*math.sin(math.pi*t)*t
   p+=d*(.05*width*q*q*math.sin(math.pi*t))
   verts.append(p);uv.append(((q+1)/2,t))
 for i in range(rows-1):
  for j in range(cols-1):k=i*cols+j;faces.append((k,k+1,k+1+cols,k+cols))
 o=C.mesh(name,verts,faces,slot,uv);solid=o.modifiers.new('Fine blade thickness','SOLIDIFY');solid.thickness=.0011;solid.offset=0;o['ReferenceLeaf']=kind;o['PleatedBlade']=True;return o

def stalk(name,base,height,angle,slot,tip_offset=None):
 base=Vector(base);d=Vector((math.cos(angle),math.sin(angle),0))
 offset=Vector(tip_offset) if tip_offset is not None else d*.035
 points=[base+offset*(t**1.35) + Vector((0,0,height*t)) for t in [i/10 for i in range(11)]]
 o=path_tube(name,points,.0064,slot,taper=.36);o['CerealCulm']=True;o['CulmIndex']=int(name.rsplit(' ',1)[1]);return points

def rice_panicle(base,angle,mature,index=0,scale=1):
 base=Vector(base);d=Vector((math.cos(angle),math.sin(angle),0));s=Vector((-d.y,d.x,0));slot='RiceGold' if mature else 'RiceGreen';stem='StemGold' if mature else 'StemGreen'
 def main(t):return base+scale*(d*((.34 if mature else .28)*t)+Vector((0,0,(.28 if mature else .36)*t-(.59 if mature else .46)*t*t)))
 o=path_tube('Rice curved panicle rachis '+str(index),[main(i/24) for i in range(25)],.0030*scale,stem,taper=.72);o['Panicle']='Rice';o['MatureDroop']=bool(mature)
 for j,t0 in enumerate([.10,.23,.36,.49,.63,.77]):
  origin=main(t0);sign=1 if j%2 else -1;side_length=(.13-.045*t0)*sign
  def branch(f):return origin+scale*(s*(side_length*f)+d*(.09*f)+Vector((0,0,.012*f-(.075 if mature else .042)*f*f)))
  branch_ob=path_tube('Rice fine side branch '+str(index)+' '+str(j),[branch(k/8) for k in range(9)],.0014*scale,stem,sides=6,taper=.72);branch_ob['PanicleBranch']=True
  for k,f in enumerate([.20,.38,.56,.74,.93]):
   anchor=branch(f);direction=(s*(sign*.22)+d*.33+Vector((0,0,-1 if mature else -.50))).normalized()
   start=anchor+scale*(s*sign*.012+Vector((0,0,-.009)))
   path_tube('Rice pedicel',[anchor,start],.0009*scale,stem,sides=5,taper=.4)
   grain('Rice filled husk',start+direction*.025*scale,direction,.060*scale,.0235*scale,.018*scale,slot)
 for k,t in enumerate([.29,.42,.55,.68,.81,.93,1.02]):
  anchor=main(min(1,t));direction=(s*(.14 if k%2 else -.14)+d*.30+Vector((0,0,-1 if mature else -.55))).normalized()
  grain('Rice terminal husk',anchor+direction*.023*scale,direction,.058*scale,.023*scale,.0175*scale,slot)

def wheat_ear(base,angle,mature,index=0,scale=1,axis=None,refined=False):
 base=Vector(base);lean=Vector(axis).normalized() if axis is not None else Vector((math.cos(angle)*.15,math.sin(angle)*.15,1)).normalized();rot=Vector((0,0,1)).rotation_difference(lean);slot='WheatGold' if mature else 'WheatGreen';stem='StemGold' if mature else 'StemGreen'
 length=.34*scale
 o=path_tube('Wheat compact ear rachis '+str(index),[base,base+lean*length],.0035*scale,stem,taper=.52);o['Panicle']='Wheat';o['UprightEar']=True;o['CulmIndex']=index;o['CulmAligned']=axis is not None;o['TaperedEar']=refined
 # Opposed, slightly alternating spikelets make a compact overlapping ear.
 for rank in range(9):
  t=rank/8;z=(.022+rank*.033)*scale;w=(.024*(1-.52*t)+.004)*scale
  if refined:
   fullness=min(1,(t+.16)/.34)*(1-.68*t**2.2)
   z=(.018+rank*.0355)*scale;w=(.006+.020*fullness)*scale
  for sign in [-1,1]:
   depth=.008*(1-t)*(-1 if rank%2 else 1)*scale
   center=base+rot@Vector((sign*w,depth,z));direction=rot@Vector((sign*(.72-.22*t),.08*(-1 if rank%2 else 1),1)).normalized()
   glen=(.043+.023*fullness) if refined else (.069-.012*t);gwidth=(.012+.030*fullness) if refined else (.043-.014*t);gdepth=(.017+.016*fullness) if refined else .033
   g=grain('Wheat overlapping spikelet',center,direction,glen*scale,gwidth*scale,gdepth*scale,slot,'Wheat');g['SpikeletRank']=rank
   tip=center+direction*((glen*.45 if refined else .031)*scale);end=tip+rot@Vector((sign*.013,-.004,(.038+.015*t if refined else .047+.015*t)*scale))
   awn=path_tube('Wheat fine awn',[tip,end],.00080*scale,'AwnGold' if mature else 'StemGreen',sides=5,taper=.90);awn['WheatAwn']=True
 grain('Wheat apical spikelet',base+lean*(.332 if refined else .329)*scale,lean,(.041 if refined else .049)*scale,(.016 if refined else .019)*scale,.015*scale,slot,'Wheat')

def seed_stage(kind):
 rng=random.Random(801 if kind=='Rice' else 802);count=3 if kind=='Rice' else 5
 for i in range(count):
  a=i*2.4;r=.046 if count==3 else .053;pos=(r*math.cos(a),r*math.sin(a),.016)
  grain(kind+' seed',pos,(math.cos(a+.4),math.sin(a+.4),.11),.099 if kind=='Rice' else .075,.034 if kind=='Rice' else .043,.024,'RiceSeed' if kind=='Rice' else 'WheatSeed',kind)

def sprout(kind):
 grain(kind+' attached germinating seed',(.025,-.01,.015),(.9,-.3,.10),.093 if kind=='Rice' else .072,.034 if kind=='Rice' else .043,.023,'RiceSeed' if kind=='Rice' else 'WheatSeed',kind)
 path_tube(kind+' sprout sheath',[(0,0,.006),(0,0,.06),(.005,0,.11)],.006,'StemGreen',taper=.3)
 for i,(a,l,lift,spread) in enumerate([(2.1,.265,1.4,.51),(-.7,.32,1.3,.45)]):blade(kind+' sprout blade '+str(i),(0,0,.065),a,l,.027 if kind=='Rice' else .034,'LeafGreen',lift=lift,spread=spread,droop=-.35,kind=kind)

def clump(kind,stage):
 rng=random.Random(808+(0 if kind=='Rice' else 100)+['Young','Heading','Ready'].index(stage))
 mature=stage=='Ready';heading=stage=='Heading';rice=kind=='Rice'
 n=5 if rice else 6
 if stage=='Young':n=5
 if heading and not rice:n=3
 heights=([.86,1.02,.92,1.10,.91] if rice else [.80,.99,.87,1.06,.83,1.00])
 if heading:heights=[h*.93 for h in heights]
 if stage=='Young':heights=[.35+.035*i for i in range(n)]
 for i in range(n):
  angle=i*2.399+.22;rad=.024+.012*(i%3);base=Vector((rad*math.cos(angle),rad*math.sin(angle),.005))
  h=heights[i];stem='StemGold' if mature else 'StemGreen'
  if stage=='Young':offset=None
  elif rice:offset=(.11*math.cos(angle),.10*math.sin(angle),0)
  elif heading:offset=([-.17,.02,.18][i],[.02,.08,0][i],0)
  else:offset=([-.28,-.15,-.04,.08,.21,.31][i],[.00,.12,-.10,.12,-.05,.06][i],0)
  points=stalk(kind+' culm '+str(i),base,h,angle,stem,offset)
  if stage!='Young':
   head_scale=.98+rng.random()*.05
   if rice:rice_panicle(points[-1],angle,mature,i,head_scale)
   else:wheat_ear(points[-1],angle,mature,i,head_scale,axis=points[-1]-points[-2] if heading else None,refined=heading)
  leaf_count=6 if rice else 5
  for j in range(leaf_count):
   angle_leaf=angle+j*2.25+rng.uniform(-.20,.20);z=.018+j*(h*.125 if stage!='Young' else .032)
   frac=j/(leaf_count-1);l=(.55 if rice else .50)*(1-.18*frac)*(.88+rng.random()*.22)
   if stage=='Young':l*=.93
   if mature:
    slot='LeafGold' if ((i+j)%4!=0 and j<4) else 'LeafWarm' if (i+j)%2 else 'LeafGreen'
   else:slot='LeafGreen' if (i+j)%4 else 'LeafWarm'
   # Lower leaves arch outwards, upper flag leaves stand much more upright.
   lift=1.17+.32*frac;spread=.66-.33*frac;droop=-.76+.17*frac
   if stage=='Young':lift+=.17;droop+=.14
   start=base+Vector((0,0,z))
   if offset is not None:start+=Vector(offset)*((z/h)**1.35)
   blade(kind+' strap blade '+str(i)+' '+str(j),start,angle_leaf,l,(.043 if rice else .058)*(1-.18*frac),slot,lift,spread,droop,twist=rng.uniform(-.20,.20),kind=kind)
 if stage!='Young':
  for i in range(12):
   a=i*2.399+.6;l=.62+.12*(i%3)/2
   slot=('LeafGold' if i%3 else 'LeafWarm') if mature else 'LeafGreen'
   blade(kind+' basal tiller '+str(i),(.035*math.cos(a),.035*math.sin(a),.008),a,l,.042 if rice else .055,slot,lift=1.38,spread=.25+.05*(i%2),droop=-.44,twist=.12,kind=kind)
 # Upright inner leaves fill the centre in the grass-only phase.
 if stage=='Young':
  for i in range(4):blade(kind+' inner spear '+str(i),(0,0,.012),i*2.40,.66,.033 if rice else .047,'LeafGreen',lift=1.36,spread=.23,droop=-.18,kind=kind)

def harvest(kind):
 # Build intact botanical heads in upright local space then lay them on the ground.
 before=set(C.current.objects)
 for i in range(3):
  x=(i-1)*.12;base=Vector((x,0,0));tip=Vector((x+(i-1)*.025,.015*i,.40+(i%2)*.07))
  if kind=='Wheat':
   base=Vector(((i-1)*.032,0,0));tip=Vector(((i-1)*.095,.010*(i%2),.30+.035*(i%2)))
  cut=path_tube(kind+' cut harvest stalk',[base,tip],.007,'StemGold',taper=.34);cut['CutCulm']=True;cut['CulmIndex']=i
  if kind=='Rice':rice_panicle(tip,-.4+i*.45,True,i)
  else:wheat_ear(tip,i*.7,True,i,axis=tip-base,refined=True)
 # Preserve enough side depth in the loose bundle; final minimum z is exactly above ground.
 from mathutils import Matrix
 rot=Matrix.Rotation(math.radians(-62),4,'Z') @ Matrix.Rotation(math.radians(84),4,'X')
 new=set(C.current.objects)-before
 for o in new:
  o.data.transform(rot)
 lo=min(v.co.z for o in new for v in o.data.vertices)
 for o in new:
  for v in o.data.vertices:v.co.z=.008+(v.co.z-lo)*(.35 if kind=='Rice' else .80)

def pile(kind,white=False):
 rng=random.Random(888+(1 if white else 0)+(2 if kind=='Wheat' else 0))
 slot='RiceWhite' if white else 'RiceSeed' if kind=='Rice' else 'WheatSeed'
 for layer,n in enumerate([15,10,5]):
  rad=.10-layer*.026
  for i in range(n):
   a=rng.random()*TAU;r=rad*math.sqrt(rng.random());angle=rng.random()*TAU
   grain(kind+' '+('polished rice' if white else 'grain')+' pile',(.02+r*math.cos(a),r*math.sin(a),.013+layer*.017),(math.cos(angle),math.sin(angle),rng.uniform(-.09,.14)),.060 if white else .080 if kind=='Rice' else .064,.022 if white else .029 if kind=='Rice' else .038,.016 if white else .022,slot,kind,detail=1)

def build(kind,stage):
 if stage=='Seed':seed_stage(kind)
 elif stage=='Sprout':sprout(kind)
 elif stage=='Harvest':harvest(kind)
 else:clump(kind,stage)
