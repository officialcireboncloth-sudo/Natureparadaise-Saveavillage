"""Native GIMP adds registered colour/height to unused Trellis tiles."""
import sys, json, math, random
from pathlib import Path
from gi.repository import Gimp, Gio
sys.path.insert(0, str(Path(__file__).resolve().parent))
from g06_config import *

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

def svg(name, height=False):
    base,hi,lo=PALETTE[name] if not height else ('#808080','#a0a0a0','#696969')
    if height and name=='OnionSkin':base,hi,lo='#808080','#8c8c8c','#777777'
    p=['<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256">']
    def line(d,c,w=1,opacity=1):
        p.append(f'<path d="{d}" stroke="{c}" stroke-width="{w}" opacity="{opacity}" fill="none" stroke-linecap="round"/>')
    if name=='RadishGradient':
        if height:p.append('<rect width="256" height="256" fill="#808080"/>')
        else:
            p.append('<defs><linearGradient id="red" x2="0" y2="1"><stop stop-color="#e81d35"/><stop offset=".68" stop-color="#e81731"/><stop offset="1" stop-color="#fff0dc"/></linearGradient></defs><rect width="256" height="256" fill="url(#red)"/>')
            pts=' '.join(f'{x},{171+7*math.sin(x*math.tau/108)+3*math.cos(x*math.tau/27):.2f}' for x in range(0,257,2))
            p.append(f'<polygon points="0,256 {pts} 256,256" fill="#f8e8ce"/>')
            p.append('<defs><linearGradient id="blend" x2="0" y2="1"><stop stop-color="#f7b9a9" stop-opacity="0"/><stop offset=".5" stop-color="#f8ddc5" stop-opacity=".65"/><stop offset="1" stop-color="#f8e8ce"/></linearGradient></defs><path d="M0 155 Q27 170 54 155 T108 155 T162 155 T216 155 T270 155 L256 188 L0 188Z" fill="url(#blend)"/>')
        rng=random.Random(607)
        for _ in range(550):
            x,y=rng.uniform(20,236),rng.uniform(20,236)
            p.append(f'<ellipse cx="{x:.2f}" cy="{y:.2f}" rx="{rng.uniform(.15,.45):.2f}" ry=".3" fill="{lo}" opacity=".16"/>')
        for i in range(22):
            x=23+i*10; y=45+(i*37)%150
            line(f'M{x} {y} q2 -1 4 0',lo,.55,.22)
    elif name in ['LeafLobed','LeafStrap','OnionSkin']:
        if height:p.append('<rect width="256" height="256" fill="#808080"/>')
        else:
            if name=='LeafLobed':stops='<stop stop-color="#5b9a1d"/><stop offset=".52" stop-color="#82b832"/><stop offset="1" stop-color="#639c20"/>'
            elif name=='LeafStrap':stops='<stop stop-color="#d2a638"/><stop offset=".14" stop-color="#dfb33f"/><stop offset=".24" stop-color="#8bae25"/><stop offset=".70" stop-color="#559719"/><stop offset="1" stop-color="#d1dc83"/>'
            else:stops='<stop stop-color="#d99540"/><stop offset=".25" stop-color="#f1b352"/><stop offset=".72" stop-color="#e69a35"/><stop offset="1" stop-color="#bd702a"/>'
            p.append(f'<defs><linearGradient id="g" x2="0" y2="1">{stops}</linearGradient></defs><rect width="256" height="256" fill="url(#g)"/>')
        if name=='LeafLobed':
            line('M128 239 Q124 149 128 19',lo,6,.25)
            line('M128 239 Q124 149 128 19','#b9d76b' if not height else '#b4b4b4',2.5,.95)
            for side in [-1,1]:
                for j in range(7):
                    y=60+j*23; tip=128+side*(65+8*math.sin(j))
                    line(f'M127 {y+26} Q{128+side*37} {y+3} {tip} {y-9}',lo,2.7,.30)
                    line(f'M127 {y+25} Q{128+side*37} {y+2} {tip} {y-10}',hi if height else '#bbce69',1.2,.83)
                    for k in [1,2,3]:
                        x=128+side*(13+k*12); yy=y+17-k*5
                        line(f'M{x} {yy} q{side*13} -5 {side*20} -18',hi,.65,.60)
                        line(f'M{x} {yy} q{side*8} 5 {side*18} 4',hi,.48,.36)
            rng=random.Random(655)
            for _ in range(110):
                x,y=rng.uniform(25,230),rng.uniform(25,230)
                p.append(f'<ellipse cx="{x}" cy="{y}" rx="3" ry="5" fill="{hi if height else "#bfcd6b"}" opacity=".035"/>')
        else:
            count=96 if name=='OnionSkin' else 27
            for j in range(count+1):
                x=20+216*j/count; jitter=1.2*math.sin(j*2.31)
                line(f'M{x:.3f} 0 C{x+jitter:.3f} 75 {x-jitter:.3f} 185 {x:.3f} 256',hi if j%3 else lo,.48 if j%3 else .75,.36 if name=='OnionSkin' else .25)
            if name=='OnionSkin':
                for j in range(18):
                    x=24+j*12; y=25+(j*29)%170
                    line(f'M{x} {y} q-1 12 1 29',lo,.75,.22)
    else:
        p.append(f'<rect width="256" height="256" fill="{base}"/>')
        if name=='OnionCut':
            for r in [17,37,58,80,103]:
                p.append(f'<circle cx="128" cy="128" r="{r}" stroke="{lo}" stroke-width="1.6" opacity=".55" fill="none"/><circle cx="128" cy="128" r="{r+2}" stroke="{hi}" stroke-width="1.4" opacity=".7" fill="none"/>')
        elif name=='RadishCut':
            for j in range(36):
                a=j*math.tau/36
                line(f'M128 128 L{128+104*math.cos(a):.2f} {128+104*math.sin(a):.2f}',hi,.8,.40)
        else:
            for j in range(20):
                x=7+j*13;line(f'M{x} 0 Q{x+2} 128 {x} 256',hi,.7,.28)
    p.append('</svg>'); return ''.join(p)

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
height=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE); height.set_name('Registered G03 + G04 + G06 surface height'); height.set_visible(False)
computed=height.copy(); normal.insert_layer(computed,None,0); computed.set_visible(True)
filt(computed,'gegl:normal-map',scale=1.15,flip_x=False,flip_y=False,tileable=False)
for name in CHANGED:
    x,y,w,h=slots[name]['pixelRectTopLeft']; la=computed.copy(); normal.insert_layer(la,None,0); la.resize(w,h,-x,-y); la.set_offsets(x,y)
normal.remove_layer(computed);old.set_visible(True)
la=normal.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);la.set_name('Trellis G03 + G04 + G06 tangent normal');height.set_visible(False)
save(normal,'Normal');normal.delete()
mask=load('SurfaceMask')
for name in CHANGED:
    la=next(la for la in mask.get_layers() if la.get_name().startswith(name+' '));la.set_opacity(SMOOTH[name]*100);la.set_name(name+' metallic 0 / smoothness '+str(SMOOTH[name]))
    slots[name]['smoothness']=SMOOTH[name];slots[name]['surfaceDetail']=dict(revision='G06 R01',registeredNormal=True)
    for key,val in zip(['base','highlight','shadow'],PALETTE[name]):slots[name][key]=val
save(mask,'SurfaceMask');mask.delete()
spec.setdefault('batchRevisions',{})['G06']='R01';(TEXTURES/(ATLAS+'_Slots.json')).write_text(json.dumps(spec,indent=2))
(REVIEW/'GimpDetail.json').write_text(json.dumps(dict(status='PASS',atlas=ATLAS,size=2048,changed=CHANGED,newPages=0),indent=2))
print('G06_GIMP_PAINT_PASS',flush=True)
