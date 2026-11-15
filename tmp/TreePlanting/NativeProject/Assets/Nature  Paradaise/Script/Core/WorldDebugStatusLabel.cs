using UnityEngine;

/// <summary>Label world-space ringan untuk kebutuhan debug status mesin dan tempat pakan.</summary>
public sealed class WorldDebugStatusLabel : MonoBehaviour
{
    TextMesh textMesh;
    Camera targetCamera;

    public static WorldDebugStatusLabel GetOrCreate(Transform parent, string objectName, Vector3 localPosition)
    {
        if (parent == null) return null;
        Transform existing = parent.Find(objectName);
        GameObject owner = existing != null ? existing.gameObject : new GameObject(objectName);
        if (existing == null)
        {
            owner.transform.SetParent(parent, false);
        }
        owner.transform.localPosition = localPosition;
        WorldDebugStatusLabel label = owner.GetComponent<WorldDebugStatusLabel>();
        if (label == null) label = owner.AddComponent<WorldDebugStatusLabel>();
        label.EnsureText();
        return label;
    }

    public void SetText(string value)
    {
        EnsureText();
        textMesh.text = value ?? string.Empty;
    }

    public void SetColor(Color value)
    {
        EnsureText();
        textMesh.color = value;
    }

    void EnsureText()
    {
        if (textMesh == null) textMesh = GetComponent<TextMesh>();
        if (textMesh == null) textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.13f;
        textMesh.fontSize = 42;
        textMesh.color = new Color(1f, 0.92f, 0.35f, 1f);
    }

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position,
            targetCamera.transform.up);
        if (transform.parent != null)
        {
            Vector3 scale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        }
    }
}

public static class GameTimeDebugText
{
    public static string UntilMidnight()
    {
        if (TimeManager.Instance == null) return "jam tidak tersedia";
        int remaining = 24 * 60 - (TimeManager.Instance.hour * 60 + TimeManager.Instance.minute);
        return FormatMinutes(Mathf.Max(0, remaining));
    }

    public static string FormatHours(double hours)
    {
        if (double.IsNaN(hours) || double.IsInfinity(hours)) return "menunggu slot/output";
        return FormatMinutes(Mathf.Max(0, Mathf.CeilToInt((float)(hours * 60d))));
    }

    public static string FormatMinutes(int minutes)
    {
        int hours = Mathf.Max(0, minutes) / 60;
        int remainder = Mathf.Max(0, minutes) % 60;
        return hours > 0 ? $"{hours}j {remainder}m" : $"{remainder}m";
    }
}
