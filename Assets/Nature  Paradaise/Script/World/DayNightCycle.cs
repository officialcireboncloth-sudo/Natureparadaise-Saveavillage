using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
/// <summary>
/// Menggerakkan matahari dan mengubah ambient lighting berdasarkan waktu 24 jam.
/// Modifier cuaca diterima terpisah agar jadwal cuaca tidak bergantung pada implementasi lighting.
/// </summary>
public sealed class DayNightCycle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TimeManager timeManager;
    [SerializeField] Light sun;

    [Header("Sun")]
    [SerializeField, Range(0f, 360f)] float sunYaw = 170f;
    [SerializeField, Min(0f)] float dayIntensity = 1.5f;
    [Tooltip("Intensitas cahaya bulan. Cukup untuk membaca medan, tetapi tetap jauh lebih redup dari siang.")]
    [SerializeField, Min(0f)] float nightIntensity = 0.16f;
    [SerializeField] Color sunriseColor = new(1f, 0.48f, 0.25f, 1f);
    [SerializeField] Color noonColor = new(1f, 0.96f, 0.84f, 1f);
    [SerializeField] Color moonColor = new(0.30f, 0.38f, 0.62f, 1f);

    [Header("Environment")]
    [SerializeField] Color dayAmbient = new(0.68f, 0.75f, 0.83f, 1f);
    [SerializeField] Color sunsetAmbient = new(0.42f, 0.25f, 0.22f, 1f);
    [SerializeField] Color nightAmbient = new(0.09f, 0.12f, 0.2f, 1f);
    [SerializeField, Range(0f, 2f)] float dayReflectionIntensity = 1f;
    [SerializeField, Range(0f, 2f)] float nightReflectionIntensity = 0.32f;
    [SerializeField, Range(0.05f, 1f)] float nightSkyExposure = 0.3f;

    float weatherSunMultiplier = 1f;
    float weatherAmbientMultiplier = 1f;
    float weatherExposureMultiplier = 1f;
    Color weatherTint = Color.white;
    float targetWeatherSunMultiplier = 1f;
    float targetWeatherAmbientMultiplier = 1f;
    float targetWeatherExposureMultiplier = 1f;
    Color targetWeatherTint = Color.white;
    float weatherFogDensity;
    float targetWeatherFogDensity;
    float cloudShadowWeight;
    float targetCloudShadowWeight;
    RuntimeTerrainCloudShadow terrainCloudShadow;
    bool receivedWeatherState;
    Light interiorLight;
    bool suppressingWorldSun;
    bool worldSunWasEnabled;

    void Awake()
    {
        ResolveReferences();
        if (sun != null)
        {
            RenderSettings.sun = sun;
            // Bersihkan cookie eksperimen lama; shadow terrain ditangani overlay noise di bawah.
            sun.cookie = null;
        }
        EnsureTerrainCloudShadow();
    }

    void OnEnable()
    {
        WeatherImpactFlow.LightingUpdated += HandleWeatherImpact;
        WeatherImpactFlow.SkyUpdated += HandleWeatherImpact;
        if (WeatherImpactFlow.HasCurrent) HandleWeatherImpact(WeatherImpactFlow.Current);
    }

    void OnDisable()
    {
        WeatherImpactFlow.LightingUpdated -= HandleWeatherImpact;
        WeatherImpactFlow.SkyUpdated -= HandleWeatherImpact;
        if (terrainCloudShadow != null) terrainCloudShadow.SetWeight(0f);
        RestoreWorldSun();
    }

    void OnDestroy()
    {
        if (terrainCloudShadow != null)
            Destroy(terrainCloudShadow.gameObject);
    }

    void HandleWeatherImpact(WeatherImpactSnapshot impact)
    {
        targetWeatherSunMultiplier = impact.SunMultiplier;
        targetWeatherAmbientMultiplier = impact.AmbientMultiplier;
        targetWeatherExposureMultiplier = impact.SkyExposureMultiplier;
        targetWeatherTint = impact.LightingTint;
        targetWeatherFogDensity = impact.FogDensity;
        targetCloudShadowWeight = impact.Weather switch
        {
            WeatherType.Sunny => 0.38f,
            WeatherType.PartlyCloudy => 0.78f,
            WeatherType.Drizzle => 0.8f,
            WeatherType.Rain => 0.9f,
            WeatherType.HeavyRain or WeatherType.WindRainStorm or WeatherType.Cyclone or WeatherType.Thunderstorm => 1f,
            WeatherType.Snow => 0.62f,
            WeatherType.Blizzard => 1f,
            _ => 0f
        };
        if (!receivedWeatherState)
        {
            receivedWeatherState = true;
            weatherSunMultiplier = targetWeatherSunMultiplier;
            weatherAmbientMultiplier = targetWeatherAmbientMultiplier;
            weatherExposureMultiplier = targetWeatherExposureMultiplier;
            weatherTint = targetWeatherTint;
            weatherFogDensity = targetWeatherFogDensity;
            cloudShadowWeight = targetCloudShadowWeight;
        }
    }

    void Update()
    {
        if (!WeatherSystem.IsPlayerOutdoors())
        {
            ApplyInteriorLighting();
            return;
        }
        RestoreWorldSun();
        if (timeManager == null || sun == null)
        {
            ResolveReferences();
            if (timeManager == null || sun == null)
                return;
        }

        ApplyLighting(timeManager.CurrentTimeHours);
    }

    void ApplyInteriorLighting()
    {
        ResolveReferences();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (sun != null && sun.gameObject.scene != scene && !suppressingWorldSun)
        {
            worldSunWasEnabled = sun.enabled;
            suppressingWorldSun = true;
            sun.enabled = false;
        }
        if (interiorLight == null || interiorLight.gameObject.scene != scene)
        {
            interiorLight = sun != null && sun.gameObject.scene == scene ? sun : null;
            foreach (Light candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (candidate != sun && candidate.type == LightType.Directional && candidate.gameObject.scene == scene)
                {
                    interiorLight = candidate;
                    break;
                }
        }
        RenderSettings.sun = interiorLight;
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0f;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.72f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.55f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.32f, 0.35f);
        RenderSettings.reflectionIntensity = 0.65f;
        if (terrainCloudShadow != null) terrainCloudShadow.SetWeight(0f);
    }

    void RestoreWorldSun()
    {
        if (!suppressingWorldSun) return;
        if (sun != null) sun.enabled = worldSunWasEnabled;
        suppressingWorldSun = false;
    }

    void ResolveReferences()
    {
        if (timeManager == null)
            timeManager = TimeManager.Instance != null
                ? TimeManager.Instance
                : FindFirstObjectByType<TimeManager>();
        if (sun == null)
            sun = GetComponent<Light>();
    }

    void ApplyLighting(float hour)
    {
        RenderSettings.sun = sun;
        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 1.25f);
        weatherSunMultiplier = Mathf.Lerp(weatherSunMultiplier, targetWeatherSunMultiplier, blend);
        weatherAmbientMultiplier = Mathf.Lerp(weatherAmbientMultiplier, targetWeatherAmbientMultiplier, blend);
        weatherExposureMultiplier = Mathf.Lerp(weatherExposureMultiplier, targetWeatherExposureMultiplier, blend);
        weatherTint = Color.Lerp(weatherTint, targetWeatherTint, blend);
        weatherFogDensity = Mathf.Lerp(weatherFogDensity, targetWeatherFogDensity, blend);
        cloudShadowWeight = Mathf.Lerp(cloudShadowWeight, targetCloudShadowWeight, blend);

        GetDaylightWindow(SeasonVisualController.CurrentSeason, out float sunrise, out float sunset);
        float dayProgress = Mathf.Repeat(hour, 24f) / 24f;
        float solarAngle = dayProgress * 360f - 90f;
        float dayPhase = Mathf.InverseLerp(sunrise, sunset, hour);
        float sunHeight = hour >= sunrise && hour <= sunset
            ? Mathf.Sin(dayPhase * Mathf.PI)
            : -0.2f;
        float dawnBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunrise - 0.45f, sunrise + 0.6f, hour));
        float duskBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunset - 0.6f, sunset + 0.45f, hour));
        float daylight = Mathf.Clamp01(dawnBlend * duskBlend);
        float noonWeight = Mathf.Clamp01(sunHeight);

        // Pada malam hari sumber directional beralih ke sisi bulan. Tanpa rotasi lawan ini,
        // light mengarah dari bawah terrain sehingga menaikkan intensity tidak memberi cahaya nyata.
        // Moon and sun are exactly 180 degrees apart. Quaternion.Slerp's shortest
        // arc is ambiguous at that angle: floating-point rounding can switch the
        // arc between frames and flip the light above/below the ground at twilight.
        // Interpolate the explicit angle so the transition always takes one path.
        float lightingAngle = solarAngle + (1f - daylight) * 180f;
        sun.transform.rotation = Quaternion.Euler(lightingAngle, sunYaw, 0f);
        EnsureTerrainCloudShadow();
        WeatherSystem weather = WeatherSystem.Instance;
        bool cloudEnabled = weather == null || weather.TerrainCloudShadowEnabled;
        if (terrainCloudShadow != null)
        {
            terrainCloudShadow.Configure(
                weather != null ? weather.TerrainCloudShadowOpacity : 0.34f,
                weather != null ? weather.TerrainCloudNoiseWorldScale : 80f,
                weather != null ? weather.TerrainCloudDriftSpeed : new Vector2(0.012f, 0.007f));
            // Overlay awan tetap terasa saat malam, tetapi tidak boleh menumpuk menjadi lapisan hitam.
            float nighttimeCloudVisibility = Mathf.Lerp(0.42f, 1f, daylight);
            terrainCloudShadow.SetWeight(cloudEnabled ? cloudShadowWeight * nighttimeCloudVisibility : 0f);
        }
        float passingCloud = Mathf.PerlinNoise(Time.unscaledTime * 0.04f, 17.35f);
        float cloudSunMultiplier = Mathf.Lerp(1f, Mathf.Lerp(0.72f, 0.9f, passingCloud), cloudShadowWeight);
        float daylightIntensity = dayIntensity * weatherSunMultiplier;
        // Awan mengurangi cahaya bulan, tetapi minimum ini menjaga siluet medan dan karakter terbaca.
        float moonWeatherMultiplier = Mathf.Lerp(0.62f, 1f, Mathf.Clamp01(weatherSunMultiplier));
        float moonlightIntensity = nightIntensity * moonWeatherMultiplier;
        sun.intensity = Mathf.Lerp(moonlightIntensity, daylightIntensity, daylight) *
                        SeasonVisualController.SunMultiplier * cloudSunMultiplier;

        Color horizonToNoon = Color.Lerp(sunriseColor, noonColor, noonWeight);
        sun.color = Color.Lerp(moonColor, horizonToNoon, daylight) * weatherTint *
                    SeasonVisualController.LightingTint;

        float dawnTwilight = 1f - Mathf.Clamp01(Mathf.Abs(hour - sunrise) / 1.1f);
        float duskTwilight = 1f - Mathf.Clamp01(Mathf.Abs(hour - sunset) / 1.1f);
        float twilight = Mathf.Max(dawnTwilight, duskTwilight);
        Color daylightAmbient = Color.Lerp(dayAmbient, sunsetAmbient, twilight);
        float cloudAmbientMultiplier = Mathf.Lerp(1f, 0.92f, cloudShadowWeight);
        Color ambient = Color.Lerp(nightAmbient, daylightAmbient, daylight) * weatherTint * weatherAmbientMultiplier *
                        SeasonVisualController.LightingTint * SeasonVisualController.AmbientMultiplier * cloudAmbientMultiplier;
        // Cuaca ekstrem masih meredupkan malam, tetapi tidak boleh menghapus seluruh informasi visual.
        float nightVisibility = Mathf.Lerp(0.68f, 1f, Mathf.Clamp01(weatherAmbientMultiplier));
        Color nightVisibilityFloor = nightAmbient * nightVisibility * SeasonVisualController.LightingTint;
        ambient = MaxColor(ambient, nightVisibilityFloor * (1f - daylight));

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambient;
        RenderSettings.ambientEquatorColor = Color.Lerp(ambient * 0.7f, ambient, daylight);
        RenderSettings.ambientGroundColor = ambient * 0.45f;
        RenderSettings.reflectionIntensity = Mathf.Lerp(nightReflectionIntensity, dayReflectionIntensity, daylight);
        float morningFog = WeatherSystem.Instance != null
            ? WeatherSystem.Instance.GetSeasonalMorningFogDensity(hour)
            : 0f;
        float resolvedFogDensity = Mathf.Max(weatherFogDensity, morningFog);
        RenderSettings.fog = resolvedFogDensity > 0.0001f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = resolvedFogDensity;
        RenderSettings.fogColor = Color.Lerp(ambient, weatherTint * 0.65f, 0.35f);

        Material skybox = RenderSettings.skybox;
        if (skybox != null && skybox.HasProperty("_Exposure"))
        {
            float weatherNightExposure = nightSkyExposure *
                Mathf.Lerp(0.68f, 1f, Mathf.Clamp01(weatherExposureMultiplier));
            float weatherDayExposure = weatherExposureMultiplier;
            skybox.SetFloat("_Exposure", Mathf.Lerp(weatherNightExposure, weatherDayExposure, daylight) *
                                           SeasonVisualController.SkyExposureMultiplier *
                                           Mathf.Lerp(1f, 0.88f, cloudShadowWeight));
        }
    }

    static Color MaxColor(Color value, Color minimum) => new(
        Mathf.Max(value.r, minimum.r),
        Mathf.Max(value.g, minimum.g),
        Mathf.Max(value.b, minimum.b),
        Mathf.Max(value.a, minimum.a));

    static void GetDaylightWindow(CropSeason season, out float sunrise, out float sunset)
    {
        switch (season)
        {
            case CropSeason.Summer:
                sunrise = 5.25f;
                sunset = 19.25f;
                break;
            default:
                sunrise = 6f;
                sunset = 18f;
                break;
        }
    }

    void EnsureTerrainCloudShadow()
    {
        if (terrainCloudShadow != null) return;
        GameObject overlay = new("Runtime Terrain Cloud Shadows");
        // Harus tetap world-space; transform DayNightCycle berputar mengikuti matahari.
        overlay.transform.SetParent(null, false);
        terrainCloudShadow = overlay.AddComponent<RuntimeTerrainCloudShadow>();
        WeatherSystem weather = WeatherSystem.Instance;
        terrainCloudShadow.Configure(
            weather != null ? weather.TerrainCloudShadowOpacity : 0.34f,
            weather != null ? weather.TerrainCloudNoiseWorldScale : 80f,
            weather != null ? weather.TerrainCloudDriftSpeed : new Vector2(0.012f, 0.007f));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureCycleExists()
    {
        if (FindFirstObjectByType<DayNightCycle>() != null)
            return;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light directional = null;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional)
                continue;
            if (directional == null || lights[i].intensity > directional.intensity)
                directional = lights[i];
        }

        if (directional == null)
        {
            GameObject sunObject = new("NatureParadise_Sun");
            directional = sunObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.shadows = LightShadows.Soft;
        }

        directional.gameObject.AddComponent<DayNightCycle>();
        Debug.Log("[TIME] Day/night cycle aktif dan matahari mengikuti jam 24 jam.");
    }
}

