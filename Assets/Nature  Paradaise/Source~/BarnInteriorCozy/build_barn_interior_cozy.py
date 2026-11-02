"""Editable shared-atlas Barn Lv1. Unity-local metres; four stable feed portions.

Expansion contract: extend wall/floor bays, duplicate 1.2m trough cells and props;
never scale equipment or change existing save slot IDs to make higher levels.
"""
from pathlib import Path
import contextlib
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
library=(ROOT/'Assets/Nature  Paradaise/Source~/PoultryCozy/build_poultry_cozy.py').read_text().split('\nfor name,builder in ')[0]
exec(compile(library,__file__,'exec'),globals())
begin('Barn Interior Cozy Lv1')
OUT=ROOT/'ArtSource/BarnInteriorCozy';OUT.mkdir(parents=True,exist_ok=True)
DEST=A/'mesh/Buildings/BarnInteriorCozy';DEST.mkdir(parents=True,exist_ok=True)
master=source;master.name='01 EDITABLE BARN MODULES';exports.name='02 GAME MODULES';groups={};collisions=[]

@contextlib.contextmanager
def module(name):
 global source
 old=source;source=bpy.data.collections.new(name+' - EDITABLE');master.children.link(source);start=len(parts)
 yield
 groups[name]=parts[start:];source=old

def solid(name,c,s,slot='WoodCream',bevel=.025):return box(name,c,s,slot,bevel)
def obstacle(name,c,s):collisions.append(dict(name=name,center=c,size=s))

def straw(c,rx,rz,count,y=.026):
 # Tiny double-sided ribbons, grouped into the same static mesh; no physics/particles.
 vv=[];ff=[]
 for i in range(count):
  a=random.random()*math.tau;r=math.sqrt(random.random());x=c[0]+math.cos(a)*rx*r;z=c[2]+math.sin(a)*rz*r
  a=random.random()*math.tau;l=random.uniform(.08,.21);w=random.uniform(.010,.019);yy=c[1]+y+random.uniform(0,.008)
  dx,dz=math.cos(a)*l/2,math.sin(a)*l/2;px,pz=-math.sin(a)*w/2,math.cos(a)*w/2;k=len(vv)
  vv.extend([(x-dx-px,yy,z-dz-pz),(x+dx-px,yy+.004,z+dz-pz),(x+dx+px,yy+.004,z+dz+pz),(x-dx+px,yy,z-dz+pz)])
  ff.extend([(k,k+1,k+2,k+3),(k+3,k+2,k+1,k)])
 return mesh('Loose golden straw ribbons',vv,ff,'FlowerGold')

def bale(c):
 x,y,z=c;solid('Tied straw bale',(x,y+.32,z),(.86,.64,.64),'Hay',.07)
 for dx in [-.24,.24]:
  solid('Bale twine top',(x+dx,y+.65,z),(.024,.018,.64),'Rope',.002)
  for dz in [-.325,.325]:solid('Bale twine vertical',(x+dx,y+.32,z+dz),(.024,.64,.016),'Rope',.002)

def tray(name,x,z,width,length,water=False):
 # Folded metal trough with visible inner basin, rounded rim and four short feet.
 bottom=.17;top=.64
 solid(name+' basin base',(x,bottom,z),(width,.10,length),'Aluminium',.025)
 for sign in [-1,1]:
  solid(name+' long wall',(x+sign*(width/2-.035),.39,z),(.07,.48,length),'Aluminium',.02)
  solid(name+' end wall',(x,.39,z+sign*(length/2-.035)),(width,.48,.07),'Aluminium',.02)
  solid(name+' rolled long rim',(x+sign*width/2,top,z),(.085,.07,length+.035),'MetalHighlight',.02)
  solid(name+' rolled end rim',(x,top,z+sign*length/2),(width+.06,.07,.085),'MetalHighlight',.02)
 for dx in [-width*.34,width*.34]:
  for dz in [-length*.39,length*.39]:solid(name+' foot',(x+dx,.09,z+dz),(.11,.18,.13),'Iron',.018)
 if water:
  solid('Still blue water surface',(x,.515,z),(width-.14,.018,length-.14),'WaterBlue',0)
  # Sparse broken arcs, not a tiled glitter pattern.
  for i in range(6):
   cx=x+random.uniform(-width*.32,width*.32);cz=z+random.uniform(-length*.34,length*.34)
   for j in range(4):
    a=j*.43+i;b=a+.32;r=.10+random.random()*.025
    beam('Soft water ripple',(cx+math.cos(a)*r,.529,cz+math.sin(a)*r),(cx+math.cos(b)*r,.529,cz+math.sin(b)*r),.009,.009,'GlassBlue')

