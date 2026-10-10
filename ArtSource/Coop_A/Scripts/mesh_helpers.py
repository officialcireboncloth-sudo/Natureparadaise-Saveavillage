"""Blender 4.5: coop built in metres from the approved exterior A specification.

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
SPEC_PATH=ART/'References/Coop_A_Approved.json'
SPEC=json.loads(SPEC_PATH.read_text(encoding='utf-8'))
PAL=SPEC['materials']['swatches']
ATLAS=ART/'References/TFP_Atlas_1A_BlenderPreview.png'
for folder in ('Blender','Exports','Previews'): (ART/folder).mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='Coop A - Editable Master Lv3'
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
roof=collection('Roof - eight rows per slope',source)
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

