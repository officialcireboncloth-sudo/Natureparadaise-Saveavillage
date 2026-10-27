#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Updates only the shipping-bin art prefab, retaining its stable gameplay component and world references.</summary>
public static class ShippingBinArtSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string ModelPath=Root+"/mesh/World/ShippingBin/ShippingBin_A.fbx";
    public const string VisualPath=Root+"/Prefabs/World/ShippingBin/ShippingBinVisual.prefab";
    public const string GameplayPath=Root+"/Prefabs/World/ShippingBin.prefab";
    [MenuItem("Nature Paradise/Art/Apply Atlas Shipping Bin")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring assets.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Material/Shared Wood Atlas.mat");
        if(material==null)throw new InvalidOperationException("Shared atlas is missing");
        AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Low;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Shared Wood Atlas"),material);importer.SaveAndReimport();
        if(!AssetDatabase.IsValidFolder(Root+"/Prefabs/World/ShippingBin"))AssetDatabase.CreateFolder(Root+"/Prefabs/World","ShippingBin");
        var root=new GameObject("ShippingBinVisual");
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),root.transform);
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var importedHinge=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="LidHinge");
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            var pivot=new GameObject("LidPivot_Unity").transform;pivot.SetParent(root.transform,false);pivot.position=importedHinge.position;
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterial=material;
                if(renderer.name.Contains("_Lid_"))renderer.transform.SetParent(pivot,true);
            }
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.10f,renderers.Where(r=>r.name.EndsWith("LOD0")).Cast<Renderer>().ToArray()),new LOD(.015f,renderers.Where(r=>r.name.EndsWith("LOD1")).Cast<Renderer>().ToArray())});lod.RecalculateBounds();
            root.AddComponent<ShippingBinPresentation>().Configure(pivot);
            PrefabUtility.SaveAsPrefabAsset(root,VisualPath);
        }
        finally{Object.DestroyImmediate(root);}
        var bin=PrefabUtility.LoadPrefabContents(GameplayPath);
        try
        {
            foreach(var child in bin.transform.Cast<Transform>().ToArray())if(child.name=="Visual_ToonFarm_ShippingCrate"||child.name=="Visual_AtlasShippingBin")Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath),bin.transform);visual.name="Visual_AtlasShippingBin";
            var collider=bin.GetComponent<BoxCollider>();collider.center=new Vector3(0,.44f,0);collider.size=new Vector3(1.60f,.88f,1.02f);
            PrefabUtility.SaveAsPrefabAsset(bin,GameplayPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(bin);}
        AssetDatabase.SaveAssets();
    }
}
#endif
