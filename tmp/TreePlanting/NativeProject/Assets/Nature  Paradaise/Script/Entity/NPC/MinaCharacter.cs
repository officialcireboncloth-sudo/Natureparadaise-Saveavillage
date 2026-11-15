using UnityEngine;

/// <summary>Upgrades the legacy scene Mina without replacing her dialogue, quest, position or persistent identity.</summary>
public static class MinaCharacter
{
    public const string ResourcePath = "Characters/Mina";
    public static void Ensure(GameObject npc)
    {
        var source = Resources.Load<GameObject>(ResourcePath);
        if (source == null) return;
        var visual = npc.transform.Find("Visual_Mina_Animated");
        if (visual == null)
        {
            var old = npc.transform.Find("Visual_Mina_ToonFarm");
            if (old != null) { old.gameObject.SetActive(false); Object.Destroy(old.gameObject); }
            visual = Object.Instantiate(source, npc.transform).transform;
            visual.name = "Visual_Mina_Animated";
        }
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        // Older Map versions also scale the NPC root (0.75). Calibrate in world metres, not twice.
        Vector3 scale = npc.transform.lossyScale;
        float x = Mathf.Max(.001f, Mathf.Abs(scale.x)), y = Mathf.Max(.001f, Mathf.Abs(scale.y)), z = Mathf.Max(.001f, Mathf.Abs(scale.z));
        visual.localScale = new Vector3(source.transform.localScale.x / x, source.transform.localScale.y / y, source.transform.localScale.z / z);
        var capsule = npc.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.height = PlayerDimensions.Height / y;
            capsule.radius = PlayerDimensions.Radius / Mathf.Max(x, z);
            capsule.center = Vector3.up * capsule.height * .5f;
        }
    }
}
