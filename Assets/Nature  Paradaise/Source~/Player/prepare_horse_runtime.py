"""Extract authored horse body motion and make in-place runtime takes in memory.

The editable BLOCK and production source Actions remain sparse in the saved
master. Only the export process samples the finished takes at the runtime rate.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector

def prepare_horse_runtime(rig,scene,trajectory_path):
    names=['Player | Mount Horse','Player | Dismount Horse','Player | Mount Horse Right','Player | Dismount Horse Right']
    if not all(bpy.data.actions.get(n) and bpy.data.actions[n].get('horse_root_yaw_split') for n in names):return None
    # Read sparse editable timelines again on every export, so later manual key
    # edits cannot silently leave the runtime takes on an older revision.
    sources=['BLOCK | Kuda Naik | Frame 1-95','BLOCK | Kuda Turun | Frame 1-95','BLOCK | Kuda Naik Kanan | Frame 1-95','BLOCK | Kuda Turun Kanan | Frame 1-95']
    for name,source_name in zip(names,sources):
        source=bpy.data.actions[source_name];target=bpy.data.actions[name]
        for c in list(target.fcurves):target.fcurves.remove(c)
        for c in source.fcurves:
            out=target.fcurves.new(c.data_path,index=c.array_index,action_group=c.group.name if c.group else '');out.keyframe_points.add(len(c.keyframe_points))
            for old,new in zip(c.keyframe_points,out.keyframe_points):
                new.co=old.co;new.interpolation=old.interpolation;new.handle_left_type=old.handle_left_type;new.handle_right_type=old.handle_right_type;new.handle_left=old.handle_left;new.handle_right=old.handle_right
            out.update()
    seat=bpy.data.actions[sources[0]];rig.animation_data.action=seat;rig.animation_data.action_slot=seat.slots[0];scene.frame_set(95);bpy.context.view_layer.update()
    seated_basis={b.name:b.matrix_basis.copy() for b in rig.pose.bones if b.name.startswith('Ctrl_')}
    riding=bpy.data.actions['Player | Riding Idle']
    for c in list(riding.fcurves):riding.fcurves.remove(c)
    for f in (1,31,61):
        rig.animation_data.action=riding;scene.frame_set(f)
        for n,m in seated_basis.items():
            b=rig.pose.bones[n];b.matrix_basis=m
            for prop in ('location','rotation_quaternion','scale'):b.keyframe_insert(prop,frame=f,group=n)
            if 'ik_fk_switch' in b:b.keyframe_insert('["ik_fk_switch"]',frame=f,group=n)
    controls=sorted([b for b in rig.pose.bones if b.name.startswith('Ctrl_')],key=lambda b:len(b.parent_recursive))
    def assign(a,f):
        rig.animation_data.action=a
        if a.slots:rig.animation_data.action_slot=a.slots[0]
        scene.frame_set(f);bpy.context.view_layer.update()
    assign(bpy.data.actions['Player | Idle'],1)
    idle={b.name:b.matrix_basis.copy() for b in controls}
    hips_idle=rig.pose.bones['mixamorig:Hips'].matrix.to_3x3().copy()
    S=1.33043254;OFF=Vector((-.0174398734,.0974751771,.02268))
    trajectories={};pose_report={}
    for name in names:
        a=bpy.data.actions[name];samples=[]
        for f in range(1,96):
            assign(a,f);hip=rig.pose.bones['mixamorig:Hips'];point=OFF+(rig.matrix_world@hip.head)*S
            axis=hip.matrix.to_3x3()@hips_idle.inverted()@Vector((1,0,0))
            yaw=math.atan2(axis.y,axis.x)
            mats={b.name:b.matrix.copy() for b in controls};delta=Matrix.Rotation(-yaw,4,'Z')
            # Rotate controls around the hips; translation follows a separate,
            # scale-independent trajectory in PersonalAnimal.
            target={}
            for n,m in mats.items():
                out=delta@m;out.translation=hip.head+delta.to_3x3()@(m.translation-hip.head);target[n]=out
            samples.append((f,target,point,-math.degrees(yaw)))
        if not name.endswith('Right'):
            kind='mount' if 'Mount Horse' in name else 'dismount'
            p0,p1=samples[0][2],samples[-1][2];points=[]
            for f,_,p,yaw in samples:
                phase=(f-1)/94;horizontal=(p.x-p0.x)/(p1.x-p0.x);vertical=horizontal
                points.append({'phase':phase,'horizontal':horizontal,'vertical':vertical,
                               'lift':p.z-(p0.z+(p1.z-p0.z)*vertical),
                               'forward':-p.y-(-p0.y+(-p1.y+p0.y)*horizontal),'yaw':yaw})
            trajectories[kind]=points
            if kind=='mount':
                anchor=bpy.data.objects['SaddleSeatAnchor_UnityReference'];seat=OFF+anchor.matrix_world.translation*S
                d=p1-seat;trajectories['seatedHipOffset']={'x':-d.x,'y':d.z,'z':-d.y}
        old_name=a.name;a.name='TEMP export source '+old_name
        runtime=bpy.data.actions.new(old_name);runtime.use_fake_user=True;runtime.use_frame_range=True;runtime.frame_start=1;runtime.frame_end=95
        rig.animation_data.action=None;previous={}
        for f,target,_,_ in samples:
            # Work with native controls, in their true parent order.
            for b in controls:b.matrix=target[b.name];bpy.context.view_layer.update()
            # Match ordinary standing Idle exactly at gameplay hand-offs. The
            # visible world-facing direction is supplied by the yaw trajectory.
            weight=(1-max(0,min(1,(f-1)/11))) if 'Mount Horse' in old_name else max(0,min(1,(f-85)/10))
            for b in controls:
                if weight:
                    l,q,s=b.matrix_basis.decompose();li,qi,si=idle[b.name].decompose();b.matrix_basis=Matrix.LocRotScale(l.lerp(li,weight),q.slerp(qi,weight),s.lerp(si,weight))
            authored={b.name:b.matrix_basis.copy() for b in controls}
            assign(runtime,f)
            for b in controls:
                b.matrix_basis=authored[b.name];b.rotation_mode='QUATERNION'
                if b.name in previous and b.rotation_quaternion.dot(previous[b.name])<0:b.rotation_quaternion.negate()
                previous[b.name]=b.rotation_quaternion.copy()
                for prop in ('location','rotation_quaternion','scale'):b.keyframe_insert(prop,frame=f,group=b.name)
                if 'ik_fk_switch' in b:b.keyframe_insert('["ik_fk_switch"]',frame=f,group=b.name)
            rig.animation_data.action=None
        for c in runtime.fcurves:
            for k in c.keyframe_points:k.interpolation='LINEAR'
        bpy.data.actions.remove(a)
        pose_report[old_name]=95
    trajectories['version']=2
    Path(trajectory_path).write_text(json.dumps(trajectories,indent=2))
    return pose_report
