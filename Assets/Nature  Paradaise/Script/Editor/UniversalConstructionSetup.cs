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

/// <summary>Explicit construction-only authoring. Never saves or replaces the user's open scene.</summary>
[InitializeOnLoad]
public static class UniversalConstructionSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string Models=Root+"/mesh/Construction";
    const string Prefabs=Root+"/Prefabs/Construction";
    public const string Output="ArtSource/Construction_Kit/Previews";
    const string Request="ArtSource/Construction_Kit/apply.request";
    static double next;
    static UniversalConstructionSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
        const string render="ArtSource/Construction_Kit/render.request";
        if(File.Exists(render)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Move(render,Output+"/Render_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
            try{RenderReview();}catch(Exception e){File.WriteAllText(Output+"/RenderError.txt",e.ToString());Debug.LogException(e);}return;
        }
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Output);File.Move(Request,Output+"/Apply_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");
        try{Apply();}catch(Exception e){File.WriteAllText(Output+"/UnityError.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Nature Paradise/Construction/Apply Universal Open Top Kit")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var atlas=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials/TFP_Atlas_1A.mat");
        if(atlas==null)throw new InvalidOperationException("Toon Farm Pack atlas material missing.");
        var report=new StringBuilder("UNIVERSAL OPEN-TOP CONSTRUCTION IMPORT\n");
        GameObject Import(string name,bool capsule=false)
        {
            string path=Models+"/"+name+".fbx";
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new InvalidOperationException("Missing model "+path);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;importer.importAnimation=false;
            importer.animationType=ModelImporterAnimationType.None;importer.isReadable=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance=Object.Instantiate(source);instance.name=name;
            // Never overwrite the FBX root's unit/basis conversion when sizing a module at runtime.
            GameObject output=new GameObject(name);
            try
            {
                foreach(var r in instance.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(atlas,r.sharedMaterials.Length).ToArray();
                if(capsule)
                {
                    // Bake FBX basis once, then use clean identity local axes for the object contact rig.
                    string meshPath=Models+"/BuilderContactRig.asset";
                    var existing=AssetDatabase.LoadAllAssetsAtPath(meshPath).OfType<Mesh>().ToDictionary(m=>m.name);
                    bool hasFile=File.Exists(meshPath);
                    foreach(var f in instance.GetComponentsInChildren<MeshFilter>())
                    {
                        string part=f.name;var vertices=f.sharedMesh.vertices;
                        Matrix4x4 matrix=f.transform.localToWorldMatrix;
                        Vector3 center=matrix.MultiplyPoint3x4(f.sharedMesh.bounds.center);
                        var baked=Object.Instantiate(f.sharedMesh);baked.name=part;
                        var transformed=vertices.Select(v=>matrix.MultiplyPoint3x4(v)-center).ToArray();
                        bool limb=part.StartsWith("UpperArm")||part.StartsWith("Forearm")||part.StartsWith("Leg");
                        float length=transformed.Max(v=>v.y)-transformed.Min(v=>v.y);
                        if(limb)for(int i=0;i<transformed.Length;i++)transformed[i].y/=Mathf.Max(.001f,length);
                        if(matrix.determinant<0)
                        {
                            for(int sub=0;sub<baked.subMeshCount;sub++)
                            {
                                var indices=baked.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3)(indices[i+1],indices[i+2])=(indices[i+2],indices[i+1]);
                                baked.SetTriangles(indices,sub);
                            }
                        }
                        baked.vertices=transformed;baked.RecalculateNormals();baked.RecalculateBounds();
                        Mesh mesh;
                        if(existing.TryGetValue(part,out mesh)){EditorUtility.CopySerialized(baked,mesh);Object.DestroyImmediate(baked);EditorUtility.SetDirty(mesh);}
                        else{mesh=baked;if(!hasFile){AssetDatabase.CreateAsset(mesh,meshPath);hasFile=true;}else AssetDatabase.AddObjectToAsset(mesh,meshPath);}
                        mesh.vertices=mesh.vertices;mesh.UploadMeshData(false);
                        var go=new GameObject(part,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(output.transform,false);go.transform.localPosition=center;
                        if(limb)go.transform.localScale=new Vector3(1,length,1);
                        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=atlas;
                    }
                }
                else instance.transform.SetParent(output.transform,true);
                var renderers=output.GetComponentsInChildren<Renderer>();
                if(renderers.Length==0)throw new InvalidOperationException("Empty module "+name);
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                if(name=="TarpPanel" && (bounds.size.y<2.2f||bounds.size.x<1.9f||bounds.size.x>2.2f))throw new InvalidOperationException("Panel metre/basis conversion: "+bounds);
                if(capsule && (bounds.size.y<1.9f||bounds.size.y>2.1f))throw new InvalidOperationException("Worker metre/basis conversion: "+bounds);
                string target=Prefabs+"/"+name+".prefab";var result=PrefabUtility.SaveAsPrefabAsset(output,target);
                report.AppendLine("PASS "+name+" atlas shared; no colliders or gameplay scripts");return result;
            }
            finally{Object.DestroyImmediate(output);if(instance!=null)Object.DestroyImmediate(instance);}
        }
        var settings=ConstructionWorkerSettings.Load();
        if(settings==null)throw new InvalidOperationException("Shared construction worker settings missing.");
        settings.tarpPanel=Import("TarpPanel");settings.supportPost=Import("SupportPost");
        settings.workBench=Import("WorkBench");settings.lumberStack=Import("LumberStack");
        settings.toolCrate=Import("ToolCrate");settings.workingBoard=Import("WorkingBoard");
        settings.hammerPrefab=Import("Hammer");settings.sawPrefab=Import("Saw");
        // Keep an explicitly selected Humanoid/custom worker instead of reverting it on kit reimport.
        var capsule=Import("BuilderCapsule",true);
        if(settings.workerPrefab==null||settings.workerPrefab.name=="BuilderCapsule")
        {settings.workerPrefab=capsule;settings.workerScale=1.6f;}
        settings.panelWidth=2;settings.panelHeight=2.4f;settings.siteClearance=.65f;
        settings.revealSeconds=2.6f;settings.dustIntensity=.65f;
        settings.revealShader=Shader.Find("Nature Paradise/Construction Reveal");settings.dustShader=Shader.Find("Nature Paradise/Construction Dust");
        if(settings.revealShader==null||settings.dustShader==null)throw new InvalidOperationException("Construction shader missing.");
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        report.AppendLine("PASS shared Resources settings connected; no world/interior scene saved");
        File.WriteAllText(Output+"/UnityImportAudit.txt",report.ToString());
        EditorApplication.delayCall+=RenderReview;
        Debug.Log("[CONSTRUCTION KIT] Shared open-top kit and contact-driven capsule applied.");
    }
    [MenuItem("Nature Paradise/Construction/Render Universal Kit Review")]
    public static void RenderReview()
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();
        ConstructionProjectVisuals visuals=null;RenderTexture rt=null;
        var previewSkins=new System.Collections.Generic.List<Mesh>();
        try
        {
            var owner=new GameObject("Construction preview");SceneManager.MoveGameObjectToScene(owner,scene);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PlayerHouse/Exterior/HouseExterior_Lv1.prefab");
            // Resolve actual configured prefab instead of assuming a hand-named path.
            source=Resources.Load<BuildingDefinitionSO>("Buildings/Player House Building")?.GetLevel(1)?.completedPrefab??source;
            var anchor=owner.transform;var bounds=new Bounds(new Vector3(0,3,0),new Vector3(10,6,8));
            var settings=ConstructionWorkerSettings.Load();
            visuals=new ConstructionProjectVisuals(anchor,anchor,bounds,new Vector3(0,0,-6.1f),null,source,false,settings);
            visuals.Worker.SetPositionAndRotation(new Vector3(0,0,-6.1f),Quaternion.identity);
            visuals.SetStage(.5f);
            for(int f=0;f<=240;f++)visuals.Animate(false,true,f/30f);
            // Editor review has no elapsed gameplay frames; advance live emitters explicitly.
            foreach(var particles in owner.GetComponentsInChildren<ParticleSystem>())
                if(particles.main.loop)particles.Simulate(1.2f,true,false);
            // SubmitRenderRequest does not run a normal game frame. Snapshot the evaluated
            // bone pose on CPU so manual post-Animator work IK is visible, not a cached skin.
            foreach(var skin in visuals.Worker.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!skin.enabled)continue;
                var snapshot=new Mesh{name="Worker review evaluated pose"};skin.BakeMesh(snapshot,true);previewSkins.Add(snapshot);
                var frozen=new GameObject("EvaluatedSkin_PREVIEW_ONLY",typeof(MeshFilter),typeof(MeshRenderer));
                frozen.transform.SetParent(skin.transform,false);frozen.GetComponent<MeshFilter>().sharedMesh=snapshot;
                frozen.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            File.WriteAllText(Output+"/WorkerPreviewMethod.txt","Isolated edit-mode CPU skin snapshot of the actual evaluated construction pose (BakeMesh useScale=true). Side camera shows face/body/table direction. This is not a full Play Mode capture. No gameplay prefab, rotation, animation or user scene changes.\n");
            File.WriteAllText(Output+"/WorkerDiagnostic.txt",string.Join("\n",visuals.Worker.GetComponentsInChildren<Renderer>(true).Select(r=>$"{r.name} scene={r.gameObject.scene.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} bounds={r.bounds} material={r.sharedMaterial?.name} layer={r.gameObject.layer}")));
            foreach(var canvas in owner.GetComponentsInChildren<Canvas>())canvas.gameObject.SetActive(false);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.localScale=Vector3.one*3;
            var fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));fm.color=new Color(.22f,.31f,.21f);floor.GetComponent<Renderer>().sharedMaterial=fm;
            var light=new GameObject("Review sun",typeof(Light));SceneManager.MoveGameObjectToScene(light,scene);light.transform.rotation=Quaternion.Euler(50,-25,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;
            var camera=new GameObject("Review camera",typeof(Camera)).GetComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;
            camera.transform.position=new Vector3(16,15,-21);camera.transform.LookAt(new Vector3(0,1.5f,0));camera.orthographic=true;camera.orthographicSize=12;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.20f,.24f);
            rt=new RenderTexture(1400,1100,24);rt.Create();
            for(int frame=0;frame<4;frame++)
            {
                if(frame>0)
                {
                    visuals.Reveal(new[]{0,.25f,.65f,1f}[frame]);
                    foreach(var ps in owner.GetComponentsInChildren<ParticleSystem>())ps.Simulate(.3f,true,false);
                }
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Output+"/Unity_Stage_"+frame+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            // Side/front-quarter camera makes nose/body/table direction unambiguous.
            float workerHeight=2*settings.workerScale;
            camera.transform.position=visuals.Worker.position+new Vector3(4,2,1)*workerHeight;
            camera.transform.LookAt(visuals.Worker.position+new Vector3(0,workerHeight*.5f,.4f));camera.orthographicSize=workerHeight*.95f;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var previousRT=RenderTexture.active;RenderTexture.active=rt;var closeup=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            closeup.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);closeup.Apply();RenderTexture.active=previousRT;
            File.WriteAllBytes(Output+"/Unity_Worker.png",closeup.EncodeToPNG());Object.DestroyImmediate(closeup);
            Object.DestroyImmediate(fm);
        }
        finally
        {
            visuals?.Dispose();if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}
            foreach(var snapshot in previewSkins)Object.DestroyImmediate(snapshot);
            EditorSceneManager.ClosePreviewScene(scene);if(previous.IsValid())SceneManager.SetActiveScene(previous);
        }
    }
}
#endif
