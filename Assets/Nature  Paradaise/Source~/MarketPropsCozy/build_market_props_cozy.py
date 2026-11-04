"""Rework the actual stand/bin masters using the shared Cozy atlas; retain genuine Toon Farm produce."""
from pathlib import Path
import bpy, bmesh, math, json, random
from mathutils import Vector, Matrix
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise';OUT=ROOT/'ArtSource/MarketPropsCozy';OUT.mkdir(parents=True,exist_ok=True)
HELP=(A/'Source~/HouseCozy/build_house_cozy.py').read_text().split('# Ground, body and readable exposed framing.')[0]

def begin(kind):
 exec(compile(HELP,__file__,'exec'),globals());random.seed(1102)
 scene.name=kind+' - Cozy Editable Atlas Master'
 old=A/f'Source~/MarketPropsCozy/Library/{kind}_Base.blend'
 with bpy.data.libraries.load(str(old),link=False) as (data,target):
  target.collections=[n for n in data.collections if n[:2].isdigit() and int(n[:2])<90]
 for c in target.collections:source.children.link(c)
 bpy.context.view_layer.update()
 return target.collections

def uv_rect(obj,slot):
 assign(obj,slot)
 # Small objects and palette regions use a compact inset; UVs remain nondegenerate.
 if slot in ('Rope','Burlap','FabricSage','FabricCream'):
  uv=obj.data.uv_layers.active;r=region(slot);cu=(r[0]+r[2])/2;cvv=(r[1]+r[3])/2
  for d in uv.data:d.uv=(cu+(d.uv.x-cu)*.35,cvv+(d.uv.y-cvv)*.35)

def remap_produce(obj):
 oldmat=obj.data.materials[0]
 tx=next(n.image for n in oldmat.node_tree.nodes if n.type=='TEX_IMAGE')
 import numpy as np
 if tx.name not in old_pixels:old_pixels[tx.name]=np.array(tx.pixels[:]).reshape(tx.size[1],tx.size[0],4)
 pixels=old_pixels[tx.name];uv=obj.data.uv_layers.active;uv.name='CozyAtlasUV'
 for other in list(obj.data.uv_layers):
  if other!=uv:obj.data.uv_layers.remove(other)
 slots=[]
 for p in obj.data.polygons:
  avg=sum((uv.data[l].uv for l in p.loop_indices),Vector((0,0)))/len(p.loop_indices)
  rgb=pixels[min(tx.size[1]-1,max(0,int(avg.y*tx.size[1]))),min(tx.size[0]-1,max(0,int(avg.x*tx.size[0]))),:3]
  green=rgb[1]>rgb[0]*1.1 and rgb[1]>rgb[2]*1.1
  slot='LeafGreen' if green or 'Cabbage' in obj.name or 'Greens' in obj.name else ('FoodOrange' if 'Carrot' in obj.name else 'FoodPurple' if 'Eggplant' in obj.name else 'FoodRed')
  slots.append(slot)
 obj.data.materials.clear();obj.data.materials.append(M[0])
 for p,slot in zip(obj.data.polygons,slots):
  r=region(slot);points=[obj.data.vertices[obj.data.loops[l].vertex_index].co for l in p.loop_indices]
  axes=sorted(range(3),key=lambda j:max(v[j] for v in points)-min(v[j] for v in points),reverse=True)[:2]
  lo=[min(v[j] for v in points) for j in axes];span=[max(v[j] for v in points)-lo[i] for i,j in enumerate(axes)]
  for li,v in zip(p.loop_indices,points):uv.data[li].uv=(r[0]+(r[2]-r[0])*(.2+.6*(v[axes[0]]-lo[0])/max(span[0],1e-5)),r[1]+(r[3]-r[1])*(.2+.6*(v[axes[1]]-lo[1])/max(span[1],1e-5)))

def rawbox(name,pos,size,slot='WoodHoney',width=.012):
 # Existing masters use Blender X/right, -Y/front, Z/up.
 return box(name,(pos[0],pos[2],-pos[1]),(size[0],size[2],size[1]),slot,width)

def rawbeam(name,a,b,width=.02,slot='Rope'):
 return beam(name,(a[0],a[2],-a[1]),(b[0],b[2],-b[1]),width,width,slot)

def rawmesh(name,verts,faces,slot):
 return mesh(name,[(x,z,-y) for x,y,z in verts],faces,slot)

def cord(name,points,r=.012,slot='Rope'):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=1;curve.bevel_depth=r;curve.bevel_resolution=1
 spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
 for p,co in zip(spline.points,points):p.co=(*co,1)
 obj=bpy.data.objects.new(name,curve);source.objects.link(obj)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH');uv_rect(obj,slot)
 parts.append(obj);obj['CozyDetailTier']=1;return obj

