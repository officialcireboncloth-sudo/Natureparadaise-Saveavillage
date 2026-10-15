"""Build approved Chicken Store A in Blender. LIVE_BUILD preserves existing scenes."""
import bpy,bmesh,math,json,hashlib,random,sys,ast,shutil
from pathlib import Path
from mathutils import Vector,Matrix
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
LIVE=globals().get('LIVE_BUILD',False)
if not LIVE:bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
random.seed(1410)
AUDIT=json.loads((ART/'References/atlas-audit.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256((ROOT/AUDIT['sourcePSD']).read_bytes()).hexdigest()==AUDIT['sourceSha256']
BASE={'materials':{'atlasGuid':AUDIT['sourceGUID']}}
PAL={k:{'uvSafeRectBottomLeft':v['safeUvBottomLeft']} for k,v in AUDIT['swatches'].items()}
atlas=bpy.data.images.load(str(ART/'References/TFP_Atlas_1A_BlenderPreview.png'));atlas.pack()
atlas['UnityGUID']=AUDIT['sourceGUID'];atlas['OriginalPSD']=AUDIT['sourcePSD']
mat=bpy.data.materials.get('TFP_Atlas_1A') or bpy.data.materials.new('TFP_Atlas_1A');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88;bs.inputs['Specular IOR Level'].default_value=.05
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;tx.interpolation='Linear';tx.extension='EXTEND'
mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color']);mat['UnityMaterial']='TFP_Atlas_1A.mat'
# Freeze the proven mesh/export helpers locally; importing the farm builder would execute it.
old=(ROOT/'ArtSource/FarmBuildingsFence_A/Scripts/build_farm.py').read_text(encoding='utf-8')
names={'coll','setup','activate','Build','empty','join_copy','export','studio','render'}
common='\n\n'.join(ast.get_source_segment(old,n) for n in ast.parse(old).body if isinstance(n,(ast.FunctionDef,ast.ClassDef)) and n.name in names)
common=common.replace("scene['Approval']='User approved sketch 2026-10-13'","scene['Approval']='User approved Chicken Store sketch 2026-10-14'")
common=common.replace('bpy.ops.object.modifier_apply(modifier=mod.name);ob.select_set(False)','bpy.ops.object.modifier_apply(modifier=mod.name);ob.select_set(False);me=ob.data')
(ART/'Scripts/mesh_common.py').write_text(common,encoding='utf-8')
exec(compile(common,str(ART/'Scripts/mesh_common.py'),'exec'),globals())

def clean(obj,triangulate=False):
 bm=bmesh.new();bm.from_mesh(obj.data)
 bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if triangulate or len(f.verts)>4])
 bad=[f for f in bm.faces if len(f.verts)==3 and (f.verts[1].co-f.verts[0].co).cross(f.verts[2].co-f.verts[0].co).length<2e-10]
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
def clip(poly,condition):
 if not poly:return []
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  va,vb=condition(*a),condition(*b);ia,ib=va>=-1e-8,vb>=-1e-8
  if ia:out.append(a)
  if ia!=ib:
   t=va/(va-vb);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
 result=[]
 for p in out:
  if not result or math.dist(p,result[-1])>1e-7:result.append(p)
 if len(result)>1 and math.dist(result[0],result[-1])<1e-7:result.pop()
 return result if len(result)>=3 else []
def subtract(poly,conditions):
 remaining=poly;outside=[]
 for f in conditions:
  part=clip(remaining,lambda x,y,f=f:-f(x,y))
  if part:outside.append(part)
  remaining=clip(remaining,f)
  if not remaining:break
 return outside
