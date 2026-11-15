using UnityEngine;

/// <summary>Local barn window daylight and warm lantern fill, independent of the outdoor sun.</summary>
public sealed class BarnInteriorCozyLighting : MonoBehaviour
{
    [SerializeField] Light[] windows;
    [SerializeField] Light[] lanterns;
    [SerializeField] Renderer panes;
    [SerializeField] int glowIndex;
    [SerializeField,Range(.1f,2)] float daylightScale=1;
    MaterialPropertyBlock block;
    float next;
    public void Configure(Light[] daylight, Light[] warm, Renderer geometry, int materialIndex,float strength=1)
    { windows=daylight;lanterns=warm;panes=geometry;glowIndex=materialIndex;daylightScale=strength; }
    void OnEnable() { next=0; }
    void Update()
    {
        if(Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;
        ApplyLighting(TimeManager.Instance!=null?TimeManager.Instance.CurrentTimeHours:10,
            WeatherSystem.Instance!=null?WeatherSystem.Instance.CurrentWeather:WeatherType.Sunny);
    }
    public void ApplyLighting(float hour,WeatherType weather)
    {
        float day=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,7,hour))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(17,19,hour)));
        float sky=weather switch { WeatherType.Sunny or WeatherType.Heatwave=>1,WeatherType.PartlyCloudy=>.72f,
            WeatherType.Cloudy=>.35f,WeatherType.Drizzle=>.28f,WeatherType.Rain=>.20f,WeatherType.HeavyRain=>.13f,WeatherType.Snow=>.48f,_=>.09f };
        for(int i=0;i<(windows?.Length??0);i++)if(windows[i]!=null)
        {
            windows[i].enabled=!Application.isMobilePlatform||i==0;
            windows[i].intensity=Mathf.Lerp(.25f,28*sky*daylightScale,day);
            windows[i].color=Color.Lerp(new Color(.47f,.63f,1),new Color(1,.88f,.66f),day);
            windows[i].shadows=Application.isMobilePlatform?LightShadows.None:LightShadows.Soft;
        }
        for(int i=0;i<(lanterns?.Length??0);i++)if(lanterns[i]!=null)
        {
            lanterns[i].enabled=!Application.isMobilePlatform||i==0;
            lanterns[i].intensity=Mathf.Lerp(5,1.8f,day*sky);lanterns[i].shadows=LightShadows.None;
        }
        if(panes==null||glowIndex<0)return;
        block??=new MaterialPropertyBlock();block.Clear();
        block.SetColor("_EmissionColor",Color.Lerp(new Color(.65f,.38f,.12f),new Color(.85f,.67f,.36f),day*sky));
        panes.SetPropertyBlock(block,glowIndex);
    }
}
