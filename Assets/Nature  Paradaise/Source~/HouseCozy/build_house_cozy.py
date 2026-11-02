"""Native Blender authoring for the approved cozy Lv1 facade; shared atlas UVs, editable modules and three LODs."""
import bpy, bmesh, math, random, json
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir()); A=ROOT/'Assets/Nature  Paradaise'; OUT=ROOT/'ArtSource/HouseCozy'
for path in [OUT/'Previews', A/'Source~/HouseCozy', A/'mesh/Buildings/HouseCozy']:path.mkdir(parents=True,exist_ok=True)
random.seed(731)
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.name='House Cozy Lv1 - Atlas Master';scene.unit_settings.system='METRIC'
source=bpy.data.collections.new('01 EDITABLE HOUSE MODULES');scene.collection.children.link(source)
exports=bpy.data.collections.new('02 GAME LODS');scene.collection.children.link(exports)
studio=bpy.data.collections.new('03 PREVIEW ONLY - NO EXPORT');scene.collection.children.link(studio)
manifest=json.loads((A/'Textures/Shared/Cozy/NP_CozyAtlasSlots.json').read_text())
S={r['name']:r for r in manifest['slots']}
atlas=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_01_BaseColor.png'));atlas.pack()
foliage=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozyFoliage_01_BaseColor.png'));foliage.pack()
normal=bpy.data.images.load(str(A/'Textures/Shared/Cozy/NP_CozySurfaces_01_Normal.png'));normal.colorspace_settings.name='Non-Color';normal.pack()
def mat(name,img,alpha=False,glow=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(.7,.5,.25,1)
 ns=m.node_tree.nodes;bs=ns.get('Principled BSDF');bs.inputs['Roughness'].default_value=.78
 tx=ns.new('ShaderNodeTexImage');tx.image=img;tx.extension='EXTEND';m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
 if alpha:
  m.node_tree.links.new(tx.outputs['Alpha'],bs.inputs['Alpha']);m.surface_render_method='DITHERED';m.use_transparent_shadow=True
 elif not glow:
  nt=ns.new('ShaderNodeTexImage');nt.image=normal;n=ns.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.30;m.node_tree.links.new(nt.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],bs.inputs['Normal'])
 if glow:m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.4
 return m
M=[mat('NP_CozySurfaces_01',atlas),mat('NP_CozyFoliage_01',foliage,True),mat('NP_CozyGlow_01',atlas,glow=True)]
parts=[]; lodtag={}
def cv(p):return Vector((p[0],-p[2],p[1]))
def link(obj,name,detail=0):
 obj.name=name
 for c in list(obj.users_collection):c.objects.unlink(obj)
 source.objects.link(obj);parts.append(obj);lodtag[obj.name]=detail;obj['CozyDetailTier']=detail
 return obj
def region(name):return S[name]['uvSafe']
MASONRY_SCALE={'StoneWarm':(2.6,1.35),'StoneGray':(2.6,1.35),'Brick':(1.8,1.6),'Paving':(2.4,1.2)}
uv_audit=[]

