"""Native GIMP adds registered colour/height to unused Trellis tiles."""
import sys, json, math, random
from pathlib import Path
from gi.repository import Gimp, Gio
sys.path.insert(0, str(Path(__file__).resolve().parent))
from g07_config import *
if ATLAS=='NP_Plants_Leafy_01':
 import runpy
 runpy.run_path(str(Path(__file__).with_name('paint_g07_r02_gimp.py')),run_name='__main__')
 raise SystemExit(0)

def run(name, **kw):
    proc=Gimp.get_pdb().lookup_procedure(name); cfg=proc.create_config()
    for k,v in kw.items(): cfg.set_property(k.replace('_','-'),v)
    result=proc.run(cfg)
    assert result.index(0)==Gimp.PDBStatusType.SUCCESS, (name,str(result.index(0)))

def filt(layer, name, **kw):
    f=Gimp.DrawableFilter.new(layer,name,name); cfg=f.get_config()
    for k,v in kw.items(): cfg.set_property(k.replace('_','-'),v)
    layer.merge_filter(f)

spec=json.loads((TEXTURES/(ATLAS+'_Slots.json')).read_text())
slots={s['name']:s for s in spec['slots']}

from g07_surfaces import svg

def load(s):
    return Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+s+'.xcf'))))

def save(im,s):
    run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/'Textures'/(ATLAS+'_'+s+'.xcf'))))
    run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(TEXTURES/(ATLAS+'_'+s+'.png'))),compression=9,include_color_profile=False)

def tile(im,name,height=False):
    p=GENERATED/(name+('_Height' if height else '_Color')+'.svg'); p.write_text(svg(name,height),encoding='utf-8')
    la=Gimp.file_load_layer(Gimp.RunMode.NONINTERACTIVE,im,Gio.File.new_for_path(str(p))); im.insert_layer(la,None,0)
    la.scale(256,256,False); la.set_offsets(*slots[name]['pixelRectTopLeft'][:2]); la.set_name(name); return la

color=load('BaseColor')
for name in CHANGED:
    color.remove_layer(next(la for la in color.get_layers() if la.get_name()==name)); tile(color,name)
save(color,'BaseColor'); color.delete()
normal=load('Normal'); old=next(la for la in normal.get_layers() if la.get_visible()); height=next(la for la in normal.get_layers() if not la.get_visible())
old.set_visible(False); height.set_visible(True)
for name in CHANGED:tile(normal,name,True)
height=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE); height.set_name('Registered G03 through G07 surface height'); height.set_visible(False)
computed=height.copy(); normal.insert_layer(computed,None,0); computed.set_visible(True)
filt(computed,'gegl:normal-map',scale=1.15,flip_x=False,flip_y=False,tileable=False)
for name in CHANGED:
    x,y,w,h=slots[name]['pixelRectTopLeft']; la=computed.copy(); normal.insert_layer(la,None,0); la.resize(w,h,-x,-y); la.set_offsets(x,y)
normal.remove_layer(computed);old.set_visible(True)
la=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);la.set_name('Trellis G03 through G07 tangent normal');height.set_visible(False)
save(normal,'Normal');normal.delete()
mask=load('SurfaceMask')
for name in CHANGED:
    la=next(la for la in mask.get_layers() if la.get_name().startswith(name+' '));la.set_opacity(SMOOTH[name]*100);la.set_name(name+' metallic 0 / smoothness '+str(SMOOTH[name]))
    slots[name]['smoothness']=SMOOTH[name];slots[name]['surfaceDetail']=dict(revision='G07 R01',registeredNormal=True)
    for key,val in zip(['base','highlight','shadow'],PALETTE[name]):slots[name][key]=val
save(mask,'SurfaceMask');mask.delete()
spec.setdefault('batchRevisions',{})['G07']='R01';(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(spec,indent=2))
(REVIEW/'GimpDetail.json').write_text(json.dumps(dict(status='PASS',atlas=ATLAS,size=2048,changed=CHANGED,newPages=0),indent=2))
print('G07_GIMP_PAINT_PASS',flush=True)
