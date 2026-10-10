using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CalendarCafeSetup
{
    const string Root="Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/Shop/Refresh Cafe and Calendar Layouts")]
    public static void RefreshLayouts()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        foreach(string path in new[]{Root+"/Prefabs/Shops/Testing/06_Cafe_Shop.prefab",Root+"/Resources/UI/House Calendar.prefab"})
        {
            if(!File.Exists(path))continue;var go=PrefabUtility.LoadPrefabContents(path);var ui=go.GetComponent<DataDrivenModal>();if(ui.uiRoot!=null)Object.DestroyImmediate(ui.uiRoot);ui.Build();PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
        }
    }
    [MenuItem("Nature Paradise/Shop/Create Cafe and House Calendar")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        ScheduleCafeCsvDatabase.EnsureAssets();
        Directory.CreateDirectory(Root+"/Prefabs/Shops/Testing");Directory.CreateDirectory(Root+"/Resources/UI");
        var map=SceneManager.GetSceneByPath(Root+"/Map/Scenes/World/Map.unity");if(!map.isLoaded)map=EditorSceneManager.OpenScene(Root+"/Map/Scenes/World/Map.unity",OpenSceneMode.Additive);
        var shops=map.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopFront>(true)).Where(s=>s.catalog!=null&&s.interactionEnabled).ToArray();
        var first=shops.FirstOrDefault(s=>s.catalog.kind==ShopKind.Crops);var parent=first!=null?first.transform.parent:null;
        var seller=map.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCSeller>(true)).FirstOrDefault();
        string cafePath=Root+"/Prefabs/Shops/Testing/06_Cafe_Shop.prefab";
        if(!File.Exists(cafePath))
        {
            var go=new GameObject("06_Cafe_Shop");var cafe=go.AddComponent<CafeUI>();cafe.catalog=AssetDatabase.LoadAssetAtPath<CafeCatalog>(ScheduleCafeCsvDatabase.CafePath);cafe.Build();
            var capsule=GameObject.CreatePrimitive(PrimitiveType.Capsule);capsule.name="Seller_Capsule_Editable";capsule.transform.SetParent(go.transform,false);
            capsule.transform.localScale=seller!=null?seller.transform.lossyScale:new Vector3(.7f,1,.7f);capsule.transform.localPosition=Vector3.up*capsule.transform.localScale.y;
            if(seller!=null&&seller.TryGetComponent<Renderer>(out var renderer))capsule.GetComponent<Renderer>().sharedMaterials=renderer.sharedMaterials;
            Label(go.transform,"KAFE & RUMAH MAKAN");PrefabUtility.SaveAsPrefabAsset(go,cafePath);Object.DestroyImmediate(go);
        }
        if(!map.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CafeUI>(true)).Any())
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(cafePath),map);if(parent!=null)go.transform.SetParent(parent,true);
            Vector3 pos=(first!=null?first.transform.position:Vector3.zero)+Vector3.right*25;
            foreach(var t in Terrain.activeTerrains)if(pos.x>=t.transform.position.x&&pos.z>=t.transform.position.z&&pos.x<=t.transform.position.x+t.terrainData.size.x&&pos.z<=t.transform.position.z+t.terrainData.size.z){pos.y=t.SampleHeight(pos)+t.transform.position.y;break;}
            go.transform.position=pos;EditorSceneManager.MarkSceneDirty(map);
        }
        string calendarPath=Root+"/Resources/UI/House Calendar.prefab";
        if(!File.Exists(calendarPath))
        {
            var go=new GameObject("Calendar_Interactable_Editable");var calendar=go.AddComponent<CalendarUI>();calendar.data=AssetDatabase.LoadAssetAtPath<GameScheduleData>(ScheduleCafeCsvDatabase.SchedulePath);calendar.Build();calendar.interactionRadius=2;
            var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="Calendar_ModelSlot";board.transform.SetParent(go.transform,false);board.transform.localPosition=new Vector3(0,1.6f,0);board.transform.localScale=new Vector3(1.2f,.8f,.08f);
            Label(go.transform,"KALENDER");PrefabUtility.SaveAsPrefabAsset(go,calendarPath);Object.DestroyImmediate(go);
        }
        string housePath=Root+"/Map/Scenes/Interiors/HouseInterior.unity";
        var houseScene=SceneManager.GetSceneByPath(housePath);bool opened=!houseScene.isLoaded;if(opened)houseScene=EditorSceneManager.OpenScene(housePath,OpenSceneMode.Additive);
        var house=houseScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HouseInteriorController>(true)).FirstOrDefault();
        if(house!=null)
        {
            foreach(Transform layout in house.transform)
            {
                if(!layout.name.StartsWith("InteriorLayout_Lv")||layout.GetComponentInChildren<CalendarUI>(true)!=null)continue;
                var calendar=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(calendarPath),houseScene);calendar.transform.SetParent(layout,false);calendar.transform.localPosition=new Vector3(.25f,0,4.3f);calendar.transform.localRotation=Quaternion.Euler(0,90,0);
            }
            EditorSceneManager.MarkSceneDirty(houseScene);EditorSceneManager.SaveScene(houseScene);
        }
        if(opened)EditorSceneManager.CloseScene(houseScene,true);
        EditorSceneManager.SaveScene(map);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        if(!Directory.Exists(ScheduleCafeCsvDatabase.Root))ScheduleCafeCsvDatabase.Export();
        Debug.Log("[CALENDAR CAFE] Installed cafe and house calendars; empty menu preserved.");
    }
    static void Label(Transform parent,string title)
    {
        var text=new GameObject("StoreLabel_Editable").AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=new Vector3(0,2.65f,0);text.text=title;text.fontSize=3;text.alignment=TextAlignmentOptions.Center;text.color=GameplayHUDStyle.TextColor;text.rectTransform.sizeDelta=new Vector2(7,1.1f);text.raycastTarget=false;text.gameObject.AddComponent<ShopWorldLabel>();
    }
}
[CustomEditor(typeof(CafeUI))]public class CafeUIEditor:Editor
{
    public override void OnInspectorGUI(){DrawDefaultInspector();var ui=(CafeUI)target;if(GUILayout.Button(Application.isPlaying?"Open Cafe For Testing":"Preview UI In Hierarchy"))ui.Open();if(GUILayout.Button("Close / Hide Preview"))ui.Close();if(ui.catalog!=null&&GUILayout.Button("Edit categories, menu and image slots"))Selection.activeObject=ui.catalog;}
}
[CustomEditor(typeof(CalendarUI))]public class CalendarUIEditor:Editor
{
    public override void OnInspectorGUI(){DrawDefaultInspector();var ui=(CalendarUI)target;if(GUILayout.Button(Application.isPlaying?"Open Calendar For Testing":"Preview UI In Hierarchy"))ui.Open();if(GUILayout.Button("Close / Hide Preview"))ui.Close();if(ui.data!=null&&GUILayout.Button("Edit schedule and event image slots"))Selection.activeObject=ui.data;}
}
