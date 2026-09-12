using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authoring portal Map dan scene BarnInterior. Tidak membuat object saat Play.</summary>
public static class BarnSystemSetup
{
    const string BarnScenePath="Assets/Nature  Paradaise/Map/Scenes/Interiors/BarnInterior.unity";
    const string BarnDefinitionPath="Assets/Nature  Paradaise/Resources/Buildings/Barn Building.asset";
    const string ExteriorFolder="Assets/Nature  Paradaise/Prefabs/Barn/Exterior";
    const string InteriorFolder="Assets/Nature  Paradaise/Prefabs/Barn/Interior";
    static readonly string[] SourceExteriorPaths=
    {
        "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Barn Presets/TFP_Barn_01A.prefab",
        "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Barn Presets/TFP_Barn_02A.prefab",
        "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Barn Presets/TFP_Barn_03A.prefab",
        "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Barn Presets/TFP_Barn_04A.prefab"
    };

    [MenuItem("Nature Paradise/Barn/Setup or Update Barn System",false,100)]
    public static void CreateModularBarnPrefabs()
    {
        if(EditorApplication.isPlaying) return;
        EnsureAssetFolder("Assets/Nature  Paradaise/Prefabs");
        EnsureAssetFolder("Assets/Nature  Paradaise/Prefabs/Barn");
        EnsureAssetFolder(ExteriorFolder);
        EnsureAssetFolder(InteriorFolder);

        GameObject[] exteriorPrefabs=new GameObject[4];
        for(int level=1;level<=4;level++)
            exteriorPrefabs[level-1]=EnsureExteriorPrefab(level);
        AssignExteriorPrefabs(exteriorPrefabs);

        if(!File.Exists(BarnScenePath)) EnsureBarnScene(false);
        EnsureInteriorLayoutPrefabs();
        if(Object.FindObjectsByType<PropertySite>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length>0)
            CreateAllInteriors();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject=exteriorPrefabs[0];
        EditorGUIUtility.PingObject(exteriorPrefabs[0]);
        Debug.Log("[BARN PREFAB] Exterior dan interior Lv1-Lv4 sudah modular. Edit prefab di Assets/Nature  Paradaise/Prefabs/Barn.");
    }

    [MenuItem("Nature Paradise/Barn/Open Barn Interior Scene",false,110)]
    public static void CreateOrOpenBarnScene()
    {
        EnsureBarnScene(true);
    }

    static void EnsureBarnScene(bool openAfterCreate)
    {
        if(EditorApplication.isPlaying) return;
        if(File.Exists(BarnScenePath))
        {
            SceneAsset existing=AssetDatabase.LoadAssetAtPath<SceneAsset>(BarnScenePath);
            Selection.activeObject=existing;
            EditorGUIUtility.PingObject(existing);
            if(openAfterCreate) AssetDatabase.OpenAsset(existing);
            Debug.Log("[BARN SETUP] BarnInterior.unity sudah ada dan siap diedit.");
            return;
        }

        SceneSetup[] previous=EditorSceneManager.GetSceneManagerSetup();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        GameObject root=new("BarnInterior_Editable");
        var controller=root.AddComponent<BarnInteriorSceneController>();
        var layouts=new List<GameObject>();
        float[] widths={22f,28f,36f,46f};
        float[] depths={24f,30f,38f,48f};
        for(int level=0;level<4;level++)
        {
            GameObject layout=new($"Layout_Lv{level+1}_Editable");
            layout.transform.SetParent(root.transform,false);
            layout.transform.position=new Vector3(0f,-200f,0f);
            CreateDummyShell(layout.transform,widths[level],depths[level]);
            layout.SetActive(level==0);
            layouts.Add(layout);
        }

        GameObject entry=new("BarnInteriorEntrySpawn");
        entry.transform.SetParent(root.transform,false);
        entry.transform.position=new Vector3(0f,-199.45f,-7.5f);
        entry.transform.rotation=Quaternion.identity;
        entry.AddComponent<PlayerSpawnPoint>().Configure("barn-interior-entry");

        GameObject exit=Primitive("BarnExitDoor_Editable",root.transform,new Vector3(0f,-199f,-9f),new Vector3(2.4f,2f,0.35f));
        GameObject spotsRoot=new("AnimalSpots_Editable");
        spotsRoot.transform.SetParent(root.transform,false);
        spotsRoot.transform.position=new Vector3(0f,-200f,0f);
        Transform[] spots=new Transform[30];
        for(int i=0;i<spots.Length;i++)
        {
            GameObject spot=new($"AnimalSlot_{i+1:00}_Editable");
            spot.transform.SetParent(spotsRoot.transform,false);
            spot.transform.localPosition=new Vector3(-4+(i%5)*2f,0.1f,2+(i/5)*2f);
            spots[i]=spot.transform;
        }
        controller.Configure(layouts,exit.transform,spots);

        Directory.CreateDirectory(Path.GetDirectoryName(BarnScenePath));
        EditorSceneManager.SaveScene(scene,BarnScenePath);
        AddSceneToBuildSettings(BarnScenePath);
        EditorSceneManager.RestoreSceneManagerSetup(previous);
        AssetDatabase.Refresh();
        SceneAsset createdAsset=AssetDatabase.LoadAssetAtPath<SceneAsset>(BarnScenePath);
        Selection.activeObject=createdAsset;
        EditorGUIUtility.PingObject(createdAsset);
        if(openAfterCreate) AssetDatabase.OpenAsset(createdAsset);
        Debug.Log("[BARN SETUP] BarnInterior.unity dibuat, ditambahkan ke Build Settings, dan siap diedit.");
    }

