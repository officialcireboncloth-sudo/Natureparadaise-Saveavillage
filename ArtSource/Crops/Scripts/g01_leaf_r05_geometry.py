"""Low-poly closed blades whose actual serrated outlines come from the user's pictures."""
import math,json
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
from g12_geometry import bezier
import plant_blender_common as C
import g01_config as G
from unified_plant_atlas import load
CONTOURS=load(G.REVIEW/'Leaf_R05/ReferenceContours.json')['leaves']

def create(old,origin):
 kind=old['ReferenceLeaf'];ref=CONTOURS[kind];length=float(old['LeafLength']);width=length*ref['measuredAspect'];angle=float(old['LeafAzimuth']);elevation=float(old['LeafElevation']);roll=float(old['LeafRoll']);index=int(old['LeafIndex']);origin=Vector(origin);a=math.radians(angle);e=math.radians(elevation);radial=Vector((math.cos(a),math.sin(a),0));across=Vector((radial.y,-radial.x,0));tip=origin+radial*length*math.cos(e)+Vector((0,0,length*math.sin(e)));control=[origin,origin+radial*length*.28*math.cos(e)+Vector((0,0,length*(.30*math.sin(e)+.075))),origin+radial*length*.76*math.cos(e)+Vector((0,0,length*(.82*math.sin(e)+.055))),tip]
 # The measured left/right edges retain their asymmetric teeth and long pointed tip.
 left=[(float(x),float(t)) for t,x in ref['left']];right=[(float(x),float(t)) for t,x in ref['right']];spine=[(0.,t) for t in [.20,.40,.60,.80]];coords=[];ids={}
 def add(p):
  key=tuple(round(v,8) for v in p)
  if key not in ids:ids[key]=len(coords);coords.append(tuple(p))
  return ids[key]
 polys=[left+list(reversed(spine)),[right[0]]+spine+list(reversed(right[1:]))]
 triangles=[]
 for poly in polys:
  vectors=[Vector((x,t,0)) for x,t in poly]
  for p in poly:add(p)
  for tri in tessellate_polygon([vectors]):
   triVectors=[vectors[v] if isinstance(v,int) else v for v in tri];p=[(float(v.x),float(v.y)) for v in triVectors];ind=[min(range(len(coords)),key=lambda k:(coords[k][0]-q[0])**2+(coords[k][1]-q[1])**2) for q in p]
   cross=(p[1][0]-p[0][0])*(p[2][1]-p[0][1])-(p[1][1]-p[0][1])*(p[2][0]-p[0][0])
   if cross<0:ind.reverse()
   triangles.append(tuple(ind))
 outline=[add(p) for p in right]+[add(p) for p in reversed(left[1:-1])];verts=[];uv=[];uvScale=.465/max(abs(x) for x,t in coords)
 for side in [1,-1]:
  for x,t in coords:
   p0,p1,p2,p3=control;center=bezier(control,t);tangent=(3*(1-t)**2*(p1-p0)+6*(1-t)*t*(p2-p1)+3*t*t*(p3-p2)).normalized();u=(across-tangent*across.dot(tangent)).normalized();n=u.cross(tangent).normalized();r=roll+.05*math.sin(index*1.7)*t;u,n=u*math.cos(r)+n*math.sin(r),-u*math.sin(r)+n*math.cos(r);edge=math.sin(math.pi*t);rib=.0024*math.exp(-x*x/.015)*edge;cup=width*.065*(2*x)**2*edge;half=.00023+.0015*edge**.8*max(0,1-(2*x)**2)**.6;verts.append(center+u*width*x+n*(rib+cup+side*half));uv.append((.5+uvScale*x,.035+.93*t))
 count=len(coords);faces=triangles+[tuple(v+count for v in reversed(tri)) for tri in triangles]
 for i,a in enumerate(outline):b=outline[(i+1)%len(outline)];faces.append((a,a+count,b+count,b))
 ob=C.mesh(kind+' R05 reference-traced serrated blade',verts,faces,kind+'Leaf',uv)
 for key in old.keys():
  if key!='_RNA_UI':ob[key]=old[key]
 ob['LeafRevision']='R05';ob['ClosedVolume']=True;ob['LeafOrigin']=list(origin);ob['LeafOriginIncludesGroundOffset']=True;ob['LeafWidth']=width;ob['ReferenceContour']=ref['reference'];ob['ReferenceContourSha256']=ref['referenceSha256'];ob['ReferenceOutlineJSON']=json.dumps(dict(left=ref['left'],right=ref['right']));ob['ReferenceOutlineVertexCount']=len(outline);ob['ReferenceBoundaryIndices']=outline;ob['Serrated']=True;ob['VeinUV']='reference contour with continuous midrib';return ob
