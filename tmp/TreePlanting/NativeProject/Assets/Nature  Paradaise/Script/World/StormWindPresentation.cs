using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Shared wind presentation; temporary material values are restored when weather ends.</summary>
public sealed class StormWindPresentation : MonoBehaviour
{
    sealed class Leaves { public Material material; public float intensity,speed; public Vector4 direction; public bool hasDirection; }
    sealed class Grass { public TerrainData data; public float strength,speed,amount; }
    readonly List<Leaves> leaves=new();
    readonly List<Grass> grass=new();
    readonly Dictionary<Material,Material> copies=new();
    readonly Dictionary<Renderer,Material[]> bindings=new();
    float nextScan;
    WindZone zone;
    float strength;
    bool dirty=true;
    public float Strength=>strength;
    void OnEnable()=>SceneManager.sceneLoaded+=SceneLoaded;
    void SceneLoaded(Scene _,LoadSceneMode __)=>dirty=true;
    void Cache()
    {
        Restore();
        ScanLeaves();
        foreach(var terrain in Terrain.activeTerrains)
        {
            var host=terrain.GetComponent<TerrainRuntimeDataHost>();
            if(host==null)host=terrain.gameObject.AddComponent<TerrainRuntimeDataHost>();
            if(!host.EnsureInitialized()||terrain.terrainData==null)continue;
            var data=terrain.terrainData;
            grass.Add(new Grass{data=data,strength=data.wavingGrassStrength,speed=data.wavingGrassSpeed,amount=data.wavingGrassAmount});
        }
        if(zone==null){zone=new GameObject("Shared storm wind",typeof(WindZone)).GetComponent<WindZone>();zone.transform.SetParent(transform,false);zone.mode=WindZoneMode.Directional;}
        zone.transform.rotation=Quaternion.LookRotation(RainVisualProfile.WindDirection);dirty=false;
    }
    void ScanLeaves()
    {
        nextScan=Time.unscaledTime+3;
        foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if(bindings.ContainsKey(renderer))continue;
            var original=renderer.sharedMaterials;Material[] assigned=null;
            for(int i=0;i<original.Length;i++)
            {
                var source=original[i];
                if(source==null||!source.HasProperty("_WindIntensity")||!source.HasProperty("_WindSpeed"))continue;
                if(!copies.TryGetValue(source,out var copy))
                {
                    copy=new Material(source){name=source.name+" (Runtime Storm Wind)",hideFlags=HideFlags.DontSave};copies.Add(source,copy);
                    leaves.Add(new Leaves{material=copy,intensity=copy.GetFloat("_WindIntensity"),speed=copy.GetFloat("_WindSpeed"),hasDirection=copy.HasProperty("_Wind_Direction"),direction=copy.HasProperty("_Wind_Direction")?copy.GetVector("_Wind_Direction"):Vector4.zero});
                }
                assigned??=(Material[])original.Clone();assigned[i]=copy;
            }
            if(assigned!=null){bindings.Add(renderer,original);renderer.sharedMaterials=assigned;}
        }
    }
    /// <summary>Drives visuals only; preserves collider roots, tree growth and authored terrain data.</summary>
    public void Tick(WeatherType weather,bool outdoors,float deltaTime)
    {
        if(!outdoors){Clear();return;}
        float target=RainVisualProfile.WindStrength(weather);
        strength=Mathf.MoveTowards(strength,target,Mathf.Clamp(deltaTime,0,.1f)/1.2f);
        if(strength<=.0001f){if(leaves.Count>0||grass.Count>0)Restore();Shader.SetGlobalVector("_NP_StormWind",Vector4.zero);if(zone!=null)zone.windMain=0;return;}
        if(dirty)Cache();
        else if(Time.unscaledTime>=nextScan)ScanLeaves();
        Vector3 direction=RainVisualProfile.WindDirection;
        float gust=.8f+.2f*Mathf.Sin(Time.time*1.7f);
        Shader.SetGlobalVector("_NP_StormWind",new Vector4(direction.x,0,direction.z,strength*gust));
        if(zone!=null){zone.windMain=strength*1.5f;zone.windTurbulence=strength*.25f;zone.windPulseMagnitude=.15f;zone.windPulseFrequency=.4f;}
        foreach(var item in leaves)if(item.material!=null)
        {
            item.material.SetFloat("_WindIntensity",item.intensity*(1+strength*2.5f*gust));
            item.material.SetFloat("_WindSpeed",item.speed*(1+strength*1.6f));
            if(item.hasDirection)item.material.SetVector("_Wind_Direction",new Vector4(direction.x,0,direction.z,0));
        }
        foreach(var item in grass)if(item.data!=null)
        {item.data.wavingGrassStrength=Mathf.Lerp(item.strength,.8f,strength);item.data.wavingGrassSpeed=Mathf.Lerp(item.speed,.8f,strength);item.data.wavingGrassAmount=Mathf.Lerp(item.amount,.65f,strength);}
        foreach(var tree in WorldTree.Active)if(tree!=null)tree.ApplyWeatherWind(direction,strength*gust,Time.time);
    }
    void Restore()
    {
        foreach(var item in leaves)if(item.material!=null){item.material.SetFloat("_WindIntensity",item.intensity);item.material.SetFloat("_WindSpeed",item.speed);if(item.hasDirection)item.material.SetVector("_Wind_Direction",item.direction);}
        foreach(var pair in bindings)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
        bindings.Clear();foreach(var copy in copies.Values)if(copy!=null)Destroy(copy);copies.Clear();leaves.Clear();
        foreach(var item in grass)if(item.data!=null){item.data.wavingGrassStrength=item.strength;item.data.wavingGrassSpeed=item.speed;item.data.wavingGrassAmount=item.amount;}
        grass.Clear();dirty=true;
        foreach(var tree in WorldTree.Active)if(tree!=null)tree.ApplyWeatherWind(Vector3.forward,0,0);
    }
    public void Clear(){if(strength>0||leaves.Count>0||grass.Count>0)Restore();strength=0;Shader.SetGlobalVector("_NP_StormWind",Vector4.zero);if(zone!=null)zone.windMain=0;}
    void OnDisable(){SceneManager.sceneLoaded-=SceneLoaded;Clear();}
}