    public static void CreateAllInteriors()
    {
        if(EditorApplication.isPlaying) return;
        if(!File.Exists(BarnScenePath)) EnsureBarnScene(false);
        PropertySite[] sites=Object.FindObjectsByType<PropertySite>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(sites.Length==0) {Debug.LogWarning("[BARN SETUP] Buka scene Map sebelum membuat portal Barn.");return;}
        Transform container=GetOrCreatePortalContainer(sites[0].gameObject.scene);
        int created=0;
        int removed=0;
        foreach(PropertySite site in sites)
        {
            if(site==null) continue;
            AnimalHome home=site.GetComponent<AnimalHome>();
            if(home==null) home=Undo.AddComponent<AnimalHome>(site.gameObject);
            home.site=site;
            Transform entrance=EnsureEntranceMarker(home);
            BarnInterior[] matchingPortals=Object.FindObjectsByType<BarnInterior>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(candidate=>candidate!=null && candidate.home==home).ToArray();
            BarnInterior portal=matchingPortals.FirstOrDefault();
            for(int duplicateIndex=1;duplicateIndex<matchingPortals.Length;duplicateIndex++)
            {
                Undo.DestroyObjectImmediate(matchingPortals[duplicateIndex].gameObject);
                removed++;
            }
            if(portal==null)
            {
                GameObject root=new(BarnAccessName(site));
                Undo.RegisterCreatedObjectUndo(root,"Create Barn Portal");
                Undo.SetTransformParent(root.transform,container,"Parent Barn Portal");
                portal=root.AddComponent<BarnInterior>();
                created++;
            }
            else if(portal.transform.parent!=container)
            {
                Undo.SetTransformParent(portal.transform,container,"Move Barn Portal");
            }
            portal.gameObject.name=BarnAccessName(site);
            Undo.RecordObject(portal,"Configure Barn Portal");
            BoxCollider barrier=EnsureExteriorBarrier(portal,home);
            portal.ConfigureScene(home,entrance,barrier);
            removed+=RemoveLegacyPortalChildren(portal);
            portal.ConfigureManualExterior(false,false);
            EditorUtility.SetDirty(portal);
        }
        removed+=RemoveOrphanPortals(container,sites);
        removed+=RemoveEmptyLegacyInteriorContainer(container.parent);
        EditorSceneManager.MarkSceneDirty(container.gameObject.scene);
        Debug.Log($"[BARN SETUP] {created} akses Barn dibuat, {removed} object legacy/duplikat dibersihkan. Save Map.");
    }

    public static void OrganizeExistingInteriors() => CreateAllInteriors();

