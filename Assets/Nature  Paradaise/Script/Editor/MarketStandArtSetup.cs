#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

/// <summary>Imports the editable Blender stand export and updates its existing gameplay prefab without opening a world scene.</summary>
public static class MarketStandArtSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string ModelPath=Root+"/mesh/World/MarketStand/MarketStand_A.fbx";
    public const string VisualPath=Root+"/Prefabs/World/MarketStand/MarketStandVisual.prefab";
    public const string GameplayPath=Root+"/Prefabs/World/MarketStand.prefab";
    public const float VisualScale=.8f;
    [MenuItem("Nature Paradise/Art/Apply Atlas Market Stand")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring assets.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared Wood Atlas.mat");
        if(material==null)throw new InvalidOperationException("Shared wood atlas material is missing.");
        AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
        importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Low;
        importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Shared Wood Atlas"),material);importer.SaveAndReimport();
        if(!AssetDatabase.IsValidFolder(Root+"/Prefabs/World/MarketStand"))AssetDatabase.CreateFolder(Root+"/Prefabs/World","MarketStand");
        var root=new GameObject("MarketStandVisual");
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),root.transform);
            model.name="Atlas Model - Single Material";
            model.transform.localScale=Vector3.one*VisualScale;
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            foreach(var renderer in renderers){renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
            var near=renderers.Single(r=>r.name.Contains("LOD0"));var far=renderers.Single(r=>r.name.Contains("LOD1"));
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.18f,new Renderer[]{near}),new LOD(.015f,new Renderer[]{far})});lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,VisualPath);
        }
        finally{Object.DestroyImmediate(root);}
        var stand=PrefabUtility.LoadPrefabContents(GameplayPath);
        try
        {
            foreach(Transform child in stand.transform.Cast<Transform>().ToArray())
                if(child.name=="Visual_ToonFarm_ProduceStand"||child.name=="Visual_AtlasMarketStand")Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath),stand.transform);visual.name="Visual_AtlasMarketStand";
            var collider=stand.GetComponent<BoxCollider>();collider.center=new Vector3(0,.54f,0)*VisualScale;collider.size=new Vector3(3.3f,1.08f,1.4f)*VisualScale;
            var settings=new SerializedObject(stand.GetComponent<MarketStand>());settings.FindProperty("normalizeAtlasWorldScale").boolValue=true;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(stand,GameplayPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(stand);}
        AssetDatabase.SaveAssets();
    }
}
#endif
