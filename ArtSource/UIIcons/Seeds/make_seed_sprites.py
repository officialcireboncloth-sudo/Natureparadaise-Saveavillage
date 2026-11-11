"""Draw direct seed illustrations and the simplified G01 fruit icons for Unity."""
import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[3]
ART = Path(__file__).resolve().parent
ICONS = ROOT/'Assets/Nature  Paradaise/Textures/UI/Icons'
FAMILY = {
    'black_flat':'watermelon kiwi sunflower', 'ivory_flat':'melon zucchini cucumber chili_pepper moonflower',
    'pea':'pea', 'bean':'green_bean', 'bronze_flat':'eggplant rose jasmine daisy',
    'round':'cabbage broccoli cauliflower hibiscus blueberry raspberry blackberry',
    'angular':'radish onion spinach water_spinach', 'rice':'rice orchid', 'wheat':'wheat',
    'thin':'chrysanthemum dahlia marigold lavender', 'bulb':'tulip',
}
PALETTE = {
    'black_flat':('#302b3a','#524757','#827282'), 'ivory_flat':('#a57949','#d6b47a','#f6e0aa'),
    'pea':('#4c6733','#8aa754','#c6d889'), 'bean':('#725c35','#aa945c','#e2c88d'),
    'bronze_flat':('#7d492c','#bd8451','#e5b878'), 'round':('#524136','#936c4c','#c69765'),
    'angular':('#4b3c34','#84604a','#b98763'), 'rice':('#a68b53','#d9c284','#fae8af'),
    'wheat':('#905b2e','#c48e49','#efc481'), 'thin':('#4e4036','#8f6e4e','#bb946e'),
    'bulb':('#8e5133','#bf8250','#e9b779'),
}
FAMILY_BY_CROP = {name:family for family,names in FAMILY.items() for name in names.split()}
BERRIES = ('blueberry','raspberry','blackberry')
RESAMPLE = Image.Resampling.LANCZOS


def ellipse(draw, x, y, w, h, color):
    draw.ellipse((x-w,y-h,x+w,y+h), fill=color)


def radial(x,y,outer,inner,count):
    return [(x+math.cos(i*math.pi/count-math.pi/2)*(outer if i%2==0 else inner),
             y+math.sin(i*math.pi/count-math.pi/2)*(outer if i%2==0 else inner)) for i in range(2*count)]


def rounded(draw,points,color):
    contour=[]
    count=len(points)
    for i in range(count):
        p0,p1,p2,p3=(points[(i+j)%count] for j in (-1,0,1,2))
        for t in (0,.2,.4,.6,.8):
            contour.append(tuple(.5*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)
                                 for a,b,c,d in zip(p0,p1,p2,p3)))
    draw.polygon(contour,fill=color)


def berry_leaf(draw,x,y,flip=1):
    points = [(x,y),(x+23*flip,y-25),(x+63*flip,y-34),(x+54*flip,y-8),(x+34*flip,y+12)]
    draw.polygon(points,fill='#506d3a')
    draw.polygon([points[0],points[1],points[2],(x+36*flip,y-8)],fill='#82a356')
    draw.line((x,y,x+54*flip,y-26),fill='#b0bf74',width=3)


def blue(draw,x,y,size):
    ellipse(draw,x,y,size,size*.89,'#344474')
    ellipse(draw,x-size*.04,y-size*.08,size*.95,size*.82,'#5874b1')
    ellipse(draw,x-size*.19,y-size*.29,size*.64,size*.51,'#7895ce')
    ellipse(draw,x-size*.34,y-size*.45,size*.33,size*.18,'#a6bbe1')
    draw.polygon(radial(x+size*.14,y-size*.54,size*.36,size*.20,5),fill='#455b91')
    draw.polygon(radial(x+size*.14,y-size*.51,size*.27,size*.14,5),fill='#354672')


