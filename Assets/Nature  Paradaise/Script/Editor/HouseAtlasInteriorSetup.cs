#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Explicit authoring bridge for the approved atlas house interiors.</summary>
[InitializeOnLoad]
public static class HouseAtlasInteriorSetup
{
    const string Root="Assets/Nature  Paradaise";
    const string ScenePath=Root+"/Map/Scenes/Interiors/HouseInterior.unity";
    const string Output="ArtSource/HouseInterior_B/Unity";
    const string Models=Root+"/mesh/Buildings/HouseInterior_H";
    const string Prefabs=Root+"/Prefabs/PlayerHouse/Interior";
    const string PackMaterials=Root+"/Pack/Toon Series/Toon Farm Pack/Models/Materials";
    const string AtlasPath=Root+"/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd";
    static Material atlas,glass,display;static StringBuilder log;
    static bool busy,authoringMutated;static double next;
    static HouseAtlasInteriorSetup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        string request=File.Exists(Output+"/apply.request")?Output+"/apply.request":File.Exists(Output+"/verify.request")?Output+"/verify.request":Output+"/inspect.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try{Directory.CreateDirectory(Output);File.Move(request,Output+"/"+Path.GetFileNameWithoutExtension(request)+"_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");if(request.EndsWith("apply.request"))Apply();else if(request.EndsWith("verify.request"))VerifySaved();else Inspect();}
        catch(Exception e){File.WriteAllText(Output+"/Error.txt",e.ToString());Debug.LogException(e);if(authoringMutated){var s=SceneManager.GetSceneByPath(ScenePath);if(s.isLoaded)EditorSceneManager.CloseScene(s,true);EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);authoringMutated=false;}}finally{busy=false;}
    }
    static JObject Vec(Vector3 v)=>new JObject{["x"]=v.x,["y"]=v.y,["z"]=v.z};
    static string PathOf(Transform t,Transform root)=>t==root?"":AnimationUtility.CalculateTransformPath(t,root);
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("House atlas: "+message);log?.AppendLine("PASS "+message);}
    static Vector3 V(JToken j)=>new Vector3((float)j[0],(float)j[1],(float)j[2]);
    static Transform Child(Transform p,string n,Vector3 pos=default)
    {var o=new GameObject(n);o.transform.SetParent(p,false);o.transform.localPosition=pos;return o.transform;}
    static BoxCollider Box(Transform p,string name,Bounds b)
    {var t=Child(p,name,b.center);var c=t.gameObject.AddComponent<BoxCollider>();c.size=b.size;return c;}
    static Bounds Expected(JToken j)=>new Bounds((V(j["min"])+V(j["max"]))/2,V(j["max"])-V(j["min"]));
    static Bounds MeshBounds(MeshFilter f,Transform relative)
    {
        var m=(relative?relative.worldToLocalMatrix:Matrix4x4.identity)*f.transform.localToWorldMatrix;var vv=f.sharedMesh.vertices;
        var b=new Bounds(m.MultiplyPoint3x4(vv[0]),Vector3.zero);foreach(var v in vv)b.Encapsulate(m.MultiplyPoint3x4(v));return b;
    }
    static void Materials()
    {
        atlas=AssetDatabase.LoadAssetAtPath<Material>(PackMaterials+"/TFP_Atlas_1A.mat");var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        Require(atlas&&tex&&atlas.shader.isSupported,"Existing Toonpack atlas and supported shader");
        var shader=Shader.Find("Universal Render Pipeline/Lit");Require(shader&&shader.isSupported,"URP material variants supported");
        string gp=Models+"/HouseAtlasGlass.mat";glass=AssetDatabase.LoadAssetAtPath<Material>(gp);
        if(!glass){glass=new Material(shader);AssetDatabase.CreateAsset(glass,gp);}glass.shader=shader;
        glass.SetTexture("_BaseMap",tex);glass.SetColor("_BaseColor",new Color(1,1,1,.09f));glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",2);glass.SetFloat("_Smoothness",.17f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
        string dp=Models+"/HouseAtlasDisplay.mat";display=AssetDatabase.LoadAssetAtPath<Material>(dp);
        if(!display){display=new Material(shader);AssetDatabase.CreateAsset(display,dp);}display.shader=shader;display.SetTexture("_BaseMap",tex);display.SetColor("_BaseColor",Color.white);display.SetTexture("_EmissionMap",tex);display.SetColor("_EmissionColor",Color.white*.35f);display.EnableKeyword("_EMISSION");display.SetFloat("_Smoothness",.1f);
        EditorUtility.SetDirty(glass);EditorUtility.SetDirty(display);
    }
    sealed class Part{public MeshFilter f;public JObject spec;public Bounds bounds;public string Role=>(string)spec["role"];public string Id=>(string)spec["id"];public string Module=>(string)spec["module"];}
    static List<Part> Import(int lv,Scene scene,JObject spec,out GameObject source,out float error)
    {
        string path=Models+"/HouseInterior_Lv"+lv+".fbx";File.Copy(Output+"/Source/HouseInterior_Lv"+lv+".fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.globalScale=1;imp.useFileScale=true;imp.bakeAxisConversion=true;imp.importAnimation=false;imp.animationType=ModelImporterAnimationType.None;imp.importCameras=false;imp.importLights=false;imp.addCollider=false;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;imp.importNormals=ModelImporterNormals.Import;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"TFP_Atlas_1A"),atlas);imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"House_AtlasGlass_ExistingTexture"),glass);imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"House_AtlasLights_ExistingTexture"),display);imp.SaveAndReimport();
        source=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
        var dict=source.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name);var parts=new List<Part>();
        foreach(JObject o in spec["objects"]){Require(dict.ContainsKey((string)o["name"]),"Lv"+lv+" mesh found "+o["name"]);parts.Add(new Part{f=dict[(string)o["name"]],spec=o});}
        Require(parts.Count==dict.Count,"Lv"+lv+" exact source object count");
        // Test multiple asymmetric landmarks, so neither the plan nor entrance can be mirrored.
        var markers=parts.Where(p=>p.Role.Contains("bed")||p.Role=="Refrigerator"||p.Role=="Weather radio"||p.f.name.Contains("BackWall")||p.f.name.Contains("Pink_Back")).Take(6).ToArray();Require(markers.Length>=2,"Asymmetric orientation landmarks");
        var originalRotation=source.transform.rotation;var originalScale=source.transform.localScale;float best=float.PositiveInfinity;Quaternion rotation=originalRotation;Vector3 scale=originalScale;
        foreach(int flip in new[]{1,-1})foreach(int x in new[]{0,90,180,270})foreach(int y in new[]{0,90,180,270})foreach(int z in new[]{0,90,180,270})
        {
            source.transform.rotation=Quaternion.Euler(x,y,z)*originalRotation;source.transform.localScale=Vector3.Scale(originalScale,new Vector3(flip,1,1));float score=0;
            foreach(var p in markers){var b=MeshBounds(p.f,source.transform.parent);var e=Expected(p.spec);score+=Vector3.Distance(b.min,e.min)+Vector3.Distance(b.max,e.max);}score/=markers.Length;
            if(score<best){best=score;rotation=source.transform.rotation;scale=source.transform.localScale;}
        }
        source.transform.rotation=rotation;source.transform.localScale=scale;error=best;Require(best<.006f,"Lv"+lv+" upright, unmirrored metre coordinates: "+best);
        foreach(var p in parts)
        {
            p.bounds=MeshBounds(p.f,source.transform.parent);var e=Expected(p.spec);Require(Vector3.Distance(p.bounds.min,e.min)+Vector3.Distance(p.bounds.max,e.max)<.012f,"Lv"+lv+" placement "+p.f.name+" actual "+p.bounds+" expected "+e);
            Require(p.f.sharedMesh.uv.Length==p.f.sharedMesh.vertexCount,"Source UV "+p.f.name);
        }
        Require(parts.Sum(p=>p.f.sharedMesh.triangles.Length/3)==(int)spec["triangles"],"Lv"+lv+" imported triangles");return parts;
    }
    static string Interaction(Part p,int lv)
    {
        if((p.Role=="Single bed"&&lv<=2)||p.Role=="Double bed")return "Bed_MeshSlot";
        if(p.Role=="Flat TV"||p.Role=="Weather radio")return "Forecast_Editable";
        if(p.Role=="Save book")return "SaveBook_Editable";
        if(p.Role=="Refrigerator")return "Refrigerator_MeshSlot";
        if(p.Role=="Stove oven")return "Kitchen_MeshSlot";
        if(p.Role=="Aquarium tank"||p.Role=="Aquarium glass"||p.Module.EndsWith("_Stand")&&p.Module.StartsWith("Aquarium"))return "Aquarium_Editable";
        if(p.Role=="Storage chest"||p.Role=="Wardrobe"&&p.Id!="C4")return "Storage_Editable";
        return null;
    }
    static string Group(Part p,int lv)
    {
        var interact=Interaction(p,lv);if(interact!=null)return interact;
        if(p.Role=="Architecture")return p.f.name.Contains("Floor")||p.f.name.Contains("Foundation")?"Structure_Floors":"Structure_WallsAndSteps";
        if(p.Id.StartsWith("M"))return "MasterFurniture";if(p.Id.StartsWith("C"))return "PinkFurniture";if(p.Id.StartsWith("T"))return "LoungeFurniture";if(p.Id.StartsWith("D")||p.Role=="Dining chair"||p.Role=="Dining table")return "DiningFurniture";
        if(p.Id.StartsWith("K")||p.Role=="Island stool")return "KitchenFurniture";if(p.Id.StartsWith("E"))return "EntranceFurniture";
        return p.bounds.center.z>4.5f?"AttachedProps_Rear":"AttachedProps_Front";
    }
    static bool SolidFurniture(Part p)=>new[]{"Single bed","Double bed","Storage chest","Wardrobe","Cabinet","Glass cupboard","Kitchen counter","Sofa","Sofa L one assembly","Armchair","Ottoman","Coffee table","Dining table","Dining chair","Study chair","Island stool","Bedside cabinet","Refrigerator","Stove oven","Small bookcase","Open bookshelf","TV console","Radio book shelf"}.Contains(p.Role);
    static Mesh Combine(IEnumerable<(Part p,int sub)> entries,Transform frame,string name)
    {
        var ci=entries.Select(e=>new CombineInstance{mesh=e.p.f.sharedMesh,subMeshIndex=e.sub,transform=frame.worldToLocalMatrix*e.p.f.transform.localToWorldMatrix}).ToArray();
        // Axis conversion may include a reflection. Correct winding before combining.
        var copies=new List<Mesh>();
        for(int i=0;i<ci.Length;i++)if(ci[i].transform.determinant<0)
        {var m=Object.Instantiate(ci[i].mesh);for(int s=0;s<m.subMeshCount;s++){var ix=m.GetTriangles(s);for(int j=0;j<ix.Length;j+=3){int k=ix[j+1];ix[j+1]=ix[j+2];ix[j+2]=k;}m.SetTriangles(ix,s);}ci[i].mesh=m;copies.Add(m);}
        var mesh=new Mesh{name=name,indexFormat=ci.Sum(c=>c.mesh.vertexCount)<65536?IndexFormat.UInt16:IndexFormat.UInt32};mesh.CombineMeshes(ci,true,true);mesh.RecalculateBounds();MeshUtility.Optimize(mesh);foreach(var m in copies)Object.DestroyImmediate(m);return mesh;
    }
    static void Set(Component c,string property,object value)
    {var so=new SerializedObject(c);var p=so.FindProperty(property);Require(p!=null,c.GetType().Name+"."+property);if(value is Object o)p.objectReferenceValue=o;else if(value is bool b)p.boolValue=b;else if(value is float f)p.floatValue=f;else if(value is int i)p.intValue=i;else if(value is string s)p.stringValue=s;so.ApplyModifiedPropertiesWithoutUndo();}
    static void CopySettings<T>(Transform old,GameObject target) where T:MonoBehaviour
    {var src=old.GetComponentsInChildren<T>(true).FirstOrDefault();Require(src,"Existing "+typeof(T).Name+" settings");var dst=target.AddComponent<T>();EditorUtility.CopySerialized(src,dst);}
    static Bounds Merged(IEnumerable<Part> parts){var a=parts.ToArray();var b=a[0].bounds;foreach(var p in a.Skip(1))b.Encapsulate(p.bounds);return b;}
    static GameObject Build(int lv,Scene preview,JObject spec,Transform old,out JObject result)
    {
        var parts=Import(lv,preview,spec,out var source,out float axisError);var root=new GameObject("InteriorLayout_Lv"+lv+"_Editable");SceneManager.MoveGameObjectToScene(root,preview);var visual=Child(root.transform,"Visuals_Atlas_Optimized");var collision=Child(root.transform,"Collision_Editable");var fixtures=Child(root.transform,"Interactions_Editable");
        var anchors=new Dictionary<string,Transform>();
        foreach(var group in parts.GroupBy(p=>Interaction(p,lv)).Where(g=>g.Key!=null))
        {var b=Merged(group);anchors[group.Key]=Child(fixtures,group.Key,b.center);}
        string meshPath=Models+"/HouseInterior_Lv"+lv+"_Runtime.asset";var existing=AssetDatabase.LoadAllAssetsAtPath(meshPath).OfType<Mesh>().ToDictionary(m=>m.name);bool assetExists=File.Exists(meshPath);var sourceMeshes=new List<Mesh>();
        foreach(var group in parts.GroupBy(p=>Group(p,lv)))
        {
            Transform frame=anchors.ContainsKey(group.Key)?anchors[group.Key]:Child(visual,group.Key,Merged(group).center);
            var entries=group.SelectMany(p=>Enumerable.Range(0,p.f.sharedMesh.subMeshCount).Select(s=>(p,s)));
            foreach(var matGroup in entries.GroupBy(e=>e.p.f.GetComponent<Renderer>().sharedMaterials[e.s]))
            {
                var material=matGroup.Key;Require(material==atlas||material==glass||material==display,"Atlas material remapped");
                string mn="HouseInterior_Lv"+lv+"_"+group.Key+"_"+material.name;var node=Child(frame,material==glass?"Glass":"Geometry");var me=Combine(matGroup,node,mn);
                if(existing.TryGetValue(mn,out var saved)){EditorUtility.CopySerialized(me,saved);Object.DestroyImmediate(me);me=saved;EditorUtility.SetDirty(me);}else if(!assetExists){AssetDatabase.CreateAsset(me,meshPath);assetExists=true;}else AssetDatabase.AddObjectToAsset(me,meshPath);
                node.gameObject.AddComponent<MeshFilter>().sharedMesh=me;var r=node.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;
                sourceMeshes.Add(me);
                if(material==glass){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            }
        }
        // Static collision follows real meshes: no rectangular collider across an L sofa void.
        foreach(var p in parts.Where(SolidFurniture))
        {
            if(p.Role=="Sofa L one assembly")
            {Box(collision,"SofaMain",new Bounds(new Vector3(2.65f,.50f,4.45f),new Vector3(2.71f,1,.9f)));Box(collision,"SofaReturn",new Bounds(new Vector3(1.745f,.50f,3.40f),new Vector3(.9f,1,1.2f)));}
            else Box(collision,p.f.name+"_Collision",p.bounds);
        }
        foreach(var p in parts.Where(p=>p.Role=="Architecture"&&!p.f.name.StartsWith("Curtain")&&!p.f.name.Contains("Countertop")&&!p.f.name.Contains("Peninsula")&&!p.f.name.Contains("RoundSideTable")))
        {
            bool floorSurface=p.f.name.Contains("Floor")&&!p.f.name.Contains("Door");var t=Child(collision,floorSurface?"ContinuousFloorCollider_Editable":p.f.name+"_Surface");
            // Reuse exact closed geometry for steps, doorway voids and stepped floor outline.
            var mesh=Combine(Enumerable.Range(0,p.f.sharedMesh.subMeshCount).Select(s=>(p,s)),t,"HouseInterior_Lv"+lv+"_Collider_"+p.f.name);
            if(existing.TryGetValue(mesh.name,out var saved)){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.AddObjectToAsset(mesh,meshPath);
            t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        var primary=parts.Single(p=>Interaction(p,lv)=="Bed_MeshSlot");var bed=anchors["Bed_MeshSlot"];var bedBox=bed.gameObject.AddComponent<BoxCollider>();bedBox.size=primary.bounds.size;
        CopySettings<PlayerBed>(old,bed.gameObject);var sleep=Child(bed,"SleepPose_Editable");var wake=Child(bed,"WakeStandPoint_Editable");
        float floor=lv>=4?.36f:.0055f;sleep.position=new Vector3(primary.bounds.center.x,floor-.32f,primary.bounds.center.z);sleep.rotation=Quaternion.Euler(0,180,0);
        float wakeZ=primary.bounds.min.z-.50f;if(lv==5)wakeZ-=.75f;wake.position=lv==3?new Vector3(primary.bounds.max.x+.55f,floor+.08f,primary.bounds.center.z-.25f):new Vector3(primary.bounds.center.x,floor+.08f,wakeZ);wake.rotation=Quaternion.Euler(0,180,0);
        Set(bed.GetComponent<PlayerBed>(),"sleepPose",sleep);Set(bed.GetComponent<PlayerBed>(),"wakeStandPoint",wake);Set(bed.GetComponent<PlayerBed>(),"useTriggerCollider",false);Set(bed.GetComponent<PlayerBed>(),"interactionRadius",1.3f);
        foreach(var p in anchors)
        {
            if(p.Key=="Bed_MeshSlot")continue;
            var col=p.Value.gameObject.AddComponent<BoxCollider>();col.size=Merged(parts.Where(q=>Interaction(q,lv)==p.Key)).size;col.isTrigger=true;
            if(p.Key=="Forecast_Editable")CopySettings<WeatherForecastTV>(old,p.Value.gameObject);
            if(p.Key=="SaveBook_Editable")CopySettings<HouseSaveBook>(old,p.Value.gameObject);
            if(p.Key=="Storage_Editable")CopySettings<HouseStorageChest>(old,p.Value.gameObject);
            if(p.Key=="Refrigerator_MeshSlot")CopySettings<Refrigerator>(old,p.Value.gameObject);
            if(p.Key=="Kitchen_MeshSlot")CopySettings<KitchenSet>(old,p.Value.gameObject);
            if(p.Key=="Aquarium_Editable")
            {
                // A separate empty keeps Aquarium's procedural decoration from recoloring the atlas tank.
                CopySettings<Aquarium>(old,p.Value.gameObject);p.Value.GetComponent<Aquarium>().Configure("house.aquarium.test.main",lv==5?AquariumSize.Grand:AquariumSize.Medium);
                // Runtime fish/decor use a normalized tank space, aligned inside the new glass volume.
                var tank=parts.Single(q=>q.Role=="Aquarium tank").bounds;var runtime=Child(p.Value,"FishInteraction_Normalized",tank.center-p.Value.position);runtime.localScale=new Vector3(Mathf.Min(tank.size.x,.9f),tank.size.y*.80f,tank.size.z*.42f);var aq=p.Value.GetComponent<Aquarium>();EditorUtility.CopySerialized(aq,runtime.gameObject.AddComponent<Aquarium>());Object.DestroyImmediate(aq);
            }
        }
        // Lv3-5 references have no separate chest/book meshes: bind storage to the wardrobe;
        // reuse the same atlas save-book module on the master nightstand.
        if(!anchors.ContainsKey("SaveBook_Editable"))
        {
            var night=parts.First(p=>p.Role=="Bedside cabinet"&&(lv<4||p.Id=="M2"));var book=Child(fixtures,"SaveBook_Editable",new Vector3(night.bounds.center.x,night.bounds.max.y+.03f,night.bounds.center.z));
            var sourceBook=AssetDatabase.LoadAllAssetsAtPath(Models+"/HouseInterior_Lv1_Runtime.asset").OfType<Mesh>().Single(m=>m.name.StartsWith("HouseInterior_Lv1_SaveBook_Editable_"));var go=Child(book,"BookVisual");go.localScale=Vector3.one;go.gameObject.AddComponent<MeshFilter>().sharedMesh=sourceBook;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=atlas;
            var bc=book.gameObject.AddComponent<BoxCollider>();bc.size=new Vector3(.29f,.07f,.21f);bc.isTrigger=true;CopySettings<HouseSaveBook>(old,book.gameObject);
        }
        // Retain the user's existing calendar and its UI bindings as a compact wall fixture.
        var oldCalendar=old.GetComponentsInChildren<CalendarUI>(true).Single();var calendar=Object.Instantiate(oldCalendar.gameObject,fixtures);calendar.name="House Calendar";calendar.transform.localScale=Vector3.one*.65f;calendar.transform.localPosition=new Vector3(.12f,0,lv<=2?1.35f:1.1f);calendar.transform.localRotation=Quaternion.identity;
        var plan=lv>=4?JArray.Parse(File.ReadAllText("Documentation/Design/HouseInteriorReview/RevisionH/placement.json")).OfType<JObject>().Single(p=>(int)p["level"]==lv):null;
        float width=lv==1?5.2f:lv==2?6.7f:10.9f,depth=lv==1?5:lv==2?5.8f:8.1f;if(plan!=null){width=(float)plan["W"];depth=(float)plan["D"];}
        foreach(int side in new[]{-1,1})Box(collision,"FrontBoundary_"+side,new Bounds(new Vector3(width/2+side*(width/4+.35f),1,0),new Vector3(width/2-.7f,2,.12f)));
        Child(root.transform,"EntryPoint_Editable",new Vector3(width/2,.08f,.95f));Child(root.transform,"ExitPoint_Editable",new Vector3(width/2,.65f,.10f));
        var cam=Child(root.transform,"InteriorPerspectiveCamera_Editable").gameObject.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=40;cam.nearClipPlane=.05f;cam.farClipPlane=200;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.22f,.26f,.28f);
        cam.transform.localRotation=Quaternion.Euler(54,0,0);cam.transform.localPosition=new Vector3(width/2,.55f,depth*.51f)-cam.transform.forward*(Mathf.Max(width,depth)*1.65f);
        var runtimeParts=root.GetComponentsInChildren<MeshFilter>(true);
        // Count the source groups separately; retained calendar/save book are additive gameplay fixtures.
        int sourceTriangles=sourceMeshes.Sum(m=>m.triangles.Length/3);Require(sourceTriangles==(int)spec["triangles"],"Lv"+lv+" optimized meshes preserve every source triangle");
        string prefabPath=Prefabs+"/HouseInteriorLayout_Lv"+lv+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        result=new JObject{["level"]=lv,["sourceTriangles"]=sourceTriangles,["totalTrianglesWithExistingFixtures"]=runtimeParts.Sum(f=>f.sharedMesh.triangles.Length/3),["unityVertices"]=runtimeParts.Sum(f=>f.sharedMesh.vertexCount),["renderers"]=root.GetComponentsInChildren<MeshRenderer>(true).Length,["sourceRenderers"]=parts.Count,["axisError"]=axisError,["prefab"]=prefabPath};
        Object.DestroyImmediate(root);Object.DestroyImmediate(source);return prefab;
    }
    [MenuItem("Nature Paradise/House/Apply Atlas Interior H")]
    public static void Apply()
    {
        log=new StringBuilder("HOUSE INTERIOR H UNITY APPLY\n");Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Play Mode stopped");Directory.CreateDirectory(Output);Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs);
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);Require(!scene.isDirty,"House scene has no unsaved edits");
        var house=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HouseInteriorController>(true)).Single();int originalPreview=house.PreviewLevel;
        string backup=Output+"/Before_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"/HouseInterior.unity");
        Materials();var specs=JArray.Parse(File.ReadAllText(Output+"/SourceManifest.json"));var preview=EditorSceneManager.NewPreviewScene();var prefabs=new List<GameObject>();var records=new JArray();
        try{for(int lv=1;lv<=5;lv++){var old=house.transform.Find("InteriorLayout_Lv"+lv+"_Editable");Require(old,"Existing level "+lv);prefabs.Add(Build(lv,preview,(JObject)specs[lv-1],old,out var rec));records.Add(rec);}}finally{EditorSceneManager.ClosePreviewScene(preview);}
        AssetDatabase.SaveAssets();
        var layouts=new List<GameObject>();var entries=new List<Transform>();var exits=new List<Transform>();var cameras=new List<Camera>();
        authoringMutated=true;
        for(int lv=1;lv<=5;lv++)
        {
            var old=house.transform.Find("InteriorLayout_Lv"+lv+"_Editable");Object.DestroyImmediate(old.gameObject);var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[lv-1],scene);obj.transform.SetParent(house.transform,false);obj.name="InteriorLayout_Lv"+lv+"_Editable";layouts.Add(obj);entries.Add(obj.transform.Find("EntryPoint_Editable"));exits.Add(obj.transform.Find("ExitPoint_Editable"));cameras.Add(obj.GetComponentInChildren<Camera>(true));
        }
        foreach(string n in new[]{"OriginalPreset_Reference_Inactive","PreviousFourLayouts_Reference_Inactive"}){var old=house.transform.Find(n);if(old)Object.DestroyImmediate(old.gameObject);}
        house.ConfigureAuthoredLevels(layouts,entries,exits);house.GetComponent<HouseInteriorView>().Configure(cameras);house.PreviewLayout(Mathf.Clamp(originalPreview,1,5));
        var portal=house.transform.Find("HouseInteriorExitDoor_Editable");Require(portal&&portal.GetComponent<HouseScenePortal>(),"Existing exit portal retained");foreach(var r in portal.GetComponentsInChildren<Renderer>(true))r.enabled=false;portal.localScale=Vector3.one;var pc=portal.GetComponent<BoxCollider>();if(pc){pc.size=new Vector3(1.3f,1.3f,.25f);pc.center=Vector3.zero;pc.isTrigger=true;}
        Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ScenePath),"Interior in enabled build settings");
        Validate(house);EditorUtility.SetDirty(house);EditorUtility.SetDirty(house.GetComponent<HouseInteriorView>());EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"House scene saved");authoringMutated=false;
        for(int lv=1;lv<=5;lv++){var imp=(ModelImporter)AssetImporter.GetAtPath(Models+"/HouseInterior_Lv"+lv+".fbx");imp.isReadable=false;imp.SaveAndReimport();}
        AssetDatabase.SaveAssets();File.WriteAllText(Output+"/UnityImportAudit.txt",log.ToString());File.WriteAllText(Output+"/UnityApplyResult.json",new JObject{["status"]="PASS",["revision"]="H Lv4-5; B Lv1-3",["scene"]=ScenePath,["backup"]=backup,["levels"]=records,["atlas"]=AtlasPath,["rootScalePreserved"]=Vec(house.transform.localScale),["playModeTested"]=false,["mapModified"]=false}.ToString());
        FinishReview(scene);Debug.Log("HOUSE_ATLAS_INTERIOR_APPLY_PASS");
    }
    static void Validate(HouseInteriorController house)
    {
        int previous=house.PreviewLevel;var so=new SerializedObject(house);var refs=so.FindProperty("levelLayouts");Require(refs.arraySize==5,"Five controller bindings");
        for(int lv=1;lv<=5;lv++)
        {
            house.PreviewLayout(lv);Physics.SyncTransforms();var l=house.transform.Find("InteriorLayout_Lv"+lv+"_Editable");Require(l.gameObject.activeSelf&&Enumerable.Range(1,5).Count(i=>house.transform.Find("InteriorLayout_Lv"+i+"_Editable").gameObject.activeSelf)==1,"Lv"+lv+" exclusive activation");
            Require(refs.GetArrayElementAtIndex(lv-1).objectReferenceValue==l.gameObject,"Lv"+lv+" serialized layout reference");Require(l.GetComponentsInChildren<PlayerBed>().Length==1,"One primary bed "+lv);Require(l.GetComponentsInChildren<WeatherForecastTV>().Single().IsRadio==(lv==1),"Forecast device "+lv);Require(l.GetComponentsInChildren<HouseSaveBook>().Length==1,"Save book "+lv);Require(l.GetComponentsInChildren<HouseStorageChest>().Length>=1,"Unified storage "+lv);Require(l.GetComponentsInChildren<Refrigerator>().Length==(lv>=2?1:0),"Fridge tier "+lv);Require(l.GetComponentsInChildren<KitchenSet>().Length==(lv>=3?1:0),"Kitchen tier "+lv);Require(l.GetComponentsInChildren<Aquarium>().Length==(lv>=3?1:0),"Aquarium tier "+lv);Require(l.GetComponentsInChildren<CalendarUI>().Length==1,"Existing calendar retained "+lv);
            var bed=l.GetComponentInChildren<PlayerBed>();var bs=new SerializedObject(bed);var wake=(Transform)bs.FindProperty("wakeStandPoint").objectReferenceValue;Require(wake&&bs.FindProperty("sleepPose").objectReferenceValue,"Bed anchors "+lv);
            var entry=house.transform.Find("HouseInteriorEntrySpawn");Require(Vector3.Distance(entry.position,l.Find("EntryPoint_Editable").position)<.001f,"Entry follows level "+lv);
            Require(Physics.Raycast(entry.position+Vector3.up,Vector3.down,out var hit,2f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(l),"Entry above new floor "+lv);
            Require(Physics.Raycast(wake.position+Vector3.up,Vector3.down,out hit,2f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(l),"Wake above floor "+lv);
            Require(l.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing script "+lv);
            foreach(var r in l.GetComponentsInChildren<MeshRenderer>(true))Require(r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported),"Supported material "+r.name);
        }
        house.PreviewLayout(previous);
    }
    [MenuItem("Nature Paradise/House/Verify Atlas Interior H")]
    public static void VerifySaved()
    {
        log=new StringBuilder("HOUSE H SAVED VALIDATION\n");var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);var house=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HouseInteriorController>(true)).Single();Validate(house);ValidatePhysics(house);for(int lv=1;lv<=5;lv++)Render(lv);File.WriteAllText(Output+"/SavedValidation.txt",log+"\nHOUSE_H_SAVED_VALIDATION_PASS");File.WriteAllText(Output+"/SavedValidation.json",new JObject{["status"]="PASS",["levels"]=5,["capsuleChecks"]=true,["playModeTested"]=false}.ToString());FinishReview(scene);Debug.Log("HOUSE_H_SAVED_VALIDATION_PASS");
    }
    static void ValidatePhysics(HouseInteriorController house)
    {
        var player=Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);var actual=player?player.GetComponent<CharacterController>():null;
        var probeGo=new GameObject("HouseValidationCapsule");probeGo.transform.position=new Vector3(-10000,1000,-10000);var probe=probeGo.AddComponent<CharacterController>();probe.enabled=false;
        if(actual){probe.height=actual.height;probe.radius=actual.radius;probe.center=actual.center;probe.skinWidth=actual.skinWidth;probeGo.transform.localScale=actual.transform.lossyScale;}
        var pos=house.transform.position;int preview=house.PreviewLevel;house.transform.position+=new Vector3(10000,0,10000);
        try
        {
            for(int lv=1;lv<=5;lv++)
            {
                house.PreviewLayout(lv);Physics.SyncTransforms();var layout=house.transform.Find("InteriorLayout_Lv"+lv+"_Editable");var bed=layout.GetComponentInChildren<PlayerBed>();var wake=(Transform)new SerializedObject(bed).FindProperty("wakeStandPoint").objectReferenceValue;
                foreach(var point in new[]{house.transform.Find("HouseInteriorEntrySpawn"),wake})
                {bool clear=PlayerLifeCycle.TryResolveStandingPoint(point.position,probe,layout,out var grounded);Require(clear,"Lv"+lv+" standing capsule resolves "+point.name);Require(Vector2.Distance(new Vector2(point.position.x,point.position.z),new Vector2(grounded.x,grounded.z))<.3f,"Lv"+lv+" authored point clear without lateral fallback "+point.name);}
            }
        }
        finally{house.transform.position=pos;house.PreviewLayout(preview);Object.DestroyImmediate(probeGo);Physics.SyncTransforms();}
    }
    static void Render(int lv)
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/HouseInteriorLayout_Lv"+lv+".prefab"),scene);float width=lv==1?5.2f:lv==2?6.7f:lv==5?11.6f:10.9f,depth=lv==1?5:lv==2?5.8f:lv==3?8.1f:lv==4?10.85f:11.6f;
            var sun=new GameObject("Review Sun").AddComponent<Light>();SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=1;sun.transform.rotation=Quaternion.Euler(50,-30,0);
            var cam=new GameObject("Review Camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,scene);cam.scene=scene;cam.orthographic=true;cam.orthographicSize=Mathf.Max(width,depth)*.63f;cam.nearClipPlane=.05f;cam.farClipPlane=100;cam.transform.position=new Vector3(width*.12f,width*2.05f,-depth*.9f);cam.transform.LookAt(new Vector3(width/2,.55f,depth*.51f));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.48f,.53f,.53f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt=new RenderTexture(1400,1200,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var prev=RenderTexture.active;RenderTexture.active=rt;var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();RenderTexture.active=prev;File.WriteAllBytes(Output+"/Unity_Lv"+lv+".png",png.EncodeToPNG());Object.DestroyImmediate(png);
        }
        finally{if(rt){rt.Release();Object.DestroyImmediate(rt);}EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void FinishReview(Scene scene)
    {
        var map=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(map.isLoaded)SceneManager.SetActiveScene(map);EditorSceneManager.CloseScene(scene,true);
        EditorApplication.delayCall+=()=>{var stage=PrefabStageUtility.OpenPrefab(Prefabs+"/HouseInteriorLayout_Lv5.prefab");var view=SceneView.lastActiveSceneView;if(view){view.sceneLighting=false;view.drawGizmos=false;view.LookAtDirect(new Vector3(5.8f,.8f,5.8f),Quaternion.Euler(56,-12,0),8);}};
    }
    static void Frame(HouseInteriorController house,int level)
    {
        // Preview only; saving the scene already preserves its original preview level.
        house.PreviewLayout(level);Selection.activeGameObject=house.gameObject;var sv=SceneView.lastActiveSceneView;if(sv){sv.sceneLighting=false;sv.LookAtDirect(house.transform.TransformPoint(new Vector3(5.8f,1,5.8f)),Quaternion.Euler(56,-12,0),10);sv.Repaint();}SceneManager.SetActiveScene(house.gameObject.scene);
    }
    static void Inspect()
    {
        var loaded=new JArray();for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);loaded.Add(new JObject{["path"]=s.path,["dirty"]=s.isDirty,["loaded"]=s.isLoaded});}
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var house=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HouseInteriorController>(true)).Single();
            var levels=new JArray();
            for(int lv=1;lv<=5;lv++)
            {
                var t=house.transform.Find("InteriorLayout_Lv"+lv+"_Editable");var comps=new JArray();
                foreach(var c in t.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c))
                {
                    var j=new JObject{["path"]=PathOf(c.transform,t),["type"]=c.GetType().Name,["json"]=EditorJsonUtility.ToJson(c),["position"]=Vec(t.InverseTransformPoint(c.transform.position))};
                    var rr=c.GetComponentsInChildren<Renderer>(true);if(rr.Length>0){var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);j["boundsCenter"]=Vec(t.InverseTransformPoint(b.center));j["boundsSize"]=Vec(b.size);}comps.Add(j);
                }
                levels.Add(new JObject{["level"]=lv,["active"]=t.gameObject.activeSelf,["localPosition"]=Vec(t.localPosition),["localScale"]=Vec(t.localScale),["components"]=comps,["cameras"]=new JArray(t.GetComponentsInChildren<Camera>(true).Select(c=>new JObject{["path"]=PathOf(c.transform,t),["position"]=Vec(t.InverseTransformPoint(c.transform.position)),["rotation"]=Vec(c.transform.localEulerAngles),["fov"]=c.fieldOfView}))});
            }
            var json=new JObject{["status"]="INSPECTED",["loadedScenes"]=loaded,["houseRoot"]=house.name,["housePosition"]=Vec(house.transform.position),["houseScale"]=Vec(house.transform.lossyScale),["controller"]=EditorJsonUtility.ToJson(house),["view"]=EditorJsonUtility.ToJson(house.GetComponent<HouseInteriorView>()),["levels"]=levels};
            File.WriteAllText(Output+"/BeforeUnity.json",json.ToString());Debug.Log("HOUSE_ATLAS_INSPECTED");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
#endif
