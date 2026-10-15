import bpy,json,math
from pathlib import Path
ART=Path(__file__).resolve().parents[1]
manifest=json.loads((ART/'Previews/MeshManifest.json').read_text());results=[]
for level in range(1,5):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(ART/'Exports'/f'BarnInterior_Lv{level}.fbx'),use_anim=False)
 bpy.context.view_layer.update();obs=[o for o in bpy.context.scene.objects if o.type=='MESH']
 tris=sum(len(p.vertices)-2 for o in obs for p in o.data.polygons)
 missing=[o.name for o in obs if not o.data.materials or o.data.materials[0] is None]
 lo=[min((o.matrix_world@v.co)[i] for o in obs for v in o.data.vertices) for i in range(3)];hi=[max((o.matrix_world@v.co)[i] for o in obs for v in o.data.vertices) for i in range(3)]
 expected=manifest['levels'][level-1]
 status='PASS' if tris==expected['triangles'] and not missing and len(obs)==expected['meshObjects'] else 'FAIL'
 results.append({'level':level,'status':status,'triangles':tris,'meshObjects':len(obs),'missingMaterials':missing,'roundtripBoundsBlenderXYZ':[lo,hi]})
(ART/'Previews/FBXRoundTrip.json').write_text(json.dumps(results,indent=2));print(json.dumps(results))
assert all(r['status']=='PASS' for r in results)
