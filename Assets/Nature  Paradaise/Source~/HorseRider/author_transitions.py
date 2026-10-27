import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion,Euler
ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx').exists());OUT=ROOT/'ArtSource/HorseRider';FBX=ROOT/'Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player.fbx'
for folder in ['References','Exports','Blender']:(OUT/folder).mkdir(parents=True,exist_ok=True)
reference=OUT/'References/Horse_UnityScale_Reference.obj'
if not reference.exists():
    import shutil
    shutil.copyfile(Path(__file__).resolve().parent/'Horse_UnityScale_Reference.obj',reference)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(FBX),use_anim=True)
scene=bpy.context.scene;scene.name='Rider Animation Sources';scene.render.fps=30
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');meshes=[o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)];source_actions=list(bpy.data.actions)
for o in list(bpy.data.objects):
    if o.type=='MESH' and o not in meshes:bpy.data.objects.remove(o,do_unlink=True)
def action_named(s):return next(a for a in source_actions if a.name.endswith('Player | '+s))
def assign(action):
    rig.animation_data.action=action
    if len(action.slots):rig.animation_data.action_slot=action.slots[0]
def snapshot(action,frame):
    assign(action);scene.frame_set(frame);bpy.context.view_layer.update()
    return {p.name:p.matrix_basis.copy() for p in rig.pose.bones}
ride=snapshot(action_named('Riding Idle'),1);idle=snapshot(action_named('Idle'),1)
def smooth(a,b,t):
    x=max(0,min(1,(t-a)/(b-a)));return x*x*(3-2*x)
def pose_blend(weight):
    for p in rig.pose.bones:
        l0,q0,s0=ride[p.name].decompose();l1,q1,s1=idle[p.name].decompose()
        p.matrix_basis=Matrix.LocRotScale(l0.lerp(l1,weight),q0.slerp(q1,weight),s0.lerp(s1,weight))
    bpy.context.view_layer.update()
def rotate_global(name,euler):
    p=rig.pose.bones[name];m=p.matrix.copy();m=Euler(euler,'XYZ').to_matrix().to_4x4()@m;m.translation=p.head.copy();p.matrix=m;bpy.context.view_layer.update()
def solve_chain(upper_name,lower_name,end_name,target,pole):
    upper,lower,end=[rig.pose.bones[n] for n in (upper_name,lower_name,end_name)]
    h,k,f=[p.head.copy() for p in (upper,lower,end)];end_rotation=end.matrix.to_quaternion();l1=(k-h).length;l2=(f-k).length
    direction=Vector(target)-h;d=min(max(direction.length,.0001),l1+l2-.00001);direction.normalize()
    bend=Vector(pole)-h;bend-=direction*direction.dot(bend)
    if bend.length<.001:bend=Vector((0,-1,0));bend-=direction*direction.dot(bend)
    bend.normalize();along=(l1*l1-l2*l2+d*d)/(2*d);knee=h+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
    q=(k-h).rotation_difference(knee-h);m=q.to_matrix().to_4x4()@upper.matrix;m.translation=h;upper.matrix=m;bpy.context.view_layer.update()
    k=lower.head.copy();f=end.head.copy();target_actual=h+direction*d
    q=(f-k).rotation_difference(target_actual-k);m=q.to_matrix().to_4x4()@lower.matrix;m.translation=k;lower.matrix=m;bpy.context.view_layer.update()
    m=end_rotation.to_matrix().to_4x4();m.translation=end.head.copy();end.matrix=m;bpy.context.view_layer.update()
    return (end.head-Vector(target)).length

def target_at(keys,t):
    for (a,p),(b,q) in zip(keys,keys[1:]):
        if t<=b:return Vector(p).lerp(Vector(q),smooth(a,b,t))
    return Vector(keys[-1][1])