def roof_prism(builder,poly,h,color,thickness=.035,offset=0):
 if len(poly)<3:return
 area=abs(sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(poly,poly[1:]+poly[:1])))/2
 if area<1e-8:return
 n=len(poly);vs=[(x,y,h(x,y)+offset+dz) for dz in (-thickness,0) for x,y in poly]
 builder.solid(vs,[tuple(range(n)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],color)
def make_roofs(tile,base,planes):
 # Subtract the exact footprint of higher roof planes from each surface.
 # No shingle or base plane remains hidden/coincident inside the intersecting gables.
 for i,p in enumerate(planes):
  xmin,xmax,ymin,ymax=p['rect'];h=p['height']
  def visible(poly):
   parts=[poly]
   for j,q in enumerate(planes):
    if i==j:continue
    a,b,c,d=q['rect'];oh=q['height']
    conditions=[lambda x,y,a=a:x-a,lambda x,y,b=b:b-x,lambda x,y,c=c:y-c,lambda x,y,d=d:d-y,lambda x,y,oh=oh,h=h:oh(x,y)-h(x,y)-1e-6]
    parts=[piece for part in parts for piece in subtract(part,conditions)]
   return parts
  for poly in visible([(xmin,ymin),(xmax,ymin),(xmax,ymax),(xmin,ymax)]):roof_prism(base,poly,h,'wood_shadow',.08,-.038)
  # Stagger courses with alternating half-tiles along the ridge direction.
  along_x=p['ridge']=='X';lo,hi=(xmin,xmax) if along_x else (ymin,ymax);rlo,rhi=(ymin,ymax) if along_x else (xmin,xmax)
  rows=max(1,math.ceil((rhi-rlo)/.34));pitch=(rhi-rlo)/rows;tile_w=.48
  for row in range(rows):
   aa=rlo+row*pitch+.004;bb=rlo+(row+1)*pitch-.004;shift=(row%2)*tile_w/2
   start=lo-shift
   while start<hi:
    a=max(lo,start)+.004;b=min(hi,start+tile_w)-.004;start+=tile_w
    if b-a<.02:continue
    poly=[(a,aa),(b,aa),(b,bb),(a,bb)] if along_x else [(aa,a),(bb,a),(bb,b),(aa,b)]
    for piece in visible(poly):roof_prism(tile,piece,h,'roof',.032)

def wall(builder,a,b,y,zmin,top,color,holes=(),planks=False):
 cuts={a,b}
 # Resolve sloping gables and the shoulders of the raised central cap.
 cuts.update(x for x in [-1.6,0,1.6] if a<x<b)
 if planks:
  n=math.ceil((b-a)/.24);cuts.update(a+(b-a)*i/n for i in range(1,n))
 for x,w,z,h in holes:cuts.update([max(a,x-w/2),min(b,x+w/2)])
 cuts=sorted(cuts)
 for lo,hi in zip(cuts,cuts[1:]):
  if hi-lo<.01:continue
  tlo=top(lo+1e-7);thi=top(hi-1e-7)
  intervals=[(zmin,max(tlo,thi))]
  for x,w,z,h in holes:
   if x-w/2<(lo+hi)/2<x+w/2:intervals=[p for aa,bb in intervals for p in [(aa,min(bb,z)),(max(aa,z+h),bb)] if p[1]>p[0]+.01]
  for aa,bb in intervals:
   gap=.004 if planks else 0
   if min(bb,tlo)<=aa or min(bb,thi)<=aa:continue
   builder.extrude([(lo+gap,aa),(hi-gap,aa),(hi-gap,min(bb,thi)),(lo+gap,min(bb,tlo))],y,.10,color)
def sidewall(builder,x,d,profile,color,planks=False):
 b=Build();wall(b,-d/2,d/2,0,.25 if d==6 else .15,profile,color,planks=planks)
 # Swap local horizontal u into world Y, with thickness in world X.
 for v in b.v:builder.v.append(Vector((x+v.y,v.x,v.z)))
 off=len(builder.v)-len(b.v);builder.f.extend(tuple(off+i for i in face) for face in b.f);builder.col.extend(b.col)
def rect_frame(b,x,y,z,w,h,color='timber',depth=.16):
 for xx in [x-w/2-.055,x+w/2+.055]:b.box((xx,y,z),(.11,depth,h),color)
 for zz in [z-h/2-.055,z+h/2+.055]:b.box((x,y,zz),(w+.22,depth,.11),color)
def window(b,t,x,y,z,w,h,shutter=False):
 b.box((x,y+.04,z),(w,.06,h),'wood_shadow' if shutter else 'window');rect_frame(t,x,y-.025,z,w,h)
 if shutter:
  for zz in [z-.27*h,z,z+.27*h]:t.box((x,y-.04,zz),(w,.06,.09),'timber')
 else:
  for xx in [x-w/4,x,x+w/4]:t.box((xx,y-.045,z),(.045,.09,h),'timber')
  for zz in [z-h/6,z+h/6]:t.box((x,y-.05,zz),(w,.09,.045),'timber')
def bird(builder,x,y,z,size,color):
 outline=[(-.35,.02),(-.42,.20),(-.50,.35),(-.31,.30),(-.40,.48),(-.18,.36),(-.10,.29),(.06,.30),(.13,.47),(.14,.58),(.22,.64),(.30,.60),(.32,.53),(.43,.50),(.32,.45),(.29,.32),(.23,.16),(.12,.06),(.06,.03),(.04,-.13),(.15,-.14),(.15,-.18),(-.02,-.18),(-.04,0),(-.13,0),(-.16,-.12),(-.07,-.13),(-.07,-.17),(-.25,-.17),(-.22,.03)]
 builder.extrude([(x+a*size,z+b*size) for a,b in outline],y,.025,color)
def text_mesh(col,name,body,loc,size,color,max_width):
 curve=bpy.data.curves.new(name,'FONT');curve.body=body;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=size;curve.extrude=.002;curve.resolution_u=2
 font=Path('C:/Windows/Fonts/georgiab.ttf')
 if font.exists():curve.font=bpy.data.fonts.load(str(font))
 ob=bpy.data.objects.new(name,curve);col.objects.link(ob);ob.location=loc;ob.rotation_euler=(math.pi/2,0,0)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH');ob=bpy.context.object
 bpy.context.view_layer.update()
 if ob.dimensions.x>max_width:ob.scale*=max_width/ob.dimensions.x
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);clean(ob,True)
 ob.data.materials.append(mat)
 while ob.data.uv_layers:ob.data.uv_layers.remove(ob.data.uv_layers[0])
 uv=ob.data.uv_layers.new(name='ToonAtlasUV');a,b,c,d=PAL[color]['uvSafeRectBottomLeft']
 for loop in uv.data:loop.uv=((a+c)/2,(b+d)/2)
 ob.select_set(False);return ob
