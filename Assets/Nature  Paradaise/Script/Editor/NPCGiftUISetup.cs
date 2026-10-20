#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Creates an editable uGUI prefab. No NPC interactions, item data or Map objects are changed.</summary>
public static class NPCGiftUISetup
{
    const string Root = "Assets/Nature  Paradaise";
    public const string PrefabPath = Root + "/Resources/UI/NPCGiftUI.prefab";
    const string ThemePath = Root + "/Resources/UI/NPCGiftTheme.asset";

    [MenuItem("Nature Paradise/UI/NPC Gift/Create or Update Editable Prefab")]
    public static void BuildPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Root + "/Resources/UI");
        var theme = AssetDatabase.LoadAssetAtPath<NPCGiftTheme>(ThemePath);
        if (theme == null) { theme = ScriptableObject.CreateInstance<NPCGiftTheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }
        var go = new GameObject("NPC Gift UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        try
        {
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 480;
            var scale = go.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1920, 1080); scale.matchWidthOrHeight = .5f;
            var ui = go.AddComponent<NPCGiftUI>(); var data = new SerializedObject(ui);
            void Ref(string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
            Ref("canvas", canvas); Ref("theme", theme);
            var dim = R("Backdrop", go.transform, 0, 0, 1, 1).gameObject.AddComponent<Image>(); dim.color = new Color(.035f, .06f, .07f, .34f); dim.raycastTarget = true;
            var safe = R("Safe Area", go.transform, 0, 0, 1, 1); safe.gameObject.AddComponent<SafeAreaFitter>();
            Ref("npcPortrait", Art("NPC Portrait Image Slot", safe, .015f, .035f, .435f, .93f));
            var panel = R("Gift Panel", safe, .445f, .15f, .975f, .72f);
            var glass = GameplayHUDStyle.Surface(panel, GameplayHUDStyle.Modal, 16); glass.raycastTarget = true;
            glass.borderWidth = 1; glass.borderColor = new Color(.7f, .77f, .7f, .35f);
            var shadow = panel.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .22f); shadow.effectDistance = new Vector2(0, -5);
            Ref("panelArtwork", Art("Panel Artwork Image Slot", panel, 0, 0, 1, 1, true));
            Ref("leftDecoration", Art("Left Decoration Image Slot", panel, .005f, .015f, .12f, .39f));
            Ref("rightDecoration", Art("Right Decoration Image Slot", panel, .88f, .015f, .995f, .39f));
            Ref("headerIcon", Art("Header Icon Image Slot", panel, .035f, .867f, .077f, .95f));
            Ref("heading", T("Recipient Heading", panel, "Berikan Hadiah untuk NPC", 29, .09f, .865f, .73f, .963f));
            var weekly = T("Weekly Gift Count", panel, "", 17, .735f, .875f, .965f, .945f); weekly.alignment = TextAlignmentOptions.MidlineRight; weekly.color = GameplayHUDStyle.Muted; Ref("weeklyCount", weekly);
            Line(panel, .035f, .845f, .965f);
            var giftRows = data.FindProperty("cards"); giftRows.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                float x = .035f + i * .155f;
                var card = B("Gift Card " + (i + 1).ToString("00"), panel, "", x, .465f, x + .145f, .815f);
                // Keep the selection rim independent of the normal background tint.
                card.GetComponent<MainMenuRoundedImage>().color = GameplayHUDStyle.Card;
                var cardColors = card.colors; cardColors.normalColor = cardColors.selectedColor = Color.white;
                cardColors.highlightedColor = new Color(.86f, 1f, .86f, 1); cardColors.pressedColor = new Color(.85f, .95f, .82f, 1);
                cardColors.disabledColor = new Color(1, 1, 1, .45f); card.colors = cardColors;
                var element = giftRows.GetArrayElementAtIndex(i);
                void CardRef(string field, Object value) => element.FindPropertyRelative(field).objectReferenceValue = value;
                CardRef("button", card); CardRef("surface", card.GetComponent<MainMenuRoundedImage>());
                CardRef("artwork", Art("Card Artwork Image Slot", card.transform, .015f, .015f, .985f, .985f, true));
                CardRef("icon", Art("Shared Item Icon Image Slot", card.transform, .10f, .43f, .90f, .96f));
                var name = T("Item Name", card.transform, "", 18, .06f, .16f, .94f, .34f); name.alignment = TextAlignmentOptions.Center; CardRef("name", name);
                var count = T("Available Count", card.transform, "", 18, .06f, .015f, .94f, .17f); count.alignment = TextAlignmentOptions.Center; count.color = GameplayHUDStyle.Muted; CardRef("count", count);
                var quality = T("Quality Text Fallback", card.transform, "", 15, .04f, .335f, .96f, .45f); quality.alignment = TextAlignmentOptions.Center; quality.color = GameplayHUDStyle.Muted; CardRef("quality", quality);
                var stars = element.FindPropertyRelative("stars"); stars.arraySize = 5;
                for (int s = 0; s < 5; s++) stars.GetArrayElementAtIndex(s).objectReferenceValue = Art("Quality Star Image Slot " + (s + 1), card.transform, .08f + s * .17f, .34f, .23f + s * .17f, .44f);
            }
            Ref("previousButton", B("Previous Gift", panel, "‹", .001f, .585f, .033f, .705f));
            Ref("nextButton", B("Next Gift", panel, "›", .967f, .585f, .999f, .705f));
            var empty = T("Empty Inventory Message", panel, "Belum ada barang untuk dipilih.", 22, .10f, .57f, .90f, .70f); empty.alignment = TextAlignmentOptions.Center; empty.color = GameplayHUDStyle.Muted; Ref("emptyMessage", empty);
            var detail = R("Selected Gift Detail", panel, .035f, .21f, .965f, .445f);
            GameplayHUDStyle.Surface(detail, GameplayHUDStyle.Card, 12);
            Ref("detailIllustration", Art("Shared Item Illustration Image Slot", detail, .025f, .08f, .29f, .93f));
            var detailName = T("Selected Item Name", detail, "Pilih hadiah", 25, .32f, .67f, .975f, .96f); detailName.fontStyle = FontStyles.Bold; Ref("detailName", detailName);
            var viewport = R("Description Viewport", detail, .32f, .28f, .965f, .66f); viewport.gameObject.AddComponent<RectMask2D>();
            var input = viewport.gameObject.AddComponent<Image>(); input.color = Color.clear; input.raycastTarget = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = viewport; scroll.movementType = ScrollRect.MovementType.Clamped;
            var description = T("Gift Description", viewport, "", 20, 0, 1, 1, 1); description.rectTransform.pivot = new Vector2(.5f, 1);
            description.enableAutoSizing = false; description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal; description.overflowMode = TextOverflowModes.Overflow;
            description.color = GameplayHUDStyle.Muted; scroll.content = description.rectTransform; Ref("description", description); Ref("descriptionScroll", scroll);
            var stock = T("Selected Stock Count", detail, "", 18, .32f, .035f, .965f, .245f); stock.color = GameplayHUDStyle.Muted; Ref("stockCount", stock);
            var question = T("Confirmation Question", panel, "Pilih hadiah untuk NPC.", 20, .10f, .145f, .90f, .205f); question.alignment = TextAlignmentOptions.Center; Ref("confirmation", question);
            var give = B("Give Button", panel, "Berikan", .225f, .045f, .495f, .13f); Ref("giveButton", give);
            Ref("giveButtonArtwork", ArtBehindLabel("Button Artwork Image Slot", give.transform));
            Ref("giveIcon", Art("Give Icon Image Slot", give.transform, .08f, .20f, .22f, .80f));
            var cancel = B("Cancel Button", panel, "Batal", .525f, .045f, .755f, .13f); Ref("cancelButton", cancel);
            Ref("cancelButtonArtwork", ArtBehindLabel("Button Artwork Image Slot", cancel.transform));
            var status = T("Service Feedback", panel, "", 17, .08f, .003f, .92f, .038f); status.alignment = TextAlignmentOptions.Center; Ref("status", status);
            var page = T("Gift Page", panel, "", 16, .83f, .045f, .965f, .125f); page.alignment = TextAlignmentOptions.MidlineRight; page.color = GameplayHUDStyle.Muted; Ref("pageCount", page);
            var hint = T("Keyboard Guide", safe, "A / D   Pilih Hadiah       Enter   Berikan       Esc   Batal", 20, .46f, .075f, .96f, .12f); hint.alignment = TextAlignmentOptions.Center; hint.color = GameplayHUDStyle.Muted;
            data.ApplyModifiedPropertiesWithoutUndo(); GameplayHUDStyle.ApplyHierarchy(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); AssetDatabase.SaveAssets();
            Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab);
            Debug.Log("NPC Gift UI prefab ready: " + PrefabPath);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [MenuItem("Nature Paradise/UI/NPC Gift/Open Editable Prefab")]
    static void OpenPrefab() { var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); if (prefab != null) AssetDatabase.OpenAsset(prefab); }
    [MenuItem("Nature Paradise/UI/NPC Gift/Select Image Slots")]
    static void SelectSlots() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<NPCGiftTheme>(ThemePath); EditorGUIUtility.PingObject(Selection.activeObject); }

    [MenuItem("Nature Paradise/UI/NPC Gift/Preview in Play Mode")]
    public static void Preview()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode to preview NPC Gift UI."); return; }
        var speakers = Resources.LoadAll<DialogueSpeakerSO>("Narrative");
        var speaker = speakers.Length > 0 ? speakers[0] : null;
        if (speaker == null) { Debug.LogWarning("Assign a DialogueSpeakerSO before previewing gifts."); return; }
        var gifts = new List<NPCGiftOption>();
        foreach (string path in new[] { "Food/Omelette", "Animal/Milk", "Fish/Tilapia", "Food/Vegetable Soup", "Food/Bread", "Animal/Egg" })
        { var item = Resources.Load<ItemSO>("Items/" + path); if (item != null) gifts.Add(new NPCGiftOption { item = item, inventorySlot = gifts.Count, availableCount = gifts.Count + 2, qualityStars = gifts.Count % 4 }); }
        NPCGiftUI ui = null;
        ui = NPCGiftUI.Show(speaker, gifts, request => ui.CompleteRequest(false, "Preview UI: " + request.Item.itemName + " dipilih."), 1, 2);
    }
    static RectTransform R(string name, Transform parent, float x1, float y1, float x2, float y2) => GameplayHUDStyle.Rect(name, parent, new Vector2(x1, y1), new Vector2(x2, y2));
    static TMP_Text T(string name, Transform parent, string text, float size, float x1, float y1, float x2, float y2) => GameplayHUDStyle.Text(name, parent, text, size, new Vector2(x1, y1), new Vector2(x2, y2));
    static Image Art(string name, Transform parent, float x1, float y1, float x2, float y2, bool sliced = false)
    {
        var image = R(name, parent, x1, y1, x2, y2).gameObject.AddComponent<Image>();
        image.preserveAspect = !sliced; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; image.raycastTarget = false; image.enabled = false; return image;
    }
    static Image ArtBehindLabel(string name, Transform parent) { var image = Art(name, parent, .015f, .04f, .985f, .96f, true); image.transform.SetAsFirstSibling(); return image; }
    static void Line(Transform parent, float x1, float y, float x2) => GameplayHUDStyle.Surface(R("Header Divider", parent, x1, y, x2, y + .003f), new Color(.7f, .77f, .7f, .12f), 0);
    static Button B(string name, Transform parent, string label, float x1, float y1, float x2, float y2)
    {
        var rect = R(name, parent, x1, y1, x2, y2); var image = GameplayHUDStyle.Surface(rect, Color.white, 10); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors; colors.normalColor = GameplayHUDStyle.Card; colors.selectedColor = colors.normalColor;
        colors.highlightedColor = new Color(.32f, .53f, .37f, .94f); colors.pressedColor = new Color(.23f, .40f, .28f);
        colors.disabledColor = new Color(.16f, .22f, .25f, .45f); colors.fadeDuration = .12f; button.colors = colors;
        rect.gameObject.AddComponent<MainMenuButtonAudio>();
        if (!string.IsNullOrEmpty(label)) { var text = T("Label", rect, label, 22, .06f, .06f, .94f, .94f); text.alignment = TextAlignmentOptions.Center; }
        return button;
    }
}
#endif
