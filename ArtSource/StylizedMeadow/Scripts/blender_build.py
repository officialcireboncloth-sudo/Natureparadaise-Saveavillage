"""Blender 4.5: editable meadow kit, metric FBX exports, LODs and reference renders."""
import bpy, math, random, os, json
from mathutils import Vector
ROOT=os.path.abspath('ArtSource/StylizedMeadow')
OUT=os.path.abspath('Assets/Nature  Paradaise/Art/StylizedMeadow')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1

def mat(name,path):
    m=bpy.data.materials.new(name);m.use_nodes=True
    n=m.node_tree.nodes;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=.88
    tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(path);tex.image.pack()
    m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);return m
leafmat=mat('Meadow Leaf Palette',ROOT+'/Textures/LeafPalette_Albedo.png')
groundmat=mat('Meadow Grass Ground',ROOT+'/Textures/GrassGround_Albedo.png')
groundmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=1
library=bpy.data.collections.new('01 Source Meshes — metres');scene.collection.children.link(library)
lods=bpy.data.collections.new('02 LOD1 Meshes');scene.collection.children.link(lods)
preview=bpy.data.collections.new('03 Preview Meadow');scene.collection.children.link(preview)

class Builder:
    def __init__(self):self.v=[];self.f=[];self.uv=[]
    def leaf(self,pos,yaw,length,width,height,lane=0,segments=5,roundness=.85):
        start=len(self.v);d=Vector((math.cos(yaw),math.sin(yaw),0));side=Vector((-d.y,d.x,0));p=Vector(pos)
        for i in range(segments+1):
            t=i/segments;w=width*(math.sin(math.pi*t)**roundness)*.5
            # Arched broad leaf; a raised midrib gives soft dimensional form.
            center=p+d*(length*t)+Vector((0,0,height*math.sin(math.pi*t*.80)+.02*t))
            for j in range(3):
                q=center+side*((j-1)*w)+Vector((0,0,(1-abs(j-1))*width*.12*math.sin(math.pi*t)))
                self.v.append(q);self.uv.append(((lane+.12+j*.38)/8,.08+t*.84))
        for i in range(segments):
            for j in range(2):
                a=start+i*3+j;self.f.append((a,a+1,a+4,a+3))
    def stem(self,x,y,h,lane=6):
        start=len(self.v)
        for z in [0,h]:
            for i in range(6):
                a=i*math.tau/6;self.v.append((x+.012*math.cos(a),y+.012*math.sin(a),z));self.uv.append(((lane+.5)/8,.2+.5*z/max(.01,h)))
        for i in range(6):self.f.append((start+i,start+(i+1)%6,start+(i+1)%6+6,start+i+6))
    def bulb(self,p,r,lane=5,uv_height=.6):
        start=len(self.v)
        for j in range(5):
            t=j*math.pi/4
            for i in range(8):
                a=i*math.tau/8;self.v.append((p[0]+r*math.sin(t)*math.cos(a),p[1]+r*math.sin(t)*math.sin(a),p[2]+r*math.cos(t)*.65));self.uv.append(((lane+.5)/8,uv_height))
        for j in range(4):
            for i in range(8):self.f.append((start+j*8+i,start+j*8+(i+1)%8,start+(j+1)*8+(i+1)%8,start+(j+1)*8+i))
    def object(self,name,col):
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(self.v,[],self.f);mesh.update()
        uv=mesh.uv_layers.new(name='Leaf Palette UV')
        for face in mesh.polygons:
            face.use_smooth=True
            for loop in face.loop_indices:uv.data[loop].uv=self.uv[mesh.loops[loop].vertex_index]
        obj=bpy.data.objects.new(name,mesh);col.objects.link(obj);mesh.materials.append(leafmat);return obj

def grass(name,seed,count,length,height,lod=False):
    rng=random.Random(seed);b=Builder()
    for i in range(count):
        angle=i*math.tau/count+rng.uniform(-.24,.24);scale=rng.uniform(.72,1.12)
        b.leaf((rng.uniform(-.045,.045),rng.uniform(-.045,.045),.005),angle,length*scale,rng.uniform(.10,.17),height*scale,i%4,3 if lod else 5)
    return b.object(name,lods if lod else library)

def bush(name,seed,count,lod=False):
    rng=random.Random(seed);b=Builder()
    b.bulb((0,0,.245),.37,1,.35)
    b.stem(0,0,.50)
    # Low skirt anchors the canopy to the soil instead of leaving floating leaves.
    skirt=6 if lod else 12
    for i in range(skirt):
        a=i*math.tau/skirt;r=.27
        b.leaf((math.cos(a)*r,math.sin(a)*r,.018),a,.25,.18,.14,i%4,3 if lod else 5,.6)
    for i in range(count):
        # Golden-angle distribution forms a rounded canopy, not intersecting flat cards.
        a=i*2.399963;r=math.sqrt((i+.5)/count)*.48
        z=.10+.49*math.sqrt(max(0,1-(r/.53)**2))
        b.leaf((math.cos(a)*r,math.sin(a)*r,z),a+rng.uniform(-.4,.4),rng.uniform(.19,.31),rng.uniform(.14,.23),rng.uniform(.04,.09),i%4,3 if lod else 5,.6)
    return b.object(name,lods if lod else library)

