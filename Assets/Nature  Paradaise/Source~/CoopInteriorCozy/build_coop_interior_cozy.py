"""Player Coop Lv1: one six-cell feeder, single functional incubator, reusable atlas modules."""
from pathlib import Path
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
prefix=(ROOT/'Assets/Nature  Paradaise/Source~/BarnInteriorCozy/build_barn_interior_cozy.py').read_text().split("with module('Architecture'):")[0]
exec(compile(prefix,__file__,'exec'),globals())
scene.name='Coop Interior Cozy Lv1 - Editable Atlas Master';master.name='01 EDITABLE COOP MODULES'
OUT=ROOT/'ArtSource/CoopInteriorCozy';OUT.mkdir(parents=True,exist_ok=True)
DEST=A/'mesh/Buildings/CoopInteriorCozy';DEST.mkdir(parents=True,exist_ok=True)
glass=bpy.data.materials.new('CoopIncubatorGlass');glass.use_nodes=True
bs=glass.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.80,.91,.93,.19);bs.inputs['Alpha'].default_value=.19;bs.inputs['Roughness'].default_value=.20
glass.surface_render_method='DITHERED';M.append(glass)

def tuft(x,z,scale=1):
 for i in range(7):
  a=i*math.tau/7+random.uniform(-.2,.2);l=random.uniform(.17,.33)*scale;w=.043*scale
  dx,dz=math.cos(a),math.sin(a);px,pz=-dz*w,dx*w
  vv=[(x,.025,z),(x+dx*l*.45+px,.04,z+dz*l*.45+pz),(x+dx*l,.028,z+dz*l),(x+dx*l*.45-px,.04,z+dz*l*.45-pz)]
  mesh('Soft stylized straw tuft',vv,[(0,1,2,3),(3,2,1,0)],'FlowerGold')

with module('Floor'):
 solid('Warm earthen foundation',(0,-.10,4),(10.18,.20,8.16),'Earth',.025)
 for j in range(4):
  for i in range(5):
   x=-5+i*2;z=j*2
   mesh('Soft packed earth',[(x,.003,z),(x+2,.003,z),(x+2,.003,z+2),(x,.003,z+2)],[(0,3,2,1)],'Earth')

ground=M[0].copy();ground.name='NP_CozyCoopEarth';ns=ground.node_tree.nodes;bs=ns.get('Principled BSDF');tex=next(n for n in ns if n.type=='TEX_IMAGE' and n.image==atlas)
mix=ns.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(1.50,1.65,1.05,1)
sat=ns.new('ShaderNodeHueSaturation');sat.inputs['Saturation'].default_value=.78
ground.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);ground.node_tree.links.new(mix.outputs[0],sat.inputs['Color']);ground.node_tree.links.new(sat.outputs[0],bs.inputs['Base Color']);M.append(ground)
for node in ns:
 if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.10
for o in groups['Floor']:o.data.materials[0]=ground

