"""Paint fruit-tree icons, compose kraft packets and export layered masters in GIMP."""
import math
import os
import random
from pathlib import Path
from xml.sax.saxutils import escape

import gi
gi.require_version('Gimp', '3.0')
from gi.repository import Gimp, Gio


ROOT = Path(__file__).resolve().parents[3]
WORK = Path(os.environ['NP_TREE_ICON_STAGE'])
BLANK = ROOT / 'ArtSource/UIIcons/Seeds/Generated/KraftSeedPacket_Blank.png'
RUN = Gimp.RunMode.NONINTERACTIVE
FRUIT = {
    'apple': ('Apel', 'M127 278C105 214 128 156 179 149C216 141 237 164 255 159C284 150 298 137 335 151C389 165 403 222 385 282C369 339 340 392 301 396C279 398 263 386 247 387C226 388 211 401 189 390C162 378 140 323 127 278Z',
              ('#f88167','#e44e51','#972d40','#68283a'), None),
    'avocado': ('Alpukat', 'M98 272C104 223 152 162 165 121C180 70 222 62 243 104C256 131 251 181 279 224C306 262 338 315 306 354C276 398 170 399 124 360C100 339 91 308 98 272Z',
                ('#84a052','#54753e','#2f5235','#2b4434'), 'avocado'),
    'guava': ('Jambu biji', 'M94 269C90 222 118 170 152 152C173 139 184 109 210 113C238 116 242 146 262 161C306 181 329 215 323 259C316 326 279 365 218 372C150 381 105 338 94 269Z',
              ('#d8dc7d','#a5be55','#63853e','#4a6336'), 'guava'),
    'mango': ('Mangga', 'M91 259C101 216 129 179 159 132C183 92 229 91 263 113C305 141 312 191 308 223C303 285 274 342 223 373C180 400 119 386 98 349C84 326 81 288 91 259Z',
              ('#ffd65c','#f3a638','#da642a','#94512c'), 'mango'),
    'orange': ('Jeruk', 'M80 258C77 180 123 132 192 129C274 124 326 177 326 253C326 323 277 376 206 381C135 385 84 335 80 258Z',
               ('#ffc658','#f28d31','#cd581f','#9b4c25'), 'orange'),
    'pear': ('Pir', 'M223 104C253 101 260 129 267 154C277 198 322 232 338 274C363 338 316 379 249 383C172 389 121 361 118 313C115 271 142 240 167 204C191 168 184 111 223 104Z',
             ('#f4db79','#d3b34f','#ab7930','#775323'), None),
    'banana': ('Pisang', '', ('#ffed91','#edc341','#d99a29','#895e2c'), None),
    'coconut': ('Kelapa', 'M79 269C78 203 111 144 167 126C240 100 315 154 327 228C340 295 298 359 233 381C169 397 101 357 84 299Z',
                ('#bc8858','#946442','#5e3d2d','#513627'), 'coconut'),
    'cherry': ('Cherry', '', ('#fa8b76','#de4156','#9e2644','#6d2941'), None),
    'rambutan': ('Rambutan', '', ('#fa8a57','#d54f31','#a82d27','#732721'), None),
    'durian': ('Durian', '', ('#d6c36e','#a49a50','#76703b','#535632'), None),
    'oilpalm': ('Sawit', '', ('#f08c35','#ba4626','#7b2d29','#50292a'), None),
}
ORDER = ('apple', 'avocado', 'cherry', 'guava', 'mango', 'orange', 'coconut', 'rambutan', 'durian', 'pear',
         'banana', 'oilpalm')
ILLUSTRATIONS = os.environ.get('NP_TREE_ILLUSTRATIONS')
GREEN = ('#bdce74', '#6b983d', '#3d672f')


def path(d, fill, stroke='none', width=5):
    return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" stroke-linejoin="round" stroke-linecap="round"/>'


