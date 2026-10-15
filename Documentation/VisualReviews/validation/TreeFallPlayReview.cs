#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Temporary Play Mode fixture in its own scene. Never edits Map or player save.
public sealed class TreeFallPlayReview : MonoBehaviour
{
    const string Root="Assets/Nature  Paradaise";
    internal const string ScenePath="Assets/TreeFallRegression_Test.unity";
    internal const string Active="Library/TreeFallPlayReview.active";
    internal const string Result="Library/TreeFallPlayReview.result";
    static object Read(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static object Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if(File.Exists(Active) && SceneManager.GetActiveScene().name=="TreeFallRegression_Test")
            new GameObject("Tree regression runner").AddComponent<TreeFallPlayReview>();
    }
    IEnumerator Start()
    {
        var iterator=Test();Exception error=null;
        while(true)
        {
            bool next=false;object wait=null;
            try{next=iterator.MoveNext();if(next)wait=iterator.Current;}catch(Exception ex){error=ex;}
            if(error!=null || !next)break;
            yield return wait;
        }
        if(error!=null){File.WriteAllText(Result,"FAIL: "+error);Debug.LogException(error);}
        EditorApplication.isPlaying=false;
    }
    IEnumerator Test()
    {
        Check(Object.FindFirstObjectByType<SaveManager>()==null,"Isolated scene must not contain SaveManager");
        string mode=File.ReadAllText(Active).Trim();var report=new StringBuilder(mode+" native Play Mode tree streaming/vegetation review\n");
        for(int scenario=0;scenario<(mode=="before"?1:3);scenario++)
        {
        report.AppendLine("Scenario "+scenario+": "+(scenario==0?"tree manager first":scenario==1?"vegetation initializes first":"host absent in authored terrain; managers create shared host"));
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SoStylized/Environment/Trees/Oak/Prefabs/P_OakTree3.prefab");
        var prefab=Resources.Load<GameObject>("Prefabs/Trees/P_OakTree3_Interactive 1").GetComponent<WorldTree>();
        var data=new TerrainData{heightmapResolution=33,size=new Vector3(40,10,40)};
        data.treePrototypes=new[]{new TreePrototype{prefab=source}};
        var original=new TreeInstance{position=new Vector3(.5f,0,.5f),prototypeIndex=0,widthScale=.5f,heightScale=.5f,color=Color.white,lightmapColor=Color.white};
        data.SetTreeInstances(new[]{original},false);
        var terrainObject=Terrain.CreateTerrainGameObject(data);terrainObject.SetActive(false);
        var terrain=terrainObject.GetComponent<Terrain>();terrainObject.transform.position=new Vector3(-10000,0,-10000);
        var player=new GameObject("Test observer").transform;player.position=terrainObject.transform.position+new Vector3(20,0,22);
        var host=scenario<2?terrainObject.AddComponent<TerrainRuntimeDataHost>():null;
        TerrainLayerVegetation vegetation=null;
        if(scenario==1)
        {
            vegetation=terrainObject.AddComponent<TerrainLayerVegetation>();Set(vegetation,"terrain",terrain);
            Invoke(vegetation,"EnsureWritableData");
        }
        var manager=terrainObject.AddComponent<TerrainTreeManager>();
        Set(manager,"targetTerrain",terrain);Set(manager,"player",player);Set(manager,"terrainId","regression-only");Set(manager,"logSetup",false);
        Set(manager,"prototypeSettings",new[]{new TerrainTreeManager.PrototypeSetting{terrainPrefab=source,interactivePrefab=prefab,choppable=true,removeStumpAfterFelling=true}});
        terrainObject.SetActive(true);
        // Add vegetation AFTER manager initialization: the production duplication failure.
        if(vegetation==null)vegetation=terrainObject.AddComponent<TerrainLayerVegetation>();
        host=terrainObject.GetComponent<TerrainRuntimeDataHost>();
        if(mode!="before")
        {
            var grass=terrainObject.AddComponent<TerrainDetailGrassManager>();
            Check((TerrainData)Read(grass,"runtimeData")==host.RuntimeData,"Wild grass shares runtime data");
        }
        bool sharedBefore=terrain.terrainData==host.RuntimeData;
        Check(manager.Records.Count==1,"Fixture has one mapped tree");
        var record=manager.Records[0];Invoke(manager,"Activate",record);
        var tree=record.activeObject;
        Check(tree!=null && tree.IsAvailable,"Interactive tree spawned");
        bool duplicateAtActivation=terrain.terrainData.GetTreeInstance(0).widthScale>0;
        report.AppendLine("Shared TerrainData after vegetation enable="+sharedBefore+"; duplicate standing Terrain tree at activation="+duplicateAtActivation);
        for(int hit=0;hit<tree.StandingDurability;hit++)Check(tree.Chop(4),"Chop accepted");
        Check(tree.IsTransitioning,"Actual falling coroutine started");
        yield return new WaitForSeconds(1.1f);
        var saved=manager.CaptureManagedTrees();
        Check(saved[0].state==TreeGrowthState.Depleted,"Felling completed and captured as depleted");
        bool ghostAfterFall=terrain.terrainData.GetTreeInstance(0).widthScale>0;
        report.AppendLine("Tree after fall="+saved[0].state+"; ghost standing Terrain visual="+ghostAfterFall);
        if(mode=="before")Check(!sharedBefore && duplicateAtActivation && ghostAfterFall,"Expected duplicated TerrainData bug reproduced");
        else
        {
            Check(sharedBefore && !duplicateAtActivation && !ghostAfterFall,"No duplicate Terrain visual before/during/after fall");
            Check(terrain.GetComponent<TerrainCollider>().terrainData==host.RuntimeData,"Collider shares runtime terrain");
            int drops=Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None).Length;
            Check(drops==1,"Wood emitted once after fall");
            manager.CaptureManagedTrees();Check(Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None).Length==drops,"Repeated snapshot does not duplicate wood");
            vegetation.enabled=false;yield return null;
            Check(terrain.terrainData==host.RuntimeData && host.RuntimeData!=null,"Disabling vegetation keeps shared terrain alive");
            vegetation.enabled=true;yield return null;
            Check(terrain.terrainData==host.RuntimeData && terrain.terrainData.GetTreeInstance(0).widthScale==0,"Re-enable cannot resurrect felled tree");
            manager.RestoreManagedTrees(saved);yield return null;
            Check(terrain.terrainData.GetTreeInstance(0).widthScale==0,"Loaded depleted tree remains invisible");
            Check(Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None).Length==drops,"Restore does not duplicate drops");
            Set(manager,"today",saved[0].respawnDay);Set(manager,"maintenanceIndex",0);Invoke(manager,"ProcessMaintenance");
            yield return null;yield return null;
            Check(record.state==TreeGrowthState.Standing,"Scheduled regrow returns standing state");
            Check(record.activeObject!=null || terrain.terrainData.GetTreeInstance(0).widthScale>0,"Regrow has a visible representation");
            Check(data.GetTreeInstance(0).widthScale==original.widthScale,"Authored TerrainData unchanged");
            report.AppendLine("PASS: one representation, real fall/depletion, wood once, save/restore, vegetation disable/re-enable, scheduled regrow, source data unchanged.");
        }
        foreach(var pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))Destroy(pickup.gameObject);
        Destroy(terrainObject);Destroy(player.gameObject);Destroy(data);yield return null;
        }
        File.WriteAllText("Documentation/VisualReviews/Tree-Fall-2026-10-12-"+mode+".txt",report.ToString());
        File.WriteAllText(Result,"PASS "+mode+"\n"+report);
    }
}

[InitializeOnLoad]
public static class TreeFallPlayReviewEditor
{
    const string Request="Library/TreeFallPlayReview.request";
    const string Previous="Library/TreeFallPlayReview.previous-startscene";
    static TreeFallPlayReviewEditor(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
    static void Poll()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
        string mode;try{mode=File.ReadAllText(Request).Trim();File.Delete(Request);}catch(IOException){return;}
        try
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene,TreeFallPlayReview.ScenePath);EditorSceneManager.CloseScene(scene,true);
            File.WriteAllText(Previous,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(TreeFallPlayReview.ScenePath);
            File.WriteAllText(TreeFallPlayReview.Active,mode);EditorApplication.EnterPlaymode();
        }
        catch(Exception ex){File.WriteAllText(TreeFallPlayReview.Result,"FAIL: "+ex);Debug.LogException(ex);}
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode || !File.Exists(TreeFallPlayReview.Active))return;
        string old=File.ReadAllText(Previous);
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(old)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(old);
        File.Delete(TreeFallPlayReview.Active);
    }
}
#endif
