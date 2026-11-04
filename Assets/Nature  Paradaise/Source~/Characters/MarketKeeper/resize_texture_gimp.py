"""Native GIMP resize. Keep the original upload in ignored ArtSource backups."""
from pathlib import Path
from gi.repository import Gimp,Gio
import json
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
SRC=ROOT/'ArtSource/MarketKeeper/Originals/elderly_man_figure_3d_model_basecolor.JPEG'
OUT=ROOT/'Assets/Nature  Paradaise/mesh/Character/MarketKeeper/Textures/MarketKeeper_BaseColor.jpg'
OUT.parent.mkdir(parents=True,exist_ok=True)
im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SRC)))
before=[im.get_width(),im.get_height()]
assert before==[4096,4096],before
Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
im.scale(1024,1024)
p=Gimp.get_pdb().lookup_procedure('file-jpeg-export'); c=p.create_config()
for k,v in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(OUT)),quality=0.95).items():
    c.set_property(k.replace('_','-'),v)
r=p.run(c);assert r.index(0)==Gimp.PDBStatusType.SUCCESS,str(r.index(0))
im.delete()
verify=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(OUT)))
assert [verify.get_width(),verify.get_height()]==[1024,1024]
(ROOT/'ArtSource/MarketKeeper/TextureAudit.json').write_text(json.dumps(dict(before=before,after=[1024,1024],oldBytes=SRC.stat().st_size,newBytes=OUT.stat().st_size,interpolation='GIMP NOHALO',quality=95),indent=2))
verify.delete();print('MARKET_KEEPER_TEXTURE_PASS',flush=True)