def lantern(m,g,x,y,z):
 m.box((x,y+.12,z+.14),(.10,.20,.12),'metal')
 for zz,ww in [(z-.17,.17),(z+.15,.20)]:m.box((x,y,zz),(ww,.15,.055),'metal')
 g.box((x,y,z),(.12,.09,.25),'lantern')
 for xx in [x-.075,x+.075]:m.box((xx,y-.035,z),(.025,.10,.34),'metal')
 m.extrude([(x-.14,z+.18),(x+.14,z+.18),(x+.045,z+.32),(x-.045,z+.32)],y,.18,'metal')
def foundation(b,w,d,h):
 for yy in [-d/2,d/2]:
  n=math.ceil(w/.62)
  for i in range(n):b.box((-w/2+(i+.5)*w/n,yy,h/2),(w/n-.012,.18,h),'foundation')
 for xx in [-w/2,w/2]:
  n=math.ceil(d/.65)
  for i in range(n):b.box((xx,-d/2+(i+.5)*d/n,h/2),(.18,d/n-.012,h),'foundation')

assets=[]
def finalize(name,scene,source,groups,extra=(),movable=None):
 obs=[]
 for key,builder in groups.items():
  if not builder.f:continue
  o=builder.obj(name+'_'+key,source,.012 if key in {'Foundation','DoorLeaf','Counter','Frames'} else 0);clean(o);obs.append(o)
 obs+=list(extra)
 ec=coll('02 EXPORT '+name,scene);marker=coll('03 SOCKETS '+name,scene)
 moving=[o for o in obs if movable and o.name.endswith('_'+movable)]
 static=[o for o in obs if o not in moving]
 merged=join_copy(static,name+'_Body',ec);ex=[merged]
 if moving:
  pivot=(-.31,-3.07,.19);hinge=empty(name+'_DoorHinge',pivot,source)
  for o in moving:o.parent=hinge;o.matrix_parent_inverse=Matrix.Translation(-Vector(pivot))
  eh=empty(name+'_DoorHinge_Export',pivot,ec);ex.append(eh)
  for o in moving:
   cp=o.copy();cp.data=o.data.copy();ec.objects.link(cp);cp.parent=eh;cp.matrix_parent_inverse=Matrix.Translation(-Vector(pivot));clean(cp,True);ex.append(cp)
 bpy.context.view_layer.update();export(ex,name);ec.hide_render=True;ec.hide_viewport=True
 empty(name+'_Ground',(0,0,0),marker);empty(name+'_Entrance',(0,-3.85 if name=='ChickenStore_A' else -2.2,0),marker)
 cam=studio(scene);assets.append((name,scene,obs,ex,cam));return obs