/// <summary>
/// Overlay noise transparan yang mengikuti tinggi Terrain. Mesh dibangun ulang hanya ketika kamera
/// berpindah/zoom, sementara gerakan awan cukup menggeser UV material.
/// </summary>
sealed class RuntimeTerrainCloudShadow : MonoBehaviour
{
    const int GridResolution = 32;
    const float RebuildDistance = 3f;
    const float HeightOffset = 0.08f;

    Mesh mesh;
    Material material;
    Texture2D noiseTexture;
    Camera targetCamera;
    float maximumOpacity = 0.28f;
    float noiseWorldScale = 80f;
    Vector2 driftSpeed = new(0.012f, 0.007f);
    float weight;
    float nextRebuildTime;
    Vector3 lastCenter = new(float.PositiveInfinity, 0f, float.PositiveInfinity);
    float lastCameraSize = -1f;

    public void Configure(float opacity, float worldScale, Vector2 speed)
    {
        maximumOpacity = Mathf.Clamp(opacity, 0.05f, 0.6f);
        float nextWorldScale = Mathf.Clamp(worldScale, 30f, 180f);
        if (!Mathf.Approximately(noiseWorldScale, nextWorldScale))
        {
            noiseWorldScale = nextWorldScale;
            lastCameraSize = -1f;
            nextRebuildTime = 0f;
        }
        driftSpeed = speed;
        EnsureResources();
    }

