using UnityEngine;

[DisallowMultipleComponent]
/// <summary>Overlay responsive untuk bite dan minigame; tidak menambah object ke scene GameplayUI.</summary>
public sealed class FishingMinigameUI : MonoBehaviour
{
    FishingSystem fishing;
    GUIStyle titleStyle;
    GUIStyle labelStyle;
    GUIStyle buttonStyle;
    PlayerController movement;
    bool collectionOpen;
    FishingOverlayUI overlay;
    bool idleCastHeld;
    public bool MobileReelHeld => overlay != null && overlay.ReelHeld;
    public bool MobileCastHeld => fishing != null && (fishing.State == FishingState.Idle || fishing.State == FishingState.Charging) && idleCastHeld;

    public void Bind(FishingSystem owner)
    {
        fishing = owner;
        movement = GetComponent<PlayerController>();
        overlay = GetComponent<FishingOverlayUI>();
        if (overlay == null) overlay = gameObject.AddComponent<FishingOverlayUI>();
        overlay.Bind(owner);
    }

    void Update()
    {
        if (fishing != null && fishing.State == FishingState.Idle && GameplayInput.GetKeyDown(KeyCode.O))
            SetCollectionOpen(!collectionOpen);
    }

    void OnDisable() => SetCollectionOpen(false);

    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint) idleCastHeld = false;
        if (fishing != null && fishing.State == FishingState.Charging && Application.isMobilePlatform && !GameplayPauseMenu.IsOpen)
        {
            EnsureStyles();
            bool held=GUI.RepeatButton(new Rect(Screen.width*.5f-120f,Screen.height*.82f,240f,42f),"Hold — lepas untuk cast",buttonStyle);
            if(Event.current.type==EventType.Repaint)idleCastHeld=held;
        }
        if (fishing == null || fishing.State != FishingState.Idle || GameplayPauseMenu.IsOpen) return;
        EnsureStyles();
        if (fishing.State == FishingState.Idle)
        {
            if (FishCollectionService.Collection.Count > 0 &&
                GUI.Button(new Rect(Screen.width - 168f, 16f, 150f, 38f), "FISHDEX [O]", buttonStyle))
                SetCollectionOpen(!collectionOpen);
            if (collectionOpen) DrawCollection();
            else if(GetComponent<PlayerToolHotbar>()?.SelectedTool == PlayerToolType.FishingRod)
            {
                bool held=GUI.RepeatButton(new Rect(Screen.width*0.5f-120f,Screen.height*0.82f,240f,42f),"F - Hold to Cast",buttonStyle);
                if(Event.current.type==EventType.Repaint) idleCastHeld=held;
            }
            return;
        }
    }

    void DrawCollection()
    {
        float width = Mathf.Min(Screen.width - 32f, 620f);
        float height = Mathf.Min(Screen.height - 60f, 520f);
        Rect panel = new((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        DrawRect(panel, new Color(0.025f, 0.045f, 0.06f, 0.97f));
        GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 110f, 36f), "FISH COLLECTION", titleStyle);
        if (GUI.Button(new Rect(panel.xMax - 74f, panel.y + 12f, 54f, 34f), "X", buttonStyle))
        { SetCollectionOpen(false); return; }
        float y = panel.y + 68f;
        foreach (FishCollectionEntrySaveData entry in FishCollectionService.Collection)
        {
            string name = string.IsNullOrWhiteSpace(entry.fishId) ? entry.itemId : entry.fishId.Replace("fish.", string.Empty).Replace('_', ' ');
            GUI.Label(new Rect(panel.x + 30f, y, panel.width - 60f, 30f),
                $"{name.ToUpperInvariant()}     Caught: {entry.caughtCount}     Record: {entry.largestSizeCm:0.0} cm", labelStyle);
            y += 34f;
            if (y > panel.yMax - 40f) break;
        }
    }

    void SetCollectionOpen(bool open)
    {
        if (collectionOpen == open) return;
        collectionOpen = open;
        if (open)
        {
            movement?.AcquireMovementLock(this);
            WorldInteractionPrompt.AcquireSuppression(this);
        }
        else
        {
            movement?.ReleaseMovementLock(this);
            WorldInteractionPrompt.ReleaseSuppression(this);
        }
    }

    void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        labelStyle.normal.textColor = new Color(0.88f, 0.92f, 0.95f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
    }

    static void DrawProgress(Rect rect, float value, Color fill, Color background)
    {
        DrawRect(rect, background);
        rect.width *= Mathf.Clamp01(value);
        DrawRect(rect, fill);
    }

    static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
