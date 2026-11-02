"""Native GIMP: explicit alpha smoothness; foreground fill ignores colour alpha."""
from pathlib import Path
from gi.repository import Gimp,Gegl,Gio
import json
ROOT=next(p for start in (Path.cwd(),Path(__file__).resolve().parent) for p in [start,*start.parents] if (p/'Assets/Nature  Paradaise').is_dir());A=ROOT/'Assets/Nature  Paradaise';DEST=A/'Textures/Shared/Cozy';SOURCE=A/'Source~/SharedAtlas'
def run(name,**values):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in values.items():c.set_property(k.replace('_','-'),v)
 r=p.run(c);assert r.index(0)==Gimp.PDBStatusType.SUCCESS
image=Gimp.Image.new(2048,2048,Gimp.ImageBaseType.RGB);manifest=json.loads((DEST/'NP_CozyAtlasSlots.json').read_text())
Gimp.context_push()
for s in manifest['slots']:
 x,y,w,h=s['pixelRectTopLeft'];name=s['name'];metal=.9 if name=='Iron' else 1. if name=='Aluminium' else 0.;smooth=.45 if name=='Aluminium' else .22 if name=='Iron' else .12
 la=Gimp.Layer.new(image,name+' surface',w,h,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);image.insert_layer(la,None,0)
 color=Gegl.Color.new('black');color.set_rgba(metal,1.,0.,1.);Gimp.context_set_foreground(color);la.fill(Gimp.FillType.FOREGROUND);la.set_offsets(x,y);la.set_opacity(smooth*100)
run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(str(SOURCE/'NP_CozySurfaces_01_SurfaceMask.xcf')))
run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,file=Gio.File.new_for_path(str(DEST/'NP_CozySurfaces_01_SurfaceMask.png')),compression=9,include_color_profile=False)
image.delete();Gimp.context_pop();print('SURFACE_ALPHA_GIMP_PASS',flush=True)
