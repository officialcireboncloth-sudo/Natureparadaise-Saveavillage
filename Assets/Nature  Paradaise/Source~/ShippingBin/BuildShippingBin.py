"""Native Blender shipping bin: plank chest, articulated lid, corner straps and carved harvest emblem."""
import bpy, bmesh, math, json, random
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
ASSET=ROOT/'Assets/Nature  Paradaise'
OUT=ROOT/'ArtSource/ShippingBin'
SOURCE=ASSET/'Source~/ShippingBin'
EXPORT=ASSET/'mesh/World/ShippingBin'
PACK=ASSET/'Pack/Toon Series/Toon Farm Pack/Models'
(OUT/'Previews').mkdir(parents=True,exist_ok=True)
random.seed(27)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='Shipping Bin - Editable Atlas Master'
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
for name in ['01 Chest Body','02 Corner Metal','03 Carved Harvest Emblem','04 Hinged Lid','05 Hinges','99 Review Setup']:
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
def box(name,loc,size,group='01 Chest Body',style='warm',bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Small softened timber edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,group,style)
def beam(name,a,b,width=.09,depth=None,group='01 Chest Body',style='warm'):
 a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(width,depth or width,(b-a).length),group,style)
 o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def mesh_obj(name,verts,faces,group,style):
 m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);colls[group].objects.link(o);o.data.materials.append(mat);map_uv(o,style);return o

BOLT=swatch((.38,.29,.18))
for x in [-.72,.72]:
 for y in [-.41,.41]:
  box('Dark Corner Upright',(x,y,.44),(.115,.115,.86),style='dark',bevel=.009)
  box('Corner Foot',(x,y,.035),(.13,.13,.07),style='dark',bevel=.008)
for row in range(4):
 z=.17+row*.178
 for y in [-.435,.435]:box('Horizontal Wall Plank',(0,y,z),(1.38,.060,.171),bevel=.006)
 for x in [-.72,.72]:box('Side Wall Plank',(x,0,z),(.060,.76,.171),bevel=.006)
for i in range(4):box('Inside Floor Plank',(-.50+i*.334,0,.105),(.326,.80,.050),style='dark',bevel=.004)
for y in [-.435,.435]:box('Top Rim Long',(0,y,.825),(1.48,.080,.060),bevel=.006)
for x in [-.72,.72]:box('Top Rim End',(x,0,.825),(.080,.84,.060),bevel=.006)

def pin(name,pos,radius,depth,axis,group='02 Corner Metal',style=METAL):
 bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=radius,depth=depth,location=pos)
 obj=bpy.context.object;obj.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();return finish(obj,name,group,style)
for x in [-.72,.72]:
 for y in [-.41,.41]:
  sx=1 if x>0 else -1;sy=1 if y>0 else -1
  for z in [.17,.735]:
   box('Corner Iron Front',(x,y+sy*.069,z),(.16,.023,.13),'02 Corner Metal',METAL,.006)
   box('Corner Iron Side',(x+sx*.069,y,z),(.023,.15,.13),'02 Corner Metal',METAL,.006)
   pin('Corner Rivet',(x,y+sy*.085,z),.016,.009,(0,sy,0),style=BOLT)
   pin('Corner Rivet',(x+sx*.085,y,z),.016,.009,(sx,0,0),style=BOLT)

# A real relief emblem on the front plank: carrot between two leafy root vegetables.
def relief(name,outline,y=-.486,depth=.019,style='warm'):
 n=len(outline);verts=[(x,y,z) for x,z in outline]+[(x,y+depth,z) for x,z in outline]
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 obj=mesh_obj(name,verts,faces,'03 Carved Harvest Emblem',style)
 mod=obj.modifiers.new('Carved softened edge','BEVEL');mod.width=.003;mod.segments=1
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name);map_uv(obj,style);return obj
def leaf(name,start,end,width=.027,y=-.509):
 a,b=Vector(start),Vector(end);d=b-a;side=Vector((-d.y,d.x)).normalized()*width
 points=[a,a+d*.35+side,a+d*.72+side*.6,b,a+d*.72-side*.6,a+d*.35-side]
 return relief(name,[(p.x,p.y) for p in points],y,style='warm')
