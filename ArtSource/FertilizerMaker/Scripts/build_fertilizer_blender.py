"""Blender 4.5: reference fertilizer maker, existing Cozy atlas + additive page 05.
Editable model parts and hinges; three runtime LODs, FBX and explicit Unity mesh JSON.
"""
import bpy, bmesh, math, json, random, hashlib
from pathlib import Path
from mathutils import Vector

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'
SOURCE=ROOT/'ArtSource/FertilizerMaker/Blender'
MODELS=A/'mesh/Props/FertilizerMaker'
OUT=ROOT/'ArtSource/FertilizerMaker/Previews'
for directory in (SOURCE,MODELS,OUT):directory.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
parts=bpy.data.collections.new('FERTILIZER MAKER - editable parts');scene.collection.children.link(parts)
studio=bpy.data.collections.new('STUDIO - excluded from export');scene.collection.children.link(studio)
slots={};materials=[]
for page in ['01','05']:
    filename='NP_CozyAtlasSlots'+('' if page=='01' else '_05')+'.json'
    manifest=json.loads((A/'Textures/Shared/Cozy'/filename).read_text())
    for s in manifest['slots']:slots[s['name']]=(len(materials),s['uvSafe'])
    mat=bpy.data.materials.new('NP_CozySurfaces_'+page);mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value=.65
    for suffix in ['BaseColor','Normal','SurfaceMask']:
        image=bpy.data.images.load(str(A/f'Textures/Shared/Cozy/NP_CozySurfaces_{page}_{suffix}.png'));image.pack()
        if suffix!='BaseColor':image.colorspace_settings.name='Non-Color'
        if suffix=='SurfaceMask':image.alpha_mode='CHANNEL_PACKED'
        tex=nodes.new('ShaderNodeTexImage');tex.image=image
        if suffix=='BaseColor':links.new(tex.outputs['Color'],bs.inputs['Base Color'])
        elif suffix=='Normal':
            normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.18
            links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bs.inputs['Normal'])
        else:
            sep=nodes.new('ShaderNodeSeparateColor');links.new(tex.outputs['Color'],sep.inputs['Color']);links.new(sep.outputs['Red'],bs.inputs['Metallic'])
            inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
            links.new(tex.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
    materials.append(mat)

# Reuse the project's scanner geometry/atlas helpers, without running scanner construction.
helper=(ROOT/'ArtSource/SoilScanner/Scripts/build_scanner_blender.py').read_text()
exec('def select(o):'+helper.split('def select(o):',1)[1].split('# Cabinet and feet:',1)[0])

def lathe(name,profile,slot,center=(0,0,0),scale=(1,1),segments=48,power=2):
    verts=[]
    for radius,z in profile:
        for i in range(segments):
            angle=math.tau*i/segments;c,s=math.cos(angle),math.sin(angle)
            x=math.copysign(abs(c)**(2/power),c)*radius*scale[0]
            y=math.copysign(abs(s)**(2/power),s)*radius*scale[1]
            verts.append((center[0]+x,center[1]+y,center[2]+z))
    faces=[]
    for row in range(len(profile)-1):
        for i in range(segments):
            j=(i+1)%segments;a=row*segments;b=(row+1)*segments
            faces.append((a+i,a+j,b+j,b+i))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new(name,mesh);parts.objects.link(obj)
    return finish(obj,name,slot)

def ring(name,center,radius,width,depth,slot,axis='Z'):
    obj=lathe(name,[(radius-width,-depth/2),(radius,-depth/2),(radius,depth/2),(radius-width,depth/2),(radius-width,-depth/2)],slot)
    if axis=='Y':obj.rotation_euler[0]=math.pi/2
    if axis=='X':obj.rotation_euler[1]=math.pi/2
    obj.location=center
    return obj

def screw(name,x,y,z,r=.014):
    cylinder(name,(x,y,z),r,.009,'Aluminium','Y',12)
    groove=box(name+' - slot',(x,y-.006,z),(.018,.0025,.0025),'Iron',.001)
    groove.rotation_euler[1]=math.radians(35)

# Broad, rounded rectangular tank. Its front is Blender -Y / Unity -Z.
body_profile=[(0,.105),(.34,.105),(.43,.14),(.485,.22),(.50,.30),(.50,.69),
              (.491,.78),(.465,.86),(.41,.925),(.32,.958),(.22,.966),(0,.966)]
lathe('01 - continuous rounded red enamel tank',body_profile,'EnamelRed',scale=(1.10,.86),segments=64,power=3.2)
for side in [-1,1]:
    box('02 - dark protective sled foot',(side*.455,-.035,.082),(.168,.73,.164),'Iron',.047)
    screw('03 - foot bolt',side*.455,-.407,.093,.015)
lathe('04 - continuous aluminium waist band',[(.50,.288),(.509,.29),(.511,.305),(.511,.338),(.503,.35),(.50,.347)],
      'Aluminium',scale=(1.10,.86),segments=64,power=3.2)
for angle in [-2.65,-2.15,-1.0,-.5,.45,1.2,1.95,2.65]:
    c,s=math.cos(angle),math.sin(angle)
    x=math.copysign(abs(c)**(2/3.2),c)*.513*1.1;y=math.copysign(abs(s)**(2/3.2),s)*.513*.86
    sphere('05 - waist band domed rivet',(x,y,.319),(.013,.013,.014),'Aluminium')

# Open cream hopper with a real inner wall and recessed bottom.
lathe('06 - loading hopper double-wall ceramic rim',[(.226,.965),(.237,.988),(.250,1.065),(.276,1.113),
    (.278,1.13),(.265,1.143),(.249,1.126),(.240,1.07),(.217,.995),(.215,.976),(.226,.965)],'PaintCream',segments=64)
cylinder('07 - recessed hopper interior floor',(0,0,.971),.215,.008,'PaintCream',vertices=48)
ring('08 - hopper aluminium mounting collar',(0,0,.987),.243,.014,.042,'Aluminium')
for i in range(8):
    a=math.tau*i/8;sphere('09 - hopper collar rivet',(.245*math.cos(a),.245*math.sin(a),.986),(.010,.010,.011),'Aluminium')

# Lid stays fully editable on a meaningful hinge pivot in the master Blender file.
hinge=bpy.data.objects.new('PIVOT - hopper lid opens about local X',None);parts.objects.link(hinge)
hinge.location=(0,.205,1.138);hinge.rotation_euler[0]=math.radians(-58)
lid=lathe('10 - open cream lid / shallow concave inner face',[(0,.032),(.16,.031),(.225,.024),(.247,.008),
    (.252,-.003),(.263,.003),(.263,.043),(.248,.054),(.18,.058),(0,.058)],'PaintCream',center=(0,-.19,0),segments=64)
lid.parent=hinge
for x in [-.062,.062]:
    mount=box('11 - lid handle mounting pad',(x,-.395,.063),(.040,.045,.025),'Aluminium',.009);mount.parent=hinge
grab=bezel('12 - lid lift handle open centre',0,-.427,.086,.17,.055,.017,.012,.035,'Aluminium');grab.parent=hinge
for x in [-.104,0,.104]:cylinder('13 - lid hinge barrel',(x,.205,1.138),.029,.097,'Aluminium','X',24)
for x in [-.159,.159]:
    cylinder('14 - hinge pin end',(x,.205,1.138),.023,.012,'Iron','X',16)

# Short mushroom vent with actual spaces between the cream hood supports.
cylinder('15 - vent base flange',(.385,.095,.963),.073,.026,'Aluminium',vertices=32)
cylinder('16 - dark vent neck',(.385,.095,1.014),.053,.096,'Iron',vertices=24)
for i in range(6):
    a=math.tau*i/6
    bar=box('17 - vent hood slotted support',(.385+.052*math.cos(a),.095+.052*math.sin(a),1.079),(.014,.014,.059),'Aluminium',.004)
lathe('18 - cream mushroom vent cap',[(0,0),(.080,0),(.089,.012),(.078,.030),(.059,.050),(.032,.063),(0,.067)],
      'PaintCream',center=(.385,.095,1.104),segments=32)
for i in range(4):
    a=math.tau*i/4;sphere('19 - vent flange bolt',(.385+.064*math.cos(a),.095+.064*math.sin(a),.976),(.008,.008,.008),'Aluminium')

# Raised front console: analogue meter plus green/orange physical buttons.
box('20 - dark rounded console housing',(0,-.416,.789),(.475,.106,.207),'Iron',.036)
bezel('21 - console aluminium edge',0,-.475,.789,.466,.203,.045,.011,.017,'Aluminium')
cylinder('22 - gauge backing',(-.082,-.485,.789),.089,.012,'Iron','Y',48)
ring('23 - gauge metal bezel',(-.082,-.499,.789),.092,.010,.018,'Aluminium','Y')
gauge=cylinder('24 - printed analogue gauge face',(-.082,-.502,.789),.081,.004,'GaugeFace','Y',48)
# Needle remains separate geometry, pointing towards the upper-right of the dial.
needle=box('25 - gauge pointer',(-.060,-.511,.813),(.006,.006,.063),'Iron',.002)
needle.rotation_euler[1]=math.radians(44)
sphere('26 - gauge needle hub',(-.082,-.516,.789),(.012,.006,.012),'Iron')
for z,slot in [(.841,'LeafGreen'),(.751,'FoodOrange')]:
    cylinder('27 - button metal collar',(.142,-.491,z),.032,.018,'Aluminium','Y',24)
    sphere('28 - '+slot+' push button',(.142,-.511,z),(.026,.012,.026),slot)
for x in [-.168,.004]:screw('29 - gauge face retaining screw',x,-.516,.789,.004)

# Front service hatch with open-centre frame, visible hinges, latch and screws.
box('30 - service hatch dark gasket',(0,-.422,.531),(.565,.063,.283),'Iron',.036)
bezel('31 - continuous aluminium service door frame',0,-.462,.531,.560,.282,.040,.024,.025,'Aluminium')
door_pivot=bpy.data.objects.new('PIVOT - service hatch left hinge',None);parts.objects.link(door_pivot)
door_pivot.location=(-.245,-.468,.531)
door=box('32 - red inset service hatch',(.245,0,0),(.487,.025,.218),'EnamelRed',.025);door.parent=door_pivot
for x in [-.248,.248]:
    for z in [.423,.638]:screw('33 - slotted service frame screw',x,-.492,z,.012)
for z in [.476,.584]:cylinder('34 - service door hinge barrel',(-.248,-.495,z),.015,.060,'Aluminium','Z',20)
cylinder('35 - continuous service hinge pin',(-.248,-.495,.531),.007,.158,'Iron','Z',16)
box('36 - hatch latch arm',(.195,-.496,.531),(.120,.021,.034),'Aluminium',.011)
box('37 - latch clasp',(.260,-.497,.531),(.033,.038,.100),'Aluminium',.012)
screw('38 - latch pivot screw',.149,-.511,.531,.012)

# A genuine open extrusion chute, with a dark interior rather than a filled cube.
box('39 - output mounting gasket',(0,-.435,.267),(.253,.039,.172),'Iron',.027)
bezel('40 - aluminium output flange',0,-.465,.269,.253,.170,.029,.020,.025,'Aluminium')
verts=[]
for y,z in [(-.452,.288),(-.642,.224)]:
    for w,h in [(.188,.107),(.158,.077)]:
        verts.extend([(-w/2,y,z-h/2),(w/2,y,z-h/2),(w/2,y,z+h/2),(-w/2,y,z+h/2)])
faces=[]
for i in range(4):
    j=(i+1)%4;faces.extend([(i,j,8+j,8+i),(4+i,12+i,12+j,4+j),(8+i,8+j,12+j,12+i)])
mesh=bpy.data.meshes.new('Hollow discharge chute');mesh.from_pydata(verts,[],faces);mesh.update()
chute=bpy.data.objects.new('41 - hollow downward red discharge chute',mesh);parts.objects.link(chute);finish(chute,chute.name,'EnamelRed',.007,2)
box('42 - recessed dark outlet interior',(0,-.447,.288),(.158,.006,.078),'Iron',.005)

# Removable cream collection tray, frontward of both tank and feet.
base=box('43 - dark collection tray base',(0,-.614,.043),(.45,.360,.056),'Iron',.050)
tray=bezel('44 - open cream tray rim',0,0,0,.431,.326,.058,.025,.083,'PaintCream')
tray.rotation_euler[0]=math.pi/2;tray.location=(0,-.614,.063)
box('45 - cream tray recessed floor',(0,-.614,.068),(.392,.291,.014),'PaintCream',.035)
for side in [-1,1]:
    inset=bezel('46 - tray side handhold aluminium edge',0,0,0,.123,.045,.013,.009,.008,'Aluminium')
    inset.rotation_euler[2]=math.pi/2;inset.location=(side*.214,-.614,.119)
    shadow=box('47 - dark recessed side handhold',(side*.213,-.614,.119),(.006,.105,.028),'Iron',.008)
random.seed(81126)
for row in range(5):
    for col in range(8):
        x=(col-3.5)*.039+random.uniform(-.004,.004);y=-.614+(row-2)*.038+random.uniform(-.005,.005)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,.090+random.uniform(0,.012)))
        pellet=bpy.context.object;pellet.scale=(random.uniform(.014,.019),random.uniform(.014,.019),random.uniform(.011,.016))
        finish(pellet,'48 - organic fertilizer granule '+str(row*8+col),'Earth' if (row+col)%3 else 'SoilDark')
