using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SprinklerAssetSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string ModelRoot = Root + "/mesh/Prop/Prop_Garden_Sprinkle";
    [MenuItem("Nature Paradise/Farming/Apply Five Sprinkler Levels")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + "/Sprinkle.fbx");
        if (model == null) throw new InvalidOperationException("Sprinkle.fbx belum di-import.");
        string output = Root + "/Prefabs/Farming/Sprinklers";
        Directory.CreateDirectory(output); AssetDatabase.Refresh();
        var normalImporter = AssetImporter.GetAtPath(ModelRoot + "/tri-arm_turret_3d_model_normal.JPEG") as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        { normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.SaveAndReimport(); }
        var metalImporter = AssetImporter.GetAtPath(ModelRoot + "/tri-arm_turret_3d_model_metallic.JPEG") as TextureImporter;
        if (metalImporter != null && metalImporter.sRGBTexture)
        { metalImporter.sRGBTexture = false; metalImporter.SaveAndReimport(); }
        Color[] colors = { new Color(.8f,.8f,.8f), new Color(.45f,.95f,.45f), new Color(.4f,.7f,1f), new Color(.8f,.45f,1f), new Color(1f,.8f,.3f) };
        ItemSO[] items = new ItemSO[5];
        int[] prices = {500,1500,5000,12000,25000};
        for(int level=1;level<=5;level++)
        {
            string materialPath = output + $"/Sprinkler Lv{level}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,materialPath); }
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ModelRoot+"/tri-arm_turret_3d_model_basecolor.JPEG"));
            material.SetColor("_BaseColor",colors[level-1]);
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ModelRoot+"/tri-arm_turret_3d_model_normal.JPEG"));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(ModelRoot+"/tri-arm_turret_3d_model_metallic.JPEG"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness",.25f);
            EditorUtility.SetDirty(material);
            GameObject root = new GameObject($"Sprinkler Lv{level}");
            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(model);
            mesh.transform.SetParent(root.transform,false);
            foreach(Renderer renderer in mesh.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach(Renderer r in renderers) bounds.Encapsulate(r.bounds);
            float scale = .65f / Mathf.Max(.001f, Mathf.Max(bounds.size.x,bounds.size.z));
            mesh.transform.localScale *= scale;
            mesh.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            string prefabPath = output+$"/Sprinkler Lv{level}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            string itemPath = Root+$"/Resources/Items/Farming/Sprinkler Lv{level}.asset";
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(itemPath);
            if(item==null) { item=ScriptableObject.CreateInstance<ItemSO>(); item.itemId=$"item.sprinkler_lv{level}"; item.itemName=$"Sprinkler Lv.{level}"; item.buyPrice=prices[level-1]; item.maxStack=16; item.requiredVillageLevel=Mathf.Min(level,4); AssetDatabase.CreateAsset(item,itemPath); }
            item.sprinklerLevel=level; item.canDropToWorld=true; item.canPlaceInWorld=true;
            item.worldPrefab=prefab; item.worldScale=Vector3.one;
            EditorUtility.SetDirty(item); items[level-1]=item;
        }
        FarmEquipmentCatalog catalog = FarmEquipmentCatalog.Load();
        catalog.sprinklers=items; EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }
}
