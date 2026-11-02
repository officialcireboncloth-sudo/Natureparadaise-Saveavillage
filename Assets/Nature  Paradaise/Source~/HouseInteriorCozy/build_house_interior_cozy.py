"""Cozy Lv1 interior, authored in the existing room's canonical Unity metres."""
from pathlib import Path
import contextlib
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
library=(ROOT/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py').read_text().split('\nfor name,builder in ')[0]
library=library.replace('PoultryCozy','HouseInteriorCozy')
exec(compile(library,__file__,'exec'),globals())
from mathutils import Matrix
begin('House Interior Cozy Lv1')
MASONRY_SCALE['Plaster']=(1.8,1.6)
master_source=source;groups={}

@contextlib.contextmanager
def module(name):
 global source
 previous=source;source=bpy.data.collections.new(name+' - EDITABLE');master_source.children.link(source);start=len(parts)
 yield
 groups[name]=parts[start:];source=previous

def wood(name,c,size,slot='WoodDark',detail=0):return box(name,c,size,slot,.028,detail)
def softbox(name,c,size,slot):
 bpy.ops.mesh.primitive_cube_add(size=1,location=cv(c));o=bpy.context.object;o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 m=o.modifiers.new('Soft textile corners','BEVEL');m.width=min(size)*.30;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
 assign(o,slot)
 for p in o.data.polygons:p.use_smooth=True
 return link(o,name)

def cloth(name,xa,xb,za,zb,y,slot,drop=.0):
 nx,nz=10,18;vv=[]
 for j in range(nz+1):
  z=za+(zb-za)*j/nz
  for i in range(nx+1):
   x=xa+(xb-xa)*i/nx;u=i/nx;v=j/nz
   wave=.012*math.sin(v*19+u*3)+.008*math.sin(u*13-v*7)
   fold=.023*math.exp(-((v-.18)/.06)**2)*math.cos(u*5)
   edge=drop*(abs(2*u-1)**10+.55*(1-v)**12)
   surface=max(y+wave+fold,y-.018) if 'blanket' in name else y+wave+fold
   vv.append((x,surface-edge,z))
 ff=[]
 for j in range(nz):
  for i in range(nx):
   k=j*(nx+1)+i;ff.append((k,k+1,k+nx+2,k+nx+1))
 o=mesh(name,vv,ff,slot);r=region(slot)
 # Stay well inside each textile swatch and repeat fine weave per cloth panel.
 du=(r[2]-r[0])*.20;dv=(r[3]-r[1])*.23;r=(r[0]+du,r[1]+dv,r[2]-du,r[3]-dv)
 for p in o.data.polygons:
  p.use_smooth=True
  for li,uv in zip(p.loop_indices,[(r[0],r[1]),(r[2],r[1]),(r[2],r[3]),(r[0],r[3])]):o.data.uv_layers.active.data[li].uv=uv
 m=o.modifiers.new('Fabric thickness','SOLIDIFY');m.thickness=.012;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 return o

def horizontal_text(name,text,c,size,slot='WoodHoney'):
 curve=bpy.data.curves.new(name,'FONT');curve.body=text;curve.align_x='CENTER';curve.align_y='CENTER';curve.size=size;curve.extrude=.0005;curve.resolution_u=2
 o=bpy.data.objects.new(name,curve);source.objects.link(o);o.location=cv(c)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o.scale.y=-1;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);assign(o,slot);return link(o,name)

def compact_plant(c,count,spread,flowers,scale):
 start=len(parts);plants(c,count,spread/scale,flowers)
 pivot=cv(c);matrix=Matrix.Translation(pivot)@Matrix.Scale(scale,4)@Matrix.Translation(-pivot)
 for o in parts[start:]:o.matrix_world=matrix@o.matrix_world

def table_bouquet():
 for dx,dz,hh in [(-.07,-.03,1.20),(.01,.045,1.27),(.08,-.02,1.22),(-.025,-.08,1.25),(.05,.09,1.19)]:
  x,z=4.05+dx,2.31+dz;beam('Flower green stem',(4.05,.98,2.31),(x,hh,z),.009,.009,'LeafGreen')
  for k in range(5):
   a=k*math.tau/5;cc=Vector((x+math.cos(a)*.028,hh,z+math.sin(a)*.028));poly=[]
   for j in range(6):
    b=j*math.tau/6;along=.040*math.cos(b);across=.018*math.sin(b)
    poly.append((cc.x+along*math.cos(a)-across*math.sin(a),hh+.007*math.cos(b),cc.z+along*math.sin(a)+across*math.cos(a)))
   vv=poly+[(x,y-.008,z) for x,y,z in poly]
   mesh('White daisy petal',vv,[tuple(range(6)),tuple(range(11,5,-1))]+[(j,(j+1)%6,(j+1)%6+6,j+6) for j in range(6)],'FlowerWhite')
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.018,location=cv((x,hh+.012,z)));assign(bpy.context.object,'FlowerGold');link(bpy.context.object,'Daisy gold center')

