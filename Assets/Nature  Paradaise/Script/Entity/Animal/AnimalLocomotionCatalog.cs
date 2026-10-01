using System;
using UnityEngine;

/// <summary>Animation pairs belong to a rig, including the separate baby animal rigs.</summary>
[CreateAssetMenu(menuName = "Nature Paradise/Animal/Locomotion Catalog")]
public sealed class AnimalLocomotionCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Rig
    {
        public string controllerPrefix;
        public string modelToken;
        public RuntimeAnimatorController idle;
        public RuntimeAnimatorController walk;
        public RuntimeAnimatorController eat;
    }
    public Rig[] rigs;
    public static AnimalLocomotionCatalog Load() => Resources.Load<AnimalLocomotionCatalog>("Catalogs/AnimalLocomotionCatalog");
    public Rig Find(RuntimeAnimatorController controller)
    {
        if (controller == null || rigs == null) return null;
        foreach (Rig rig in rigs)
            if (rig != null && !string.IsNullOrEmpty(rig.controllerPrefix) &&
                controller.name.StartsWith(rig.controllerPrefix, StringComparison.Ordinal)) return rig;
        return null;
    }
    public Rig Find(Animator animator)
    {
        if (animator == null) return null;
        Rig byController = Find(animator.runtimeAnimatorController);
        if (byController != null) return byController;
        if (rigs == null) return null;
        foreach (Rig rig in rigs)
        {
            if (rig == null || string.IsNullOrEmpty(rig.modelToken)) continue;
            if (animator.name.Replace(' ', '_').Contains(rig.modelToken) ||
                (animator.avatar != null && animator.avatar.name.Replace(' ', '_').Contains(rig.modelToken))) return rig;
        }
        return null;
    }
}
