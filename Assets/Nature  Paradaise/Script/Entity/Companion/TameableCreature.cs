using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TameableCreatureSaveData
{
    public string creatureId;
    public float health;
    public bool defeated;
    public bool ended;
}

/// <summary>Jembatan combat ke taming. Combat cukup memanggil ApplyDamage lalu player memilih Mercy atau End.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PersonalAnimal))]
public sealed class TameableCreature : MonoBehaviour
{
    static readonly List<TameableCreature> Registry = new();
    [SerializeField] string creatureId;
    [SerializeField, Min(1f)] float maxHealth = 100f;
    [SerializeField, Min(0f)] float currentHealth = 100f;
    [SerializeField] bool defeated;
    [SerializeField] bool ended;
    PersonalAnimal companion;
    readonly Dictionary<Renderer, bool> rendererStates = new();
    readonly Dictionary<Collider, bool> colliderStates = new();

    public float Health => currentHealth;
    public bool IsDefeated => defeated;
    public bool IsEnded => ended;

    void Awake()
    {
        companion = GetComponent<PersonalAnimal>();
        if (string.IsNullOrWhiteSpace(creatureId))
            creatureId = $"{gameObject.scene.name}/{gameObject.name}/{transform.position.x:0.##}/{transform.position.z:0.##}";
        if (!companion.IsTamed) companion.SetTamed(false);
        ApplyEndedVisual();
    }

    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() => Registry.Remove(this);

    void Update()
    {
        if (!defeated || ended) return;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null || !PlayerInteractionTarget.Contains(player.transform, transform)) return;
        WorldInteractionPrompt.Request(this, transform, "E: Pilih Mercy / End", Vector3.Distance(player.transform.position, transform.position), 1.2f);
        if (PlayerInteractionTarget.Press(player.transform, transform, KeyCode.E))
            CreatureFatePanel.Show(this, player);
    }

    public bool ApplyDamage(float amount)
    {
        if (amount <= 0f || defeated || ended || companion.IsTamed) return false;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth <= 0f)
        {
            defeated = true;
            QuestEventHub.Publish(QuestObjectiveType.Defeat, creatureId);
            SaveLoadFeedback.Instance?.ShowMessage($"{companion.DisplayName} kalah. Dekati lalu pilih Mercy atau End.");
        }
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Defeat Creature")]
    void DebugDefeatCreature()
    {
        if (ended || companion.IsTamed) return;
        currentHealth = 0f;
        defeated = true;
        SaveLoadFeedback.Instance?.ShowMessage($"{companion.DisplayName} kalah. Dekati lalu tekan E.");
    }
#endif

    public void ChooseMercy()
    {
        if (!defeated || ended) return;
        defeated = false;
        currentHealth = Mathf.Max(1f, maxHealth * 0.25f);
        companion.SetTamed(true);
        companion.SetCommand(CompanionCommand.Stay);
        QuestEventHub.Publish(QuestObjectiveType.Tame, creatureId);
        SaveLoadFeedback.Instance?.ShowMessage($"Mercy: {companion.DisplayName} sekarang menjadi companion.");
    }

    public void ChooseEnd()
    {
        if (!defeated || ended) return;
        ended = true;
        ApplyEndedVisual();
        SaveLoadFeedback.Instance?.ShowMessage($"End dipilih untuk {companion.DisplayName}.");
    }

    void ApplyEndedVisual()
    {
        if (!ended) return;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        { if (!rendererStates.ContainsKey(renderer)) rendererStates[renderer] = renderer.enabled; renderer.enabled = false; }
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        { if (!colliderStates.ContainsKey(collider)) colliderStates[collider] = collider.enabled; collider.enabled = false; }
    }

    public TameableCreatureSaveData Capture() => new()
    { creatureId = creatureId, health = currentHealth, defeated = defeated, ended = ended };

    public void Restore(TameableCreatureSaveData data)
    {
        if (data == null) return;
        currentHealth = Mathf.Clamp(data.health, 0f, maxHealth);
        defeated = data.defeated; ended = data.ended;
        if (!ended)
        {
            foreach (var pair in rendererStates) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in colliderStates) if (pair.Key != null) pair.Key.enabled = pair.Value;
            rendererStates.Clear(); colliderStates.Clear();
        }
        ApplyEndedVisual();
    }

    public static List<TameableCreatureSaveData> CaptureAll()
    {
        List<TameableCreatureSaveData> result = new();
        foreach (TameableCreature creature in Registry) if (creature != null) result.Add(creature.Capture());
        return result;
    }

    public static void RestoreAll(List<TameableCreatureSaveData> data)
    {
        if (data == null) return;
        foreach (TameableCreatureSaveData saved in data)
            foreach (TameableCreature creature in Registry)
                if (creature != null && creature.creatureId == saved.creatureId) { creature.Restore(saved); break; }
    }
}
