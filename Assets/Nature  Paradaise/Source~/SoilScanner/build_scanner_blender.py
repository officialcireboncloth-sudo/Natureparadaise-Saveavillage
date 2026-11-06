"""Blender 4.5: editable modular scanner, shared atlas UVs, FBX and explicit Unity mesh data."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'; SOURCE=A/'Source~/SoilScanner'; MODELS=A/'mesh/Props/SoilScanner'; OUT=ROOT/'ArtSource/SoilScanner/Review'
for p in (SOURCE,MODELS,OUT):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
parts=bpy.data.collections.new('SCANNER - editable parts');scene.collection.children.link(parts)
studio=bpy.data.collections.new('STUDIO - excluded from export');scene.collection.children.link(studio)
slots={};materials=[]
for page in ['01','04']:
    manifest=json.loads((A/f'Textures/Shared/Cozy/NP_CozyAtlasSlots{"_04" if page=="04" else ""}.json').read_text())
    for s in manifest['slots']:slots[s['name']]=(len(materials),s['uvSafe'])
    mat=bpy.data.materials.new('NP_CozySurfaces_'+page);mat.use_nodes=True
    n=mat.node_tree.nodes;l=mat.node_tree.links;bs=n.get('Principled BSDF');bs.inputs['Roughness'].default_value=.65
    for suffix in ['BaseColor','Normal','SurfaceMask']:
        im=bpy.data.images.load(str(A/f'Textures/Shared/Cozy/NP_CozySurfaces_{page}_{suffix}.png'));im.pack()
        if suffix!='BaseColor':im.colorspace_settings.name='Non-Color'
        tex=n.new('ShaderNodeTexImage');tex.image=im
        if suffix=='BaseColor':l.new(tex.outputs['Color'],bs.inputs['Base Color'])
        elif suffix=='Normal':
            normal=n.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.22;l.new(tex.outputs['Color'],normal.inputs['Color']);l.new(normal.outputs['Normal'],bs.inputs['Normal'])
        else:
            sep=n.new('ShaderNodeSeparateColor');l.new(tex.outputs['Color'],sep.inputs['Color']);l.new(sep.outputs['Red'],bs.inputs['Metallic'])
            inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(tex.outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],bs.inputs['Roughness'])
    materials.append(mat)
def select(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def move(o,col):
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o)
def finish(o,name,slot,bevel=0,segments=3,screen=False):
    o.name=name;move(o,parts);select(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft machined edges','BEVEL');mod.width=bevel;mod.segments=segments
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for f in o.data.polygons:f.use_smooth=True
    mod=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=50
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mi,r=slots[slot];o.data.materials.clear();o.data.materials.append(materials[mi]);o['AtlasSlot']=slot;o['EditablePart']=True
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='AtlasUV')
    vs=[v.co for v in o.data.vertices];lo=Vector([min(v[i] for v in vs) for i in range(3)]);hi=Vector([max(v[i] for v in vs) for i in range(3)])
    for f in o.data.polygons:
        axis=max(range(3),key=lambda i:abs(f.normal[i]));axes=([1,2],[0,2],[0,1])[axis]
        if screen:axes=[0,2]
        for li in f.loop_indices:
            v=vs[o.data.loops[li].vertex_index];u=(v[axes[0]]-lo[axes[0]])/max(hi[axes[0]]-lo[axes[0]],1e-6);w=(v[axes[1]]-lo[axes[1]])/max(hi[axes[1]]-lo[axes[1]],1e-6)
            uv.data[li].uv=(r[0]+u*(r[2]-r[0]),r[1]+w*(r[3]-r[1]))
    return o
def box(name,p,size,slot,bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=size;return finish(o,name,slot,bevel)
def cylinder(name,p,r,depth,slot,axis='Z',vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=p);o=bpy.context.object
    if axis=='Y':o.rotation_euler[0]=math.pi/2
    if axis=='X':o.rotation_euler[1]=math.pi/2
    return finish(o,name,slot,min(r*.13,.006),2)
def sphere(name,p,size,slot):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=p);o=bpy.context.object;o.scale=size;return finish(o,name,slot)
def tube(name,points,r,slot,cyclic=False):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=12;cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
    for b,p in zip(sp.bezier_points,points):b.co=p;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
    sp.use_cyclic_u=cyclic;o=bpy.data.objects.new(name,cu);parts.objects.link(o);select(o);bpy.ops.object.convert(target='MESH');return finish(o,name,slot)
def rounded(w,h,r,steps=6):
    pts=[]
    for cx,cz,a in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
        for j in range(steps+1):
            t=math.radians(a+j*90/steps);pts.append((cx+r*math.cos(t),cz+r*math.sin(t)))
    return pts
def bezel(name,x,y,z,w,h,r,thickness,depth,slot):
    outer=rounded(w,h,r);inner=rounded(w-thickness*2,h-thickness*2,max(.008,r-thickness));N=len(outer);verts=[]
    for yy in [y,y+depth]:
        for loop in [outer,inner]:verts.extend((x+xx,yy,z+zz) for xx,zz in loop)
    faces=[]
    for i in range(N):
        j=(i+1)%N
        faces.extend([(i,j,N+j,N+i),(2*N+i,3*N+i,3*N+j,2*N+j),(i,2*N+i,2*N+j,j),(N+i,N+j,3*N+j,3*N+i)])
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);parts.objects.link(o)
    select(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    return finish(o,name,slot,.006,2)
# Cabinet and feet: front is Blender -Y, Unity -Z. Origin at bottom centre.
box('01 - reinforced teal plinth',(0,0,.065),(.79,.61,.13),'EnamelTeal',.035)
for x in [-.325,.325]:
    for y in [-.235,.235]:box('02 - sage corner bumper',(x,y,.060),(.15,.17,.12),'EnamelSage',.022)
box('03 - cream cabinet shell',(0,.015,.49),(.655,.475,.78),'PaintCream',.032)
box('04 - control console sage shoulder',(0,-.016,.85),(.75,.56,.15),'EnamelSage',.037)
box('05 - upper monitor enclosure',(0,.045,1.14),(.75,.38,.62),'EnamelTeal',.060)
for x in [-.36,.36]:box('06 - ivory monitor side cheek',(x,.115,1.14),(.038,.29,.49),'PaintCream',.018)
bezel('07 - deep continuous monitor bezel',0,-.325,1.145,.745,.622,.085,.054,.165,'EnamelTeal')
# Flat graphic is recessed behind the bevel and framed on every side.
screen=box('08 - recessed illustrated soil display',(0,-.316,1.145),(.624,.013,.498),'Screen',.025)
finish(screen,screen.name,'Screen',screen=True)
# Carry handle on top, raised bridge and mounting shoes.
for x in [-.245,.245]:box('09 - top handle shoe',(x,.035,1.458),(.19,.19,.062),'EnamelSage',.018)
box('10 - top recessed carry bridge',(0,.038,1.476),(.39,.075,.046),'Rubber',.019)
# Lower cream panel.
box('11 - cream front console',(0,-.284,.689),(.70,.075,.305),'PaintCream',.030)
for x in [-.205,.048]:
    cylinder('12 - copper dial escutcheon',(x,-.335,.704),.101,.027,'Copper','Y',32)
    cylinder('13 - dark scalloped dial',(x,-.369,.704),.075,.055,'Rubber','Y',12)
    # Raised ivory pointer at the face, with a slight angle like reference.
    p=box('14 - ivory dial index',(x-.010,-.401,.742),(.012,.008,.041),'LabelCream',.005);p.rotation_euler[1]=-.20
cylinder('15 - indicator brass collar',(.244,-.335,.690),.043,.020,'Aluminium','Y')
sphere('16 - green status lamp',(.244,-.354,.690),(.032,.016,.032),'Signal')
for x in [-.296,.296]:
    for z in [.575,.804]:
        cylinder('17 - console copper fastener',(x,-.331,z),.018,.013,'Copper','Y',16)
box('18 - service panel dark gasket',(0,-.234,.316),(.492,.022,.306),'Rubber',.028)
box('19 - brushed aluminium access panel',(0,-.252,.316),(.47,.027,.285),'Aluminium',.025)
for x in [-.185,.185]:
    for z in [.215,.415]:cylinder('20 - service cover copper screw',(x,-.273,z),.018,.010,'Copper','Y',16)
# Side rails taper softly into cabinet, round upper corners are actual geometry.
for s in [-1,1]:
    tube('21 - bent brushed metal side guard',[(s*.408,-.19,.57),(s*.410,-.19,.89),(s*.410,-.145,.98),(s*.410,.12,1.01)],.026,'Aluminium')
    for z in [.59,.87]:box('22 - guard mounting shoe',(s*.35,-.16,z),(.058,.075,.06),'PaintCream',.012)
# Probe cradle mounted on right wall; U shaped opening leaves shaft visible.
box('23 - probe cradle mounting plate',(.367,.08,.67),(.085,.17,.30),'PaintCream',.016)
bezel('24 - open probe retaining collar',.492,-.035,.720,.208,.178,.047,.038,.157,'PaintCream')
# Actual shaft, depth collar, pointed tip and rectangular grip.
cylinder('25 - stainless probe barrel',(.495,.045,.64),.046,.51,'Aluminium','Z',24)
cylinder('26 - probe depth stop',(.495,.045,.486),.052,.025,'Aluminium')
bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=.008,radius2=.046,depth=.225,location=(.495,.045,.355));finish(bpy.context.object,'27 - tapered soil penetration tip','Aluminium',.004,2)
cylinder('28 - lower sage grip collar',(.495,.045,.94),.061,.052,'EnamelSage')
box('29 - rubber probe grip',(.495,.045,1.01),(.092,.10,.12),'Rubber',.022)
bezel('30 - D handle with open centre',.495,-.010,1.135,.225,.205,.062,.050,.103,'EnamelSage')
# Cable socket and thick flexible loop. Endpoints genuinely terminate at socket and shaft.
box('31 - copper side socket flange',(.337,-.072,.490),(.025,.15,.18),'Copper',.014)
cylinder('32 - aluminium cable strain relief',(.370,-.072,.490),.038,.062,'Aluminium','X')
for y in [-.125,-.020]:
    for z in [.425,.555]:sphere('33 - flange rivet',(.355,y,z),(.009,.013,.013),'Copper')
tube('34 - flexible probe cable',[(.40,-.072,.49),(.44,-.073,.455),(.472,-.073,.325),(.55,-.035,.295),(.57,.035,.39),(.55,.045,.515),(.495,.045,.545)],.019,'Rubber')
# Rear service detailing makes the station complete from the gameplay camera.
box('35 - back recessed maintenance hatch',(0,.262,.51),(.44,.012,.51),'EnamelTeal',.025)
for z in [.60,.64,.68,.72]:box('36 - rear cooling vent',(0,.274,z),(.28,.012,.014),'Rubber',.006)
box('37 - rear serial plate',(0,.276,.385),(.20,.008,.075),'Aluminium',.008)
for x in [-.167,.167]:
    for z in [.30,.72]:cylinder('38 - rear service screw',(x,.278,z),.012,.008,'Copper','Y',12)
# Export copies only. Master keeps every named component editable.
master=list(parts.objects)
def evaluated_objects(lod):
    copies=[]
    for original in master:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);select(o)
        if lod:
            d=o.modifiers.new('Distance simplification','DECIMATE');d.ratio=[1,.48,.22][lod];d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
        copies.append(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=copies[0];o.name='SoilScanner_Cozy_LOD'+str(lod)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    tri=o.modifiers.new('Export triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
    return o
lods=[]
for lod in range(3):
    o=evaluated_objects(lod);select(o)
    export_name=o.name;o.name='ScannerMesh_Distance'+str(lod)
    bpy.ops.export_scene.fbx(filepath=str(MODELS/(export_name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    o.name=export_name
    me=o.data;me.calc_loop_triangles();uv=me.uv_layers.active.data;norm=me.corner_normals
    vertices=[];normals=[];uvs=[];submeshes=[[],[]];lookup={}
    # Exchange Blender Y/Z and reverse winding: Unity front -Z with explicit metres.
    for f in me.polygons:
        indices=[]
        for li in f.loop_indices:
            p=me.vertices[me.loops[li].vertex_index].co;n=norm[li].vector;u=uv[li].uv
            key=tuple(round(float(v),7) for v in (*p,*n,*u))
            if key not in lookup:
                lookup[key]=len(vertices);vertices.append([p.x,p.z,p.y]);normals.append([n.x,n.z,n.y]);uvs.append(list(u))
            indices.append(lookup[key])
        mi=materials.index(o.data.materials[f.material_index]);submeshes[mi].extend([indices[0],indices[2],indices[1]])
    spec=dict(name=o.name,vertices=vertices,normals=normals,uv=uvs,submeshes=submeshes,materials=[m.name for m in materials],triangles=sum(len(i)//3 for i in submeshes),min=[min(p[i] for p in vertices) for i in range(3)],max=[max(p[i] for p in vertices) for i in range(3)])
    (MODELS/(o.name+'.json')).write_text(json.dumps(spec,separators=(',',':')));lods.append({k:spec[k] for k in ['name','triangles','min','max']})
    bpy.data.objects.remove(o,do_unlink=True)
(SOURCE/'SoilScanner_Manifest.json').write_text(json.dumps(dict(lods=lods,parts=len(master),unityFront='-Z',origin='ground centre',sharedMaterials=[m.name for m in materials],existingAtlasModified=False),indent=2))
# Neutral studio, saved with a readable material viewport and render camera.
def studio_mat(name,color,rough=1):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=rough;return m
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Studio ground';ground.data.materials.append(studio_mat('Studio ivory',(.80,.81,.78)));move(ground,studio);ground.location.z=-.007
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.65,-4.6,2.58));cam=bpy.context.object;cam.name='Review - front right';move(cam,studio);cam.data.type='ORTHO';cam.data.ortho_scale=1.96;aim(cam,(.055,0,.75));scene.camera=cam
for name,pos,power,size in [('Key',(-3,-4,6),420,4),('Fill',(4,-2,3),240,3),('Rim',(1,4,4),350,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,.7));move(o,studio)
scene.world=bpy.data.worlds.new('Neutral studio environment');scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.40,.43,.45,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.35
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.view_settings.view_transform='AgX';scene.render.resolution_x=960;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.region_3d.view_distance=2.6;a.spaces.active.region_3d.view_location=(0,0,.75);a.spaces.active.shading.type='MATERIAL'
select(screen);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'SoilScanner_Cozy_Master.blend'))
scene.render.filepath=str(OUT/'SoilScanner_FrontRight.png');bpy.ops.render.render(write_still=True)
cam.location=(-2.9,4.4,2.4);aim(cam,(.04,0,.75));scene.render.filepath=str(OUT/'SoilScanner_RearLeft.png');bpy.ops.render.render(write_still=True)
print('SCANNER_BLENDER_PASS',json.dumps(lods),flush=True)
