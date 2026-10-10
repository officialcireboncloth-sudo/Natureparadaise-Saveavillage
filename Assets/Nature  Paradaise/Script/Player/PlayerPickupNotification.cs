using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable top-center screen notifications. Item sprites share their ItemSO reference.</summary>
[DisallowMultipleComponent]
public sealed class PlayerPickupNotification : MonoBehaviour
{
    sealed class Entry { public RectTransform root; public TMP_Text text; public Image icon; public CanvasGroup group; public float age; }
    static PlayerPickupNotification instance;
    readonly List<Entry> entries = new();
    const float Duration = 2.5f;
    public static void Show(Transform player, string message)
    {
        if (player != null && !string.IsNullOrWhiteSpace(message)) Ensure().Display(message, null);
    }
    public static void ShowItem(Inventory inventory, ItemSO item, int amount)
    {
        if (inventory == null || item == null || amount <= 0) return;
        Ensure().Display($"{item.itemName} ditemukan  +{amount}", item.icon);
    }
    static PlayerPickupNotification Ensure()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<PlayerPickupNotification>();
        if (instance == null) instance = new GameObject("ItemDiscoveryToast", typeof(RectTransform)).AddComponent<PlayerPickupNotification>();
        return instance;
    }
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        var root = new GameObject("ItemDiscoveryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        // Independent root: player scale and movement cannot alter notification layout.
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 310;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        canvasRoot = root;
    }
    GameObject canvasRoot;
    void Display(string message, Sprite sprite)
    {
        Entry entry = entries.Find(x => !x.root.gameObject.activeSelf);
        if (entry == null && entries.Count < 4)
        {
            var rect = GameplayHUDStyle.Rect("Discovery", canvasRoot.transform, new Vector2(.5f,1), new Vector2(.5f,1));
            rect.pivot = new Vector2(.5f,1); rect.sizeDelta = new Vector2(560,56);
            var bg = GameplayHUDStyle.Surface(rect,new Color(.07f,.12f,.16f,.8f));
            var theme = Resources.Load<MiningHUDTheme>("UI/MiningHUDTheme");
            if (theme != null) MiningHUD.ApplyArtwork(rect, theme.notificationPanel, bg);
            var iconRect = GameplayHUDStyle.Rect("SharedItemIcon",rect,new Vector2(.025f,.12f),new Vector2(.12f,.88f));
            var icon = iconRect.gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            entry = new Entry { root=rect, icon=icon, group=rect.gameObject.AddComponent<CanvasGroup>(),
                text=GameplayHUDStyle.Text("Message",rect,"",22,new Vector2(.14f,.08f),new Vector2(.98f,.92f)) };
            entries.Add(entry);
        }
        if (entry == null) { entry=entries[0]; foreach(var candidate in entries) if(candidate.age>entry.age) entry=candidate; }
        entry.age=0; entry.text.text=message; entry.icon.sprite=sprite; entry.icon.enabled=sprite!=null;
        entry.group.alpha=1; entry.root.gameObject.SetActive(true); Layout();
    }
    void Layout()
    {
        int lane=0;
        foreach(var entry in entries) if(entry.root.gameObject.activeSelf) entry.root.anchoredPosition=new Vector2(0,-110-62*lane++);
    }
    void Update()
    {
        foreach(var entry in entries)
        {
            if(!entry.root.gameObject.activeSelf) continue;
            entry.age+=Time.unscaledDeltaTime;
            entry.group.alpha=Mathf.Clamp01((Duration-entry.age)/.6f);
            if(entry.age>=Duration) entry.root.gameObject.SetActive(false);
        }
        Layout();
    }
    void OnDestroy() { if(canvasRoot!=null) Destroy(canvasRoot); if(instance==this) instance=null; }
}
