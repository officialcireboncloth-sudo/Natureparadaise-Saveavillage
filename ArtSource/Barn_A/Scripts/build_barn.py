"""Blender 4.5: barn built in metres from the approved exterior A specification.

Run in a separate Blender process. Never opens or overwrites the user's unsaved
scene. Blender coordinates X/right Y/rear Z/up; front faces -Y. All export
meshes have a ground-centre origin and identity transforms. Runtime uses the
existing pack atlas; the packed PNG is only a Blender-readable PSD composite.
"""
import bpy, bmesh, json, math, random, hashlib, sys
from pathlib import Path
from mathutils import Vector, Matrix

ART=Path(__file__).resolve().parents[1]
ROOT=ART.parents[1]
SPEC_PATH=ART/'References/Barn_A_Approved.json'
SPEC=json.loads(SPEC_PATH.read_text(encoding='utf-8'))
PAL=SPEC['materials']['swatches']
ATLAS=ART/'References/TFP_Atlas_1A_BlenderPreview.png'
for folder in ('Blender','Exports','Previews'): (ART/folder).mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='Barn A - Editable Master Lv3'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='CYCLES';scene.cycles.samples=12
scene.cycles.use_denoising=True
scene.render.resolution_x=1280;scene.render.resolution_y=960;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.fps=30

def collection(name,parent=None):
    col=bpy.data.collections.new(name);(parent or scene.collection).children.link(col);return col
source=collection('01 EDITABLE - master dimensions Lv3')
walls=collection('Walls - boards 0.25 m',source)
roof=collection('Roof - ten rows per slope',source)
foundation=collection('Foundation - two stone courses',source)
joinery=collection('Timber - posts frames and X braces',source)
metal=collection('Hardware and lantern',source)
exports=collection('02 EXPORT MESHES - identity transforms')
markers=collection('03 UNITY MARKERS - not render geometry')
studio=collection('04 PREVIEW STUDIO - never export')
exports.hide_render=True;exports.hide_viewport=True
markers.hide_render=True

image=bpy.data.images.load(str(ATLAS));image.name='TFP_Atlas_1A - existing PSD composite';image.pack()
image['UnityAtlasGUID']=SPEC['materials']['atlasGuid']
image['SourcePSD']=SPEC['materials']['atlasPath']

def material(name,glow=False):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    mat.diffuse_color=(.7,.7,.7,1)
    nodes=mat.node_tree.nodes;bsdf=nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value=1
    bsdf.inputs['Specular IOR Level'].default_value=0
    tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Linear';tex.extension='EXTEND'
    tex.label='Existing Toon Farm Pack atlas - no new runtime texture'
    mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    if glow:
        mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value=.7
    mat['UnitySharedMaterial']=name+'.mat'
    mat['UnityAtlasGUID']=SPEC['materials']['atlasGuid']
    return mat
opaque=material('TFP_Atlas_1A');emission=material('TFP_Atlas_Lights_1A',True)