with module('Architecture'):
 wood('Gray masonry foundation',(2.6,-.145,2.5),(5.52,.27,5.32),'StoneGray')
 wood('Dark floor underlay',(2.6,-.027,2.5),(5.20,.035,5.0),'WoodDark')
 # Physical staggered boards; varying atlas crops keep the floor from repeating one knot.
 for row in range(28):
  z0=row*5/28;z1=(row+1)*5/28
  x=-.7 if row%2 else 0
  while x<5.2:
   x0=max(0,x);x1=min(5.2,x+1.40);x+=1.40
   if x1-x0>.015:wood('Individual staggered floorboard',((x0+x1)/2,-.021,(z0+z1)/2),(x1-x0-.006,.053,z1-z0-.005),'WoodFloor')
 # Cream plaster around the real recessed window opening.
 for c,s in [((1.0,1.35,4.98),(2.0,2.70,.17)),((4.22,1.35,4.98),(1.96,2.70,.17)),((2.62,.49,4.98),(1.24,.98,.17)),((2.62,2.56,4.98),(1.24,.28,.17))]:wood('Back wall plaster panel',c,s,'Plaster')
 for xx in [.025,5.175]:
  outline=[(0,0),(5,0),(5,2.70),(0,1.25)]
  vv=[(xx+dx,yy,zz) for dx in [-.075,.075] for zz,yy in outline]
  o=mesh('Sloping cutaway side plaster',vv,[(0,1,2,3),(7,6,5,4)]+[(i,i+4,(i+1)%4+4,(i+1)%4) for i in range(4)],'PaintCream')
  pivot=cv((xx,1.35,2.5))
  for vertex in o.data.vertices:vertex.co-=pivot
  o.location=pivot;assign(o,'Plaster')
 for xa,xb in [(-.03,2.363),(3.149,5.23)]:
  wood('Low front cream plaster panel',((xa+xb)/2,.55,0),(xb-xa,1.10,.15),'Plaster')
  wood('Front cutaway heavy top rail',((xa+xb)/2,1.14,0),(xb-xa,.17,.25))
  wood('Front lower rail',((xa+xb)/2,.13,-.03),(xb-xa,.18,.23))
 for xx in [0,5.2]:
  for zz,hh in [(0,1.25),(5.0,2.72)]:
   wood('Heavy corner post',(xx,hh/2,zz),(.25,hh,.25))
   wood('Dark metal corner cap',(xx,hh+.02,zz),(.29,.16,.29),'Iron')
  beam('Sloping cutaway side top beam',(xx,1.25,-.02),(xx,2.67,5.02),.23,.22,'WoodDark')
  wood('Side bottom timber',(xx,.16,2.5),(.21,.22,5.0))
  for zz in [1.65,3.3]:
   hh=1.25+(2.70-1.25)*zz/5-.10
   wood('Side wall timber stud',(xx,.08+hh/2,zz),(.15,hh,.15))
 wood('Back continuous top beam',(2.6,2.67,4.90),(5.25,.22,.23))
 wood('Back timber base',(2.6,.18,4.86),(5.20,.22,.20))
 for xx in [1.04,1.90,3.34,4.95]:wood('Back exposed upright',(xx,1.38,4.86),(.16,2.48,.19))
 # Deep window trim, golden panes, and a small view of greenery behind the frame.
 wood('Warm window pane',(2.62,1.72,5.015),(1.22,1.36,.035),'GlowWarm');assign(parts[-1],'GlowWarm',2)
 for i in range(14):
  xx=random.uniform(2.06,3.18);yy=random.uniform(1.15,2.31)
  card('Soft outside leaf cluster',(xx,yy,4.997),random.uniform(.10,.18),random.uniform(.15,.27),random.randrange(4,8))
 for xx in [1.97,3.27]:wood('Thick window jamb',(xx,1.72,4.84),(.15,1.49,.21),'WoodHoney')
 for yy in [1.02,2.42]:wood('Deep window horizontal frame',(2.62,yy,4.83),(1.46,.16,.23),'WoodHoney')
 wood('Wide window sill',(2.62,.98,4.77),(1.55,.14,.35),'WoodHoney')
 wood('Window vertical mullion',(2.62,1.72,4.75),(.075,1.30,.09),'WoodHoney')
 wood('Window horizontal mullion',(2.62,1.72,4.75),(1.21,.075,.09),'WoodHoney')
 wood('Front stone threshold',(2.6,-.09,-.36),(1.50,.18,.69),'Paving')
 # Open inward-edge door; all components stay editable as separate modules.
 start=len(parts);hinge=cv((2.31,.04,-.04))
 for i in range(5):wood('Door vertical plank',(2.31+(i+.5)*.80/5,.98,-.04),(.80/5-.004,1.87,.075),'WoodHoney')
 for yy in [.22,1.68]:wood('Door lower and upper rail',(2.71,yy,-.10),(.80,.11,.07))
 for yy in [.32,1.56]:wood('Door iron hinge',(2.40,yy,-.15),(.17,.045,.022),'Iron')
 wood('Door dark handle',(3.00,.91,-.15),(.05,.14,.05),'Iron')
 rot=Matrix.Translation(hinge)@Matrix.Rotation(math.radians(35),4,'Z')@Matrix.Translation(-hinge)
 for o in parts[start:]:o.matrix_world=rot@o.matrix_world
 for xx in [2.29,3.22]:wood('Low cutaway door jamb',(xx,.64,.0),(.16,1.28,.21))

