"""Reference-based Daisy rosettes and Moonflower vines in Blender coordinates."""
from math import cos, exp, pi, sin, tau
from mathutils import Vector
import plant_blender_common as art


UP = Vector((0, 0, 1))
FRONT = Vector((0, -1, 0))


def frame(direction):
    forward = Vector(direction).normalized()
    right = forward.cross(UP if abs(forward.z) < .94 else FRONT).normalized()
    return forward, right, right.cross(forward).normalized()


def surface(name, sections, columns, slot):
    vertices, uv, faces = [], [], []
    for row, section in enumerate(sections):
        for col, point in enumerate(section):
            vertices.append(point)
            uv.append((col / (columns - 1), row / (len(sections) - 1)))
            if row and col:
                i = row * columns + col
                faces.append((i-columns-1, i-columns, i, i-1))
    return art.mesh(name, vertices, faces, slot, uv)


def tube(name, points, radii, slot='FlowerStem', sides=7):
    points = [Vector(p) for p in points]
    vertices, faces, uv = [], [], []
    for row, (point, radius) in enumerate(zip(points, radii)):
        tangent = (points[min(row+1, len(points)-1)] - points[max(0, row-1)]).normalized()
        if row == 0:
            _, x, y = frame(tangent)
        else:
            x = (x - tangent * x.dot(tangent)).normalized()
            y = x.cross(tangent).normalized()
        for i in range(sides+1):
            angle = tau * (i % sides) / sides
            vertices.append(point + radius * (x*cos(angle) + y*sin(angle)))
            uv.append((i/sides, row/(len(points)-1)))
            if row and i:
                v = row*(sides+1)+i
                faces.append((v-sides-2, v-1, v, v-sides-1))
    faces.extend([tuple(range(sides)), tuple((len(points)-1)*(sides+1)+i for i in range(sides-1,-1,-1))])
    return art.mesh(name, vertices, faces, slot, uv)


def oval(name, root, direction, length, radius, depth, slot, low):
    forward, right, normal = frame(direction)
    origin = Vector(root)
    sides, steps = (6, 5) if low else (10, 9)
    rings = []
    for i in range(steps+1):
        t = i / steps
        bulge = max(.015, sin(pi*t)**.84)
        rings.append([origin + forward*length*t + bulge*(right*radius*cos(tau*j/sides)
                      - normal*depth*sin(tau*j/sides)) for j in range(sides+1)])
    obj = surface(name, rings, sides+1, slot)
    return obj


def leaf(name, root, direction, length, width, kind, low=False, lift=.30):
    forward, right, normal = frame(direction)
    origin = Vector(root)
    heart = kind == 'MoonflowerLeaf' and name not in ('Cotyledon', 'Sepal')
    if heart and name != 'Sepal':
        right = forward.cross(Vector((0,-1,.58))).normalized()
        normal = right.cross(forward).normalized()
    elif name == 'Cotyledon':
        right = forward.cross(Vector((0,-.72,.72))).normalized()
        normal = right.cross(forward).normalized()
    lobes = kind == 'DaisyLeaf' and name not in ('Cotyledon', 'Sepal')
    rows = (15 if lobes else 9) if low else (25 if lobes else 15)
    cols = 3 if low else 5
    sections = []
    for i in range(rows):
        t = i / (rows-1)
        if heart:
            outline = max(.012, sin(pi*t**.55)**.72) * (1.12 - .24*t)
        elif lobes:
            outline = max(.018, sin(pi*t**1.25)**.48) * (1 + .16*cos(tau*4.8*t))
        else:
            outline = max(.018, sin(pi*t**1.12)**.64)
        row = []
        for j in range(cols):
            q = 2*j/(cols-1)-1
            along = t - (.28*exp(-((t-.15)/.17)**2) * abs(q)**1.3 if heart else 0)
            rise = sin(pi*t) * (lift - .15*abs(q)**1.4) - .18*t*t
            row.append(origin + forward*length*along + right*(width*.5*outline*q)
                       + normal*length*(rise + q*.035*sin(tau*t)))
        sections.append(row)
    return surface(name, sections, cols, kind)