def bolt(pos,axis=(0,-1,0),r=.014,slot='Iron'):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=1,location=pos);o=bpy.context.object
 o.scale=(r,r,r*.42);o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 assign(o,slot);return link(o,'Rounded iron rivet',1)

def cloth(name,verts,faces,slot,coords=None):
 o=rawmesh(name,verts,faces,slot);r=region(slot);uv=o.data.uv_layers.active
 # Fine weave repeats inside the padded swatch, avoiding adjacent atlas regions.
 cx=(r[0]+r[2])/2;cy=(r[1]+r[3])/2
 for p in o.data.polygons:
  p.use_smooth=True
  points=[o.data.vertices[o.data.loops[li].vertex_index].co for li in p.loop_indices]
  ru=(r[2]-r[0])*min(.84,(points[1]-points[0]).length/.45)
  rv=(r[3]-r[1])*min(.84,(points[-1]-points[0]).length/.45)
  for li,(u,v) in zip(p.loop_indices,[(0,0),(1,0),(1,1),(0,1)]):uv.data[li].uv=(cx+(u-.5)*ru,cy+(v-.5)*rv)
 m=o.modifiers.new('Sewn fabric thickness','SOLIDIFY');m.thickness=.009;m.offset=0;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 return o

def canopy_point(q,t,rear=False):
 if rear:
  y=.62+.46*t;z=2.75-.51*math.sin(t*math.pi/2);half=1.50+.12*t
 else:
  y=.62-1.55*t
  z=(1-t)**3*2.75+3*(1-t)**2*t*2.08+3*(1-t)*t*t*2.12+t**3*2.25
  half=1.50+.20*t
 return (q*half,y,z-.035*(1-q*q)*math.sin(math.pi*t))

def market():
 if (A/'Source~/MarketCart/MarketCart_Manifest.json').exists():
  import runpy
  return runpy.run_path(str(A/'Source~/MarketCart/build_market_cart.py'),run_name='__main__')
 collections=begin('MarketStand');global old_pixels;old_pixels={}
 for c in collections:
  for obj in list(c.objects):
   if c.name.startswith('04'):
    bpy.data.objects.remove(obj,do_unlink=True);continue
   if obj.name.startswith('Roof Side Beam'):
    bpy.data.objects.remove(obj,do_unlink=True);continue
   if obj.type!='MESH':continue
   obj.data=obj.data.copy()
   if c.name.startswith('05'):
    remap_produce(obj)
    # Reverse the display order without a negative object scale (which would invert
    # the vendor produce's thin leaf faces when combined for export).
    if 'Greens' in obj.name:
     for v in obj.data.vertices:v.co.x*=-1;v.co.z+=.06
    else:
     obj.location.x*=-1
     obj.location.z+=.12 if 'Tomato' in obj.name else .09 if 'Cabbage' in obj.name else .06
     bpy.context.view_layer.update()
     # Keep fruit bodies inside the real crate walls; leaves may rise above them.
     points=[obj.matrix_world@v.co for v in obj.data.vertices];span=Vector([max(p[j] for p in points)-min(p[j] for p in points) for j in range(3)])
     factor=min(1,.53/max(span.y,.001),.57/max(span.x,.001))
     obj.scale*=factor;bpy.context.view_layer.update();points=[obj.matrix_world@v.co for v in obj.data.vertices]
     low=min(p.y for p in points);high=max(p.y for p in points)
     if low<-.30:obj.location.y+=-.30-low
     elif high>.27:obj.location.y+=.27-high
     for p in obj.data.polygons:p.use_smooth=True
   else:
    slot='Rope' if 'Rope' in obj.name else 'WoodDark' if any(s in obj.name for s in ['Ground Foot','Cross Support','Lower Frame','Lower Side']) else 'WoodHoney'
    uv_rect(obj,slot)
    if obj.name.startswith('Main Timber Post'):
     for v in obj.data.vertices:v.co.x*=1.22;v.co.y*=1.22
    if obj.name.startswith('Open Lower Shelf'):obj.location.z+=.15
    if obj.name.startswith('Knee Brace') or 'Knee Brace' in obj.name:
     obj.scale.x*=1.3;obj.scale.y*=1.3
   parts.append(obj);obj['CozyDetailTier']=0
 # Rounded continuous ribbons, deeper scalloped hem, stitched seams and side drapes.
 for stripe in range(9):
  q0=-1+2*stripe/9;q1=-1+2*(stripe+1)/9;slot='FabricSage' if stripe%2==0 else 'FabricCream'
  for rear,steps in [(False,18),(True,7)]:
   vv=[]
   for j in range(steps+1):
    for q in [q0,q1]:vv.append(canopy_point(q,j/steps,rear))
   ff=[(2*j,2*j+1,2*j+3,2*j+2) for j in range(steps)]
   cloth(('Canopy rear return ' if rear else 'Soft curved canopy stripe ')+str(stripe),vv,ff,slot)
   vv=[]
   for j in range(11):
    u=j/10;x,y,z=canopy_point(q0+(q1-q0)*u,1,rear);drop=.17+.085*math.sin(math.pi*u)
    vv.extend([(x,y,z),(x,y+(.012 if rear else -.012),z-drop)])
   cloth(('Rear hem ' if rear else 'Rounded front scallop ')+str(stripe),vv,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(10)],slot)
  # A fine seam follows the actual curved surface, not a straight beam through it.
  cord('Stitched longitudinal seam',[tuple(Vector(canopy_point(q0,j/18))+Vector((0,0,.006))) for j in range(19)],.0025,'FabricCream')
 for sign in [-1,1]:
  vv=[]
  for j in range(10):
   t=j/9;x,y,z=canopy_point(sign,t);vv.extend([(x,y,z),(x+sign*.035,y,z-.14-.09*math.sin(t*math.pi))])
  cloth('Side hanging fabric edge',vv,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(9)],'FabricSage')
  cloth('Gathered rear corner cloth',[(sign*1.49,.60,2.75),(sign*1.57,.98,2.40),(sign*1.52,.64,2.01),(sign*1.67,1.05,1.98),(sign*1.65,.85,2.20)],[(0,1,2),(1,3,2),(1,4,3)],'FabricSage')
  for x,y,z in [(sign*1.43,.62,2.72),(sign*1.43,-.62,2.17)]:
   cord('Wrapped canopy binding',[(x+.10*math.cos(t*math.tau*4),y+.10*math.sin(t*math.tau*4),z-.075+t*.13) for t in [i/88 for i in range(89)]],.013)
  rawbox('End grain canopy peg',(sign*1.43,-.73,2.24),(.20,.18,.17),'WoodHoney',.021)
  for x in [sign*1.43,sign*1.08]:bolt((x,-.705,1.96 if abs(x)>1.3 else 2.16))
 # Fasteners and bevelled crate fronts sharpen the reference's construction details.
 for c,x in enumerate([-1.17,-.39,.39,1.17]):
  rawbox('Crate handle upper bridge',(x,-.36,1.33),(.70,.045,.045),'WoodHoney',.007)
  for dx in [-.29,.29]:
   for z in [1.16,1.31]:bolt((x+dx,-.389,z),r=.009,slot='MetalHighlight')
 return finish('MarketStand',parts,[],None)

