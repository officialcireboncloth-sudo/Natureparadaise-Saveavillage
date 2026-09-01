using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
/// <summary>Menampilkan waktu dan hari aktif dari <see cref="TimeManager"/> pada TMP text.</summary>
public class ClockUI : MonoBehaviour
{
    TMP_Text txt;

    void Awake()                         // gunakan Awake agar txt siap lebih awal
    {
        txt = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (!TimeManager.Instance) return;        // TimeManager belum ada, keluar dulu

        var t = TimeManager.Instance;
        txt.text = $"{t.hour:00}:{t.minute:00}  Day {t.day}";
    }
}
