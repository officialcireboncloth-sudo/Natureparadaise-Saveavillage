def coll(name,scene,parent=None):
 c=bpy.data.collections.new(name);(parent or scene.collection).children.link(c);return c

def setup(scene):
 scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
 scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True
 scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard'
 scene.world=bpy.data.worlds.new(scene.name+' World');scene.world.use_nodes=True
 scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.78,.83,.9,1)
 scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
 scene['Approval']='User approved Chicken Store sketch 2026-10-14';scene['Front']='-Y';scene['Units']='metres'

def activate(scene):bpy.context.window.scene=scene

class Build:
 def __init__(self):self.v=[];self.f=[];self.col=[]
 def face(self,vs,color,normal=None):
  vs=[Vector(v) for v in vs]
  if normal is not None and (vs[1]-vs[0]).cross(vs[2]-vs[0]).dot(Vector(normal))<0:vs.reverse()
  start=len(self.v);self.v.extend(vs);self.f.append(tuple(range(start,start+len(vs))));self.col.append((color,random.uniform(-.3,.3)))
 def solid(self,vs,faces,color):
  vs=[Vector(v) for v in vs];ctr=sum(vs,Vector())/len(vs)
  for inds in faces:
   points=[vs[i] for i in inds];self.face(points,color,sum(points,Vector())/len(points)-ctr)
 def box(self,c,size,color):
  c=Vector(c);e=Vector(size)/2
  vs=[c+Vector((x*e.x,y*e.y,z*e.z)) for x in (-1,1) for y in (-1,1) for z in (-1,1)]
  self.solid(vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],color)
 def beam(self,a,b,width,depth,color):
  a=Vector(a);b=Vector(b);axis=(b-a).normalized();ref=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))
  side=axis.cross(ref).normalized();up=axis.cross(side).normalized()
  vs=[p+side*s*width/2+up*t*depth/2 for p in (a,b) for s in (-1,1) for t in (-1,1)]
  self.solid(vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],color)
 def extrude(self,poly,y,depth,color):
  # polygon is X/Z cross-section, thickness along Y
  n=len(poly);vs=[(x,y+d,z) for d in (-depth/2,depth/2) for x,z in poly]
  faces=[tuple(range(n)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  self.solid(vs,faces,color)
 def obj(self,name,col,bevel=0):
  me=bpy.data.meshes.new(name+'Mesh');me.from_pydata(self.v,[],self.f);me.update();me.materials.append(mat)
  uv=me.uv_layers.new(name='ToonAtlasUV');attr=me.attributes.new('PaletteID','INT','FACE');keys=list(PAL)
  for i,p in enumerate(me.polygons):
   color,var=self.col[i];a,b,c,d=PAL[color]['uvSafeRectBottomLeft'];u=(a+c)/2;v=(b+d)/2+var*(d-b)*.25
   attr.data[i].value=keys.index(color)
   for idx in p.loop_indices:uv.data[idx].uv=(u,v)
  ob=bpy.data.objects.new(name,me);col.objects.link(ob)
  if bevel:
   bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
   mod=ob.modifiers.new('Soft chunky edges','BEVEL');mod.width=bevel;mod.segments=1
   bpy.ops.object.modifier_apply(modifier=mod.name);ob.select_set(False);me=ob.data
   # Reassign bevel UV from inherited palette IDs.
   uv=me.uv_layers.active;attr=me.attributes.get('PaletteID')
   for p in me.polygons:
    color=keys[attr.data[p.index].value];a,b,c,d=PAL[color]['uvSafeRectBottomLeft']
    for idx in p.loop_indices:uv.data[idx].uv=((a+c)/2,(b+d)/2)
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
  ob['AtlasGUID']=BASE['materials']['atlasGuid'];return ob

def empty(name,pos,col):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.location=pos;o.empty_display_type='PLAIN_AXES';o.empty_display_size=.15;bpy.context.view_layer.update();return o

def join_copy(obs,name,col):
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for o in obs:
  cp=o.copy();cp.data=o.data.copy();col.objects.link(cp);cp.select_set(True);copies.append(cp)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces))
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if (f.verts[1].co-f.verts[0].co).cross(f.verts[2].co-f.verts[0].co).length<2e-10],context='FACES')
 bm.to_mesh(o.data);bm.free();o.data.update()
 o.select_set(False);return o

def export(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 # A second, unparented empty keeps Unity's generated file root at identity.
 # Axis conversion stays on authored child nodes, including gate hinges.
 axis_reference=bpy.data.objects.new('FBX_AxisReference',None);bpy.context.scene.collection.objects.link(axis_reference)
 axis_reference.empty_display_size=.01;axis_reference.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/f'{name}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
  global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
  use_mesh_modifiers=True,mesh_smooth_type='FACE',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 bpy.data.objects.remove(axis_reference,do_unlink=True)
 bpy.ops.object.select_all(action='DESELECT')

def studio(scene):
 c=coll('90 PREVIEW ONLY',scene)
 fm=bpy.data.materials.get('PREVIEW Floor') or bpy.data.materials.new('PREVIEW Floor');fm.diffuse_color=(.65,.69,.65,1)
 bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.02));o=bpy.context.object
 for old in list(o.users_collection):old.objects.unlink(o)
 c.objects.link(o);o.data.materials.append(fm);o.name='Studio floor - excluded export'
 for name,loc,power,size in [('Key',(-7,-10,13),1700,8),('Fill',(9,-2,10),1100,7),('Rim',(0,8,11),1400,6)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size;o=bpy.data.objects.new(name,data);c.objects.link(o);o.location=loc
  o.rotation_euler=(Vector((0,0,2.5))-o.location).to_track_quat('-Z','Y').to_euler()
 cam=bpy.data.objects.new('Preview Camera',bpy.data.cameras.new('Preview Camera'));c.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';return cam

def render(scene,cam,name,pos,target,scale):
 activate(scene);cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 scene.render.filepath=str(ART/'Previews'/f'{name}.png')
 if '--skip-renders' not in sys.argv:bpy.ops.render.render(write_still=True)