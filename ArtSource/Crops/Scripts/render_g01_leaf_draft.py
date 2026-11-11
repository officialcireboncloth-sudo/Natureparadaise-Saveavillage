"""Review foliage adjustments before replacing any published mesh."""
import bpy,sys,math,ast
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
bpy.ops.wm.open_mainfile(filepath=str(G.REVIEW/'Leaf_R03/Draft.blend'));scene=bpy.context.scene;camera=scene.camera;camera.data.sensor_fit='HORIZONTAL';roots=[o for o in bpy.data.objects if o.get('Phase')];scene.cycles.samples=8;scene.render.threads=4;out=G.REVIEW/'Leaf_R03';
tree=ast.parse(Path(__file__).with_name('render_g01.py').read_text());nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ['aim','frame']];exec(compile(ast.Module(body=nodes,type_ignores=[]),'camera','exec'))
for kind in ['Blueberry','Raspberry','Blackberry']:
 for root in roots:
  for ob in root.children_recursive:
   if ob.type=='MESH':ob.hide_render=not(root['Plant']==kind and root['Phase']=='Ready')
 bpy.data.objects[kind+'_Ready'].location=(0,0,0);frame(650,850,0,22);scene.render.resolution_x=650;scene.render.resolution_y=850;scene.render.filepath=str(out/(kind+'_Draft.png'));bpy.ops.render.render(write_still=True);print('LEAF_DRAFT_RENDER',kind,flush=True)