def masonry_uv(obj,slot):
 # Repeat at physical scale, clipping faces so no UV ever leaves its atlas slot.
 old=obj.data;world=[v.co+obj.location for v in old.vertices];verts=[];faces=[];mapped=[]
 def clip(poly,axis,value,positive):
  result=[]
  for a,b in zip(poly,poly[1:]+poly[:1]):
   da=(a[axis]-value)*(1 if positive else -1);db=(b[axis]-value)*(1 if positive else -1)
   if da>=-1e-7:result.append(a)
   if (da>1e-7 and db<-1e-7) or (da<-1e-7 and db>1e-7):result.append(a+(b-a)*(da/(da-db)))
  return result
 for p in old.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=(0,1) if axis==2 else ((0,2) if axis==1 else (1,2))
  face_slot='StoneWarm' if slot=='Paving' and axis!=2 else slot
  period=MASONRY_SCALE[face_slot];rect=region(face_slot);poly=[world[i].copy() for i in p.vertices]
  start=[math.floor(min(v[a] for v in poly)/period[j]+1e-7) for j,a in enumerate(axes)]
  end=[math.floor(max(v[a] for v in poly)/period[j]-1e-7) for j,a in enumerate(axes)]
  for ucell in range(start[0],max(start[0],end[0])+1):
   for vcell in range(start[1],max(start[1],end[1])+1):
    cells=(ucell,vcell);piece=poly
    for j,a in enumerate(axes):
     piece=clip(piece,a,cells[j]*period[j],True)
     if piece:piece=clip(piece,a,(cells[j]+1)*period[j],False)
     if not piece:break
    if len(piece)<3:continue
    clean=[]
    for point in piece:
     if not clean or (point-clean[-1]).length>1e-6:clean.append(point)
    if len(clean)>2 and (clean[0]-clean[-1]).length<1e-6:clean.pop()
    if len(clean)<3:continue
    first=len(verts);verts.extend([tuple(point-obj.location) for point in clean]);faces.append(tuple(range(first,first+len(clean))))
    mapped.append([(rect[0]+(rect[2]-rect[0])*max(0,min(1,point[axes[0]]/period[0]-ucell)),rect[1]+(rect[3]-rect[1])*max(0,min(1,point[axes[1]]/period[1]-vcell))) for point in clean])
 me=bpy.data.meshes.new(old.name+' - density corrected');me.from_pydata(verts,[],faces);me.update();me.materials.append(M[0]);uv=me.uv_layers.new(name='CozyAtlasUV')
 for p,coords in zip(me.polygons,mapped):
  for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
 obj.data=me;bpy.data.meshes.remove(old);obj['MasonryUV']='Metre-scaled repeats; horizontal courses; separate paving top'
 assert all(p.normal.dot(p.center)>=-1e-5 for p in me.polygons),'Masonry outward winding: '+obj.name
 uv_audit.append(dict(object=obj.name,slot=slot,repeatMetres=MASONRY_SCALE[slot],polygons=len(faces)))
 return obj

def assign(obj,slot,mi=0,rect=None):
 if slot in MASONRY_SCALE and mi==0 and rect is None:return masonry_uv(obj,slot)
 obj.data.materials.clear();obj.data.materials.append(M[mi]);uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='CozyAtlasUV');uv.name='CozyAtlasUV'
 xyz=[v.co for v in obj.data.vertices];lo=[min(v[i] for v in xyz) for i in range(3)];hi=[max(v[i] for v in xyz) for i in range(3)]
 u0,v0,u1,v1=rect or region(slot)
 # Differing crops avoid identical knots on adjacent timber. No UV crosses its padded slot.
 if slot in ('WoodHoney','WoodDark','WoodCream','WoodFloor','WoodRed'):
  du=(u1-u0)*.18;dv=(v1-v0)*.12;u0+=random.random()*du;u1-=du;v0+=random.random()*dv;v1-=dv
 for p in obj.data.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i])); axes=[i for i in range(3) if i!=axis]
  if (hi[axes[0]]-lo[axes[0]])>(hi[axes[1]]-lo[axes[1]]):axes.reverse()
  for li in p.loop_indices:
   co=xyz[obj.data.loops[li].vertex_index];u=(co[axes[0]]-lo[axes[0]])/max(.0001,hi[axes[0]]-lo[axes[0]]);v=(co[axes[1]]-lo[axes[1]])/max(.0001,hi[axes[1]]-lo[axes[1]])
   uv.data[li].uv=(u0+(u1-u0)*u,v0+(v1-v0)*v)
 return obj
def bevel(obj,w=.045):
 bpy.context.view_layer.objects.active=obj;obj.select_set(True);m=obj.modifiers.new('Soft structural edges','BEVEL');m.width=w;m.segments=1;m.affect='EDGES';bpy.ops.object.modifier_apply(modifier=m.name);obj.select_set(False)