def ellipse(x, y, rx, ry, fill, extra=''):
    return f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="{fill}" {extra}/>'


def gradient(name, colors):
    stops = ''.join(f'<stop offset="{offset}" stop-color="{color}"/>' for offset, color in zip((0, .50, 1), colors))
    return f'<radialGradient id="{name}" cx=".33" cy=".28" r=".76">{stops}</radialGradient>'


def leaf(x, y, angle=0, size=1, narrow=False):
    shape = 'M0 0C17-47 65-59 133-50C101-6 49 18 0 0Z'
    marks = path('M2 0Q47-20 116-45M23-8L31-26M45-16L58-37M63-25L79-45M31-11L54-3M61-24L84-16',
                 'none', '#b9cd76', 2.8)
    return (f'<g transform="translate({x} {y}) rotate({angle}) scale({size} {size * (.58 if narrow else 1)})">'
            + path(shape, 'url(#leaf)', '#475d31', 4) + marks + '</g>')


def rind_section(kind):
    style = {
        'avocado': ('#425d32','#a1b951','#d7dc82','#f1e6a8'),
        'guava': ('#637c38','#a3b954','#f5af9a','#e57576'),
        'mango': ('#a75a25','#f7a33a','#ffd264','#ee922e'),
        'orange': ('#b4602b','#f09d42','#ffe0a0','#f6a934'),
        'coconut': ('#563a2b','#8b633f','#fff0cd','#dcc8a9'),
    }
    edge, skin, light, flesh = style[kind]
    x, y, angle, rx, ry = {
        'avocado': (326, 296, 27, 79, 130), 'guava': (325, 307, 21, 100, 89),
        'mango': (326, 320, 35, 83, 109), 'orange': (327, 304, -22, 100, 96),
        'coconut': (322, 315, 19, 110, 73),
    }[kind]
    draw = [f'<g transform="translate({x} {y}) rotate({angle}) scale({rx/100:.3f} {ry/100:.3f})">']
    pear = 'M0-100C35-102 52-63 70-39C91-13 108 20 96 55C84 85 48 100 0 100C-50 100-85 81-97 50C-109 15-88-15-68-44C-44-72-33-102 0-100Z'
    oval = 'M0-100C58-103 102-64 101-5C101 56 58 101 3 101C-55 101-103 61-102 3C-101-58-54-99 0-100Z'
    shape = pear if kind in ('avocado','mango') else oval
    draw.extend((path(shape,skin,edge,5.5), '<g transform="scale(.895 .886)">',
                 path(shape,light), '</g>', '<g transform="scale(.82 .80)">',
                 path(shape, 'url(#flesh)' if kind != 'coconut' else flesh), '</g>'))
    if kind == 'avocado':
        draw.extend((ellipse(1,17,49,54,'#8b5739'), ellipse(-2,14,44,50,'url(#pit)'),
                     path('M-32 7Q-26-19-8-30','none','#e2ae73',5)))
    if kind == 'guava':
        for i in range(18):
            a = math.tau * i / 18
            ring = 17 if i % 2 == 0 else 34
            draw.append(ellipse(math.cos(a)*ring, math.sin(a)*ring,4,6,'#f8dda1'))
        draw.append(path('M-38-9Q0-52 39-9M-30 28Q0 44 32 22','none','#d76069',3))
    if kind == 'orange':
        for i in range(10):
            a = i * math.tau / 10
            draw.append(path(f'M{7*math.cos(a):.1f} {7*math.sin(a):.1f}Q'
                             f'{43*math.cos(a+.05):.1f} {43*math.sin(a+.05):.1f} '
                             f'{76*math.cos(a):.1f} {76*math.sin(a):.1f}',
                             'none', '#ffe5a1', 3.7))
        draw.append(ellipse(0,0,7,8,'#fbe6ab'))
    if kind == 'mango':
        draw.append(path('M-30-49Q-45-24-46 6M-16-41Q-27-23-33 14M-4-36Q-22-13-21 17M18-21Q2 1 5 28',
                         'none','#ffe29b',3.5))
    if kind == 'coconut':
        draw.extend((ellipse(0,0,73,59,'#ad8d68'),ellipse(-2,-4,70,52,'#ede2c7'),
                     ellipse(-4,-9,59,39,'#e3d8bf'),
                     path('M-48-14Q-26-47 15-40','none','#f6efdf',5)))
    return gradient('flesh',(light,flesh,light)), ''.join(draw) + '</g>'


