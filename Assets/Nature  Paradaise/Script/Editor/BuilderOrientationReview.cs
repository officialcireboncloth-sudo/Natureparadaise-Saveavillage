#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Isolated actual-Animator review from table/front, back and side. No scene/save writes.</summary>
[InitializeOnLoad]
public static class BuilderOrientationReview
{
    const string Request="ArtSource/Builder_Worker/review-rotation.request";
    const string Output="ArtSource/Builder_Worker/Previews";
    static BuilderOrientationReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Move(Request,Output+"/RotationRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        try{Run();}catch(Exception e){File.WriteAllText(Output+"/RotationError.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Nature Paradise/Construction/Review Builder Facing From Multiple Sides")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before isolated review.");
        var scene=EditorSceneManager.NewPreviewScene();var previous=SceneManager.GetActiveScene();
        ConstructionHumanoidMotion motion=null;RenderTexture rt=null;Material groundMat=null;
        var report=new StringBuilder("ACTUAL BUILDER ORIENTATION REVIEW\nTable is +Z of worker; camera Front is on table side (+Z).\n");
        try
        {
            var settings=ConstructionWorkerSettings.Load();float scale=settings.workerScale;
            var owner=new GameObject("OrientationReview");SceneManager.MoveGameObjectToScene(owner,scene);
            var worker=new GameObject("Worker").transform;worker.SetParent(owner.transform,false);worker.localScale=Vector3.one*scale;
            var model=ConstructionProjectVisuals.VisualClone(settings.workerPrefab,worker);
            var animator=model.GetComponentInChildren<Animator>();
            Transform Prop(GameObject prefab,string name,Vector3 position)
            {
                var t=ConstructionProjectVisuals.VisualClone(prefab,owner.transform).transform;t.name=name;
                t.SetPositionAndRotation(position*scale,Quaternion.identity);t.localScale=Vector3.one*scale;return t;
            }
            Prop(settings.workBench,"Bench",new Vector3(0,0,.67f));
            var board=Prop(settings.workingBoard,"Board",new Vector3(0,1.04f,.67f));
            motion=new ConstructionHumanoidMotion(worker,animator,settings);motion.BindBoard(board);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(floor,scene);
            groundMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMat.color=new Color(.20f,.24f,.27f);floor.GetComponent<Renderer>().sharedMaterial=groundMat;
            var sun=new GameObject("Sun",typeof(Light));SceneManager.MoveGameObjectToScene(sun,scene);
            sun.transform.rotation=Quaternion.Euler(50,-35,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=1.1f;
            var cam=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,scene);
            cam.scene=scene;cam.orthographic=true;cam.orthographicSize=1.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.15f,.19f);
            rt=new RenderTexture(900,900,24);rt.Create();
            void Snapshot(string label)
            {
                var l=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var r=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var head=animator.GetBoneTransform(HumanBodyBones.Head);
                var torso=Vector3.ProjectOnPlane(Vector3.Cross(r.position-l.position,head.position-hips.position),Vector3.up).normalized;
                var lf=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var lt=animator.GetBoneTransform(HumanBodyBones.LeftToes);var rtBone=animator.GetBoneTransform(HumanBodyBones.RightToes);
                var feet=Vector3.ProjectOnPlane((lt.position-lf.position)+(rtBone.position-rf.position),Vector3.up).normalized;
                report.AppendLine(label+" torsoForward="+torso.ToString("F3")+" dotTable="+Vector3.Dot(torso,Vector3.forward).ToString("F3")+" feetForward="+feet.ToString("F3"));
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                report.AppendLine(label+" hand before render="+hand.position.ToString("F3"));
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
                report.AppendLine(label+" skin references evaluated hand="+skin.bones.Contains(hand));
                void Render(string suffix)
                {
                    foreach(var view in new[]{("Front",new Vector3(2.6f,1.8f,4)),("Side",new Vector3(4,1.5f,0)),("Back",new Vector3(2.6f,1.8f,-4))})
                    {
                        cam.transform.position=view.Item2;cam.transform.LookAt(new Vector3(0,.85f,.15f));
                        RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                        report.AppendLine(label+" "+view.Item1+suffix+" hand after render="+hand.position.ToString("F3"));
                        var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(900,900,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();RenderTexture.active=old;
                        File.WriteAllBytes(Output+"/Facing_"+label+"_"+view.Item1+suffix+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
                    }
                }
                Render("");
                var baked=new Mesh();var cpu=new GameObject("CPU skin comparison",typeof(MeshFilter),typeof(MeshRenderer));
                try
                {
                    // Compensate the FBX renderer Transform's unit scale, per BakeMesh API.
                    skin.BakeMesh(baked,true);cpu.transform.SetParent(skin.transform,false);cpu.GetComponent<MeshFilter>().sharedMesh=baked;
                    report.AppendLine(label+" native bounds="+skin.bounds+" baked local="+baked.bounds+" lossyScale="+skin.transform.lossyScale);
                    cpu.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;Render("_SkinBaked");
                }
                finally{skin.enabled=true;Object.DestroyImmediate(cpu);Object.DestroyImmediate(baked);}
            }
            for(int f=0;f<=30;f++)motion.Animate(false,false,f/30f);Snapshot("Idle");
            for(int f=31;f<=300;f++)motion.Animate(false,true,f/30f);Snapshot("Hammer");
            for(int f=301;f<=420;f++)motion.Animate(false,true,f/30f);Snapshot("Saw");
            File.WriteAllText(Output+"/RotationReview.txt",report.ToString());
        }
        finally
        {
            motion?.Dispose();if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}if(groundMat!=null)Object.DestroyImmediate(groundMat);
            EditorSceneManager.ClosePreviewScene(scene);if(previous.IsValid())SceneManager.SetActiveScene(previous);
        }
    }
}
#endif
