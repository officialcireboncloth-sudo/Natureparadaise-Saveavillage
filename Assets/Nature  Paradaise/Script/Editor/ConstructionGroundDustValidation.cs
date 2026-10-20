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

/// <summary>Sloping ground below an elevated anchor, plus actual rendered particle pixels.</summary>
[InitializeOnLoad]
public static class ConstructionGroundDustValidation
{
    const string Request="ArtSource/Construction_Kit/verify-ground-dust.request";
    const string Output="ArtSource/Construction_Kit/Previews";
    static ConstructionGroundDustValidation(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        Directory.CreateDirectory(Output);File.Move(Request,Output+"/GroundDustRequest_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Run();
    }
    [MenuItem("Nature Paradise/Construction/Validate Terrain Contact And Visible Dust")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var report=new StringBuilder("TERRAIN CONTACT AND RENDERED DUST — ISOLATED EDITOR FIXTURE\n");int checks=0;
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        ConstructionSitePresentation presentation=null;TerrainData data=null;ConstructionWorkerSettings profile=null;RenderTexture rt=null;
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;report.AppendLine("PASS "+message);}
        GameObject InScene(string name){var o=new GameObject(name);SceneManager.MoveGameObjectToScene(o,scene);return o;}
        try
        {
            data=new TerrainData{heightmapResolution=33,size=new Vector3(24,4,24)};
            var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=.1f+x/32f*.18f+z/32f*.1f;
            data.SetHeights(0,0,heights);
            var terrainObject=Terrain.CreateTerrainGameObject(data);SceneManager.MoveGameObjectToScene(terrainObject,scene);
            terrainObject.transform.position=new Vector3(27000,0,27000);var terrain=terrainObject.GetComponent<Terrain>();
            float Ground(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;
            var owner=InScene("Elevated building anchor, not moved");owner.transform.SetPositionAndRotation(new Vector3(27012,3.5f,27012),Quaternion.Euler(0,37,0));
            profile=Object.Instantiate(ConstructionWorkerSettings.Load());
            var originalMesh=profile.tarpPanel.GetComponentInChildren<MeshFilter>().sharedMesh;
            var originalVertices=originalMesh.vertices;var bounds=new Bounds(new Vector3(0,4,0),new Vector3(8,8,10));
            var layout=new ConstructionSiteLayout(owner.transform.position,owner.transform.rotation,bounds);
            Vector3 work=layout.ToWorld(new Vector3(0,0,-8));work.y=Ground(work);
            Physics.SyncTransforms();
            presentation=new ConstructionSitePresentation(owner.transform,owner.transform,layout.WorldBounds,work,null,profile,layout);
            foreach(var panel in owner.GetComponentsInChildren<Transform>().Where(t=>t.name=="SideTarp"))
            {
                float minimum=float.MaxValue,maximum=float.MinValue;
                foreach(var filter in panel.GetComponentsInChildren<MeshFilter>())
                {
                    Check(filter.sharedMesh!=originalMesh,"Panel uses private conforming mesh, not shared asset mutation");
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var world=filter.transform.TransformPoint(vertex);float offset=world.y-Ground(world);
                        minimum=Mathf.Min(minimum,offset);maximum=Mathf.Max(maximum,offset);
                    }
                }
                Check(Mathf.Abs(minimum+.06f)<.015f&&maximum<=profile.screenHeight+.015f,"Fabric hem contacts slope despite elevated building anchor");
            }
            foreach(var post in owner.GetComponentsInChildren<Transform>().Where(t=>t.name=="SidePost"))
                Check(Mathf.Abs(post.position.y-Ground(post.position)+.04f)<.015f&&Vector3.Dot(post.up,Vector3.up)>.999f,"Post grounded and upright");
            Check(owner.transform.position.y==3.5f,"Authoritative building/player transforms not moved");
            Check(originalMesh.vertices.SequenceEqual(originalVertices),"Source FBX mesh vertices unchanged");
            presentation.Tick(true,false);
            var systems=owner.GetComponentsInChildren<ParticleSystem>();var workDust=systems.First(p=>p.name=="WorkDust");
            var clouds=systems.Where(p=>p.name.StartsWith("SiteDust")).ToArray();
            Check(clouds.Length==4,"Four overlapping interior cloud quadrants");
            foreach(var cloud in clouds)
            {
                var local=owner.transform.InverseTransformPoint(cloud.transform.position);var extent=cloud.shape.scale/2;
                Check(Mathf.Abs(local.x)+extent.x<bounds.extents.x+profile.siteClearance&&Mathf.Abs(local.z)+extent.z<bounds.extents.z+profile.siteClearance,"Entire emission box lies INSIDE tarp footprint");
            }
            Check(clouds.Min(c=>owner.transform.InverseTransformPoint(c.transform.position).x)<0&&clouds.Max(c=>owner.transform.InverseTransformPoint(c.transform.position).x)>0&&
                clouds.Min(c=>owner.transform.InverseTransformPoint(c.transform.position).z)<0&&clouds.Max(c=>owner.transform.InverseTransformPoint(c.transform.position).z)>0,"Clouds cover all four sides of the centre");
            foreach(var p in systems)
            {
                float ground=Ground(p.transform.position);float clip=p.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetFloat("_GroundY");
                Check(Mathf.Abs(clip-ground+.12f)<.015f&&clip<owner.transform.position.y-1,"Dust shader uses local ground, not elevated building pivot");
            }
            Check(workDust.particleCount>0&&clouds.All(p=>p.particleCount>0),"Immediate populated interior clouds when construction begins");
            foreach(var p in systems.Where(p=>p.main.loop))p.Simulate(.8f,true,false);
            // Pixel proof of the real custom dust shader, isolated from opaque scene geometry.
            terrain.drawHeightmap=false;
            foreach(var r in owner.GetComponentsInChildren<Renderer>())r.enabled=r is ParticleSystemRenderer;
            foreach(var p in systems)p.GetComponent<Renderer>().enabled=clouds.Contains(p);
            var camera=InScene("Interior dust pixel proof camera").AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=5;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.24f,.2f);
            camera.transform.position=owner.transform.position+Vector3.up*15;camera.transform.rotation=Quaternion.LookRotation(Vector3.down,owner.transform.forward);
            rt=new RenderTexture(512,512,24);rt.Create();
            Color32[] Capture(bool visible,string filename)
            {
                foreach(var cloud in clouds)cloud.GetComponent<Renderer>().enabled=visible;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var previousRT=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(512,512,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();RenderTexture.active=previousRT;
                var pixels=image.GetPixels32();if(filename!=null)File.WriteAllBytes(Output+"/"+filename,image.EncodeToPNG());Object.DestroyImmediate(image);return pixels;
            }
            var baseline=Capture(false,null);var smoke=Capture(true,"TerrainDust_ActualShader.png");int changed=0,centreChanged=0;
            for(int i=0;i<smoke.Length;i++)if(Mathf.Abs(smoke[i].r-baseline[i].r)+Mathf.Abs(smoke[i].g-baseline[i].g)+Mathf.Abs(smoke[i].b-baseline[i].b)>12)changed++;
            for(int y=128;y<384;y++)for(int x=128;x<384;x++){int i=y*512+x;if(Mathf.Abs(smoke[i].r-baseline[i].r)+Mathf.Abs(smoke[i].g-baseline[i].g)+Mathf.Abs(smoke[i].b-baseline[i].b)>12)centreChanged++;}
            Check(changed>200,"Custom dust shader produces visible pixels: "+changed);
            Check(centreChanged>256*256*.25f,"Actual rendered clouds obscure the MIDDLE, covered centre pixels: "+centreChanged);
            presentation.Tick(false,false);Check(workDust.emission.rateOverTime.constant==0&&clouds.All(p=>p.emission.rateOverTime.constant==0),"Work and interior emission stop with builder");
            presentation.Dispose();presentation=null;
            report.AppendLine("AUDIT_PASS checks="+checks+"; native terrain/mesh/particle/render tests, not full gameplay Play Mode. No scene/save/completion writes.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);}
        finally
        {
            presentation?.Dispose();if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);
            if(data!=null)Object.DestroyImmediate(data);if(profile!=null)Object.DestroyImmediate(profile);
            if(previous.IsValid())SceneManager.SetActiveScene(previous);Directory.CreateDirectory(Output);File.WriteAllText(Output+"/TerrainDustValidation.txt",report.ToString());
        }
    }
}
#endif
