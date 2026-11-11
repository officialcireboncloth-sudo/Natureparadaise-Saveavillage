"""Draw consistent paper seed packets and crop illustrations with native GIMP layers.

No image generation or external raster editor. All exports are transparent 256px.
The existing native-selection drawing helpers are reused without running their batch.
"""
from pathlib import Path
import ast, math, json, hashlib
from gi.repository import Gimp, Gegl, Gio

ROOT=Path(__file__).resolve().parents[3]
ART=Path(__file__).resolve().parent
DEST=ROOT/'Assets/Nature  Paradaise/Textures/UI/Icons'
SIZE=512
INK='#42513a'
CREAM='#f6ecd5'
namespace=dict(Path=Path,math=math,json=json,Gimp=Gimp,Gegl=Gegl,Gio=Gio,SIZE=SIZE,INK=INK,CREAM=CREAM)
old=ROOT/'ArtSource/UIIcons/HandPainted/draw_icons_gimp.py'
tree=ast.parse(old.read_text())
nodes=[n for n in tree.body if isinstance(n,(ast.FunctionDef,ast.ClassDef)) and n.name in ['path','oval','Drawing','export_png']]
exec(compile(ast.Module(body=nodes,type_ignores=[]),str(old),'exec'),namespace)
Drawing=namespace['Drawing'];path=namespace['path'];export_png=namespace['export_png']

PLANTS=[('watermelon','SEMANGKA'),('melon','MELON'),('zucchini','ZUCCHINI'),
        ('cucumber','TIMUN'),('pea','ERCIS'),('kiwi','KIWI'),
        ('green_bean','BUNCIS'),('chili_pepper','CABAI'),('eggplant','TERONG'),
        ('cabbage','KUBIS'),('broccoli','BROKOLI'),('cauliflower','KEMBANG KOL')]

def leaf(d,x,y,w,h,angle=0,color='#598d3b'):
    pts=path(('M',0,h/2),('C',-w*.6,0,-w*.45,-h*.4,0,-h/2),('C',w*.5,-h*.3,w*.55,0,0,h/2))
    a=math.radians(angle);c,s=math.cos(a),math.sin(a)
    d.poly('Leaf silhouette',[(x+c*px-s*py,y+s*px+c*py) for px,py in pts],color)
    d.stroke('Leaf vein',[(x-s*h*.34,y+c*h*.34),(x+s*h*.33,y-c*h*.33)],3,'#aec36b')

def fruit_outline(d,name,color,commands):
    d.curve(name,color,*commands)

def squash(d,kind):
    green='#396b37' if kind=='cucumber' else '#315b35'
    d.curve('Fruit silhouette',green,('M',125,342),('C',140,286,259,151,345,147),('C',397,146,404,193,373,225),('C',309,290,188,391,146,381),('C',123,376,116,360,125,342))
    d.curve('Fruit light plane','#5f9847',('M',143,342),('C',180,279,276,178,345,160),('C',362,156,375,166,378,178),('C',283,216,205,311,150,362),('L',143,342),outline=False)
    d.stroke('Fruit longitudinal seam',path(('M',166,356),('C',214,302,287,228,361,191)),4,'#8eae58')
    if kind=='cucumber':
        for x,y in [(177,329),(211,297),(246,255),(282,229),(317,195),(215,325),(252,285),(300,245),(338,218)]:
            d.ellipse('Cucumber bump',x,y,4,6,'#9eb969',False)
    else:
        for x,y in [(183,336),(229,292),(284,239),(333,201)]:d.stroke('Zucchini fine fleck',[(x,y),(x+2,y-4)],2,'#c4ca7d')
    d.poly('Stem',[(329,150),(344,123),(361,122),(361,151),(350,165)],'#82984b')
    d.stroke('Stem ridge',[(346,128),(345,151)],4,'#b7bd70')
    d.ellipse('Blossom end',136,365,9,7,'#d8b052')

