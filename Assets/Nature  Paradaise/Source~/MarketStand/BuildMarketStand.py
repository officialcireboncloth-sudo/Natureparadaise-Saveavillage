"""Native Blender authoring: shared-atlas timber/striped canopy and genuine Toon Farm produce."""
import bpy, math, json, random
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
ASSET=ROOT/'Assets/Nature  Paradaise'
OUT=ROOT/'ArtSource/MarketStand'
SOURCE=ASSET/'Source~/MarketStand'
EXPORT=ASSET/'mesh/World/MarketStand'
PACK=ASSET/'Pack/Toon Series/Toon Farm Pack/Models'
(OUT/'Previews').mkdir(parents=True,exist_ok=True)
random.seed(27)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='Market Stand - Editable Atlas Master'
scene.unit_settings.system='METRIC'
atlas=bpy.data.images.load(str(ASSET/'Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.png'));atlas.pack()
mat=bpy.data.materials.new('Shared Wood Atlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.82
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=atlas;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
pixels=np.asarray(atlas.pixels[:],dtype=np.float32).reshape(atlas.size[1],atlas.size[0],4)
palette=[]
for y in range(32,1024,64):
 for x in range(16,1024,32):palette.append(((x/2048,y/2048),pixels[y,x,:3]))
def swatch(rgb):return min(palette,key=lambda p:float(np.sum((p[1]-np.array(rgb))**2)))[0]
SAGE=swatch((.43,.49,.30));CREAM=swatch((.86,.82,.66));METAL=swatch((.19,.22,.22));ROPE=swatch((.65,.48,.27))
colls={}
for name in ['01 Timber Frame','02 Counter and Shelf','03 Produce Crates','04 Striped Canopy','05 Toon Pack Produce','06 Rope and Fasteners','90 Source Produce Library','99 Review Setup']:
 c=bpy.data.collections.new(name);scene.collection.children.link(c);colls[name]=c
def move(obj,group):
 for c in list(obj.users_collection):c.objects.unlink(obj)
 colls[group].objects.link(obj)
def finish(obj,name,group,uv='warm'):
 obj.name=name;move(obj,group);obj.data.materials.clear();obj.data.materials.append(mat)
 if uv is not None:map_uv(obj,uv)
 return obj
def map_uv(obj,style):
 mesh=obj.data;layer=mesh.uv_layers.active or mesh.uv_layers.new(name='AtlasUV')
 if isinstance(style,tuple):
  for d in layer.data:d.uv=style
  return
 rect=(.0078125,.7578125,.1171875,.9921875) if style=='warm' else (.1328125,.7578125,.2421875,.9921875)
 for poly in mesh.polygons:
  coords=[mesh.vertices[mesh.loops[l].vertex_index].co for l in poly.loop_indices]
  ranges=[max(v[i] for v in coords)-min(v[i] for v in coords) for i in range(3)]
  axes=sorted(range(3),key=lambda i:ranges[i],reverse=True);length=axes[0];cross=axes[1]
  lo=[min(v[i] for v in coords) for i in range(3)]
  shift=random.uniform(.01,.10)
  for l,v in zip(poly.loop_indices,coords):
   u=(v[cross]-lo[cross])/max(ranges[cross],.001);w=(v[length]-lo[length])/max(ranges[length],.001)
   layer.data[l].uv=(rect[0]+(rect[2]-rect[0])*(.05+.90*u),rect[1]+(rect[3]-rect[1])*(shift+.80*w))
def box(name,loc,size,group='01 Timber Frame',style='warm',bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Small softened timber edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,group,style)
def beam(name,a,b,width=.09,depth=None,group='01 Timber Frame',style='warm'):
 a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(width,depth or width,(b-a).length),group,style)
 o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def mesh_obj(name,verts,faces,group,style):
 m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);colls[group].objects.link(o);o.data.materials.append(mat);map_uv(o,style);return o

