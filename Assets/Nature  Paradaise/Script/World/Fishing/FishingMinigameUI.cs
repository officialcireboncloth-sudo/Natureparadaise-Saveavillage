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
    public bool MobileReelHeld { get; private set; }

    public void Bind(FishingSystem owner)
    {
        fishing = owner;
        movement = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (fishing != null && fishing.State == FishingState.Idle && Input.GetKeyDown(KeyCode.O))
            SetCollectionOpen(!collectionOpen);
    }

    void OnDisable() => SetCollectionOpen(false);

    void OnGUI()
    {
        MobileReelHeld = false;
        if (fishing == null) return;
        EnsureStyles();
        if (fishing.State == FishingState.Idle)
        {
            if (FishCollectionService.Collection.Count > 0 &&
                GUI.Button(new Rect(Screen.width - 168f, 16f, 150f, 38f), "FISHDEX [O]", buttonStyle))
                SetCollectionOpen(!collectionOpen);
            if (collectionOpen) DrawCollection();
            return;
        }
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.65f, 1.35f);
        float width = Mathf.Min(Screen.width - 32f, 720f * scale);
        Rect panel = new((Screen.width - width) * 0.5f, Screen.height * 0.66f, width, 230f * scale);
        DrawRect(panel, new Color(0.025f, 0.045f, 0.06f, 0.94f));

        string title = fishing.State switch
        {
            FishingState.WaitingForBite => "Menunggu ikan menggigit...",
            FishingState.Bite => "BITE! Tekan F / USE TOOL",
            _ => $"Tarik {fishing.ActiveCatchLabel}"
        };
        GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 34f * scale), title, titleStyle);
        string bait = fishing.ActiveCastBait != null
            ? $"Bait: {fishing.ActiveCastBait.itemName}  |  Sisa: {fishing.ActiveCastBaitRemaining}"
            : "Bait: None";
        GUI.Label(new Rect(panel.x + 22f, panel.y + 40f, panel.width - 44f, 24f * scale),
            $"{bait}  |  Fishing Lv.{fishing.FishingLevel}", labelStyle);

        if (fishing.State == FishingState.Bite)
        {
            DrawProgress(new Rect(panel.x + 28f, panel.y + 70f, panel.width - 56f, 24f * scale), fishing.HookTimeRemaining,
                new Color(1f, 0.68f, 0.1f), new Color(0.13f, 0.15f, 0.17f));
            return;
        }
        if (fishing.State != FishingState.Minigame) return;

        Rect track = new(panel.x + 34f, panel.y + 70f, panel.width - 68f, 44f * scale);
        DrawRect(track, new Color(0.09f, 0.12f, 0.14f));
        float zoneWidth = track.width * (fishing.ActiveFish != null ? fishing.ActiveFish.catchZoneSize : 0.3f);
        Rect fishZone = new(track.x + fishing.FishPosition * track.width - zoneWidth * 0.5f, track.y, zoneWidth, track.height);
        fishZone.x = Mathf.Clamp(fishZone.x, track.x, track.xMax - fishZone.width);
        DrawRect(fishZone, new Color(0.18f, 0.68f, 0.86f, 0.82f));
        float cursorX = track.x + fishing.ReelPosition * track.width;
        DrawRect(new Rect(cursorX - 4f * scale, track.y - 5f, 8f * scale, track.height + 10f), Color.white);

        GUI.Label(new Rect(track.x, track.yMax + 8f, 130f, 24f), "Catch", labelStyle);
        DrawProgress(new Rect(track.x + 90f * scale, track.yMax + 9f, track.width - 90f * scale, 18f * scale), fishing.CatchProgress,
            new Color(0.22f, 0.82f, 0.4f), new Color(0.12f, 0.15f, 0.16f));
        GUI.Label(new Rect(track.x, track.yMax + 35f * scale, 130f, 24f), "Stress", labelStyle);
        DrawProgress(new Rect(track.x + 90f * scale, track.yMax + 36f * scale, track.width - 90f * scale, 18f * scale), fishing.LineStress,
            new Color(0.95f, 0.24f, 0.18f), new Color(0.12f, 0.15f, 0.16f));

        Rect reelButton = new(panel.xMax - 178f * scale, panel.yMax - 58f * scale, 150f * scale, 42f * scale);
        MobileReelHeld = GUI.RepeatButton(reelButton, "TAHAN REEL", buttonStyle);
        GUI.Label(new Rect(panel.x + 28f, panel.yMax - 52f * scale, panel.width - 220f * scale, 40f), "Tahan F/klik untuk naik • lepas untuk turun", labelStyle);
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