def convert(p):return (p[0],p[2],p[1])
class MeshBuilder:
    def __init__(self):self.v=[];self.faces=[];self.uv=[];self.mats=[];self.groups=[];self.expected_normal=None
    def face(self,verts,color,variation=0,material_index=0,normal=None):
        expected=normal or self.expected_normal
        if expected:
            cross=(Vector(verts[1])-Vector(verts[0])).cross(Vector(verts[2])-Vector(verts[0]))
            if cross.dot(Vector(expected))<0:verts=list(reversed(verts))
        # Unity-to-Blender is a reflection: reverse the vertex winding.
        offset=len(self.v);self.v.extend(convert(p) for p in verts)
        self.faces.append(tuple(reversed(range(offset,offset+len(verts)))))
        u0,v0,u1,v1=PAL[color]['uvSafeRectBottomLeft']
        u=(u0+u1)/2+variation*(u1-u0)*.10
        v=(v0+v1)/2+variation*(v1-v0)*.14
        # Sample a small safe island. Never map a face across unrelated cells.
        self.uv.append([(u,v) for _ in verts]);self.mats.append(material_index)
    def quad(self,a,b,c,d,color,variation=0):self.face([a,b,c,d],color,variation)
    def box(self,center,size,color,back_axis=None,bevel=0):
        cx,cy,cz=center;sx,sy,sz=[v/2 for v in size]
        patterns=[[(1,-1,-1),(1,1,-1),(1,1,1),(1,-1,1)],
          [(-1,-1,1),(-1,1,1),(-1,1,-1),(-1,-1,-1)],
          [(-1,1,-1),(-1,1,1),(1,1,1),(1,1,-1)],
          [(-1,-1,-1),(1,-1,-1),(1,-1,1),(-1,-1,1)],
          [(1,-1,1),(1,1,1),(-1,1,1),(-1,-1,1)],
          [(-1,-1,-1),(-1,1,-1),(1,1,-1),(1,-1,-1)]]
        for n,pat in enumerate(patterns):
            if n==back_axis:continue
            self.face([(cx+x*sx,cy+y*sy,cz+z*sz) for x,y,z in pat],color)
    def object(self,name,col):
        mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(self.v,[],self.faces);mesh.update()
        mesh.materials.append(opaque);mesh.materials.append(emission)
        uv=mesh.uv_layers.new(name='ToonAtlasUV')
        for n,poly in enumerate(mesh.polygons):
            poly.material_index=self.mats[n]
            for k,loop in enumerate(poly.loop_indices):uv.data[loop].uv=self.uv[n][k]
        obj=bpy.data.objects.new(name,mesh);col.objects.link(obj)
        obj['AtlasGUID']=SPEC['materials']['atlasGuid'];obj['Units']='metres'
        return obj

def normals(obj):
    # Components include open external panels, so preserve their authored
    # winding. Closed solids may be recalculated safely.
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    if all(e.is_manifold for e in bm.edges):bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data);bm.free()

def mesh_object(name,col,builder,bevel=0):
    obj=builder.object(name,col)
    normals(obj)
    if bevel>0:
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        mod=obj.modifiers.new('One segment edge softness','BEVEL');mod.width=bevel;mod.segments=1
        mod.affect='EDGES';mod.limit_method='ANGLE';mod.angle_limit=.65
        bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False)
        # New bevel faces inherit material but UV is explicitly put back in
        # the same safe palette cell; no accidentally stretched texture.
        color=obj.get('Palette')
    return obj

def reset_uv(obj,color):
    u0,v0,u1,v1=PAL[color]['uvSafeRectBottomLeft']
    uv=obj.data.uv_layers.active
    if uv is None:uv=obj.data.uv_layers.new(name='ToonAtlasUV')
    for loop in uv.data:loop.uv=((u0+u1)/2,(v0+v1)/2)
    obj['Palette']=color

def beam_object(part,col,lod):
    a=Vector(convert(part['fromXYZ']));b=Vector(convert(part['toXYZ']));direction=(b-a).normalized()
    depth=Vector((0,1,0)) if abs(part['toXYZ'][2]-part['fromXYZ'][2])<1e-8 else Vector((1,0,0))
    width=direction.cross(depth).normalized()
    verts=[q+width*sw*part['width']/2+depth*sd*part['depth']/2 for q in (a,b) for sw in (-1,1) for sd in (-1,1)]
    builder=MeshBuilder()
    for indices in [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]:
        builder.face([(verts[k][0],verts[k][2],verts[k][1]) for k in indices],part['material'])
    obj=mesh_object(part['name'],col,builder)
    # Bevel only the large, exposed door diagonals, not the tiny brackets.
    if lod==0 and part['name'].startswith('DoorX_'):
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        mod=obj.modifiers.new('Soft X brace edges','BEVEL');mod.width=.015;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name);obj.select_set(False);reset_uv(obj,part['material'])
    return obj