def illustration(kind):
    _, shape, shades, section = FRUIT[kind]
    definitions = gradient('fruit',shades[:3]) + gradient('leaf',GREEN) + gradient('pit',('#da9f66','#b77646','#8b5238'))
    if kind == 'oilpalm':
        bunches = []
        for x, y, angle in ((282, 208, 26), (200, 287, -27)):
            bunches.append(f'<g transform="translate({x} {y}) rotate({angle})">')
            bunches.append(path('M0 49V104', 'none', '#ad8756', 25))
            bunches.append(ellipse(0, 0, 71, 87, shades[3]))
            for row in range(7):
                for column in range(7):
                    u, v = column * 18 - 54 + (row % 2) * 7, row * 23 - 67
                    if (u / 70)**2 + (v / 87)**2 < 1:
                        bunches.append(ellipse(u, v, 10, 14, 'url(#fruit)'))
            bunches.append('</g>')
        return definitions, [('Oil palm bunches', ''.join(bunches))]
    if kind == 'banana':
        blade = ('M0 0C-16 17-17 70-43 109C-65 141-99 162-127 171C-148 178-146 200-124 203'
                 'C-79 208-32 183 0 146C38 102 44 48 27 10C21-1 7-9 0 0Z')
        stem = (leaf(246,146,-37,.80,True)
                + path('M275 147Q279 127 290 107','none','#776435',26)
                + ellipse(292,106,13,7,'#af8a54','transform="rotate(-23 292 106)"'))
        fingers = []
        for dx, dy, angle, scale in ((-25,0,37,.86),(-15,5,22,.94),(-2,10,5,1),(13,14,-17,1.02)):
            fingers.extend((f'<g transform="translate({267+dx} {147+dy}) rotate({angle}) scale({scale})">',
                            path(blade,'url(#fruit)',shades[-1],5),
                            path('M10 21Q-5 130-115 184','none','#fff1a0',6),
                            path('M14 13L19 4','none','#8f913c',12),
                            ellipse(-135,187,6,9,'#65462b','transform="rotate(-23 -135 187)"'), '</g>'))
        return definitions, [('Stem and leaf',stem),('Golden bananas',''.join(fingers))]

    if kind == 'cherry':
        berry = 'M0-62C20-64 37-77 62-62C102-36 98 24 66 61C39 92-16 98-48 65C-85 27-82-40-53-61C-32-75-18-61 0-62Z'
        foliage = (path('M172 229Q211 135 258 110Q303 143 337 248','none','#495d33',13)
                   + path('M172 229Q211 135 258 110Q303 143 337 248','none','#97a455',6)
                   + leaf(254,115,-11,.96))
        body = ''
        for index,(x,y,a) in enumerate(((164,279,-17),(330,309,11))):
            body += (f'<g transform="translate({x} {y}) rotate({a})">'
                     + path(berry,'url(#fruit)',shades[-1],6)
                     + path('M-42-26Q-30-43-16-40','none','#ffb4a1',8)
                     + path('M-26 66Q15 85 51 49','none','#842947',5) + '</g>')
        return definitions, [('Stems and leaf',foliage),('Painted cherries',body)]

    if kind == 'durian':
        rng = random.Random('durian shell')
        silhouette = []
        for index in range(68):
            angle = index * math.tau / 68
            radius = rng.uniform(1.01, 1.13) if index % 2 else .91
            silhouette.append(f'{192 + radius * 109 * math.cos(angle):.1f},{255 + radius * 126 * math.sin(angle):.1f}')
        stalk = path('M183 143Q178 119 193 93','none','#755033',16) + leaf(191,108,-12,.81,True)
        body = f'<polygon points="{" ".join(silhouette)}" fill="url(#fruit)" stroke="{shades[-1]}" stroke-width="5"/>'
        for index in range(81):
            angle = index * 2.39996
            reach = .90 * math.sqrt((index + .5) / 81)
            x = 192 + math.cos(angle) * reach * 109
            y = 255 + math.sin(angle) * reach * 126
            size = rng.uniform(6,12)
            body += path(f'M{x-size:.1f} {y+3:.1f}L{x+4:.1f} {y-size:.1f}L{x+size*.82:.1f} {y+size*.45:.1f}Z',
                         rng.choice(('#c6b861','#a5984d','#968745')),'#746536',1.4)
        opened = (ellipse(324,321,87,78,'#817941', 'stroke="#5d5932" stroke-width="6"')
                  + ellipse(324,318,78,69,'#f2deab')
                  + ellipse(302,303,31,45,'url(#pulp)','transform="rotate(26 302 303)"')
                  + ellipse(349,323,31,44,'url(#pulp)','transform="rotate(35 349 323)"')
                  + ellipse(309,352,29,24,'url(#pulp)'))
        definitions += gradient('pulp',('#ffe593','#efc360','#cf903f'))
        return definitions, [('Stalk and leaf', stalk), ('Thorned shell', body), ('Cut flesh', opened)]

    if kind == 'rambutan':
        foliage = path('M135 220Q181 143 243 107Q302 149 337 262M223 129Q212 181 220 213','none','#765039',11)
        foliage += leaf(237,115,-15,.95) + leaf(223,124,-132,.78)
        body = ''
        rng = random.Random('rambutan icon')
        for x, y, rx, ry, opened in ((153,252,71,86,False), (250,253,77,89,False), (329,321,85,81,True)):
            fringe, surface = '', ''
            for index in range(23):
                angle = math.tau * index / 23
                dx, dy = math.cos(angle), math.sin(angle)
                length = rng.uniform(15, 25)
                start = (x + dx * rx * .90, y + dy * ry * .90)
                bend = (x + dx * (rx + length) - dy * 6, y + dy * (ry + length) + dx * 6)
                tip = (bend[0] - dy * 7 - dx * 4, bend[1] + dx * 7 - dy * 4)
                fringe += path(f'M{start[0]:.1f} {start[1]:.1f} Q{bend[0]:.1f} {bend[1]:.1f} '
                               f'{tip[0]:.1f} {tip[1]:.1f}', 'none', rng.choice(('#b07837','#c3ab4f','#cf9e45')), 5.3)
            surface += ellipse(x,y,rx,ry,'url(#fruit)',f'stroke="{shades[-1]}" stroke-width="6"')
            if opened:
                surface += ellipse(x+5,y-5,rx*.79,ry*.81,'#ea9972')
                surface += ellipse(x+7,y-8,rx*.68,ry*.70,'url(#rambutanFlesh)')
                surface += path(f'M{x-22} {y-28}Q{x-11} {y-45} {x+7} {y-44}','none','#fff4d8',5)
            else:
                for index in range(27):
                    angle = index * 2.40
                    radius = .78 * math.sqrt((index + .5) / 27)
                    px, py = x + math.cos(angle) * radius * rx, y + math.sin(angle) * radius * ry
                    surface += path(f'M{px:.1f} {py:.1f}q-7 -10 -2 -15','none','#e49e53',3.7)
            body += fringe + surface
        definitions += gradient('rambutanFlesh',('#fff4d4','#f1dcba','#d4b58f'))
        return definitions, [('Cluster stalk and leaves', foliage), ('Red fruit, hairs and aril', body)]

    top = {
        'apple': (251,157), 'avocado': (204,96), 'guava': (209,129),
        'mango': (235,114), 'orange': (196,139), 'pear': (223,108), 'coconut': (168,132)
    }[kind]
    x,y = top
    foliage = path(f'M{x} {y+5}Q{x+1} {y-24} {x+18} {y-42}','none','#674634',12)
    foliage += path(f'M{x-1} {y-4}Q{x+3} {y-24} {x+15} {y-38}','none','#ad8756',3)
    if kind == 'coconut':
        foliage += leaf(178,122,-19,1.15,True) + leaf(185,116,19,1,True)
    else:
        foliage += leaf(x+6,y-8,-17,.90,kind=='mango')
        if kind in ('guava','orange'):
            foliage += leaf(x+4,y-13,-113,.70)
    body = path(shape,'url(#fruit)',shades[-1],6)
    specular = path(f'M{x-58} {y+61}Q{x-80} {y+78} {x-86} {y+108}','none',shades[0],9)
    if kind == 'mango':
        definitions += ('<radialGradient id="blush" cx=".74" cy=".33">'
                        '<stop stop-color="#df6231" stop-opacity=".88"/>'
                        '<stop offset="1" stop-color="#df6231" stop-opacity="0"/></radialGradient>')
        specular = ellipse(243,181,106,115,'url(#blush)') + specular
    body += f'<g clip-path="url(#skin)">{specular}</g>'
    rng = random.Random(kind)
    texture = ''
    for _ in range(75):
        px,py = rng.uniform(95,390),rng.uniform(147,385)
        radius = rng.uniform(1.2,3.2)
        texture += ellipse(px,py,radius,radius*.82,rng.choice((shades[0],shades[2])),
                           f'opacity="{rng.uniform(.19,.42):.2f}"')
    if kind == 'coconut':
        for shift in range(0,97,16):
            texture += path(f'M{117+shift} 127C{122+shift} 186 {190+shift} 321 {255+shift*.5} 379',
                            'none','#73503b',2.8)
    definitions += f'<clipPath id="skin">{path(shape,"white","none")}</clipPath>'
    layers = [('Stalk and leaves',foliage),('Painted fruit skin',body),
              ('Skin flecks and fibers',f'<g clip-path="url(#skin)">{texture}</g>')]
    if section:
        further_defs, drawing = rind_section(section)
        definitions += further_defs
        layers.append(('Cut fruit',drawing))
    return definitions,layers