def box(name,c,size,slot='WoodHoney',w=.035,detail=0,mi=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=cv(c));o=bpy.context.object;o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if w:bevel(o,min(w,min(size)*.18))
 assign(o,slot,mi);return link(o,name,detail)
def beam(name,a,b,width=.18,depth=None,slot='WoodHoney',detail=0):
 aa,bb=cv(a),cv(b);delta=bb-aa
 bpy.ops.mesh.primitive_cube_add(size=1,location=(aa+bb)*.5);o=bpy.context.object;o.dimensions=(width,depth or width,delta.length);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bevel(o,min(.035,width*.16));assign(o,slot);o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();return link(o,name,detail)
def mesh(name,verts,faces,slot='WoodHoney',detail=0,mi=0,rect=None):
 me=bpy.data.meshes.new(name);me.from_pydata([cv(v) for v in verts],[],faces);me.update();o=bpy.data.objects.new(name,me);source.objects.link(o);parts.append(o);lodtag[o.name]=detail;o['CozyDetailTier']=detail;assign(o,slot,mi,rect);return o
def cyl(name,c,r,depth,slot='Iron',vertices=10,detail=0):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=cv(c));o=bpy.context.object;assign(o,slot);return link(o,name,detail)
def arch(name,c,width,straight,radius,thick,slot):
 # Closed arch prism in the facade plane. Local vertical plane is authored in Unity metres.
 x,y,z=c;outline=[(-width/2,0),(width/2,0),(width/2,straight)]
 outline += [(radius*math.cos(t),straight+radius*math.sin(t)) for t in [i*math.pi/12 for i in range(1,13)]]
 vv=[(x+u,y+v,z+d) for d in [-thick/2,thick/2] for u,v in outline];n=len(outline)
 ff=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,vv,ff,slot)
def card(name,c,width,height,index,angle=0):
 x,y,z=c;u=Vector((math.cos(angle)*width*.5,0,math.sin(angle)*width*.5));v=Vector((0,height*.5,0));cc=Vector(c)
 col=index%4;row=index//4;pad=.035;rect=(col/4+pad/4,1-(row+1)/4+pad/4,(col+1)/4-pad/4,1-row/4-pad/4)
 o=mesh(name,[tuple(cc-u-v),tuple(cc+u-v),tuple(cc+u+v),tuple(cc-u+v)],[(0,1,2,3)],detail=1,mi=1,rect=rect)
 # Explicit card UV orientation; generic vertical projection is unsuitable for oblique cards.
 for i,uv in enumerate([(rect[0],rect[1]),(rect[2],rect[1]),(rect[2],rect[3]),(rect[0],rect[3])]):o.data.uv_layers.active.data[i].uv=uv
 return o
def plants(c,count=12,spread=.55,flowers=True):
 x,y,z=c
 for i in range(count):
  px=x+random.uniform(-spread,spread);pz=z+random.uniform(-spread*.4,spread*.4);py=y+random.uniform(.05,.45)
  card('Leaf cluster', (px,py,pz),random.uniform(.25,.5),random.uniform(.3,.6),random.randrange(8),random.uniform(-.7,.7))
  if flowers and i%2==0:card('Daisy',(px,py+.15,pz+.06),.24,.24,random.randrange(8,14),random.uniform(-.5,.5))
def pot(name,c,r=.36,h=.65):
 x,y,z=c
 cyl(name,(x,y+h*.5,z),r,h,'Terracotta',10,1);cyl(name+' rim',(x,y+h*.91,z),r*1.10,h*.16,'Terracotta',12,1);cyl(name+' soil',(x,y+h,z),r*.88,.045,'Earth',12,1);plants((x,y+h+.1,z),12,r*.85)

