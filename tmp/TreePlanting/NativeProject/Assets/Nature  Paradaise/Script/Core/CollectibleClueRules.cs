using System;

/// <summary>Distance fade for discovery clues, independent of a renderer or camera.</summary>
public static class CollectibleClueRules
{
    /// <summary>Final rendered opacity: fully transparent between glints, full strength at the peak.</summary>
    public static float Opacity(float distance, float radius, float time, float phase)
        => Visibility(distance, radius) * Twinkle(time, phase);

    /// <summary>A short soft glint followed by a long quiet interval; phase staggers nearby items.</summary>
    public static float Twinkle(float time, float phase)
    {
        float cycle = (time + phase) % 5f;
        if (cycle < 0f) cycle += 5f;
        if (cycle >= .85f) return 0f;
        float wave = (float)Math.Sin(cycle / .85f * Math.PI);
        return wave * wave;
    }

    public static float Visibility(float distance, float radius)
    {
        if (radius <= 0f || float.IsNaN(distance)) return 0f;
        float t = Math.Max(0f, Math.Min(1f, (distance - radius * .8f) / (radius * .2f)));
        return 1f - t * t * (3f - 2f * t);
    }
}