for cx,cz in [(-.105,.425),(.115,.419)]:
 outline=[(cx+.080*math.sin(i*math.tau/12),cz+.092*math.cos(i*math.tau/12)) for i in range(12)]
 relief('Turnip Shadow',[(cx+(x-cx)*1.08,cz+(z-cz)*1.08) for x,z in outline],-.484,.012,'dark')
 relief('Carved Turnip',outline,-.502,.018)
 for i in range(3):leaf('Turnip Leaves',(cx,cz+.077),(cx+(i-1)*.055,cz+.19-abs(i-1)*.025),.023,-.512-i*.0015)
carrot=[(-.017,.282),(-.067,.487),(-.051,.536),(0,.555),(.055,.533),(.068,.490),(.014,.289)]
relief('Carrot Shadow',[(x*1.12,.43+(z-.43)*1.08) for x,z in carrot],-.501,.014,'dark')
relief('Carved Central Carrot',carrot,-.524,.020)
for i in range(5):leaf('Carrot Leaf Fan',(0,.545),((i-2)*.036,.70-abs(i-2)*.023),.019,-.528-i*.0015)
# Shallow wooden ridges across the root read as a carved carrot even from above.
for i in range(3):beam('Carrot Carving',( -.035+i*.007,-.547,.48-i*.045),(.035-i*.007,-.547,.475-i*.045),.005,.005,'03 Carved Harvest Emblem','dark')

HINGE=Vector((0,.465,.855))
for i in range(5):box('Lid Horizontal Plank',(0,-.416+i*.208,.890),(1.59,.200,.070),'04 Hinged Lid',bevel=.007)
for x in [-.755,.755]:box('Lid Side Edge',(x,0,.900),(.065,1.05,.085),'04 Hinged Lid',bevel=.007)
for x in [-.63,0,.63]:box('Lid Underside Brace',(x,0,.836),(.075,.97,.052),'04 Hinged Lid','dark',.006)
box('Lid Front Lip',(0,-.505,.901),(1.60,.065,.082),'04 Hinged Lid',bevel=.007)
box('Wooden Grip Plaque',(0,-.510,.944),(.34,.11,.045),'04 Hinged Lid','dark',.009)
for x in [-.50,.50]:
 pin('Hinge Barrel',(x,.469,.852),.024,.145,(1,0,0),'05 Hinges')
 box('Back Hinge Plate',(x,.477,.786),(.145,.020,.092),'05 Hinges',METAL,.004)
 box('Lid Hinge Plate',(x,.422,.846),(.145,.085,.018),'04 Hinged Lid',METAL,.004)
 for dx in [-.042,.042]:pin('Hinge Rivet',(x+dx,.491,.786),.009,.008,(0,1,0),'05 Hinges',BOLT)

# Palette patches retain a small, nondegenerate UV area for stable normal-map tangents.
editable=[o for name,c in colls.items() if name!='99 Review Setup' for o in c.objects if o.type=='MESH']
for obj in editable:
 bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
 mesh=obj.data;uv=mesh.uv_layers.active;uv.name='AtlasUV';uv.active_render=True
 for poly in mesh.polygons:
  loops=list(poly.loop_indices);coords=[uv.data[l].uv.copy() for l in loops]
  area=sum(coords[i].x*coords[(i+1)%len(coords)].y-coords[(i+1)%len(coords)].x*coords[i].y for i in range(len(coords)))
  if abs(area)>1e-8:continue
  center=sum(coords,Vector((0,0)))/len(coords);points=[mesh.vertices[mesh.loops[l].vertex_index].co for l in loops]
  ranges=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)];axes=sorted(range(3),key=lambda i:ranges[i],reverse=True)[:2];mins=[min(p[a] for p in points) for a in axes]
  for l,p in zip(loops,points):uv.data[l].uv=(center.x+((p[axes[0]]-mins[0])/max(ranges[axes[0]],.00001)-.5)*.004,center.y+((p[axes[1]]-mins[1])/max(ranges[axes[1]],.00001)-.5)*.004)

