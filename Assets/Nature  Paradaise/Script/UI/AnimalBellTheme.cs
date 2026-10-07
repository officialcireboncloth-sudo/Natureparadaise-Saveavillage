using UnityEngine;
[CreateAssetMenu(menuName="Nature Paradise/UI/Animal Bell Theme")]
public sealed class AnimalBellTheme : ScriptableObject
{
    [Header("Optional artwork — empty until sprites are supplied")]
    public Sprite panel, bellIcon, rainIcon, snowIcon, fairWeatherIcon, infoIcon;
}
