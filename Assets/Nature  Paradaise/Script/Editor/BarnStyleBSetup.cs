#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Explicit Barn B import. Existing prefab roots, interior bindings and scene references are retained.</summary>
public static class BarnStyleBSetup
{
    public const string Root = "Assets/Nature  Paradaise";
    public const string Models = Root + "/mesh/Buildings/BarnStyle_B";
    public const string Atlas = Root + "/Textures/Buildings/BarnStyle_B/TFP_Atlas_BarnB_Wood_1A.mat";
    public const string Output = "Documentation/BarnB-Unity-2026-10-21";
    static readonly float[] Scales = {1.3125f, 1.53125f, 1.75f, 1.96875f, 2.1875f};
    static readonly int[] Capacity = {4,8,14,20};
    static readonly int[] ExteriorTriangles = {4446,3812,2966};
    static Material atlas;
    static StringBuilder report;

    [MenuItem("Nature Paradise/Barn/Apply Barn B Wood and Roof")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before applying Barn B.");
        Directory.CreateDirectory(Models); Directory.CreateDirectory(Output);
        report = new StringBuilder("BARN B UNITY APPLY\n");
        atlas = AssetDatabase.LoadAssetAtPath<Material>(Atlas);
        Check(atlas && atlas.shader && atlas.shader.isSupported, "Existing Toon shader and Barn B atlas material supported");
        var texture = atlas.GetTexture("_TextureSample") as Texture2D;
        Check(texture && texture.width == 2048 && texture.height == 2048, "One 2048 x 2048 painted atlas");
        string revision = Output + "/Before_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        Directory.CreateDirectory(revision);
        foreach (string path in Enumerable.Range(1,5).Select(i=>Exterior(i)).Concat(Enumerable.Range(1,4).Select(i=>Interior(i))))
            File.Copy(path, revision + "/" + Path.GetFileName(path));
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            ApplyExteriors();
            for (int i=1;i<=4;i++) ApplyInterior(i,preview);
            ValidatePrefabs(preview);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        AssetDatabase.SaveAssets();
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Exterior(1)),"Unity_BarnB_Exterior_Lv1.png",false,0);
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Exterior(4)),"Unity_BarnB_Exterior_Lv4.png",false,0);
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Exterior(3)),"Unity_BarnB_Exterior_Medium.png",false,1);
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Exterior(3)),"Unity_BarnB_Exterior_Low.png",false,2);
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Interior(1)),"Unity_BarnB_Interior_Lv1.png",true);
        RenderPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Interior(4)),"Unity_BarnB_Interior_Lv4.png",true);
        foreach (string path in AssetDatabase.FindAssets("t:Model",new[]{Models}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path); importer.isReadable=false; importer.SaveAndReimport();
        }
        report.AppendLine("BARN_B_UNITY_APPLY_PASS\nExisting scenes, building cost/capacity, placement footprint and player saves were not changed.");
        File.WriteAllText(Output+"/ApplyValidation.txt",report.ToString());
        Debug.Log("BARN_B_UNITY_APPLY_PASS");
    }

    public static string Exterior(int level) => Root+"/Prefabs/Barn/Exterior/BarnExterior_Lv"+level+".prefab";
    public static string Interior(int level) => Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv"+level+".prefab";
    static void Check(bool value,string message)
    { if(!value)throw new InvalidOperationException("Barn B: "+message);report.AppendLine("PASS "+message); }
    static Transform Find(Transform root,string name)
    {
        var all=root.GetComponentsInChildren<Transform>(true);var result=all.SingleOrDefault(t=>t.name==name);
        if(!result)throw new InvalidOperationException("Missing "+name+" under "+root.name+"; available: "+string.Join(", ",all.Where(t=>t.GetComponent<MeshFilter>()).Select(t=>t.name)));
        return result;
    }
    static Transform Child(Transform parent,string name)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
    static GameObject Import(string filename)
    {
        string path=Models+"/"+filename;
        File.Copy("ArtSource/BarnStyle_B/Exports/"+filename,path,true);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.globalScale=1; importer.useFileScale=true; importer.bakeAxisConversion=true;
        importer.importAnimation=false; importer.animationType=ModelImporterAnimationType.None;
        importer.importCameras=false; importer.importLights=false; importer.addCollider=false;
        importer.isReadable=true; importer.meshCompression=ModelImporterMeshCompression.Off;
        importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        foreach(string name in new[]{"TFP_Atlas_BarnB_Wood_1A","TFP_Atlas_BarnB_Wood_1A_DullMetal"})
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),atlas);
        importer.SaveAndReimport();
        var result=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Check(result && result.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),filename+" all source material slots remapped to shared atlas");
        return result;
    }
    static void ApplyExteriors()
    {
        var models=new[]{"High","Medium","Low"}.Select(n=>Import("BarnExterior_B_"+n+".fbx")).ToArray();
        var bellSource=Import("BarnBell_B.fbx");
        var filters=models.Select(m=>m.GetComponentsInChildren<MeshFilter>(true).Single()).ToArray();
        var meshes=new Mesh[3];
        for(int i=0;i<3;i++)
        {
            var source=filters[i].sharedMesh;var mesh=new Mesh{name="BarnB_"+new[]{"High","Medium","Low"}[i]};
            mesh.CombineMeshes(Enumerable.Range(0,source.subMeshCount).Select(sub=>new CombineInstance{mesh=source,subMeshIndex=sub,transform=Matrix4x4.identity}).ToArray(),true,true);
            meshes[i]=StoreMesh(Models+"/BarnExterior_B_Runtime.asset",mesh);
            Check(meshes[i].triangles.Length/3==ExteriorTriangles[i],"Stored exterior LOD "+i+" contains "+ExteriorTriangles[i]+" triangles");
        }
        Vector3 entrance=Find(models[0].transform,"BarnB_Entrance").position;
        Check(Mathf.Abs(entrance.z)>3 && Mathf.Abs(entrance.x)<.01f,"Imported front entrance marker is aligned to Z");
        Quaternion yaw=Quaternion.Euler(0,entrance.z<0?180:0,0);
        for(int i=0;i<3;i++)Check(filters[i].sharedMesh.triangles.Length/3==ExteriorTriangles[i],"Exterior "+i+" authored triangle count "+ExteriorTriangles[i]);
        for(int level=1;level<=5;level++)
        {
            var root=PrefabUtility.LoadPrefabContents(Exterior(level));
            try
            {
                var authoring=root.GetComponent<BarnExteriorAuthoring>();
                Check(authoring!=null,"Lv"+level+" exterior authoring retained");
                var model=authoring.ModelRoot;
                foreach(Transform child in model.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var group=model.GetComponent<LODGroup>()??model.gameObject.AddComponent<LODGroup>();
                model.localRotation=yaw;model.localPosition=Vector3.zero;model.localScale=Vector3.one*Scales[level-1];
                var lods=new LOD[3];
                for(int i=0;i<3;i++)
                {
                    var child=Child(model,"BarnB_"+new[]{"High","Medium","Low"}[i]);
                    child.localPosition=filters[i].transform.position;child.localRotation=filters[i].transform.rotation;child.localScale=filters[i].transform.lossyScale;
                    child.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[i];
                    var renderer=child.gameObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials=new[]{atlas};
                    renderer.shadowCastingMode=ShadowCastingMode.On;
                    lods[i]=new LOD(new[]{.28f,.10f,.012f}[i],new[]{renderer}){fadeTransitionWidth=.15f};
                }
                group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(lods);group.RecalculateBounds();
                float scale=Scales[level-1];var collision=authoring.BuildingCollision;
                collision.center=yaw*Find(models[0].transform,"BarnB_BodyCentre").position*scale;
                collision.size=new Vector3(5.5f,3,5)*scale;collision.enabled=true;collision.isTrigger=false;
                foreach(var extra in root.GetComponentsInChildren<BoxCollider>(true).Where(c=>c!=collision).ToArray())Object.DestroyImmediate(extra.gameObject);
                foreach(var spec in new[]{("PorchSideCentre",new Vector3(.15f,2.1f,5)),("PorchBackCentre",new Vector3(1.5f,2.1f,.15f)),("PorchPostCentre",new Vector3(.2f,2,.2f))})
                {
                    var collider=Child(root.transform,"BarnB_"+spec.Item1+"_Collider").gameObject.AddComponent<BoxCollider>();
                    collider.center=yaw*Find(models[0].transform,"BarnB_"+spec.Item1).position*scale;collider.size=spec.Item2*scale;
                }
                authoring.Entrance.localPosition=yaw*entrance*scale;authoring.Entrance.localRotation=Quaternion.identity;
                Check(authoring.Entrance.localPosition.z>0,"Lv"+level+" entrance follows placement +Z");
                authoring.Configure(model,collision,authoring.Entrance);
                authoring.ConfigureBell(CreateBellRig(model,bellSource,true,entrance));
                PrefabUtility.SaveAsPrefabAsset(root,Exterior(level));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var fallback=new GameObject("AnimalBell");
        try
        {
            var rig=CreateBellRig(fallback.transform,bellSource,false,entrance);
            rig.transform.localRotation=yaw;rig.transform.localScale=Vector3.one*Scales[0];
            rig.transform.localPosition=-(yaw*rig.Approach.localPosition)*Scales[0];
            // Resource component on the prefab root for coops/older exteriors.
            var component=fallback.AddComponent<AnimalBellVisual>();
            component.Configure(rig.SwingPivot,rig.PromptAnchor,rig.Approach);
            Object.DestroyImmediate(rig);
            PrefabUtility.SaveAsPrefabAsset(fallback,Root+"/Resources/Buildings/AnimalBell.prefab");
        }
        finally{Object.DestroyImmediate(fallback);}
    }

    static AnimalBellVisual CreateBellRig(Transform parent,GameObject source,bool mounted,Vector3 entrance)
    {
        var root=Child(parent,"BarnB_AuthoredBell");
        Vector3 pivotPosition=Find(source.transform,"BarnB_BellPivot").position;
        var pivot=Child(root,"BellSwingPivot");pivot.localPosition=pivotPosition;
        foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))
        {
            string name=filter.name;
            var child=Child(name.Contains("Swing")?pivot:root,name);
            child.localPosition=filter.transform.position-(name.Contains("Swing")?pivotPosition:Vector3.zero);
            child.localRotation=filter.transform.rotation;child.localScale=filter.transform.lossyScale;
            var mesh=new Mesh{name=name};
            mesh.CombineMeshes(Enumerable.Range(0,filter.sharedMesh.subMeshCount).Select(i=>new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=Matrix4x4.identity}).ToArray(),true,true);
            child.gameObject.AddComponent<MeshFilter>().sharedMesh=StoreMesh(Models+"/BarnBell_B_Runtime.asset",mesh);
            child.gameObject.AddComponent<MeshRenderer>().sharedMaterial=atlas;
            if(mounted&&!name.Contains("Swing"))child.gameObject.SetActive(false);
        }
        var prompt=Child(root,"BellPrompt");prompt.localPosition=Find(source.transform,"BarnB_BellPrompt").position;
        var approach=Child(root,"BellApproach");approach.localPosition=Find(source.transform,"BarnB_BellApproach").position;
        approach.localRotation=Quaternion.LookRotation(new Vector3(entrance.x,0,entrance.z).normalized);
        var visual=root.gameObject.AddComponent<AnimalBellVisual>();visual.Configure(pivot,prompt,approach);
        return visual;
    }
    static string Key(MeshFilter f)
    {
        string n=f.name;
        var match=System.Text.RegularExpressions.Regex.Match(n,"^FeedTrough_(\\d+)");if(match.Success)return "Trough_"+match.Groups[1].Value;
        match=System.Text.RegularExpressions.Regex.Match(n,"^FeedFill_(\\d+)_(\\d+)");if(match.Success)return "Fill_"+match.Groups[1].Value+"_"+match.Groups[2].Value;
        if(n.Contains("_Floor"))return "Floor";
        if(n.StartsWith("N01_")||n.Contains("FrontCutaway"))return "Shell";
        if(n.StartsWith("N03_")||n.StartsWith("N04_")||n.StartsWith("N06_"))return "Machine_"+n;
        if(n.StartsWith("N05_")||n.StartsWith("DropPipe")||n.Contains("Connection")||n.Contains("TankBridge"))return "Distribution";
        return "PropsAndStraw";
    }
    static Mesh Combine(IEnumerable<MeshFilter> filters,Transform target,string name)
    {
        var instances=new List<CombineInstance>();
        foreach(var filter in filters)for(int submesh=0;submesh<filter.sharedMesh.subMeshCount;submesh++)
            instances.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=submesh,transform=target.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt16};mesh.CombineMeshes(instances.ToArray(),true,true);
        mesh.RecalculateBounds();MeshUtility.Optimize(mesh);return mesh;
    }
    static Mesh StoreMesh(string path,Mesh mesh)
    {
        mesh.RecalculateTangents();
        var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
        if(existing)
        {
            existing.Clear();EditorUtility.CopySerialized(mesh,existing);
            existing.UploadMeshData(false);
            Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
        if(!File.Exists(path))AssetDatabase.CreateAsset(mesh,path);else AssetDatabase.AddObjectToAsset(mesh,path);
        return mesh;
    }
    static int AddOpenBackfaces(Mesh mesh)
    {
        var vertices=mesh.vertices;var triangles=mesh.triangles;int count=triangles.Length/3;
        string Point(int n){var v=vertices[n];return Mathf.RoundToInt(v.x*100000)+","+Mathf.RoundToInt(v.y*100000)+","+Mathf.RoundToInt(v.z*100000);}
        string Edge(int a,int b){string x=Point(a),y=Point(b);return string.CompareOrdinal(x,y)<0?x+"|"+y:y+"|"+x;}
        var edges=new Dictionary<string,List<int>>();var keys=new string[count,3];
        for(int t=0;t<count;t++)for(int k=0;k<3;k++){string edge=Edge(triangles[t*3+k],triangles[t*3+(k+1)%3]);keys[t,k]=edge;if(!edges.TryGetValue(edge,out var adjacent))edges[edge]=adjacent=new List<int>();adjacent.Add(t);}
        var done=new HashSet<int>();var reverse=new List<int>();
        for(int t=0;t<count;t++)
        {
            if(done.Contains(t))continue;var component=new List<int>();var queue=new Queue<int>();queue.Enqueue(t);done.Add(t);bool open=false;
            while(queue.Count>0){int current=queue.Dequeue();component.Add(current);for(int k=0;k<3;k++){var adjacent=edges[keys[current,k]];if(adjacent.Count==1)open=true;foreach(int other in adjacent)if(done.Add(other))queue.Enqueue(other);}}
            if(open)reverse.AddRange(component);
        }
        if(reverse.Count==0)return 0;
        var vlist=vertices.ToList();var normals=mesh.normals.ToList();var uv=mesh.uv.ToList();var output=triangles.ToList();var map=new Dictionary<int,int>();
        int Back(int old){if(map.TryGetValue(old,out int index))return index;index=vlist.Count;map[old]=index;vlist.Add(vertices[old]);normals.Add(-mesh.normals[old]);uv.Add(mesh.uv[old]);return index;}
        foreach(int t in reverse){output.Add(Back(triangles[t*3+2]));output.Add(Back(triangles[t*3+1]));output.Add(Back(triangles[t*3]));}
        mesh.Clear();mesh.SetVertices(vlist);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(output,0);mesh.RecalculateBounds();return reverse.Count;
    }
    static void ApplyInterior(int level,Scene preview)
    {
        var imported=Import("BarnInterior_Lv"+level+".fbx");
        var source=(GameObject)PrefabUtility.InstantiatePrefab(imported,preview);
        source.transform.rotation=Quaternion.Euler(-90,0,0)*source.transform.rotation;
        // Select orientation by the imported authored entrance and floor extents,
        // independent of FBX importer node-axis conventions.
        Quaternion initial=source.transform.rotation,chosen=initial;float best=float.PositiveInfinity;
        float width=new[]{12,16,20,24}[level-1],depth=new[]{10,12,14,16}[level-1];
        foreach(int pitch in new[]{0,90,180,270})foreach(int yaw in new[]{0,90,180,270})foreach(int roll in new[]{0,90,180,270})
        {
            source.transform.rotation=Quaternion.Euler(pitch,yaw,roll)*initial;
            var bounds=BoundsOf(source);
            float error=Mathf.Abs(bounds.min.y+.18f)+Mathf.Abs(bounds.max.y-3.4f)+Mathf.Abs(bounds.center.x)+Mathf.Abs(bounds.center.z-depth/2)+Mathf.Abs(bounds.size.x-width-.26f)+Mathf.Abs(bounds.size.z-depth-.23f);
            if(error<best){best=error;chosen=source.transform.rotation;}
        }
        source.transform.rotation=chosen;Check(best<.3f,"Lv"+level+" interior import metre scale and rear +Z (error "+best+")");
        var filters=source.GetComponentsInChildren<MeshFilter>(true);int input=filters.Sum(f=>f.sharedMesh.triangles.Length/3);
        Check(input==new[]{6252,8006,9996,12307}[level-1],"Lv"+level+" full Barn B geometry, including every material submesh");
        var root=PrefabUtility.LoadPrefabContents(Interior(level));var scale=root.transform.localScale;
        try
        {
            root.transform.localScale=Vector3.one;
            var binding=root.GetComponent<BarnInteriorLayout>();
            Check(binding && binding.FeedingSlots.Length==Capacity[level-1],"Lv"+level+" existing feeding references retained");
            Check(root.GetComponentsInChildren<MeshFilter>(true).All(f=>f.sharedMesh),"Lv"+level+" previous mesh dependencies resolve before replacement");
            string meshPath=Models+"/BarnInterior_B_Lv"+level+"_Runtime.asset";int backs=0;
            var usedTargets=new HashSet<Transform>();
            foreach(var group in filters.GroupBy(Key))
            {
                string targetName=group.Key.StartsWith("Fill_")?group.Key:group.Key+"_Geometry";
                Transform target;
                if(group.Key.StartsWith("Machine_"))
                {
                    string Canonical(string name)=>System.Text.RegularExpressions.Regex.Replace(name,@"\.\d+(?=_Geometry$)","");
                    var sourceBounds=new Bounds();bool first=true;
                    foreach(var f in group)foreach(var v in f.sharedMesh.vertices)
                    {var point=f.transform.TransformPoint(v);if(first){sourceBounds=new Bounds(point,Vector3.zero);first=false;}else sourceBounds.Encapsulate(point);}
                    var candidates=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>!usedTargets.Contains(f.transform)&&Canonical(f.name)==Canonical(targetName)).ToArray();
                    Check(candidates.Length>0,"Lv"+level+" machine role retained: "+Canonical(targetName));
                    target=candidates.OrderBy(f=>Vector3.SqrMagnitude(f.transform.TransformPoint(f.sharedMesh.bounds.center)-sourceBounds.center)).First().transform;
                }
                else target=Find(root.transform,targetName);
                usedTargets.Add(target);
                var mesh=Combine(group,target,"BarnB_Lv"+level+"_"+group.Key);
                if(group.Key.StartsWith("Machine_"))backs+=AddOpenBackfaces(mesh);
                target.GetComponent<MeshFilter>().sharedMesh=StoreMesh(meshPath,mesh);
                target.GetComponent<MeshRenderer>().sharedMaterials=new[]{atlas};
            }
            Check(root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3)==input+backs,"Lv"+level+" optimized mesh preserves source triangles and open machine backs");
            root.transform.localScale=scale;
            PrefabUtility.SaveAsPrefabAsset(root,Interior(level));
            report.AppendLine("Lv"+level+": "+input+" source triangles + "+backs+" open backs; "+root.GetComponentsInChildren<MeshRenderer>(true).Length+" renderers.");
        }
        finally {PrefabUtility.UnloadPrefabContents(root);Object.DestroyImmediate(source);}
    }
    static Bounds BoundsOf(GameObject root)
    {
        var bounds=new Bounds();bool first=true;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))foreach(var vertex in f.sharedMesh.vertices)
        {var point=f.transform.TransformPoint(vertex);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
        return bounds;
    }
    public static void ValidatePrefabs(Scene preview)
    {
        for(int level=1;level<=5;level++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Exterior(level));
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);var authoring=instance.GetComponent<BarnExteriorAuthoring>();
            var lods=authoring.ModelRoot.GetComponent<LODGroup>().GetLODs();
            Check(lods.Length==3 && instance.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m==atlas)),"Lv"+level+" three B LODs share the painted atlas");
            foreach(float angle in new[]{0f,90,180,270})
            {
                instance.transform.rotation=Quaternion.Euler(0,angle,0);Physics.SyncTransforms();
                Check(instance.GetComponentsInChildren<Collider>().All(c=>!c.bounds.Contains(authoring.Entrance.position+Vector3.up)),"Lv"+level+" entrance clears body and porch collision at yaw "+angle);
                Check(Vector3.Dot(authoring.Entrance.forward,instance.transform.forward)>.999f,"Lv"+level+" entrance faces placement direction at yaw "+angle);
            }
            Object.DestroyImmediate(instance);
        }
        for(int level=1;level<=4;level++)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Interior(level)),preview);
            var binding=instance.GetComponent<BarnInteriorLayout>();
            Check(binding.FeedingSlots.Length==Capacity[level-1] && binding.AnimalSpots.Length==Capacity[level-1] && binding.FeedFills.All(r=>r),"Lv"+level+" feed and animal capacity "+Capacity[level-1]+" retained");
            Check(instance.transform.localScale==Vector3.one*1.75f,"Lv"+level+" approved 1.75 interior scale retained");
            Check(instance.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.SequenceEqual(new[]{atlas})),"Lv"+level+" optimized interior renderer uses the new atlas");
            Physics.SyncTransforms();
            foreach(var spot in binding.AnimalSpots)Check(instance.GetComponentsInChildren<BoxCollider>().Where(c=>c.name!="Floor").All(c=>!c.bounds.Contains(spot.position+Vector3.up*.5f)),"Lv"+level+" animal marker clears solid colliders");
            Object.DestroyImmediate(instance);
        }
    }
    static void RenderPrefab(GameObject prefab,string filename,bool interior,int lod=0)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name.StartsWith("Fill_"))renderer.gameObject.SetActive(true);
            var group=model.GetComponentInChildren<LODGroup>();
            if(group)
            {
                // Preview scenes have no normal frame/culling update between
                // instantiation and this synchronous render request.
                group.ForceLOD(lod);
                var lods=group.GetLODs();
                Object.DestroyImmediate(group);
                for(int i=0;i<lods.Length;i++)if(i!=lod)foreach(var renderer in lods[i].renderers)Object.DestroyImmediate(renderer.gameObject);
            }
            var bounds=new Bounds();bool first=true;
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true)){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
            var sun=new GameObject("Preview Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(48,-35,0);
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.orthographic=true;
            camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.z)*.72f;camera.nearClipPlane=.05f;camera.farClipPlane=250;
            camera.transform.position=bounds.center+new Vector3(bounds.size.x*.9f,bounds.size.x*1.15f,(interior?-1:1)*bounds.size.z*1.5f);camera.transform.LookAt(bounds.center);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.29f,.34f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(1280,960,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Output+"/"+filename,image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally {if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
