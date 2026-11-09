"""Native GIMP: additive Cozy atlas page 05, red enamel and a pressure-gauge dial.
Existing shared pages 01-04 are read-only. No external/generated image is used.
"""
from pathlib import Path
from gi.repository import Gimp, Gegl, Gio
import math, random, json

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
DEST=ROOT/'Assets/Nature  Paradaise/Textures/Shared/Cozy'
SOURCE=ROOT/'ArtSource/FertilizerMaker/Textures'
SOURCE.mkdir(parents=True,exist_ok=True)
W,H=512,256
random.seed(81126)

def layer(image,name,color):
    out=Gimp.Layer.new(image,name,W,H,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL)
    image.insert_layer(out,None,0)
    if color:
        Gimp.context_set_foreground(Gegl.Color.new(color));out.fill(Gimp.FillType.FOREGROUND)
    return out

def rect(image,drawable,x,y,w,h,color):
    image.select_rectangle(Gimp.ChannelOps.REPLACE,x,y,w,h)
    Gimp.context_set_foreground(Gegl.Color.new(color));drawable.edit_fill(Gimp.FillType.FOREGROUND)
    Gimp.Selection.none(image)

def ellipse(image,drawable,x,y,w,h,color):
    image.select_ellipse(Gimp.ChannelOps.REPLACE,x,y,w,h)
    Gimp.context_set_foreground(Gegl.Color.new(color));drawable.edit_fill(Gimp.FillType.FOREGROUND)
    Gimp.Selection.none(image)

def polygon(image,drawable,points,color):
    image.select_polygon(Gimp.ChannelOps.REPLACE,[v for point in points for v in point])
    Gimp.context_set_foreground(Gegl.Color.new(color));drawable.edit_fill(Gimp.FillType.FOREGROUND)
    Gimp.Selection.none(image)

def save(image,name):
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,image,Gio.File.new_for_path(str(SOURCE/(name+'.xcf'))))
    proc=Gimp.get_pdb().lookup_procedure('file-png-export');config=proc.create_config()
    for key,value in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,
                          file=Gio.File.new_for_path(str(DEST/(name+'.png'))),compression=9,
                          include_color_profile=False).items():config.set_property(key.replace('_','-'),value)
    assert proc.run(config).index(0)==Gimp.PDBStatusType.SUCCESS

Gimp.context_push()
try:
    Gimp.context_set_antialias(True);Gimp.context_set_feather(False)
    image=Gimp.Image.new(W,H,Gimp.ImageBaseType.RGB)
    base=layer(image,'Base - warm red enamel and aged cream dial',None)
    rect(image,base,0,0,256,256,'#952f29');rect(image,base,256,0,256,256,'#e9d5ad')
    texture=layer(image,'Enamel - restrained hand-painted flecks',None)
    for i in range(380):
        x,y=random.uniform(1,252),random.uniform(1,252);r=random.uniform(.45,1.8)
        ellipse(image,texture,x,y,r,r,'rgba(0.36,0.075,0.05,0.085)' if i%2 else 'rgba(0.9,0.53,0.33,0.08)')
    texture.set_opacity(6)
    dial=layer(image,'Dial - circular border and graduated scale',None)
    # UV-safe dial circle has no baked pointer; the needle is modelled separately.
    ellipse(image,dial,267,11,234,234,'#b59f7d')
    ellipse(image,dial,270,14,228,228,'#ead8b3')
    ellipse(image,dial,274,18,220,220,'#e6d0a4')
    ellipse(image,dial,281,25,206,206,'#efdfbb')
    cx,cy=384,128
    for i in range(17):
        a=math.radians(145+i*15.625);r0=78 if i%2==0 else 86;r1=97
        dx,dy=math.cos(a),math.sin(a);nx,ny=-dy*1.65,dx*1.65
        polygon(image,dial,[(cx+dx*r0+nx,cy+dy*r0+ny),(cx+dx*r1+nx,cy+dy*r1+ny),
                            (cx+dx*r1-nx,cy+dy*r1-ny),(cx+dx*r0-nx,cy+dy*r0-ny)],'#47483c')
    ellipse(image,dial,380,124,8,8,'#847b63')
    save(image,'NP_CozySurfaces_05_BaseColor');image.delete()
    normal=Gimp.Image.new(W,H,Gimp.ImageBaseType.RGB)
    layer(normal,'Neutral tangent normal - smooth enamel', '#8080ff')
    save(normal,'NP_CozySurfaces_05_Normal');normal.delete()
    mask=Gimp.Image.new(W,H,Gimp.ImageBaseType.RGB)
    m=layer(mask,'Enamel metal R 0.12 / smoothness A 0.30',None)
    rect(mask,m,0,0,256,256,'#1fff00');m.set_opacity(30)
    m=layer(mask,'Dial metal R 0 / smoothness A 0.12',None)
    rect(mask,m,256,0,256,256,'#00ff00');m.set_opacity(12)
    save(mask,'NP_CozySurfaces_05_SurfaceMask');mask.delete()
    slots=[dict(name='EnamelRed',pixelRectTopLeft=[0,0,256,256],uvSafe=[8/W,8/H,248/W,248/H],metallic=.12,smoothness=.30),
           dict(name='GaugeFace',pixelRectTopLeft=[256,0,256,256],uvSafe=[264/W,8/H,504/W,248/H],metallic=0,smoothness=.12)]
    (DEST/'NP_CozyAtlasSlots_05.json').write_text(json.dumps(dict(version=5,colorSize=[W,H],paddingPixels=8,
        authoring='Native GIMP editable enamel and gauge layers',existingAtlasPagesModified=False,slots=slots),indent=2))
    print('FERTILIZER_GIMP_ATLAS_PASS page=05 size=512x256',flush=True)
finally:
    Gimp.context_pop()