with module('Architecture'):
 solid('Earthen foundation',(0,-.105,5),(12.23,.21,10.20),'Earth',.025)
 # World-density soil surface. Each patch remains inside the padded Earth atlas tile.
 for j in range(10):
  for i in range(12):
   x=-6+i;z=j
   mesh('Packed earth floor',[(x,.004,z),(x+1,.004,z),(x+1,.004,z+1),(x,.004,z+1)],[(0,3,2,1)],'Earth')
 # Back red plank wall, pale weathered framing, deep knee braces.
 for i in range(30):solid('Rear red vertical plank',(-5.8+i*.4,1.7,10),(.393,3.4,.15),'WoodRed',.006)
 for x in [-6,-2,2,6]:
  solid('Rear heavy pale timber post',(x,1.72,9.90),(.26,3.44,.29))
  solid('Rear iron post shoe',(x,.12,9.87),(.29,.22,.31),'Iron',.015)
 solid('Rear continuous top beam',(0,3.4,9.88),(12.28,.25,.30))
 solid('Rear horizontal wall rail',(0,1.0,9.87),(12,.15,.20))
 for x in [-6,-2,2,6]:
  for sign in [-1,1]:
   if -6<=x+sign*.78<=6:beam('Rear diagonal knee brace',(x,2.6,9.71),(x+sign*.78,3.31,9.71),.15,.15,'WoodCream')
 # Side wall silhouettes descend toward camera. Real window recesses in both sides.
 for side in [-1,1]:
  x=side*6
  for i in range(25):
   z=(i+.5)*.4;h=1.36+2.04*z/10
   windows=any(abs(z-wz)<.61 for wz in [3,7]);wtop=1.78 if z<5 else 2.44
   if windows:
    solid('Side window lower red panel',(x,.55,z),(.15,1.10,.393),'WoodRed',.005)
    if h>wtop:solid('Side window upper red panel',(x,(h+wtop)/2,z),(.15,h-wtop,.393),'WoodRed',.005)
   else:solid('Side red plank',(x,h/2,z),(.15,h,.393),'WoodRed',.005)
  beam('Sloping heavy side top beam',(x,1.36,0),(x,3.40,10),.26,.26,'WoodCream')
  solid('Side base rail',(x,.16,5),(.22,.22,10))
  for z in [0,2,5,8,10]:
   h=1.36+2.04*z/10;solid('Side wall upright',(x,h/2,z),(.26,h,.26))
  for z in [3,7]:
   top=1.78 if z<5 else 2.44;mid=(1.10+top)/2;hh=top-1.1
   for dz in [-.63,.63]:solid('Deep window jamb',(x-side*.12,mid,z+dz),(.24,hh+.14,.12),'WoodHoney')
   for y in [1.10,top]:solid('Window lintel sill',(x-side*.12,y,z),(.26,.14,1.38),'WoodHoney')
   solid('Warm window pane',(x,mid,z),(.025,hh-.08,1.14),'GlowWarm');assign(parts[-1],'GlowWarm',2)
   solid('Window center upright',(x-side*.16,mid,z),(.08,hh-.09,.06),'WoodCream')
   solid('Window cross rail',(x-side*.16,mid,z),(.08,.065,1.15),'WoodCream')
  for z in [1.4,8.5]:
   start=len(parts);lantern((0,1.25,z),.8)
   # Helper lantern faces -Z. Rotate its bracket to the inner face of each side wall.
   for o in parts[start:]:
    p=o.location.copy();o.location.x=x-side*(.28-p.y-z);o.location.y=-z+side*p.x;o.rotation_euler.z+=side*math.pi/2
 # Low front wall leaves the existing 2m entrance completely clear.
 for sign in [-1,1]:
  solid('Front cutaway red panel',(sign*3.53,.33,0),(4.94,.66,.16),'WoodRed')
  solid('Front pale top rail',(sign*3.53,.69,0),(5.0,.15,.25))

