"""Paint Daisy pigment, broad detail readable in mips, and tangent relief."""
import numpy as np
from PIL import Image


def blend(a, b, weight):
    weight = np.clip(weight, 0, 1)[..., None]
    return np.asarray(a) * (1 - weight) + np.asarray(b) * weight


def smooth(a, b, t):
    t = np.clip((t-a)/(b-a), 0, 1)
    return t*t*(3-2*t)


def bell(distance, width):
    return np.exp(-np.square(distance/width))


def cloud(size, divisions, seed):
    grid = np.random.default_rng(seed).random((divisions, divisions)).astype('float32')
    return np.asarray(Image.fromarray(grid).resize((size,size), Image.Resampling.BICUBIC)) - .5


def leaf(u, t, grain, patches):
    lateral = 2*u-1
    edge = abs(lateral)
    light = smooth(.02,.72,t)*.86 + patches*.11
    pigment = blend((29,62,17), (83,137,37), light)
    sunlight = bell(t-.67,.31)*bell(lateral+.27,.57)
    pigment = blend(pigment, (149,176,57), .58*sunlight)
    pigment = blend(pigment, (57,103,25), .34*edge**2)
    pigment = blend(pigment, (163,181,66), .39*smooth(.85,1,edge)*smooth(.28,.9,t))
    pigment += (grain*7 + patches*18)[..., None]

    # Branches originate at the midrib and arc toward successive lobes.
    midline = .022*np.sin(np.pi*t)*np.sin(5.1*t)
    axis = lateral-midline
    primary = np.zeros_like(t)
    secondary = np.zeros_like(t)
    trough = np.zeros_like(t)
    for side in (-1,1):
        s = side*axis
        arm = np.clip(s,0,1)
        arm_fade = smooth(.012,.13,s)*(1-.32*smooth(.72,1,arm))
        for i, base in enumerate((.10,.26,.43,.60,.76)):
            base += .009*side*np.sin(1+i*1.9)
            center = base + (.14-.012*i)*arm + .067*np.sin(np.pi*arm*.62)
            width = (.020-.0013*i)*(1-.47*arm)
            primary = np.maximum(primary,bell(t-center,width)*arm_fade)
            trough = np.maximum(trough,bell(t-center+.025,width*1.12)*arm_fade)
            for joint in (.32,.66):
                tip = center + .085*smooth(joint,min(joint+.44,1),arm)
                twig = smooth(joint,joint+.08,s)*(1-smooth(joint+.23,min(joint+.38,1.02),arm))
                secondary = np.maximum(secondary,bell(t-tip,.010)*twig)

    midrib = bell(axis,.041-.018*t)*(1-.67*smooth(.76,1,t))
    ribs = np.maximum(.76*midrib,.57*primary)
    pigment = blend(pigment, (60,97,28), trough*.33)
    pigment = blend(pigment, (160,181,70), ribs*.83+secondary*.26)
    pigment = blend(pigment, (201,204,111), .51*bell(axis,.014-.004*t)*(1-.84*t))
    height = .62*midrib + .26*primary + .095*secondary - .10*trough - .30*edge**1.7
    height += patches*.077 + grain*.024
    gloss = .24 + .10*sunlight + .044*midrib - .056*edge
    return pigment, height, gloss, 5.4


def petal(u, t, grain, patches):
    lateral = 2*u-1
    rim = abs(lateral)
    pigment = blend((192,166,105), (247,244,230), smooth(.0,.59,t))
    pigment = blend(pigment, (253,250,241), .76*smooth(.48,.97,t))
    pigment = blend(pigment, (206,197,184), .38*rim**2*(1-.56*smooth(.78,1,t)))
    rib_height = np.zeros_like(t)
    groove = np.zeros_like(t)
    sheen = np.zeros_like(t)
    fiber_envelope = smooth(.04,.24,t)*(1-.85*smooth(.86,1,t))
    for i, position in enumerate((-.78,-.52,-.27,0,.28,.55,.79)):
        curve = position*(.65+.35*np.sin(np.pi*t*.71)) + .024*np.sin(4.6*t+i*.68)*np.sin(np.pi*t)
        width = .075 if i in (1,3,5) else .046
        gain = 1. if i in (1,3,5) else .58
        rib_height += gain*bell(lateral-curve,width)*fiber_envelope
        groove += gain*bell(lateral-curve-.055,width*.67)*fiber_envelope
        sheen += gain*bell(lateral-curve+.028,width*.74)*fiber_envelope
    longitudinal = np.sin(102*lateral + 1.5*np.sin(t*10) + 3*patches)
    pigment = blend(pigment, (179,166,149), np.minimum(.26,.26*groove)*(1-.43*t))
    pigment = blend(pigment, (255,252,245), .41*sheen)
    pigment += (2.2*longitudinal*fiber_envelope + 3.6*grain + 5*patches)[...,None]
    height = .26*rib_height-.13*groove + .32*(1-rim**2)*np.sin(np.pi*t) + .018*longitudinal*fiber_envelope
    return pigment, height, .28+.085*sheen-.07*groove, 4.6


