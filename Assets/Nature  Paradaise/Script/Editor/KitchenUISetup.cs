using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class KitchenItemIconSlots : EditorWindow
{
    Vector2 scroll;
    [MenuItem("Nature Paradise/UI/Kitchen/Shared Item Image Slots")]
    public static void Open()=>GetWindow<KitchenItemIconSlots>("Kitchen Item Icons");
    [MenuItem("Nature Paradise/UI/Kitchen/Select Theme Image Slots")]
    static void SelectTheme()
    {
        const string path="Assets/Nature  Paradaise/Resources/UI/KitchenTheme.asset";
        var theme=AssetDatabase.LoadAssetAtPath<KitchenTheme>(path);
        if(theme==null){theme=CreateInstance<KitchenTheme>();AssetDatabase.CreateAsset(theme,path);AssetDatabase.SaveAssets();}
        Selection.activeObject=theme;EditorGUIUtility.PingObject(theme);
    }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Ikon di sini langsung mengubah ItemSO asli. Seluruh UI memakai Sprite yang sama; CSV import tidak menimpa gambar. Ilustrasi kosong memakai ikon yang sama.",MessageType.Info);
        var recipes=AssetDatabase.FindAssets("t:KitchenRecipeSO").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<KitchenRecipeSO>);
        var items=recipes.Where(r=>r!=null).SelectMany(r=>r.ingredients.Where(v=>v?.item!=null).Select(v=>v.item).Append(r.resultItem)).Where(i=>i!=null).Distinct().OrderBy(i=>i.itemName);
        scroll=EditorGUILayout.BeginScrollView(scroll);
        foreach(var item in items)
        {
            EditorGUILayout.BeginVertical("box");EditorGUILayout.LabelField(item.itemName+"  ["+item.Id+"]",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var icon=(Sprite)EditorGUILayout.ObjectField("Shared Icon",item.icon,typeof(Sprite),false);
            var illustration=(Sprite)EditorGUILayout.ObjectField("Illustration (optional)",item.inventoryIllustration,typeof(Sprite),false);
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(item,"Kitchen shared image slot");item.icon=icon;item.inventoryIllustration=illustration;EditorUtility.SetDirty(item);}
            if(GUILayout.Button("Select Item")){Selection.activeObject=item;EditorGUIUtility.PingObject(item);}
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();if(GUILayout.Button("Save Item Images"))AssetDatabase.SaveAssets();
    }
}
