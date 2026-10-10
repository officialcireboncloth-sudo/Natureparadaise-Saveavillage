using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Creates the shared worker profile and Lumber catalog without rebuilding or relocating the user's scene.</summary>
public static class ConstructionWorkerSetup
{
    const string Root="Assets/Nature  Paradaise";
    [MenuItem("Nature Paradise/Construction/Create Worker Settings and Lumber Catalog")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Root+"/Resources/Buildings");Directory.CreateDirectory(Root+"/Material/Construction");
        AssetDatabase.Refresh();
        string path=Root+"/Resources/Buildings/Construction Worker Settings.asset";
        var settings=AssetDatabase.LoadAssetAtPath<ConstructionWorkerSettings>(path);
        if(settings==null){settings=ScriptableObject.CreateInstance<ConstructionWorkerSettings>();AssetDatabase.CreateAsset(settings,path);}
        settings.wood ??= MakeMaterial("Scaffold Wood",new Color(.49f,.30f,.16f));
        settings.stone ??= MakeMaterial("Foundation Stone",new Color(.58f,.61f,.59f));
        settings.roof ??= MakeMaterial("Roof Clay",new Color(.48f,.25f,.17f));
        settings.workerClothes ??= MakeMaterial("Builder Clothes",new Color(.22f,.36f,.43f));
        settings.workerSkin ??= MakeMaterial("Builder Skin",new Color(.95f,.79f,.62f));
        settings.workerHat ??= MakeMaterial("Builder Hat",new Color(.83f,.64f,.30f));
        settings.progressTextShader ??= Shader.Find("TextMeshPro/Distance Field Overlay");
        EditorUtility.SetDirty(settings);
        path=Root+"/Resources/Buildings/Lumber Building Catalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<BuildingCatalogSO>(path);
        if(catalog==null)
        {
            catalog=ScriptableObject.CreateInstance<BuildingCatalogSO>();AssetDatabase.CreateAsset(catalog,path);
            var serialized=new SerializedObject(catalog);var buildings=serialized.FindProperty("buildings");
            var definitions=Resources.LoadAll<BuildingDefinitionSO>("Buildings").Where(x=>x.category!=BuildingCategory.House).OrderBy(x=>x.displayName).ToArray();
            buildings.arraySize=definitions.Length;for(int i=0;i<definitions.Length;i++)buildings.GetArrayElementAtIndex(i).objectReferenceValue=definitions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        // Empty global Coop sites need a final exterior, just as Barn and Fish Pond already have.
        var coop=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Root+"/Resources/Buildings/Coop Building.asset");
        if(coop!=null)
        {
            Directory.CreateDirectory(Root+"/Prefabs/Coop/Exterior");AssetDatabase.Refresh();
            foreach(var level in coop.levels)
            {
                if(level.completedPrefab!=null)continue;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Various Presets/TFP_Chicken_Coop_Preset_"+(level.level<3?"01":"02")+".prefab");
                if(source==null)continue;
                var go=new GameObject("CoopExterior_Lv"+level.level);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(go.transform,false);
                // Size to the existing placement footprint; never modify the source pack prefab.
                var renderers=model.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float scale=Mathf.Min((coop.footprintWidth-.2f)/Mathf.Max(.1f,bounds.size.x),(coop.footprintDepth-.2f)/Mathf.Max(.1f,bounds.size.z));
                    model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
                }
                level.completedPrefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Coop/Exterior/CoopExterior_Lv"+level.level+".prefab");
                Object.DestroyImmediate(go);
            }
            EditorUtility.SetDirty(coop);
        }
        var shed=AssetDatabase.LoadAssetAtPath<BuildingDefinitionSO>(Root+"/Resources/Buildings/Shed Building.asset");
        if(shed!=null)
        {
            Directory.CreateDirectory(Root+"/Prefabs/Buildings/Shed");AssetDatabase.Refresh();
            foreach(var level in shed.levels)
            {
                if(level.completedPrefab!=null)continue;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Pack/Toon Series/Toon Farm Pack/Prefabs/Buildings/Farm Structures/TFP_Shed_0"+Mathf.Min(2,level.level)+"A.prefab");
                if(source==null)continue;
                var go=new GameObject("ShedExterior_Lv"+level.level);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(go.transform,false);
                var renderers=model.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float scale=Mathf.Min((shed.footprintWidth-.2f)/Mathf.Max(.1f,bounds.size.x),(shed.footprintDepth-.2f)/Mathf.Max(.1f,bounds.size.z));
                    model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
                }
                level.completedPrefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Buildings/Shed/ShedExterior_Lv"+level.level+".prefab");Object.DestroyImmediate(go);
            }
            EditorUtility.SetDirty(shed);
        }
        string lumberPath=Root+"/Prefabs/Shops/Testing/05_Lumber_Store.prefab";
        if(File.Exists(lumberPath))
        {
            var go=PrefabUtility.LoadPrefabContents(lumberPath);var front=go.GetComponent<UpgradeShopFront>();
            if(front.constructionCatalog==null)front.constructionCatalog=catalog;
            if(front.builderDeparturePoint==null)
            {
                var point=new GameObject("BuilderDeparture_Editable").transform;point.SetParent(front.transform,false);point.localPosition=Vector3.forward*2.2f;
                front.builderDeparturePoint=point;
            }
            front.EnsureConstructionTabs();PrefabUtility.SaveAsPrefabAsset(go,lumberPath);PrefabUtility.UnloadPrefabContents(go);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[CONSTRUCTION SETUP] Worker profile, Lumber catalog and missing Coop exteriors ready. Existing scene transforms and UI overrides preserved.");
    }
    static Material MakeMaterial(string name,Color color)
    {
        string path=Root+"/Material/Construction/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material!=null)return material;
        var shader=Shader.Find("Nature Paradise/Anime Surface")??Shader.Find("Universal Render Pipeline/Lit");
        material=new Material(shader){name=name,color=color};AssetDatabase.CreateAsset(material,path);return material;
    }
}