with module('SetDressing'):
 # Reference back-left water trough, two barrels, pail; rear stacks flank machine.
 tray('Water trough',-3.85,8.90,2.15,.85,True);obstacle('Water basin',(-3.85,.34,8.90),(2.23,.69,.93))
 for p in [(-5.35,0,9.25),(-5.35,0,8.2)]:barrel(p,.36,.78);obstacle('Barrel', (p[0],.40,p[2]),(.76,.80,.76))
 bucket((-2.40,.015,8.80),.22,.36)
 for x,z in [(-1.75,8.95),(3.85,9.05)]:
  for dx in [-.46,.46]:
   for y in [0,.65]:bale((x+dx,y,z))
  bale((x,1.30,z));obstacle('Hay stack',(x,.98,z),(1.85,1.96,.73))
 for x,z in [(5.17,9.1),(5.18,8.20)]:sack((x,0,z),.35,1.05);obstacle('Sack',(x,.51,z),(.72,1.04,.57))
 crate((1.75,0,9.1),.80,.65,.61);obstacle('Supply crate',(1.75,.31,9.1),(.88,.62,.73))
 for x in [-5.5,5.5]:
  for z in [.7,9.3]:straw((x,0,z),.55,.55,100)
 for z in [1,2,3,4,5,6,7,8,9]:
  for x in [-5.7,5.7]:straw((x,0,z),.22,.8,38)
 for x,z in [(-1.7,8.15),(3.8,8.2),(-4.8,7.7),(4.7,1.3),(-3.5,1.8),(1.3,6.6),(-1.0,5.3)]:straw((x,0,z),.65,.55,45)
 straw((0,0,5),5.5,4.4,650)

with module('Machine'):
 # Shared-atlas chaff cutter: hopper, steel body, flywheel, outlet, filled crate.
 x,z=0,8.75
 for dx in [-.44,.44]:
  for dz in [-.38,.38]:beam('Machine splayed leg',(x+dx,.03,z+dz),(x+dx*.8,.75,z+dz*.75),.13,.13,'GlassBlue')
 solid('Machine painted cutter housing',(0,.94,z),(.98,.58,.78),'GlassBlue',.05)
 solid('Machine aluminium front fascia',(0,.95,z-.42),(.77,.31,.06),'Aluminium')
 # Tapered open hopper funnel with real inside faces.
 vv=[(dx*w,y,z+dz*d) for w,d,y in [(.37,.28,1.2),(.61,.46,1.83)] for dx,dz in [(-1,-1),(1,-1),(1,1),(-1,1)]]
 o=mesh('Open steel hopper',vv,[(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)],'GlassBlue')
 mod=o.modifiers.new('Hopper sheet thickness','SOLIDIFY');mod.thickness=.035;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 solid('Hopper fresh cut grass',(0,1.67,z),(1.03,.045,.75),'LeafGreen',.025)
 for i in range(30):
  xx=random.uniform(-.44,.44);zz=z+random.uniform(-.28,.28)
  beam('Grass in hopper',(xx,1.68,zz),(xx+random.uniform(-.08,.08),random.uniform(1.76,1.99),zz+.06),.020,.020,'LeafGreen')
 wheel=cyl('Large cutter flywheel',(.68,.99,z),.33,.10,'Iron',16);wheel.rotation_euler.y=math.pi/2
 for j in range(6):
  a=j*math.tau/6;beam('Flywheel spoke',(.74,.99,z),(.74,.99+.27*math.sin(a),z+.27*math.cos(a)),.055,.055,'Aluminium')
 beam('Hand crank',(.83,1.0,z),(.83,1.0,z-.39),.055,.055,'Aluminium')
 beam('Wood crank handle',(.83,1.0,z-.39),(1.05,1.0,z-.39),.075,.075,'WoodHoney')
 solid('Machine output chute',(0,.69,z-.56),(.36,.13,.39),'Aluminium')
 crate((0,0,z-.85),.78,.62,.37)
 solid('Crate freshly chopped fodder',(0,.32,z-.85),(.64,.05,.49),'LeafGreen')
 straw((0,.34,z-.85),.28,.22,35,.01)
 obstacle('Machine',(0,.92,z),(1.15,1.85,.99));obstacle('Machine output crate',(0,.22,z-.86),(.83,.44,.69))