sc=bpy.data.scenes.new('01 Chicken Store - Editable');setup(sc);activate(sc);source=coll('01 EDITABLE Store_Main',sc)
G={k:Build() for k in ['Plaster','Foundation','RoofTiles','RoofBase','Timber','Frames','Counter','DoorLeaf','Windows','Hardware','Awning','Sign','Logo']}
W,F,R,RB,T,FR,C,D,WIN,M,A,S,L=[G[k] for k in G]
front=-3
wall(W,-4,4,front,.25,lambda x:3.0,'plaster',[(.35,1.4,.15,2.45),(-2.25,2.8,1,1.7),(2.65,1.6,1,1.65)])
wall(W,-4,4,3,.25,lambda x:3.,'plaster',[(0,.9,.15,2.05),(2.2,1,1.1,1)])
for x in [-4,4]:sidewall(W,x,6,lambda y:4.7-(1.6/3)*abs(y)-.10,'plaster')
foundation(F,8,6,.25)
F.box((.35,-3.25,.075),(1.72,.55,.15),'foundation')
for x in [-3.95,-3.72,-.80,1.18,1.73,3.59,3.95]:T.box((x,-3.05,1.61),(.16,.16,2.72),'timber')
for y in [-3,3]:T.box((0,y,2.90),(8,.18,.18),'timber')
for x in [-4,4]:
 for y in [-3,0,3]:T.box((x,y,1.61),(.18,.18,2.72),'timber')
 T.extrude([(-3,3.0),(0,4.60),(3,3.0),(3,2.85),(0,4.45),(-3,2.85)],x,.16,'timber') if False else None
 # Side gable fascia follows Y/Z; do not substitute front-facing gables.
 for ya,yb in [(-3.4,0),(0,3.4)]:
  za=4.7-(1.6/3)*abs(ya);zb=4.7-(1.6/3)*abs(yb)
  T.beam((x+math.copysign(.40,x),ya,za-.08),(x+math.copysign(.40,x),yb,zb-.08),.16,.13,'timber')
 T.box((x,0,2.92),(.18,6,.16),'timber')
# Raised front gable wall, tiled roof intersections clipped analytically.
def facade_top(x):return max(5.05-.6875*abs(x),5.6-.71875*abs(x) if abs(x)<=1.6 else -100)
wall(W,-2.4,2.4,-3.045,3.,lambda x:facade_top(x)-.10,'plaster')
wall(W,-1.6,1.6,-.70,4.30,lambda x:5.6-.71875*abs(x)-.10,'plaster')
# Close both raised-gable cheeks above the lower roof, including the valley transition.
for xx in [-1.6,1.6]:
 for ya,yb in [(-3.08,-1.40625),(-1.40625,-.8),(-.8,-.7)]:
  lower=lambda y:max(3.95 if y<=-.8 else -100,4.7+(1.6/3)*y)-.04
  cheek=Build();cheek.extrude([(ya,lower(ya)),(yb,lower(yb)),(yb,4.35),(ya,4.35)],0,.10,'plaster')
  off=len(W.v);W.v.extend(Vector((xx+v.y,v.x,v.z)) for v in cheek.v);W.f.extend(tuple(off+i for i in f) for f in cheek.f);W.col.extend(cheek.col)
planes=[{'rect':(-4.4,4.4,-3.4,0),'height':lambda x,y:4.7+(1.6/3)*y,'ridge':'X'},
 {'rect':(-4.4,4.4,0,3.4),'height':lambda x,y:4.7-(1.6/3)*y,'ridge':'X'}]
for extent,peak,slope,ya,yb in [(2.5,5.05,.6875,-3.46,-.80),(1.70,5.6,.71875,-3.10,-.70)]:
 for lo,hi,sg in [(-extent,0,1),(0,extent,-1)]:planes.append({'rect':(lo,hi,ya,yb),'height':lambda x,y,peak=peak,slope=slope,sg=sg:peak+sg*slope*x,'ridge':'Y'})
make_roofs(R,RB,planes)
for extent,peak,slope,yy in [(2.5,5.05,.6875,-3.48),(1.7,5.6,.71875,-3.12)]:
 for lo,hi in [(-extent,0),(0,extent)]:T.extrude([(lo,peak-slope*abs(lo)),(hi,peak-slope*abs(hi)),(hi,peak-slope*abs(hi)-.18),(lo,peak-slope*abs(lo)-.18)],yy,.14,'timber')