with module('Architecture'):
 solid('Continuous red rear wall backing',(0,1.65,8.07),(10.0,3.30,.04),'WoodRed',.005)
 for i in range(25):solid('Rear red plank',(-4.8+i*.4,1.65,8),(.393,3.3,.15),'WoodRed',.006)
 for x in [-5,-2.5,0,2.5,5]:
  solid('Heavy honey rear post',(x,1.68,7.89),(.25,3.36,.28),'WoodHoney')
  for sign in [-1,1]:
   if -5<=x+sign*.58<=5:beam('Rear timber knee brace',(x,2.57,7.7),(x+sign*.58,3.23,7.7),.17,.17,'WoodHoney')
 solid('Heavy back header',(0,3.28,7.91),(10.27,.27,.32),'WoodHoney')
 solid('Rear lower rail',(0,.24,7.88),(10,.20,.22),'WoodHoney')
 solid('Rear middle rail',(0,1.1,7.88),(10,.15,.20),'WoodHoney')
 # Side boards descend to the low front cutaway; recesses are actual openings.
 for side in [-1,1]:
  x=side*5
  for i in range(20):
   z=(i+.5)*.4;h=1.3+2.0*z/8;window=any(abs(z-wz)<.59 for wz in [2.2,5.8]);top=1.73 if z<4 else 2.55
   if window:
    solid('Window bottom red panel',(x,.47,z),(.15,.94,.393),'WoodRed',.006)
    if h>top:solid('Window upper red panel',(x,(h+top)/2,z),(.15,h-top,.393),'WoodRed',.006)
   else:solid('Side red plank',(x,h/2,z),(.15,h,.393),'WoodRed',.006)
  beam('Sloped honey top beam',(x,1.3,0),(x,3.3,8),.25,.25,'WoodHoney')
  solid('Side timber bottom rail',(x,.18,4),(.24,.22,8),'WoodHoney')
  for z in [0,3.5,6.8,8]:
   hh=1.3+2*z/8;solid('Side heavy upright',(x,hh/2,z),(.26,hh,.26),'WoodHoney')
  for z in [2.2,5.8]:
   top=1.73 if z<4 else 2.55;mid=(.94+top)/2;hh=top-.94
   for dz in [-.61,.61]:solid('Inset window jamb',(x-side*.12,mid,z+dz),(.25,hh+.14,.13),'WoodHoney')
   for yy in [.94,top]:solid('Window sill lintel',(x-side*.12,yy,z),(.27,.14,1.35),'WoodHoney')
   solid('Window warm glass',(x,mid,z),(.025,hh-.09,1.1),'GlowWarm');assign(parts[-1],'GlowWarm',2)
   solid('Window centre upright',(x-side*.15,mid,z),(.08,hh-.08,.07),'WoodHoney')
   solid('Window centre crossbar',(x-side*.15,mid,z),(.08,.07,1.12),'WoodHoney')
 for sign in [-1,1]:
  solid('Low front red wall',(sign*3,.29,0),(4,.58,.16),'WoodRed')
  solid('Front heavy timber rail',(sign*3,.62,0),(4.09,.18,.27),'WoodHoney')
 # Tiny warm panes remain readable without a separate light per lantern.
 for side in [-1,1]:
  for z in [1.0,6.9]:
   start=len(parts);lantern((0,1.16,z),.67)
   for o in parts[start:]:
    p=o.location.copy();o.location.x=side*5-side*(.24-p.y-z);o.location.y=-z+side*p.x;o.rotation_euler.z+=side*math.pi/2
 # Small back window above the feed mill.
 solid('Mill window warm glass',(-2.7,2.18,7.90),(1.04,.82,.028),'GlowWarm');assign(parts[-1],'GlowWarm',2)
 frame('Mill window frame',-2.7,2.18,7.84,1.14,.92,.11,'WoodHoney')
 solid('Mill window crossbar',(-2.7,2.18,7.75),(1.06,.065,.08),'WoodHoney')
 solid('Mill window mullion',(-2.7,2.18,7.75),(.065,.85,.08),'WoodHoney')

with module('SetDressing'):
 for x in [-4.63,4.63]:
  for z in [.65,1.25,1.85,2.7,3.4,4.1,4.8,5.5,6.2,6.95,7.45]:tuft(x+random.uniform(-.10,.10),z,random.uniform(.8,1.2))
 for x in [-4,-3.2,-2.2,-1.3,0,1.1,2.1,4.0]:tuft(x,7.60,.9)
 for x in [-4.5,4.5]:straw((x,0,.65),.35,.35,30)
 sack((-4.05,0,6.91),.32,.85);sack((-3.57,0,6.90),.21,.59)
 obstacle('Feed sacks',(-3.92,.44,6.9),(1.12,.88,.69))
 crate((-4.03,0,6.91),.76,.69,.10)
 # Open grain at the large sack's mouth makes it read as feed rather than a generic bag.
 cyl('Open sack grain',(-4.05,.78,6.91),.25,.04,'Grain',14)
 # Ceramic bell drinker and teal water tray, on a small wooden platform.
 x,z=4.10,1.13
 for dx in [-.29,.29]:
  for dz in [-.26,.26]:solid('Drinker plinth foot',(x+dx,.11,z+dz),(.08,.22,.08),'WoodDark')
 solid('Drinker wooden platform',(x,.24,z),(.78,.12,.71),'WoodHoney')
 cyl('Drinker teal base',(x,.34,z),.34,.12,'WaterBlue',18)
 cyl('Drinker water',(x,.407,z),.30,.018,'GlassBlue',18)
 # A bell profile with broad shoulders and a capped neck.
 profile=[(.25,.41),(.28,.49),(.25,.83),(.19,.96),(.075,1.0),(.065,1.08)]
 vv=[(x+r*math.cos(i*math.tau/18),y,z+r*math.sin(i*math.tau/18)) for r,y in profile for i in range(18)]
 ff=[tuple(range(17,-1,-1)),tuple(range(90,108))]+[(j*18+i,j*18+(i+1)%18,(j+1)*18+(i+1)%18,(j+1)*18+i) for j in range(5) for i in range(18)]
 o=mesh('Cream bell chicken drinker',vv,ff,'Ceramic')
 for f in o.data.polygons:f.use_smooth=True
 obstacle('Chicken drinker',(x,.55,z),(.78,1.10,.72))

