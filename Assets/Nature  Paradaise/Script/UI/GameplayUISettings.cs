using System;
using UnityEngine;

/// <summary>Only supported options are saved here; pause menu edits a draft until Apply.</summary>
[Serializable]
public sealed class GameplayUISettings
{
    public int textSize = 1;
    public int uiSize = 1;
    public int quality;
    public bool highContrast;
    public bool reduceCameraMotion;
    public bool showItemNames = true;
    public bool confirmExit = true;
    public float cameraSensitivity = .55f;
    public float opacity = .70f;
    public float master = 1f, music = .8f, effects = .9f;
    public static event Action Applied;
    static GameplayUISettings current;
    public static GameplayUISettings Current => current ??= Load();
    public GameplayUISettings Copy() => JsonUtility.FromJson<GameplayUISettings>(JsonUtility.ToJson(this));
    static GameplayUISettings Load()
    {
        var value = new GameplayUISettings { quality = QualitySettings.GetQualityLevel() };
        try { if (PlayerPrefs.HasKey("NatureParadise.UISettings")) value = JsonUtility.FromJson<GameplayUISettings>(PlayerPrefs.GetString("NatureParadise.UISettings")) ?? value; }
        catch (ArgumentException) { }
        value.master = GameAudio.MasterVolume; value.music = GameAudio.AmbientVolume; value.effects = GameAudio.MainVolume;
        return value;
    }
    public void Apply()
    {
        textSize = Mathf.Clamp(textSize,0,2); uiSize = Mathf.Clamp(uiSize,0,2);
        cameraSensitivity = Mathf.Clamp01(cameraSensitivity); opacity = Mathf.Clamp(opacity,.2f,1f);
        GameAudio.SetMaster(master); GameAudio.SetAmbient(music); GameAudio.SetMain(effects);
        if (QualitySettings.names.Length > 0)
        {
            quality = Mathf.Clamp(quality,0,QualitySettings.names.Length-1);
            QualitySettings.SetQualityLevel(quality,true);
            PlayerPrefs.SetInt("NatureParadise.GraphicsQuality", quality);
        }
        current = Copy();
        foreach (var scaler in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsSortMode.None))
            if (scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scaler.referenceResolution = new Vector2(1920,1080)/(uiSize==0?.9f:uiSize==2?1.1f:1f);
        PlayerPrefs.SetString("NatureParadise.UISettings",JsonUtility.ToJson(this)); PlayerPrefs.Save();
        Applied?.Invoke();
    }
}