# Front is Blender -Y, exported to Unity -Z; player scale 1.5m, countertop 1.02m.
for x in [-1.43,1.43]:
 for y in [-.62,.62]:
  height=2.34 if y<0 else 2.78
  box('Main Timber Post', (x,y,height/2),(.13,.13,height))
  box('Ground Foot', (x,y,.065),(.26,.32,.13),style='dark')
  beam('Foot Diagonal Brace',(x,y,.12),(x,y+(.30 if y>0 else -.30),.38),.09)
  for z in [height-.16,.18]:
   bpy.ops.mesh.primitive_torus_add(major_radius=.095,minor_radius=.014,major_segments=8,minor_segments=4,location=(x,y,z))
   finish(bpy.context.object,'Rope Collar','06 Rope and Fasteners',ROPE)
beam('Back Canopy Crossbar',(-1.5,.62,2.59),(1.5,.62,2.59),.12)
beam('Front Canopy Crossbar',(-1.52,-.67,2.14),(1.52,-.67,2.14),.11)
for x in [-1.43,1.43]:
 beam('Roof Side Beam',(x,.68,2.51),(x,-.78,2.06),.10)
 beam('Front Knee Brace',(x,-.62,1.80),(x-.37*(1 if x>0 else -1),-.62,2.23),.07)
 beam('Side Knee Brace',(x,.62,2.16),(x,.24,2.49),.075)

for i in range(5):box('Counter Plank %02d'%i,(0,-.64+i*.315,1.02),(3.30,.305,.105),'02 Counter and Shelf',bevel=.012)
for x in [-1.40,1.40]:box('Counter Cross Support',(x,0,.94),(.12,1.48,.16),'02 Counter and Shelf',style='dark')
for i in range(15):box('Back Panel Plank %02d'%i,(-1.33+i*.19,.49,.56),(.183,.06,.80),'02 Counter and Shelf',bevel=.004)
for i in range(3):box('Open Lower Shelf Plank',(0,-.40+i*.29,.29),(2.78,.28,.07),'02 Counter and Shelf')
for y in [-.51,.43]:box('Lower Frame Rail',(0,y,.21),(2.91,.09,.12),'02 Counter and Shelf',style='dark')
for x in [-1.42,1.42]:box('Lower Side Rail',(x,0,.21),(.09,1.16,.12),'02 Counter and Shelf',style='dark')

# Separate planks leave actual small handle openings rather than alpha textures.
centers=[-1.17,-.39,.39,1.17]
for c,x in enumerate(centers):
 g='03 Produce Crates';base=1.085
 box('Crate %d Base'%c,(x,-.02,base),(.71,.69,.045),g,bevel=.003)
 for side in [-1,1]:
  box('Crate %d Side'%c,(x+side*.35,-.02,base+.13),(.045,.72,.25),g,bevel=.004)
  box('Crate %d Corner'%c,(x+side*.32,-.35,base+.13),(.055,.055,.26),g,style='dark',bevel=.003)
 box('Crate %d Back'%c,(x,.32,base+.16),(.70,.045,.30),g,bevel=.004)
 box('Crate %d Front Lower'%c,(x,-.36,base+.065),(.70,.045,.13),g,bevel=.003)
 for s in [-1,1]:box('Crate %d Handle Rail'%c,(x+s*.23,-.36,base+.18),(.24,.045,.055),g,bevel=.004)