def sepal_cup(origin, direction, radius, length, slot, low):
    forward, right, normal = frame(direction)
    for i in range(5):
        theta = tau*i/5
        radial = right*cos(theta) + normal*sin(theta)
        leaf('Sepal', Vector(origin) + radial*radius*.22, forward*.82 + radial*.55,
             length, radius*.85, slot, low, .05)


def sprout(kind, low):
    top = (0, 0, .146)
    slot = 'DaisyStem' if kind == 'Daisy' else 'FlowerStem'
    tube('Sprout stem', [(0,0,0), (.002,0,.045), top], [.014,.010,.009], slot, sides=5 if low else 7)
    for side in (-1, 1):
        leaf('Cotyledon', top, (side, -.12, .48), .165, .095, kind+'Leaf', low, .16)


def daisy_flower(origin, direction, size, low):
    axis, a, b = frame(direction)
    origin = Vector(origin)
    sepal_cup(origin-axis*.017*size, axis, .07*size, .054*size, 'DaisyLeaf', low)
    count = 13 if low else 15
    for i in range(count):
        angle = tau*i/count
        radial = a*cos(angle)+b*sin(angle)
        tangent = -a*sin(angle)+b*cos(angle)
        rows, cols = (5, 3) if low else (8, 5)
        section = []
        for row in range(rows):
            t = row/(rows-1)
            blade = (.28+.72*sin(pi*t**1.24)**.55) if row < rows-1 else .13
            reach = (.056 + t*(.157 + .010*sin(i*5))) * size
            curl = (-.006 + .023*sin(pi*t) - .019*t*t) * size
            section.append([origin + radial*reach + tangent*(.035*size*blade*q)
                            + axis*(curl + .012*size*(1-q*q)*sin(pi*t))
                            for q in (2*j/(cols-1)-1 for j in range(cols))])
        surface(f'Ray petal {i+1:02}', section, cols, 'DaisyPetal')
    oval('Raised golden floret disk', origin-axis*.018*size, axis, .081*size,
         .070*size, .070*size, 'DaisyDisk', low)


def daisy_bud(origin, direction, size, low):
    axis, a, b = frame(direction)
    origin = Vector(origin)
    for i in range(5):
        angle = tau*i/5
        radial = a*cos(angle)+b*sin(angle)
        oval('Cream rolled ray', origin + radial*.015*size, axis + radial*.12,
             .102*size, .023*size, .016*size, 'DaisyPetal', low)
        oval('Overlapping green bract', origin-axis*.018*size+radial*.021*size, axis*.98+radial*.18,
             .101*size, .024*size, .014*size, 'DaisyLeaf', low)


def daisy(stage, low):
    if stage == 'Seed':
        for i, (point, angle) in enumerate([((-.074,-.028,.011), .32), ((-.035,.038,.013), -.34), ((.042,-.04,.011), .53)]):
            oval(f'Striped seed {i+1}', point, (cos(angle),sin(angle),.06), .071, .013, .011, 'DaisySeed', low)
        return
    if stage == 'Sprout':
        sprout('Daisy', low)
        return
    if stage == 'Harvest':
        tip = Vector((.38,.035,.12))
        tube('Cut stem', [(-.33,0,.015),(-.16,.002,.033),(.12,.024,.069),tip], [.019,.017,.015,.013],
             'DaisyStem', sides=5 if low else 8)
        leaf('Cut flower leaf', (-.12,.015,.051), (.53,.61,.39), .23, .080, 'DaisyLeaf', low, .17)
        daisy_flower(tip, (.33,-.73,.65), 1.13, low)
        return
    young = stage == 'Young'
    ready = stage == 'Ready'
    n = 9 if young else (11 if low else 15)
    for i in range(n):
        theta = i * 2.39996 + .22
        inner = .60 if i >= n-4 else 0
        length = (.275 if young else .371) * (1-.32*inner) * (1+.12*sin(i*5.1))
        base = (.018*cos(theta), .018*sin(theta), .039+inner*.015)
        leaf(f'Rosette blade {i+1:02}', base, (cos(theta),sin(theta),.14+inner),
             length, length*(.47-.08*inner), 'DaisyLeaf', low, .28 if inner else .23)
    if young:
        return
    for i, (end, axis, size) in enumerate([
        ((-.25,-.02,.54),(-.38,-.62,.68),.76), ((.012,.018,.83),(.01,-.52,.86),1.06),
        ((.244,.006,.52),(.42,-.61,.66),.78)]):
        x,y,z = end
        if not ready:
            x,y,z = x*.83,y,z*.86
        tube(f'Flower stalk {i+1}', [(x*.1,y,.048),(x*.35,y+.022,z*.36),(x*.76,y+.011,z*.73),(x,y,z)],
             [.021,.018,.016,.013], 'DaisyStem', sides=5 if low else 8)
        (daisy_flower if ready else daisy_bud)((x,y,z), axis if ready else (axis[0]*.45,-.08,1), size, low)


