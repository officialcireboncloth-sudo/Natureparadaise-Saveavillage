using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Marker dunia modular untuk NPC, quest, shop, dan lokasi yang bergerak.</summary>
[DisallowMultipleComponent]
public sealed class WorldMapMarker : MonoBehaviour
{
    static readonly HashSet<WorldMapMarker> ActiveMarkers = new();

    [SerializeField] string markerId = "marker.new";
    [SerializeField] string displayName = "Marker";
    [SerializeField] MapMarkerCategory category;
    [SerializeField] Sprite icon;
    [SerializeField] bool unlocked = true;
    [SerializeField] bool hideWhenObjectInactive = true;
    [SerializeField] string questId;

    public string Id => string.IsNullOrWhiteSpace(markerId) ? name : markerId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public MapMarkerCategory Category => ResolveQuestCategory();
    public Sprite Icon => icon;
    public Vector3 WorldPosition => transform.position;
    public bool IsVisible => unlocked && (!hideWhenObjectInactive || gameObject.activeInHierarchy);
    public static IEnumerable<WorldMapMarker> All => ActiveMarkers;

    void OnEnable()
    {
        ActiveMarkers.Add(this);
        WorldMapService.Instance?.NotifyMarkerListChanged();
    }

    void OnDisable()
    {
        ActiveMarkers.Remove(this);
        WorldMapService.Instance?.NotifyMarkerListChanged();
    }

    public void SetUnlocked(bool value)
    {
        if (unlocked == value) return;
        unlocked = value;
        WorldMapService.Instance?.NotifyMarkerListChanged();
    }

    MapMarkerCategory ResolveQuestCategory()
    {
        if (string.IsNullOrWhiteSpace(questId) || QuestService.Instance == null)
            return category;
        foreach (QuestDefinitionSO quest in QuestService.Instance.Definitions)
        {
            if (!string.Equals(quest.Id, questId, StringComparison.OrdinalIgnoreCase)) continue;
            QuestStatus status = QuestService.Instance.GetStatus(quest);
            if (status == QuestStatus.Available) return MapMarkerCategory.QuestAvailable;
            if (status == QuestStatus.Active || status == QuestStatus.ReadyToTurnIn)
                return MapMarkerCategory.QuestTarget;
            return category;
        }
        return category;
    }
}