pose_blend(0);ride_foot={s:rig.pose.bones['mixamorig:'+s+'Foot'].head.copy() for s in ['Left','Right']};ride_hand={s:rig.pose.bones['mixamorig:'+s+'Hand'].head.copy() for s in ['Left','Right']}
pose_blend(1);idle_foot={s:rig.pose.bones['mixamorig:'+s+'Foot'].head.copy() for s in ['Left','Right']};idle_hand={s:rig.pose.bones['mixamorig:'+s+'Hand'].head.copy() for s in ['Left','Right']}
new_actions=[];metrics=[]
def author_pose(t):
    base=smooth(.50,.90,t);pose_blend(base)
    if t<=0 or t>=.90:return
    lift=math.sin(math.pi*smooth(0,.82,t))*.028
    p=rig.pose.bones['mixamorig:Hips'];m=p.matrix.copy();m.translation.z+=lift;p.matrix=m;bpy.context.view_layer.update()
    weight=math.sin(math.pi*smooth(0,.84,t))
    for n in ['Spine','Spine1','Spine2']:rotate_global('mixamorig:'+n,(math.radians(7)*weight,0,math.radians(6)*weight))
    # Swing the extended leg around the rear of the saddle, away from the waist.
    r=target_at([(0,ride_foot['Right']),(.16,(-.23,.12,.36)),(.30,(-.18,.34,.61)),(.39,(-.01,.42,.65)),(.48,(.23,.31,.58)),(.62,(.26,.08,.32)),(.80,idle_foot['Right']),(.90,idle_foot['Right'])],t)
    l=target_at([(0,ride_foot['Left']),(.36,ride_foot['Left']),(.60,(.15,-.02,.27)),(.80,idle_foot['Left']),(.90,idle_foot['Left'])],t)
    knee=target_at([(0,(-.167,-.148,.495)),(.12,(-.30,-.08,.52)),(.30,(-.36,.20,.60)),(.39,(-.20,.30,.76)),(.48,(.16,.30,.72)),(.62,(.32,-.05,.51)),(.80,(-.05,-.05,.32)),(.90,(-.05,-.05,.32))],t)
    solve_chain('mixamorig:RightUpLeg','mixamorig:RightLeg','mixamorig:RightFoot',r,knee)
    foot=rig.pose.bones['mixamorig:RightFoot'];foot_matrix=foot.matrix.copy()
    ankle=math.sin(math.pi*smooth(0,.80,t));level=Euler((math.radians(-24)*ankle,0,math.radians(-22)*ankle),'XYZ').to_matrix().to_4x4()
    leveled=level@foot_matrix;leveled.translation=foot_matrix.translation;foot.matrix=leveled;bpy.context.view_layer.update()
    solve_chain('mixamorig:LeftUpLeg','mixamorig:LeftLeg','mixamorig:LeftFoot',l,(.25,-.30,.46))
    for side in ['Left','Right']:
        p=target_at([(0,ride_hand[side]),(.25,(.10 if side=='Left' else .015,-.11,.63)),(.48,(.14 if side=='Left' else .07,-.09,.60)),(.72,idle_hand[side]),(.90,idle_hand[side])],t)
        solve_chain('mixamorig:'+side+'Arm','mixamorig:'+side+'ForeArm','mixamorig:'+side+'Hand',p,(.38 if side=='Left' else -.38,.09,.65))

def create(name,frames,reverse=False):
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for f in range(1,frames+2):
        t=(f-1)/frames;author_pose(1-t if reverse else t)
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION';p.keyframe_insert('location',frame=f,group=p.name);p.keyframe_insert('rotation_quaternion',frame=f,group=p.name);p.keyframe_insert('scale',frame=f,group=p.name)
    for curve in action.fcurves:
        for key in curve.keyframe_points:key.interpolation='LINEAR'
    new_actions.append(action);return action
# Endpoint poses are shared exactly across all three takes.
dismount=create('Dismount Horse',90);mount=create('Mount Horse',75,True)
idle_action=bpy.data.actions.new('Riding Idle');idle_action.use_fake_user=True;rig.animation_data.action=idle_action
for frame in range(1,62):
    pose_blend(0)
    for p in rig.pose.bones:
        p.rotation_mode='QUATERNION';p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
new_actions.append(idle_action)
# Generic Unity rigs do not support Humanoid state mirroring: bake right-side takes.
reflection=Matrix.Diagonal((-1,1,1,1))
def mirrored_right(name,frames,reverse=False):
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for f in range(1,frames+2):
        t=(f-1)/frames;t=1-t if reverse else t;author_pose(t)
        global_pose={p.name:p.matrix.copy() for p in rig.pose.bones}
        def opposite(n):
            return n.replace('Left','@@').replace('Right','Left').replace('@@','Right')
        mirrored={}
        for p in rig.pose.bones:
            other=opposite(p.name)
            if other not in rig.pose.bones:other=p.name
            mirrored[p.name]=reflection@global_pose[other]@rig.data.bones[other].matrix_local.inverted()@reflection@p.bone.matrix_local
        for p in rig.pose.bones:
            parent=p.parent
            p.matrix_basis=p.bone.convert_local_to_pose(mirrored[p.name],p.bone.matrix_local,parent_matrix=mirrored[parent.name] if parent else Matrix.Identity(4),parent_matrix_local=parent.bone.matrix_local if parent else Matrix.Identity(4),invert=True)
        bpy.context.view_layer.update()
        endpoint=1-smooth(0,.08,t) if t<.08 else smooth(.85,1,t) if t>.85 else 0
        target=ride if t<.08 else idle
        if endpoint>0:
            for p in rig.pose.bones:
                l,q,s=p.matrix_basis.decompose();l1,q1,s1=target[p.name].decompose();p.matrix_basis=Matrix.LocRotScale(l.lerp(l1,endpoint),q.slerp(q1,endpoint),s.lerp(s1,endpoint))
            bpy.context.view_layer.update()
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION';p.keyframe_insert('location',frame=f,group=p.name);p.keyframe_insert('rotation_quaternion',frame=f,group=p.name);p.keyframe_insert('scale',frame=f,group=p.name)
    new_actions.append(action);return action
