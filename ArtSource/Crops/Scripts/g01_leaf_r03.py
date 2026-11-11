"""Broad ovate leaves, fine edge teeth, aligned veins and restrained natural rotations."""
import math
import numpy as np
from mathutils import Vector
from g12_geometry import bezier,closed_grid
import plant_blender_common as C

def create(origin,angle,elevation,length,width,index,kind,roll=0):
 origin=Vector(origin);a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));across=Vector((radial.y,-radial.x,0));e=math.radians(elevation)
 # Short petiole taper, fuller lower-middle blade and a gently lowered pointed tip.
 tip=origin+radial*length*math.cos(e)+Vector((0,0,length*math.sin(e)))
 points=[origin,origin+radial*length*.28*math.cos(e)+Vector((0,0,length*(.30*math.sin(e)+.075))),origin+radial*length*.76*math.cos(e)+Vector((0,0,length*(.82*math.sin(e)+.055))),tip]
 rows,cols=(29,7) if kind!='Blueberry' else (21,7);verts=[];uv=[]
 for side in [1,-1]:
  for i in range(rows):
   t=i/(rows-1);center=bezier(points,t);p0,p1,p2,p3=points;tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized();u=(across-tangent*across.dot(tangent)).normalized();n=u.cross(tangent).normalized();r=roll+.045*math.sin(index*1.7)*t;u,n=u*math.cos(r)+n*math.sin(r),-u*math.sin(r)+n*math.cos(r)
   profile=.001+math.sin(math.pi*t)**.72*(1.12-.30*t)
   if kind!='Blueberry':profile*=1-.065*(.5-.5*math.cos(22*math.pi*t))
   for j in range(cols):
    q=j/(cols-1)*2-1;edge=math.sin(math.pi*t);asym=1+.025*q*math.sin(index*1.31);rib=.0018*math.exp(-q*q/.10)*edge;cup=width*.035*q*q*edge;half=.00023+.0015*edge**.8*max(0,1-q*q)**.6
    verts.append(center+u*width*.5*profile*q*asym+n*(rib+cup+side*half))
    # Sample the blade silhouette on the painted surface, keeping one central vein.
    # Previously every narrow tip row spanned the whole square, stretching its veins.
    uv.append((.5+.465*q*profile,.035+.93*t))
 ob=closed_grid(kind+' R03 ovate leaf',verts,uv,rows,cols,kind+'Leaf');ob['ReferenceLeaf']=kind;ob['LeafIndex']=index;ob['SpatialLeaf']=True;ob['Serrated']=kind!='Blueberry';ob['LeafFrame']='radial growth and world up';ob['LeafRevision']='R03';ob['LeafOrigin']=list(origin);ob['LeafLength']=length;ob['LeafWidth']=width;ob['LeafElevation']=elevation;ob['LeafAzimuth']=angle;ob['LeafRoll']=roll;ob['VeinUV']='taper-aware centerline';ob['CurveControlPoints']=[list(p) for p in points];return ob

def replacement(old,kind,stage,fruit_bounds):
 v=np.array([tuple(p.co) for p in old.data.vertices]);nv=len(v)//2;cols=5;rows=nv//cols;top=v[:nv].reshape(rows,cols,3);bottom=v[nv:].reshape(rows,cols,3);centers=(top[:,2]+bottom[:,2])*.5;origin=centers[0];tip=centers[-1];d=tip-origin;length=float(np.linalg.norm(d))*.96;width=float(np.linalg.norm(top[:,-1]-top[:,0],axis=1).max())*(1.00 if kind=='Blueberry' else 1.20);idx=int(old.get('LeafIndex',0));angle=math.degrees(math.atan2(d[1],d[0]));old_e=math.degrees(math.atan2(d[2],np.linalg.norm(d[:2])))
 if stage=='Sprout':elevation=19+11*math.sin(idx*1.8+.4)
 elif old_e>63 and origin[2]>.32:elevation=39+6*math.sin(idx*1.4)
 elif origin[2]<.14:elevation=-4+10*math.sin(idx*1.6)
 else:elevation=8+17*math.sin(idx*1.73+.45)
 # Keep leaf bases at their actual branch junctions and all azimuths around the shrub.
 roll=.22*math.sin(idx*2.13+.6);best=None
 for da,de in [(0,0),(-18,0),(18,0),(0,-13),(0,13),(-28,8),(28,8)]:
  ob=create(origin,angle+da,elevation+de,length,width,idx,kind,roll);pts=np.array([tuple(p.co) for p in ob.data.vertices]);hits=0
  for center,extent in fruit_bounds:hits+=int(np.sum(np.sum(((pts-center)/extent)**2,axis=1)<.94))
  penalty=hits*100+abs(da)*.04+abs(de)*.05
  if best is None or penalty<best[0]:
   if best is not None:
    import bpy;bpy.data.objects.remove(best[1],do_unlink=True)
   best=(penalty,ob)
  else:
   import bpy;bpy.data.objects.remove(ob,do_unlink=True)
  if hits==0:break
 ob=best[1];ob['FruitCollisionSamples']=int(best[0]//100);return ob