def bezier(a,b,c,d,t):return (1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d
def canopy_point(q,t,rear=False):
 # Continuous cloth sag and a shallow transverse bow; the stripe does not restart at each face.
 if rear:
  y=.62+.45*t;z=2.70-.46*math.sin(t*math.pi/2);half=1.50+.12*t
 else:
  y=bezier(.62,.16,-.48,-.90,t);z=bezier(2.70,2.69,2.23,2.26,t);half=1.50+.20*t
 x=q*half;z-=.055*(1-q*q)*math.sin(math.pi*min(1,t+.08))
 return (x,y,z)
def cloth_surface(name,verts,faces,color,uv_coords):
 obj=mesh_obj(name,verts,faces,'04 Striped Canopy',color)
 uv=obj.data.uv_layers.active
 # A single coherent UV patch over the WHOLE ribbon, not a new patch per quad.
 # The existing palette still supplies both stripe colours; no additional image is created.
 for poly in obj.data.polygons:
  poly.use_smooth=True
  for loop in poly.loop_indices:
   u,v=uv_coords[obj.data.loops[loop].vertex_index];uv.data[loop].uv=(color[0]+(u-.5)*.004,color[1]+(v-.5)*.004)
 mod=obj.modifiers.new('Thin sewn cloth','SOLIDIFY');mod.thickness=.012;mod.offset=0
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
 obj['StripeDirection']='Continuous rear to front';return obj
for s in range(9):
 q0=-1+2*s/9;q1=-1+2*(s+1)/9;color=SAGE if s%2==0 else CREAM
 for rear,steps in [(False,14),(True,5)]:
  verts=[];coords=[]
  for i in range(steps+1):
   for edge,q in enumerate([q0,q1]):verts.append(canopy_point(q,i/steps,rear));coords.append((edge,i/steps))
  faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(steps)]
  cloth_surface(('Rear Return' if rear else 'Curved Canopy')+' Stripe %02d'%s,verts,faces,color,coords)
 for rear in [False,True]:
  verts=[];coords=[];segments=8
  for i in range(segments+1):
   u=i/segments;q=q0+(q1-q0)*u;x,y,z=canopy_point(q,1,rear)
   depth=(.20 if rear else .25)+.075*math.sin(math.pi*u)
   verts.extend([(x,y,z),(x,y+(.025 if rear else -.025),z-depth)])
   coords.extend([(u,1),(u,0)])
  faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(segments)]
  cloth_surface(('Rear Scallop' if rear else 'Deep Rounded Front Valance')+' %02d'%s,verts,faces,color,coords)
# Short folded cloth at both rear corners, gathered around the upright like the reference.
for sign in [-1,1]:
 for band,color in enumerate([SAGE,CREAM]):
  x=sign*(1.50+.045*band)
  verts=[(x,.61,2.69),(x+sign*.04,.85,2.49),(sign*1.43,.66,2.08),(x+sign*.08,1.02,1.99),(x+sign*.13,.83,2.22)]
  cloth_surface('Gathered Side Drape',verts,[(0,1,2),(1,3,2),(1,4,3)],color,[(0,1),(1,1),(.3,.4),(.5,0),(1,.4)])
 for z in [2.66,2.70]:
  bpy.ops.mesh.primitive_torus_add(major_radius=.096,minor_radius=.012,major_segments=8,minor_segments=4,location=(sign*1.43,.62,z))
  finish(bpy.context.object,'Rear Canopy Rope Tie','06 Rope and Fasteners',ROPE)