exportcol=bpy.data.collections.new('95 UNITY EXPORT - Body and Hinged Lid LODs');scene.collection.children.link(exportcol)
root=bpy.data.objects.new('ShippingBin_Atlas',None);exportcol.objects.link(root)
pivot=bpy.data.objects.new('LidHinge',None);exportcol.objects.link(pivot);pivot.parent=root;pivot.location=HINGE
def combine(parts,name,is_lid=False):
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for obj in parts:
  o=obj.copy();o.data=obj.data.copy();exportcol.objects.link(o);o.select_set(True);copies.append(o)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 obj.data.materials.clear();obj.data.materials.append(mat)
 for p in obj.data.polygons:p.material_index=0
 if is_lid:
  for v in obj.data.vertices:v.co-=HINGE
  obj.parent=pivot;obj.location=(0,0,0)
 else:obj.parent=root
 return obj
lid_parts=list(colls['04 Hinged Lid'].objects)
body=combine([o for o in editable if o not in lid_parts],'ShippingBin_Body_LOD0')
lid=combine(lid_parts,'ShippingBin_Lid_LOD0',True)
def distant(obj,name):
 o=obj.copy();o.data=obj.data.copy();exportcol.objects.link(o);o.name=name;bpy.context.view_layer.objects.active=o
 m=o.modifiers.new('Mobile far LOD','DECIMATE');m.ratio=.55;m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name);return o
body1=distant(body,'ShippingBin_Body_LOD1');lid1=distant(lid,'ShippingBin_Lid_LOD1')
for o in bpy.context.selected_objects:o.select_set(False)
for o in [root,pivot,body,lid,body1,lid1]:o.select_set(True)
EXPORT.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'ShippingBin_A.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
stats={'nearTriangles':sum(len(p.vertices)-2 for o in [body,lid] for p in o.data.polygons),'farTriangles':sum(len(p.vertices)-2 for o in [body1,lid1] for p in o.data.polygons),'material':'Existing Shared Wood Atlas (Toon Farm based)','hingeBlenderXYZ':list(HINGE),'closedBodyDimensions':[1.60,1.05,.95]}
(OUT/'ModelStats.json').write_text(json.dumps(stats,indent=2));exportcol.hide_render=True;exportcol.hide_viewport=True
# The editable source opens in the same raised-lid pose as the user's reference.
sourcepivot=bpy.data.objects.new('Editable Lid Hinge - closed 0 open -78deg',None);colls['04 Hinged Lid'].objects.link(sourcepivot);sourcepivot.location=HINGE
bpy.context.view_layer.update()
for obj in lid_parts:
 matrix=obj.matrix_world.copy();obj.parent=sourcepivot;obj.matrix_world=matrix
sourcepivot.rotation_euler.x=math.radians(-78)
review=colls['99 Review Setup']
def area(name,pos,power,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);review.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.85))-o.location).to_track_quat('-Z','Y').to_euler()
area('Soft Key',(-3,-4,5),430,4);area('Soft Fill',(3,0,4),250,4)
d=bpy.data.cameras.new('Review Camera');cam=bpy.data.objects.new('Review Camera',d);review.objects.link(cam);cam.location=(2.6,-3.7,2.5);cam.rotation_euler=(Vector((0,0,.89))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=2.85;scene.camera=cam
scene.world=bpy.data.worlds.new('Soft review world');scene.world.color=(.35,.35,.35);scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=1050;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True;scene.view_settings.view_transform='Standard'
scene['Units']='Metres; body about 1.6m wide x .9m high';scene['Atlas']='Existing shared Toon Farm wood atlas; packed colour image';scene['Export']='Closed lid at zero; two body/lid LOD pairs; hinge pivot at rear edge'
bpy.ops.object.select_all(action='DESELECT');bpy.data.orphans_purge(do_recursive=True);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ShippingBin_A.blend'))
scene.render.filepath=str(OUT/'Previews/ShippingBin-Blender-Open.png');bpy.ops.render.render(write_still=True)
sourcepivot.rotation_euler.x=0;scene.render.filepath=str(OUT/'Previews/ShippingBin-Blender-Closed.png');bpy.ops.render.render(write_still=True)
print('SHIPPING_BIN_COMPLETE',json.dumps(stats))