for row,x in enumerate([-2.3,2.3]):
 with module('Trough'+str(row+1)):
  tray('Two portion aluminium feeding trough',x,4.5,.9,2.4)
  solid('Middle divider',(x,.40,4.5),(.78,.43,.055),'Aluminium')
 for cell,z in enumerate([3.9,5.1]):
  with module('Fill'+str(row+1)+'_'+str(cell+1)):
   solid('One hay portion',(x,.47,z),(.70,.13,1.00),'Hay',.06)
   straw((x,.54,z),.33,.47,90,.012)

# Clean outward normals (ribbons deliberately keep two sides).
for o in parts:
 if 'straw ribbons' in o.name:continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
bpy.context.view_layer.update();stats=[]
for label,items in groups.items():
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for original in items:
  cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);cp.select_set(True);copies.append(cp)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name='BarnCozy_'+label;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 old=[o.data.materials[p.material_index] for p in o.data.polygons];used=[m for m in M if m in old];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,old):p.material_index=used.index(m)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(DEST/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
 coords=[(v.co.x,v.co.z,-v.co.y) for v in o.data.vertices]
 stats.append(dict(name=o.name,module=label,triangles=len(o.data.polygons),materials=[m.name for m in used],min=[min(v[i] for v in coords) for i in range(3)],max=[max(v[i] for v in coords) for i in range(3)]))
master.hide_render=True;master.hide_viewport=True
bpy.ops.object.camera_add(location=(0,9.5,13.5));camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=14.2;target=Vector((0,-5,.8));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Cozy barn preview');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
for pos,power,size in [((-3,1,10),1700,8),((5,-5,7),700,7)]:
 bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.size=size;lamp.rotation_euler=(target-lamp.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1200;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=16;area.spaces.active.region_3d.view_location=target
scene.render.filepath=str(OUT/'BarnInterior_Blender.png')
bpy.ops.wm.save_as_mainfile(filepath=str(A/'Source~/BarnInteriorCozy/BarnInterior_Cozy_Lv1_Master.blend'))
manifest=dict(status='BUILT',level=1,capacity=4,roomWidth=12,roomDepth=10,layoutScale=1.4,modules=stats,colliders=collisions,triangles=sum(s['triangles'] for s in stats),expansion=dict(capacities=[4,8,14,20],wallBayMetres=2,troughCellMetres=1.2,equipmentScale='constant; extend room and add cells'))
(A/'Source~/BarnInteriorCozy/BarnInterior_Cozy_Lv1_Manifest.json').write_text(json.dumps(manifest,indent=2))
print('BARN_COZY_BUILD_PASS',manifest['triangles'],flush=True)