with module('Machine'):
 x,z=-2.7,6.9
 for dx in [-.26,.26]:
  for dz in [-.25,.25]:solid('Mill short foot',(x+dx,.16,z+dz),(.12,.32,.14),'WaterBlue')
 solid('Teal grain mill housing',(x,.64,z),(.66,.70,.54),'WaterBlue',.05)
 solid('Mill dark inlet',(x,.94,z-.285),(.42,.12,.025),'Iron',.01)
 # Round funnel, matching the reference feed mill silhouette.
 profile=[(.17,.98),(.24,1.16),(.43,1.39),(.45,1.46),(.40,1.46),(.38,1.39),(.17,1.14)]
 vv=[(x+r*math.cos(i*math.tau/20),y,z+r*math.sin(i*math.tau/20)) for r,y in profile for i in range(20)]
 ff=[(j*20+i,j*20+(i+1)%20,(j+1)*20+(i+1)%20,(j+1)*20+i) for j in range(6) for i in range(20)]
 o=mesh('Open teal grain funnel',vv,ff,'WaterBlue')
 for f in o.data.polygons:f.use_smooth=True
 cyl('Hopper full of grain',(x,1.38,z),.365,.035,'Grain',20)
 for i in range(24):
  a=random.random()*math.tau;r=.31*math.sqrt(random.random());sphere('Gold feed kernel',(x+math.cos(a)*r,1.412,z+math.sin(a)*r),(.033,.025,.030),'FlowerGold')
 for dx in [-.16,0,.16]:sphere('Mill control button',(x+dx,.66,z-.285),(.025,.025,.012),'FlowerGold')
 wheel=cyl('Mill side wheel',(x+.40,.87,z),.20,.10,'Iron',14);wheel.rotation_euler.y=math.pi/2
 beam('Mill crank',(x+.47,.87,z),(x+.47,.87,z-.26),.045,.045,'Aluminium');beam('Crank wooden grip',(x+.47,.87,z-.26),(x+.64,.87,z-.26),.06,.06,'WoodHoney')
 solid('Mill grain spout',(x+.31,.52,z-.27),(.22,.13,.24),'Aluminium')
 crate((x+.58,0,z-.47),.48,.46,.42);solid('Fresh feed in output crate',(x+.58,.39,z-.47),(.36,.035,.34),'Grain')
 obstacle('Feed mill',(x,.74,z),(.91,1.49,.85));obstacle('Mill output crate',(x+.58,.22,z-.47),(.52,.44,.50))

with module('IncubatorBody'):
 x,z=3.5,6.95
 for dx in [-.38,.38]:
  for dz in [-.32,.32]:solid('Incubator timber table leg',(x+dx,.40,z+dz),(.10,.80,.10),'WoodHoney')
 solid('Incubator table shelf',(x,.22,z),(.84,.08,.73),'WoodHoney')
 solid('Incubator tabletop',(x,.82,z),(1.02,.12,.91),'WoodHoney')
 cyl('Teal incubation bowl',(x,.94,z),.44,.18,'WaterBlue',20)
 cyl('Bowl straw bed',(x,1.034,z),.38,.035,'Hay',20)
 straw((x,1.04,z),.35,.35,65,.005)
 solid('Incubator amber thermostat',(x,.96,z-.45),(.16,.06,.04),'GlowWarm');assign(parts[-1],'GlowWarm',2)
 obstacle('Incubator table',(x,.48,z),(1.06,.96,.95))

with module('Cover'):
 x,z=3.5,6.95;n=24;rings=9;vv=[]
 for j in range(rings):
  a=j/(rings-1)*math.pi/2;rr=.46*math.cos(a);yy=1.04+.48*math.sin(a)
  for i in range(n):vv.append((x+rr*math.cos(i*math.tau/n),yy,z+rr*math.sin(i*math.tau/n)))
 ff=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(rings-1) for i in range(n)]
 o=mesh('Clear domed incubation cover',vv,ff,'Ceramic',mi=3)
 for p in o.data.polygons:p.use_smooth=True
 # Narrow teal rim is part of the opening cover; no false opaque lid over the nest.
 for i in range(n):
  a=i*math.tau/n;b=(i+1)*math.tau/n
  beam('Dome rolled rim',(x+.46*math.cos(a),1.045,z+.46*math.sin(a)),(x+.46*math.cos(b),1.045,z+.46*math.sin(b)),.035,.035,'WaterBlue')
 sphere('Dome top grip',(x,1.545,z),(.07,.035,.07),'Ceramic')