def planar_part(part,col):
    # Distant LOD trims retain the same visible face placement and dimensions.
    builder=MeshBuilder();c=part['centerXYZ'];s=part['sizeXYZ'];name=part['name']
    if 'Side' in name or name.startswith('Left'):
        x=c[0]+math.copysign(s[0]/2,c[0])
        builder.expected_normal=(math.copysign(1,c[0]),0,0)
        builder.quad((x,c[1]-s[1]/2,c[2]-s[2]/2),(x,c[1]+s[1]/2,c[2]-s[2]/2),
                     (x,c[1]+s[1]/2,c[2]+s[2]/2),(x,c[1]-s[1]/2,c[2]+s[2]/2),part['material'])
    else:
        z=c[2]+math.copysign(s[2]/2,c[2])
        builder.expected_normal=(0,0,math.copysign(1,c[2]))
        builder.face([(c[0]-s[0]/2,c[1]-s[1]/2,z),(c[0]+s[0]/2,c[1]-s[1]/2,z),
                      (c[0]+s[0]/2,c[1]+s[1]/2,z),(c[0]-s[0]/2,c[1]+s[1]/2,z)],part['material'],material_index=1 if part['material']=='glow' else 0)
    return mesh_object(part['name'],col,builder)

def lantern_frame(part,col):
    b=MeshBuilder();cx,cy,cz=part['centerXYZ']
    def ring(y,rx,rz):return [(cx+rx*math.cos(i*math.tau/8),cy+y,cz+rz*math.sin(i*math.tau/8)) for i in range(8)]
    # Eight-sided upper hood and base, with an open cage between them.
    bottom=ring(-.24,.115,.07);base=ring(-.19,.17,.11)
    shoulder=ring(.16,.16,.10);top=ring(.24,.07,.045)
    b.face(bottom[::-1],'metal',normal=(0,-1,0));b.face(base,'metal',normal=(0,1,0))
    b.face(shoulder[::-1],'metal',normal=(0,-1,0));b.face(top,'metal',normal=(0,1,0))
    for a,c in ((bottom,base),(shoulder,top)):
        for i in range(8):b.face([a[i],a[(i+1)%8],c[(i+1)%8],c[i]],'metal')
    for x,z in [(-.145,-.065),(.145,-.065),(-.145,.065),(.145,.065)]:
        b.box((cx+x,cy-.015,cz+z),(.022,.35,.022),'metal')
    obj=mesh_object(part['name'],col,b);reset_uv(obj,'metal');return obj

def timber_and_hardware(lod,col):
    objects=[]
    for part in SPEC['parts']:
        name=part['name'];kind=part['type']
        if name in ('WallBody','Foundation') or kind=='surface':continue
        if lod==2 and (name.startswith(('MainHinge','MainHandle','SideHinge','EaveBrace','RearVentLouver')) or name=='LanternBracket'):continue
        target=metal if part['material'] in ('metal','glow') else joinery
        if col is not source:target=col
        if name=='LanternFrame' and lod==0:
            obj=lantern_frame(part,target)
        elif kind=='beam':
            obj=beam_object(part,target,lod)
        elif lod==2 and not name.startswith(('CornerPost','EaveFascia','RidgeCap')):
            obj=planar_part(part,target)
        else:
            builder=MeshBuilder()
            builder.box(part['centerXYZ'],part['sizeXYZ'],part['material'])
            big_trim=name.startswith(('CornerPost','MainDoorJamb','MainDoorLintel','LoftFrame','LoftRail','SideJamb','SideLintel','RidgeCap'))
            obj=mesh_object(name,target,builder,.025 if lod==0 and big_trim else 0)
            reset_uv(obj,part['material'])
            if part['material']=='glow':
                for poly in obj.data.polygons:poly.material_index=1
        obj['ApprovedPart']=name
        obj['ApprovedSource']=SPEC['revision']
        objects.append(obj)
    return objects