    [MenuItem("Nature Paradise/Barn/Create Feed Maker at Selection",false,120)]
    public static void CreateMachine()
    {
        if(EditorApplication.isPlaying) return;
        Vector3 point=Selection.activeTransform!=null ? Selection.activeTransform.position : Vector3.zero;
        GameObject machine=Primitive("FeedMaker_Editable",null,point+Vector3.right*2,new Vector3(1.2f,1.2f,1.2f));
        machine.AddComponent<FeedMaker>().Configure(System.Guid.NewGuid().ToString("N"),Resources.Load<FeedMakerCatalog>("Catalogs/FeedMakerCatalog"));
        Undo.RegisterCreatedObjectUndo(machine,"Create Feed Maker");
        Selection.activeGameObject=machine;
        EditorSceneManager.MarkSceneDirty(machine.scene);
    }

    public static void CreateInterior()
    {
        if(EditorApplication.isPlaying) return;
        AnimalHome home=Selection.activeGameObject!=null ? Selection.activeGameObject.GetComponent<AnimalHome>() : null;
        if(home==null) {Debug.LogWarning("Pilih object yang memiliki AnimalHome.");return;}
        if(!File.Exists(BarnScenePath)) EnsureBarnScene(false);
        Transform entrance=EnsureEntranceMarker(home);
        Transform container=GetOrCreatePortalContainer(home.gameObject.scene);
        BarnInterior portal=Object.FindObjectsByType<BarnInterior>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .FirstOrDefault(candidate=>candidate!=null && candidate.home==home);
        if(portal==null)
        {
            GameObject root=new(BarnAccessName(home.site));
            Undo.RegisterCreatedObjectUndo(root,"Create Barn Portal");
            Undo.SetTransformParent(root.transform,container,"Parent Barn Portal");
            portal=root.AddComponent<BarnInterior>();
        }
        else if(portal.transform.parent!=container)
        {
            Undo.SetTransformParent(portal.transform,container,"Move Barn Portal");
        }
        portal.gameObject.name=BarnAccessName(home.site);
        BoxCollider barrier=EnsureExteriorBarrier(portal,home);
        portal.ConfigureScene(home,entrance,barrier);
        RemoveLegacyPortalChildren(portal);
        portal.ConfigureManualExterior(false,false);
        Selection.activeGameObject=portal.gameObject;
        EditorSceneManager.MarkSceneDirty(portal.gameObject.scene);
    }

    static void CreateDummyShell(Transform parent,float width,float depth)
    {
        const float front=-10f;
        float centerZ=front+depth*0.5f;
        Primitive("Floor_ReplaceMe",parent,new Vector3(0f,-0.25f,centerZ),new Vector3(width,0.5f,depth));
        Primitive("Wall_Left_ReplaceMe",parent,new Vector3(-width*0.5f,2.5f,centerZ),new Vector3(0.4f,5f,depth));
        Primitive("Wall_Right_ReplaceMe",parent,new Vector3(width*0.5f,2.5f,centerZ),new Vector3(0.4f,5f,depth));
        Primitive("Wall_Back_ReplaceMe",parent,new Vector3(0f,2.5f,front+depth),new Vector3(width,5f,0.4f));
        Primitive("FeedMaker_Position_ReplaceMe",parent,new Vector3(width*0.25f,0.75f,front+depth-3f),new Vector3(1.5f,1.5f,1.5f));
        Primitive("FeedSilo_Position_ReplaceMe",parent,new Vector3(-width*0.35f,1.5f,front+depth-3f),new Vector3(1.5f,3f,1.5f));
        GameObject stalls=new("AnimalArea_ReplaceMe");
        stalls.transform.SetParent(parent,false);
        stalls.transform.localPosition=new Vector3(0f,0.05f,2f);
    }

