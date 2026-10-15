using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit native review request only. Keep outside Assets after use.
[InitializeOnLoad]
public static class PlayerToolRevisionReview
{
    const string Root = "Assets/Nature  Paradaise";
    const string Request = "Library/PlayerToolRevisionReview.request";
    const string Result = "Library/PlayerToolRevisionReview.result";
    const string Output = "Documentation/VisualReviews/Player-Tools-2026-10-12";
    static PlayerToolRevisionReview() { EditorApplication.update += Run; }
    static void Run()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch(IOException) { return; }
        try { Review(command); File.WriteAllText(Result, "PASS " + command); }
        catch (Exception ex) { File.WriteAllText(Result, "FAIL " + ex); Debug.LogException(ex); }
    }
    static AnimationClip Clip(PlayerAnimationSetSO set, string id) => id switch {
        "Hoe" => set.hoeing, "Axe" => set.choppingTree, "Hammer" => set.hammeringRock,
        "Sickle" => set.sickle, "WateringCan" => set.wateringPlant, _ => set.idle
    };
    static Vector3 Palm(Transform hand)
    {
        var knuckles = hand.GetComponentsInChildren<Transform>().Where(t => t.name.EndsWith("HandIndex1") || t.name.EndsWith("HandMiddle1") || t.name.EndsWith("HandRing1")).ToArray();
        return knuckles.Length == 0 ? hand.position : Vector3.Lerp(hand.position, knuckles.Aggregate(Vector3.zero, (v,t)=>v+t.position)/knuckles.Length, .75f);
    }
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static void Review(string command)
    {
        if(command=="apply") { PlayerToolVisualSetup.ApplyRevisedToolFit();command="after"; }
        var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSetSO>(Root + "/Player/Animations/Player Animation Set.asset");
        var catalog = AssetDatabase.LoadAssetAtPath<PlayerToolVisualCatalog>(Root + "/Resources/Player/Player Tool Visual Catalog.asset");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Player/PlayerVisual.prefab");
        var report = new StringBuilder(command + " native poses from updated Player.fbx\n");
        foreach (string id in new[] {"Hoe", "Axe", "Hammer", "Sickle", "WateringCan"})
        {
            var clip = Clip(set,id); var entry = catalog.Find(id);
            if (clip == null || entry == null) throw new Exception("Missing clip/tool " + id);
            report.AppendLine(id + " clip=" + clip.name + " length=" + clip.length + " handEuler=" + entry.handEuler + " scale=" + entry.scale);
            int columns=8, cw=300, ch=380;
            var sheet=new Texture2D(columns*cw,ch,TextureFormat.RGB24,false);
            float[] times={-1f,0f,.25f,.45f,.55f,.68f,.85f,Mathf.Max(0,clip.length-.05f)};
            for(int column=0;column<columns;column++)
            {
                var preview = new PreviewRenderUtility();
                try
                {
                    var player = Object.Instantiate(source); preview.AddSingleGO(player);
                    var rig = player.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController!=null); rig.enabled=false;
                    if(times[column]<0)set.idle.SampleAnimation(rig.gameObject,.25f);
                    else clip.SampleAnimation(rig.gameObject,Mathf.Min(times[column],clip.length));
                    var hand=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");
                    var left=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:LeftHand");
                    Vector3 rp=Palm(hand), lp=Palm(left);
                    var held=player.AddComponent<PlayerHeldTools>(); held.enabled=false; Set(held,"catalog",catalog); Call(held,"ResolveHand");
                    var tool=Object.Instantiate(entry.prefab,held.GripPoint,false);
                    Call(held,"PoseTool",tool.transform,entry,times[column]>=0,player.transform.position+Vector3.forward*2f);
                    report.AppendLine(" t=" + times[column].ToString("F3") + " raw palms R=" + rp.ToString("F3") + " L=" + lp.ToString("F3") +
                        " shaft=" + tool.transform.forward.ToString("F3") + " face=" + tool.transform.up.ToString("F3") + " head=" + tool.transform.TransformPoint(Vector3.forward*.8f).ToString("F3"));
                    preview.camera.orthographic=true;preview.camera.orthographicSize=3.15f;
                    preview.camera.transform.position=new Vector3(6,4.5f,7);preview.camera.transform.LookAt(new Vector3(0,2.3f,0));
                    preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=100;
                    preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.28f,.3f,.32f);
                    preview.ambientColor=new Color(.65f,.69f,.72f);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);
                    preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(30,130,0);
                    preview.BeginStaticPreview(new Rect(0,0,cw,ch));preview.Render(true);var frame=preview.EndStaticPreview();
                    sheet.SetPixels(column*cw,0,cw,ch,frame.GetPixels());Object.DestroyImmediate(frame);
                }
                finally{preview.Cleanup();}
            }
            sheet.Apply();File.WriteAllBytes(Output + "-" + command + "-" + id + ".png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
        }
        File.WriteAllText(Output + "-" + command + ".txt",report.ToString());
        if(command=="after") Validate(set,catalog,source,report);
    }
    static void Require(bool pass,string message) { if(!pass)throw new Exception(message); }
    static void Validate(PlayerAnimationSetSO set,PlayerToolVisualCatalog catalog,GameObject source,StringBuilder report)
    {
        int count=0;float maxGrip=0,maxLeft=0,maxTurn=0;
        foreach(string id in new[]{"Hoe","Axe","Hammer","Sickle","WateringCan"})
        foreach(float yaw in new[]{0f,90f,180f,270f})
        {
            var preview=new PreviewRenderUtility();
            try
            {
                var player=Object.Instantiate(source);preview.AddSingleGO(player);player.transform.rotation=Quaternion.Euler(0,yaw,0);
                var rig=player.GetComponentsInChildren<Animator>(true).First(a=>a.runtimeAnimatorController!=null);rig.enabled=false;
                var held=player.AddComponent<PlayerHeldTools>();held.enabled=false;Set(held,"catalog",catalog);Call(held,"ResolveHand");
                var entry=catalog.Find(id);var tool=Object.Instantiate(entry.prefab,held.GripPoint,false);
                var hand=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");
                var left=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:LeftHand");
                var feet=player.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="mixamorig:RightFoot" || t.name=="mixamorig:LeftFoot").ToArray();
                var clip=Clip(set,id);Quaternion previous=Quaternion.identity;
                for(int i=0;i<=100;i++)
                {
                    clip.SampleAnimation(rig.gameObject,clip.length*i/100f);
                    Vector3[] before=feet.Select(t=>t.position).ToArray();
                    Call(held,"PoseTool",tool.transform,entry,true,player.transform.position+player.transform.forward*2f);
                    float grip=Vector3.Distance(tool.transform.position,Palm(hand));maxGrip=Mathf.Max(maxGrip,grip);
                    Require(grip<.002f,id+" primary grip drift "+grip);
                    for(int f=0;f<feet.Length;f++)Require(Vector3.Distance(before[f],feet[f].position)<.0001f,id+" foot modified");
                    if(entry.twoHandsDuringAction)
                    {
                        float error=Vector3.Distance(tool.transform.TransformPoint(entry.secondHandGrip),Palm(left));maxLeft=Mathf.Max(maxLeft,error);
                        Require(error<.025f,id+" support grip drift "+error);
                    }
                    if(id=="WateringCan")
                    {
                        Vector3 spout=tool.transform.right;
                        Require(Mathf.Abs(Vector3.Dot(spout,player.transform.right))<.001f,"Watering spout sideways");
                        Require(Vector3.Dot(spout,player.transform.forward)>0,"Watering spout backwards");
                    }
                    if(entry.actionPlaneNormal.sqrMagnitude>.001f)
                    {
                        Vector3 face=Vector3.Cross(player.transform.TransformDirection(entry.actionPlaneNormal),tool.transform.forward).normalized;
                        Require(Vector3.Dot(face,tool.transform.up)>.999f,id+" face misaligned with stroke plane");
                    }
                    if(i>0) {float turn=Quaternion.Angle(previous,tool.transform.rotation);maxTurn=Mathf.Max(maxTurn,turn);Require(turn<80,id+" rotation flip "+turn);}
                    previous=tool.transform.rotation;count++;
                }
                rig.enabled=true;rig.Rebind();rig.Update(0);
                string state=id switch {"Hoe"=>"Hoeing","Axe"=>"Chopping Tree","Hammer"=>"Hammering Rock","Sickle"=>"Sickle",_=>"Watering Plant"};
                rig.Play(state,0,.5f);rig.Update(0);
                var usingTool=typeof(PlayerHeldTools).GetMethod("UsingWorkTool",BindingFlags.Instance|BindingFlags.NonPublic);
                Require((bool)usingTool.Invoke(held,new object[]{entry}),id+" Animator action state not recognized");
                rig.Play("Locomotion",0,0);rig.Update(0);
                Require(!(bool)usingTool.Invoke(held,new object[]{entry}),id+" action pose did not release in locomotion");
            }
            finally {preview.Cleanup();}
        }
        report.AppendLine("PASS "+count+" action samples across four directions; primary palm error="+maxGrip+"m; support="+maxLeft+"m; max adjacent turn="+maxTurn+"deg; feet unchanged; watering cerat forward; signed axe/hammer face alignment; actual Animator action/locomotion state detection.");
        File.WriteAllText(Output+"-after.txt",report.ToString());
    }
}
