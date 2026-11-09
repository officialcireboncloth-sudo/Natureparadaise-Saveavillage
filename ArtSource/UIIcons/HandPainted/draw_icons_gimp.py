"""Draw the tool and fertilizer-chemical icons with native GIMP selections/layers.

No generated imagery, external renderer or raster editing library is used.
512px editable masters are downsampled in GIMP to the existing 128px UI sprites.
"""
from pathlib import Path
import math
import json
from gi.repository import Gimp, Gegl, Gio

ROOT = Path(__file__).resolve().parents[3]
ART = Path(__file__).resolve().parent
ICONS = ROOT / 'Assets/Nature  Paradaise/Textures/UI/Icons'
SIZE = 512
INK = '#584839'
WOOD = '#b98b54'
WOOD_DARK = '#936b42'
WOOD_LIGHT = '#d2ad77'
STEEL = '#a6afa2'
STEEL_DARK = '#748577'
STEEL_LIGHT = '#d0d4bc'
SAGE = '#81916a'
CREAM = '#efdfb6'


def path(*commands):
    """Sample explicit Bezier drawing points for GIMP's polygon selection."""
    points = []
    current = (0, 0)
    for cmd in commands:
        if cmd[0] in ('M', 'L'):
            current = cmd[1:3]
            points.append(current)
        elif cmd[0] == 'C':
            start = current
            for i in range(1, 17):
                t = i / 16
                s = 1 - t
                points.append(tuple(s**3 * start[j] + 3*s*s*t*cmd[1+j] +
                                    3*s*t*t*cmd[3+j] + t**3*cmd[5+j] for j in (0, 1)))
            current = cmd[5:7]
    return points


def oval(cx, cy, rx, ry, count=48):
    return [(cx + rx*math.cos(i*math.tau/count), cy + ry*math.sin(i*math.tau/count)) for i in range(count)]