    static Transform GetOrCreatePortalContainer(Scene scene)
    {
        Transform world=scene.GetRootGameObjects().FirstOrDefault(root=>root.name=="30_WORLD")?.transform;
        if(world==null)
        {
            GameObject created=new("30_WORLD");
            SceneManager.MoveGameObjectToScene(created,scene);
            Undo.RegisterCreatedObjectUndo(created,"Create World Category");
            world=created.transform;
        }
        Transform buildings=world.Find("Buildings");
        if(buildings==null) {GameObject created=new("Buildings");Undo.RegisterCreatedObjectUndo(created,"Create Buildings Category");Undo.SetTransformParent(created.transform,world,"Parent Buildings");buildings=created.transform;}
        Transform portals=buildings.Find("BarnPortals_Editable");
        if(portals==null) {GameObject created=new("BarnPortals_Editable");Undo.RegisterCreatedObjectUndo(created,"Create Barn Portals");Undo.SetTransformParent(created.transform,buildings,"Parent Barn Portals");portals=created.transform;}
        return portals;
    }

    static string BarnAccessName(PropertySite site)
    {
        string id=site!=null && !string.IsNullOrWhiteSpace(site.SiteId) ? site.SiteId : "unknown-site";
        return "BarnAccess_"+id.Replace(' ','_');
    }

    static int RemoveLegacyPortalChildren(BarnInterior portal)
    {
        int removed=0;
        for(int index=portal.transform.childCount-1;index>=0;index--)
        {
            Transform child=portal.transform.GetChild(index);
            bool legacyRoom=child.name=="Room_Editable";
            bool obsoleteDoor=child.name=="ExteriorDoor" && portal.exteriorDoor!=child;
            if(!legacyRoom && !obsoleteDoor) continue;
            Undo.DestroyObjectImmediate(child.gameObject);
            removed++;
        }
        return removed;
    }

    static int RemoveOrphanPortals(Transform container,PropertySite[] sites)
    {
        HashSet<AnimalHome> validHomes=new(sites.Where(site=>site!=null)
            .Select(site=>site.GetComponent<AnimalHome>()).Where(home=>home!=null));
        int removed=0;
        for(int index=container.childCount-1;index>=0;index--)
        {
            BarnInterior portal=container.GetChild(index).GetComponent<BarnInterior>();
            if(portal==null || validHomes.Contains(portal.home)) continue;
            Undo.DestroyObjectImmediate(container.GetChild(index).gameObject);
            removed++;
        }
        return removed;
    }

    static int RemoveEmptyLegacyInteriorContainer(Transform buildings)
    {
        if(buildings==null) return 0;
        Transform legacy=buildings.Find("Interiors_Editable");
        if(legacy==null || legacy.childCount>0) return 0;
        Undo.DestroyObjectImmediate(legacy.gameObject);
        return 1;
    }

    static Transform EnsureEntranceMarker(AnimalHome home)
    {
        Transform parent=home.site!=null ? home.site.BuildingAnchor : home.transform;
        Transform existing=home.door!=null ? home.door : parent.Find("BarnEntrance_Editable");
        if(existing==null)
        {
            GameObject marker=new("BarnEntrance_Editable");
            Undo.RegisterCreatedObjectUndo(marker,"Create Barn Entrance");
            Undo.SetTransformParent(marker.transform,parent,"Parent Barn Entrance");
            existing=marker.transform;
            existing.localPosition=DefaultEntranceLocal(home.site!=null ? home.site.CurrentLevel : 1);
            existing.localRotation=Quaternion.identity;
            existing.localScale=Vector3.one;
        }
        else if(existing.parent!=parent)
            Undo.SetTransformParent(existing,parent,"Parent Barn Entrance");
        Undo.RecordObject(home,"Assign Barn Entrance");
        home.door=existing;
        EditorUtility.SetDirty(home);
        return existing;
    }

    static Vector3 DefaultEntranceLocal(int level) => Mathf.Clamp(level,1,4) switch
    {
        1=>new Vector3(0f,0f,8.7f),
        2=>new Vector3(0f,0f,13f),
        3=>new Vector3(0f,0f,-6.5f),
        4=>new Vector3(0f,0f,10.8f),
        _=>new Vector3(0f,0f,8.7f)
    };

    static BoxCollider EnsureExteriorBarrier(BarnInterior portal,AnimalHome home)
    {
        BoxCollider barrier=portal.GetComponent<BoxCollider>();
        if(barrier==null) barrier=Undo.AddComponent<BoxCollider>(portal.gameObject);
        Transform anchor=home.site!=null ? home.site.BuildingAnchor : home.transform;
        portal.transform.SetPositionAndRotation(anchor.position,anchor.rotation);
        portal.transform.localScale=Vector3.one;
        barrier.isTrigger=false;
        if(barrier.size==Vector3.zero || barrier.size==Vector3.one)
        {
            barrier.center=new Vector3(0f,2.5f,0f);
            barrier.size=new Vector3(12f,5f,14f);
        }
        return barrier;
    }