manifest=[]
def import_produce(file):
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(PACK/file),use_anim=False)
 imported=list(set(bpy.data.objects)-before);meshes=[o for o in imported if o.type=='MESH']
 selected=next((o for o in meshes if 'LOD1' in o.name.upper()),next((o for o in meshes if 'LOD0' in o.name.upper()),meshes[0]))
 data=selected.data.copy();data.transform(selected.matrix_world)
 lo=Vector([min(v.co[i] for v in data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in data.vertices) for i in range(3)])
 center=(lo+hi)/2
 for v in data.vertices:v.co-=Vector((center.x,center.y,lo.z))
 data.materials.clear();data.materials.append(mat)
 active_uv=data.uv_layers.active
 for layer in list(data.uv_layers):
  if layer!=active_uv:data.uv_layers.remove(layer)
 active_uv.name='AtlasUV';active_uv.active_render=True
 # Vendor vegetation tint/white eggplant variants are remapped to matching swatches on the same atlas.
 if 'Cabbage' in file or 'Eggplant' in file:
  uv=data.uv_layers.active
  for poly in data.polygons:
   avg=sum((uv.data[l].uv for l in poly.loop_indices),Vector((0,0)))/len(poly.loop_indices)
   sample=pixels[min(2047,int(avg.y*2048)),min(2047,int(avg.x*2048)),:3]
   if 'Cabbage' in file or not(sample[1]>sample[0]*1.08 and sample[1]>sample[2]*1.08):
    color=swatch((.40,.56,.20)) if 'Cabbage' in file else swatch((.27,.12,.32))
    for l in poly.loop_indices:uv.data[l].uv=color
 # Library sources remain available for editing but are excluded from export/render.
 lib=bpy.data.objects.new(file.replace('.fbx','')+'_Source',data);colls['90 Source Produce Library'].objects.link(lib);lib.hide_render=True;lib.hide_set(True)
 selected_name=selected.name
 for obj in imported:bpy.data.objects.remove(obj,do_unlink=True)
 manifest.append({'file':file,'mesh':selected_name, 'vertices':len(data.vertices),'triangles':sum(len(p.vertices)-2 for p in data.polygons)})
 return data,hi-lo

sources={}
for key,file in [('Tomato','TFP_Tomato_01A.fbx'),('Carrot','TFP_Carrot_01A.fbx'),('Cabbage','TFP_Cabbage_01A.fbx'),('Eggplant','TFP_Eggplant_01A.fbx')]:sources[key]=import_produce(file)
def produce(key,pos,height,tilt=(0,0,0)):
 data,size=sources[key];o=bpy.data.objects.new(key,data);colls['05 Toon Pack Produce'].objects.link(o);o.location=pos;o.scale=(height/max(size.z,.001),)*3;o.rotation_euler=tilt;return o
for row in range(3):
 for col in range(3):produce('Tomato',(centers[0]+(col-1)*.20,-.23+row*.20,1.12),.19,(0,0,random.random()*math.tau))
for row in range(2):
 for col in range(4):
  carrot=produce('Carrot',(centers[1]+(col-1.5)*.14,-.26+row*.21,1.15),.38,(math.radians(-62),random.uniform(-.10,.10),random.uniform(-.14,.14)))
  bpy.context.view_layer.update();tip=carrot.matrix_world@Vector((0,0,sources['Carrot'][1].z))
  for leaf in range(3):
   d=Vector(((leaf-1)*.07,.10,.11+leaf*.02));side=Vector((.017,0,0))
   mesh_obj('Carrot Greens',[tip,tip+d*.55-side,tip+d,tip+d*.55+side],[(0,1,2),(0,2,3),(2,1,0),(3,2,0)],'05 Toon Pack Produce',swatch((.26,.43,.13)))