def pods(d,kind):
    for dx,dy,angle in [(-63,20,-9),(21,-31,6),(65,35,15)]:
        # Three slender pods with distinct shoulders and pointed tips.
        pts=path(('M',215+dx,130+dy),('C',277+dx,152+dy,301+dx,311+dy,213+dx,408+dy),
                 ('C',243+dx,295+dy,215+dx,220+dy,201+dx,161+dy),('L',215+dx,130+dy))
        d.poly('Pod silhouette',pts,'#6ca242' if kind=='green_bean' else '#78aa43')
        d.stroke('Pod highlighted seam',path(('M',214+dx,160+dy),('C',246+dx,240+dy,265+dx,310+dy,225+dx,374+dy)),5,'#c1d475')
        d.stroke('Curved pod stalk',path(('M',211+dx,146+dy),('C',214+dx,125+dy,211+dx,108+dy,199+dx,105+dy)),8,'#647d39')
        if kind=='pea':
            for y in [205,242,279,316]:d.ellipse('Pea swelling',232+dx+(y-245)*.05,y+dy,15,19,'#91b84d',False)
    leaf(d,190,115,46,85,-65)

def cabbage(d):
    for x,y,w,h,a in [(150,310,109,162,-46),(364,314,111,166,49),(183,345,110,154,-77),(321,352,111,159,72)]:leaf(d,x,y,w,h,a,'#538d44')
    d.ellipse('Cabbage head outline',254,263,127,130,'#9bbd60')
    d.curve('Head folded leaf','#bed67e',('M',163,251),('C',146,176,259,116,319,164),('C',290,172,248,192,225,222),('C',211,247,203,267,200,302),('C',187,296,171,276,163,251),outline=False)
    d.curve('Head folded leaf right','#82ad50',('M',325,184),('C',382,217,391,320,319,367),('C',341,319,328,291,304,263),('C',318,236,327,207,325,184),outline=False)
    for cmds in [path(('M',186,206),('C',224,224,245,241,256,296)),path(('M',252,146),('C',269,184,289,215,302,263)),path(('M',155,288),('C',201,279,253,310,298,370)),path(('M',256,296),('C',284,275,318,273,368,283))]:d.stroke('Cabbage fold vein',cmds,5,'#d6e5a1')

def head_crop(d,kind):
    cauliflower=kind=='cauliflower'
    leaf(d,155,330,112,214,-35,'#548845');leaf(d,359,324,112,215,34,'#548845')
    d.poly('Thick crop stalk',[(224,287),(281,286),(300,418),(211,418)],'#89ad58')
    d.stroke('Stalk highlighted ridge',[(247,308),(246,401)],13,'#b7ce77')
    centres=[(177,245,56),(250,202,66),(328,242,64),(210,285,55),(287,288,61),(141,286,40),(365,289,36)]
    for i,(x,y,r) in enumerate(centres):
        d.ellipse('Head cluster',x,y,r,r*.83,'#e9dfb2' if cauliflower else '#367d48')
        d.ellipse('Head cluster light',x-8,y-11,r*.72,r*.50,'#faf1d4' if cauliflower else '#579e55',False)
        for j in range(8):
            a=j*math.tau/8;xx=x+math.cos(a)*r*.52;yy=y+math.sin(a)*r*.41
            d.ellipse('Floret detail',xx,yy,7 if cauliflower else 5,6 if cauliflower else 5,'#e3d6a2' if cauliflower else '#91b665',False)
    if cauliflower:
        leaf(d,186,359,99,151,-65,'#67934b');leaf(d,329,362,103,158,64,'#67934b')

