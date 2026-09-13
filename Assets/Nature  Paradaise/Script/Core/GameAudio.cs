using System;
using UnityEngine;

public enum GameAudioBus { Main, Ambient }

/// <summary>
/// Volume global modular. Master memengaruhi AudioListener; Ambient berisi ambience/music,
/// sedangkan Main berisi UI, langkah kaki, tool, hit, bell, dan efek gameplay.
/// </summary>
public static class GameAudio
{
    const string MasterKey = "audio.master";
    const string AmbientKey = "audio.ambient";
    const string MainKey = "audio.main";

    public static event Action VolumesChanged;

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterKey, 1f);
    public static float AmbientVolume => PlayerPrefs.GetFloat(AmbientKey, 0.8f);
    public static float MainVolume => PlayerPrefs.GetFloat(MainKey, 0.9f);
    public static float Volume(GameAudioBus bus) => bus == GameAudioBus.Ambient ? AmbientVolume : MainVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() => ApplyMaster();

    public static void SetMaster(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        ApplyAndSave();
    }

    public static void SetAmbient(float value)
    {
        PlayerPrefs.SetFloat(AmbientKey, Mathf.Clamp01(value));
        ApplyAndSave();
    }

    public static void SetMain(float value)
    {
        PlayerPrefs.SetFloat(MainKey, Mathf.Clamp01(value));
        ApplyAndSave();
    }

    static void ApplyAndSave()
    {
        ApplyMaster();
        PlayerPrefs.Save();
        VolumesChanged?.Invoke();
    }

    static void ApplyMaster() => AudioListener.volume = MasterVolume;

    public static void PlayOneShot(AudioSource source, AudioClip clip, GameAudioBus bus, float baseVolume = 1f)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, Mathf.Clamp01(baseVolume) * Volume(bus));
    }

    public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, GameAudioBus bus, float baseVolume = 1f)
    {
        if (clip == null) return null;
        GameObject temporary = new($"Audio_{bus}_{clip.name}");
        temporary.transform.position = position;
        AudioSource source = temporary.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.volume = Mathf.Clamp01(baseVolume) * Volume(bus);
        source.Play();
        UnityEngine.Object.Destroy(temporary, clip.length + 0.2f);
        return source;
    }
}