T.box((0,-3.115,4.80),(.17,.14,1.28),'timber')
for x in [-1.35,1.35]:T.beam((x,-3.12,4.44),(0,-3.12,5.41),.11,.09,'timber')
for x in [-1.45,1.45]:
 for row in range(4):
  z=4.18+(row+.5)*.24
  F.box((x,.30,z),(.40,.45,.228),'foundation')
  if row%2==0:M.box((x,.067,z),(.012,.012,.228),'wood_shadow')
 F.box((x,.3,5.21),(.5,.55,.18),'foundation')
# Counter and striped fabric. The strip material colors use the same atlas.
for i in range(10):C.box((-3.65+(i+.5)*.28,-3.12,.61),(.27,.18,.78),'timber')
C.box((-2.25,-3.30,1.02),(2.92,.65,.12),'timber')
WIN.box((-2.25,-2.86,1.82),(2.8,.04,1.60),'wood_shadow');rect_frame(FR,-2.25,-3.05,1.85,2.8,1.7)
for i in range(10):
 x0=-3.7+i*.29;x1=x0+.29;color='awning_red' if i%2==0 else 'awning_ivory'
 poly=[(x0,-3),(x1,-3),(x1,-4.2),(x0,-4.2)]
 roof_prism(A,poly,lambda x,y:2.9+.25*(y+3),color,.027)
 # One continuous scallop face per strip, no texture pattern required.
 curve=[(x0,2.6),(x1,2.6),(x1,2.48)]+[(x0+.145+.145*math.cos(a),2.48+.145*math.sin(a)) for a in [0,-math.pi/4,-math.pi/2,-3*math.pi/4,-math.pi]]
 A.extrude(curve,-4.20,.025,color)
for x in [-3.7,-.8]:T.beam((x,-3.01,2.42),(x,-4.12,2.59),.09,.09,'timber')
window(WIN,FR,2.65,-3.055,1.825,1.6,1.65)
# Closed main leaf, nested under a real hinge at finalization.
for i in range(6):D.box((-.31+(i+.5)*.22,-3.07,.79),(.212,.12,1.2),'timber')
D.box((.35,-3.07,2.08),(1.32,.12,.96),'timber');D.box((.35,-3.142,2.07),(1.06,.024,.73),'window')
for x in [-.20,.35,.90]:D.box((x,-3.16,2.07),(.055,.045,.82),'timber')
for z in [1.67,2.07,2.47]:D.box((.35,-3.16,z),(1.16,.045,.055),'timber')
D.box((-.14,-3.19,1.20),(.06,.07,.26),'metal');rect_frame(FR,.35,-3.095,1.375,1.4,2.45)
window(WIN,FR,2.2,3.07,1.6,1,1)
WIN.box((0,3.06,1.175),(.9,.08,2.05),'timber');rect_frame(FR,0,3.10,1.175,.9,2.05)
for x in [-4.06,4.06]:
 WIN.box((x,.25,1.8),(.08,1,1),'wood_shadow' if x<0 else 'window')
 for y in [-.30,.80]:FR.box((x,y,1.8),(.15,.11,1.22),'timber')
 for z in [1.25,2.35]:FR.box((x,.25,z),(.15,1.22,.11),'timber')
 if x<0:
  for y in [-.05,.25,.55]:FR.box((x-.015,y,1.8),(.13,.08,1),'timber')
# Broad front sign and separate arched crest with chicken relief.
S.box((0,-3.63,3.43),(4.4,.14,.75),'plaster')
for z in [3.06,3.80]:FR.box((0,-3.71,z),(4.48,.06,.07),'wood_shadow')
for x in [-2.21,2.21]:FR.box((x,-3.71,3.43),(.07,.06,.80),'wood_shadow')
arch=[(-.70,3.80),(.70,3.80)]+[(.70*math.cos(a),3.84+.70*math.sin(a)) for a in [0,math.pi/8,math.pi/4,3*math.pi/8,math.pi/2,5*math.pi/8,3*math.pi/4,7*math.pi/8,math.pi]]
S.extrude(arch,-3.63,.12,'plaster');bird(L,0,-3.72,4.02,.67,'metal')
for x in [-.85,1.27,3.75]:lantern(M,WIN,x,-3.30,2.30)
for x in [-1.25,1.25]:lantern(M,WIN,x,-3.72,4.03)
letters=text_mesh(source,'ChickenStore_A_SignLetters','LIVE CHICKEN STORE',(0,-3.712,3.43),.31,'metal',3.96)
finalize('ChickenStore_A',sc,source,G,[letters],movable='DoorLeaf')

