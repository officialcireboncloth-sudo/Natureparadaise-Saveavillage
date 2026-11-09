"""Native GIMP 3 authoring: additive machine enamel/rubber/copper and screen graphic.
Existing Cozy atlas pages are read-only. All new painted layers remain editable in XCF.
Run with GIMP's python-fu-eval batch interpreter.
"""
from pathlib import Path
from gi.repository import Gimp, Gegl, Gio
import json, random, math
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'; SOURCE=ROOT/'ArtSource/SoilScanner/Textures'; DEST=A/'Textures/Shared/Cozy'
SOURCE.mkdir(parents=True,exist_ok=True)
random.seed(61106); SIZE=512
def run(name,**values):
    p=Gimp.get_pdb().lookup_procedure(name); c=p.create_config()
    for k,v in values.items(): c.set_property(k.replace('_','-'),v)
    r=p.run(c); assert r.index(0)==Gimp.PDBStatusType.SUCCESS,(name,str(r.index(0)))
def save(im,name):
    run('gimp-xcf-save',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(SOURCE/(name+'.xcf'))))
    run('file-png-export',run_mode=Gimp.RunMode.NONINTERACTIVE,image=im,file=Gio.File.new_for_path(str(DEST/(name+'.png'))),compression=9,include_color_profile=False)
def layer(im,name,w,h,color,x=0,y=0):
    la=Gimp.Layer.new(im,name,w,h,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL);im.insert_layer(la,None,0)
    if color: Gimp.context_set_foreground(Gegl.Color.new(color));la.fill(Gimp.FillType.FOREGROUND)
    else: la.fill(Gimp.FillType.TRANSPARENT)
    la.set_offsets(x,y);return la
def stroke(la,pts,color,width,opacity=100):
    Gimp.context_set_foreground(Gegl.Color.new(color));Gimp.context_set_brush_size(max(1.0,width));Gimp.context_set_opacity(opacity)
    Gimp.paintbrush_default(la,[float(v) for p in pts for v in p]);Gimp.context_set_opacity(100)
def poly(im,la,points,color):
    im.select_polygon(Gimp.ChannelOps.REPLACE,[float(v) for p in points for v in p]);Gimp.context_set_foreground(Gegl.Color.new(color));la.edit_fill(Gimp.FillType.FOREGROUND);Gimp.Selection.none(im)
def ellipse(im,la,x,y,w,h,color):
    im.select_ellipse(Gimp.ChannelOps.REPLACE,x,y,w,h);Gimp.context_set_foreground(Gegl.Color.new(color));la.edit_fill(Gimp.FillType.FOREGROUND);Gimp.Selection.none(im)
Gimp.context_push();Gimp.context_set_brush(Gimp.Brush.get_by_name('2. Hardness 100'));Gimp.context_enable_dynamics(False)
im=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
spec=[('EnamelTeal',(0,0,256,128),'#32606a',.12,.28),('EnamelSage',(0,128,256,128),'#619391',.10,.26),('Copper',(256,0,128,256),'#bc825c',.65,.36),('Rubber',(384,0,128,256),'#283c43',0,.16),('Screen',(0,256,256,256),'#193d44',0,.22),('Signal',(256,256,128,128),'#6ee69b',0,.28),('LabelCream',(384,256,128,128),'#f1dfb6',0,.14),('Soil',(256,384,128,128),'#8e5836',0,.10),('Leaf',(384,384,128,128),'#a4bd55',0,.12)]
slots=[]
for name,(x,y,w,h),color,metal,smooth in spec:
    la=layer(im,name,w,h,color,x,y)
    if name in ('EnamelTeal','EnamelSage','Copper','Rubber'):
        for j in range(100):
            px=x+random.uniform(3,w-5);py=y+random.uniform(3,h-5)
            stroke(la,[(px,py),(min(x+w-2,px+random.uniform(3,22)),py+.4)],'#cedacb' if name!='Copper' else '#f9d4a4',random.uniform(.4,1.1),random.uniform(2,6))
    p=8;slots.append(dict(name=name,pixelRectTopLeft=[x,y,w,h],uvSafe=[(x+p)/SIZE,1-(y+h-p)/SIZE,(x+w-p)/SIZE,1-(y+p)/SIZE],metallic=metal,smoothness=smooth))
# Graphic in the lower-left quarter, all coordinates absolute canvas pixels.
g=layer(im,'Screen - soil strata, sprout and three status bars',256,256,None,0,256)
# Soil sample rounded border and wavy strata.
stroke(g,[(37,463),(115,463),(123,370),(112,359),(90,354),(70,363),(52,356),(37,362),(30,458),(37,463)],'#eedbb5',7)
poly(im,g,[(37,457),(115,457),(120,374),(109,363),(91,359),(71,368),(51,361),(41,367)],'#825033')
for j,col in enumerate(['#b77948','#9b603a','#c48a53']):
    y=389+j*22;pts=[(39,y),(56,y-3),(76,y+3),(94,y-1),(117,y+1),(116,y+15),(96,y+13),(76,y+17),(57,y+11),(38,y+14)]
    poly(im,g,pts,col)
for x,y,r in [(49,439,6),(78,449,4),(100,442,5),(50,383,3),(106,382,3)]:ellipse(im,g,x-r,y-r,r*2,r*2,'#a9754c')
stroke(g,[(78,363),(80,337),(84,320)],'#98b654',5)
poly(im,g,[(82,337),(68,335),(57,327),(53,313),(52,302),(67,304),(78,314),(83,330)],'#adc55d')
poly(im,g,[(82,335),(89,316),(101,307),(119,305),(117,318),(106,331),(91,336)],'#afc960')
stroke(g,[(80,333),(61,312)],'#799843',1.6)
stroke(g,[(85,332),(110,312)],'#799843',1.6)
for y,col in [(340,'#a3b95e'),(383,'#eca448'),(426,'#dc7256')]:
    stroke(g,[(155,y),(222,y)],'#f3dfb6',29)
    stroke(g,[(155,y),(222,y)],col,21)
save(im,'NP_CozySurfaces_04_BaseColor');im.delete()
normal=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB);layer(normal,'Neutral tangent normal - enamel is smooth',SIZE,SIZE,'#8080ff');save(normal,'NP_CozySurfaces_04_Normal');normal.delete()
mask=Gimp.Image.new(SIZE,SIZE,Gimp.ImageBaseType.RGB)
for s in slots:
    x,y,w,h=s['pixelRectTopLeft'];la=layer(mask,s['name']+' metal R smooth A',w,h,None,x,y)
    color=Gegl.Color.new('#%02xff00'%round(s['metallic']*255));Gimp.context_set_foreground(color);la.fill(Gimp.FillType.FOREGROUND);la.set_opacity(s['smoothness']*100)
save(mask,'NP_CozySurfaces_04_SurfaceMask');mask.delete();Gimp.context_pop()
(DEST/'NP_CozyAtlasSlots_04.json').write_text(json.dumps(dict(version=4,date='2026-11-06',colorSize=[SIZE,SIZE],paddingPixels=8,authoring='Native GIMP 3 painted editable layers; additive machine page',existingAtlasPagesModified=False,slots=slots),indent=2))
print('SCANNER_GIMP_PASS',flush=True)
