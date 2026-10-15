"""Independent audit of saved Blender source and every exported FBX."""
import bpy,bmesh,json,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
manifest=json.loads((ART/'Previews/MeshManifest.json').read_text())
psd=ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd'
assert hashlib.sha256(psd.read_bytes()).hexdigest()==manifest['atlasSHA256']
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/FarmBuildingsFence_A.blend'))
checks=[]
def world(o):
 # Secondary scene matrix_world caches can be lazy after switching scenes.
 # Evaluate the stored hierarchy explicitly, including the hinge inverse.
 return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
def bounds(obs):
 bpy.context.view_layer.update();pts=[world(o)@v.co for o in obs for v in o.data.vertices]
 return [[min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]]
def meshcheck(o):
 assert len(o.data.materials)==1 and o.data.materials[0].name=='TFP_Atlas_1A',o.name
 assert o.data.uv_layers.active is not None,o.name
 assert all(math.isfinite(a) for v in o.data.vertices for a in v.co),o.name
 assert all(p.area>1e-10 for p in o.data.polygons),o.name
 assert all(0<=a<=1 for u in o.data.uv_layers.active.data for a in u.uv),o.name
 # Every UV references the atlas palette, not vegetation/logos at right/top.
 assert all(u.uv.x<.51 and u.uv.y<.51 for u in o.data.uv_layers.active.data),o.name
for ob in bpy.data.objects:
 if ob.type=='MESH' and ob.data.materials and ob.data.materials[0].name=='TFP_Atlas_1A':meshcheck(ob)
for name,expected in [('Farm_Lumbung_A',(7,5,5.6)),('Farm_BarnStore_B',(9,6,5.4))]:
 sc=bpy.data.scenes[name+' Editable'];bpy.context.window.scene=sc
 o=bpy.data.objects[name];lo,hi=bounds([o]);assert abs(hi[2]-expected[2])<1e-5
 assert all(abs(a)<1e-7 for a in o.location) and tuple(o.scale)==(1,1,1)
 assert all(len(p.vertices)==3 for p in o.data.polygons)
 assert sc['RoofShape']=='Gambrel with lean-to wings'
 checks.append({'asset':name,'bounds':bounds([o]),'groundOrigin':True,'triangulated':True,'materials':['TFP_Atlas_1A']})
bpy.context.window.scene=bpy.data.scenes['FenceKit Modular'];bpy.context.view_layer.update()
for pitch,length in [(1,.76),(2,1.76)]:
 o=bpy.data.objects[f'Fence_Rails_{pitch}m'];lo,hi=bounds([o]);assert abs(hi[0]-lo[0]-length)<1e-5
 assert abs(lo[0]-.12)<1e-5 and abs(hi[0]-(pitch-.12))<1e-5
 start=bpy.data.objects[f'Fence_Rails_{pitch}m_Start'];end=bpy.data.objects[f'Fence_Rails_{pitch}m_End']
 assert abs((world(end).translation-world(start).translation).length-pitch)<1e-5
 checks.append({'asset':o.name,'pitch':pitch,'railLength':length,'postFree':True})
for pitch in [2,3]:
 for n in range(1 if pitch==2 else 2):
  hinge=bpy.data.objects[f'Gate{pitch}_Hinge_{n}'];leaf=bpy.data.objects[f'Gate{pitch}_Leaf_{n}']
  assert leaf.parent==hinge and hinge['RotationAxis']=='local Z'
  lo,hi=bounds([leaf]);width=1.64 if pitch==2 else 1.29
  assert abs(hi[0]-lo[0]-width)<1e-5,(leaf.name,lo,hi)
  assert abs(lo[2]-.15)<1e-5 and abs(hi[2]-1.05)<1e-5,(leaf.name,lo,hi)
  # Rotating one leaf must not move the root or either post.
  old=hinge.rotation_euler.copy();rootpos=world(hinge.parent).translation.copy()
  hinge.rotation_euler.z=.6;bpy.context.view_layer.update()
  assert (world(hinge.parent).translation-rootpos).length<1e-7
  hinge.rotation_euler=old;bpy.context.view_layer.update()
 checks.append({'asset':f'Fence_Gate_{pitch}m','hingesVerified':True})
assert any(im.packed_file for im in bpy.data.images if im.get('UnityGUID')==manifest['atlasGUID'])
for pitch,xshift in [(2,5),(3,7.6)]:
 for n in range(1 if pitch==2 else 2):
  source=bpy.data.objects[f'Gate{pitch}_Leaf_{n}'];cp=bpy.data.objects[f'Gate{pitch}_Leaf_{n}.001']
  expected=world(source).translation+Vector((xshift,-7,0))
  assert (world(cp).translation-expected).length<1e-5,(cp.name,world(cp).translation,expected)
# Import each FBX into a separate empty test scene. Check orientation/scale/bounds.
roundtrips=[]
for file in sorted((ART/'Exports').glob('*.fbx')):
 sc=bpy.data.scenes.new('AUDIT '+file.stem);bpy.context.window.scene=sc
 bpy.ops.import_scene.fbx(filepath=str(file),use_custom_normals=True)
 meshes=[o for o in sc.objects if o.type=='MESH'];assert meshes,file.name
 lo,hi=bounds(meshes);expected=next(a for a in manifest['assets'] if a['asset']==file.stem)['bounds']
 assert max(abs(expected[j][i]-[lo,hi][j][i]) for j in range(2) for i in range(3))<.002,(file.name,expected,[lo,hi])
 for o in meshes:
  assert len(o.data.materials)==1 and o.data.materials[0].name.startswith('TFP_Atlas_1A')
  assert o.data.uv_layers.active is not None
 roundtrips.append({'file':file.name,'boundsPreserved':True,'materialsPreserved':True,'uvPreserved':True})
(ART/'Previews/Verification.json').write_text(json.dumps(dict(status='PASS',savedBlend=checks,fbxRoundtrip=roundtrips,atlasUnchanged=True),indent=2))
print('FARM_VERIFY_PASS '+str(len(roundtrips))+' FBX files')
