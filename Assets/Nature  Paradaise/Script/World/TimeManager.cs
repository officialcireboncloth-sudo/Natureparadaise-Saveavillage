using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jam dan kalender 24 jam yang mengubah waktu nyata menjadi waktu game,
/// mengirim event pergantian jam/hari, dan mendukung pause lock multi-owner.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public static event Action OnMinute;
    public static event Action OnHour;
    public static event Action OnDay;
    public int minute = 0;
    public int hour = 6;
    public int day = 1;

    [Header("Time Speed")]
    [Tooltip("Durasi real-time untuk satu hari penuh (24 jam) dalam menit. Contoh: 15 = satu hari game berlangsung 15 menit nyata.")]
    [SerializeField, Min(0.1f)] float realMinutesPerFullDay = 15f;

    // Jam game maju per 10 menit. Satu hari penuh berisi 144 tick.
    const float GameTicksPerFullDay = 24f * 6f;
    float SecondsPerGameTick => Mathf.Max(0.01f, realMinutesPerFullDay * 60f / GameTicksPerFullDay);

    public float RealMinutesPerFullDay => realMinutesPerFullDay;

    float timer;
    readonly HashSet<object> pauseOwners = new();
    bool manualPause;

    public bool IsPaused => manualPause || pauseOwners.Count > 0;

    /// <summary>Jam desimal yang ikut bergerak halus di antara tick 10 menit.</summary>
    public float CurrentTimeHours
    {
        get
        {
            float tickProgress = Mathf.Clamp01(timer / SecondsPerGameTick);
            return Mathf.Repeat(hour + minute / 60f + tickProgress / 6f, 24f);
        }
    }

    public float DayProgress => CurrentTimeHours / 24f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (IsPaused)
            return;

        timer += Time.deltaTime;
        // While menjaga waktu tetap akurat apabila terjadi frame drop panjang.
        while (timer >= SecondsPerGameTick)
        {
            timer -= SecondsPerGameTick;
            AdvanceMinute();
        }
    }

    void AdvanceMinute()
    {
        minute += 10;
        OnMinute?.Invoke();

        if (minute >= 60)
        {
            minute = 0;
            hour++;
            OnHour?.Invoke();

            if (hour >= 24)
            {
                hour = 0;
                day++;
                OnDay?.Invoke();
            }
        }
    }

    /// <summary>Memajukan kalender satu hari dan mengatur waktu ke jam bangun.</summary>
    public void AdvanceToNextDay(int wakeHour = 6)
    {
        minute = 0;
        hour = Mathf.Clamp(wakeHour, 0, 23);
        day++;
        timer = 0f;
        OnMinute?.Invoke();
        OnHour?.Invoke();
        OnDay?.Invoke();
    }

    /// <summary>Menahan waktu untuk satu owner; waktu aktif kembali setelah seluruh owner melepas lock.</summary>
    public void AcquirePause(object owner)
    {
        if (owner != null)
            pauseOwners.Add(owner);
    }

    /// <summary>Melepas pause lock milik caller.</summary>
    public void ReleasePause(object owner)
    {
        if (owner != null)
            pauseOwners.Remove(owner);
    }

    public void SetPaused(bool paused) => manualPause = paused;

    void OnValidate()
    {
        realMinutesPerFullDay = Mathf.Max(0.1f, realMinutesPerFullDay);
    }
}