with module('Egg'):
 sphere('Single functional incubating egg',(3.5,1.185,6.95),(.105,.125,.105),'Ceramic')

def rounded(w,d,r):
 return [(cx+r*math.cos(a*math.pi/2+i*math.pi/8),cz+r*math.sin(a*math.pi/2+i*math.pi/8)) for a,(cx,cz) in enumerate([(w/2-r,d/2-r),(-w/2+r,d/2-r),(-w/2+r,-d/2+r),(w/2-r,-d/2+r)]) for i in range(5)]

with module('Trough'):
 z=2.2;profiles=[(3.0,.56,.10,.12),(3.12,.66,.13,.43),(2.98,.52,.09,.43),(2.86,.43,.07,.19)]
 vv=[]
 for w,d,r,y in profiles:vv.extend([(x,y,z+zz) for x,zz in rounded(w,d,r)])
 n=20;ff=[tuple(range(n-1,-1,-1)),tuple(range(3*n,4*n))]+[(q*n+i,q*n+(i+1)%n,(q+1)*n+(i+1)%n,(q+1)*n+i) for q in range(3) for i in range(n)]
 mesh('Continuous six cell aluminium feed basin',vv,ff,'Aluminium')
 for xx in [-1,-.5,0,.5,1]:solid('Rounded feeder divider',(xx,.31,z),(.042,.25,.49),'Aluminium',.008)
 for x in [-1.24,1.24]:
  for dz in [-.19,.19]:solid('Feeder low feet',(x,.08,z+dz),(.10,.16,.10),'Iron',.015)

for i in range(6):
 with module('Fill'+str(i+1)):
  x=(i-2.5)*.5;z=2.2
  solid('One portion poultry grain',(x,.30,z),(.45,.12,.41),'Grain',.03)
  for j in range(10):
   sphere('Soft individual grain',(x+random.uniform(-.18,.18),.37,z+random.uniform(-.15,.15)),(.026,.016,.023),'FlowerGold')

# Export grouped modules with editable objects retained in the master.
for o in parts:
 if 'straw ribbons' in o.name or 'tuft' in o.name:continue
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
bpy.context.view_layer.update();stats=[]
for label,items in groups.items():
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for original in items:
  cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);cp.select_set(True);copies.append(cp)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name='CoopCozy_'+label;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 old=[o.data.materials[p.material_index] for p in o.data.polygons];used=[m for m in M if m in old];o.data.materials.clear()
 for m in used:o.data.materials.append(m)
 for p,m in zip(o.data.polygons,old):p.material_index=used.index(m)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bad=[f for f in bm.faces if f.calc_area()<1e-10]
 if bad:bmesh.ops.delete(bm,geom=bad,context='FACES')
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bm.to_mesh(o.data);bm.free();o.data.update()
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.export_scene.fbx(filepath=str(DEST/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
 coords=[(v.co.x,v.co.z,-v.co.y) for v in o.data.vertices]
 stats.append(dict(name=o.name,module=label,triangles=len(o.data.polygons),materials=[m.name for m in used],min=[min(v[i] for v in coords) for i in range(3)],max=[max(v[i] for v in coords) for i in range(3)]))
master.hide_render=True;master.hide_viewport=True
bpy.ops.object.camera_add(location=(0,10.5,13.5));camera=bpy.context.object;scene.camera=camera;camera.data.type='PERSP';camera.data.lens=40;target=Vector((0,-4,.6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Cozy coop studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
for pos,power,size in [((-3,1,9),1400,7),((4,-5,6),650,6)]:
 bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.data.energy=power;lamp.data.size=size;lamp.rotation_euler=(target-lamp.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1200;scene.render.resolution_y=1050;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=14;area.spaces.active.region_3d.view_location=target
bpy.ops.wm.save_as_mainfile(filepath=str(A/'Source~/CoopInteriorCozy/CoopInterior_Cozy_Lv1_Master.blend'))
manifest=dict(status='BUILT',level=1,capacity=6,roomWidth=10,roomDepth=8,modules=stats,colliders=collisions,triangles=sum(s['triangles'] for s in stats),feedSlots=[[(i-2.5)*.5,.24,2.2] for i in range(6)],expansionCapacities=[6,9,12,15],incubatorCapacity=1)
(A/'Source~/CoopInteriorCozy/CoopInterior_Cozy_Lv1_Manifest.json').write_text(json.dumps(manifest,indent=2))
print('COOP_COZY_BUILD_PASS',manifest['triangles'],flush=True)