def shipping():
 collections=begin('ShippingBin');lid=[];body=[];hinge=Vector((0,.465,.855));editable_pivot=None
 for c in collections:
  for obj in list(c.objects):
   if obj.type=='EMPTY':
    if 'Editable Lid Hinge' in obj.name:editable_pivot=obj;obj.rotation_euler.x=0
 for c in collections:
  for obj in list(c.objects):
   if obj.type!='MESH':continue
   obj.data=obj.data.copy()
   slot='Iron' if c.name.startswith('02') or c.name.startswith('05') or 'Hinge Plate' in obj.name else 'WoodDark' if any(s in obj.name for s in ['Shadow','Dark Corner','Foot','Underside','Grip','Carving']) else 'WoodHoney'
   if 'Rivet' in obj.name:slot='MetalHighlight'
   uv_rect(obj,slot)
   # Broader, softer exposed timber edges, while preserving the already modelled relief.
   if c.name.startswith('01') or c.name.startswith('04'):
    bevel(obj,.004)
   parts.append(obj);obj['CozyDetailTier']=0
   (lid if c.name.startswith('04') else body).append(obj)
 bpy.context.view_layer.update()
 # Reference has a wider body and more substantial corner uprights.
 for obj in parts:
  world=obj.matrix_world.copy();obj.parent=None;obj.matrix_world=world
  obj.location.x*=1.09
  for v in obj.data.vertices:v.co.x*=1.09
  if 'Corner Upright' in obj.name:
   for v in obj.data.vertices:v.co.x*=1.13;v.co.y*=1.13
 if editable_pivot:bpy.data.objects.remove(editable_pivot,do_unlink=True)
 # Caps make the top grain and the iron straps distinct from horizontal wall planks.
 for x in [-.72*1.09,.72*1.09]:
  for y in [-.41,.41]:
   o=rawbox('Bevelled corner post cap',(x,y,.847),(.145,.145,.032),'WoodDark',.008);body.append(o)
   o=rawbox('Corner post end grain',(x,y,.865),(.12,.12,.004),'EndGrain',.001);body.append(o)
 return finish('ShippingBin',body,lid,hinge)

