using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(ShopCatalogSO))]public sealed class ShopCatalogInspector:Editor
{
 public override void OnInspectorGUI(){DrawDefaultInspector();EditorGUILayout.HelpBox("Tabs menentukan produk tiap toko. Product Settings mengatur harga (-1 = ItemSO), jumlah awal/maksimum, quality barang yang diterima dan gambar khusus toko. Animal offers memakai price dan icon pada offer; quality bintang tidak berlaku untuk hewan hidup.",MessageType.Info);if(GUILayout.Button("Add Settings For Listed Items")){var catalog=(ShopCatalogSO)target;Undo.RecordObject(catalog,"Add shop product settings");foreach(var tab in catalog.tabs)foreach(var item in tab.items)if(item!=null&&catalog.Settings(item)==null)catalog.priceOverrides.Add(new ShopPriceOverride{item=item});EditorUtility.SetDirty(catalog);}}
}
public static class AquariumUISetup
{
 [MenuItem("Nature Paradise/UI/Create Aquarium Interface")]
 public static void Apply(){const string path="Assets/Nature  Paradaise/Resources/UI/Aquarium UI.prefab";if(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;var go=new GameObject("Aquarium UI",typeof(RectTransform));try{go.AddComponent<AquariumUI>().Build();PrefabUtility.SaveAsPrefabAsset(go,path);}finally{Object.DestroyImmediate(go);}AssetDatabase.SaveAssets();}
}