def melon(d,kind):
    if kind=='watermelon':
        d.ellipse('Watermelon rind',235,242,144,118,'#3e7d3c')
        for x,y,w in [(151,233,16),(196,221,17),(247,226,18),(295,235,16)]:
            d.curve('Watermelon stripe','#9eb957',('M',x,y-86),('C',x-29,y-45,x-30,y+45,x,y+94),('L',x+w,y+86),('C',x-5,y+35,x-5,y-31,x+w,y-83),('L',x,y-86),outline=False)
        d.curve('Watermelon slice rind','#487a37',('M',190,292),('L',404,232),('C',438,328,367,414,270,408),('C',225,400,203,355,190,292))
        d.curve('Watermelon white rind','#e3e0a2',('M',202,299),('L',397,244),('C',421,330,355,402,274,395),('C',232,386,213,346,202,299),outline=False)
        d.curve('Watermelon red flesh','#e85032',('M',212,301),('L',389,252),('C',407,324,350,388,278,382),('C',243,373,224,340,212,301),outline=False)
        for x,y,a in [(255,319,-30),(296,307,10),(340,286,25),(305,354,-45),(355,331,22)]:
            d.ellipse('Watermelon seed',x,y,4,8,'#573d30',False)
    else:
        d.ellipse('Melon rind',239,246,137,132,'#d8c37c')
        d.ellipse('Melon light plane',223,230,104,108,'#e5d18c',False)
        for i in range(5):
            x=141+i*40;d.stroke('Melon netting',path(('M',x,154),('C',x+40,211,x-25,284,x+35,340)),3,'#f4e8b1')
            y=171+i*34;d.stroke('Melon netting',path(('M',130,y),('C',210,y-25,287,y+42,351,y+1)),3,'#f4e8b1')
        d.ellipse('Cut melon rind',330,329,91,84,'#c3ad65');d.ellipse('Cut melon flesh',330,329,78,72,'#c8d78a',False)
        d.ellipse('Melon seed cavity',333,331,31,46,'#f0dea0',False)
        for x,y in [(323,307),(341,318),(321,331),(342,342),(324,357)]:d.ellipse('Melon seeds',x,y,4,8,'#b6924e',False)
    d.stroke('Curved fruit stem',path(('M',236,124),('C',232,93,255,92,268,110)),11,'#6d853b')

def kiwi(d):
    d.ellipse('Whole kiwi',200,247,108,133,'#9b6c40')
    d.ellipse('Whole kiwi highlight',182,229,73,101,'#b48650',False)
    for i in range(24):
        a=i*2.399963;x=193+math.cos(a)*75*(i%5+2)/7;y=247+math.sin(a)*101*(i%4+2)/6
        d.stroke('Kiwi skin fleck',[(x,y),(x+3,y-4)],2,'#c69d68')
    d.ellipse('Kiwi cut rind',326,309,101,111,'#93623b')
    d.ellipse('Kiwi cut flesh',326,309,91,101,'#8dbd45',False)
    d.ellipse('Kiwi inner flesh',326,309,62,73,'#bdd46b',False)
    for i in range(17):
        a=i*math.tau/17;d.stroke('Kiwi radial flesh',[(326+math.cos(a)*23,309+math.sin(a)*31),(326+math.cos(a)*78,309+math.sin(a)*88)],2,'#dce69a')
        d.ellipse('Kiwi black seed',326+math.cos(a)*52,309+math.sin(a)*64,3,5,'#38452c',False)
    d.ellipse('Kiwi white core',326,309,22,32,'#f1ebc3',False)

def eggplant(d):
    d.curve('Eggplant silhouette','#623e87',('M',293,154),('C',348,203,371,279,349,355),('C',329,423,236,439,180,398),('C',122,357,163,266,223,203),('C',247,178,269,161,293,154))
    d.curve('Eggplant shaded plane','#452f67',('M',293,170),('C',354,244,363,335,314,388),('C',288,415,248,420,213,405),('C',298,397,329,297,276,181),('L',293,170),outline=False)
    d.curve('Eggplant gloss','#9a72ad',('M',249,207),('C',210,250,178,317,191,342),('C',213,353,227,269,267,213),('L',249,207),outline=False)
    d.poly('Eggplant calyx',[(275,141),(309,150),(331,187),(299,177),(287,206),(267,181),(237,184),(252,158)],'#6c973e')
    d.stroke('Eggplant curved green stem',path(('M',285,156),('C',301,118,277,100,252,107)),15,'#68833b')
    leaf(d,202,125,60,100,-60)