def finish(kind,body,lid,hinge):
 bpy.context.view_layer.update();all_parts=body+lid
 for obj in all_parts:
  bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
 def combined(items,name,pivot=None):
  bpy.ops.object.select_all(action='DESELECT');copies=[]
  for original in items:
   cp=original.copy();cp.data=original.data.copy();exports.objects.link(cp);cp.parent=None;cp.matrix_world=original.matrix_world.copy();cp.select_set(True);copies.append(cp)
  bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.data.materials.clear();o.data.materials.append(M[0])
  for p in o.data.polygons:p.material_index=0
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  if pivot is not None:
   for v in o.data.vertices:v.co-=pivot
   o.location=pivot
  return o
 lods=[];spec=[]
 for label,items,pivot in [('Body',body,None),('Lid',lid,hinge)]:
  if not items:continue
  near=combined(items,f'{kind}_{label}_LOD0',pivot);far=near.copy();far.data=near.data.copy();exports.objects.link(far);far.name=f'{kind}_{label}_LOD1'
  bpy.context.view_layer.objects.active=far;mod=far.modifiers.new('Mobile distance mesh','DECIMATE');mod.ratio=.48;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
  lods.extend([near,far])
  for o in [near,far]:
   coords=[o.matrix_world@v.co for v in o.data.vertices];unity=[(v.x,v.z,-v.y) for v in coords]
   spec.append(dict(name=o.name,triangles=sum(len(p.vertices)-2 for p in o.data.polygons),min=[min(v[i] for v in unity) for i in range(3)],max=[max(v[i] for v in unity) for i in range(3)]))
  if pivot is not None:
   for o in [near,far]:o.data.transform(Matrix.Translation(pivot));o.location=Vector((0,0,0))
 bpy.ops.object.select_all(action='DESELECT')
 for o in lods:o.select_set(True)
 for name,position in [('CozyOrigin',(0,0,0)),('CozyRight',(1,0,0)),('CozyForward',(0,-1,0)),('CozyUp',(0,0,1))]:
  marker=bpy.data.objects.new(name,None);exports.objects.link(marker);marker.location=position;marker.select_set(True)
 bpy.context.view_layer.objects.active=lods[0]
 model=A/f'mesh/World/{kind}/{kind}_A.fbx'
 bpy.ops.export_scene.fbx(filepath=str(model),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False,path_mode='STRIP',embed_textures=False)
 exports.hide_render=True;exports.hide_viewport=True
 if lid:
  pivot=bpy.data.objects.new('Editable rear hinge - 0 closed, -72deg open',None);source.objects.link(pivot);pivot.location=hinge
  for o in lid:world=o.matrix_world.copy();o.parent=pivot;o.matrix_world=world
  pivot.rotation_euler.x=math.radians(-72)
 source.hide_render=False;source.hide_viewport=False
 # Reference studio stays in the editable master and is excluded from the FBX.
 for label,pos,power,size in [('Soft key',(-3,-4,7),450,5),('Soft fill',(4,-1,5),200,4)]:
  d=bpy.data.lights.new(label,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(label,d);studio.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1.0 if kind=='MarketStand' else .6))-o.location).to_track_quat('-Z','Y').to_euler()
 d=bpy.data.cameras.new('Reference camera');camera=bpy.data.objects.new('Reference camera',d);studio.objects.link(camera);scene.camera=camera
 camera.location=(4,-6,3.7) if kind=='MarketStand' else (2.4,-3.7,2.4);target=Vector((0,0,1.3 if kind=='MarketStand' else .8));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=4.8 if kind=='MarketStand' else 2.8
 scene.world=bpy.data.worlds.new('Cozy neutral studio');scene.world.color=(.4,.4,.4);scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.view_settings.view_transform='Standard'
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=d.ortho_scale*1.3;area.spaces.active.region_3d.view_location=target
 scene['Reference']='MarketProps_Cozy_Target.png, right column';scene['Atlas']='Existing NP_CozySurfaces_01, packed colour and normal';scene['Export']='Closed rest pose; 2 LODs; gameplay size preserved in Unity'
 bpy.ops.object.select_all(action='DESELECT');bpy.data.orphans_purge(do_recursive=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(A/f'Source~/{kind}/{kind}_A.blend'))
 report=dict(kind=kind,meshes=spec,hingeUnity=[0,.855,-.465] if hinge else None,openAngle=72 if hinge else None,sharedMaterial='NP_CozySurfaces_01',sourceProduce='Original Toon Farm Pack meshes retained' if kind=='MarketStand' else None)
 (A/f'Source~/MarketPropsCozy/{kind}_Manifest.json').write_text(json.dumps(report,indent=2));print('COZY_PROP_BUILT',json.dumps(report),flush=True)
 return report

if __name__=='__main__':
 market();shipping()