mirrored_right('Dismount Horse Right',90);mirrored_right('Mount Horse Right',75,True)
# Only the five authored takes enter this FBX; all original player takes remain untouched in Player.fbx.
for a in source_actions:bpy.data.actions.remove(a)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
rig.animation_data.action=idle_action
# Preserve the production FBX hierarchy: Unity Armature scale=100 and
# child positions in centimetre-local coordinates. Pose world sizes stay metres.
scene.unit_settings.scale_length=.01
rig.scale=(100,100,100)
bpy.context.view_layer.update()
bpy.ops.export_scene.fbx(filepath=str(OUT/'Exports/RiderHorseTransitions.fbx'),use_selection=True,object_types={'ARMATURE'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=1,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
rig.scale=(1,1,1)
scene.unit_settings.scale_length=1
bpy.context.view_layer.update()
# Reference scene shows the actual Unity-sized horse and a linked rider at its real visual scale.
preview=bpy.data.scenes.new('Unity Horse Dismount Review');preview.render.fps=30;preview.frame_start=1;preview.frame_end=91
bpy.context.window.scene=preview
bpy.ops.wm.obj_import(filepath=str(OUT/'References/Horse_UnityScale_Reference.obj'),forward_axis='Y',up_axis='Z')
horse_objects=list(bpy.context.selected_objects)
for o in horse_objects:o.rotation_euler.z+=math.pi;o.name='UnityScale_'+o.name
bpy.ops.export_scene.fbx(filepath=str(OUT/'Exports/Horse_UnityScale_Reference.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_anim=False,add_leaf_bones=False)
floor=bpy.data.materials.new('Horse reference warm wood');floor.diffuse_color=(.36,.21,.10,1)
for o in horse_objects:
    if o.type=='MESH':o.data.materials.clear();o.data.materials.append(floor)
parent=bpy.data.objects.new('RiderRoot_UnityWorldMotion',None);preview.collection.objects.link(parent)
view_rig=rig.copy();view_rig.data=rig.data.copy();preview.collection.objects.link(view_rig);view_rig.name='Rider_UnityScale_1_33043254';view_rig.parent=parent;view_rig.scale=(1.33043254,)*3;view_rig.location=(-.0174398734,.0974751771,.15)
view_rig.animation_data.action=dismount
if len(dismount.slots):view_rig.animation_data.action_slot=dismount.slots[0]
for mesh in meshes:
    copy=mesh.copy();copy.data=mesh.data.copy();preview.collection.objects.link(copy);copy.parent=view_rig;copy.matrix_parent_inverse=mesh.matrix_parent_inverse.copy()
    for mod in copy.modifiers:
        if mod.type=='ARMATURE':mod.object=view_rig
    copy.name='RiderReference_'+mesh.name
anchor=bpy.data.objects.new('SaddleSeatAnchor_UnityReference',None);preview.collection.objects.link(anchor);anchor.empty_display_type='SPHERE';anchor.empty_display_size=.035;anchor.location=(0,0,1.044548)
hips_target=Vector((0,0,1.094548));preview.frame_set(1);bpy.context.view_layer.update();start=view_rig.matrix_world@view_rig.pose.bones['mixamorig:Hips'].head
for f in range(1,92):
    t=(f-1)/90;h=smooth(.30,.82,t);v=smooth(.38,.90,t);lift=math.sin(math.pi*smooth(0,.85,t))*.055
    target=Vector((.65*h,0,hips_target.z*(1-v)+.8491117*v+lift));preview.frame_set(f);parent.location=(0,0,0);bpy.context.view_layer.update();actual=view_rig.matrix_world@view_rig.pose.bones['mixamorig:Hips'].head;parent.location=target-actual;parent.keyframe_insert('location',frame=f)
for frame,name in [(1,'Seated_matches_RidingIdle'),(16,'Hands_support_saddle'),(30,'Right_leg_over_back'),(46,'Both_legs_left_side'),(73,'Feet_landing'),(82,'Standing_matches_Idle'),(91,'Control_ready')]:preview.timeline_markers.new(name,frame=frame)
preview.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');view_rig.select_set(True);bpy.context.view_layer.objects.active=view_rig
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=3.8;area.spaces.active.region_3d.view_location=Vector((0,0,.8))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Blender/Horse_Rider_Transitions.blend'),compress=True)
report={'runtime_fbx':str(OUT/'Exports/RiderHorseTransitions.fbx'),'source_fbx_sha256':hashlib.sha256(FBX.read_bytes()).hexdigest(),'takes':[{ 'name':a.name,'range':list(a.frame_range)} for a in new_actions],'unity_horse_root_scale':.633059631,'unity_rider_visual_scale':1.33043254,'saddle_surface_height':1.044548,'rider_pelvis_target_height':1.094548,'preview_coordinate_mapping':'Blender=(-UnityX,-UnityZ,UnityY)','notes':'Standing end matches Idle frame1; mount end and dismount start exactly match Riding Idle. Canonical dismount is left side; right-side takes are baked explicitly.'}
(OUT/'References/Authoring.json').write_text(json.dumps(report,indent=2));print('HORSE_RIDER_AUTHORING_PASS',flush=True)