def chili(d):
    for dx,dy,color,shadow in [(-58,17,'#c9412e','#9d332a'),(34,-18,'#e55230','#b33726')]:
        d.curve('Chili silhouette',color,('M',229+dx,149+dy),('C',287+dx,156+dy,297+dx,249+dy,268+dx,320+dy),('C',248+dx,367+dy,204+dx,397+dy,167+dx,409+dy),('C',225+dx,347+dy,239+dx,281+dy,214+dx,199+dy),('C',204+dx,170+dy,210+dx,153+dy,229+dx,149+dy))
        d.stroke('Chili gloss',path(('M',232+dx,184+dy),('C',256+dx,225+dy,256+dx,277+dy,239+dx,310+dy)),7,'#f89a60')
        d.poly('Chili calyx',[(212+dx,154+dy),(232+dx,143+dy),(257+dx,158+dy),(253+dx,180+dy),(238+dx,169+dy),(222+dx,183+dy)],'#6e933b')
        d.stroke('Chili stalk',path(('M',233+dx,151+dy),('C',235+dx,116+dy,258+dx,111+dy,271+dx,123+dy)),11,'#637e37')
    leaf(d,316,157,65,118,37)

def illustration(kind):
    d=Drawing('Crop_'+kind)
    if kind in ['watermelon','melon']:melon(d,kind)
    elif kind in ['zucchini','cucumber']:squash(d,kind)
    elif kind in ['pea','green_bean']:pods(d,kind)
    elif kind=='kiwi':kiwi(d)
    elif kind=='eggplant':eggplant(d)
    elif kind=='chili_pepper':chili(d)
    elif kind=='cabbage':cabbage(d)
    else:head_crop(d,kind)
    return d

def text(d,value,y,size,color):
    font=Gimp.Font.get_by_name('Segoe UI Bold')
    layer=Gimp.TextLayer.new(d.image,value,font,size,Gimp.Unit.pixel())
    d.image.insert_layer(layer,None,0);layer.set_color(Gegl.Color.new(color));layer.set_name('Print - '+value)
    layer.set_offsets((SIZE-layer.get_width())//2,y)

def packet(kind,label,art):
    d=Drawing('Seed_'+kind)
    d.poly('Paper side gusset',[(372,45),(398,58),(412,462),(381,481),(350,460)],'#bca178')
    # Deliberately regular serration: a real crimped paper seal, no generated lettering/noise.
    top=[(103,37)]+[(103+i*10,35+(i%2)*7) for i in range(29)]+[(383,48)]
    d.poly('Kraft paper front',top+[(385,463),(94,461)],'#ead8b3')
    d.poly('Paper front light plane',[(109,57),(143,57),(127,444),(104,447)],'#f2e4c5',False)
    d.poly('Paper right fold',[(358,56),(382,54),(385,461),(359,447),(364,278)],'#d3b98e',False)
    d.stroke('Top sealed seam',[(111,68),(373,72)],4,'#bea67e')
    d.stroke('Top seal highlight',[(111,59),(374,63)],3,'#fff1d1')
    d.stroke('Bottom folded seam',[(107,444),(357,447),(381,462)],4,'#c3aa83')
    for x,y in [(119,115),(335,144),(121,363),(337,405),(145,430),(345,94)]:
        d.stroke('Fine paper fibre',[(x,y),(x+15,y-1)],1.4,'#dcc7a2')
    # Two small leaves form a consistent printed mark.
    leaf(d,245,103,21,40,-38,'#6f8e45');leaf(d,268,101,21,42,40,'#6f8e45')
    d.poly('Printed seed label',[(128,128),(350,128),(350,190),(128,190)],'#567a43',False)
    text(d,'BENIH',132,20,'#e9eed1')
    text(d,label,157,22 if len(label)<11 else 19,'#fff3d4')
    layer=Gimp.Layer.new_from_drawable(art.get_layers()[0],d.image)
    d.image.insert_layer(layer,None,0);layer.set_name('Illustration - '+kind)
    layer.scale(288,288,False);layer.set_offsets(104,182)
    d.stroke('Printed bottom rule',[(145,430),(335,430)],2,'#bba27b')
    return d

def save_master(d,name):
    p=ART/'Masters'/(name+'.xcf');p.parent.mkdir(parents=True,exist_ok=True)
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,d.image,Gio.File.new_for_path(str(p)))
    return p