def aggregate(draw,x,y,scale,palette):
    edge,shade,body,light = palette
    ellipse(draw,x,y,99*scale,107*scale,edge)
    for row,(count,offset,width) in enumerate([(4,-74,112),(5,-37,150),(5,4,158),(4,47,134),(3,79,94)]):
        for i in range(count):
            dx=(i/(count-1)-.5)*width
            cx=x+(dx+5*math.sin(i+row*2))*scale
            cy=y+(offset+5*math.cos(i*2.9+row))*scale
            radius=(25 if row not in (0,4) else 23)*scale
            ellipse(draw,cx,cy,radius+2*scale,radius*.90+2*scale,edge)
            ellipse(draw,cx,cy,radius,radius*.90,shade)
            ellipse(draw,cx-radius*.09,cy-radius*.13,radius*.87,radius*.72,body)
            ellipse(draw,cx-radius*.29,cy-radius*.31,radius*.44,radius*.23,light)


def fruit_sprite(kind):
    canvas=Image.new('RGBA',(512,512))
    d=ImageDraw.Draw(canvas)
    if kind=='blueberry':
        berry_leaf(d,242,145,-1)
        berry_leaf(d,246,150)
        blue(d,199,210,106)
        blue(d,339,281,88)
        blue(d,203,352,88)
    else:
        berry_leaf(d,286,162)
        berry_leaf(d,277,155,-1)
        colors = (('#713549','#ac4459','#d05c70','#e9949c') if kind=='raspberry'
                  else ('#352d4a','#524267','#715985','#9a79a7'))
        aggregate(d,326,293,.78,colors)
        aggregate(d,204,272,1.06,colors)
    return canvas.resize((256,256),RESAMPLE)


def mutation(sprite):
    art=sprite.resize((512,512),RESAMPLE)
    draw=ImageDraw.Draw(art)
    draw.polygon(radial(399,393,49,24,5),fill='#8c6632')
    draw.polygon(radial(399,390,43,21,5),fill='#d7ac43')
    return art.resize((256,256),RESAMPLE)


def seed(family):
    edge,body,shine=PALETTE[family]
    art=Image.new('RGBA',(172,130))
    d=ImageDraw.Draw(art)
    if family in ('round','pea'):
        ellipse(d,86,65,52,50,edge)
        ellipse(d,83,61,47,44,body)
        ellipse(d,70,47,26,16,shine)
        ellipse(d,107,73,8,5,edge)
    elif family=='bulb':
        rounded(d,[(32,88),(42,47),(70,23),(88,8),(108,31),(134,49),(144,90),(128,111),(51,111)],edge)
        rounded(d,[(39,86),(52,47),(88,12),(103,33),(128,56),(134,87),(122,104),(51,104)],body)
        rounded(d,[(59,93),(58,66),(70,43),(90,18),(88,60),(81,89)],shine)
        d.line((51,101,121,101),fill=edge,width=5)
    elif family=='angular':
        d.polygon([(29,83),(43,47),(75,25),(110,31),(141,56),(138,80),(114,101),(54,99)],fill=edge)
        d.polygon([(36,80),(50,50),(76,32),(109,37),(134,57),(126,79),(108,93),(55,91)],fill=body)
        d.polygon([(50,55),(77,34),(107,39),(91,59),(61,67)],fill=shine)
    elif family=='bean':
        ellipse(d,80,65,61,43,edge)
        ellipse(d,77,63,55,37,body)
        ellipse(d,65,50,35,17,shine)
        d.arc((97,39,139,79),83,216,fill=edge,width=7)
    else:
        narrow=family in ('rice','wheat','thin')
        h=24 if narrow else 34
        rounded(d,[(9,65),(33,65-h),(73,60-h),(112,62-h),(142,52),(162,65),(140,81),(109,65+h),(54,68+h),(27,85)],edge)
        rounded(d,[(18,64),(39,71-h),(77,66-h),(112,68-h),(148,57),(155,65),(135,75),(108,59+h),(55,62+h),(34,80)],body)
        rounded(d,[(26,60),(49,49),(90,43 if narrow else 35),(126,50),(100,55),(64,58),(43,69)],shine)
        if family in ('rice','wheat','black_flat','bronze_flat'):
            d.line((31,70,66,64,109,62,147,63),fill=edge,width=4)
    return art.crop(art.getchannel('A').getbbox())


