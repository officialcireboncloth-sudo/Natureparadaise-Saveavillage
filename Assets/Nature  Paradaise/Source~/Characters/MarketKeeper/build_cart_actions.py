"""Editable sparse cart actions in the real keeper file; export baked copies only for Unity."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'; SRC=Path(__file__).parent
OUT=A/'mesh/Character/MarketKeeper'; REVIEW=ROOT/'ArtSource/MarketDeparture'
spec=json.loads((A/'Source~/MarketCart/MarketCart_Manifest.json').read_text())
keeper_z=spec['keeperAnchor'][2];grip_position=spec['gripAnchors'][1]
REVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC/'MarketKeeper.blend'))
sc=bpy.context.scene; sc.render.fps=30
rig=bpy.data.objects['MarketKeeper_Rig']; body=bpy.data.objects['MarketKeeper_Body']
rig.animation_data_create(); rig.animation_data.action=None
for action in list(bpy.data.actions):
    if action.name in ('Cart_Grip','Cart_Pull','Cart_Push'): bpy.data.actions.remove(action)
for ob in list(bpy.data.objects):
    if ob.get('CartReference'): bpy.data.objects.remove(ob,do_unlink=True)
rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
length={b.name:b.length for b in rig.data.bones}
def pb(n):return rig.pose.bones['mixamorig:'+n]
def update():bpy.context.view_layer.update()
def reset():
    for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4);b.rotation_mode='QUATERNION'
    update()
def orient(name,rotation):
    b=pb(name); m=rotation.to_matrix().to_4x4();m.translation=b.head.copy();b.matrix=m;update()
def aim(name,point):
    b=pb(name);q=(b.tail-b.head).rotation_difference(Vector(point)-b.head)
    orient(name,q@b.matrix.to_quaternion())
def bend_chain(upper,lower,end,target,pole):
    a=pb(upper).head.copy();t=Vector(target);d=t-a
    l1=length[pb(upper).name];l2=length[pb(lower).name]
    if upper.endswith('Arm'):assert d.length < l1+l2+.002,(upper,'unreachable wrist',d.length,l1+l2)
    distance=min(d.length,l1+l2-.001); direction=d.normalized()
    pole=Vector(pole); side=(pole-direction*pole.dot(direction)).normalized()
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    joint=a+direction*along+side*math.sqrt(max(.000001,l1*l1-along*along))
    aim(upper,joint);aim(lower,t)
def foot(side,target):
    bend_chain(side+'UpLeg',side+'Leg',side+'Foot',target,(0,-1,.05))
    orient(side+'Foot',rest[pb(side+'Foot').name].to_quaternion())
def hand(side,target,grip):
    sign=1 if side=='Left' else -1
    bend_chain(side+'Arm',side+'ForeArm',side+'Hand',target,(sign*.8,.25,-.25))
    b=pb(side+'Hand');r=rest[b.name].to_quaternion()
    q=(r@Vector((0,1,0))).rotation_difference(Vector((0,-1,0)))@r
    # Keep both palms facing the shaft, with the thumb on the inside.
    axis=Vector((0,-1,0));normal=q@Vector((0,0,1));wanted=Vector((0,0,1))
    angle=math.atan2(axis.dot(normal.cross(wanted)),normal.dot(wanted))
    orient(side+'Hand',Quaternion(axis,angle)@q)
    for finger in ('Index','Middle','Ring','Pinky'):
        for j in range(1,4):
            n=side+'Hand'+finger+str(j)
            if pb(n):orient(n,Quaternion(Vector((1,0,0)),grip*(.60 if j==1 else .8))@pb(n).matrix.to_quaternion())
    for j in range(1,4):
        n=side+'HandThumb'+str(j)
        orient(n,Quaternion(Vector((0,0,1)),sign*grip*.28)@pb(n).matrix.to_quaternion())
def pose(kind,frame):
    reset();phase=frame/30
    grip= min(1,frame/30) if kind=='Cart_Grip' else 1
    lean=(10 if kind!='Cart_Pull' else 5)*grip
    # Spine lean distributes through the torso instead of bending only the pelvis.
    pb('Hips').location.z=-.009*grip
    for n,amount in [('Spine',.35),('Spine1',.35),('Spine2',.30)]:
        b=pb(n);orient(n,Quaternion(Vector((1,0,0)),math.radians(lean*amount))@b.matrix.to_quaternion())
    for side,sign in [('Left',1),('Right',-1)]:
        ankle=rest[pb(side+'Foot').name].translation.copy()
        if kind!='Cart_Grip':
            p=(phase+(0 if sign>0 else .5))%1
            # 60% planted stance; the returning foot lifts gently, no sliding root motion.
            travel=.297 if kind=='Cart_Push' else -.17
            if p<.6: ankle.y+=(p/.6-.5)*travel
            else:
                s=(p-.6)/.4;ankle.y+=(.5-s)*travel;ankle.z+=math.sin(s*math.pi)*.045
        foot(side,ankle)
        wrist=Vector((sign*grip_position[0]/1.5151714,-(grip_position[2]-keeper_z)/1.5151714,(grip_position[1]-.03674819)/1.5151714))
        relaxed=Vector((sign*.13,.015,.50))
        hand(side,relaxed.lerp(wrist,grip),grip)
    update()
def curves(action):
    return [f for layer in action.layers for strip in layer.strips for bag in strip.channelbags for f in bag.fcurves]
audit=[]
for kind in ('Cart_Grip','Cart_Pull','Cart_Push'):
    action=bpy.data.actions.new(kind);action.use_fake_user=True;rig.animation_data.action=action
    frames=[0,6,12,18,24,30] if kind=='Cart_Grip' else [0,3,6,9,12,15,18,21,24,27,30]
    for frame in frames:
        sc.frame_set(frame);pose(kind,frame)
        for b in rig.pose.bones:
            b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name)
            if b.name.endswith(':Hips'):b.keyframe_insert('location',frame=frame,group=b.name)
    for f in curves(action):
        for key in f.keyframe_points:key.interpolation='BEZIER';key.handle_left_type='AUTO_CLAMPED';key.handle_right_type='AUTO_CLAMPED'
    # Quaternion continuity avoids taking the long rotation path between sparse keys.
    for b in rig.pose.bones:
        fs=sorted([f for f in curves(action) if f.data_path==b.path_from_id('rotation_quaternion')],key=lambda f:f.array_index)
        if len(fs)!=4:continue
        previous=None
        for i in range(len(fs[0].keyframe_points)):
            q=Quaternion([f.keyframe_points[i].co.y for f in fs])
            if previous is not None and q.dot(previous)<0:
                for f in fs:f.keyframe_points[i].co.y*=-1
                q.negate()
            previous=q
        for f in fs:f.update()
    audit.append({'action':kind,'frames':frames,'interpolation':'BEZIER AUTO_CLAMPED','durationSeconds':1})
    sc.frame_start=0;sc.frame_end=30
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(OUT/(kind+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='RELATIVE')
# Reference cart stays in the main editing file, excluded from FBX exports above.
with bpy.data.libraries.load(str(A/'Source~/MarketStand/MarketStand_A.blend'),link=False) as (data,dest):
    dest.objects=[n for n in data.objects if n.startswith('MarketCart_') and n.endswith('LOD2')]
reference=bpy.data.objects.new('Cart reference - DO NOT EXPORT',None);sc.collection.objects.link(reference)
reference['CartReference']=True;reference.scale=(1/1.5151714,)*3;reference.location=(0,keeper_z/1.5151714,-.03674819/1.5151714)
for ob in dest.objects:
    if ob:sc.collection.objects.link(ob);ob.parent=reference;ob['CartReference']=True
rig.animation_data.action=bpy.data.actions['Cart_Push'];sc.frame_set(0)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=3
sc.frame_start=0;sc.frame_end=30
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'MarketKeeper.blend'))
(SRC/'CartActions_Manifest.json').write_text(json.dumps(audit,indent=2))
# Native Blender four-pose review using the actual character and cart proportions.
sc.render.engine='CYCLES';sc.cycles.samples=12;sc.render.resolution_x=900;sc.render.resolution_y=700;sc.render.resolution_percentage=100
if sc.world is None:sc.world=bpy.data.worlds.new('Cart review world')
sc.world.color=(.4,.4,.4);sc.view_settings.view_transform='AgX'
def look(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2,1.8,1.3));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=2.9;look(cam,(0,-.7,.6));sc.camera=cam
for pos in [(2,0,4),(-2,-1,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=180;o.data.shape='DISK';o.data.size=3;look(o,(0,-.7,.5))
for kind,frame in [('Cart_Grip',0),('Cart_Grip',30),('Cart_Pull',12),('Cart_Push',12)]:
    rig.animation_data.action=bpy.data.actions[kind];sc.frame_set(frame);sc.render.filepath=str(REVIEW/(kind+'_'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
print('CART_ACTIONS_PASS sparse source keys; three baked Unity exports',flush=True)
