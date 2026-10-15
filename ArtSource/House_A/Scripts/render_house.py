"""Render saved production meshes, not planning geometry; no source rebuild."""
from pathlib import Path
import bpy,json,sys
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
S=json.loads((ART/'References/House_A_Approved.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/House_A_Masters.blend'))
selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
keys=selected or [m['id'] for m in S['models']]
for key in keys:
 if key=='Levels':
  scene=bpy.data.scenes['House A Levels 1-5'];bpy.context.window.scene=scene
  scene.render.filepath=str(ART/'Previews/House_A_Levels_1_to_5.png');bpy.ops.render.render(write_still=True);continue
 m=next(m for m in S['models'] if m['id']==key);scene=bpy.data.scenes['House A '+key];bpy.context.window.scene=scene
 q=m['visualBounds'];center=Vector(((q['minXYZ'][0]+q['maxXYZ'][0])/2,-(q['minXYZ'][2]+q['maxXYZ'][2])/2,q['sizeWDH'][2]*.40))
 for side,direction in [('FrontRight',(1,-1,.85)),('RearLeft',(-1,1,.85))]:
  cam=scene.camera;cam.location=center+Vector(direction)*22;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
  cam.data.ortho_scale=max(q['sizeWDH'][0],q['sizeWDH'][1])*1.24+2.5
  scene.render.filepath=str(ART/'Previews'/f'House_A_{key}_{side}.png')
  bpy.ops.render.render(write_still=True);print('HOUSE_RENDER_DONE '+key+' '+side,flush=True)