def seed_sprite(crop, family):
    foreground=Image.new('RGBA',(512,512))
    mark=crop.crop(crop.getchannel('A').getbbox())
    mark.thumbnail((296,282),RESAMPLE)
    foreground.alpha_composite(mark,(327-mark.width//2,166-mark.height//2))
    shape=seed(family)
    placements = [(120,320,130,-28),(231,336,136,17),(177,398,146,-10)]
    if family=='bulb':
        placements = [(213,335,211,-12)]
    for x,y,width,angle in placements:
        piece=shape.resize((width,round(shape.height*width/shape.width)),RESAMPLE)
        piece=piece.rotate(angle,Image.Resampling.BICUBIC,expand=True)
        mask=piece.getchannel('A').filter(ImageFilter.GaussianBlur(4))
        shadow=Image.new('RGBA',piece.size,'#2c251e')
        shadow.putalpha(mask.point(lambda v: v//5))
        foreground.alpha_composite(shadow,(x-piece.width//2,y-piece.height//2+5))
        foreground.alpha_composite(piece,(x-piece.width//2,y-piece.height//2))
    return foreground.resize((256,256),RESAMPLE)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_sprite(image,target,changes):
    before=digest(target) if target.is_file() else None
    image.save(target)
    after=digest(target)
    if changes is not None and before is not None and before!=after:
        name=target.relative_to(ROOT).as_posix()
        previous=changes.get(name)
        changes[name]={'before':previous['before'] if previous and previous['after']==before else before,'after':after}
    return after


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path)
    parser.add_argument('--publish',action='store_true')
    args=parser.parse_args()
    if args.publish == (args.output is not None):
        parser.error('Use --publish to update Unity or --output to stage a preview.')
    icons=ICONS if args.publish else args.output
    for folder in ('Crops','Seeds'):
        (icons/folder).mkdir(parents=True,exist_ok=True)
    records_path=ART/'Manifest.json'
    records=json.loads(records_path.read_text(encoding='utf8'))
    audit_path=ROOT/'ArtSource/Crops/Integration/G01_SeedSprites_BindingChanges.json'
    changes=None
    if args.publish:
        changes=json.loads(audit_path.read_text(encoding='utf8'))['authorizedChanges'] if audit_path.is_file() else {}
    assert {row['plant'] for row in records['icons']}==set(FAMILY_BY_CROP)
    for kind in BERRIES:
        fruit=fruit_sprite(kind)
        save_sprite(fruit,icons/f'Crops/icon_crop_{kind}.png',changes)
        save_sprite(mutation(fruit),icons/f'Crops/icon_crop_{kind}_mutation.png',changes)
    preview=Image.new('RGBA',(2016,1440),'#344b3d')
    for index,row in enumerate(records['icons']):
        kind=row['plant']
        print_art=ART/f'Generated/G14_{kind}_PacketPrint.png'
        source=(print_art if print_art.is_file() else
                (icons if kind in BERRIES else ICONS)/f'Crops/icon_crop_{kind}.png')
        with Image.open(source) as raw:
            sprite=seed_sprite(raw.convert('RGBA'),FAMILY_BY_CROP[kind])
        target=icons/f'Seeds/icon_seed_{kind}.png'
        sha=save_sprite(sprite,target,changes)
        preview.alpha_composite(sprite,((index%7)*288+16,(index//7)*288+16))
        if args.publish:
            row.update(seedSha256=sha,seedStyle='Direct seed illustration',
                       seedSource='ArtSource/UIIcons/Seeds/make_seed_sprites.py')
    preview.save((ART if args.publish else icons)/'SeedSprites_Preview.png')
    if args.publish:
        records['renderer']='Direct seed shapes with crop silhouettes on transparent sprites'
        records_path.write_text(json.dumps(records,indent=2,ensure_ascii=False),encoding='utf8')
        audit_path.write_text(json.dumps(dict(status='APPLIED_SCOPED_EXTENSION',scope='Seed and G01 produce artwork',
                                              authorizedChanges=changes),indent=2),encoding='utf8')
    print('SEED_SPRITES',len(records['icons']),'BERRY_SPRITES',2*len(BERRIES))


if __name__=='__main__':
    main()