co=bpy.data.scenes.new('02 Coop Companion - Editable');setup(co);activate(co);source=coll('01 EDITABLE Coop_Companion',co)
G={k:Build() for k in ['Boards','Foundation','RoofTiles','RoofBase','Timber','Frames','Windows','NestBoxes','Ramp','Hardware']}
W,F,R,RB,T,FR,WIN,N,RA,M=[G[k] for k in G]
profile=lambda x:3.1-.9*abs(x)-.10
wall(W,-1.5,1.5,-1.4,.15,profile,'awning_red',[(0,.65,.12,.95),(-.9,.68,.51,.60),(.9,.68,.51,.60),(0,.40,2.06,.45)],True)
wall(W,-1.5,1.5,1.4,.15,profile,'awning_red',[(0,.75,.15,1)],True)
for x in [-1.5,1.5]:sidewall(W,x,2.8,lambda y:1.65,'awning_red',True)
foundation(F,3,2.8,.15)
for x in [-1.44,1.44]:
 for y in [-1.4,1.4]:T.box((x,y,.92),(.16,.16,1.54),'plaster')
for y in [-1.44,1.44]:T.box((0,y,1.59),(3,.16,.16),'plaster')
planes=[{'rect':(-1.75,0,-1.65,1.65),'height':lambda x,y:3.1+.9*x,'ridge':'Y'},{'rect':(0,1.75,-1.65,1.65),'height':lambda x,y:3.1-.9*x,'ridge':'Y'}]
make_roofs(R,RB,planes)
for y in [-1.67,1.67]:
 for a,b in [(-1.75,0),(0,1.75)]:T.extrude([(a,3.1-.9*abs(a)),(b,3.1-.9*abs(b)),(b,2.94-.9*abs(b)),(a,2.94-.9*abs(a))],y,.14,'plaster')
WIN.box((0,-1.1,.595),(.65,.06,.95),'wood_shadow');rect_frame(FR,0,-1.46,.595,.65,.95,'plaster')
for x in [-.9,.9]:
 WIN.box((x,-1.12,.81),(.68,.06,.60),'wood_shadow')
 for xx in [x-.36,x+.36]:N.box((xx,-1.56,.81),(.07,.35,.68),'timber')
 N.box((x,-1.56,.48),(.78,.38,.07),'timber');N.box((x,-1.56,1.14),(.78,.35,.07),'timber')
 roof_prism(N,[(x-.40,-1.43),(x+.40,-1.43),(x+.40,-1.80),(x-.40,-1.80)],lambda xx,y:1.34+.34*(y+1.43),'timber',.05)
# Vent: a genuine arched frame built from short straight segments.
arch=[(-.2,2.06),(.2,2.06),(.2,2.31)]+[(.2*math.cos(a),2.31+.2*math.sin(a)) for a in [0,math.pi/6,math.pi/3,math.pi/2,2*math.pi/3,5*math.pi/6,math.pi]]
WIN.extrude(arch,-1.36,.025,'wood_shadow')
outline=[(-.25,2.02),(.25,2.02),(.25,2.31)]+[(.25*math.cos(a),2.31+.25*math.sin(a)) for a in [0,math.pi/6,math.pi/3,math.pi/2,2*math.pi/3,5*math.pi/6,math.pi]]
for (x,z),(xx,zz) in zip(outline,outline[1:]+outline[:1]):FR.beam((x,-1.48,z),(xx,-1.48,zz),.08,.10,'plaster')
for z in [2.13,2.24,2.35]:FR.box((0,-1.45,z),(.38,.07,.055),'timber')
# Solid wedge meets ground at its toe, avoiding below-ground export geometry.
RA.solid([(-.325,-2.1,0),(.325,-2.1,0),(-.325,-1.4,0),(.325,-1.4,0),(-.325,-1.4,.12),(.325,-1.4,.12)],[(0,1,3,2),(2,3,5,4),(0,2,4),(1,5,3),(0,4,5,1)],'timber')
for y in [-1.52,-1.68,-1.84,-2.0]:RA.box((0,y,.12*(y+2.1)/.7+.017),(.65,.045,.035),'wood_shadow')
WIN.box((0,1.46,.65),(.75,.10,1),'timber');rect_frame(FR,0,1.49,.65,.75,1,'plaster')
for x in [-1.56,1.56]:
 WIN.box((x,.2,1.35),(.08,.35,.30),'wood_shadow')
 for y in [-.03,.43]:FR.box((x,y,1.35),(.14,.07,.42),'plaster')
 for z in [1.16,1.54]:FR.box((x,.2,z),(.14,.52,.07),'plaster')