def runtime(image,dest):
    im=image.duplicate();im.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE);im.scale(256,256)
    dest.parent.mkdir(parents=True,exist_ok=True);export_png(im,dest)
    return im

def run():
    ART.mkdir(parents=True,exist_ok=True)
    sheet=Gimp.Image.new(1152,864,Gimp.ImageBaseType.RGB)
    bg=Gimp.Layer.new(sheet,'Sage review background',1152,864,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL)
    sheet.insert_layer(bg,None,0);Gimp.context_set_foreground(Gegl.Color.new('#344b3d'));bg.edit_fill(Gimp.FillType.FOREGROUND)
    records=[]
    for i,(kind,label) in enumerate(PLANTS):
        d=illustration(kind);master=save_master(d,'Crop_'+kind)
        crop=runtime(d.image,DEST/'Crops'/('icon_crop_'+kind+'.png'))
        # Mutation uses the same real produce with a small explicit gold star marker.
        star=Drawing('Mutation_'+kind)
        layer=Gimp.Layer.new_from_drawable(d.image.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE),star.image)
        star.image.insert_layer(layer,None,0)
        pts=[]
        for j in range(10):
            a=-math.pi/2+j*math.pi/5;r=45 if j%2==0 else 22
            pts.append((397+math.cos(a)*r,391+math.sin(a)*r))
        star.poly('Mutation star',pts,'#d7ac43')
        mutated=runtime(star.image,DEST/'Crops'/('icon_crop_'+kind+'_mutation.png'));mutated.delete();star.image.delete()
        packet_d=packet(kind,label,crop)
        # Crop sprite is 256px. Scale its illustration to 288px on the 512px packet master.
        packet_master=save_master(packet_d,'Seed_'+kind)
        icon=runtime(packet_d.image,DEST/'Seeds'/('icon_seed_'+kind+'.png'))
        copy=Gimp.Layer.new_from_drawable(icon.get_layers()[0],sheet);sheet.insert_layer(copy,None,0)
        copy.set_name(label);copy.set_offsets((i%4)*288+16,(i//4)*288+16)
        check=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(packet_master)))
        assert check.get_width()==check.get_height()==512 and len(check.get_layers())>12
        count=len(check.get_layers());check.delete()
        records.append(dict(plant=kind,label=label,size=[256,256],seedMaster=str(packet_master.relative_to(ROOT)),produceMaster=str(master.relative_to(ROOT)),layers=count,
            seed=str((DEST/'Seeds'/('icon_seed_'+kind+'.png')).relative_to(ROOT)),seedSha256=hashlib.sha256((DEST/'Seeds'/('icon_seed_'+kind+'.png')).read_bytes()).hexdigest()))
        icon.delete();crop.delete();packet_d.image.delete();d.image.delete()
        print('GIMP_SEED_PACKET_PASS '+kind,flush=True)
    export_png(sheet,ART/'SeedPackets_Preview.png')
    assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE,sheet,Gio.File.new_for_path(str(ART/'SeedPackets_Review.xcf')))
    sheet.delete()
    (ART/'Manifest.json').write_text(json.dumps(dict(status='PASS',renderer='GIMP native selections, editable layers and text',runtimeSize=[256,256],icons=records),indent=2))
    print('GIMP_SEED_PACKETS_COMPLETE 12 packets + 24 produce/mutation sprites',flush=True)

Gimp.context_push()
try:
    Gimp.context_set_antialias(True);Gimp.context_set_feather(False)
    Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
    run()
finally:Gimp.context_pop()
