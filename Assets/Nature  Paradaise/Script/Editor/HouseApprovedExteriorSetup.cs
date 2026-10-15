using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Approved house meshes, shared atlas, stair portals and fixed-camera authoring.</summary>
public static class HouseApprovedExteriorSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Models = Root + "/mesh/Buildings/House_A";
    const string Prefabs = Root + "/Prefabs/PlayerHouse/Exterior";
    const string DefinitionPath = Root + "/Resources/Buildings/Player House Building.asset";
    const string MaterialPath = Root + "/Pack/Toon Series/Toon Farm Pack/Models/Materials/";
    const string Output = "ArtSource/House_A/Previews";
    static readonly string[] Keys = { "Lv1", "Lv2", "Lv3_4", "Lv5" };
    static readonly float[] Width = { 9, 13, 9.6f, 15 }, Depth = { 7.5f, 8.5f, 8.5f, 8.5f };
    static readonly float[] PorchWidth = { 3.8f, 5.8f, 4.4f, 6.6f }, PorchDepth = { 2.2f, 2.8f, 2.4f, 3 };
    static readonly int[,] Tris = { {3831,2167,589}, {4661,2669,661}, {5715,3191,907}, {5030,3006,972} };
    static readonly Vector3[] BoundsMin = { new(-4.995f,0,-7.99f), new(-6.995f,0,-8.99f), new(-5.295f,0,-8.99f), new(-7.995f,0,-8.99f) };
    static readonly Vector3[] BoundsMax = { new(5.095f,9.035f,3.7f), new(7.095f,9.335f,4.3f), new(9.495f,9.335f,3.9f), new(11.04f,9.335f,4.5f) };
    static readonly string[] ScenePaths = { Root + "/Map/Scenes/World/Map.unity", Root + "/Map/Scenes/World/Map_GameplayPreview.unity", Root + "/Map/Scenes/Testing/TestingScene.unity" };
    static int ModelIndex(int level) => level <= 2 ? level-1 : level <= 4 ? 2 : 3;
    static void Require(bool value,string label) { if(!value) throw new InvalidOperationException("House A: " + label); }
    static Transform Child(Transform parent,string name) { var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform; }
    static Bounds Measure(Mesh mesh,Quaternion rotation)
    {
        var b=new Bounds(rotation*mesh.vertices[0],Vector3.zero);
        foreach(var v in mesh.vertices)b.Encapsulate(rotation*v);
        return b;
    }
    static float BoundsError(Bounds b,int index)
    {
        // Unity is left handed: Blender screen-right becomes local -X when front is +Z.
        var lo=BoundsMin[index];var hi=BoundsMax[index];
        return Vector3.Distance(b.min,new Vector3(-hi.x,lo.y,lo.z))+
               Vector3.Distance(b.max,new Vector3(-lo.x,hi.y,hi.z));
    }

    [MenuItem("Nature Paradise/House/Apply Approved House A Exterior",false,120)]
    public static void Apply()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play Mode before applying");
        Directory.CreateDirectory(Models);Directory.CreateDirectory(Prefabs);Directory.CreateDirectory(Output);
        var materials=new[] { AssetDatabase.LoadAssetAtPath<Material>(MaterialPath+"TFP_Atlas_1A.mat"), AssetDatabase.LoadAssetAtPath<Material>(MaterialPath+"TFP_Atlas_Lights_1A.mat") };
        Require(materials.All(m=>m),"Shared atlas materials exist");
        Require(materials[0].GetTexture("_MainTex")!=null,"Opaque atlas texture exists");
        var meshes=new Mesh[4,3];var rotations=new Quaternion[4,3];
        var report=new StringBuilder("HOUSE A UNITY APPLY\nSolid blue opaque bay; 2 shared atlas materials; 4 unique designs / 5 levels.\n");
        for(int model=0;model<4;model++)for(int lod=0;lod<3;lod++)
        {
            string filename=$"House_A_{Keys[model]}_LOD{lod}.fbx",path=Models+"/"+filename;
            File.Copy("ArtSource/House_A/Exports/"+filename,path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;
            importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
            importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var mat in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);
            importer.SaveAndReimport();
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var filter=imported.GetComponentsInChildren<MeshFilter>(true).Single();
            meshes[model,lod]=filter.sharedMesh;
            Require(filter.sharedMesh.triangles.Length/3==Tris[model,lod],filename+" triangle count");
            Require(filter.sharedMesh.subMeshCount==2,filename+" two atlas submeshes");
            Require(Vector3.Distance(filter.transform.lossyScale,Vector3.one)<.001f,"Imported metre scale");
            // Retain FBX node conversion. Resolve front from complete asymmetric approved bounds.
            var baseRotation=filter.transform.rotation;
            rotations[model,lod]=Enumerable.Range(0,4).Select(y=>Quaternion.Euler(0,90*y,0)*baseRotation)
                .OrderBy(q=>BoundsError(Measure(filter.sharedMesh,q),model)).First();
            Bounds b=Measure(filter.sharedMesh,rotations[model,lod]);
            Require(BoundsError(b,model)<(lod==0?.11f:.27f),filename+" metre bounds/front match approved coordinates: "+b);
            if(model==1 || model==3)
            {
                var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.GetTriangles(1);int skylightFaces=0;
                for(int k=0;k<indices.Length;k+=3)
                {
                    var a=rotations[model,lod]*vertices[indices[k]];var c=rotations[model,lod]*vertices[indices[k+1]];var d=rotations[model,lod]*vertices[indices[k+2]];
                    if((a.y+c.y+d.y)/3<5)continue;
                    Require(Vector3.Cross(c-a,d-a).normalized.y>.7f,filename+" skylight faces outward for backface culling");skylightFaces++;
                }
                Require(skylightFaces==(model==1?2:4),filename+" visible skylight pane count");
            }
            foreach(var renderer in imported.GetComponentsInChildren<MeshRenderer>(true))
                Require(renderer.sharedMaterials.All(m=>materials.Contains(m)),"Imported materials remapped to existing pack");
            // Blue panel UV is on the audited RGB 137/178/197 swatch, never transparent glass.
            if(model==3)
            {
                int blue=filter.sharedMesh.GetIndices(0).Distinct().Count(i=>Vector2.Distance(filter.sharedMesh.uv[i],new Vector2(.2109375f,.078125f))<.00001f);
                Require(blue>=10,"Lv5 solid-blue pane vertices use atlas UV");
            }
            report.AppendLine(filename+": "+Tris[model,lod]+" triangles, atlas/front/bounds PASS");
        }
        var definition=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(DefinitionPath);
        Require(definition && definition.levels.Count>=5,"Existing house definition has Lv1-5");
        var prefabs=new GameObject[5];
        for(int level=1;level<=5;level++)
        {
            prefabs[level-1]=BuildPrefab(level,meshes,rotations,materials);
            definition.GetLevel(level).completedPrefab=prefabs[level-1];
            ValidatePrefab(prefabs[level-1],level,meshes,materials,report);
        }
        // House cannot be relocated; reserve the largest visual envelope plus stair approach.
        definition.footprintWidth=22;definition.footprintDepth=17;
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
        var prior=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(string path in ScenePaths)if(File.Exists(path))ApplyScene(path,prefabs,definition,report);
        }
        finally
        {
            // A fresh batch editor can start without any loaded scene to restore.
            if(prior.Any(s=>s.isLoaded) && prior.Any(s=>s.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(prior);
        }
        for(int model=0;model<4;model++)for(int lod=0;lod<3;lod++)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Models+$"/House_A_{Keys[model]}_LOD{lod}.fbx");
            importer.isReadable=false;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();report.AppendLine("APPLY_PASS");
        File.WriteAllText(Output+"/UnityImportAudit.txt",report.ToString());
        Debug.Log("[House A] APPLY_PASS: Lv1-5, opaque blue atlas, stair E, camera-facing front.");
    }
    static GameObject BuildPrefab(int level,Mesh[,] meshes,Quaternion[,] rotations,Material[] materials)
    {
        int i=ModelIndex(level);string path=Prefabs+$"/HouseExterior_Lv{level}.prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root=existing?PrefabUtility.LoadPrefabContents(path):new GameObject($"HouseExterior_Lv{level}");
        try
        {
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var model=Child(root.transform,"House_A_Model");var group=model.gameObject.AddComponent<LODGroup>();
            var lods=new LOD[3];float[] thresholds={.28f,.10f,.012f};
            for(int lod=0;lod<3;lod++)
            {
                var t=Child(model,"House_A_LOD"+lod);t.localRotation=rotations[i,lod];
                t.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[i,lod];
                var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;
                lods[lod]=new LOD(thresholds[lod],new Renderer[]{renderer});
            }
            group.SetLODs(lods);group.RecalculateBounds();
            Box(root,"BodyCollision",new Vector3(0,2.4f,-Depth[i]/2),new Vector3(Width[i],4.8f,Depth[i]));
            if(i==2)Box(root,"WingCollision",new Vector3(-6.8f,2.075f,-3.9f),new Vector3(4.2f,4.15f,6.8f));
            if(i==3)Box(root,"BlueBayCollision",new Vector3(-9.15f,2.275f,-3.6f),new Vector3(3.3f,4.55f,5.8f));
            float pd=PorchDepth[i],pw=PorchWidth[i];
            Box(root,"PorchCollision",new Vector3(0,.3f,pd/2),new Vector3(pw,.6f,pd));
            for(int step=0;step<3;step++)
            {
                float height=.6f-.2f*step;
                Box(root,"StairCollision"+step,new Vector3(0,height/2,pd+.25f+.5f*step),new Vector3(pw-.35f,height,.5f));
            }
            var entrance=Child(root.transform,"Stairs_Interact_E");entrance.localPosition=new Vector3(0,0,pd+.75f);
            var trigger=entrance.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;
            trigger.center=new Vector3(0,1.8f,0);trigger.size=new Vector3(pw-.35f,3.6f,1.5f);
            var portal=entrance.gameObject.AddComponent<HouseScenePortal>();portal.Configure(false,"HouseInterior","house-interior-entry","player-house-exit");
            var data=new SerializedObject(portal);data.FindProperty("interactionRadius").floatValue=.8f;
            data.FindProperty("promptHeight").floatValue=1.5f;data.ApplyModifiedPropertiesWithoutUndo();
            var exit=Child(root.transform,"PlayerHouseExitSpawn");exit.localPosition=new Vector3(0,0,pd+2.4f);
            exit.gameObject.AddComponent<PlayerSpawnPoint>().Configure("player-house-exit");
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { if(existing)PrefabUtility.UnloadPrefabContents(root);else Object.DestroyImmediate(root); }
    }
    static void Box(GameObject root,string name,Vector3 center,Vector3 size)
    {
        var t=Child(root.transform,name);var box=t.gameObject.AddComponent<BoxCollider>();box.center=center;box.size=size;
    }
    static void ValidatePrefab(GameObject prefab,int level,Mesh[,] meshes,Material[] materials,StringBuilder report)
    {
        int i=ModelIndex(level);var lods=prefab.GetComponentInChildren<LODGroup>(true).GetLODs();
        Require(lods.Length==3,"Three authored LODs");
        for(int lod=0;lod<3;lod++)
        {
            Require(lods[lod].renderers.Length==1,"One mesh renderer per LOD");
            Require(lods[lod].renderers[0].GetComponent<MeshFilter>().sharedMesh==meshes[i,lod],"Correct shared level mesh");
            Require(lods[lod].renderers[0].sharedMaterials.SequenceEqual(materials),"Two existing shared materials");
        }
        var portal=prefab.GetComponentInChildren<HouseScenePortal>(true);
        Require(portal && portal.GetComponent<BoxCollider>().isTrigger,"Stair portal trigger");
        Require(Mathf.Abs(portal.transform.localPosition.z-(PorchDepth[i]+.75f))<.001f,"E anchor on stairs");
        var ps=new SerializedObject(portal);Require(ps.FindProperty("interactKey").intValue==(int)KeyCode.E,"E interaction key");
        var preview=EditorSceneManager.NewPreviewScene();var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
        var player=new GameObject("PortalAuditPlayer");SceneManager.MoveGameObjectToScene(player,preview);
        var capsule=player.AddComponent<CharacterController>();capsule.height=3.6f;capsule.center=new Vector3(0,1.8f,0);capsule.radius=.4f;
        try
        {
            var p=instance.GetComponentInChildren<HouseScenePortal>();
            player.transform.position=new Vector3(0,.35f,PorchDepth[i]+.75f);Physics.SyncTransforms();Require(p.CanInteract(player.transform),"Player on steps can enter");
            player.transform.position=new Vector3(0,0,-Depth[i]-2);Physics.SyncTransforms();Require(!p.CanInteract(player.transform),"Rear facade cannot enter");
            player.transform.position=new Vector3(PorchWidth[i]+4,0,PorchDepth[i]+.75f);Physics.SyncTransforms();Require(!p.CanInteract(player.transform),"Far side cannot enter");
        }
        finally { Object.DestroyImmediate(player);EditorSceneManager.ClosePreviewScene(preview); }
        report.AppendLine($"Lv{level}: correct prefab/LOD/shared materials/stair E/rear rejection PASS");
    }
    static void ApplyScene(string path,GameObject[] prefabs,BuildingDefinitionSO definition,StringBuilder report)
    {
        Directory.CreateDirectory("Library/HouseExteriorBackups");string backup="Library/HouseExteriorBackups/"+Path.GetFileNameWithoutExtension(path)+".unity";
        if(!File.Exists(backup))File.Copy(path,backup);
        Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var house=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerHouseController>(true)).Single();
        var root=house.transform;
        var follow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TopDownCameraFollow>(true)).FirstOrDefault();
        var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.CompareTag("MainCamera"));
        Vector3 front=follow?Quaternion.Euler(0,follow.Yaw,0)*Vector3.back:camera?Vector3.ProjectOnPlane(-camera.transform.forward,Vector3.up).normalized:Vector3.back;
        var legacyDoor=root.GetComponentsInChildren<HouseScenePortal>(true).FirstOrDefault(p=>p.name=="HouseEntranceDoor_Editable");
        bool alreadyApplied=root.Find("HouseExterior_Lv5_Approved")!=null;
        Vector3 doorGround=legacyDoor?legacyDoor.transform.position:root.position;
        if(!alreadyApplied)root.position=new Vector3(doorGround.x,root.position.y,doorGround.z);
        root.rotation=Quaternion.LookRotation(front,Vector3.up);root.localScale=Vector3.one;
        if(legacyDoor)legacyDoor.gameObject.SetActive(false);
        foreach(var spawn in root.GetComponentsInChildren<PlayerSpawnPoint>(true))
            if(spawn.transform.parent==root && spawn.SpawnId=="player-house-exit")spawn.gameObject.SetActive(false);
        foreach(var t in root.Cast<Transform>().ToArray())
        {
            if(t.name.StartsWith("HouseExterior_Lv") && t.name.EndsWith("_Approved"))Object.DestroyImmediate(t.gameObject);
            else if(t.name.StartsWith("HouseExterior_Lv"))t.gameObject.SetActive(false);
        }
        var so=new SerializedObject(house);so.FindProperty("houseDefinition").objectReferenceValue=definition;
        var list=so.FindProperty("exteriorLevelVisuals");list.arraySize=5;
        int current=Mathf.Clamp(so.FindProperty("currentLevel").intValue,1,5);
        for(int level=1;level<=5;level++)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[level-1],scene);obj.name=$"HouseExterior_Lv{level}_Approved";
            obj.transform.SetParent(root,false);obj.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);obj.SetActive(level==current);
            list.GetArrayElementAtIndex(level-1).objectReferenceValue=obj;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Require(Vector3.Dot(root.forward,front)>.999f,"House door faces fixed camera");
        Require(root.GetComponentsInChildren<HouseScenePortal>().Length==1,"Exactly one active stair portal in authored scene");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        report.AppendLine(path+": five authored levels, fixed-camera facing "+front+", old facade retired, stair portal PASS");
    }
}