def wall_panels(lod,col):
    objects=[];pitch=.25
    for view in ('Front','Rear','Right','Left'):
        b=MeshBuilder();front=view in ('Front','Rear');half=4 if front else 5
        normal=-1 if view in ('Front','Left') else 1
        b.expected_normal=(0,0,normal) if front else (normal,0,0)
        def q(u,y,inset=0):
            return (u,y,normal*(5+inset)) if front else (normal*(4+inset),y,u)
        # Continuous dark substrate; boards are one low-poly outer face each.
        b.quad(q(-half,.5,-.008),q(half,.5,-.008),q(half,4.2,-.008),q(-half,4.2,-.008),'red_dark')
        if front:b.face([q(-4,4.2,-.008),q(4,4.2,-.008),q(0,6.6,-.008)],'red_dark')
        if lod==2:
            b.quad(q(-half,.5,-.002),q(half,.5,-.002),q(half,4.2,-.002),q(-half,4.2,-.002),'red')
            if front:b.face([q(-4,4.2,-.002),q(4,4.2,-.002),q(0,6.6,-.002)],'red')
        else:
            for k in range(round(half*2/pitch)):
                u0=-half+k*pitch+.004;u1=u0+pitch-.008
                upper0=4.2+(2.4*(1-abs(u0)/4) if front else 0)
                upper1=4.2+(2.4*(1-abs(u1)/4) if front else 0)
                # Board faces sit 2 mm inside the nominal facade. Corner-post
                # faces stay at their approved coordinates, without z-fighting.
                b.quad(q(u0,.5,-.002),q(u1,.5,-.002),q(u1,upper1,-.002),q(u0,upper0,-.002),'red',((k*17)%7-3)/3)
        obj=mesh_object('WallBoards_'+view,col,b);obj['BoardPitchMetres']=pitch;objects.append(obj)
    return objects

def roof_planes(lod,col):
    objects=[]
    for side in (-1,1):
        b=MeshBuilder();name='RoofRight' if side>0 else 'RoofLeft'
        def surface(x,z,lip=0):return (side*x,6.6-.6*x+lip,z)
        # Roof thickness is normal to the plane and points inward, preserving
        # the approved 8.9 x 10.9 outer dimensions.
        delta=(side*.12*.6/math.sqrt(1.36),.12/math.sqrt(1.36),0)
        # The simplified roof needs the same clearance above the fascia as
        # the detailed shingles; otherwise their coplanar faces shimmer.
        lip=.003 if lod==2 else 0
        top=[surface(0,-5.45,lip),surface(4.45,-5.45,lip),surface(4.45,5.45,lip),surface(0,5.45,lip)]
        bottom=[tuple(p[i]-delta[i] for i in range(3)) for p in top]
        b.face(bottom[::-1],'roof_dark',normal=(-side*.6,-1,0))
        edge_normals=[(0,0,-1),(side,0,0),(0,0,1)]
        for i in (0,1,2):b.face([top[i],bottom[i],bottom[(i+1)%4],top[(i+1)%4]],'roof',normal=edge_normals[i])
        b.face(top,'roof_dark' if lod<2 else 'roof',normal=(side*.6,1,0))
        b.expected_normal=(side*.6,1,0)
        if lod<2:
            rows,cols=(10,16) if lod==0 else (6,8)
            for row in range(rows):
                x0=4.45*row/rows;x1=4.45*(row+1)/rows
                gap=.018 if lod==0 else .024
                for column in range(cols):
                    z0=-5.45+10.9*column/cols;z1=-5.45+10.9*(column+1)/cols
                    if column:z0+=gap/2
                    if column<cols-1:z1-=gap/2
                    b.quad(surface(x0+.003,z0,.003),surface(x1-.004,z0,.025),
                           surface(x1-.004,z1,.025),surface(x0+.003,z1,.003),'roof',((column+row*3)%5-2)/2)
                # One continuous lip per row, not sixteen solid tile boxes.
                b.quad(surface(x1-.004,-5.45,.025),surface(x1-.004,-5.45,.002),
                       surface(x1-.004,5.45,.002),surface(x1-.004,5.45,.025),'roof')
        obj=mesh_object(name,col,b);obj['RowsPerSlope']=10 if lod==0 else 6 if lod==1 else 0
        obj['ColumnsAlongDepth']=16 if lod==0 else 8 if lod==1 else 0;objects.append(obj)
    return objects