def spiral(center, radius, phase, low, slot='FlowerStem'):
    c = Vector(center)
    steps = 13 if low else 25
    points, radii = [], []
    for i in range(steps+1):
        t = i / steps
        angle = phase + t*tau*1.18
        r = radius*(1-.90*t)
        points.append(c + Vector((cos(angle)*r, -.03*t, sin(angle)*r)))
        radii.append(radius*(.18-.134*t))
    return tube('Curling vine tendril', points, radii, slot, 5 if low else 7)


def moonflower_bell(origin, direction, size, low):
    axis, a, b = frame(direction)
    origin = Vector(origin)
    sepal_cup(origin, axis, .044*size, .117*size, 'MoonflowerLeaf', low)
    # Each strip shares the exact boundary with the next strip. The silhouette is a fused five-lobed bell.
    cols = 4 if low else 9
    profile = [(0,.019),(.052,.030),(.102,.060),(.138,.099),(.172,.172),(.164,.194)]
    if not low:
        profile = [(0,.019),(.025,.023),(.055,.032),(.081,.043),(.104,.057),(.128,.083),
                   (.150,.123),(.169,.158),(.178,.181),(.170,.197)]
    for i in range(5):
        sections = []
        for j, (height, radius) in enumerate(profile):
            t = j/(len(profile)-1)
            row = []
            for k in range(cols):
                u = k/(cols-1)
                angle = (i+u)*tau/5
                flute = sin(pi*u)**1.2
                r = radius * (1 + .13*flute*t*t)
                z = height + (.012*flute - .011*(1-flute))*t**2
                row.append(origin + (a*cos(angle)+b*sin(angle))*(r*size) + axis*(z*size))
            sections.append(row)
        surface(f'Fused corolla panel {i+1}', sections, cols, 'MoonflowerPetal')
    for i in range(3 if low else 5):
        angle = tau*i/(3 if low else 5)
        radial = a*cos(angle)+b*sin(angle)
        start = origin+axis*.068*size+radial*.009*size
        tip = origin+axis*.149*size+radial*.024*size
        tube('Filament', [start, start.lerp(tip,.58), tip], [.0036*size,.003*size,.002*size], 'MoonflowerSeed', 4)
        if not low:
            oval('Ivory anther', tip, axis+radial*.24, .016*size, .006*size,.005*size, 'MoonflowerSeed', True)
    oval('Closed flower throat', origin+axis*.016*size, axis, .052*size,
         .036*size, .036*size, 'MoonflowerPetal', True)


def moonflower_bud(origin, direction, size, low):
    axis, _, _ = frame(direction)
    origin = Vector(origin)
    oval('Twisted tapered ivory bud', origin, axis, .225*size, .044*size,.040*size, 'MoonflowerPetal', low)
    sepal_cup(origin, axis, .042*size, .109*size, 'MoonflowerLeaf', low)


def vine_point(t, vine, height, scale):
    angle = t*tau*1.22 + vine*pi
    radius = (.068+.044*sin(pi*t))*scale
    return Vector((radius*cos(angle)+.052*t, radius*.8*sin(angle), .008+height*t*(1-.13*vine)))


