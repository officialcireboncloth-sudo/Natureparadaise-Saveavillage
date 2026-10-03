#if UNITY_EDITOR
using UnityEditor;
public static class DialogueShippingUISetup
{
    [MenuItem("Nature Paradise/UI/Dialogue/Select Image Slots")]
    static void DialogueSlots()=>Select("DialogueTheme");
    [MenuItem("Nature Paradise/UI/Shipping Bin/Select Image Slots")]
    static void BinSlots()=>Select("ShippingBinTheme");
    [MenuItem("Nature Paradise/UI/Dialogue/Select NPC Portrait Slots")]
    static void PortraitSlots()
    {
        var ids=AssetDatabase.FindAssets("t:DialogueSpeakerSO");
        var speakers=new UnityEngine.Object[ids.Length];
        for(int i=0;i<ids.Length;i++)speakers[i]=AssetDatabase.LoadAssetAtPath<DialogueSpeakerSO>(AssetDatabase.GUIDToAssetPath(ids[i]));
        Selection.objects=speakers;
    }
    static void Select(string name){Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Nature  Paradaise/Resources/UI/"+name+".asset");EditorGUIUtility.PingObject(Selection.activeObject);}
}
#endif

