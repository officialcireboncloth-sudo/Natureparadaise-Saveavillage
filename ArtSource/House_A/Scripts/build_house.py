"""Blender 4.5 house masters from approved coordinate manifest. No Unity mutations."""
from pathlib import Path
import bpy,bmesh,json,math,hashlib,itertools,re,sys
from mathutils import Vector

ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
S=json.loads((ART/'References/House_A_Approved.json').read_text(encoding='utf-8'))
PAL=S['materials']['swatches'];SLOTS=S['materials']['productionSlotByPalette']
assert hashlib.sha256((ROOT/S['materials']['atlasPath']).read_bytes()).hexdigest()==S['materials']['atlasSha256']
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0

def convert(p):return Vector((p[0],-p[2],p[1]))
def coll(name,scene,parent=None):
 c=bpy.data.collections.new(name);(parent or scene.collection).children.link(c);return c

atlas=bpy.data.images.load(str(ART/'References/TFP_Atlas_1A_BlenderPreview.png'));atlas.name='Toon Farm Pack existing atlas';atlas.pack()
atlas['SourcePSD']=S['materials']['atlasPath'];atlas['UnityGUID']=S['materials']['atlasGuid']
def material(name,emissive=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(.7,.7,.7,1)
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.92
 bs.inputs['Specular IOR Level'].default_value=.05
 tx=m.node_tree.nodes.new('ShaderNodeTexImage');tx.image=atlas;tx.interpolation='Linear';tx.extension='EXTEND'
 m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
 if emissive:
  m.node_tree.links.new(tx.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.7
 m['UnitySharedMaterial']=name+'.mat';m['BlenderShader']='Preview only; remap to existing Unity material'
 return m
MATS=[material('TFP_Atlas_1A'),material('TFP_Atlas_Lights_1A',True)]

class Builder:
 def __init__(self):self.verts=[];self.faces=[];self.colors=[];self.ids=[];self.slots=[]
 def face(self,vs,color,pid=-1,normal=None):
  vs=[Vector(v) for v in vs]
  if normal is not None and (vs[1]-vs[0]).cross(vs[2]-vs[0]).dot(Vector(normal))<0:vs.reverse()
  off=len(self.verts);self.verts.extend(convert(v) for v in vs);self.faces.append(tuple(range(off,off+len(vs))))
  self.colors.append(color);self.ids.append(pid);self.slots.append(SLOTS[color])
 def box(self,p,pid,flatten=False):
  c=Vector(p['centerXYZ']);e=Vector(p['sizeXYZ'])/2
  vs=[c+Vector(t)*e for t in itertools.product([-1,1],repeat=3)]
  patterns=[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]
  if flatten:
   # Facade plane for distant window/frame; same outer position, not an arbitrary decimation.
   axis=min(range(3),key=lambda i:p['sizeXYZ'][i]);name=p['name']
   sign=-1 if 'rear' in name or 'left' in name else 1
   if axis==0: ids=patterns[1 if sign>0 else 0]
   elif axis==2: ids=patterns[5 if sign>0 else 4]
   else: ids=patterns[3 if sign>0 else 2]
   n=Vector((0,0,0));n[axis]=sign
   self.face([vs[i] for i in ids],p['material'],pid,n);return
  for idx in patterns:
   face=[vs[i] for i in idx];n=sum(face,Vector())/len(face)-c
   self.face(face,p['material'],pid,n)
 def beam(self,p,pid):
  a=Vector(p['fromXYZ']);b=Vector(p['toXYZ']);axis=(b-a).normalized()
  ref=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((0,0,1));side=axis.cross(ref).normalized();up=axis.cross(side)
  vs=[q+side*t*p['width']/2+up*u*p['depth']/2 for q in [a,b] for t,u in itertools.product([-1,1],repeat=2)]
  center=(a+b)/2
  for idx in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:
   face=[vs[i] for i in idx];self.face(face,p['material'],pid,sum(face,Vector())/4-center)
 def object(self,name,col,slots=2,bevel=False):
  mesh=bpy.data.meshes.new(name+'Mesh');mesh.from_pydata(self.verts,[],self.faces);mesh.update()
  for m in MATS[:slots]:mesh.materials.append(m)
  uv=mesh.uv_layers.new(name='ToonAtlasUV')
  attr=mesh.attributes.new('ApprovedPartIndex','INT','FACE');palattr=mesh.attributes.new('PaletteIndex','INT','FACE')
  for i,p in enumerate(mesh.polygons):
   color=self.colors[i];p.material_index=self.slots[i];attr.data[i].value=self.ids[i];palattr.data[i].value=list(PAL).index(color)
   u0,v0,u1,v1=PAL[color]['safeUvBottomLeft'];u=(u0+u1)/2;v=(v0+v1)/2
   coords=[(u,v)]*len(p.loop_indices)
   for j,k in enumerate(p.loop_indices):uv.data[k].uv=coords[j%len(coords)]
  obj=bpy.data.objects.new(name,mesh);col.objects.link(obj)
  if bevel:
   bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bm.to_mesh(mesh);bm.free();mesh.update()
   bpy.context.view_layer.objects.active=obj;obj.select_set(True)
   mod=obj.modifiers.new('Soft exposed timber - one segment','BEVEL');mod.width=.025;mod.segments=1;mod.limit_method='ANGLE';mod.angle_limit=.65
   bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False)
   # Bevel copies palette face attributes; every new face remains inside its own swatch.
   uv=obj.data.uv_layers.active;att=obj.data.attributes.get('PaletteIndex')
   for p in obj.data.polygons:
    color=self.colors[0];att.data[p.index].value=list(PAL).index(color);a,b,c,d=PAL[color]['safeUvBottomLeft']
    for k in p.loop_indices:uv.data[k].uv=((a+c)/2,(b+d)/2)
  return obj

def expected_normal(p,families,m):
 vs=[Vector(v) for v in p['verticesXYZ']];name=p['name']
 if name.startswith('skylight_') and p['material']=='window':return (0,1,0)
 if p['group']=='roof':return (0,1,0)
 if name.endswith('_gable'):
  if name.startswith(('porch','dormer')):
   zfront=m['porch']['depth'] if name.startswith('porch') else -1
   return (0,0,1 if abs(vs[0].z-zfront)<.001 else -1)
  cx=m['additionalFeatures']['wing']['centerX'] if name.startswith('right_wing') else 0
  return (1 if vs[0].x>cx else -1,0,0)
 if p['group']=='sunroom':
  if 'floor' in name or 'roof' in name:return (0,1,0)
  c=sum(vs,Vector())/len(vs);return c-Vector((m['mainBodyWD'][0]/2+.75,c.y,-3.6))
 if name.endswith('_top'):return (0,1,0)
 if p['group']=='decor':
  key=re.sub(r'_[0-5]{3}$','',name) if len(vs)==3 else re.sub(r'_\d+$','',name)
  center=families[key];c=sum(vs,Vector())/len(vs)
  return c-center
 return None

def family_centers(parts):
 grouped={}
 for p in parts:
  if p['type']!='surface' or p['group']!='decor':continue
  name=p['name'];key=re.sub(r'_[0-5]{3}$','',name) if len(p['verticesXYZ'])==3 else re.sub(r'_\d+$','',name)
  if name.endswith('_top'):key=name[:-4]
  grouped.setdefault(key,[]).extend(p['verticesXYZ'])
 return {k:Vector(tuple((min(v[i] for v in vs)+max(v[i] for v in vs))/2 for i in range(3))) for k,vs in grouped.items()}

def roof_reduction(parts,lod):
 groups={}
 for p in parts:
  match=re.match(r'(.+)_slate_(-?1)_(\d+)_(\d+)$',p['name'])
  if match:
   key=(match[1],match[2],match[3] if lod==1 else 'all');groups.setdefault(key,[]).append(p)
 out=[]
 for (prefix,sign,row),ps in groups.items():
  # A strip/plane spans precisely the first and last approved corners.
  ps.sort(key=lambda p:[int(v) for v in p['name'].rsplit('_',2)[1:]])
  first,last=ps[0],ps[-1]
  if lod==1:vs=[first['verticesXYZ'][0],last['verticesXYZ'][1],last['verticesXYZ'][2],first['verticesXYZ'][3]]
  else:
   lastrow=int(last['name'].rsplit('_',2)[1]);lastps=[p for p in ps if int(p['name'].rsplit('_',2)[1])==lastrow]
   lastps.sort(key=lambda p:int(p['name'].rsplit('_',1)[1]));topps=[p for p in ps if p['name'].rsplit('_',2)[1]=='0'];topps.sort(key=lambda p:int(p['name'].rsplit('_',1)[1]))
   vs=[topps[0]['verticesXYZ'][0],topps[-1]['verticesXYZ'][1],lastps[-1]['verticesXYZ'][2],lastps[0]['verticesXYZ'][3]]
  out.append({'name':prefix+'_reduced_'+sign+'_'+row,'type':'surface','verticesXYZ':vs,'material':'roof','group':'roof'})
 return out

def build_pieces(m,lod,col):
 parts=m['parts'];centers=family_centers(parts);builders={};beveled={};slots=2
 def target(p,soft=False):
  store=beveled if soft else builders;key=p['group']+'_'+p['material'] if soft else p['group']
  if key not in store:store[key]=Builder()
  return store[key]
 for pid,p in enumerate(parts):
  name=p['name'];typ=p['type'];group=p['group']
  # Back porch edge must join the sloping main roof, not leave a floating rear barge.
  if name=='porch_gable' and p['verticesXYZ'][0][2]<0:continue
  if name=='porch_barge' and p['fromXYZ'][2]<0 and p['toXYZ'][2]<0:continue
  if lod and '_slate_' in name:continue
  if lod and '_rowlip' in name:continue
  if lod>=1 and group=='decor' and re.search(r'_flower[134]',name):continue
  if lod==1 and group=='foundation' and pid%2:continue
  if lod==2:
   if group=='foundation':continue
   if group=='decor' and (typ=='surface' or 'hanging' in name or 'lantern_frame' in name):continue
   if 'louver' in name or 'brick_course' in name or 'handle' in name or 'mullion' in name or 'transom' in name:continue
   if 'porch_brace' in name or 'porch_gable_brace' in name:continue
   if group=='windows' and name.endswith(('_vertical','_horizontal')):
    # Only two outer rails/jambs are retained; discard internal crossbars.
    c=p['centerXYZ']
    if name.endswith('_vertical'):
     family=[q for q in parts if q['name']==name];axis=2 if 'left' in name or 'right' in name else 0
     middle=sorted(q['centerXYZ'][axis] for q in family)[1]
     if abs(c[axis]-middle)<1e-6:continue
    else:
     family=[q for q in parts if q['name']==name];middle=sorted(q['centerXYZ'][1] for q in family)[1]
     if abs(c[1]-middle)<1e-6:continue
  # Preserve all silhouette positions. Bevel removes corners inwards, never expands them.
  soft=lod==0 and typ=='box' and min(p['sizeXYZ'])>=.18 and p['material'] in ['trim','stone','stone_light'] and group!='foundation'
  b=target(p,soft)
  if typ=='box':
   if name=='chimney_flue':
    c=Vector(p['centerXYZ']);sz=p['sizeXYZ'];thick=.07
    for axis in [0,2]:
     for sign in [-1,1]:
      cc=c.copy();cc[axis]+=sign*(sz[axis]-thick)/2;ss=sz.copy();ss[axis]=thick
      b.box(dict(centerXYZ=list(cc),sizeXYZ=ss,material='metal',name=name),pid)
   elif group=='windows' and p['material']=='trim' and name.endswith(('_vertical','_horizontal')):
    # Non-overlapping window grid: continuous top/bottom rails, short jambs,
    # split central upright and inset middle rail. Outer silhouette unchanged.
    side='left' in name or 'right' in name;axis=2 if side else 0
    family=[q for q in parts if q['name']==name]
    if name.endswith('_vertical'):
     middle=sorted(q['centerXYZ'][axis] for q in family)[1]
     pp=dict(p);pp['sizeXYZ']=p['sizeXYZ'].copy();pp['sizeXYZ'][1]-=.28
     if abs(p['centerXYZ'][axis]-middle)<1e-6:
      h=(pp['sizeXYZ'][1]-.14)/2
      for sign in [-1,1]:
       qq=dict(pp);qq['centerXYZ']=p['centerXYZ'].copy();qq['centerXYZ'][1]+=sign*(h+.14)/2
       qq['sizeXYZ']=pp['sizeXYZ'].copy();qq['sizeXYZ'][1]=h;b.box(qq,pid,flatten=lod==2)
     else:b.box(pp,pid,flatten=lod==2)
    else:
     middle=sorted(q['centerXYZ'][1] for q in family)[1];pp=dict(p);pp['sizeXYZ']=p['sizeXYZ'].copy()
     if abs(p['centerXYZ'][1]-middle)<1e-6:pp['sizeXYZ'][axis]-=.28
     b.box(pp,pid,flatten=lod==2)
   else:b.box(p,pid,flatten=lod==2 and group=='windows')
  elif typ=='beam':b.beam(p,pid)
  else:b.face(p['verticesXYZ'],p['material'],pid,expected_normal(p,centers,m))
 for p in roof_reduction(parts,lod) if lod else []:target(p).face(p['verticesXYZ'],p['material'],-1,(0,1,0))
 # Close the porch/main roof junction within approved visual envelope. The
 # original front gable and eaves stay in place; hidden rear roof extends to intersection.
 pw=m['porch']['width'];pd=m['porch']['depth'];pr=m['porch']['ridgeY'];pe=m['porch']['eaveY']
 zback=-1.05;slope=(m['ridgeY']-m['eaveY'])/(m['mainBodyWD'][1]/2)
 roofY=m['eaveY']-zback*slope+.04;cut=(pr+.035-roofY)*(pw/2)/(pr-pe)
 zi=-(pr+.035-m['eaveY']-.04)/slope
 connector=Builder()
 for sign in [-1,1]:connector.face([(0,pr+.035,zback),(sign*cut,roofY,zback),(0,pr+.035,zi)],'roof',-1,(0,1,0))
 connector.beam(dict(fromXYZ=[0,pr+.12,zback],toXYZ=[0,pr+.12,zi],width=.22,depth=.22,material='roof'),-1)
 builders['roof_junction']=connector
 if m['id']=='Lv5':
  # Warm window backdrop only, behind the glass bay; no new gameplay interior.
  x=m['mainBodyWD'][0]/2+.028;z=-3.6;y=2.7;back=Builder()
  back.face([(x,y-1.1,z-1.2),(x,y-1.1,z+1.2),(x,y+1.1,z+1.2),(x,y+1.1,z-1.2)],'window',-1,(1,0,0))
  for yy in [y-1.19,y+1.19,y]:back.box(dict(centerXYZ=[x+.04,yy,z],sizeXYZ=[.12,.14,2.6],material='trim',name='bay_backdrop'),-1)
  for zz in [z-1.29,z+1.29]:back.box(dict(centerXYZ=[x+.04,y,zz],sizeXYZ=[.12,2.24,.14],material='trim',name='bay_backdrop'),-1)
  builders['bay_warm_backdrop']=back
 objs=[b.object(m['id']+'_'+g,col,slots) for g,b in builders.items()]
 objs += [b.object(m['id']+'_'+g+'_soft',col,slots,True) for g,b in beveled.items()]
 return objs

def combine(objects,name,col):
 copies=[]
 for o in objects:
  clone=bpy.data.objects.new(name+'_piece',o.data.copy());col.objects.link(clone);copies.append(clone)
 bpy.ops.object.select_all(action='DESELECT')
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
 bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
 bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=list(bm.edges))
 zero=[f for f in bm.faces if f.calc_area()<1e-10]
 if zero:bmesh.ops.delete(bm,geom=zero,context='FACES')
 bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
 obj['ApprovedOrigin']='Unity main front wall center at ground';obj['UnityFront']='+Z';obj['BlenderFront']='-Y'
 obj['ModelledFrom']='House_A_Approved.json';obj['LOD']=int(name[-1]);return obj

