#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Small freestanding timber bells to the front-right of player Barn/Coop doors.</summary>
public static class HousingBellSetup
{
    const string Root="Assets/Nature  Paradaise";
    public static void Configure(BarnExteriorAuthoring authored,bool coop)
    {
        var bell=authored.Bell;
        if(bell==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Resources/Buildings/AnimalBell.prefab");
            var copy=Object.Instantiate(source,authored.ModelRoot);copy.name="Front Right Animal Bell";
            bell=copy.GetComponentInChildren<AnimalBellVisual>(true);authored.ConfigureBell(bell);
        }
        float scale=Mathf.Max(.01f,authored.ModelRoot.lossyScale.x);
        Vector3 post=coop?new Vector3(-.20f,0,5.25f):new Vector3(-2.65f,0,4.35f);
        Vector3 approach=post+Vector3.forward*(.65f/Mathf.Max(.01f,authored.ModelRoot.lossyScale.z));
        bell.PlaceOnPost(authored.ModelRoot,post,approach,(coop?.56f:.68f)/scale);
    }
    [MenuItem("Nature Paradise/Art/Apply Front Right Barn and Coop Bells")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play before applying bells.");
        foreach(string kind in new[]{"Barn","Coop"})for(int level=1;level<=5;level++)
        {
            string path=$"{Root}/Prefabs/{kind}/Exterior/{kind}Exterior_Lv{level}.prefab";
            var contents=PrefabUtility.LoadPrefabContents(path);
            try{Configure(contents.GetComponent<BarnExteriorAuthoring>(),kind=="Coop");PrefabUtility.SaveAsPrefabAsset(contents,path);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