    static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes=EditorBuildSettings.scenes.ToList();
        if(scenes.All(scene=>scene.path!=path)) scenes.Add(new EditorBuildSettingsScene(path,true));
        EditorBuildSettings.scenes=scenes.ToArray();
    }

    static GameObject EnsureExteriorPrefab(int level)
    {
        string targetPath=$"{ExteriorFolder}/BarnExterior_Lv{level}.prefab";
        GameObject existing=AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
        if(existing!=null) return existing;

        GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(SourceExteriorPaths[level-1]);
        if(source==null)
        {
            Debug.LogError($"[BARN PREFAB] Model sumber Lv{level} tidak ditemukan: {SourceExteriorPaths[level-1]}");
            return null;
        }

        GameObject root=new($"BarnExterior_Lv{level}");
        try
        {
            GameObject model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);
            model.name="Model_Editable";
            model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            model.transform.localScale=Vector3.one;

            CalculateLocalRendererBounds(root.transform,model,out Vector3 minimum,out Vector3 maximum);
            Vector3 entranceLocal=FindExteriorEntrance(model,root.transform,minimum,maximum);
            foreach(Collider sourceCollider in model.GetComponentsInChildren<Collider>(true))
                sourceCollider.enabled=false;
            GameObject collisionObject=new("Collision_Editable");
            collisionObject.transform.SetParent(root.transform,false);
            BoxCollider collision=collisionObject.AddComponent<BoxCollider>();
            collision.center=(minimum+maximum)*0.5f;
            collision.size=maximum-minimum;

            GameObject entranceObject=new("Entrance_Editable");
            entranceObject.transform.SetParent(root.transform,false);
            entranceObject.transform.localPosition=entranceLocal;
            Vector3 towardCenter=(minimum+maximum)*0.5f-entranceLocal;
            towardCenter.y=0f;
            entranceObject.transform.localRotation=towardCenter.sqrMagnitude>0.001f
                ? Quaternion.LookRotation(towardCenter.normalized,Vector3.up)
                : Quaternion.identity;

            BarnExteriorAuthoring authoring=root.AddComponent<BarnExteriorAuthoring>();
            authoring.Configure(model.transform,collision,entranceObject.transform);
            GameObject saved=PrefabUtility.SaveAsPrefabAsset(root,targetPath);
            Debug.Log($"[BARN PREFAB] Dibuat: {targetPath}",saved);
            return saved;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void AssignExteriorPrefabs(GameObject[] prefabs)
    {
        BuildingDefinitionSO definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(BarnDefinitionPath);
        if(definition==null)
        {
            Debug.LogError($"[BARN PREFAB] Building Definition tidak ditemukan: {BarnDefinitionPath}");
            return;
        }
        Undo.RecordObject(definition,"Assign Modular Barn Prefabs");
        for(int level=1;level<=4;level++)
        {
            BuildingLevelDefinition levelData=definition.GetLevel(level);
            if(levelData!=null && prefabs[level-1]!=null)
                levelData.completedPrefab=prefabs[level-1];
        }
        EditorUtility.SetDirty(definition);
    }

    static void EnsureInteriorLayoutPrefabs()
    {
        Scene scene=SceneManager.GetSceneByPath(BarnScenePath);
        bool openedHere=!scene.IsValid() || !scene.isLoaded;
        if(openedHere) scene=EditorSceneManager.OpenScene(BarnScenePath,OpenSceneMode.Additive);

        GameObject root=scene.GetRootGameObjects().FirstOrDefault(item=>item.name=="BarnInterior_Editable");
        BarnInteriorSceneController controller=root!=null ? root.GetComponent<BarnInteriorSceneController>() : null;
        if(root==null || controller==null)
        {
            Debug.LogError("[BARN PREFAB] Root/controller BarnInterior tidak ditemukan.");
            if(openedHere) EditorSceneManager.CloseScene(scene,true);
            return;
        }

        List<GameObject> layouts=new();
        for(int level=1;level<=4;level++)
        {
            string objectName=$"Layout_Lv{level}_Editable";
            string prefabPath=$"{InteriorFolder}/BarnInteriorLayout_Lv{level}.prefab";
            Transform current=root.transform.Find(objectName);
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if(current!=null && prefab==null)
            {
                prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(current.gameObject,prefabPath,InteractionMode.UserAction);
                current=root.transform.Find(objectName);
                Debug.Log($"[BARN PREFAB] Layout Lv{level} dibuat dan terhubung: {prefabPath}",prefab);
            }
            else if(prefab!=null && (current==null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject)!=prefabPath))
            {
                Vector3 localPosition=current!=null ? current.localPosition : Vector3.zero;
                Quaternion localRotation=current!=null ? current.localRotation : Quaternion.identity;
                Vector3 localScale=current!=null ? current.localScale : Vector3.one;
                bool active=current==null ? level==1 : current.gameObject.activeSelf;
                if(current!=null) Object.DestroyImmediate(current.gameObject);
                GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                instance.name=objectName;
                instance.transform.SetParent(root.transform,false);
                instance.transform.SetLocalPositionAndRotation(localPosition,localRotation);
                instance.transform.localScale=localScale;
                instance.SetActive(active);
                current=instance.transform;
            }

            if(current!=null) layouts.Add(current.gameObject);
        }

        controller.EditorConfigureLayouts(layouts);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if(openedHere) EditorSceneManager.CloseScene(scene,true);
    }

    static Vector3 FindExteriorEntrance(GameObject model,Transform root,Vector3 minimum,Vector3 maximum)
    {
        Vector3 total=Vector3.zero;
        int count=0;
        foreach(Collider candidate in model.GetComponentsInChildren<Collider>(true))
        {
            if(candidate.name.IndexOf("Door",System.StringComparison.OrdinalIgnoreCase)<0 ||
               candidate.name.IndexOf("LOD",System.StringComparison.OrdinalIgnoreCase)>=0) continue;
            total+=root.InverseTransformPoint(candidate.bounds.center);
            count++;
        }
        Vector3 center=(minimum+maximum)*0.5f;
        Vector3 door=count>0 ? total/count : center+Vector3.back;
        Vector3 offset=door-center;
        if(Mathf.Abs(offset.x)>Mathf.Abs(offset.z))
            door.x=offset.x>=0f ? maximum.x+0.8f : minimum.x-0.8f;
        else
            door.z=offset.z>=0f ? maximum.z+0.8f : minimum.z-0.8f;
        door.y=minimum.y;
        return door;
    }

    static void CalculateLocalRendererBounds(Transform root,GameObject model,out Vector3 minimum,out Vector3 maximum)
    {
        minimum=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        maximum=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        Renderer[] renderers=model.GetComponentsInChildren<Renderer>(true);
        foreach(Renderer renderer in renderers)
        {
            Bounds bounds=renderer.localBounds;
            for(int x=-1;x<=1;x+=2)
            for(int y=-1;y<=1;y+=2)
            for(int z=-1;z<=1;z+=2)
            {
                Vector3 point=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                Vector3 local=root.InverseTransformPoint(renderer.transform.TransformPoint(point));
                minimum=Vector3.Min(minimum,local);
                maximum=Vector3.Max(maximum,local);
            }
        }
        if(renderers.Length==0)
        {
            minimum=new Vector3(-6f,0f,-7f);
            maximum=new Vector3(6f,5f,7f);
        }
    }

    static void EnsureAssetFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        int separator=path.LastIndexOf('/');
        string parent=path.Substring(0,separator);
        string name=path.Substring(separator+1);
        if(!AssetDatabase.IsValidFolder(parent)) EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent,name);
    }

    static GameObject Primitive(string name,Transform parent,Vector3 position,Vector3 scale)
    {
        GameObject result=GameObject.CreatePrimitive(PrimitiveType.Cube);
        result.name=name;
        result.transform.SetParent(parent,false);
        result.transform.localPosition=position;
        result.transform.localScale=scale;
        return result;
    }
}
