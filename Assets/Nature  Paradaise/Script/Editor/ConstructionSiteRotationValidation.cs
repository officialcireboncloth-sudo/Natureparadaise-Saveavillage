#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Building/tarp/collision alignment at 0, 37 and 90 degrees. Never changes the placed building.</summary>
[InitializeOnLoad]
public static class ConstructionSiteRotationValidation
{
    const string Request="ArtSource/Construction_Kit/verify-site-rotation.request";
    const string Output="ArtSource/Construction_Kit/Previews";
    static ConstructionSiteRotationValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        File.Move(Request,Output+"/RotationRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Run();
    }
    [MenuItem("Nature Paradise/Construction/Validate Building Aligned Tarp Rotation")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder("ORIENTED BUILDING / TARP REGRESSION\n");int checks=0;
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture rt=null;ConstructionSitePresentation presentation=null;GameObject owner=null;
        void Check(bool yes,string label){if(!yes)throw new InvalidOperationException(label);checks++;report.AppendLine("PASS "+label);}
        try
        {
            var profile=ConstructionWorkerSettings.Load();var barn=Resources.Load<BuildingDefinitionSO>("Buildings/Barn Building");
            var source=barn.GetLevel(1).completedPrefab;
            var sun=new GameObject("Rotation review sun",typeof(Light));SceneManager.MoveGameObjectToScene(sun,scene);
            sun.transform.rotation=Quaternion.Euler(45,-25,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=1.1f;
            var camera=new GameObject("Rotation review camera",typeof(Camera)).GetComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);
            camera.scene=scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.19f);
            rt=new RenderTexture(1100,900,24);rt.Create();Vector3 referenceSize=default;
            foreach(int yaw in new[]{0,37,90})
            {
                owner=new GameObject("Barn reference yaw "+yaw);SceneManager.MoveGameObjectToScene(owner,scene);
                owner.transform.SetPositionAndRotation(new Vector3(20000,0,20000),Quaternion.Euler(0,yaw,0));
                owner.transform.localScale=new Vector3(1.15f,1,.85f);
                var position=owner.transform.position;var rotation=owner.transform.rotation;var scale=owner.transform.localScale;
                var layout=ConstructionSiteLayout.Measure(owner,owner.transform,barn,source,false);
                if(yaw==0)referenceSize=layout.LocalBounds.size;
                Check((layout.LocalBounds.size-referenceSize).magnitude<.02f,"Metre dimensions remain invariant at yaw "+yaw+" (no double-rotated AABB)");
                Check(Quaternion.Angle(layout.Rotation,rotation)<.01f,"Site heading matches actual building anchor yaw "+yaw);
                var measuredCentre=layout.ToWorld(new Vector3(layout.LocalBounds.center.x,0,layout.LocalBounds.center.z));
                var work=layout.ToWorld(new Vector3(layout.LocalBounds.center.x,0,layout.LocalBounds.min.z-profile.siteClearance-2));
                // Presentation roots are neutral scale just like the production ConstructionProjectVisuals root.
                var visualRoot=new GameObject("Neutral metre presentation root").transform;visualRoot.SetParent(owner.transform,false);
                visualRoot.SetPositionAndRotation(position,rotation);visualRoot.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
                presentation=new ConstructionSitePresentation(visualRoot,owner.transform,layout.WorldBounds,work,source,profile,layout);
                var reference=ConstructionProjectVisuals.VisualClone(source,owner.transform);
                reference.transform.localPosition=Vector3.zero;reference.transform.localRotation=Quaternion.identity;reference.transform.localScale=source.transform.localScale;
                var site=owner.GetComponentsInChildren<Transform>().First(t=>t.name=="Universal_OpenTop_Site");
                Check(Quaternion.Angle(site.rotation,rotation)<.01f,"Tarp does not stay at Quaternion.identity for yaw "+yaw);
                Check(Vector3.Distance(site.position,measuredCentre)<.02f,"Offset model pivot gives correct screen centre yaw "+yaw);
                foreach(var wall in owner.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="ConstructionWallBarrier"))
                {
                    float alignment=Mathf.Max(Mathf.Abs(Vector3.Dot(wall.transform.right,owner.transform.right)),Mathf.Abs(Vector3.Dot(wall.transform.right,owner.transform.forward)));
                    Check(alignment>.999f,"Physical barrier follows building axes yaw "+yaw);
                }
                var gates=owner.GetComponentsInChildren<Transform>().Where(t=>t.name=="ConstructionExit_AlwaysOpen").ToArray();
                Check(gates.Length==2,"Both safe exits retained yaw "+yaw);
                Check(gates.All(g=>Mathf.Abs(Vector3.Dot(g.forward,owner.transform.forward))>.999f),"Exit approach rotates with the building yaw "+yaw);
                foreach(var renderer in reference.GetComponentsInChildren<Renderer>(true))
                {
                    var b=renderer.localBounds;
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    {
                        var point=layout.ToLocal(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                        Check(point.x>=layout.LocalBounds.min.x-.02f&&point.x<=layout.LocalBounds.max.x+.02f&&point.z>=layout.LocalBounds.min.z-.02f&&point.z<=layout.LocalBounds.max.z+.02f,"Actual Barn mesh contained in oriented footprint yaw "+yaw);
                    }
                }
                Check(owner.transform.position==position&&owner.transform.rotation==rotation&&owner.transform.localScale==scale,"Real building anchor unchanged yaw "+yaw);
                float span=Mathf.Max(layout.LocalBounds.size.x,layout.LocalBounds.size.z);
                camera.orthographicSize=span*.85f;camera.transform.position=measuredCentre+new Vector3(1,1,-1.3f)*span;
                camera.transform.LookAt(measuredCentre+Vector3.up*2);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var previousRT=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1100,900,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1100,900),0,0);image.Apply();RenderTexture.active=previousRT;
                File.WriteAllBytes(Output+"/Barn_Tarp_Aligned_Yaw"+yaw+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
                presentation.Dispose();presentation=null;Object.DestroyImmediate(owner);owner=null;
            }
            report.AppendLine("AUDIT_PASS checks="+checks+"; actual Barn + collider geometry at 0/37/90 degrees, editor fixtures only. No user scenes/saves changed.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);}
        finally
        {
            presentation?.Dispose();if(owner!=null)Object.DestroyImmediate(owner);
            if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);if(previous.IsValid())SceneManager.SetActiveScene(previous);
            File.WriteAllText(Output+"/SiteRotationValidation.txt",report.ToString());
        }
    }
}
#endif