def stone_foundation(lod,col):
    b=MeshBuilder();b.box((0,.25,0),(8,.5,10),'stone')
    if lod<2:
        for view in ('Front','Rear','Right','Left'):
            front=view in ('Front','Rear');half=4 if front else 5
            normal=-1 if view in ('Front','Left') else 1
            b.expected_normal=(0,0,normal) if front else (normal,0,0)
            def p(u,y,out=0):return (u,y,normal*(5+out)) if front else (normal*(4+out),y,u)
            # Fewer broad stones read clearly at game camera distance; all
            # nominal widths .55-.90 m, two courses, no sculpt/subdivision.
            rng=random.Random(120+len(view))
            for row in range(2):
                # Stagger interior joints without creating a tiny last stone.
                # Widths stay inside the approved .55-.90 m range.
                count=10 if front else 12
                joints=[-half]+[-half+i*(half*2/count)+rng.uniform(-.025,.025) for i in range(1,count)]+[half]
                for u,end in zip(joints[:-1],joints[1:]):
                    span=end-u;assert .55<=span<=.9
                    x0=u+.007;x1=u+span-.007;y0=row*.25+.009;y1=(row+1)*.25-.009
                    if x1<=x0:break
                    color='stone_light' if rng.random()<.22 else 'stone'
                    if lod==1:b.quad(p(x0,y0,.014),p(x1,y0,.014),p(x1,y1,.014),p(x0,y1,.014),color)
                    else:
                        bevel=min(.03,(x1-x0)*.15);out=.025+rng.uniform(-.003,.003)
                        outer=[(x0+bevel,y0),(x1-bevel,y0),(x1,y0+bevel),(x1,y1-bevel),
                               (x1-bevel,y1),(x0+bevel,y1),(x0,y1-bevel),(x0,y0+bevel)]
                        inner=[(x0+bevel,y0+bevel),(x1-bevel,y0+bevel),(x1-bevel,y1-bevel),(x0+bevel,y1-bevel)]
                        b.face([p(x,y,out) for x,y in inner],color)
                        for edge,(i,j,k,l) in enumerate([(0,1,1,0),(2,3,2,1),(4,5,3,2),(6,7,0,3)]):
                            b.face([p(*outer[i],.002),p(*outer[j],.002),p(*inner[k],out),p(*inner[l],out)],color)
                        for i,k in [(1,1),(3,2),(5,3),(7,0)]:
                            b.face([p(*outer[i],.002),p(*outer[(i+1)%8],.002),p(*inner[k],out)],color)
    return [mesh_object('Foundation_TwoStoneCourses',col,b)]

def join_intersecting_timbers(objects,lod,col):
    if lod==2:return objects
    # Exact unions remove coincident joint faces. Original timber components
    # remain editable operands; the visible joint uses live Boolean modifiers.
    groups=[('MainDoorFrame',lambda n:n.startswith(('MainDoorJamb','MainDoorLintel'))),
            ('LoftFrameJoint',lambda n:n.startswith(('LoftFrame','LoftRail'))),
            ('SideDoorFrame',lambda n:n.startswith(('SideJamb','SideLintel'))),
            ('RearVentFrame',lambda n:n.startswith(('RearVentJamb','RearVentRail'))),
            ('DoorCrossLeft',lambda n:n.startswith('DoorX_-1_')),
            ('DoorCrossRight',lambda n:n.startswith('DoorX_1_')),
            ('LeftWindowFrontFrame',lambda n:n.startswith(('LeftWindowJamb_-2.5','LeftWindowRail_-2.5'))),
            ('LeftWindowRearFrame',lambda n:n.startswith(('LeftWindowJamb_2.5','LeftWindowRail_2.5')))]
    result=list(objects)
    for name,match in groups:
        parts=[obj for obj in objects if match(obj.name)]
        if len(parts)<2:continue
        operands=collection('Joint operands - '+name,col)
        for obj in parts[1:]:operands.objects.link(obj)
        target=joinery if col is source else col
        visible=bpy.data.objects.new(name+'_CleanJoint',parts[0].data.copy());target.objects.link(visible)
        visible['EditableOperands']=[obj.name for obj in parts]
        visible['JointMethod']='Live exact Boolean union; operands retained for edit'
        mod=visible.modifiers.new('Clean timber joint - no coincident faces','BOOLEAN')
        mod.operation='UNION';mod.solver='EXACT';mod.operand_type='COLLECTION';mod.collection=operands
        simplify=visible.modifiers.new('Dissolve redundant coplanar joint edges','DECIMATE')
        simplify.decimate_type='DISSOLVE';simplify.angle_limit=.001;simplify.delimit={'UV','MATERIAL'}
        for obj in parts:obj.hide_render=True;obj.hide_set(True)
        result.append(visible)
    return result

