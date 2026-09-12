using UnityEditor;
using UnityEngine;

/// <summary>Editor setup untuk memasang whistle/companion tanpa runtime bootstrap atau dummy.</summary>
public static class CompanionSetupUtility
{
    [MenuItem("Nature Paradise/Animals/Setup Animal Bell and Whistle",false,100)]
    static void SetupAnimalCalls()
    {
        SetupWhistle();
        SetupAnimalBell();
    }

    static void SetupWhistle()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null) { Debug.LogWarning("[COMPANION SETUP] PlayerController tidak ditemukan."); return; }
        PlayerWhistle whistle = player.GetComponent<PlayerWhistle>();
        if (whistle == null) whistle = Undo.AddComponent<PlayerWhistle>(player.gameObject);
        Selection.activeGameObject = player.gameObject;
        EditorUtility.SetDirty(player.gameObject);
        Debug.Log("[COMPANION SETUP] Player Whistle siap. Tekan V saat Play Mode.", player);
    }

    static void SetupAnimalBell()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null) { Debug.LogWarning("[ANIMAL BELL SETUP] PlayerController tidak ditemukan."); return; }
        PlayerAnimalBell bell = player.GetComponent<PlayerAnimalBell>();
        if (bell == null) bell = Undo.AddComponent<PlayerAnimalBell>(player.gameObject);
        Selection.activeGameObject = player.gameObject;
        EditorUtility.SetDirty(player.gameObject);
        Debug.Log("[ANIMAL BELL SETUP] Animal Bell siap. Tekan Y saat Play Mode.", player);
    }

    [MenuItem("GameObject/Nature Paradise/Make Personal Companion", false, 20)]
    static void MakeCompanion()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null) return;
        PersonalAnimal companion = target.GetComponent<PersonalAnimal>();
        if (companion == null) companion = Undo.AddComponent<PersonalAnimal>(target);
        Configure(companion, true);
        Debug.Log("[COMPANION SETUP] Object menjadi personal companion. Atur Species, Home, animasi, dan scale di Inspector.", target);
    }

    [MenuItem("GameObject/Nature Paradise/Make Tameable Creature", false, 21)]
    static void MakeTameable()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null) return;
        PersonalAnimal companion = target.GetComponent<PersonalAnimal>();
        if (companion == null) companion = Undo.AddComponent<PersonalAnimal>(target);
        Configure(companion, false);
        if (target.GetComponent<TameableCreature>() == null) Undo.AddComponent<TameableCreature>(target);
        EditorUtility.SetDirty(target);
        Debug.Log("[COMPANION SETUP] Object siap menerima ApplyDamage(), lalu pilihan Mercy / End.", target);
    }

    static void Configure(PersonalAnimal companion, bool tamed)
    {
        SerializedObject serialized = new(companion);
        SerializedProperty id = serialized.FindProperty("companionId");
        if (string.IsNullOrWhiteSpace(id.stringValue))
            id.stringValue = GlobalObjectId.GetGlobalObjectIdSlow(companion.gameObject).ToString();
        serialized.FindProperty("displayName").stringValue = companion.gameObject.name;
        serialized.FindProperty("tamed").boolValue = tamed;
        serialized.FindProperty("activeCompanion").boolValue = tamed;
        serialized.FindProperty("homePosition").vector3Value = companion.transform.position;
        serialized.FindProperty("homeScene").stringValue = companion.gameObject.scene.name;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(companion);
    }
}
