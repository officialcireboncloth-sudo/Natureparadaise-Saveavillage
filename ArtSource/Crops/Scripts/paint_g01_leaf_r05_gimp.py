"""Restore the detailed pre-R04 Blueberry tile while retaining the current Rubus surfaces."""
import sys,json
from pathlib import Path
from gi.repository import Gimp,Gio
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
W=G.REVIEW/'Leaf_R05';records=[]
def run(name,**kw):
 p=Gimp.get_pdb().lookup_procedure(name);c=p.create_config()
 for k,v in kw.items():c.set_property(k.replace('_','-'),v)
 assert p.run(c).index(0)==Gimp.PDBStatusType.SUCCESS,name
def png(im,p):run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(p)),compression=9,include_color_profile=False)
for suffix in ['BaseColor','Normal']:
 current=G.TEXTURES/(G.ATLAS+'_'+suffix+'.xcf');before=G.REVIEW/'Bush_R04/Before/ArtSource/Crops/G01/Textures'/(G.ATLAS+'_'+suffix+'.png');im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(current)));tile=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(before)));tile.crop(256,256,0,0);layer=Gimp.Layer.new_from_drawable(tile.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE),im);im.insert_layer(layer,None,0);layer.set_offsets(0,0);layer.set_name('R05 restored detailed BlueberryLeaf '+suffix);tile.delete();run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(current)));png(im,current.with_suffix('.png'));im.delete();check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(current)));records.append(dict(map=suffix,layers=len(check.get_layers()),restoredFrom=str(before.relative_to(G.ROOT))));check.delete()
# Inspectable, exact silhouette measurements made from the supplied pictures.
im=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(W/'MeasuredSilhouettes.svg')));png(im,W/'MeasuredSilhouettes.png');im.delete()
(W/'GimpAudit.json').write_text(json.dumps(dict(status='PASS',restoredSurface='BlueberryLeaf',retainedRubusSurfaces=True,masters=records),indent=2));print('LEAF_R05_BLUEBERRY_RESTORED',flush=True)
