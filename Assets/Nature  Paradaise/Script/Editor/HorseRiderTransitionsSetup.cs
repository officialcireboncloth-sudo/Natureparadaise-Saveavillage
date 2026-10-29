using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Links horse Actions from the complete player export and the matching approved trajectory, without editing world scenes.</summary>
public static class HorseRiderTransitionsSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string MotionPath=Root+"/mesh/Dummy/PlayerDummy/Player.fbx";
    public const string ProfilePath=Root+"/Resources/Profiles/Horse Rider Transitions.asset";
    [MenuItem("Nature Paradise/Animals/Apply Horse Rider Transitions")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying horse animation assets.");
        AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(MotionPath);
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.preserveHierarchy=true;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.KeyframeReduction;
        importer.animationRotationError=.1f;importer.animationPositionError=.02f;importer.animationScaleError=.02f;
        var clips=importer.clipAnimations.ToList();
        foreach(var take in importer.defaultClipAnimations)
        {
            string name=take.name.Substring(take.name.LastIndexOf('|')+1).Trim();
            bool horseAction=name=="Riding Idle"||name=="Mount Horse"||name=="Dismount Horse"||name=="Mount Horse Right"||name=="Dismount Horse Right";
            var clip=clips.FirstOrDefault(x=>x.name==name);
            if(clip==null){clip=take;clip.name=name;clips.Add(clip);}
            if(!horseAction)continue;
            clip.takeName=take.takeName;clip.firstFrame=take.firstFrame;clip.lastFrame=take.lastFrame;
            clip.loopTime=clip.name=="Riding Idle";clip.loopPose=clip.loopTime;
            clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;
            clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
        }
        importer.clipAnimations=clips.ToArray();importer.SaveAndReimport();
        var all=AssetDatabase.LoadAllAssetsAtPath(MotionPath).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")).ToArray();
        AnimationClip Clip(string name)=>all.Single(x=>x.name==name);
        var profile=AssetDatabase.LoadAssetAtPath<HorseRiderTransitionProfile>(ProfilePath);
        if(profile==null){profile=ScriptableObject.CreateInstance<HorseRiderTransitionProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);}
        profile.mount=Clip("Mount Horse");profile.mountRight=Clip("Mount Horse Right");profile.dismount=Clip("Dismount Horse");profile.dismountRight=Clip("Dismount Horse Right");profile.ridingIdle=Clip("Riding Idle");
        ApplyAuthoredTrajectory(profile);
        CalibrateStandingSoles();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/mesh/Dummy/PlayerDummy/Player.fbx");
        var originalIdle=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<AnimationClip>().Single(x=>x.name=="Idle");
        var sample=UnityEngine.Object.Instantiate(source);originalIdle.SampleAnimation(sample,0);
        var hips=sample.GetComponentsInChildren<Transform>().Single(x=>x.name=="mixamorig:Hips");profile.standingHipModelLocal=sample.transform.InverseTransformPoint(hips.position);UnityEngine.Object.DestroyImmediate(sample);
        EditorUtility.SetDirty(profile);
        var set=AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root+"/Player/Animations/Player Animation Set.asset");ApplyClipOverrides(set);EditorUtility.SetDirty(set);
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        ConfigureController(visual.GetComponentInChildren<Animator>().runtimeAnimatorController as AnimatorController);
        string path=Root+"/Prefabs/Animals/Companions/Horse Companion.prefab";
        var horse=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var companion=horse.GetComponent<PersonalAnimal>();var serialized=new SerializedObject(companion);
            var old=serialized.FindProperty("mountPoint").objectReferenceValue as Transform;
            var spine=horse.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Def_Spine");
            var anchor=spine.Find("SaddleSeatAnchor");if(anchor==null){anchor=new GameObject("SaddleSeatAnchor").transform;anchor.SetParent(spine,false);anchor.SetPositionAndRotation(old!=null?old.position:horse.transform.TransformPoint(new Vector3(0,1.65f,0)),horse.transform.rotation);}
            serialized.FindProperty("mountActionDuration").floatValue=profile.mount.length/1.6f;serialized.FindProperty("dismountActionDuration").floatValue=profile.dismount.length/1.6f;
            var trajectory=ReadTrajectory();
            if(trajectory.version>=2)serialized.FindProperty("riderSeatOffset").vector3Value=trajectory.seatedHipOffset;
            serialized.FindProperty("mountPoint").objectReferenceValue=anchor;serialized.FindProperty("riderTransitionProfile").objectReferenceValue=profile;serialized.ApplyModifiedPropertiesWithoutUndo();
            var legacy=horse.transform.Find("MountPoint");if(legacy!=null&&legacy!=anchor)UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            PrefabUtility.SaveAsPrefabAsset(horse,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(horse);}
        AssetDatabase.SaveAssets();Debug.Log("HORSE_RIDER_TRANSITIONS_APPLIED five takes + saddle anchor; no scene edited");
    }
    [Serializable] sealed class Point { public float phase,horizontal,vertical,lift,forward,yaw; }
    [Serializable] sealed class Trajectory { public int version;public Vector3 seatedHipOffset;public Point[] mount,dismount; }
    static Trajectory ReadTrajectory()=>JsonUtility.FromJson<Trajectory>(System.IO.File.ReadAllText(Root+"/Source~/Player/HorseTrajectory.json"));
    static void ApplyAuthoredTrajectory(HorseRiderTransitionProfile profile)
    {
        var data=ReadTrajectory();
        AnimationCurve Curve(Point[] points,Func<Point,float> value)
        {
            var curve=new AnimationCurve(points.Select(p=>new Keyframe(p.phase,value(p))).ToArray());
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            return curve;
        }
        profile.mountHorizontal=Curve(data.mount,p=>p.horizontal);profile.mountVertical=Curve(data.mount,p=>p.vertical);profile.mountLiftShape=Curve(data.mount,p=>p.lift);
        profile.dismountHorizontal=Curve(data.dismount,p=>p.horizontal);profile.dismountVertical=Curve(data.dismount,p=>p.vertical);profile.dismountLiftShape=Curve(data.dismount,p=>p.lift);
        profile.mountLift=1;profile.dismountLift=1;
        profile.authoredBodyYaw=data.version>=2;
        if(profile.authoredBodyYaw)
        {
            profile.mountYaw=Curve(data.mount,p=>p.yaw);profile.dismountYaw=Curve(data.dismount,p=>p.yaw);
            profile.mountForwardOffset=Curve(data.mount,p=>p.forward);profile.dismountForwardOffset=Curve(data.dismount,p=>p.forward);
        }
    }
    static void CalibrateStandingSoles()
    {
        const string visualPath=Root+"/Prefabs/Player/PlayerVisual.prefab";
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
        var instance=UnityEngine.Object.Instantiate(visual);float minimum=float.PositiveInfinity;
        try
        {
            var rig=instance.GetComponentInChildren<Animator>();
            var idle=AssetDatabase.LoadAllAssetsAtPath(Root+"/mesh/Dummy/PlayerDummy/Player.fbx").OfType<AnimationClip>().Single(x=>x.name=="Idle");
            idle.SampleAnimation(rig.gameObject,0);
            foreach(var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                var mesh=new Mesh();renderer.BakeMesh(mesh,true);
                foreach(var vertex in mesh.vertices)minimum=Mathf.Min(minimum,renderer.transform.TransformPoint(vertex).y);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(instance);}
        if(float.IsInfinity(minimum)||Mathf.Abs(minimum)<.001f)return;
        var contents=PrefabUtility.LoadPrefabContents(visualPath);
        try
        {
            var rig=contents.GetComponentInChildren<Animator>().transform;
            var position=rig.localPosition;position.y-=minimum/(rig.parent!=null?rig.parent.lossyScale.y:1);rig.localPosition=position;
            PrefabUtility.SaveAsPrefabAsset(contents,visualPath);
            Debug.Log("HORSE_STANDING_SOLES corrected visual offset by "+minimum+"m; sampled model pose was not saved into the prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(contents);}
    }
    public static void ApplyClipOverrides(PlayerAnimationSetSO set)
    {
        var profile=AssetDatabase.LoadAssetAtPath<HorseRiderTransitionProfile>(ProfilePath);if(set==null||profile==null||profile.mount==null)return;
        set.mountHorse=profile.mount;set.dismountHorse=profile.dismount;set.ridingIdle=profile.ridingIdle;
    }
    public static void ConfigureController(AnimatorController controller)
    {
        var profile=AssetDatabase.LoadAssetAtPath<HorseRiderTransitionProfile>(ProfilePath);if(controller==null||profile==null||profile.mount==null)return;
        var machine=controller.layers[0].stateMachine;
        AnimatorState State(string name)=>machine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name)??machine.AddState(name);
        var idle=State("Riding Idle");idle.motion=profile.ridingIdle;
        foreach(bool mounting in new[]{true,false})foreach(bool right in new[]{false,true})
        {
            string name=(mounting?"Mount Horse":"Dismount Horse")+(right?" Right":"");var state=State(name);
            state.motion=mounting?(right?profile.mountRight:profile.mount):(right?profile.dismountRight:profile.dismount);state.speed=1.6f;
            state.mirrorParameterActive=false;state.mirror=false;
            foreach(var exit in state.transitions.ToArray())state.RemoveTransition(exit);
            if(!mounting){var exit=state.AddTransition(State("Locomotion"));exit.hasExitTime=true;exit.exitTime=1;exit.hasFixedDuration=true;exit.duration=.08f;}
            var trigger=mounting?"MountHorse":"DismountHorse";
            var enter=machine.anyStateTransitions.FirstOrDefault(x=>x.destinationState==state);
            if(enter==null)enter=machine.AddAnyStateTransition(state);
            enter.hasExitTime=false;enter.hasFixedDuration=true;enter.duration=.06f;enter.canTransitionToSelf=false;
            enter.conditions=new[]{new AnimatorCondition{parameter=trigger,mode=AnimatorConditionMode.If},new AnimatorCondition{parameter="HorseMountMirror",mode=right?AnimatorConditionMode.If:AnimatorConditionMode.IfNot}};
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);
    }
}