def layer(image, name, body, definitions='', parent=None):
    w,h=image.get_width(),image.get_height()
    temp=WORK/'layer.svg'
    temp.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
                    f'viewBox="0 0 {w} {h}"><defs>{definitions}</defs>{body}</svg>',encoding='utf8')
    result=Gimp.file_load_layer(RUN,image,Gio.File.new_for_path(str(temp)))
    assert result is not None,name
    image.insert_layer(result,parent,0)
    result.set_name(name)
    return result


def text(x,y,value,size,color):
    return (f'<text x="{x}" y="{y}" text-anchor="middle" font-family="Arial Narrow,Arial,sans-serif" '
            f'font-weight="bold" font-size="{size}" fill="{color}">{escape(value)}</text>')


def group(image,name):
    result=Gimp.GroupLayer.new(image,name)
    image.insert_layer(result,None,0)
    return result


def write(image,name,procedure):
    proc=Gimp.get_pdb().lookup_procedure(procedure)
    config=proc.create_config()
    config.set_property('run-mode',RUN)
    config.set_property('image',image)
    config.set_property('file',Gio.File.new_for_path(str(WORK/name)))
    assert proc.run(config).index(0)==Gimp.PDBStatusType.SUCCESS,name


def export(image,name):
    output=image.duplicate()
    output.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
    output.scale(256,256)
    write(output,name,'file-png-export')
    output.delete()


