"""Rebuild only the planted berry shrubs using the fuller reference-driven crown."""
import bpy,bmesh,sys,ast,math
from pathlib import Path
from mathutils import Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import g01_raspberry_config as R
import g01_bush_r04_geometry as B
import plant_blender_common as C
import unified_plant_atlas as U
W=G.REVIEW/'Bush_R04';draft='--draft' in sys.argv
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));scene=bpy.context.scene
# Create independent copies of cached unit spheres without repeatedly invoking
# context operators across thousands of editable drupelet objects.
sphere_templates={}
def sphere(name,loc,scale,slot,sub=1):
 if sub not in sphere_templates:
  me=bpy.data.meshes.new('R04 unit ico '+str(sub));bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=sub,radius=1.);bm.to_mesh(me);bm.free();coords=[]
  for v in me.vertices:
   n=v.co.normalized();coords.append((.5+math.atan2(n.y,n.x)/math.tau,.5+math.asin(n.z)/math.pi))
  sphere_templates[sub]=(me,coords)
 template,coords=sphere_templates[sub];ob=bpy.data.objects.new(name,template.copy());C.current.objects.link(ob);ob.parent=C.parent;ob.location=loc;ob.scale=scale;C.atlas(ob,slot,coords);return ob
C.sphere=sphere
if not draft:
 assert not any(o.get('BushRevision')=='R04' for o in bpy.data.objects),'R04 is already applied; use its saved before-master when rebuilding'
builder=Path(__file__).with_name('build_g01_blender.py');tree=ast.parse(builder.read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='export');context=dict(bpy=bpy,scene=scene,C=C,MODELS=G.MODELS);exec(compile(ast.Module(body=[fn],type_ignores=[]),str(builder),'exec'),context)
# Replace packed source-map images in both authoring materials with the edited files.
for config in [G,R]:
 mat=bpy.data.materials[config.ATLAS]
 for node in mat.node_tree.nodes:
  if node.type!='TEX_IMAGE' or not node.image:continue
  name=next((s for s in ['BaseColor','Normal','SurfaceMask'] if s in node.image.name),None)
  if name:
   im=bpy.data.images.load(str(G.TEXTURES/(config.ATLAS+'_'+name+'.png')),check_existing=False)
   if name!='BaseColor':im.colorspace_settings.name='Non-Color'
   im.pack();node.image=im
stats={};exports=[]
for kind in ['Blueberry','Raspberry','Blackberry']:
 config=R if kind=='Raspberry' else G;mat=bpy.data.materials[config.ATLAS];config.configure(C,mat);context['mat']=mat
 for stage in (['Ready'] if draft else ['Sprout','Young','Flowering','Ready']):
  root=bpy.data.objects[kind+'_'+stage];coll=root.users_collection[0]
  for ob in list(root.children_recursive):bpy.data.objects.remove(ob,do_unlink=True)
  C.current=coll;C.parent=root;B.build(kind,stage);obs=[o for o in root.children_recursive if o.type=='MESH']
  for ob in obs:ob.data.transform(ob.matrix_basis);ob.matrix_basis=Matrix.Identity(4)
  offset=.001-min(v.co.z for ob in obs for v in ob.data.vertices)
  for ob in obs:
   for v in ob.data.vertices:v.co.z+=offset
   if ob.get('JointPoint'):ob['JointPoint']=[ob['JointPoint'][0],ob['JointPoint'][1],ob['JointPoint'][2]+offset]
  root['GroundOffset']=offset;root['BushRevision']='R04';stats[root.name]=dict(leaves=sum(o.get('ReferenceLeaf')==kind for o in obs),fruits=sum(o.get('BerryKind')==kind for o in obs),flowers=sum(o.get('OpenFlower')==kind for o in obs),canes=sum('R04 curved main cane' in o.name for o in obs))
  if not draft:exports.append(context['export'](root,'NP_'+kind+'_'+stage))
  print('BUSH_R04_BUILT',kind,stage,stats[root.name],flush=True)
if draft:bpy.ops.wm.save_as_mainfile(filepath=str(W/('Draft_Fast.blend' if '--fast' in sys.argv else 'Draft.blend')))
else:
 bpy.ops.wm.save_as_mainfile(filepath=str(G.MASTER));data=U.load(G.MASTER.parent/'G01_Manifest.json');current={r['name']:r for r in data['exports']};current.update({r['name']:r for r in exports});data.update(revision='R04 fuller spatial berry shrubs and species-specific leaf detail',exports=list(current.values()));U.write(G.MASTER.parent/'G01_Manifest.json',data);U.write(W/'Build.json',dict(status='SOURCE_EXPORTED',roots=stats,exports=exports,masterSha256=U.sha(G.MASTER)))
print('BUSH_R04_BUILD_PASS',len(exports),'exports',flush=True)
