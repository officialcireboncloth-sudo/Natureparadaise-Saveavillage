"""Reference-specific root crops with radial, curved foliage and hollow onion tips."""
import math
import bpy
from mathutils import Vector
import plant_blender_common as C

def bezier(points, count=25):
    a,b,c,d=map(Vector,points)
    return [(1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d for t in [i/(count-1) for i in range(count)]]

def swept(name, points, radii, slot, sides=8, hollow=False, yellow=False):
    """Tube frames follow the centreline; hollow leaves have a recessed inner wall."""
    pts=list(map(Vector,points)); vertices=[]; uv=[]; faces=[]; frames=[]
    for i,p in enumerate(pts):
        d=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
        n=d.cross(Vector((0,1,0)))
        if n.length<.01:n=d.cross(Vector((1,0,0)))
        n.normalize();b=d.cross(n).normalized();frames.append((n,b))
        for j in range(sides+1):
            a=j*math.tau/sides
            vertices.append(p+radii[i]*(n*math.cos(a)+b*math.sin(a)))
            uv.append((j/sides,(i/(len(pts)-1))*(1 if yellow else .79)))
    for i in range(len(pts)-1):
        for j in range(sides):
            a=i*(sides+1)+j;faces.append((a,a+1,a+sides+2,a+sides+1))
    faces.append(tuple(range(sides-1,-1,-1)))
    if hollow:
        rim=(len(pts)-1)*(sides+1);start=len(vertices)
        for k in range(5):
            index=len(pts)-1-k;p=pts[index];n,b=frames[index]
            for j in range(sides+1):
                a=j*math.tau/sides;vertices.append(p+radii[index]*.70*(n*math.cos(a)+b*math.sin(a)))
                uv.append((j/sides,.55))
        for j in range(sides):faces.append((rim+j,rim+j+1,start+j+1,start+j))
        for k in range(4):
            for j in range(sides):
                a=start+k*(sides+1)+j;faces.append((a,a+sides+1,a+sides+2,a+1))
        faces.append(tuple(start+4*(sides+1)+j for j in range(sides)))
    else:faces.append(tuple((len(pts)-1)*(sides+1)+j for j in range(sides)))
    return C.mesh(name,vertices,faces,slot,uv)

def root_curve(name, points, radius=.004, slot='RootCream', count=15):
    pts=bezier(points,count)
    return swept(name,pts,[radius*(1-i/(count-1))+.00065 for i in range(count)],slot,6)

def interp(t, profile):
    for i,((x,a),(y,b)) in enumerate(zip(profile,profile[1:])):
        if x<=t<=y:
            left=profile[max(0,i-1)];right=profile[min(len(profile)-1,i+2)]
            m0=(b-left[1])/(y-left[0]);m1=(right[1]-a)/(right[0]-x)
            q=(t-x)/(y-x)
            value=(2*q**3-3*q*q+1)*a+(q**3-2*q*q+q)*(y-x)*m0+(-2*q**3+3*q*q)*b+(q**3-q*q)*(y-x)*m1
            return max(.01,min(1.025,value))
    return profile[-1][1]

def bulb(kind, scale=1, cut=False):
    onion=kind=='Onion'; rings=31; segments=32 if not cut else 16
    profile=[(0,.09),(.06,.48),(.18,.84),(.34,1),(.49,.98),(.64,.82),(.78,.51),(.90,.24),(1,.18)] if onion else [(0,.04),(.08,.48),(.22,.82),(.42,.99),(.58,.99),(.78,.83),(.92,.50),(1,.14)]
    low,high,r=(.07,.47,.215) if onion else (.075,.425,.18)
    verts=[];uv=[];faces=[]
    for i in range(rings):
        t=i/(rings-1);radius=interp(t,profile)*r*scale;z=(low+(high-low)*t)*scale
        for j in range(segments+1):
            a=j*(math.pi if cut else math.tau)/segments
            rr=radius*(1+.012*math.cos(a*7)*math.sin(math.pi*t))
            verts.append((rr*math.cos(a),rr*math.sin(a)*(.96 if not onion else 1),z))
            uv.append((j/segments,t))
    for i in range(rings-1):
        for j in range(segments):
            k=i*(segments+1)+j;faces.append((k,k+1,k+segments+2,k+segments+1))
    faces.extend([tuple(range(segments,-1,-1)),tuple((rings-1)*(segments+1)+j for j in range(segments+1))])
    o=C.mesh(kind+' smooth '+('half ' if cut else '')+'bulb',verts,faces,'OnionSkin' if onion else 'RadishGradient',uv)
    o['BulbKind']=kind
    if cut:
        # A longitudinal cut follows the actual pear/round perimeter instead of floating above it.
        edge=[(verts[i*(segments+1)][0],0,verts[i*(segments+1)][2]) for i in range(rings)]
        edge += [(verts[i*(segments+1)+segments][0],0,verts[i*(segments+1)+segments][2]) for i in range(rings-1,-1,-1)]
        center=Vector((0,-.0008,(low+high)*scale*.5));vs=[center];coords=[(.5,.5)]
        for x,y,z in edge:
            vs.append((x,-.001,z));coords.append((.5+x/(r*scale*2.10),.5+(z-center.z)/((high-low)*scale*1.10)))
        f=[(0,1+(i+1)%len(edge),1+i) for i in range(len(edge))]
        C.mesh(kind+' ivory cut face',vs,f,'OnionCut' if onion else 'RadishCut',coords)
    return o

def radish_blade(name, angle, elevation, attach, length, width, lobed=True, roll=0):
    a=math.radians(angle);e=math.radians(elevation)
    d=Vector((math.cos(a)*math.cos(e),math.sin(a)*math.cos(e),math.sin(e)))
    hint=Vector((0,-.8,.6)) if 'Sprout' in name or 'Young' in name else Vector((0,0,1))
    n=hint-d*hint.dot(d);n.normalize();u=d.cross(n).normalized()
    u,n=u*math.cos(roll)+n*math.sin(roll),-u*math.sin(roll)+n*math.cos(roll)
    verts=[];uv=[];faces=[];rows=35;cols=7
    for i in range(rows):
        t=i/(rows-1)
        blade=max(0,min(1,(t-.08)/.92))
        shape=.014+math.sin(math.pi*blade)**.42*(.58+.42*blade)
        if lobed:shape*=1+.19*math.sin(blade*math.pi*8+.20)*math.sin(math.pi*blade)
        else:shape=math.sin(math.pi*t)**.60*.93+.014
        for j in range(cols):
            q=j/(cols-1)*2-1
            bend=length*(.10*math.sin(math.pi*t)-.16*t*t-.11*q*q*math.sin(math.pi*t)+.018*math.sin(t*math.pi*8+a)*q*q)
            verts.append(Vector(attach)+d*(length*t)+u*(width*.5*shape*q)+n*bend)
            uv.append(((q+1)*.5,t))
    for i in range(rows-1):
        for j in range(cols-1):
            k=i*cols+j;faces.append((k,k+1,k+cols+1,k+cols))
    o=C.mesh(name,verts,faces,'LeafLobed',uv);m=o.modifiers.new('Supple thin blade','SOLIDIFY');m.thickness=.0018;m.offset=0
    o['ReferenceLeaf']='Radish';o['LeafAzimuthDeg']=angle%360;o['LeafNormal']=list(n);return o

def radish_foliage(stage):
    if stage=='Sprout':base=.095;specs=[(200,24,.135,.105),(20,33,.14,.105)]
    elif stage=='Young':base=.17;specs=[(205,12,.23,.15),(80,67,.255,.15),(350,30,.245,.16),(285,6,.14,.105)]
    elif stage=='RootForming':base=.255;specs=[(205,32,.34,.20),(80,72,.39,.19),(335,27,.34,.22),(140,0,.24,.17)]
    else:base=.425;specs=[(205,59,.41,.30),(45,68,.44,.30),(340,36,.34,.27),(135,18,.27,.24),(275,82,.27,.135),(90,55,.30,.18)]
    for i,(angle,elevation,length,width) in enumerate(specs):
        a=math.radians(angle);radial=Vector((math.cos(a),math.sin(a),0));pet=.038 if stage=='Sprout' else .07 if stage=='Young' else .105
        origin=Vector((0,0,base));attach=origin+radial*pet;attach.z+=pet*.90
        pts=bezier([origin,origin+radial*pet*.25+Vector((0,0,pet*.55)),attach-radial*.012,attach],13)
        swept('Radish curved petiole',pts,[.0075 if stage!='Sprout' else .0048]*len(pts),'StemGreen',7)
        radish_blade('Radish '+stage+' scalloped blade',angle,elevation,attach,length,width,stage!='Sprout',roll=.16*math.sin(i*1.91))

def radish(stage):
    if stage=='Seed':
        for i,(x,y,z) in enumerate([(-.06,-.02,.025),(.055,-.01,.024),(-.003,.067,.025)]):
            o=C.sphere('Radish rounded tan seed',(x,y,z),(.029,.025,.023),'SeedTan',2);o.rotation_euler=(.2,.4,i*.9)
        return
    if stage in ['Sprout','Young']:
        base=.095 if stage=='Sprout' else .17
        pts=bezier([(0,0,.025),(.001,0,.045),(.002,0,base*.8),(0,0,base)],15)
        swept('Pale radish hypocotyl',pts,[.004+.010*math.sin(i/(len(pts)-1)*math.pi) for i in range(len(pts))],'RootCream',8)
        root_curve('Radish pointed juvenile root',[(0,0,.035),(-.012,.005,.019),(.004,0,.013),(-.018,0,.001)],.003)
    else:
        scale=.625 if stage=='RootForming' else 1
        bulb('Radish',scale)
        root_curve('Radish curved ivory taproot',[(0,0,.087*scale),(.008,0,.043*scale),(.028,0,.012),(.115*scale,.008,.002)],.010*scale, count=24)
    if stage=='Harvest':
        # The reference's whole harvested radish keeps its leaf tops.
        radish_foliage('Ready')
    else:radish_foliage(stage)

def onion_roots(scale=1):
    for i in range(12):
        a=i*math.tau/12+.17;r=.085+(.025 if i%3==0 else 0);r*=scale
        base=(.022*scale*math.cos(a),.022*scale*math.sin(a),.074*scale)
        root_curve('Onion radial cream root',[base,(.047*scale*math.cos(a),.047*scale*math.sin(a),.038*scale),(r*.82*math.cos(a+.15),r*.82*math.sin(a+.15),.014),(r*math.cos(a+.22),r*math.sin(a+.22),.004)],.0056*scale,count=11)

def onion_leaf(points, radius, open_tip=False, yellow=False):
    pts=bezier(points,28);radii=[]
    for i in range(len(pts)):
        t=i/(len(pts)-1)
        radii.append(radius*((1-.42*t) if open_tip else max(.028,(1-t)**.38)))
    o=swept('Onion '+('open hollow tube' if open_tip else 'curved pointed tube'),pts,radii,'LeafStrap',12,hollow=open_tip,yellow=yellow)
    o['ReferenceLeaf']='Onion';o['HollowTip']=open_tip;o['LeafAzimuthDeg']=math.degrees(math.atan2(pts[-1].y-pts[0].y,pts[-1].x-pts[0].x))%360;return o

def onion_sheaths(scale=1, dry=False):
    for i in range(5):
        a=i*math.tau/5
        verts=[];uv=[]
        for j,(r,z) in enumerate([(.065,.395),(.057,.44),(.041,.50+(.016 if i%2 else 0))]):
            for q in [-1,1]:
                an=a+q*.40;verts.append((r*math.cos(an)*scale,r*math.sin(an)*scale,z*scale));uv.append(((q+1)*.5,j*.5))
        o=C.mesh('Onion papery overlapping neck',verts,[(0,1,3,2),(2,3,5,4)],'OnionSkin',uv)
        m=o.modifiers.new('Papery sheath edge','SOLIDIFY');m.thickness=.0015

def onion(stage):
    if stage=='Seed':
        for i,(x,y) in enumerate([(-.07,-.02),(.04,-.025),(-.025,.05),(.075,.06)]):
            o=C.sphere('Onion angular black seed',(x,y,.018),(.026,.019,.020),'SeedBlack',1);o.rotation_euler=(i*.42,.3,i*1.77)
        return
    if stage=='Sprout':
        pts=bezier([(0,0,.06),(-.085,-.005,.15),(-.075,.005,.28),(.04,.012,.31)],25)
        pts+=bezier([(.04,.012,.31),(.155,.02,.34),(.085,.025,.17),(.009,.015,.105)],25)[1:]
        leaf=swept('Onion single looped sprout',pts,[.011*(1-.56*i/(len(pts)-1)) for i in range(len(pts))],'LeafStrap',10)
        leaf['ReferenceLeaf']='Onion'
        root_curve('Onion sprout pale base',[(0,0,.10),(0,0,.055),(.005,0,.028),(-.025,0,.004)],.006)
        root_curve('Onion sprout side root',[(0,0,.034),(.015,.006,.018),(.035,.007,.012),(.055,.008,.009)],.0024)
        return
    if stage=='Young':
        onion_roots(.72);origin=.08
        for end,open_tip in [((-.095,.06,.55),True),((.012,-.035,.61),True),((.095,-.07,.47),True)]:
            onion_leaf([(0,0,origin),(.0,0,.20),(end[0]*.7,end[1]*.7,end[2]*.8),end],.017,open_tip)
        swept('Onion pale juvenile stem',[Vector((0,0,.065+i*.008)) for i in range(9)],[.021]*9,'RootCream',10)
        return
    scale=.54 if stage=='BulbForming' else 1
    bulb('Onion',scale);onion_roots(scale);onion_sheaths(scale,stage=='Harvest')
    if stage=='Harvest':return
    if stage=='BulbForming':
        base=.47*scale
        specs=[([(-.005,0,base),(-.035,.035,.39),(-.10,.12,.63),(-.135,.14,.76)],True),
               ([(0,0,base),(.005,-.025,.41),(.012,-.065,.61),(.015,-.08,.75)],True),
               ([(0,0,base),(.045,.025,.39),(.12,.04,.68),(.22,.055,.84)],False),
               ([(0,0,base),(.035,-.035,.33),(.10,-.12,.44),(.14,-.15,.51)],True)]
        for points,op in specs:onion_leaf(points,.020,op)
    else:
        specs=[([(0,0,.465),(-.12,-.065,.78),(-.25,-.20,1.04),(-.38,-.24,.78)],False,True),
               ([(0,-.005,.46),(.00,-.025,.69),(-.015,-.055,.96),(-.02,-.065,1.04)],True,False),
               ([(0,.01,.46),(.035,.065,.75),(.10,.16,1.03),(.17,.21,1.13)],False,False),
               ([(0,.01,.46),(.15,.16,.85),(.37,.31,1.16),(.46,.36,.80)],False,True),
               ([(0,0,.46),(.10,-.11,.73),(.40,-.30,1.00),(.39,-.34,.57)],False,True)]
        for points,op,yellow in specs:onion_leaf(points,.027,op,yellow)

def cut_half(kind):
    bulb(kind,1,True)
    if kind=='Onion':onion_roots(.95)
    else:root_curve('Cut radish ivory tip',[(0,0,.085),(.01,.01,.04),(.06,.02,.012),(.12,.015,.002)],.009)