def import_illustration(image, parent, source, maximum, center):
    drawing = Gimp.file_load_layer(RUN, image, Gio.File.new_for_path(str(source)))
    if drawing is None:
        raise RuntimeError(f'Could not open the fruit illustration {source}')
    image.insert_layer(drawing, parent, 0)
    scale = maximum / max(drawing.get_width(), drawing.get_height())
    width = round(drawing.get_width() * scale)
    height = round(drawing.get_height() * scale)
    drawing.scale(width, height, False)
    drawing.set_offsets(center[0] - width // 2, center[1] - height // 2)
    drawing.set_name('Painted fruit illustration')


def create(kind):
    label=FRUIT[kind][0].upper()
    source = Path(ILLUSTRATIONS) / f'{kind}.png' if ILLUSTRATIONS else None
    if source is None:
        definitions, paint = illustration(kind)
    else:
        definitions = gradient('leaf', GREEN)
    image=Gimp.Image.new(768,768,Gimp.ImageBaseType.RGB)
    fruit=group(image,'Buah / Harvest fruit')
    if source is None:
        for name,body in paint:
            layer(image,name,f'<g transform="translate(23 37) scale(1.41)">{body}</g>',definitions,fruit)
    else:
        import_illustration(image, fruit, source, 618, (384, 383))
    export(image,f'icon_tree_fruit_{kind}.png')
    vertices = []
    for i in range(10):
        angle = math.tau * i / 10 - math.pi / 2
        radius = 67.5 if i % 2 == 0 else 33
        vertices.append(f'{595.5+radius*math.cos(angle):.2f},{586.5+radius*math.sin(angle):.2f}')
    badge = layer(image, 'Mutation star', f'<polygon points="{" ".join(vertices)}" fill="#d7ac43" '
                  'stroke="#42513a" stroke-width="7" stroke-linejoin="round"/>', parent=fruit)
    export(image,f'icon_tree_fruit_{kind}_mutation.png')
    badge.set_visible(False)
    fruit.set_visible(False)
    packet=group(image,'Bibit / Kraft packet')
    paper=Gimp.file_load_layer(RUN,image,Gio.File.new_for_path(str(BLANK)))
    image.insert_layer(paper,packet,0)
    paper.set_name('Shared kraft paper')
    if source is None:
        for name,body in paint:
            layer(image,'Print - '+name,f'<g transform="translate(178 232) scale(.78)">{body}</g>',definitions,packet)
    else:
        import_illustration(image, packet, source, 311, (382, 458))
    accents=(f'<rect x="205" y="219" width="154" height="77" rx="7" fill="#4a6d34"/>'
             + path('M214 225H350','none','#9aac66',2)
             + text(282,248,'BIBIT',24,'#f8ebc4')
             + text(282,278,label,24 if len(label)<9 else 21,'#fff0cf')
             + leaf(380,190,-12,.23) + leaf(516,618,-12,.23))
    layer(image,'Bibit label and green marks',accents,definitions,packet)
    export(image,f'icon_tree_seed_{kind}.png')
    master_name = 'OilPalm' if kind == 'oilpalm' else kind.title()
    master = f'Tree_{master_name}.xcf'
    assert Gimp.file_save(RUN,image,Gio.File.new_for_path(str(WORK/master)))
    image.delete()
    check=Gimp.file_load(RUN,Gio.File.new_for_path(str(WORK/master)))
    assert check.get_width()==768 and len(check.get_layers())==2
    assert all(len(g.get_children())>=2 for g in check.get_layers())
    check.delete()


def main():
    WORK.mkdir(parents=True,exist_ok=True)
    selection = os.environ.get('NP_TREE_ICON_SPECIES')
    Gimp.context_push()
    try:
        Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
        for kind in (selection,) if selection else ORDER:
            create(kind)
            print('GIMP_TREE_ICONS',kind,flush=True)
    finally:
        Gimp.context_pop()


main()
