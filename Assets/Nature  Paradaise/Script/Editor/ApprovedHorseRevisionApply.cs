#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Applies an explicitly requested approved horse revision without opening or saving scenes.</summary>
[InitializeOnLoad]
public static class ApprovedHorseRevisionApply
{
    const string Root="Assets/Nature  Paradaise",Folder="ArtSource/Player/HorseUnityApply_2026-10-29";
    static ApprovedHorseRevisionApply()
    {
        if(File.Exists(Folder+"/PendingApply.txt"))EditorApplication.delayCall+=ApplyPending;
    }
    static void ApplyPending()
    {
        string path=Folder+"/PendingApply.txt";
        if(!File.Exists(path))return;
        string token=File.ReadAllText(path).Trim();
        if(File.Exists(Folder+"/UnityApplied.txt")&&File.ReadAllText(Folder+"/UnityApplied.txt").StartsWith("PASS "+token+"\n",StringComparison.Ordinal))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=ApplyPending;return;}
        try
        {
            HorseRiderTransitionsSetup.Apply();
            string report=ValidateAssetBindings();
            File.WriteAllText(Folder+"/UnityApplied.txt","PASS "+token+"\n"+report);
            Debug.Log("APPROVED_HORSE_UNITY_APPLIED "+token);
        }
        catch(Exception e){File.WriteAllText(Folder+"/UnityApplied.txt","FAIL "+token+"\n"+e);Debug.LogException(e);}
    }
    [MenuItem("Nature Paradise/Animals/Validate Approved Horse Animations")]
    public static void ValidateMenu()=>Debug.Log(ValidateAssetBindings());

    /// <summary>Checks real imported clips, shared seated/standing endpoints and explicit side states.</summary>
    public static string ValidateAssetBindings()
    {
        var report=new StringBuilder();int checks=0;
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;report.AppendLine("PASS "+message);}
        var profile=AssetDatabase.LoadAssetAtPath<HorseRiderTransitionProfile>(HorseRiderTransitionsSetup.ProfilePath);
        Check(profile!=null&&profile.authoredBodyYaw,"New body trajectory and facing are enabled");
        var clips=new[]{profile.mount,profile.mountRight,profile.dismount,profile.dismountRight,profile.ridingIdle};
        Check(clips.All(c=>c!=null&&AssetDatabase.GetAssetPath(c)==HorseRiderTransitionsSetup.MotionPath),"All five takes use the full player FBX");
        Check(clips.Take(4).All(c=>Mathf.Abs(c.length-94f/30)<.01f),"Four approved 95-frame clips are imported");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(HorseRiderTransitionsSetup.MotionPath);
        var model=Object.Instantiate(source);
        try
        {
            var bones=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")&&!t.name.EndsWith("_end")).ToArray();
            void Compare(AnimationClip a,float ta,AnimationClip b,float tb,string label)
            {
                a.SampleAnimation(model,ta);var pose=bones.Select(t=>(t.localPosition,t.localRotation,t.localScale)).ToArray();
                b.SampleAnimation(model,tb);float pos=0,rot=0;
                for(int i=0;i<bones.Length;i++){pos=Mathf.Max(pos,Vector3.Distance(pose[i].localPosition,bones[i].localPosition));rot=Mathf.Max(rot,Quaternion.Angle(pose[i].localRotation,bones[i].localRotation));}
                report.AppendLine("ENDPOINT "+label+" position="+pos+" rotation="+rot);
                Check(bones.Length>=65&&pos<.002f&&rot<.3f,label);
            }
            var idle=AssetDatabase.LoadAllAssetsAtPath(HorseRiderTransitionsSetup.MotionPath).OfType<AnimationClip>().Single(c=>c.name=="Idle");
            foreach(var c in new[]{profile.mount,profile.mountRight}){Compare(c,c.length,profile.ridingIdle,0,c.name+" ends in the shared saddle pose");Compare(c,0,idle,0,c.name+" begins at neutral standing pose before authored facing");}
            foreach(var c in new[]{profile.dismount,profile.dismountRight}){Compare(c,0,profile.ridingIdle,0,c.name+" starts in the shared saddle pose");Compare(c,c.length,idle,0,c.name+" ends at standing Idle");}
        }
        finally{Object.DestroyImmediate(model);}
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Player/PlayerVisual.prefab");
        var controller=visual.GetComponentInChildren<Animator>().runtimeAnimatorController as AnimatorController;
        foreach(string name in new[]{"Mount Horse","Mount Horse Right","Dismount Horse","Dismount Horse Right"})
        {
            var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            Check(state.motion!=null&&state.motion.name==name&&!state.mirror&&Mathf.Abs(state.speed-1.6f)<.001f,"Explicit side and playback speed: "+name);
        }
        Check(Mathf.Abs(profile.Yaw(0,true)-80)<.1f&&Mathf.Abs(profile.Yaw(1,true))<.1f&&Mathf.Abs(profile.Yaw(1,false)-80)<.1f,"Body turn matches the approved horse-facing endpoints");
        var horse=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Animals/Companions/Horse Companion.prefab");
        var so=new SerializedObject(horse.GetComponent<PersonalAnimal>());
        Check(so.FindProperty("riderTransitionProfile").objectReferenceValue==profile,"Horse prefab uses the approved transition profile");
        Check(so.FindProperty("mountPoint").objectReferenceValue is Transform t&&t.name=="SaddleSeatAnchor","Saddle anchor is assigned");
        var offset=so.FindProperty("riderSeatOffset").vector3Value;
        Check(offset.y>.14f&&offset.y<.17f&&Mathf.Abs(offset.z+.06f)<.005f,"New seated hip offset matches the approved Blender pose");
        return report+"PASS_TOTAL "+checks+"\n";
    }
}
#endif