world=bpy.data.worlds.new('House studio neutral daylight');world.use_nodes=True
world.node_tree.nodes.get('Background').inputs[0].default_value=(.75,.80,.86,1)
world.node_tree.nodes.get('Background').inputs[1].default_value=.55
floor_mat=bpy.data.materials.new('PREVIEW ONLY floor');floor_mat.use_nodes=True
floor_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.76,.72,.63,1)
floor_mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1

def scene_setup(scene):
 scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.world=world
 scene.render.engine='CYCLES';scene.cycles.samples=20;scene.cycles.use_denoising=True
 scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
 scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
 scene['Scope']='Approved Blender exterior. No Unity integration in this task.'

def studio_for(scene,m):
 col=coll('03 PREVIEW STUDIO '+m['id'],scene)
 mesh=bpy.data.meshes.new('StudioFloorMesh');mesh.from_pydata([(-80,-80,-.025),(80,-80,-.025),(80,80,-.025),(-80,80,-.025)],[],[(0,1,2,3)])
 obj=bpy.data.objects.new('Studio floor - excluded from FBX',mesh);col.objects.link(obj);mesh.materials.append(floor_mat)
 center=Vector(((m['visualBounds']['minXYZ'][0]+m['visualBounds']['maxXYZ'][0])/2,3,3.5))
 for name,position,power,size in [('Key',(-10,-14,18),2200,10),('Fill',(14,-6,12),1600,9),('Rim',(0,14,15),2100,8)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
  light=bpy.data.objects.new('Preview '+name,data);col.objects.link(light);light.location=position;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 data=bpy.data.cameras.new('House '+m['id']+' Camera');data.type='ORTHO';cam=bpy.data.objects.new(data.name,data);col.objects.link(cam);scene.camera=cam
 return cam

def camera_aim(cam,m,side):
 q=m['visualBounds'];center=convert([(q['minXYZ'][0]+q['maxXYZ'][0])/2,q['sizeWDH'][2]*.40,(q['minXYZ'][2]+q['maxXYZ'][2])/2])
 direction={'FrontRight':Vector((1,-1,.85)), 'RearLeft':Vector((-1,1,.85)), 'Front':Vector((0,-1,.0)), 'Right':Vector((1,0,0)), 'Rear':Vector((0,1,0)), 'Left':Vector((-1,0,0))}[side]
 cam.location=center+direction*22;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 cam.data.ortho_scale=max(q['sizeWDH'][0],q['sizeWDH'][1])*1.24+2.5

scenes={};masters={};audit=[]
for index,m in enumerate(S['models']):
 key=m['id'];scene=bpy.context.scene if index==0 else bpy.data.scenes.new('House A '+key)
 scene.name='House A '+key;scene_setup(scene);bpy.context.window.scene=scene
 editable=coll('01 EDITABLE SOURCE '+key,scene);ex=coll('02 COMBINED LODS '+key,scene)
 markers=coll('04 MARKERS '+key,scene);markers.hide_render=True
 for n,p in [('Door ground origin',[0,0,0]),('Threshold',m['entry']['thresholdXYZ']),('Approach',m['entry']['approachXYZ'])]:
  o=bpy.data.objects.new(key+' '+n,None);markers.objects.link(o);o.location=convert(p);o.empty_display_size=.25;o['UnityXYZ']=p
 objects=build_pieces(m,0,editable);lods=[]
 for lod in [0,1,2]:
  if lod:temp=coll('TEMP '+key+' '+str(lod),scene);pieces=build_pieces(m,lod,temp)
  else:pieces=objects
  name='House_A_'+key.replace('-','_')+'_LOD'+str(lod);obj=combine(pieces,name,ex);lods.append(obj)
  if lod:
   for p in pieces:bpy.data.objects.remove(p,do_unlink=True)
   bpy.data.collections.remove(temp)
  count=len(obj.data.polygons);limit=m['budgetTriangles']['LOD0Limit'] if lod==0 else m['budgetTriangles']['LOD'+str(lod)+'Target']
  print('HOUSE_MESH '+name+' triangles='+str(count)+' limit='+str(limit),flush=True)
  assert count<=limit,(name,count,limit)
  bounds=[[min(v.co[i] for v in obj.data.vertices) for i in range(3)],[max(v.co[i] for v in obj.data.vertices) for i in range(3)]]
  # Convert actual corners back to Unity; -Y reverses min/max.
  unitylo=[bounds[0][0],bounds[0][2],-bounds[1][1]];unityhi=[bounds[1][0],bounds[1][2],-bounds[0][1]]
  # Distant LOD removes flower tips; structural dimensions/entry do not change.
  tolerance=.051 if lod==0 else .13
  assert all(abs(unitylo[i]-m['visualBounds']['minXYZ'][i])<tolerance and abs(unityhi[i]-m['visualBounds']['maxXYZ'][i])<tolerance for i in range(3)),(name,unitylo,unityhi)
  assert all(p.area>1e-10 for p in obj.data.polygons),(name,'degenerate face')
  # Export selected single building; no floor/camera/light/markers and no image embedding.
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  orig=obj.name;obj.name='House_A_'+key.replace('-','_')+'_'+['High','Medium','Low'][lod]
  path=ART/'Exports'/(orig+'.fbx')
  bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},global_scale=1,
   apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',mesh_smooth_type='FACE',
   add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
  obj.name=orig;obj.hide_render=lod!=0;obj.hide_set(lod!=0)
  audit.append(dict(model=key,lod=lod,name=orig,triangles=count,vertices=len(obj.data.vertices),boundsUnityXYZ=[unitylo,unityhi],
    materials=[q.name for q in obj.data.materials],export='Exports/'+path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 editable.hide_render=True;editable.hide_viewport=True;markers.hide_viewport=True
 cam=studio_for(scene,m);camera_aim(cam,m,'FrontRight');scenes[key]=scene;masters[key]=lods
 if '--skip-renders' not in sys.argv:
  for side in ['FrontRight','RearLeft']:
   camera_aim(cam,m,side);scene.render.filepath=str(ART/'Previews'/f'House_A_{key}_{side}.png');bpy.ops.render.render(write_still=True)
  camera_aim(cam,m,'FrontRight')

# One comparison scene with five instances; Lv3 and Lv4 share the same mesh datablock.
comparison=bpy.data.scenes.new('House A Levels 1-5');scene_setup(comparison)
instances=coll('Level instances - four unique meshes',comparison)
for lv in range(1,6):
 key=S['levelToModel'][str(lv)];data=masters[key][0].data
 o=bpy.data.objects.new('PlayerHouse_Lv'+str(lv)+'_Preview',data);instances.objects.link(o);o.location=((lv-3)*23,0,0)
 o['Level']=lv;o['SharedModel']=key;o['UniformScale']=1
camdata=bpy.data.cameras.new('Level comparison camera');camdata.type='ORTHO';camdata.ortho_scale=121
cam=bpy.data.objects.new(camdata.name,camdata);comparison.collection.objects.link(cam);cam.location=(22,-55,46)
cam.rotation_euler=(Vector((0,3,3))-cam.location).to_track_quat('-Z','Y').to_euler();comparison.camera=cam
comparison.render.resolution_x=2600;comparison.render.resolution_y=1050
for o in scenes['Lv5'].objects:
 if o.name.startswith(('Preview ','Studio floor')):comparison.collection.objects.link(o)
if '--skip-renders' not in sys.argv:
 comparison.render.filepath=str(ART/'Previews/House_A_Levels_1_to_5.png');bpy.ops.render.render(write_still=True,scene=comparison.name)

bpy.context.window.scene=scenes['Lv5'];bpy.ops.object.select_all(action='DESELECT')
active=masters['Lv5'][0];active.select_set(True);bpy.context.view_layer.objects.active=active
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
   area.spaces.active.overlay.show_overlays=False
report={'status':'MODEL_BUILT_EXPORTED_NOT_APPLIED_TO_UNITY','approvedSpecificationSha256':hashlib.sha256((ART/'References/House_A_Approved.json').read_bytes()).hexdigest(),
 'blenderVersion':bpy.app.version_string,'models':audit,'uniqueDesigns':4,'levelMapping':S['levelToModel'],'level3And4ShareMesh':True,
 'runtimeMaterials':['TFP_Atlas_1A.mat','TFP_Atlas_Lights_1A.mat'],'newRuntimeTextures':0,
 'packedBlenderPreviewImages':1,'sunroomTreatment':'Solid blue opaque from existing atlas',
 'unityAssetsModified':False,'frontConvention':'Unity +Z / Blender -Y'}
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for mesh in list(bpy.data.meshes):
 if mesh.users==0:bpy.data.meshes.remove(mesh)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
print('HOUSE_BUILD_COMPLETE',flush=True)