with module('Furniture'):
 # Woven bedside rug and fringe, extending into the empty side of the bed.
 cloth('Woven bedside rug',.49,2.13,2.25,4.65,.047,'FabricCream')
 for xx in [.49,2.13]:
  for j in range(26):wood('Rug short fringe',(xx,.041,2.27+j*.09),(.07,.009,.012),'FabricCream')
 # Nightstand at the original save-book location.
 for xx in [1.70,2.14]:
  for zz in [3.98,4.36]:wood('Nightstand feet',(xx,.22,zz),(.065,.44,.065))
 wood('Nightstand body',(1.92,.46,4.17),(.52,.40,.46),'WoodHoney')
 wood('Nightstand top',(1.92,.67,4.17),(.57,.065,.51),'WoodHoney')
 wood('Nightstand drawer',(1.92,.49,3.925),(.45,.16,.025))
 sphere('Nightstand round pull',(1.92,.49,3.901),(.028,.028,.022),'Iron')
 # Radio bookshelf, books and a small leafy plant.
 for xx in [4.15,4.87]:
  for zz in [4.39,4.71]:wood('Radio shelf foot',(xx,.29,zz),(.065,.58,.065))
 for yy in [.14,.57]:wood('Radio shelf boards',(4.51,yy,4.55),(.87,.07,.43),'WoodHoney')
 for xx in [4.19,4.29,4.38,4.51,4.63,4.76]:wood('Shelf colored book',(xx,.32,4.56),(.075,.30+random.uniform(-.03,.03),.24),random.choice(['FabricSage','Paper','WoodHoney']))
 for xx in [4.36,4.76]:wood('Radio rubber foot',(xx,.615,4.53),(.045,.06,.11),'Iron')
 cyl('Small plant pot',(4.21,.651,4.55),.087,.14,'Terracotta',10)
 compact_plant((4.21,.72,4.55),6,.10,False,.30)
 # Framed leaf picture follows the reference's upper-right placement.
 wood('Botanical picture frame',(4.52,1.82,4.82),(.40,.46,.055),'WoodHoney')
 wood('Botanical picture paper',(4.52,1.82,4.783),(.34,.40,.015),'Paper')
 card('Botanical leaf artwork',(4.52,1.82,4.770),.25,.29,1)
 # Two chairs are tucked inside the right wall, avoiding the old overlap.
 for cx,sign in [(3.20,-1),(4.82,1)]:
  for xx in [cx-.17,cx+.17]:
   for zz in [2.04,2.36]:wood('Dining chair leg',(xx,.22,zz),(.055,.44,.055),'WoodHoney')
  wood('Dining chair soft seat',(cx,.46,2.20),(.43,.065,.42),'WoodHoney')
  for zz in [2.02,2.38]:wood('Dining chair back upright',(cx+sign*.18,.63,zz),(.055,.59,.055),'WoodHoney')
  for yy in [.67,.84]:wood('Dining chair ladder back',(cx+sign*.18,yy,2.20),(.055,.10,.37),'WoodHoney')
 for xx in [3.60,4.42]:
  for zz in [1.88,2.52]:wood('Dining table leg',(xx,.37,zz),(.085,.74,.085),'WoodHoney')
 wood('Dining table skirt',(4.01,.67,2.20),(.92,.15,.75),'WoodDark')
 wood('Dining table bevel top',(4.01,.773,2.20),(1.02,.07,.864),'WoodHoney')
 cloth('Soft cream table runner',3.84,4.18,1.75,2.65,.824,'FabricCream',.025)
 cyl('Flower pot on table',(4.05,.91,2.31),.10,.17,'Terracotta',12)
 compact_plant((4.05,1.00,2.31),5,.12,False,.32)
 table_bouquet()
 # Open drinking cup with handle.
 x,z=3.71,1.98;n=12;y=.826;h=.14;r=.062
 vv=[(x+rr*math.cos(i*math.tau/n),yy,z+rr*math.sin(i*math.tau/n)) for rr,yy in [(r,y),(r,y+h),(r-.013,y+h),(r-.013,y+.02)] for i in range(n)]
 ff=[(q*n+i,q*n+(i+1)%n,(q+1)*n+(i+1)%n,(q+1)*n+i) for q in range(3) for i in range(n)]+[tuple(range(3*n,4*n))]
 mesh('Open ceramic mug',vv,ff,'Ceramic')
 for i in range(8):
  a=i*math.tau/8;b=(i+1)*math.tau/8
  beam('Mug handle',(x-.075-.035*math.cos(a),y+.08+.047*math.sin(a),z),(x-.075-.035*math.cos(b),y+.08+.047*math.sin(b),z),.014,.014,'Ceramic')

