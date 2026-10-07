"""Read the saved .blend with Blender to check the editable source kit."""
import bpy, json, os
root = os.path.abspath('ArtSource/StylizedMeadow')
bpy.ops.wm.open_mainfile(filepath=root+'/Blender/StylizedMeadow.blend')
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and not o.name.startswith('Preview')]
assert len(meshes) == 14, len(meshes)
assert all(o.data.uv_layers and len(o.data.vertices) > 0 for o in meshes)
assert all(i.packed_file for i in bpy.data.images if i.source == 'FILE')
report = {'status':'complete', 'editableMeshObjects':len(meshes),
          'collections':[c.name for c in bpy.data.collections],
          'packedImages':[i.name for i in bpy.data.images if i.packed_file]}
open(root+'/Previews/BlenderSourceAudit.json','w').write(json.dumps(report,indent=2))
print('BLENDER_SOURCE_VERIFIED')