# Ground, body and readable exposed framing.
box('Foundation',(0,.33,-3.75),(9.15,.66,7.6),'StoneWarm',.075)
box('Cream wall body',(0,2.94,-3.75),(8.9,4.48,7.5),'Plaster',.025)
box('Front upper wall closure',(0,5.46,-.08),(8.9,.56,.20),'Plaster',.01)
box('Rear upper wall closure',(0,5.38,-7.42),(8.9,.40,.20),'Plaster',.01)
for x in [-4.45,4.45]:
 outline=[(5.17,0),(5.64,0),(7.88,-3.6),(5.44,-7.5),(5.17,-7.5)]
 vv=[(x+d,y,z) for d in [-.07,.07] for y,z in outline]
 mesh('Enclosed side roof gable',vv,[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],'Plaster')
 beam('Side gable front timber',(x,5.52,0),(x,7.77,-3.6),.20,.20)
 beam('Side gable rear timber',(x,7.77,-3.6),(x,5.35,-7.5),.20,.20)
 beam('Side gable upright',(x,5.2,-3.6),(x,7.77,-3.6),.19,.20)
for x in [-4.35,4.35]:
 for z in [.08,-7.45]:box('Corner timber',(x,2.95,z),(.30,4.7,.30),'WoodHoney',.05)
box('Front lower beam',(0,1.0,.13),(8.9,.24,.27));box('Front header',(0,4.9,.17),(9.2,.34,.35))
for x in [-1.20,1.20]:box('Facade door post',(x,2.95,.14),(.27,4.0,.32))
for z in [-2.7,-5.2]:
 for x in [-4.47,4.47]:box('Side structural post',(x,2.95,z),(.23,4.4,.22))
for x in [-4.48,4.48]:box('Side lower rail',(x,1.0,-3.8),(.2,.21,7.3));box('Side header',(x,4.9,-3.8),(.25,.29,7.5))
box('Rear lower rail',(0,1.0,-7.55),(8.9,.22,.24));box('Rear header',(0,4.9,-7.55),(9.2,.3,.28))

# Arched doorway, wood jambs, inset and warm little arched window.
arch('Door dark recess',(0,.68,.24),2.12,2.84,1.06,.12,'WoodDark')
arch('Door timber leaf',(0,.72,.35),1.77,2.75,.885,.14,'WoodHoney')
for x in [-1.0,1.0]:box('Door jamb',(x,2.1,.44),(.18,2.95,.23),'WoodDark')
for j in range(12):
 t1=j*math.pi/12;t2=(j+1)*math.pi/12
 beam('Arched door frame',(1.0*math.cos(t1),3.51+1.0*math.sin(t1),.46),(1.0*math.cos(t2),3.51+1.0*math.sin(t2),.46),.19,.22,'WoodHoney')
arch('Arched warm door glass',(0,2.85,.44),.72,.40,.36,.04,'GlowWarm')
assign(parts[-1],'GlowWarm',2)
box('Door glass cross vertical',(0,3.20,.49),(.065,.73,.07),'WoodDark',.012)
box('Door glass cross horizontal',(0,3.23,.49),(.75,.065,.07),'WoodDark',.012)
box('Door middle rail',(0,2.48,.49),(1.65,.13,.14),'WoodDark',.02)
for x in [-.56,-.28,0,.28,.56]:box('Door board seam',(x,1.54,.434),(.014,1.52,.018),'WoodDark',0,1)
box('Door lock',(.60,1.86,.55),(.15,.27,.1),'Iron',.025,1);beam('Door pull',(.63,1.75,.63),(.63,2.01,.63),.065,.065,'Iron',1)