for i in range(7):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(random.uniform(-.09,.09),random.uniform(-.65,-.56),.116))
    pellet=bpy.context.object;pellet.scale=(.016,.016,.014);finish(pellet,'49 - top layer fertilizer granule '+str(i),'Earth')

# Back/side finishing for a machine viewed from every direction in the map.
box('50 - rear service panel',(0,.425,.555),(.38,.026,.30),'EnamelRed',.021)
for z in [.512,.548,.584,.620]:box('51 - rear cooling louvre',(0,.444,z),(.265,.009,.012),'Iron',.004)
for x in [-.164,.164]:
    for z in [.438,.672]:screw('52 - rear maintenance screw',x,.444,z,.010)
box('53 - rear maker plate',(0,.443,.465),(.15,.006,.038),'Aluminium',.006)

master=[o for o in parts.objects if o.type=='MESH']
for obj in master:
    obj['Model']='Fertilizer Maker';obj['UnityFront']='-Z'
    # Blender joins UV layers by name. Primitive UVMap and custom AtlasUV must
    # be unified, or parts of the combined runtime mesh receive zero UVs.
    obj.data.uv_layers.active.name='AtlasUV'

def export_lod(lod):
    copies=[]
    for original in master:
        o=original.copy();o.data=original.data.copy();o.parent=None;o.matrix_world=original.matrix_world.copy()
        scene.collection.objects.link(o);select(o)
        dec=o.modifiers.new('Distance simplification','DECIMATE');dec.ratio=[.52,.26,.11][lod]
        dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
        copies.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=copies[0]
    o.name='FertilizerMaker_Cozy_LOD'+str(lod);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    tri=o.modifiers.new('Runtime triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
    bpy.ops.export_scene.fbx(filepath=str(MODELS/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    me=o.data;uv=me.uv_layers.active.data;norm=me.corner_normals
    assert len(me.uv_layers)==1,list(me.uv_layers.keys())
    vertices=[];normals=[];uvs=[];submeshes=[[],[]];lookup={}
    for face in me.polygons:
        indices=[]
        for li in face.loop_indices:
            p=me.vertices[me.loops[li].vertex_index].co;n=norm[li].vector;u=uv[li].uv
            key=tuple(round(float(v),7) for v in (*p,*n,*u))
            if key not in lookup:
                lookup[key]=len(vertices);vertices.append([float(p.x),float(p.z),float(p.y)])
                normals.append([float(n.x),float(n.z),float(n.y)]);uvs.append(list(u))
            indices.append(lookup[key])
        material=materials.index(o.data.materials[face.material_index])
        submeshes[material].extend([indices[0],indices[2],indices[1]])
    spec=dict(name=o.name,vertices=vertices,normals=normals,uv=uvs,submeshes=submeshes,
        materials=[m.name for m in materials],triangles=sum(len(s)//3 for s in submeshes),
        min=[min(p[i] for p in vertices) for i in range(3)],max=[max(p[i] for p in vertices) for i in range(3)])
    assert all(0<u<1 and 0<v<1 for u,v in uvs),'Atlas UVs must stay inside padded slots, never default to zero'
    (MODELS/(o.name+'.json')).write_text(json.dumps(spec,separators=(',',':')))
    if lod==0:
        o.name='REVIEW - actual runtime LOD0';move(o,studio);o.hide_render=True
    else:bpy.data.objects.remove(o,do_unlink=True)
    return {key:spec[key] for key in ['name','triangles','min','max']}

lods=[export_lod(lod) for lod in range(3)]
manifest=dict(parts=len(master),lods=lods,front='Unity -Z',origin='ground centre',units='metres',
              sharedMaterials=[m.name for m in materials],existingAtlasModified=False,
              pivots=['PIVOT - hopper lid opens about local X','PIVOT - service hatch left hinge'])
(SOURCE/'FertilizerMaker_Manifest.json').write_text(json.dumps(manifest,indent=2))

# Studio belongs to a separate collection and is never exported into the game.
def studio_mat():
    m=bpy.data.materials.new('Studio warm stone');m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.66,.67,.62,1);bs.inputs['Roughness'].default_value=1
    return m
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Studio ground';ground.location.z=-.007
ground.data.materials.append(studio_mat());move(ground,studio)
def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.5,-4.5,2.7));camera=bpy.context.object;camera.name='Review - front right';move(camera,studio)
camera.data.type='ORTHO';camera.data.ortho_scale=1.93;aim(camera,(0,-.10,.72));scene.camera=camera
for name,pos,power,size in [('Key',(-3,-4,6),460,4),('Fill',(4,-2,3),240,3),('Rim',(1,4,4),350,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.name=name;lamp.data.energy=power
    lamp.data.shape='DISK';lamp.data.size=size;aim(lamp,(0,0,.65));move(lamp,studio)
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.43,.45,.44,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.35
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.cycles.device='CPU'
scene.view_settings.view_transform='AgX';scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_distance=2.6;area.spaces.active.region_3d.view_location=(0,0,.7);area.spaces.active.shading.type='MATERIAL'
select(lid);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FertilizerMaker_Cozy_Master.blend'))
parts.hide_render=True;bpy.data.objects['REVIEW - actual runtime LOD0'].hide_render=False
for name,position in [('FrontRight',(2.5,-4.5,2.7)),('RearLeft',(-2.8,4.3,2.55)),('Front',(0,-4.8,2.1))]:
    camera.location=position;aim(camera,(0,-.1,.72));scene.render.filepath=str(OUT/('FertilizerMaker_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
print('FERTILIZER_BLENDER_PASS '+json.dumps(manifest),flush=True)
