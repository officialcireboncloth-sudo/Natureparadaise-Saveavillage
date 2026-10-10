using UnityEngine;

[CreateAssetMenu(menuName="Nature Paradise/Gathering/Wild Grass Balance")]
public class WildGrassBalance : ScriptableObject
{
    [Min(1),Tooltip("Ketahanan gulma yang dibuat otomatis di field.")] public int durability=1;
    [Min(1),Tooltip("Jumlah minimum Grass sekali disabit.")] public int minimumAmount=1;
    [Min(1),Tooltip("Jumlah maksimum Grass sekali disabit.")] public int maximumAmount=1;
    [Range(0,1),Tooltip("Peluang drop Grass; 1 = selalu, 0.25 = 25%.")] public float chance=1;
    [Min(1),Tooltip("Minimum hari game sampai gulma muncul kembali.")] public int minimumRespawnDays=2;
    [Min(1),Tooltip("Maksimum hari game sampai gulma muncul kembali.")] public int maximumRespawnDays=4;
}
