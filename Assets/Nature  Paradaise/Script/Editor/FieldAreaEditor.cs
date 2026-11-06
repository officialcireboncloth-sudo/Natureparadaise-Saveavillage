#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FieldArea)), CanEditMultipleObjects]
public sealed class FieldAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (targets.Length != 1) return;
        var field = (FieldArea)target;
        if (!field.UsesFieldSoil)
        {
            EditorGUILayout.HelpBox("Field Number = 0: soil tile legacy. Untuk scanner, isi ID unik 1–4, lalu duplicate instance dan ubah ID-nya.", MessageType.Info);
            return;
        }
        EditorGUILayout.LabelField("Scanner", field.SoilDisplayName);
        EditorGUILayout.LabelField("Save ID", field.FieldId);
        foreach (var other in Object.FindObjectsByType<FieldArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other == field || other.gameObject.scene != field.gameObject.scene) continue;
            if (field.FieldId == other.FieldId || other.UsesFieldSoil && other.FieldNumber == field.FieldNumber)
            {
                EditorGUILayout.HelpBox("ID kembar dengan " + other.name + ". Ganti Field Number setelah duplicate. Save/load dan penyuburan ditolak sampai ID unik.", MessageType.Error);
                break;
            }
        }
        if (Application.isPlaying)
        {
            var soil = field.SoilSnapshot;
            EditorGUILayout.LabelField("Kesuburan", $"{soil.Fertility} / 80 · {soil.ConditionName}");
            EditorGUILayout.LabelField("Kontribusi kualitas", soil.QualityContributionPercent + "%");
            EditorGUILayout.LabelField("Status", soil.IsRestoring ? $"Dalam Penyuburan · {soil.RemainingDays} hari" : "Aktif");
        }
    }
}
#endif