def flowers(name,seed,yellow=False,lod=False):
    rng=random.Random(seed);b=Builder();leaf_count=7 if lod else 10
    for i in range(leaf_count):b.leaf((0,0,.006),i*math.tau/leaf_count,.25,.10,.13,i%4,3 if lod else 5)
    for i in range(3 if lod else 5):
        a=i*2.4;x=math.cos(a)*rng.uniform(.02,.13);y=math.sin(a)*rng.uniform(.02,.13);h=rng.uniform(.26,.45)
        b.stem(x,y,h)
        if yellow:
            for j in range(3):b.bulb((x+.025*math.cos(a+j*2),y+.025*math.sin(a+j*2),h+j*.04),.032,5)
        else:
            for j in range(5):b.leaf((x,y,h),j*math.tau/5,.09,.07,.012,4,3,.5)
            b.bulb((x,y,h+.012),.028,5)
    return b.object(name,lods if lod else library)

assets=[]
for name,seed,count,length,height in [('Grass_Rosette_A',21,12,.38,.24),('Grass_Rosette_B',42,16,.31,.18),('Grass_Rosette_C',83,9,.28,.29)]:
    assets.append((grass(name,seed,count,length,height),grass(name+'_Low',seed,max(5,count//2),length,height,True)))
for name,seed,count in [('Bush_Round_A',120,64),('Bush_Round_B',331,48)]:assets.append((bush(name,seed,count),bush(name+'_Low',seed,count//2,True)))
for name,seed,yellow in [('Flowers_White',431,False),('Flowers_Yellow',535,True)]:assets.append((flowers(name,seed,yellow),flowers(name+'_Low',seed,yellow,True)))

report=[]
for high,low in assets:
    for obj in (high,low):
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        # Bottom-centred pivot, identity transforms; FBX conversion handles Z-up -> Y-up.
        bpy.ops.export_scene.fbx(filepath=OUT+'/Models/'+obj.name+'.fbx',use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    report.append({'name':high.name,'lod0Triangles':sum(len(p.vertices)-2 for p in high.data.polygons),'lod1Triangles':sum(len(p.vertices)-2 for p in low.data.polygons),'dimensionsMetres':list(high.dimensions),'pivot':'bottom centre','materials':1})
    # Sources remain individually editable, organized on an asset shelf.
    index=len(report)-1;high.location=(index%4*1.7,index//4*1.8,0);low.location=high.location+Vector((0,-4,0))
    high.hide_render=True;low.hide_render=True

def relocate(obj,col):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    col.objects.link(obj)
bpy.ops.mesh.primitive_plane_add(size=12)
ground=bpy.context.object;ground.name='Preview Grass Ground — 18m texture repeat';relocate(ground,preview);ground.data.materials.append(groundmat)
for uv in ground.data.uv_layers.active.data:uv.uv*=12/18
soil=bpy.data.materials.new('Preview soil sides');soil.diffuse_color=(.48,.23,.07,1)
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,-.21));slab=bpy.context.object;slab.name='Preview Soil Slab';slab.dimensions=(12,12,.4);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);relocate(slab,preview);slab.data.materials.append(soil)
bevel=slab.modifiers.new('Soft edges','BEVEL');bevel.width=.12;bevel.segments=3
placement=random.Random(918)
for i in range(85):
    choice=placement.choices(range(7),weights=[24,22,20,3,3,12,10])[0];source=assets[choice][0]
    obj=bpy.data.objects.new(f'Preview_{source.name}_{i:02}',source.data);preview.objects.link(obj)
    obj.location=(placement.uniform(-5.6,5.6),placement.uniform(-5.6,5.6),.006)
    obj.rotation_euler.z=placement.random()*math.tau;s=placement.uniform(.72,1.15);obj.scale=(s,s,s)
world=bpy.data.worlds.new('Soft blue fill');scene.world=world;world.use_nodes=True;world.node_tree.nodes.get('Background').inputs[0].default_value=(.72,.80,.90,1);world.node_tree.nodes.get('Background').inputs[1].default_value=.75
bpy.ops.object.light_add(type='AREA',location=(-3,-5,9));light=bpy.context.object;light.name='Soft warm sun';light.data.energy=1350;light.data.shape='DISK';light.data.size=5;light.rotation_euler=(Vector((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(11,-16,17));cam=bpy.context.object;cam.name='Meadow Preview Camera';cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=18;scene.camera=cam
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.filepath=ROOT+'/Previews/MeadowKit_Blender.png'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/Blender/StylizedMeadow.blend')
open(ROOT+'/Previews/MeshManifest.json','w').write(json.dumps(report,indent=2))
bpy.ops.render.render(write_still=True)
print('MEADOW_BUILD_COMPLETE')