def moonflower(stage, low):
    if stage == 'Seed':
        oval('Cream ribbed seed', (-.056,0,.024), (.92,.25,.25), .102, .038, .030, 'MoonflowerSeed', low)
        return
    if stage == 'Sprout':
        sprout('Moonflower', low)
        return
    if stage == 'Harvest':
        origin = (.045,.017,.25)
        tube('Cut flower stem', [(-.13,0,.012),(-.069,.01,.097),(-.024,.016,.169),origin],
             [.016,.015,.012,.012], sides=5 if low else 7)
        leaf('Heart leaf on cutting', (-.066,-.005,.098), (.92,-.30,-.12), .24,.185,'MoonflowerLeaf',low,.21)
        moonflower_bell(origin, (.37,-.75,.55), 1.05, low)
        return
    young = stage == 'Young'
    ready = stage == 'Ready'
    height = .49 if young else (.79 if ready else .72)
    scale = .68 if young else 1.
    for vine in range(2):
        steps = 9 if low else 16
        points = [vine_point(i/steps, vine, height, scale) for i in range(steps+1)]
        tube('Twining main vine', points, [(1-.58*i/steps)*.027*scale for i in range(steps+1)], sides=5 if low else 8)
    spiral((.09,.028,height+.084), .101*scale, -1.68, low)
    spiral((-.136*scale,-.053,.101), .072*scale, -.50, low)
    if not young:
        spiral((.239,-.027,.275), .067, pi, low)
        if not low:
            spiral((.226,.036,height-.015), .042, pi*.97, low)
    placements = [(-1,.10,.27,-.10,-.31),(1,.13,.30,-.17,-.23),(-1,.36,.36,-.22,-.23),
                  (1,.39,.33,-.16,-.25),(-1,.61,.31,-.14,.03),(1,.63,.29,.13,-.16),
                  (-1,.82,.23,.14,.03),(1,.90,.22,-.12,.04),(-1,.28,.23,.39,-.20),(1,.52,.26,.42,-.16),
                  (-1,.66,.20,.42,.08),(1,.77,.23,.34,.04),(-1,.10,.19,-.48,-.16)]
    for i, (side, fraction, length, depth, slope) in enumerate(placements[:7 if young else 13]):
        if low and i in (8,10,11):
            continue
        z = .06+height*fraction
        t = max(0, min(.98, (z-.008)/height))
        anchor = vine_point(t, 0, height, scale)
        root = anchor + Vector((side*.12*scale,depth*.22,slope*.08))
        tube('Leaf petiole', [anchor,anchor.lerp(root,.48)+UP*.016,root], [.016*scale,.012*scale,.009*scale], sides=5)
        leaf(f'Heart-shaped leaf {i+1:02}', root, (side*.84,depth,slope), length*scale,
             length*.92*scale, 'MoonflowerLeaf', low, .15)
    if young:
        origin = Vector((-.061,-.018,.377))
        start = vine_point(.68, 0, height, scale)
        tube('Young flower peduncle', [start, start.lerp(origin,.52), origin], [.013,.010,.008], sides=5)
        moonflower_bud(origin,(-.34,.07,1),.48,low)
        return
    if ready:
        clusters = [((-.14,-.225,.42),(-.35,-.88,.47),.87,True),
                    ((.186,-.15,.56),(.48,-.84,.37),.91,True),
                    ((-.047,-.168,.75),(-.10,-.88,.51),1.03,True),
                    ((.235,.064,.785),(.41,.14,1),.81,False)]
    else:
        clusters = [((-.17,-.08,.32),(-.35,-.15,1),.92,False),
                    ((.13,-.076,.55),(.32,-.12,1),.91,False),
                    ((-.066,.01,.64),(-.30,.11,1),.92,False)]
    for end, axis, size, opened in clusters:
        tip = Vector(end)
        start = vine_point(max(.04,(end[2]-.168)/height), 0, height, scale)
        tube('Flower peduncle', [start,start.lerp(tip,.54)+UP*.019,tip], [.018,.015,.012], sides=5 if low else 7)
        (moonflower_bell if opened else moonflower_bud)(tip, axis, size, low)
