import bpy,json
from pathlib import Path
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/FarmBuildingsFence_A.blend'))
o=bpy.data.objects['Farm_BarnStore_B_SignLetters']
bad=[{'area':p.area,'verts':[list(o.data.vertices[i].co) for i in p.vertices]} for p in o.data.polygons if p.area<1e-10]
print('LETTER_DIAG '+json.dumps(bad[:8]));print('BAD_COUNT '+str(len(bad)))
bpy.context.window.scene=bpy.data.scenes['FenceKit Modular'];bpy.context.view_layer.update()
for name in ['Fence_Rails_1m_Start','Fence_Rails_1m_End','Fence_Rails_2m_Start','Fence_Rails_2m_End','Gate3_Hinge_0','Gate3_Hinge_1','Gate3_Leaf_0','Gate3_Leaf_1']:
 ob=bpy.data.objects[name];print('OBJECT',name,'local',list(ob.location),'world',list(ob.matrix_world.translation),'parent',ob.parent.name if ob.parent else None,'inverse',list(ob.matrix_parent_inverse.translation))
 if ob.type=='MESH':
  pts=[ob.matrix_world@v.co for v in ob.data.vertices];print('BOUNDS',[[min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]])
def world(o):return world(o.parent)@o.matrix_parent_inverse@o.matrix_basis if o.parent else o.matrix_basis
bpy.context.window.scene=bpy.data.scenes['00 Farm + Fence Overview']
for ob in bpy.context.scene.objects:
 if ob.name.startswith('Gate3_'):
  print('OVERVIEW',ob.name,'world',list(world(ob).translation),'local',list(ob.location),'inverse',list(ob.matrix_parent_inverse.translation))