def build(lod,col):
    objs=wall_panels(lod,walls if lod==0 else col)
    objs+=roof_planes(lod,roof if lod==0 else col)
    objs+=stone_foundation(lod,foundation if lod==0 else col)
    objs+=timber_and_hardware(lod,col)
    return join_intersecting_timbers(objs,lod,col)

def join_copy(objects,name):
    # Source collection remains independently editable. Export is one mesh
    # renderer, with only opaque and lantern core material slots.
    copies=[]
    bpy.context.view_layer.update();depsgraph=bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if obj.hide_render:continue
        evaluated=obj.evaluated_get(depsgraph)
        clone=bpy.data.objects.new(name+'_part',bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=depsgraph))
        exports.objects.link(clone);copies.append(clone)
    bpy.ops.object.select_all(action='DESELECT')
    exports.hide_viewport=False
    for obj in copies:obj.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
    result=bpy.context.object;result.name=name
    bm=bmesh.new();bm.from_mesh(result.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(result.data);bm.free();result.data.update()
    # Slot deduplication performed by join; the exporter sees two shared names.
    result.location=(0,0,0);result.rotation_euler=(0,0,0);result.scale=(1,1,1)
    result['GroundCentrePivot']=True;result['ApprovedRevision']=SPEC['revision']
    return result

source_objects=build(0,source)
lod_meshes=[join_copy(source_objects,'Barn_A_Master_LOD0')]
for lod in (1,2):
    temp=collection('TEMP LOD build '+str(lod))
    pieces=build(lod,temp);lod_meshes.append(join_copy(pieces,'Barn_A_Master_LOD'+str(lod)))
    for obj in pieces:bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(temp)

def tri_count(obj):return sum(len(p.vertices)-2 for p in obj.data.polygons)
def bounds(obj):
    positions=[obj.matrix_world@v.co for v in obj.data.vertices]
    return ([min(v[i] for v in positions) for i in range(3)],[max(v[i] for v in positions) for i in range(3)])

audit=[]
for index,obj in enumerate(lod_meshes):
    lo,hi=bounds(obj)
    assert all(lo[i]>=[-4.45,-5.45,0][i]-1e-5 and hi[i]<=[4.45,5.45,6.8][i]+1e-5 for i in range(3)),(obj.name,lo,hi)
    if index==0:assert tri_count(obj)<=4500,tri_count(obj)
    assert len(obj.data.materials)<=2,len(obj.data.materials)
    assert all(math.isfinite(v) for point in (lo,hi) for v in point)
    # Every UV is inside one of the selected safe swatches.
    rects=[s['uvSafeRectBottomLeft'] for s in PAL.values()]
    for loop in obj.data.uv_layers.active.data:
        u,v=loop.uv;assert any(a-1e-7<=u<=c+1e-7 and b-1e-7<=v<=d+1e-7 for a,b,c,d in rects),(obj.name,u,v)
    bpy.ops.object.select_all(action='DESELECT');obj.hide_set(False);obj.select_set(True);bpy.context.view_layer.objects.active=obj
    filename=obj.name
    original_mesh_name=obj.data.name
    # Separate files do not contain the entire native Unity _LOD0/_LOD1 chain.
    # Neutral node names avoid Unity treating each standalone FBX as a broken
    # automatic LOD group. The prefab explicitly assigns the three meshes.
    obj.name=('Barn_A_High','Barn_A_Medium','Barn_A_Low')[lod]
    obj.data.name=obj.name+'Mesh'
    bpy.ops.export_scene.fbx(filepath=str(ART/'Exports'/f'{filename}.fbx'),use_selection=True,
        object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',
        add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
    obj.name=filename;obj.data.name=original_mesh_name
    audit.append({'name':obj.name,'triangles':tri_count(obj),'vertices':len(obj.data.vertices),
                  'boundsBlenderXYZ':[lo,hi],'materials':[m.name for m in obj.data.materials],
                  'identityTransforms':True,'groundCentreOrigin':[0,0,0],
                  'export':'Exports/'+obj.name+'.fbx'})
exports.hide_viewport=True

# Editable markers, excluded from export mesh geometry.
for name,pos in [('GroundCentre',(0,0,0)),('EntranceMarker',(0,0,-5.9))]:
    obj=bpy.data.objects.new(name,None);markers.objects.link(obj);obj.location=convert(pos);obj.empty_display_type='PLAIN_AXES';obj.empty_display_size=.4
    obj['UnityXYZ']=list(pos)
    obj['Purpose']='Placement reference only; not export geometry'

# Studio objects render only; no game assets or map modification here.
world=bpy.data.worlds.new('Barn studio ambient');scene.world=world;world.use_nodes=True
world.node_tree.nodes.get('Background').inputs[0].default_value=(.78,.82,.88,1)
world.node_tree.nodes.get('Background').inputs[1].default_value=.7
def move_to(obj,col):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    col.objects.link(obj)
def light(name,pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos);obj=bpy.context.object;obj.name=name
    move_to(obj,studio);obj.data.energy=power;obj.data.shape='DISK';obj.data.size=size
    obj.rotation_euler=(Vector((0,0,2.5))-obj.location).to_track_quat('-Z','Y').to_euler();return obj
light('Preview key - soft daylight',(-8,-10,14),1800,8)
light('Preview fill',(9,-1,10),950,7)
light('Preview rim',(0,8,12),1200,6)
floor_mat=bpy.data.materials.new('PREVIEW ONLY - neutral floor');floor_mat.use_nodes=True
floor_bsdf=floor_mat.node_tree.nodes.get('Principled BSDF');floor_bsdf.inputs['Base Color'].default_value=(.78,.75,.66,1)
floor_bsdf.inputs['Roughness'].default_value=1
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object;floor.name='Preview floor - never export';move_to(floor,studio);floor.data.materials.append(floor_mat)
bpy.ops.object.camera_add(location=(15,-20,15));camera=bpy.context.object;camera.name='Barn Preview Camera';move_to(camera,studio)
camera.data.type='ORTHO';camera.data.ortho_scale=19;scene.camera=camera
def aim(pos,target=(0,0,2.8),scale=19):
    camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
aim((15,-20,15))
scene['ApprovedSpecification']=SPEC['revision'];scene['MasterLevel']=3
scene['RuntimeTexture']='Existing Toon Farm Pack PSD GUID '+SPEC['materials']['atlasGuid']
scene['LevelScales']='0.75, 0.875, 1, 1.125, 1.25 - uniform XYZ, same shared mesh'

# Shared mesh instances for Lv1-5. No duplicated geometry or changed details.
levels_scene=bpy.data.scenes.new('Barn A - Levels 1 to 5 - shared mesh')
levels_scene.world=world;levels_scene.unit_settings.system='METRIC';levels_scene.unit_settings.scale_length=1
levels_scene.render.engine='CYCLES';levels_scene.cycles.samples=12;levels_scene.cycles.use_denoising=True
levels_scene.view_settings.view_transform='Standard';levels_scene.view_settings.look='None'
levels_scene.render.resolution_x=2000;levels_scene.render.resolution_y=700;levels_scene.render.resolution_percentage=100
levels_scene.render.image_settings.file_format='PNG'
comparison=bpy.data.collections.new('Level instances - uniform size only');levels_scene.collection.children.link(comparison)
label_mat=bpy.data.materials.new('PREVIEW ONLY - level labels');label_mat.use_nodes=True
label_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.08,.055,.035,1)
label_mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1
for lv in SPEC['levels']:
    root=bpy.data.objects.new('Barn_A_Lv'+str(lv['level']),None);comparison.objects.link(root)
    root.location=((lv['level']-3)*15,0,0);s=lv['uniformScale'];root.scale=(s,s,s)
    root['BodyWidthDepthMetres']=[8*s,10*s];root['VisualWidthDepthHeightMetres']=[8.9*s,10.9*s,6.8*s]
    obj=bpy.data.objects.new('Lv'+str(lv['level'])+' - shared master mesh',lod_meshes[0].data);comparison.objects.link(obj);obj.parent=root
    entrance=bpy.data.objects.new('Lv'+str(lv['level'])+' Entrance',None);comparison.objects.link(entrance);entrance.parent=root;entrance.location=(0,-5.9,0)
    entrance.empty_display_type='PLAIN_AXES';entrance.empty_display_size=.4
    text=bpy.data.curves.new('Lv'+str(lv['level'])+' label','FONT');text.body=f"Lv{lv['level']}  {8*s:g} x {10*s:g} m";text.align_x='CENTER';text.size=.8
    label=bpy.data.objects.new(text.name,text);comparison.objects.link(label);label.location=(root.location.x,-8,.01)
    text.materials.append(label_mat)
for obj in studio.objects:
    if obj.type!='CAMERA':levels_scene.collection.objects.link(obj)
cam_data=camera.data.copy();level_cam=bpy.data.objects.new('Level comparison camera',cam_data);levels_scene.collection.objects.link(level_cam)
level_cam.location=(0,-46,35);level_cam.rotation_euler=(Vector((0,0,2.5))-level_cam.location).to_track_quat('-Z','Y').to_euler()
level_cam.data.ortho_scale=78;levels_scene.camera=level_cam

# Store usable camera view for the opened file.
bpy.context.window.scene=scene
bpy.ops.object.select_all(action='DESELECT')
for obj in source_objects:
    if not obj.hide_render:obj.select_set(True)
bpy.context.view_layer.objects.active=source_objects[0]
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.overlay.show_overlays=False
            area.spaces.active.show_region_ui=False

report={'approvedRevision':SPEC['revision'],'blenderVersion':bpy.app.version_string,
        'status':'MODEL_BUILT_EXPORTED_NOT_APPLIED_TO_UNITY',
        'masterBodyWidthDepthMetres':[8,10],'masterVisualWidthDepthHeightMetres':[8.9,10.9,6.8],
        'lodMeshes':audit,'levels':SPEC['levels'],'levelMeshesShared':True,
        'materials':{'opaque':'TFP_Atlas_1A.mat','emission':'TFP_Atlas_Lights_1A.mat','atlasGUID':SPEC['materials']['atlasGuid'],
                     'newRuntimeTextures':0,'blenderPreviewImagePacked':True},
        'editableSourceObjects':len(source_objects),'unityMapModified':False,
        'notes':['Blender material is a matte preview; game rendering reuses existing CustomToon material.',
                 'Source objects retained; FBX meshes joined and triangulated. No preview floor, light or text in FBX.',
                 'Roof shingles are subdivided surfaces, not hundreds of solid tile cubes.',
                 'Side door and shutters remain exterior decoration; main door marker is separate.']}
(ART/'Previews/MeshManifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Barn_A_Master.blend'))
render_views=[] if '--skip-renders' in sys.argv else [('Barn_A_FrontRight',(15,-20,15),(0,0,2.8),19),
                              ('Barn_A_RearLeft',(-15,20,15),(0,0,2.8),19),
                              ('Barn_A_Front',(0,-25,3.5),(0,0,3.4),14),
                              ('Barn_A_Right',(25,0,3.5),(0,0,3.4),15),
                              ('Barn_A_Rear',(0,25,3.5),(0,0,3.4),14),
                              ('Barn_A_Left',(-25,0,3.5),(0,0,3.4),15)]
for name,pos,target,scale in render_views:
    aim(pos,target,scale);scene.render.filepath=str(ART/'Previews'/f'{name}.png');bpy.ops.render.render(write_still=True)
if render_views:
    levels_scene.render.filepath=str(ART/'Previews/Barn_A_Levels_1_to_5.png')
    bpy.ops.render.render(write_still=True,scene=levels_scene.name)
aim((15,-20,15));scene.render.filepath=str(ART/'Previews/Barn_A_FrontRight.png')
# No orphan intermediate mesh datablocks in the deliverable.
for mesh in list(bpy.data.meshes):
    if mesh.users==0:bpy.data.meshes.remove(mesh)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Blender/Barn_A_Master.blend'))
print('BARN_BUILD_COMPLETE '+json.dumps(audit))