for x,y in [(-.15,-.15),(.16,-.12),(0,.15)]:produce('Cabbage',(centers[2]+x,y,1.12),.30,(0,0,random.random()*math.tau))
for i in range(5):produce('Eggplant',(centers[3]+(i%3-1)*.18,-.17+(i//3)*.28,1.14),.32,(math.radians(55),0,random.uniform(-.2,.2)))

# Clean editable master and packed atlas; independent combined LODs are export-only.
editable=[o for c in colls.values() if c.name[:2] not in ['90','99'] for o in c.objects if o.type=='MESH']
for obj in editable:
 mesh=obj.data;uv=mesh.uv_layers.active
 if uv is None:continue
 uv.name='AtlasUV';uv.active_render=True
 # Palette-only faces need a tiny UV area for stable normal-map tangents in Unity.
 for poly in mesh.polygons:
  loops=list(poly.loop_indices);coords=[uv.data[l].uv.copy() for l in loops]
  area=sum(coords[i].x*coords[(i+1)%len(coords)].y-coords[(i+1)%len(coords)].x*coords[i].y for i in range(len(coords)))
  if abs(area)>1e-8:continue
  center=sum(coords,Vector((0,0)))/len(coords);points=[mesh.vertices[mesh.loops[l].vertex_index].co for l in loops]
  ranges=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)];axes=sorted(range(3),key=lambda i:ranges[i],reverse=True)[:2]
  mins=[min(p[a] for p in points) for a in axes]
  for l,p in zip(loops,points):uv.data[l].uv=(center.x+((p[axes[0]]-mins[0])/max(ranges[axes[0]],.00001)-.5)*.005,center.y+((p[axes[1]]-mins[1])/max(ranges[axes[1]],.00001)-.5)*.005)
for o in editable:o.select_set(False)
SOURCE.mkdir(parents=True,exist_ok=True);EXPORT.mkdir(parents=True,exist_ok=True)
scene['Atlas']='Shared Wood Atlas; existing colour/normal maps only'
scene['ProduceSource']='Original Toon Farm Pack FBX geometry; vegetation tints adapted to shared atlas palette'
scene['Scale']='Counter 1.02m; overall width 3.56m; canopy 2.8m'
lodcoll=bpy.data.collections.new('95 UNITY EXPORT - Combined LODs');scene.collection.children.link(lodcoll)
copies=[]
for o in editable:
 n=o.copy();n.data=o.data.copy();lodcoll.objects.link(n);n.hide_set(False);n.select_set(True);copies.append(n)
for o in bpy.context.selected_objects:o.select_set(o in copies)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();lod0=bpy.context.object;lod0.name='MarketStand_A_LOD0'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
# Collapse identical material slots so the combined stand renders with a single atlas draw.
lod0.data.materials.clear();lod0.data.materials.append(mat)
for p in lod0.data.polygons:p.material_index=0
lod1=lod0.copy();lod1.data=lod0.data.copy();lodcoll.objects.link(lod1);lod1.name='MarketStand_A_LOD1'
bpy.context.view_layer.objects.active=lod1;mod=lod1.modifiers.new('Mobile distance simplification','DECIMATE');mod.ratio=.53;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
for o in bpy.context.selected_objects:o.select_set(False)
for o in [lod0,lod1]:o.select_set(True)
bpy.context.view_layer.objects.active=lod0
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'MarketStand_A.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
stats={'sourceProduce':manifest,'atlas':'Existing Shared Wood Atlas','lod0Triangles':sum(len(p.vertices)-2 for p in lod0.data.polygons),'lod1Triangles':sum(len(p.vertices)-2 for p in lod1.data.polygons),'materialSlots':len(lod0.data.materials)}
(OUT/'ModelStats.json').write_text(json.dumps(stats,indent=2))
lodcoll.hide_render=True;lodcoll.hide_viewport=True
for o in bpy.context.selected_objects:o.select_set(False)
# Review camera and soft studio light do not export into the game.
review=colls['99 Review Setup']
def area(name,pos,power,size):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);review.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
area('Soft Key',(-3,-4,7),450,5);area('Soft Fill',(4,-1,5),250,4)
data=bpy.data.cameras.new('Review Camera');cam=bpy.data.objects.new('Review Camera',data);review.objects.link(cam);cam.location=(4.6,-6.8,4.0);cam.rotation_euler=(Vector((0,0,1.35))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=5.0;scene.camera=cam
scene.world=bpy.data.worlds.new('Soft review world');scene.world.color=(.32,.32,.32);scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
for area_ in bpy.context.screen.areas if bpy.context.screen else []:
 if area_.type=='VIEW_3D':area_.spaces.active.region_3d.view_distance=6;area_.spaces.active.region_3d.view_location=Vector((0,0,1.3))
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MarketStand_A.blend'))
scene.render.filepath=str(OUT/'Previews/MarketStand_Blender.png');bpy.ops.render.render(write_still=True)
print('MARKET_STAND_COMPLETE',json.dumps(stats))