with module('Bed'):
 for xx in [.51,1.57]:
  for zz,hh in [(2.42,.84),(4.48,1.15)]:wood('Bed shaped post',(xx,hh/2+.02,zz),(.10,hh,.10),'WoodHoney')
 for xx in [.51,1.57]:wood('Bed side rail',(xx,.35,3.46),(.10,.24,2.10),'WoodHoney')
 wood('Bed underside',(1.04,.34,3.47),(1.05,.13,2.05))
 wood('Bed footboard',(1.04,.64,2.42),(1.06,.31,.09),'WoodHoney')
 # Curved headboard silhouette rather than a flat rectangular cap.
 outline=[(.54,.66),(1.54,.66),(1.54,.96)]+[(1.04+.50*math.cos(i*math.pi/12),.96+.13*math.sin(i*math.pi/12)) for i in range(13)]
 panel('Bed arched headboard',outline,4.48,'WoodHoney')
 softbox('Soft white mattress',(1.04,.54,3.47),(1.03,.21,2.02),'PaintCream')
 cloth('Sage blanket with soft folds',.49,1.59,2.46,4.04,.675,'FabricSage',.14)
 softbox('White curved pillow',(1.04,.726,4.21),(.81,.17,.38),'PaintCream')

with module('SaveBook'):
 wood('Save book pages',(1.92,.744,4.19),(.25,.035,.18),'Paper')
 for yy in [.724,.766]:wood('Sage save book cover',(1.92,yy,4.19),(.275,.009,.20),'FabricSage')
 wood('Save book spine',(1.785,.746,4.19),(.015,.050,.20),'FabricSage')
 horizontal_text('SAVE book title','SAVE',(1.92,.774,4.19),.070)

with module('Radio'):
 wood('Radio wood cabinet',(4.57,.79,4.53),(.55,.29,.23),'WoodHoney')
 wood('Radio dark front',(4.57,.79,4.403),(.49,.22,.027),'Iron')
 for dx in [-.14,.04]:
  o=cyl('Radio round speaker',(4.57+dx,.79,4.382),.077,.018,'Burlap',16);o.rotation_euler.x=math.pi/2
  for j in range(5):wood('Speaker grille line',(4.57+dx,.743+j*.023,4.369),(.12,.007,.008),'Iron')
 for yy in [.75,.84]:
  o=cyl('Radio tuning knob',(4.78,yy,4.378),.025,.022,'Aluminium',12);o.rotation_euler.x=math.pi/2
 beam('Radio antenna',(4.66,.94,4.58),(4.89,1.12,4.64),.009,.009,'Iron')
 wood('Radio tuning strip',(4.56,.905,4.38),(.32,.025,.015),'Paper')

