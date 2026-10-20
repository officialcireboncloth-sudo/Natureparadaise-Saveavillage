#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Creates an editable bathing HUD; no recovery rules or world objects are installed.</summary>
public static class HotSpringUISetup
{
    const string Root = "Assets/Nature  Paradaise";
    public const string PrefabPath = Root + "/Resources/UI/HotSpringUI.prefab";
    const string ThemePath = Root + "/Resources/UI/HotSpringTheme.asset";

    [MenuItem("Nature Paradise/UI/Hot Spring/Create or Update Editable Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Root + "/Resources/UI");
        var theme = AssetDatabase.LoadAssetAtPath<HotSpringTheme>(ThemePath);
        if (theme == null) { theme = ScriptableObject.CreateInstance<HotSpringTheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }
        var go = new GameObject("Hot Spring UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        try
        {
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 480;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var ui = go.AddComponent<HotSpringUI>(); var fields = new SerializedObject(ui);
            void Ref(string name, Object value) => fields.FindProperty(name).objectReferenceValue = value;
            Ref("canvas", canvas); Ref("theme", theme);
            var safe = R("Safe Area", go.transform, 0, 0, 1, 1); safe.gameObject.AddComponent<SafeAreaFitter>();
            var date = Panel("Date and Weather", safe, .02f, .885f, .29f, .98f);
            Ref("datePanel", date.gameObject); Ref("dateArtwork", Art("Panel Image Slot", date, 0, 0, 1, 1, true));
            Ref("weatherIcon", Art("Weather Image Slot", date, .025f, .12f, .18f, .88f));
            var day = T("Date", date, "", 24, .205f, .48f, .965f, .9f); day.fontStyle = FontStyles.Bold; Ref("dateText", day);
            Ref("timeText", T("Time", date, "", 24, .205f, .08f, .965f, .48f));
            var gold = Panel("Gold", safe, .80f, .925f, .985f, .98f);
            Ref("goldPanel", gold.gameObject); Ref("goldArtwork", Art("Panel Image Slot", gold, 0, 0, 1, 1, true));
            Ref("coinIcon", Art("Coin Image Slot", gold, .035f, .15f, .17f, .85f));
            var money = T("Gold Amount", gold, "", 24, .19f, .08f, .94f, .92f); money.alignment = TextAlignmentOptions.MidlineRight; Ref("goldText", money);
            var stamina = R("Stamina Recovery", safe, .79f, .835f, .985f, .913f);
            var track = Panel("Stamina Track", stamina, 0, .62f, 1, .88f);
            Ref("staminaArtwork", Art("Track Image Slot", track, 0, 0, 1, 1, true));
            var next = R("Next Tick Fill", track, 0, 0, 1, 1); GameplayHUDStyle.Surface(next, new Color(.64f, .78f, .52f, .35f), 8); Ref("nextFill", next);
            var current = R("Current Stamina Fill", track, 0, 0, 1, 1); GameplayHUDStyle.Surface(current, GameplayHUDStyle.Accent, 8); Ref("currentFill", current);
            Ref("staminaFillArtwork", Art("Fill Image Slot", current, 0, 0, 1, 1, true));
            Ref("currentText", Center("Current Stamina", stamina, "— / —", 20, 0, .04f, .35f, .54f));
            var arrow = Center("Next Tick Arrow", stamina, "→", 22, .36f, .04f, .46f, .54f); arrow.color = GameplayHUDStyle.Accent; Ref("arrowText", arrow);
            Ref("nextText", Center("Next Stamina", stamina, "", 20, .47f, .04f, .83f, .54f));
            var gain = Center("Recovery Amount", stamina, "", 22, .84f, .04f, 1, .54f); gain.color = GameplayHUDStyle.Accent; Ref("gainText", gain);
            var feedback = R("Recovery Feedback", safe, .34f, .66f, .66f, .755f);
            Ref("springIcon", Art("Hot Spring Image Slot", feedback, .43f, .67f, .57f, 1));
            var title = Center("Recovery Title", feedback, "Berendam", 24, 0, .32f, 1, .68f); title.fontStyle = FontStyles.Bold; TextShadow(title); Ref("recoveryTitle", title);
            var rate = Center("Recovery Rate", feedback, "", 22, 0, 0, 1, .34f); rate.color = GameplayHUDStyle.Accent; TextShadow(rate); Ref("recoveryRate", rate);
            var exitRect = Panel("Exit Button", safe, .45f, .355f, .55f, .405f);
            var background = exitRect.GetComponent<MainMenuRoundedImage>(); background.raycastTarget = true;
            var exit = exitRect.gameObject.AddComponent<Button>(); exit.targetGraphic = background; exit.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = exit.colors; colors.normalColor = colors.selectedColor = Color.white; colors.highlightedColor = new Color(.8f, 1, .8f, 1); colors.pressedColor = new Color(.7f, .9f, .7f); exit.colors = colors;
            exitRect.gameObject.AddComponent<MainMenuButtonAudio>(); Ref("exitButton", exit);
            var key = Panel("E Key", exitRect, .06f, .18f, .23f, .82f); Center("Key Label", key, "E", 22, 0, 0, 1, 1);
            Ref("exitIcon", Art("Exit Image Slot", exitRect, .27f, .20f, .41f, .80f));
            Center("Exit Label", exitRect, "Keluar", 22, .33f, .08f, .96f, .92f);
            var session = R("Bathing Session", safe, .31f, .13f, .69f, .205f);
            var elapsed = Center("Elapsed Time", session, "Berendam  00:00", 22, 0, .48f, 1, 1); TextShadow(elapsed); Ref("elapsedText", elapsed);
            var tools = Center("Tools Disabled Hint", session, "Peralatan tidak dapat digunakan", 20, 0, .02f, 1, .49f); tools.color = GameplayHUDStyle.Muted; TextShadow(tools); Ref("toolsHint", tools);
            fields.ApplyModifiedPropertiesWithoutUndo(); GameplayHUDStyle.ApplyHierarchy(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); AssetDatabase.SaveAssets(); Selection.activeObject = prefab;
            Debug.Log("Hot Spring HUD prefab ready: " + PrefabPath);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [MenuItem("Nature Paradise/UI/Hot Spring/Open Editable Prefab")]
    static void OpenPrefab() { var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); if (asset != null) AssetDatabase.OpenAsset(asset); }
    [MenuItem("Nature Paradise/UI/Hot Spring/Select Image Slots")]
    static void SelectSlots() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<HotSpringTheme>(ThemePath); EditorGUIUtility.PingObject(Selection.activeObject); }
    [MenuItem("Nature Paradise/UI/Hot Spring/Preview in Play Mode")]
    public static void Preview()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode to preview Hot Spring UI."); return; }
        HotSpringUI.Show(new HotSpringViewData { dateText = "Senin, 18 Musim Panas", timeText = "16:20", gold = 12480,
            stamina = 38, maximumStamina = 50, recoveryPerTick = 1, recoveryIntervalSeconds = 10, elapsedSeconds = 80 });
    }

    static RectTransform R(string name, Transform parent, float x1, float y1, float x2, float y2) => GameplayHUDStyle.Rect(name, parent, new Vector2(x1, y1), new Vector2(x2, y2));
    static TMP_Text T(string name, Transform parent, string text, float size, float x1, float y1, float x2, float y2) => GameplayHUDStyle.Text(name, parent, text, size, new Vector2(x1, y1), new Vector2(x2, y2));
    static TMP_Text Center(string name, Transform parent, string text, float size, float x1, float y1, float x2, float y2) { var label = T(name, parent, text, size, x1, y1, x2, y2); label.alignment = TextAlignmentOptions.Center; return label; }
    static RectTransform Panel(string name, Transform parent, float x1, float y1, float x2, float y2) { var rect = R(name, parent, x1, y1, x2, y2); GameplayHUDStyle.Surface(rect, GameplayHUDStyle.Panel, 10); return rect; }
    static Image Art(string name, Transform parent, float x1, float y1, float x2, float y2, bool sliced = false) { var image = R(name, parent, x1, y1, x2, y2).gameObject.AddComponent<Image>(); image.preserveAspect = !sliced; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; image.raycastTarget = false; image.enabled = false; return image; }
    static void TextShadow(TMP_Text text) { var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .65f); shadow.effectDistance = new Vector2(1, -2); }
}
#endif
