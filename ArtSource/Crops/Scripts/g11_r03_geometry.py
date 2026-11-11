"""Leaf frames derive from radial plant growth and world-up, never a viewing direction."""
import math
from mathutils import Vector
import plant_blender_common as C
TAU=math.tau

def bezier(points,t):
 a,b,c,d=map(Vector,points);return a*(1-t)**3+3*b*(1-t)**2*t+3*c*(1-t)*t*t+d*t**3

def spatial_points(points,index,flat):
 pts=[Vector(p) for p in points]
 if flat:return pts
 stage=C.parent.get('Phase')
 targets=([.006,.040,.130,.180] if index==0 else [.012,.025,.080,.130] if index==1 else [-.012,-.060,-.170,-.230]) if stage=='Young' else ([.006,.035,.100,.150] if index==0 else [-.013,-.040,-.105,-.110])
 # Keep the familiar front projection while genuinely distributing blades in depth.
 for p,y in zip(pts,targets):p.z-=(y-p.y)*math.tan(math.radians(18));p.y=y
 return pts

def leaf(points,width,index,roll0=0,roll1=0,flat=False,shoot=False):
 pts=spatial_points(points,index,flat) if not shoot else [Vector(p) for p in points]
 p0,p1,p2,p3=pts;rows,cols=37,13;verts=[];uv=[];faces=[]
 radial=Vector((p3.x-p0.x,p3.y-p0.y,0))
 if radial.length<.001:radial=Vector((math.cos(index*2.3),math.sin(index*2.3),0))
 radial.normalize();growthAcross=Vector((radial.y,-radial.x,0))
 stage=C.parent.get('Phase')
 if not flat:
  if shoot:roll0,roll1=(.15,.90) if index==0 else (-.80,-.05)
  elif stage=='Young':roll0,roll1=[(.15,.60),(-.05,.40),(-.35,-.05)][index]
  else:roll0,roll1=(.18,.66) if index==0 else (-1.10,-1.36)
 for side in [1,-1]:
  for i in range(rows):
   t=(1-math.cos(math.pi*i/(rows-1)))*.5;center=bezier(pts,t)
   tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized()
   if flat:
    n=Vector((0,0,1));n-=tangent*n.dot(tangent);n.normalize();u=tangent.cross(n).normalized()
   else:
    u=growthAcross-tangent*growthAcross.dot(tangent);u.normalize();n=u.cross(tangent).normalized()
   roll=roll0+(roll1-roll0)*t**1.15;u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
   profile=.001+math.sin(math.pi*t)**.57*(1.24-.56*t)+.115*(1-t)**12
   for j in range(cols):
    q=j/(cols-1)*2-1
    if shoot:
     # Rolled U-shaped shoot with a real cylindrical cross-section.
     phi=q*1.35;xx=width*.52*profile*math.sin(phi);bend=width*.52*profile*(1-math.cos(phi))
    else:xx=width*.5*profile*q;bend=width*(.145*q*q-.025*(1-q*q))*math.sin(math.pi*t)
    half=.00010+(.00115 if shoot else .00155)*math.sin(math.pi*t)**.6*max(0,1-q*q)**.6
    verts.append(center+u*xx+n*(bend+side*half));uv.append(((q+1)/2,t))
 nv=rows*cols
 for i in range(rows-1):
  for j in range(cols-1):
   k=i*cols+j;face=(k,k+1,k+cols+1,k+cols);faces.extend([face,tuple(v+nv for v in reversed(face))])
 edge=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+[(rows-1)*cols+j for j in range(cols-2,-1,-1)]+[i*cols for i in range(rows-2,0,-1)]
 for i,k in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((k,k+nv,b+nv,b))
 ob=C.mesh('Tulip R03 spatial rolled shoot' if shoot else 'Tulip R03 spatial curved blade',verts,faces,'TulipLeaf',uv)
 ob['ReferenceLeaf']='Tulip';ob['LeafIndex']=index;ob['CurveControlPoints']=[list(p) for p in pts];ob['LeafRollRadians']=[roll0,roll1];ob['ClosedLeaf']=True;ob['SpatialLeaf']=True;ob['LeafFrame']='radial growth and world up';ob['TulipShoot']=shoot;return ob

def shoots():
 leaf([(-.011,0,.218),(-.018,-.010,.290),(-.044,-.032,.360),(-.040,-.040,.410)],.043,0,shoot=True)
 leaf([(.010,.004,.218),(.018,.020,.315),(.040,.045,.425),(.040,.055,.520)],.054,1,shoot=True)
