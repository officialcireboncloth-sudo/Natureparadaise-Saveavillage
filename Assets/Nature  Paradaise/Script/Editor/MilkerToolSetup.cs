#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Connects the selectable Milker and renders its compact icon from the existing model without editing a world scene.</summary>
public static class MilkerToolSetup
{
    const string Root="Assets/Nature  Paradaise";
    public const string ItemPath=Root+"/Resources/Items/Tools/Milker Tool.asset";
    const string IconPath=Root+"/Textures/UI/Icons/Tools/icon_tool_milker.png";
    [MenuItem("Nature Paradise/Tools/Apply Milker Item and Matching Icon")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring tool assets.");
        var catalog=Resources.Load<PlayerToolVisualCatalog>(PlayerToolVisualCatalog.ResourcePath);
        var entry=catalog!=null?catalog.Find("Milker"):null;
        if(entry?.prefab==null)throw new InvalidOperationException("Existing Milker model is missing.");
        RenderIcon(entry.prefab);
        AssetDatabase.ImportAsset(IconPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(IconPath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=128;
        importer.SaveAndReimport();
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        var item=AssetDatabase.LoadAssetAtPath<ItemSO>(ItemPath);
        if(item==null){item=ScriptableObject.CreateInstance<ItemSO>();item.itemId="item.milker_tool";item.itemName="Milker";item.buyPrice=750;item.maxStack=1;item.requiredVillageLevel=1;AssetDatabase.CreateAsset(item,ItemPath);}
        item.category=ItemCategory.Tool;item.equippedTool=PlayerToolType.Milker;item.icon=icon;item.isNotSellable=true;
        item.inventoryDescription="Alat perah susu sapi dan kambing. Pilih di hotbar, dekati hewan yang produknya siap, lalu tekan F/G atau tombol interaksi.";
        item.worldPrefab=entry.prefab;item.worldScale=Vector3.one*entry.scale;item.canDropToWorld=false;item.canPlaceInWorld=false;
        EditorUtility.SetDirty(item);entry.tool=PlayerToolType.Milker;EditorUtility.SetDirty(catalog);
        var shop=AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(Root+"/Resources/Shops/Animal Catalog.asset");
        if(shop==null)throw new InvalidOperationException("Animal shop catalog is missing.");
        var tab=shop.tabs.FirstOrDefault(t=>t.title=="Perlengkapan");
        if(tab==null){tab=new ShopCatalogTab{title="Perlengkapan"};shop.tabs.Add(tab);}
        if(!tab.items.Contains(item))tab.items.Add(item);EditorUtility.SetDirty(shop);
        var theme=Resources.Load<AnimalInteractionTheme>("UI/AnimalInteractionTheme");
        if(theme!=null){theme.milkIcon=icon;EditorUtility.SetDirty(theme);}
        AssetDatabase.SaveAssets();
    }
    static void RenderIcon(GameObject prefab)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var model=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(model,scene);
            var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("Milker has no renderable model.");
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            var camera=new GameObject("Milker Icon Camera",typeof(Camera)).GetComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.allowHDR=false;
            camera.transform.position=bounds.center+new Vector3(2,1.6f,3)*bounds.size.magnitude;camera.transform.LookAt(bounds.center);
            float extent=0;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=camera.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));extent=Mathf.Max(extent,Mathf.Abs(p.x),Mathf.Abs(p.y));}
            camera.orthographicSize=extent*1.08f;
            var light=new GameObject("Milker Icon Key",typeof(Light)).GetComponent<Light>();SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(35,-40,0);
            var fill=new GameObject("Milker Icon Fill",typeof(Light)).GetComponent<Light>();SceneManager.MoveGameObjectToScene(fill.gameObject,scene);fill.type=LightType.Directional;fill.intensity=.6f;fill.transform.rotation=Quaternion.Euler(25,145,0);
            var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32){antiAliasing=4};rt.Create();
            var previous=RenderTexture.active;Texture2D image=null;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(128,128,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();File.WriteAllBytes(IconPath,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);if(image!=null)Object.DestroyImmediate(image);}
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
