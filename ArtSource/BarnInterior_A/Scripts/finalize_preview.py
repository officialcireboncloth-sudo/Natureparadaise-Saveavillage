import bpy
from pathlib import Path
ART=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/BarnInterior_A.blend'))
for level in [1,2]:
 sc=next(s for s in bpy.data.scenes if s.name.startswith(f'0{level} BARN'))
 sc.camera.data.ortho_scale=sc['FootprintMetres'][0]*1.6
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.overlay.show_overlays=False;area.spaces.active.shading.type='MATERIAL'
bpy.context.window.scene=bpy.data.scenes['01 BARN INTERIOR - MANUAL']
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/BarnInterior_A.blend'))
for level in [1,2]:
 sc=next(s for s in bpy.data.scenes if s.name.startswith(f'0{level} BARN'));bpy.context.window.scene=sc
 sc.cycles.samples=10;sc.render.resolution_x=1280;sc.render.resolution_y=960;bpy.ops.render.render(write_still=True)
