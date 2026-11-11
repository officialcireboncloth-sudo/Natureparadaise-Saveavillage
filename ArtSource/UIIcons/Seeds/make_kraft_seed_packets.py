"""Composite shared kraft packaging with the existing crop artwork for Unity."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[3]
ART = Path(__file__).resolve().parent
ASSETS = ROOT / 'Assets/Nature  Paradaise/Textures/UI/Icons'
TEMPLATE = ART / 'Generated/KraftSeedPacket_Blank.png'
LAYOUT = ART / 'Generated/KraftSeedPacket_Layout.json'
FONT = 'C:/Windows/Fonts/ARIALNB.TTF'
FRONT_SIZE = (440, 620)


def leaf_mark(draw, x, y, size):
    points = lambda coords: [(x+size*u,y+size*v) for u,v in coords]
    draw.polygon(points([(0,0),(.08,-.49),(.45,-.76),(1,-.85),(.92,-.40),(.60,-.09),(.21,.01)]),
                 fill=(68,101,48,227))
    draw.polygon(points([(0,0),(-.47,-.08),(-.78,-.35),(-.83,-.67),(-.33,-.59),(-.08,-.33)]),
                 fill=(87,113,54,228))
    draw.line(points([(-.02,.13),(.15,-.20),(.68,-.62)]),fill=(176,184,114,244),width=max(2,size//14))


def fitted_label(label, max_width):
    for size in range(34,17,-1):
        font=ImageFont.truetype(FONT,size)
        if font.getlength(label)<=max_width:
            return font
    return ImageFont.truetype(FONT,17)


def packet_art(key,label):
    front=Image.new('RGBA',FRONT_SIZE)
    draw=ImageDraw.Draw(front)
    leaf_mark(draw,228,84,29)
    leaf_mark(draw,370,550,31)
    draw.rounded_rectangle((41,112,198,196),radius=7,fill=(67,101,45,238))
    draw.line((47,118,191,118),fill=(152,171,102,235),width=2)
    draw.text((119,137),'BENIH',font=ImageFont.truetype(FONT,26),anchor='mm',fill=(250,239,205,255))
    draw.text((119,170),label,font=fitted_label(label,145),anchor='mm',fill=(255,245,217,255))
    painted=ART/f'Generated/KraftSeedPrint_{key}.png'
    posed=ART/f'Generated/G14_{key}_PacketPrint.png'
    icon=ASSETS/f'Crops/icon_crop_{key}.png'
    print_source=next(path for path in (painted,posed,icon) if path.is_file())
    with Image.open(print_source) as source:
        crop=source.convert('RGBA')
        crop=crop.crop(crop.getchannel('A').getbbox())
    if print_source==icon and key in {'green_bean','cucumber','zucchini','chili_pepper','wheat','rice'}:
        crop=crop.rotate(9,Image.Resampling.BICUBIC,expand=True)
        crop=crop.crop(crop.getchannel('A').getbbox())
    scale=min(345/crop.width,338/crop.height)
    crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
    x,y=(FRONT_SIZE[0]-crop.width)//2,368-crop.height//2
    shade=Image.new('RGBA',FRONT_SIZE,(67,43,21,0))
    alpha=Image.new('L',FRONT_SIZE)
    alpha.paste(crop.getchannel('A'),(x+7,y+8))
    shade.putalpha(alpha.filter(ImageFilter.GaussianBlur(7)).point(lambda v: v//5))
    front.alpha_composite(shade)
    front.alpha_composite(crop,(x,y))
    return front


def transform_for(points):
    """Pillow's perspective transform maps output quadrilateral into the flat print."""
    w,h=FRONT_SIZE
    matrix, values=[],[]
    for (x,y),(u,v) in zip(points,((0,0),(w,0),(w,h),(0,h))):
        matrix.extend(((x,y,1,0,0,0,-u*x,-u*y),(0,0,0,x,y,1,-v*x,-v*y)))
        values.extend((u,v))
    return np.linalg.solve(np.asarray(matrix,dtype=float),np.asarray(values,dtype=float))


def build_sprite(blank,transform,key,label):
    print_layer=packet_art(key,label).transform(blank.size,Image.Transform.PERSPECTIVE,
                                                transform,Image.Resampling.BICUBIC)
    rendered=Image.alpha_composite(blank,print_layer)
    return rendered.resize((256,256),Image.Resampling.LANCZOS)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path)
    parser.add_argument('--publish',action='store_true')
    parser.add_argument('--only',nargs='+',help='Regenerate these crop keys and reuse the other Unity sprites in the preview.')
    args=parser.parse_args()
    if args.publish == (args.output is not None):
        parser.error('Use --publish to update Unity or --output to stage the sprites.')
    manifest_path=ART/'Manifest.json'
    manifest=json.loads(manifest_path.read_text(encoding='utf8'))
    assert len(manifest['icons'])==33
    keys={record['plant'] for record in manifest['icons']}
    selected=set(args.only) if args.only else keys
    if unknown:=selected-keys:
        parser.error(f'Unknown crop keys: {", ".join(sorted(unknown))}')
    audit_path=ROOT/'ArtSource/Crops/Integration/G01_KraftSeedSprites_BindingChanges.json'
    changes=json.loads(audit_path.read_text(encoding='utf8'))['authorizedChanges'] if audit_path.is_file() else {}
    layout=json.loads(LAYOUT.read_text(encoding='utf8'))
    with Image.open(TEMPLATE) as source:
        blank=source.convert('RGBA')
    assert blank.size==(768,768) and blank.getchannel('A').getextrema()==(0,255)
    transform=transform_for(layout['frontCorners'])
    directory=ASSETS/'Seeds' if args.publish else args.output
    directory.mkdir(parents=True,exist_ok=True)
    preview=Image.new('RGBA',(1260,1080),'#5a4737')
    draw=ImageDraw.Draw(preview)
    label_font=ImageFont.truetype(FONT,18)
    for index,record in enumerate(manifest['icons']):
        key,label=record['plant'],record['label']
        target=directory/f'icon_seed_{key}.png'
        if key in selected:
            sprite=build_sprite(blank,transform,key,label)
            before=digest(target) if args.publish and target.is_file() else None
            sprite.save(target)
            if args.publish:
                after=digest(target)
                name=target.relative_to(ROOT).as_posix()
                previous=changes.get(name)
                if before and before!=after:
                    changes[name]={'before':previous['before'] if previous and previous['after']==before else before,
                                   'after':after}
                record.update(seedSha256=after,seedStyle='Kraft packet with large crop artwork',
                              seedSource='ArtSource/UIIcons/Seeds/make_kraft_seed_packets.py')
        else:
            with Image.open(ASSETS/'Seeds'/target.name) as source:
                sprite=source.convert('RGBA')
        x,y=index%7*180,index//7*216
        preview.alpha_composite(sprite.resize((176,176),Image.Resampling.LANCZOS),(x+2,y+8))
        draw.text((x+90,y+196),label,anchor='mm',font=label_font,fill='#f8edd7')
    preview_path=(ART if args.publish else directory)/'SeedShopKraft_Preview.png'
    preview.save(preview_path)
    if args.publish:
        manifest.update(renderer='Shared kraft packaging with Indonesian label and crop illustration')
        manifest_path.write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf8')
        audit_path.write_text(json.dumps(dict(status='APPLIED_SCOPED_EXTENSION',scope='Kraft seed packet art',
                                              authorizedChanges=changes),indent=2),encoding='utf8')
    print('KRAFT_SEED_PACKETS',len(selected),'rendered,',len(keys),'in preview')


if __name__=='__main__':
    main()