def disk(u, t, grain, patches):
    # Offset adjacent rows to read as clustered florets rather than a checkerboard.
    s, v = u*17, t*13
    cell = np.full_like(t,10.)
    floret_height = np.zeros_like(t)
    hole = np.zeros_like(t)
    sunward = np.zeros_like(t)
    for j in (-1,0,1):
        row = np.floor(v)+j
        stagger = (row % 2)*.5
        for i in (-1,0,1):
            col = np.floor(s-stagger)+i+stagger
            dx, dy = s-col, v-row
            distance = np.hypot(dx,dy*.90)
            nearest = distance < cell
            radial = np.arctan2(dy, dx)
            floret = bell(distance,.48)*(.90+.1*np.cos(radial*5)*smooth(.08,.38,distance))
            floret_height = np.where(nearest,floret,floret_height)
            hole = np.where(nearest,bell(distance,.14),hole)
            sunward = np.where(nearest,bell(np.hypot(dx+.17,dy-.10),.21),sunward)
            cell = np.minimum(cell,distance)
    pigment = blend((154,82,7), (249,184,28), floret_height*.91+.08*t)
    pigment = blend(pigment, (254,207,48), .50*sunward)
    pigment = blend(pigment, (184,109,6), .64*hole)
    pigment += (grain*5+patches*8)[...,None]
    return pigment, .63*floret_height-.30*hole, .23+.15*sunward, 3.2


def stem(u, t, grain, patches):
    ridges = .5+.5*np.cos(2*np.pi*(u*8 + .06*np.sin(np.pi*t*2)))
    channels = bell(ridges,.20)
    tint = smooth(0,.85,t)*.32 + patches*.09
    pigment = blend((49,88,22), (122,153,54), .30+.39*ridges+tint)
    pigment = blend(pigment, (47,75,20), .30*channels)
    fibers = np.sin(2*np.pi*u*21+.45*np.sin(t*12))
    pigment += (2.6*fibers + 3.2*grain)[...,None]
    return pigment, .21*ridges-.086*channels+.029*fibers, .26+.067*ridges, 4.4


def seed(u, t, grain, patches):
    ribs = .5+.5*np.cos(2*np.pi*(u*6+.045*np.sin(t*2*np.pi)))
    ridges = ribs**1.65
    creases = bell(ribs,.19)
    pigment = blend((113,77,41), (216,176,106), .26+.60*ridges+.10*smooth(0,.55,t))
    pigment = blend(pigment, (235,206,146), .45*ridges*bell(t-.51,.38))
    pigment = blend(pigment, (96,66,39), .39*creases)
    pigment += (4*grain+7*patches)[...,None]
    return pigment, .51*ridges-.23*creases, .21+.084*ridges, 3.8


SURFACE_PAINTERS = {'DaisyLeaf':leaf,'DaisyPetal':petal,'DaisyDisk':disk,'DaisyStem':stem,'DaisySeed':seed}


def paint_daisy(name, size, pad):
    y, x = np.mgrid[0:size,0:size]
    grain = cloud(size, min(54,size//2), 61)
    patches = cloud(size, 11, 914)
    u = np.clip((x-pad+.5)/(size-2*pad),0,1)
    t = 1-np.clip((y-pad+.5)/(size-2*pad),0,1)
    rgb, height, smoothness, relief = SURFACE_PAINTERS[name](u,t,grain,patches)
    dy,dx = np.gradient(height)
    normal = np.stack((-dx*relief,dy*relief,np.ones_like(dx)),axis=-1)
    normal /= np.linalg.norm(normal,axis=-1,keepdims=True)
    color = np.full((size,size,4),255,dtype=np.uint8)
    color[...,:3] = np.rint(np.clip(rgb,0,255)).astype('uint8')
    normals = np.full_like(color,255)
    normals[...,:3] = np.rint(np.clip(normal*.5+.5,0,1)*255).astype('uint8')
    mask = np.zeros_like(color)
    mask[...,3] = np.rint(np.clip(smoothness,0,1)*255).astype('uint8')
    for pixels in (color,normals,mask):
        pixels[:] = np.pad(pixels[pad:-pad,pad:-pad],((pad,pad),(pad,pad),(0,0)),mode='edge')
    return color,normals,mask
