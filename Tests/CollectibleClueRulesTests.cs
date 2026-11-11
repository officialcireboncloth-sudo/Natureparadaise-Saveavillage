using System;
class CollectibleClueRulesTests
{
    static int checks;
    static void Check(bool valid, string label) { checks++; if (!valid) throw new Exception(label); }
    static void Main()
    {
        foreach (float radius in new[] { 1f, 3f, 6f, 12f })
        {
            Check(CollectibleClueRules.Visibility(0, radius) == 1f, "Nearby collectible stays visible");
            Check(CollectibleClueRules.Visibility(radius * .5f, radius) == 1f, "Inner range is fully readable");
            Check(CollectibleClueRules.Visibility(radius * .8f, radius) == 1f, "Clue is clear before the player gets close");
            Check(CollectibleClueRules.Visibility(radius * .9f, radius) > .35f, "Outer discovery hint remains noticeable");
            Check(CollectibleClueRules.Visibility(radius, radius) == 0f, "No abrupt glow outside discovery range");
            Check(CollectibleClueRules.Visibility(radius * 2f, radius) == 0f, "Distant objects do not glow");
            float previous = 1f;
            for (int i = 0; i <= 100; i++)
            {
                float alpha = CollectibleClueRules.Visibility(radius * i / 100f, radius);
                Check(alpha >= 0 && alpha <= 1 && alpha <= previous + .00001f, "Fade is bounded and decreases smoothly with distance");
                previous = alpha;
            }
        }
        Check(CollectibleClueRules.Visibility(0, 0) == 0f, "Disabled discovery radius is safe");
        Check(CollectibleClueRules.Visibility(float.NaN, 6) == 0f, "Invalid positions do not create invalid mesh colors");
        Check(CollectibleClueRules.Twinkle(0, 0) == 0f, "A pulse fades in rather than popping on");
        Check(CollectibleClueRules.Twinkle(.425f, 0) > .99f, "A brief glint remains readable");
        Check(CollectibleClueRules.Twinkle(1f, 0) == 0f, "The clue rests after the glint");
        Check(CollectibleClueRules.Twinkle(4.9f, 0) == 0f, "The quiet interval lasts until the next cycle");
        int quietSamples = 0;
        for (int i = 0; i < 100; i++)
        {
            float time = i * .05f, alpha = CollectibleClueRules.Twinkle(time, .37f);
            Check(alpha >= 0f && alpha <= 1f, "Twinkle opacity stays bounded");
            Check(Math.Abs(alpha - CollectibleClueRules.Twinkle(time + 5f, .37f)) < .0001f, "Twinkle repeats without abrupt drift");
            if (alpha == 0f) quietSamples++;
        }
        Check(quietSamples >= 80, "At least 80 percent of each cycle stays quiet");
        Check(CollectibleClueRules.Opacity(0, 12, 0, 0) == 0f, "Rendered sparkle starts fully transparent");
        Check(CollectibleClueRules.Opacity(0, 12, .425f, 0) > .999f, "Rendered sparkle reaches full opacity");
        Check(CollectibleClueRules.Opacity(0, 12, .85f, 0) == 0f, "Rendered sparkle ends fully transparent");
        Check(CollectibleClueRules.Opacity(0, 12, 1.5f, 0) == 0f, "No persistent base marker during the pause");
        Check(CollectibleClueRules.Opacity(0, 12, 4.9f, 0) == 0f, "Rendered sparkle stays absent until the next cycle");
        Check(CollectibleClueRules.Opacity(12, 12, .425f, 0) == 0f, "Twinkle does not override the distance boundary");
        Console.WriteLine(checks + " collectible distance-fade checks passed (actual rules; no rendering/Play Mode).");
    }
}