finalize('ChickenCoop_Companion_A',co,source,G)
# Optional weather vane is independent, never merged into the 3.10 m base building.
optional=coll('04 OPTIONAL Weather vane - removable',co);b=Build();b.box((0,0,3.18),(.025,.025,.20),'metal');bird(b,0,0,3.27,.27,'metal')
vane=b.obj('Coop_Optional_WeatherVane',optional);clean(vane,True);export([vane],'ChickenCoop_WeatherVane_Optional')

def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
ov=bpy.data.scenes.new('00 Chicken Store + Coop Overview');setup(ov);activate(ov);display=coll('01 DISPLAY linked assets',ov)
for name,scene,obs,ex,cam in assets:
 offset=Vector((-2.25,0,0)) if name=='ChickenStore_A' else Vector((4.45,-1.3,0))
 # Bake the evaluated source hierarchy into mesh copies; live hinges remain in editable/export scenes.
 for o in obs:
  cp=o.copy();cp.data=o.data;display.objects.link(cp);cp.parent=None;cp.matrix_parent_inverse=Matrix.Identity(4);cp.matrix_world=Matrix.Translation(offset)@world(o)
cp=vane.copy();display.objects.link(cp);cp.location+=Vector((4.45,-1.3,0));cp['Optional']=True
cam=studio(ov);cam.location=(13,-20,12);cam.rotation_euler=(Vector((0,0,2.2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=17
for area in bpy.context.screen.areas:
 if area.type in {'VIEW_3D','CONSOLE'}:
  area.type='VIEW_3D';sp=area.spaces.active;sp.region_3d.view_location=(0,-.2,2.1);sp.region_3d.view_distance=19
  sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.shading.type='MATERIAL';sp.overlay.show_overlays=False
def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
def stats(obs):
 points=[world(o)@v.co for o in obs for v in o.data.vertices]
 return {'triangles':sum(len(p.vertices)-2 for o in obs for p in o.data.polygons),'bounds':[[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]]}
manifest={'status':'BUILT','atlasGUID':AUDIT['sourceGUID'],'atlasSHA256':AUDIT['sourceSha256'],'newRuntimeTextures':0,'assets':[]}
for name,scene,obs,ex,cam in assets:
 record=stats([o for o in ex if o.type=='MESH']);record.update(name=name,export=name+'.fbx',editableScene=scene.name)
 expected=5.6 if name=='ChickenStore_A' else 3.1
 assert abs(record['bounds'][1][2]-expected)<.02,(name,record)
 manifest['assets'].append(record)
manifest['optionalOrnament']=stats([vane]);manifest['optionalOrnament']['export']='ChickenCoop_WeatherVane_Optional.fbx'
(ART/'Previews/MeshManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
assert hashlib.sha256((ROOT/AUDIT['sourcePSD']).read_bytes()).hexdigest()==AUDIT['sourceSha256']
# Retain the user's original scene when a background refinement follows the live build.
saved=ART/'Blender/ChickenStore_A.blend'
if not LIVE and saved.exists():
 snapshot=ART/'References/OriginalSceneSnapshot.blend'
 shutil.copyfile(saved,snapshot)
 with bpy.data.libraries.load(str(snapshot),link=False) as (data_from,data_to):
  name='Original Scene - preserved' if 'Original Scene - preserved' in data_from.scenes else 'Scene'
  data_to.scenes=[name] if name in data_from.scenes else []
 for original in data_to.scenes:
  if original:original.name='Original Scene - preserved'
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/ChickenStore_A.blend'))
print('CHICKEN_STORE_BUILD_COMPLETE '+json.dumps(manifest))