    public void SetWeight(float value)
    {
        weight = Mathf.Clamp01(value);
        if (material != null)
            SetMaterialColor(new Color(0.055f, 0.075f, 0.085f, maximumOpacity * weight));
        gameObject.SetActive(weight > 0.01f);
    }

    void Update()
    {
        EnsureResources();
        if (material == null || weight <= 0.01f) return;
        float time = Time.unscaledTime;
        Vector2 drift = driftSpeed * time;
        if (material.HasProperty("_BaseMap")) material.SetTextureOffset("_BaseMap", drift);
        if (material.HasProperty("_MainTex")) material.SetTextureOffset("_MainTex", drift);
        if (time >= nextRebuildTime) RebuildForCamera();
    }

    void EnsureResources()
    {
        if (mesh != null && material != null) return;
        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
        mesh = new Mesh { name = "Runtime Terrain Cloud Shadow Mesh" };
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        // 128x128 cukup untuk bayangan lembut dan menghindari spike saat load di mobile.
        noiseTexture = BuildNoiseTexture(128);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) return;
        material = new Material(shader)
        {
            name = "Runtime Terrain Cloud Shadow Material",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = 2995
        };
        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", noiseTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", noiseTexture);
        SetMaterialColor(new Color(0.055f, 0.075f, 0.085f, 0f));
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        targetCamera = Camera.main;
        RebuildForCamera(true);
    }

    void RebuildForCamera(bool force = false)
    {
        nextRebuildTime = Time.unscaledTime + 0.35f;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || mesh == null) return;

        Vector3 center = FindGroundCenter(targetCamera);
        float cameraSize = targetCamera.orthographic
            ? targetCamera.orthographicSize
            : Vector3.Distance(targetCamera.transform.position, center) * 0.55f;
        if (!force && Vector3.SqrMagnitude(center - lastCenter) < RebuildDistance * RebuildDistance &&
            Mathf.Abs(cameraSize - lastCameraSize) < 0.1f) return;
        lastCenter = center;
        lastCameraSize = cameraSize;

        // Overlay dibuat jauh melampaui viewport agar tepi mesh tidak pernah terlihat sebagai kotak.
        float depth = Mathf.Clamp(cameraSize * 4f + 80f, 110f, 260f);
        float width = Mathf.Clamp(cameraSize * 4f * Mathf.Max(1f, targetCamera.aspect) + 80f, 140f, 320f);
        BuildTerrainMesh(center, width, depth);
    }

    static Vector3 FindGroundCenter(Camera camera)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Plane plane = new(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float distance)) return ray.GetPoint(distance);
        return camera.transform.position + camera.transform.forward * 20f;
    }

    void BuildTerrainMesh(Vector3 center, float width, float depth)
    {
        int row = GridResolution + 1;
        Vector3[] vertices = new Vector3[row * row];
        Vector2[] uv = new Vector2[vertices.Length];
        bool[] valid = new bool[vertices.Length];
        for (int z = 0; z < row; z++)
        {
            for (int x = 0; x < row; x++)
            {
                int index = z * row + x;
                float worldX = center.x + (x / (float)GridResolution - 0.5f) * width;
                float worldZ = center.z + (z / (float)GridResolution - 0.5f) * depth;
                valid[index] = TrySampleTerrain(worldX, worldZ, out float worldY);
                vertices[index] = new Vector3(worldX, worldY + HeightOffset, worldZ);
                uv[index] = new Vector2(worldX / noiseWorldScale, worldZ / noiseWorldScale);
            }
        }

        System.Collections.Generic.List<int> triangles = new(GridResolution * GridResolution * 6);
        for (int z = 0; z < GridResolution; z++)
        {
            for (int x = 0; x < GridResolution; x++)
            {
                int a = z * row + x;
                int b = a + 1;
                int c = a + row;
                int d = c + 1;
                if (!valid[a] || !valid[b] || !valid[c] || !valid[d]) continue;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateBounds();
    }

    static bool TrySampleTerrain(float x, float z, out float height)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (x < origin.x || x > origin.x + size.x || z < origin.z || z > origin.z + size.z) continue;
            height = terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y;
            return true;
        }
        height = -1000f;
        return false;
    }

    static Texture2D BuildNoiseTexture(int resolution)
    {
        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false, true)
        {
            name = "Runtime Cloud Shadow Noise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        float[] alphaMap = new float[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)resolution;
                float v = y / (float)resolution;
                float broad = SampleTileablePerlin(u, v, 2.25f, 8.7f, 14.2f);
                float medium = SampleTileablePerlin(u, v, 4.5f, 27.1f, 3.8f);
                float detail = SampleTileablePerlin(u, v, 9f, 51.4f, 36.6f);
                float cloud = broad * 0.68f + medium * 0.24f + detail * 0.08f;
                alphaMap[y * resolution + x] = Mathf.SmoothStep(0.43f, 0.69f, cloud);
            }
        }

        // Blur wrapping mempertahankan tile seamless sekaligus menghilangkan bentuk pixel/kotak.
        float[] blurred = new float[alphaMap.Length];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sum = 0f;
                    float weightSum = 0f;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            float sampleWeight = (ox == 0 ? 2f : 1f) * (oy == 0 ? 2f : 1f);
                            int sx = (x + ox + resolution) % resolution;
                            int sy = (y + oy + resolution) % resolution;
                            sum += alphaMap[sy * resolution + sx] * sampleWeight;
                            weightSum += sampleWeight;
                        }
                    }
                    blurred[y * resolution + x] = sum / weightSum;
                }
            }
            (alphaMap, blurred) = (blurred, alphaMap);
        }

        Color[] colors = new Color[resolution * resolution];
        for (int index = 0; index < colors.Length; index++)
            colors[index] = new Color(1f, 1f, 1f, alphaMap[index]);
        texture.SetPixels(colors);
        texture.Apply(false, true);
        return texture;
    }

    static float SampleTileablePerlin(float u, float v, float frequency, float offsetX, float offsetY)
    {
        float x = u * frequency;
        float y = v * frequency;
        float a = Mathf.PerlinNoise(x + offsetX, y + offsetY);
        float b = Mathf.PerlinNoise(x - frequency + offsetX, y + offsetY);
        float c = Mathf.PerlinNoise(x + offsetX, y - frequency + offsetY);
        float d = Mathf.PerlinNoise(x - frequency + offsetX, y - frequency + offsetY);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    void SetMaterialColor(Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        if (noiseTexture != null) Destroy(noiseTexture);
    }
}
