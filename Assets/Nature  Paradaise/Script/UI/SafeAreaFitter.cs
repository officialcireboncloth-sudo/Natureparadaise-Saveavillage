using UnityEngine;

public sealed class SafeAreaFitter : MonoBehaviour
{
    Rect lastSafeArea;
    Vector2Int lastScreen;

    void OnEnable() => Apply();

    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        lastSafeArea = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
    }
}