with module('Chest'):
 cx,cz=4.37,.872
 for xx in [cx-.49,cx+.49]:
  for zz in [cz-.28,cz+.28]:wood('Chest short foot',(xx,.09,zz),(.11,.13,.11))
 wood('Chest solid core',(cx,.33,cz),(1.08,.47,.65),'WoodHoney')
 for yy in [.20,.35,.50]:
  for zz in [cz-.333,cz+.333]:wood('Chest front and rear plank',(cx,yy,zz),(1.10,.145,.04),'WoodHoney')
  for xx in [cx-.54,cx+.54]:wood('Chest end plank',(xx,yy,cz),(.04,.145,.64),'WoodHoney')
 for zz in [cz-.22,cz,cz+.22]:wood('Chest beveled lid plank',(cx,.62,zz),(1.12,.09,.218),'WoodHoney')
 for xx in [cx-.36,cx+.36]:
  wood('Chest top iron band',(xx,.67,cz),(.065,.025,.69),'Iron')
  for zz in [cz-.355,cz+.355]:wood('Chest vertical iron band',(xx,.38,zz),(.065,.53,.025),'Iron')
  for yy in [.19,.50]:
   for zz in [cz-.371,cz+.371]:sphere('Chest band rivet',(xx,yy,zz),(.018,.018,.010),'Aluminium')
 wood('Chest lock plate',(cx,.49,cz-.374),(.10,.16,.023),'Iron')
 o=cyl('Chest round lock',(cx,.47,cz-.395),.024,.018,'Aluminium',12);o.rotation_euler.x=math.pi/2

# Outward normals on solid modules; foliage cards keep their authored normals.
for o in parts:
 if 'MasonryUV' in o:continue
 if o.data.materials[0]==M[1]:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update();continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
bpy.context.view_layer.update();stats=[]
for label,items in groups.items():
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for original in items:
  cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);cp.select_set(True);copies.append(cp)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name='InteriorCozy_'+label;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 used=[m for m in M if any(o.data.materials[p.material_index]==m for p in o.data.polygons)];old=[o.data.materials[p.material_index] for p in o.data.polygons];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,old):p.material_index=used.index(m)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces));bad=[f for f in bm.faces if f.calc_area()<1e-10]
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 bm.to_mesh(o.data);bm.free();o.data.update()
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(A/f'mesh/Buildings/HouseInteriorCozy/{o.name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 coords=[(v.co.x,v.co.z,-v.co.y) for v in o.data.vertices]
 stats.append(dict(name=o.name,module=label,triangles=len(o.data.polygons),materials=[m.name for m in used],min=[min(v[i] for v in coords) for i in range(3)],max=[max(v[i] for v in coords) for i in range(3)]))
master_source.hide_render=True;master_source.hide_viewport=True
markers=bpy.data.collections.new('04 ANCHORS - NO EXPORT');scene.collection.children.link(markers)
for label,p in [('Bed',(1.04,.60,3.47)),('Book',(1.92,.74675,4.19)),('Radio',(4.57,.88,4.53)),('Chest',(4.37,.349,.872)),('Entry',(2.6,.08,.95)),('WindowLight',(2.62,2.13,4.86))]:
 o=bpy.data.objects.new(label,None);markers.objects.link(o);o.location=cv(p);o.empty_display_size=.1
bpy.ops.object.camera_add(location=(2.6,3.375,8.705));camera=bpy.context.object;scene.camera=camera;camera.data.type='PERSP';camera.data.lens=44;target=Vector((2.6,-2.55,.55));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Cozy interior preview environment');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.62,.64,.68,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.40
for label,pos,power,size in [('Preview key',(0,1,9),700,6),('Preview fill',(7,-2,7),350,6)]:
 bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.name=label;lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size;lamp.rotation_euler=(Vector((2.6,-2.5,0))-lamp.location).to_track_quat('-Z','Y').to_euler()
 for c in list(lamp.users_collection):c.objects.unlink(lamp)
 studio.objects.link(lamp)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=9;area.spaces.active.region_3d.view_location=target
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1100;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(A/'Source~/HouseInteriorCozy/HouseInterior_Cozy_Lv1_Master.blend'))
manifest=dict(status='BUILT',scope='Player house interior Lv1 only',roomWidth=5.2,roomDepth=5.0,atlasShared=True,modules=stats,triangles=sum(s['triangles'] for s in stats))
(A/'Source~/HouseInteriorCozy/HouseInterior_Cozy_Lv1_Manifest.json').write_text(json.dumps(manifest,indent=2))
print('INTERIOR_COZY_BUILD_PASS',json.dumps(manifest),flush=True)