def window(x,z=.24,side=False):
 # Facade windows have deep surrounds and separate lit panes.
 box('Window dark inset',(x,3.12,z),(1.92,2.12,.14),'WoodDark',.025)
 box('Window warm glass',(x,3.12,z+.09),(1.58,1.78,.06),'GlowWarm',.01,0,2)
 for dx in [-.91,.91]:box('Window jamb',(x+dx,3.12,z+.16),(.18,2.25,.24),'WoodHoney')
 for dy in [-1.0,1.0]:box('Window horizontal trim',(x,3.12+dy,z+.17),(2.10,.20,.28),'WoodHoney')
 box('Window center mullion',(x,3.12,z+.21),(.085,1.94,.13));box('Window crossbar',(x,3.18,z+.21),(1.88,.085,.13))
 box('Window sill',(x,2.04,z+.25),(2.22,.16,.42),'WoodHoney')
 box('Planter front',(x,1.85,z+.55),(1.99,.34,.14),'WoodDark',.025,1)
 box('Planter base',(x,1.72,z+.37),(2.02,.10,.48),'WoodHoney',.015,1)
 for dx in [-.96,.96]:box('Planter end',(x+dx,1.85,z+.35),(.12,.35,.5),'WoodHoney',.02,1)
 plants((x,2.05,z+.4),24,.92)
window(-3.0);window(3.0)
for x in [-2.4,2.4]:
 box('Rear window surround',(x,3.1,-7.59),(1.5,1.7,.12),'WoodDark');box('Rear glass',(x,3.1,-7.68),(1.25,1.4,.06),'GlowWarm',.01,1,2)
 box('Rear window cross',(x,3.1,-7.73),(.09,1.55,.11));box('Rear window cross',(x,3.1,-7.73),(1.35,.09,.11))

# Porch and the same stair approach/exit as the existing gameplay prefab.
box('Stone porch',(0,.30,1.1),(3.8,.6,2.2),'Paving',.055)
for i in range(3):
 h=.6-.2*i;box('Stone step '+str(i),(0,h/2,2.45+.5*i),(3.45,h,.5),'Paving',.045)
for x in [-1.75,1.75]:
 box('Porch post foot',(x,.83,2.04),(.54,.46,.55),'StoneWarm',.05)
 box('Porch timber pillar',(x,2.9,2.04),(.34,4.12,.34),'WoodHoney',.055)
 beam('Porch diagonal brace',(x,4.15,2.04),(x-math.copysign(.62,x),4.92,2.04),.20,.24)
box('Porch front beam',(0,4.94,2.04),(4.15,.32,.36))

