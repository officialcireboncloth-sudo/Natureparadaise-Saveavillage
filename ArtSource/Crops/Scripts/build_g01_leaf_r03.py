"""Replace only editable berry leaves, retaining branches, fruit, flowers and identities."""
import bpy,sys,json,ast
from mathutils import Vector
import numpy as np
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import g01_raspberry_config as R
import g01_leaf_r03 as L
import g01_geometry as B
import plant_blender_common as C
import unified_plant_atlas as U
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));scene=bpy.context.scene
# Reuse the accepted export implementation without executing the full plant builders.
builder=Path(__file__).with_name('build_g01_blender.py');tree=ast.parse(builder.read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='export')
context=dict(bpy=bpy,scene=scene,C=C,MODELS=G.MODELS);exec(compile(ast.Module(body=[fn],type_ignores=[]),str(builder),'exec'),context);exports=[];stats={}
only=sys.argv[sys.argv.index('--only')+1] if '--only' in sys.argv else None
for root in [o for o in bpy.data.objects if o.get('Phase')]:
 kind,stage=root['Plant'],root['Phase']
 if only and kind!=only:continue
 leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')==kind]
 if not leaves:continue
 assert not any(o.get('LeafRevision')=='R03' for o in leaves),'R03 is already applied. Rebuild from the saved pre-R03 master to avoid compounding leaf dimensions.'
 config=R if kind=='Raspberry' else G;mat=bpy.data.materials[config.ATLAS];config.configure(C,mat);context['mat']=mat;C.parent=root;C.current=leaves[0].users_collection[0];bounds=[]
 # Fruit envelopes ensure the broader new blades do not pass through berries.
 indices={o.get('FruitIndex') for o in root.children_recursive if o.get('BerryKind')==kind}
 for index in indices:
  parts=[o for o in root.children_recursive if o.type=='MESH' and o.get('FruitIndex')==index and (o.get('BerryKind')==kind or o.get('BlackberryDrupelet') or o.get('RaspberryDrupelet'))]
  points=np.array([tuple(o.matrix_basis@p.co) for o in parts for p in o.data.vertices]);lo,hi=points.min(axis=0),points.max(axis=0);bounds.append(((lo+hi)*.5,np.maximum((hi-lo)*.5,.005)))
 for old in leaves:
  ob=L.replacement(old,kind,stage,bounds);name=old.name;bpy.data.objects.remove(old,do_unlink=True);ob.name=name
 if kind=='Blueberry' and stage in ['Flowering','Ready']:
  # The planted berries previously dominated the leaf blades. Keep the detached
  # harvest artwork, and resize only attached berries with their physical joints.
  for fruit in [o for o in root.children_recursive if o.get('BerryKind')=='Blueberry']:
   idx=int(fruit['FruitIndex']);x,y,z,_=(B.BLUE_FRUIT if stage=='Ready' else B.BLUE_FRUIT[4:])[idx]
   center=Vector((x if stage=='Ready' else x*.93,y,(z if stage=='Ready' else z*.9)+float(root['GroundOffset'])))
   key='Fruit_'+str(idx);neck=next(o for o in root.children_recursive if o.get('JointRole')=='head' and o.get('JointId')==key);stem=next(o for o in root.children_recursive if o.get('JointRole')=='stem' and o.get('JointId')==key)
   for ob in [fruit,neck]:
    for v in ob.data.vertices:v.co=center+(v.co-center)*.84
   ring=lambda ob,start:sum((v.co for v in ob.data.vertices[start:start+9]),Vector())/9
   attach=ring(neck,0);axis=(ring(neck,10)-attach).normalized();start=ring(stem,0);name=stem.name;bpy.data.objects.remove(stem,do_unlink=True);B.pedicel(start,attach,axis,key,kind);new=next(o for o in root.children_recursive if o.get('JointRole')=='stem' and o.get('JointId')==key);new.name=name;neck['JointPoint']=list(attach);fruit['PlantedSizeRevision']=.84
 stats[root.name]=dict(leaves=len(leaves),fruitEnvelopes=len(bounds))
 if '--draft' not in sys.argv:exports.append(context['export'](root,'NP_'+kind+'_'+stage))
 print('LEAF_R03_UPDATED',kind,stage,len(leaves),flush=True)
if '--draft' in sys.argv:
 draft=G.REVIEW/'Leaf_R03/Draft.blend';bpy.ops.wm.save_as_mainfile(filepath=str(draft))
else:
 bpy.ops.wm.save_as_mainfile(filepath=str(G.MASTER));data=U.load(G.MASTER.parent/'G01_Manifest.json');by_name={r['name']:r for r in data['exports']};by_name.update({r['name']:r for r in exports});data.update(revision='R03 corrected leaf form, tapered UVs, size and spatial inclination',exports=list(by_name.values()));U.write(G.MASTER.parent/'G01_Manifest.json',data)
 U.write(G.REVIEW/'Leaf_R03/Build.json',dict(status='SOURCE_EXPORTED',roots=stats,exports=exports,masterSha256=U.sha(G.MASTER)))
print('LEAF_R03_BUILD_PASS',len(exports),'updated phase exports',flush=True)
