using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Nature Paradise/World/Garden Lighting Profile")]
public sealed class GardenLightingProfile : ScriptableObject
{
    [Header("Demo Gardens daylight reference")]
    public float sunIntensity = 1.2f;
    public Color sunColor = Color.white;
    public Quaternion noonRotation = new(0.28601384f, 0.43326485f, -0.35320875f, 0.77828103f);
    public Color ambientSky = new(.7924528f, .72890705f, .72890705f);
    public Color ambientEquator = new(.6603774f, .6261125f, .6261125f);
    public Color ambientGround = new(.21698111f, .1622855f, .046057317f);
    [Range(0, 1)] public float shadowStrength = .9f;
    public float shadowBias = .05f;
    public float shadowNormalBias = .4f;
    public float reflectionIntensity;
    [Header("Stylized daylight")]
    [Range(3, 45)] public float minimumDayElevation = 3;
    [Range(0, 1)] public float sunnyCloudStrength = 1;
    public bool updateAmbientProbe;
    public Color stylizedNightAmbient = new(.25f, .32f, .46f);
    public float stylizedMoonIntensity = .22f;
    [Header("Soft time-of-day palette")]
    public bool useTimeOfDayPalette;
    public Color morningSun = new(1f, .98f, .9f);
    public Color morningSky = new(.88f, .89f, .82f);
    public Color morningEquator = new(.8f, .81f, .75f);
    public Color morningGround = new(.55f, .57f, .42f);
    public Color eveningSun = new(1f, .68f, .43f);
    public Color eveningSky = new(.9f, .72f, .6f);
    public Color eveningEquator = new(.82f, .65f, .52f);
    public Color eveningGround = new(.58f, .45f, .34f);
    public Color moonColor = new(.57f, .75f, 1f);
    [Range(0, 1)] public float nightShadowStrength = .4f;
    [Header("Bright midday peak")]
    [Range(1, 2)] public float noonSunMultiplier = 1.55f;
    [Range(60, 90)] public float noonElevation = 82f;
    [Range(0, 1)] public float noonBloomIntensity = .42f;
    public float noonBloomThreshold = .85f;
    public float noonExposureBoost = .16f;
    public Color noonSky = new(.86f, .92f, 1f);
    public Color noonEquator = new(.82f, .88f, .9f);
    public Color noonGround = new(.57f, .61f, .45f);
    [Range(0, 1)] public float noonShadowStrength = .75f;
    public Material skybox;
    public VolumeProfile postProcessing;
}