# Roof shingles: small shallow prisms with real exposed lips; large flat surface texture supplies fine relief.
def roof(name,xmin,xmax,zmin,zmax,eave,ridge,along_x=True,porch=False):
 mid=(zmin+zmax)/2 if along_x else (xmin+xmax)/2
 for side in [-1,1]:
  for i in range(4 if along_x else 2):
   columns=4 if along_x else 2
   for j in range(2):
    vv=[]
    for f,t in [(i/columns,j/2),((i+1)/columns,j/2),((i+1)/columns,(j+1)/2),(i/columns,(j+1)/2)]:
     h=eave+(ridge-eave)*t;span=(zmax-zmin)/2 if along_x else (xmax-xmin)/2;axis=mid+side*span*(1-t)
     vv.append((xmin+(xmax-xmin)*f,h,axis) if along_x else (axis,h,zmin+(zmax-zmin)*f))
    # A closed thin skin keeps both slopes outward-facing after FBX export.
    vv+= [(x,y-.035,z) for x,y,z in vv]
    o=mesh(name+' textured underlay',vv,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofOlive',2)
    r=region('RoofOlive')
    for li,uv in zip(o.data.polygons[0].loop_indices,[(r[0],r[1]),(r[2],r[1]),(r[2],r[3]),(r[0],r[3])]):o.data.uv_layers.active.data[li].uv=uv
 # Source panels remain in every LOD; detail tier 0 tiles retain enough thickness at medium distance.
 sides=[-1,1];span=(zmax-zmin)/2 if along_x else (xmax-xmin)/2
 nrows=max(4,math.ceil(math.hypot(span,ridge-eave)/.56));ncols=math.ceil(((xmax-xmin) if along_x else (zmax-zmin))/.64)
 r=region('RoofOlive');single=S['RoofOlive']['singleTileUv']
 for sign in sides:
  for row in range(nrows):
   t0=row/nrows;t1=min(1,(row+1.08)/nrows)
   # Offset rows need one clipped end tile; otherwise half a tile is missing.
   for col in range(ncols+(1 if row%2 else 0)):
    single=S['RoofOlive']['singleTileUvs'][(row*5+col)%len(S['RoofOlive']['singleTileUvs'])]
    # At the main/porch intersection omit hidden shingles under the higher gable.
    f0=col/ncols;f1=(col+1)/ncols;offset=(.5/ncols if row%2 else 0)
    f0=max(0,min(1,f0-offset));f1=max(0,min(1,f1-offset))
    if f1<=f0:continue
    vals=[]
    for f,t in [(f0,t0),(f1,t0),(f1,t1),(f0,t1)]:
     h=eave+(ridge-eave)*t+.045;axis=mid+sign*span*(1-t)
     vals.append((xmin+(xmax-xmin)*f,h,axis) if along_x else (axis,h,zmin+(zmax-zmin)*f))
    if along_x and sign==1:
     cx=(vals[0][0]+vals[1][0])*.5;cz=(vals[0][2]+vals[2][2])*.5;main_y=(vals[0][1]+vals[2][1])*.5
     porch_y=6.97-(6.97-5.20)*abs(cx)/2.30
     if abs(cx)<2.30 and -1.8<cz<2.55 and porch_y>main_y+.06:continue
    vv=vals+[(x,y-.055,z) for x,y,z in vals];o=mesh(name+' shingle',vv,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'RoofOlive',0,rect=single)
    # Mapping the upper face to one shingle avoids repeating an entire tile grid on each physical tile.
    for p in o.data.polygons:
     for j,li in enumerate(p.loop_indices):o.data.uv_layers.active.data[li].uv=[(single[0],single[1]),(single[2],single[1]),(single[2],single[3]),(single[0],single[3])][j%4]
 if along_x:
  for z in [zmin,zmax]:beam(name+' eave fascia',(xmin,eave-.05,z),(xmax,eave-.05,z),.24,.25,'WoodHoney')
  for x in [xmin,xmax]:
   for z in [zmin,zmax]:beam(name+' barge trim',(x,eave,z),(x,ridge,mid),.20,.23,'RoofTrim')
  for k in range(math.ceil((xmax-xmin)/.65)):box(name+' ridge cap',(xmin+(k+.5)*(xmax-xmin)/math.ceil((xmax-xmin)/.65),ridge+.09,mid),(.67,.16,.34),'RoofTrim',.045,0)
 else:
  for x in [xmin,xmax]:beam(name+' edge',(x,eave,zmin),(x,eave,zmax),.21,.26,'RoofTrim')
  for x in [xmin,xmax]:beam(name+' front barge',(x,eave,zmax),(mid,ridge,zmax),.23,.30,'RoofTrim')
  beam(name+' ridge',(mid,ridge+.07,zmin),(mid,ridge+.07,zmax),.25,.31,'RoofTrim')
roof('Main olive roof',-5.05,5.05,-8.0,.80,5.28,8.05,True)
# Cream triangle behind porch gable, thick honey frame and king post.
mesh('Porch cream gable',[(-2.05,4.98,2.25),(2.05,4.98,2.25),(0,6.82,2.25)],[(0,1,2)],'Plaster')
beam('Porch gable left timber',(-2.08,4.98,2.34),(0,6.87,2.34),.30,.28)
beam('Porch gable right timber',(0,6.87,2.34),(2.08,4.98,2.34),.30,.28)
beam('Porch gable king post',(0,5.1,2.35),(0,6.7,2.35),.20,.22)
for sign in [-1,1]:beam('Porch decorative diagonal',(sign*1.18,5.15,2.35),(0,6.0,2.35),.16,.18)
roof('Porch olive roof',-2.30,2.30,-1.8,2.55,5.20,6.97,False,True)

# Right-hand brick chimney with stone cap and genuinely open flue.
box('Brick chimney',(3.15,7.65,-4.5),(.86,2.30,.82),'Brick',.045)
box('Chimney lower stone collar',(3.15,6.85,-4.5),(1.02,.23,.99),'StoneWarm',.04)
box('Chimney top cap',(3.15,8.86,-4.5),(1.08,.25,1.04),'StoneWarm',.05)
for dx in [-.36,.36]:box('Flue wall',(3.15+dx,9.10,-4.5),(.13,.38,.82),'WoodDark',.02)
for dz in [-.35,.35]:box('Flue wall',(3.15,9.10,-4.5+dz),(.62,.38,.13),'WoodDark',.02)
box('Flue black interior',(3.15,8.98,-4.5),(.64,.02,.60),'Iron',0)

# Lantern on the right jamb; warm panes and iron cage share the same surface atlas.
beam('Lantern wall bracket',(1.42,3.94,.26),(1.42,3.94,.77),.085,.085,'Iron',1)
cyl('Lantern cap',(1.42,3.58,.75),.24,.12,'Iron',8,1);box('Lantern warm glass',(1.42,3.27,.75),(.30,.48,.26),'GlowWarm',.01,1,2)
for dx in [-.18,.18]:
 for dz in [-.16,.16]:beam('Lantern corner',(1.42+dx,3.01,.75+dz),(1.42+dx,3.55,.75+dz),.045,.045,'Iron',1)
box('Lantern base',(1.42,3.0,.75),(.43,.10,.37),'Iron',.02,1)
beam('Lantern hanger',(1.42,3.65,.75),(1.42,3.95,.75),.065,.065,'Iron',1)

# Small chair tucked to the left of the door, beside rather than inside the stair route.
for xx in [-1.42,-.78]:
 for zz in [.72,1.35]:box('Porch chair leg',(xx,.99,zz),(.10,.78,.10),'WoodDark',.015,1)
box('Chair seat',(-1.10,1.38,1.04),(.82,.12,.82),'WoodHoney',.035,1)
for xx in [-1.43,-.77]:box('Chair rear upright',(xx,1.78,.72),(.11,1.40,.11),'WoodHoney',.02,1)
for yy in [1.74,1.98,2.22]:box('Chair back slat',(-1.10,yy,.72),(.68,.15,.10),'WoodHoney',.02,1)
pot('Porch flower pot',(1.12,.60,1.25),.29,.58)
for x,z in [(-4.5,.6),(4.6,.35),(-3.9,1.30),(4.2,1.1)]:plants((x,.35,z),22,.66)
for i in range(15):
 y=.6+i*.32;x=4.40+.12*math.sin(i*.7);z=.21
 card('Trailing ivy',(x,y,z),.76,.83,4+i%4,(-.3+i*.11))
 if i%2==0:card('Corner ivy side',(x-.18,y+.12,z+.03),.55,.63,i%4,.65)
 if i%3==0:card('Vine white flower',(x-.12,y+.1,z+.05),.20,.20,8+i%4)

# Normals and deterministic LOD generation. Thin cards remain double-sided through the shared cutout material.
for o in parts:
 # Clipped atlas islands preserve the original box's outward winding. Rebuilding
 # normals per disconnected island can flip an island inward in exported Unity.
 if 'MasonryUV' in o:continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
bpy.context.view_layer.update()
def combine(lod):
 copies=[]
 for original in parts:
  tier=lodtag[original.name]
  if lod==1 and (tier==1 or 'shingle' in original.name):continue
  if lod==2 and (tier==1 or ('shingle' in original.name or 'ridge cap' in original.name)):continue
  cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);copies.append(cp)
 bpy.ops.object.select_all(action='DESELECT')
 for cp in copies:cp.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name='HouseCozy_Lv1_LOD'+str(lod)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # Join remaps common material slots. Remove empty and repeated references before export.
 used=[]
 for m in M:
  if any(o.data.materials[p.material_index]==m for p in o.data.polygons):used.append(m)
 old=[o.data.materials[p.material_index] for p in o.data.polygons];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,old):p.material_index=used.index(m)
 if lod==2:
  d=o.modifiers.new('Distant planar simplification','DECIMATE');d.decimate_type='DISSOLVE';d.angle_limit=.04;bpy.ops.object.modifier_apply(modifier=d.name)
  d=o.modifiers.new('Distant mesh budget','DECIMATE');d.ratio=.42;d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(A/f'mesh/Buildings/HouseCozy/HouseCozy_Lv1_LOD{lod}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 o.hide_render=lod>0;o.hide_set(lod>0)
 tris=sum(len(p.vertices)-2 for p in o.data.polygons)
 return o,dict(lod=lod,triangles=tris,vertices=len(o.data.vertices),materials=[m.name for m in used],boundsMin=[min((o.matrix_world@v.co)[i] for v in o.data.vertices) for i in range(3)],boundsMax=[max((o.matrix_world@v.co)[i] for v in o.data.vertices) for i in range(3)])
combined=[];stats=[]
for lod in range(3):o,s=combine(lod);combined.append(o);stats.append(s)
source.hide_render=True;source.hide_viewport=True
# Authored anchors for future editing; not included in runtime FBX.
markers=bpy.data.collections.new('04 GAMEPLAY ANCHORS - NO EXPORT');scene.collection.children.link(markers)
for name,p in [('Door',(0,0,0)),('StairApproach',(0,0,2.95)),('ExitSpawn',(0,0,4.6)),('Lantern',(1.42,3.3,.75))]:
 o=bpy.data.objects.new(name,None);markers.objects.link(o);o.location=cv(p);o.empty_display_size=.2

def studio_obj(o):
 for c in list(o.users_collection):c.objects.unlink(o)
 studio.objects.link(o)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Preview floor';studio_obj(floor);fm=bpy.data.materials.new('Preview cream floor');fm.diffuse_color=(.78,.73,.61,1);floor.data.materials.append(fm)
for name,pos,power,size in [('Key',(-7,-10,15),2200,8),('Fill',(9,-3,11),1300,10),('Rim',(0,8,14),1800,8)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,-2,3))-o.location).to_track_quat('-Z','Y').to_euler();studio_obj(o)
scene.world=bpy.data.worlds.new('Cozy preview environment');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.5,.5,.5,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.ops.object.camera_add(location=(13,-23,14));cam=bpy.context.object;studio_obj(cam);cam.data.type='ORTHO';cam.data.ortho_scale=16;scene.camera=cam
target=Vector((0,1.9,3.8));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT');combined[0].select_set(True);bpy.context.view_layer.objects.active=combined[0]
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=18;area.spaces.active.region_3d.view_location=target
bpy.ops.wm.save_as_mainfile(filepath=str(A/'Source~/HouseCozy/House_Cozy_Master.blend'))
(OUT/'Previews/BlenderManifest.json').write_text(json.dumps(dict(status='BUILT',units='Blender Z up; front -Y; Unity front +Z after import normalization',source=str(A/'Source~/HouseCozy/House_Cozy_Master.blend'),sourceModules=len(parts),lods=stats,atlasShared=True,roofPattern='Rectangular staggered shingles; one complete tile per physical slab',masonryUV=uv_audit),indent=2))
for label,position in ([] if '--no-preview' in __import__('sys').argv else [('Front',(0,-25,12)),('FrontRight',(14,-23,14)),('RearLeft',(-15,17,12))]):
 cam.location=position;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/f'Previews/HouseCozy_{label}.png');bpy.ops.render.render(write_still=True)
print('HOUSE_COZY_BUILD_PASS',json.dumps(stats),flush=True)
