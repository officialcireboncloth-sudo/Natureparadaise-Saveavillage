using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MiningInteriorSetup
{
    public const string ScenePath = "Assets/Nature  Paradaise/Map/Scenes/Interiors/CaveInterior.unity";
    [MenuItem("Nature Paradise/Mining/Create Cave Interior Preset")]
    public static void CreatePreset()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        { Selection.activeObject=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath); return; }
        for(int i=0;i<SceneManager.sceneCount;i++)
            if(string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
            { Debug.LogWarning("Simpan scene Untitled terlebih dahulu sebelum membuat preset gua secara additive."); return; }
        // Additive creation preserves all open world/house scenes and their unsaved edits.
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root=new GameObject("CaveInterior_ArtSlot"); root.AddComponent<CaveInteriorController>();
        var spawn=Child(root,"CaveEntrySpawn",new Vector3(0,1,0)); spawn.AddComponent<PlayerSpawnPoint>().Configure("cave-interior-entry");
        var exit=Child(root,"CaveExit_ArtSlot",new Vector3(0,1,-3));
        var collider=exit.AddComponent<BoxCollider>(); collider.isTrigger=true; collider.size=new Vector3(2,2,1);
        exit.AddComponent<HouseScenePortal>().Configure(true,"CaveInterior","cave-interior-entry","","Tambang");
        Child(root,"RockPrefabSlots",Vector3.zero);
        Child(root,"LadderAndNextFloorSlots",Vector3.zero);
        // Temporary test floor only, replace with cave art/colliders later.
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="DebugFloor_ReplaceWithCaveMesh";
        floor.transform.SetParent(root.transform,false); floor.transform.localPosition=new Vector3(0,-.25f,3);
        floor.transform.localScale=new Vector3(22,.5f,22);
        EditorSceneManager.SaveScene(scene,ScenePath);
        if(previous.IsValid()) SceneManager.SetActiveScene(previous);
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=scenes.ToArray(); Selection.activeGameObject=root;
    }
    [MenuItem("Nature Paradise/Mining/Add Cave Entrance To Selected Object")]
    static void AddEntrance()
    {
        var target=Selection.activeGameObject;
        if(target==null) { Debug.LogWarning("Pilih object pintu masuk gua di world dahulu."); return; }
        var portal=target.GetComponent<HouseScenePortal>();
        if(portal==null) portal=Undo.AddComponent<HouseScenePortal>(target);
        Undo.RecordObject(portal,"Configure Cave Entrance");
        portal.Configure(false,"CaveInterior","cave-interior-entry","","Tambang");
        EditorUtility.SetDirty(portal); EditorSceneManager.MarkSceneDirty(target.scene);
    }
    [MenuItem("Nature Paradise/Mining/Select HUD Image Slots")]
    static void SelectTheme() => Selection.activeObject=AssetDatabase.LoadAssetAtPath<MiningHUDTheme>("Assets/Nature  Paradaise/Resources/UI/MiningHUDTheme.asset");
    static GameObject Child(GameObject parent,string name,Vector3 position)
    {
        var child=new GameObject(name); child.transform.SetParent(parent.transform,false); child.transform.localPosition=position; return child;
    }
}
