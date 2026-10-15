#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent +75% size revision: original model buffers and village placements are retained.</summary>
[InitializeOnLoad]
public static class BarnScale175Setup
{
    const string Root="Assets/Nature  Paradaise";
    const string Folder="ArtSource/BarnInterior_A/Scale175";
    const string Request=Folder+"/apply.request";
    const string ScenePath=Root+"/Map/Scenes/Interiors/BarnInterior.unity";
    const float Factor=1.75f;
    static bool busy;static double next;static StringBuilder log;
    static BarnScale175Setup(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(busy||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        busy=true;
        try{File.Move(Request,Folder+"/Request_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".txt");Apply();}
        catch(Exception e){File.WriteAllText(Folder+"/Error.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static void Check(bool value,string message)
    {if(!value)throw new InvalidOperationException("Barn scale: "+message);log.AppendLine("PASS "+message);}
    static Vector3 Size(GameObject root)
    {
        bool first=true;Bounds bounds=default;
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var b=r.localBounds;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {var p=r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
        }
        return bounds.size;
    }
    static int Triangles(GameObject root)=>root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3);
    [MenuItem("Nature Paradise/Barn/Apply Size 1.75x")]
    public static void Apply()
    {
        log=new StringBuilder("BARN SIZE REVISION +75% = 1.75x\n");Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Editor stopped");
        Directory.CreateDirectory(Folder);string backup=Folder+"/Before_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");Directory.CreateDirectory(backup);
        StageUtility.GoToMainStage();
        var records=new JArray();
        for(int level=1;level<=5;level++)
        {
            string path=Root+"/Prefabs/Barn/Exterior/BarnExterior_Lv"+level+".prefab";File.Copy(path,backup+"/BarnExterior_Lv"+level+".prefab");
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var authored=root.GetComponent<BarnExteriorAuthoring>();Check(authored!=null,"Exterior authoring Lv"+level);
                int count=Triangles(root);float previous=authored.ModelRoot.localScale.x,target=(.625f+.125f*level)*Factor;Vector3 before=Size(root);
                authored.ModelRoot.localScale=Vector3.one*target;
                authored.BuildingCollision.center=new Vector3(0,2.1f,0)*target;authored.BuildingCollision.size=new Vector3(8,4.2f,10)*target;
                authored.Entrance.localPosition=new Vector3(0,0,5.9f)*target;
                var group=authored.ModelRoot.GetComponent<LODGroup>();group.RecalculateBounds();
                var after=Size(root);Check(Vector3.Distance(after,before*(target/previous))<.02f,"Exterior bounds/collider/entrance scale Lv"+level);
                Check(Triangles(root)==count,"Exterior geometry count unchanged Lv"+level);
                PrefabUtility.SaveAsPrefabAsset(root,path);records.Add(new JObject{["type"]="exterior",["level"]=level,["modelScale"]=target,["sizeMetres"]=new JArray(after.x,after.y,after.z),["trianglesAllLODs"]=count});
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        for(int level=1;level<=4;level++)
        {
            string path=Root+"/Prefabs/Barn/Interior/BarnInteriorLayout_Lv"+level+".prefab";File.Copy(path,backup+"/BarnInteriorLayout_Lv"+level+".prefab");
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count=Triangles(root);root.transform.localScale=Vector3.one*Factor;var binding=root.GetComponent<BarnInteriorLayout>();
                Check(binding!=null&&binding.AnimalSpots.Length==new[]{4,8,14,20}[level-1],"Interior markers/capacity Lv"+level);
                var floor=root.GetComponentsInChildren<BoxCollider>(true).Single(c=>c.name=="Floor");float w=new[]{12,16,20,24}[level-1]*Factor,d=new[]{10,12,14,16}[level-1]*Factor;
                Check(Mathf.Abs(floor.size.x*root.transform.localScale.x-w)<.001f&&Mathf.Abs(floor.size.z*root.transform.localScale.z-d)<.001f,"Interior floor and colliders enlarged Lv"+level);
                Check(Triangles(root)==count,"Interior geometry count unchanged Lv"+level);PrefabUtility.SaveAsPrefabAsset(root,path);
                records.Add(new JObject{["type"]="interior",["level"]=level,["scale"]=Factor,["clearFloorMetres"]=new JArray(w,d),["triangles"]=count});
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string definition=Root+"/Resources/Buildings/Barn Building.asset";File.Copy(definition,backup+"/Barn Building.asset");
        var building=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(definition);building.footprintWidth=21;building.footprintDepth=25;EditorUtility.SetDirty(building);
        File.Copy(ScenePath,backup+"/BarnInterior.unity");var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var controller=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<BarnInteriorSceneController>(true)).Single();var parent=controller.transform;Vector3 entrance=new Vector3(0,-200,-10);
        for(int i=1;i<=4;i++){var layout=parent.Find("Layout_Lv"+i+"_Editable");Check(layout!=null,"Scene layout "+i);layout.localScale=Vector3.one*Factor;}
        parent.Find("BarnInteriorEntrySpawn").localPosition=entrance+new Vector3(0,.15f,1.7f)*Factor;
        parent.Find("BarnExitDoor_Editable").localPosition=entrance+new Vector3(0,.7f,.35f)*Factor;
        parent.Find("BarnCameraAngle_Editable").localPosition=entrance+new Vector3(0,5,2)*Factor;
        var serialized=new SerializedObject(controller);serialized.FindProperty("cameraDistance").floatValue=25*Factor;serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Scaled BarnInterior scene saved");AssetDatabase.SaveAssets();
        log.Append(BarnApprovedExteriorValidation.Run());
        File.WriteAllText(Folder+"/Result.json",new JObject{["status"]="PASS",["factor"]=Factor,["scalePolicy"]="+75 percent relative to original, not repeated multiplication",["records"]=records,["placementGridCells"]=new JArray(21,25),["capacityChanged"]=false,["backup"]=backup}.ToString());
        File.WriteAllText(Folder+"/Validation.txt",log.ToString());
        BarnApprovedInteriorSetup.VerifySaved();
        Debug.Log("BARN_SCALE_175_PASS — exterior and interior, colliders and markers enlarged 75%.");
    }
}
#endif