class Drawing:
    def __init__(self, name, angle=0):
        self.name = name
        self.image = Gimp.Image.new(SIZE, SIZE, Gimp.ImageBaseType.RGB)
        self.angle = math.radians(angle)

    def transform(self, points):
        c, s = math.cos(self.angle), math.sin(self.angle)
        return [(256 + c*(x-256)-s*(y-256), 256+s*(x-256)+c*(y-256)) for x, y in points]

    def layer(self, name):
        layer = Gimp.Layer.new(self.image, name, SIZE, SIZE, Gimp.ImageType.RGBA_IMAGE, 100, Gimp.LayerMode.NORMAL)
        self.image.insert_layer(layer, None, 0)
        return layer

    def select(self, points):
        self.image.select_polygon(Gimp.ChannelOps.REPLACE, [v for point in points for v in point])

    def fill(self, layer, color):
        Gimp.context_set_foreground(Gegl.Color.new(color))
        assert layer.edit_fill(Gimp.FillType.FOREGROUND)

    def shape(self, name, points, color, outline=True):
        layer = self.layer(name)
        points = self.transform(points)
        self.select(points)
        if outline:
            Gimp.Selection.grow(self.image, 4)
            self.fill(layer, INK)
            self.select(points)
        self.fill(layer, color)
        Gimp.Selection.none(self.image)
        return layer

    def poly(self, name, points, color, outline=True):
        return self.shape(name, points, color, outline)

    def curve(self, name, color, *commands, outline=True):
        return self.shape(name, path(*commands), color, outline)

    def ellipse(self, name, cx, cy, rx, ry, color, outline=True):
        return self.shape(name, oval(cx, cy, rx, ry), color, outline)

    def stroke(self, name, points, width, color):
        layer = self.layer(name)
        points = self.transform(points)
        for a, b in zip(points, points[1:]):
            dx, dy = b[0]-a[0], b[1]-a[1]
            length = max(.001, math.hypot(dx, dy))
            nx, ny = -dy/length*width/2, dx/length*width/2
            self.select([(a[0]+nx,a[1]+ny), (b[0]+nx,b[1]+ny), (b[0]-nx,b[1]-ny), (a[0]-nx,a[1]-ny)])
            self.fill(layer, color)
        for x, y in points:
            self.select(oval(x, y, width/2, width/2, 16))
            self.fill(layer, color)
        Gimp.Selection.none(self.image)
        return layer

    def ring(self, name, cx, cy, rx, ry, width, color):
        layer = self.ellipse(name, cx, cy, rx, ry, color)
        self.select(self.transform(oval(cx, cy, rx-width, ry-width)))
        assert layer.edit_clear()
        Gimp.Selection.none(self.image)
        return layer

    def grain(self, x, top, bottom):
        self.stroke('Wood - long grain', [(x,top),(x-1,top+24),(x+2,bottom-12),(x,bottom)], 2, '#a57849')
        self.stroke('Wood - short grain', [(x+11,top+35),(x+10,top+62)], 2, '#c79e66')

    def handle(self, top=162, bottom=440):
        self.curve('Wood - silhouette', WOOD,
                   ('M',241,top),('C',245,top+62,237,bottom-64,238,bottom-17),
                   ('C',239,bottom+4,267,bottom+3,272,bottom-12),
                   ('L',270,top),('L',241,top))
        self.curve('Wood - shaded right plane', WOOD_DARK,
                   ('M',260,top+4),('L',269,top+4),('L',270,bottom-14),
                   ('C',265,bottom-2,258,bottom-1,251,bottom-4),('L',259,bottom-17),
                   ('L',260,top+4),outline=False)
        self.curve('Wood - worn highlight', WOOD_LIGHT,
                   ('M',244,top+14),('L',252,top+12),('L',249,bottom-23),
                   ('L',241,bottom-17),('L',244,top+14),outline=False)
        self.grain(253, top+56, bottom-28)
        self.stroke('Grip end - small wear',[(246,bottom-18),(257,bottom-21)],3,'#d9b986')

    def socket(self, y=163, height=43):
        self.poly('Iron - socket',[(236,y),(274,y+2),(273,y+height),(238,y+height-1)],STEEL_DARK)
        self.poly('Iron - socket light',[(240,y+5),(251,y+6),(249,y+height-6),(241,y+height-5)],STEEL,False)
        self.ellipse('Iron - rivet',261,y+height-11,4,4,STEEL_LIGHT,False)

    def save(self, group, filename):
        master = ART / (self.name + '.xcf')
        assert Gimp.file_save(Gimp.RunMode.NONINTERACTIVE, self.image, Gio.File.new_for_path(str(master)))
        runtime = self.image.duplicate()
        runtime.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
        runtime.select_item(Gimp.ChannelOps.REPLACE,runtime.get_layers()[0])
        Gimp.Selection.sharpen(runtime)
        nonempty,left,top,right,bottom=Gimp.Selection.bounds(runtime)[-5:]
        assert nonempty,self.name
        Gimp.Selection.none(runtime)
        runtime.crop(right-left,bottom-top,left,top)
        scale=116/max(runtime.get_width(),runtime.get_height())
        w,h=round(runtime.get_width()*scale),round(runtime.get_height()*scale)
        runtime.scale(w,h)
        runtime.resize(128,128,(128-w)//2,(128-h)//2)
        destination = ICONS / group / filename
        destination.parent.mkdir(parents=True,exist_ok=True)
        export_png(runtime,destination)
        preview = runtime.duplicate()
        self.image.delete()
        runtime.delete()
        print('GIMP_DRAWN ' + self.name,flush=True)
        return dict(name=self.name,group=group,file=str(destination.relative_to(ROOT)),master=str(master.relative_to(ROOT))), preview


def export_png(image, destination):
    proc=Gimp.get_pdb().lookup_procedure('file-png-export')
    config=proc.create_config()
    for key,value in dict(run_mode=Gimp.RunMode.NONINTERACTIVE,image=image,
                          file=Gio.File.new_for_path(str(destination)),compression=9,
                          include_color_profile=False).items():
        config.set_property(key.replace('_','-'),value)
    assert proc.run(config).index(0)==Gimp.PDBStatusType.SUCCESS


def hoe():
    d=Drawing('Tool_Hoe',34);d.handle(157)
    d.curve('Hoe - blade silhouette',STEEL,('M',249,113),('L',365,106),('C',379,130,377,162,363,188),('L',281,193),('L',250,168),('L',249,113))
    d.poly('Hoe - underside',[(281,167),(374,154),(363,188),(281,193)],STEEL_DARK,False)
    d.stroke('Hoe - cutting edge',[(291,187),(359,182)],4,STEEL_LIGHT)
    d.poly('Hoe - neck',[(238,131),(271,126),(284,169),(259,179),(239,164)],STEEL_DARK)
    d.ellipse('Hoe - rivet',258,152,5,5,STEEL_LIGHT,False)
    return d


def axe():
    d=Drawing('Tool_Axe',32);d.handle(130)
    d.curve('Axe - forged head',STEEL,('M',224,112),('L',278,103),('C',323,115,361,106,390,88),('C',418,119,431,161,423,199),('C',370,179,330,171,278,179),('L',226,173),('L',224,112))
    d.curve('Axe - bevel',STEEL_LIGHT,('M',388,95),('C',411,127,422,164,419,190),('L',403,184),('C',403,148,395,121,375,105),('L',388,95),outline=False)
    d.poly('Axe - lower forged plane',[(227,149),(278,152),(340,150),(404,178),(403,184),(278,179),(226,173)],STEEL_DARK,False)
    d.poly('Axe - socket',[(238,109),(276,106),(278,179),(240,175)],'#819183')
    d.stroke('Axe - socket wear',[(243,116),(246,146)],4,STEEL_LIGHT)
    return d


def hammer():
    d=Drawing('Tool_Hammer',33);d.handle(164)
    d.curve('Hammer - block',STEEL,('M',169,112),('L',333,116),('C',342,117,345,126,344,136),('L',340,173),('L',176,180),('C',165,175,165,143,169,112))
    d.poly('Hammer - lower plane',[(174,159),(340,151),(340,173),(176,180)],STEEL_DARK,False)
    d.poly('Hammer - striking face',[(169,112),(193,114),(195,177),(176,180),(166,166)],'#8e9d8f')
    d.stroke('Hammer - worn edge',[(199,120),(287,123)],4,STEEL_LIGHT)
    d.poly('Hammer - socket',[(237,114),(274,115),(276,178),(237,179)],'#7c8c7c')
    d.ellipse('Hammer - pin',256,145,5,5,CREAM,False)
    return d


def pickaxe():
    d=Drawing('Tool_Pickaxe',32);d.handle(146)
    d.curve('Pickaxe - forged bow',STEEL,('M',111,198),('C',133,128,184,100,253,99),('C',320,91,382,118,412,179),('L',402,184),('C',353,148,308,137,257,143),('C',201,141,155,158,122,204),('L',111,198))
    d.curve('Pickaxe - shaded underside',STEEL_DARK,('M',120,194),('C',162,147,204,130,258,129),('C',315,120,367,141,404,178),('L',402,184),('C',353,148,308,137,257,143),('C',201,141,155,158,122,204),('L',120,194),outline=False)
    d.socket(105,67)
    return d


def pitchfork():
    d=Drawing('Tool_Pitchfork',26);d.handle(240)
    for i,x in enumerate([198,236,274,312]):
        d.poly('Fork - tine '+str(i),[(x-7,91),(x-3,72),(x+5,91),(x+6,213),(x-8,213)],STEEL)
        d.stroke('Fork - tine light '+str(i),[(x-3,101),(x-3,194)],3,STEEL_LIGHT)
    d.curve('Fork - shoulder',STEEL_DARK,('M',189,201),('L',318,201),('L',314,219),('C',297,235,279,241,269,255),('L',242,255),('C',229,238,206,235,193,219),('L',189,201))
    d.stroke('Fork - shoulder light',[(198,207),(310,207)],4,STEEL)
    d.socket(244,35)
    return d


def sickle():
    d=Drawing('Tool_Sickle',27);d.handle(277)
    d.curve('Sickle - curved steel',STEEL,('M',247,300),('C',172,271,147,205,173,146),('C',199,82,267,65,324,86),('C',366,102,394,133,395,168),('C',350,126,295,116,256,139),('C',202,170,209,228,265,267),('L',247,300))
    d.curve('Sickle - inner bevel',STEEL_LIGHT,('M',395,168),('C',350,126,295,116,256,139),('C',202,170,209,228,265,267),('L',256,282),('C',189,236,185,166,244,128),('C',295,101,356,119,395,168),outline=False)
    d.poly('Sickle - tang',[(244,269),(272,273),(272,307),(240,307)],STEEL_DARK)
    d.ellipse('Sickle - pin',254,288,4,4,CREAM,False)
    return d


def watering_can():
    d=Drawing('Tool_Watering_Can')
    d.ring('Can - rear handle',348,254,70,88,25,SAGE)
    d.stroke('Can - handle wear',path(('M',368,188),('C',399,203,406,238,399,262)),5,'#b2bc92')
    d.poly('Can - spout',[(187,277),(104,218),(83,184),(102,172),(125,198),(213,236)],SAGE)
    d.poly('Can - spout light',[(103,181),(121,201),(200,247),(190,256),(111,208),(93,184)],'#a9b58a',False)
    d.ellipse('Can - rose',91,181,30,22,'#a9b58a')
    for x,y in [(78,177),(91,172),(103,180),(88,188)]:d.ellipse('Can - rose hole',x,y,3,3,'#627451',False)
    d.curve('Can - carrying handle',SAGE,('M',201,218),('L',198,161),('C',200,125,298,126,306,160),('L',310,216),('L',289,217),('L',286,163),('C',281,149,224,150,220,166),('L',224,217),('L',201,218))
    d.curve('Can - body',SAGE,('M',166,217),('C',192,203,290,200,328,213),('L',341,365),('C',325,400,192,406,170,371),('L',166,217))
    d.curve('Can - side plane','#647a56',('M',300,213),('L',328,213),('L',341,365),('C',334,380,310,390,291,391),('L',300,213),outline=False)
    d.curve('Can - front light','#9caa7e',('M',179,232),('L',198,232),('L',203,364),('C',194,369,185,366,180,359),('L',179,232),outline=False)
    d.ellipse('Can - top rim',247,216,81,22,'#acb68c')
    d.ellipse('Can - fill opening',249,212,53,12,'#536a4d',False)
    d.stroke('Can - lower seam',path(('M',182,365),('C',217,378,288,378,325,362)),4,'#b2bb95')
    return d


def fishing_rod():
    d=Drawing('Tool_Fishing_Rod')
    line=path(('M',366,78),('C',441,145,449,270,391,330),('C',377,345,365,345,362,333))
    d.stroke('Rod - fishing line',line,3,'#c2c7ac')
    d.stroke('Rod - hook',path(('M',362,327),('L',360,344),('C',359,357,377,356,378,344)),5,'#8c9d92')
    d.poly('Rod - bamboo blank',[(135,418),(357,80),(365,84),(157,432)],WOOD)
    d.poly('Rod - light face',[(142,417),(359,86),(361,87),(150,424)],WOOD_LIGHT,False)
    for x,y in [(222,296),(279,210),(333,127)]:
        d.stroke('Rod - whipping',[(x-6,y-1),(x+6,y+7)],9,'#6c7760')
        d.ring('Rod - line guide',x+8,y+3,9,9,3,STEEL)
    d.poly('Rod - cork grip',[(125,415),(169,348),(191,363),(146,435)],'#a77f4b')
    for x,y in [(138,418),(147,404),(156,390),(165,376)]:
        d.stroke('Rod - cork bands',[(x,y),(x+12,y+7)],3,'#cca974')
    d.ellipse('Rod - reel outer',213,356,38,36,'#6f8273')
    d.ellipse('Rod - reel spool',214,354,23,23,'#c2c7b1')
    d.ellipse('Rod - reel hub',214,354,9,9,'#7c8c7b')
    d.stroke('Rod - crank',[(231,375),(246,391),(265,387)],7,INK)
    d.ellipse('Rod - crank grip',268,388,10,8,WOOD)
    return d


def shears():
    d=Drawing('Tool_Shears',-12)
    d.poly('Shears - left blade',[(238,270),(170,90),(179,75),(217,129),(274,250)],STEEL)
    d.poly('Shears - right blade',[(239,248),(306,121),(344,79),(348,96),(274,271)],'#bdc4b0')
    d.poly('Shears - left bevel',[(177,93),(217,129),(265,249),(253,253)],STEEL_LIGHT,False)
    d.stroke('Shears - left shank',[(247,258),(210,327)],28,STEEL_DARK)
    d.stroke('Shears - right shank',[(265,261),(302,325)],28,STEEL_DARK)
    d.ring('Shears - left grip',192,363,47,64,19,'#a76d54')
    d.ring('Shears - right grip',316,363,47,64,19,'#a76d54')
    d.stroke('Shears - left grip light',path(('M',175,314),('C',153,331,153,371,159,389)),4,'#d29a74')
    d.stroke('Shears - right grip light',path(('M',304,311),('C',281,325,282,366,286,383)),4,'#d29a74')
    d.ellipse('Shears - pivot',256,258,16,16,'#b6bda5')
    d.stroke('Shears - screw slot',[(249,254),(263,263)],3,INK)
    return d


def brush():
    d=Drawing('Tool_Brush',-16)
    d.curve('Brush - bristle silhouette','#c7a570',('M',126,265),('L',389,266),('L',375,345),('C',326,381,189,380,141,347),('L',126,265))
    for i,x in enumerate(range(143,378,15)):
        y=351+8*math.sin((x-143)/235*math.pi)
        d.stroke('Brush - bristle tuft '+str(i),[(x,281),(x-3,y)],8,'#ddbf86' if i%3 else '#b88f5e')
    d.ellipse('Brush - wooden back',256,264,132,62,WOOD)
    d.curve('Brush - lower wood plane',WOOD_DARK,('M',126,270),('C',164,312,350,316,388,269),('L',383,291),('C',327,331,181,333,133,294),('L',126,270),outline=False)
    d.stroke('Brush - wood grain',path(('M',152,257),('C',204,234,302,235,359,258)),3,WOOD_LIGHT)
    d.curve('Brush - leather strap','#a97351',('M',201,278),('L',206,201),('C',207,161,300,158,308,201),('L',317,280),('L',289,285),('L',281,205),('C',278,190,238,191,235,207),('L',231,285),('L',201,278))
    d.stroke('Brush - strap stitch',path(('M',214,265),('L',218,205),('C',219,183,266,175,290,195)),3,'#dbb383')
    d.ellipse('Brush - left strap rivet',216,278,5,5,CREAM)
    d.ellipse('Brush - right strap rivet',303,278,5,5,CREAM)
    return d


def milker():
    d=Drawing('Tool_Milker')
    hose=path(('M',316,178),('C',285,71,124,73,114,207))
    d.stroke('Milker - hose outline',hose,22,INK)
    d.stroke('Milker - rubber hose',hose,13,'#6c786b')
    d.stroke('Milker - hose light',path(('M',124,153),('C',143,97,237,104,278,137)),3,'#929e8a')
    d.poly('Milker - teat cup',[(95,204),(131,204),(138,310),(87,310)],STEEL)
    d.poly('Milker - cup side',[(119,209),(131,209),(138,310),(119,310)],STEEL_DARK,False)
    d.ellipse('Milker - cup collar',113,205,20,7,STEEL_LIGHT)
    d.poly('Milker - cup base',[(87,299),(138,299),(139,315),(87,315)],STEEL_DARK)
    d.ring('Milker - carry handle',346,169,46,35,12,WOOD)
    d.curve('Milker - cream body','#d5cfb0',('M',276,220),('L',274,193),('C',288,178,357,178,369,194),('L',370,221),('C',392,246,411,312,410,369),('C',399,408,270,415,245,374),('C',244,310,255,251,276,220))
    d.curve('Milker - shaded side','#b0b59b',('M',354,193),('L',369,194),('L',370,221),('C',392,246,411,312,410,369),('C',398,391,374,400,349,402),('C',371,352,376,285,354,222),('L',354,193),outline=False)
    d.curve('Milker - front light','#e9dfbc',('M',275,251),('C',264,284,259,332,264,366),('L',281,373),('C',275,333,281,283,291,248),('L',275,251),outline=False)
    d.ellipse('Milker - lid',321,193,48,17,STEEL)
    d.ellipse('Milker - lid inset',319,189,34,10,STEEL_DARK,False)
    d.curve('Milker - base rim',STEEL_DARK,('M',247,367),('C',289,392,367,392,408,365),('L',406,386),('C',369,417,278,418,249,390),('L',247,367))
    d.stroke('Milker - lower edge light',path(('M',264,389),('C',304,404,362,404,389,386)),4,STEEL)
    d.poly('Milker - side latch',[(284,204),(296,205),(293,236),(282,234)],STEEL_DARK)
    return d


LETTERS={
    'C':[[(.9,.08),(.65,0),(.2,.05),(0,.28),(0,.75),(.2,.96),(.65,1),(.9,.9)]],
    'a':[[(.85,.35),(.55,.28),(.12,.36),(0,.64),(.08,.9),(.4,1),(.78,.88),(.85,.35),(.85,1)]],
    'P':[[(.1,1),(.1,0),(.68,0),(.95,.18),(.95,.38),(.7,.53),(.1,.53)]],
    'U':[[(0,0),(0,.75),(.2,1),(.7,1),(.9,.75),(.9,0)]],
    'R':[[(.1,1),(.1,0),(.65,0),(.9,.15),(.9,.4),(.65,.52),(.1,.52)],[(.55,.52),(.98,1)]],
    'E':[[(.95,0),(.1,0),(.1,1),(.95,1)],[(.1,.48),(.75,.48)]],
    'A':[[(0,1),(.42,0),(.62,0),(1,1)],[(.22,.62),(.82,.62)]],
    'K':[[(.1,0),(.1,1)],[(.93,0),(.12,.53),(.95,1)]],
}


def lettering(d,text,x,y,w,h,color=CREAM):
    advance=w/len(text)
    for index,char in enumerate(text):
        for n,points in enumerate(LETTERS[char]):
            d.stroke('Label - '+char+' '+str(n),[(x+index*advance+px*advance*.77,y+py*h) for px,py in points],5.5,color)


def chemical(name,label,band,shadow,light,pellet_color):
    d=Drawing('Chemical_'+name)
    d.curve('Kraft - pouch outline','#c5a06c',('M',164,115),('L',346,111),('L',345,151),('C',357,177,364,223,372,262),('L',384,403),('C',350,438,167,440,130,406),('L',140,261),('C',145,218,153,172,165,151),('L',164,115))
    d.curve('Kraft - side fold','#aa8050',('M',322,150),('L',345,151),('C',357,177,364,223,372,262),('L',384,403),('C',369,420,345,426,320,429),('L',332,384),('L',325,237),('L',322,150),outline=False)
    d.curve('Kraft - worn front light','#dbbc85',('M',169,165),('L',186,167),('L',170,386),('L',157,404),('L',144,397),('L',153,261),('C',158,222,165,183,169,165),outline=False)
    d.poly('Kraft - folded top',[(163,114),(346,111),(349,139),(333,159),(178,160),(160,143)],'#d6b47c')
    d.stroke('Kraft - fold seam',[(175,139),(329,137)],4,'#a0784c')
    d.stroke('Kraft - top light',[(179,120),(328,117)],4,'#ead0a0')
    d.curve('Label - colored band',band,('M',149,246),('C',213,261,311,259,367,243),('L',376,346),('C',302,367,201,369,139,349),('L',149,246))
    d.poly('Label - shaded right fold',[(335,251),(367,243),(376,346),(337,356)],shadow,False)
    d.poly('Label - left worn plane',[(151,253),(166,256),(158,347),(143,344)],light,False)
    lettering(d,label,202 if len(label)<3 else 184,279,117 if len(label)<3 else 158,49)
    # A few physical granules above the label distinguish raw chemical from fertilizer.
    for i,(x,y,rx,ry) in enumerate([(217,211,13,10),(244,207,16,12),(272,211,12,11),(293,206,13,9)]):
        if name=='Phosphate':
            d.poly('Mineral - phosphate shard '+str(i),[(x-rx,y+ry),(x-rx-2,y-3),(x+3,y-ry),(x+rx,y+1),(x+4,y+ry)],pellet_color)
        elif name=='Potassium':
            d.poly('Mineral - potassium crystal '+str(i),[(x-rx,y),(x,y-ry),(x+rx,y-4),(x+rx-2,y+ry),(x-4,y+ry)],pellet_color)
        else:
            d.ellipse('Mineral - granule '+str(i),x,y,rx,ry,pellet_color)
            d.ellipse('Mineral - granule light '+str(i),x-3,y-3,4,3,'#f4e6c6',False)
    d.stroke('Kraft - bottom seam',path(('M',153,402),('C',206,418,302,421,357,403)),4,'#e1c493')
    d.stroke('Kraft - crease left',[(159,374),(150,391)],3,'#9c7448')
    d.stroke('Kraft - crease right',[(345,374),(356,393)],3,'#8f704a')
    return d


def review(images):
    # One reusable contact sheet made in GIMP, including the real fertilizer reference.
    sheet=Gimp.Image.new(1024,1024,Gimp.ImageBaseType.RGB)
    background=Gimp.Layer.new(sheet,'Dark sage background',1024,1024,Gimp.ImageType.RGBA_IMAGE,100,Gimp.LayerMode.NORMAL)
    sheet.insert_layer(background,None,0)
    Gimp.context_set_foreground(Gegl.Color.new('#29392f'))
    background.edit_fill(Gimp.FillType.FOREGROUND)
    for i,img in enumerate(images):
        img.scale(220,220)
        layer=Gimp.Layer.new_from_drawable(img.get_layers()[0],sheet)
        sheet.insert_layer(layer,None,0)
        layer.set_offsets((i%4)*256+18,(i//4)*256+18)
        img.delete()
    reference=Gimp.file_load(Gimp.RunMode.NONINTERACTIVE,Gio.File.new_for_path(str(ICONS/'FertilizerMaker/icon_fertilizer_basic.png')))
    reference.scale(220,220)
    layer=Gimp.Layer.new_from_drawable(reference.get_layers()[0],sheet)
    sheet.insert_layer(layer,None,0);layer.set_name('Existing fertilizer - palette reference');layer.set_offsets(786,786)
    reference.delete()
    export_png(sheet,ART/'Preview.png')
    sheet.delete()


Gimp.context_push()
try:
    Gimp.context_set_antialias(True)
    Gimp.context_set_feather(False)
    Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
    tools=[('hoe',hoe),('axe',axe),('hammer',hammer),('pickaxe',pickaxe),('pitchfork',pitchfork),
           ('sickle',sickle),('watering_can',watering_can),('fishing_rod',fishing_rod),
           ('shears',shears),('brush',brush),('milker',milker)]
    reports=[];previews=[]
    for name,draw in tools:
        info,img=draw().save('Tools','icon_tool_'+name+'.png');reports.append(info);previews.append(img)
    chemicals=[('Calcium','Ca','#748fa0','#5b7283','#9bb0b6','#d8d7bc'),
               ('Phosphate','P','#81916a','#64754f','#a5b18a','#949e87'),
               ('Urea','UREA','#ad7657','#8e5943','#c39370','#e1dcbd'),
               ('Potassium','K','#bc984e','#99783f','#d0b474','#c0a290')]
    for name,label,band,shadow,light,pellets in chemicals:
        info,img=chemical(name,label,band,shadow,light,pellets).save('FertilizerMaker','icon_chemical_'+name.lower()+'.png')
        reports.append(info);previews.append(img)
    review(previews)
    (ART/'Manifest.json').write_text(json.dumps(dict(renderer='GIMP native selections and editable layers',runtime_size=[128,128],icons=reports),indent=2),encoding='utf-8')
    print('GIMP_TOOL_CHEMICAL_PASS icons='+str(len(reports)),flush=True)
finally:
    Gimp.context_pop()
