#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Membuat salinan prefab satu-LOD yang diterima oleh Terrain Paint Details.</summary>
public static class TerrainDetailPrefabConverter
{
    const string OutputFolder = "Assets/Nature  Paradaise/Prefabs/Vegetation/Terrain Details";

    [MenuItem("Assets/Nature Paradise/Create Terrain Detail Copy (LOD1)", false, 2100)]
    static void ConvertSelected()
    {
        GameObject source = Selection.activeObject as GameObject;
        if (source == null) return;
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null) return;

        try
        {
            LODGroup group = instance.GetComponentInChildren<LODGroup>(true);
            if (group == null)
            {
                Debug.LogWarning("[Terrain Detail] Prefab terpilih tidak memiliki LODGroup.", source);
                return;
            }

            LOD[] lods = group.GetLODs();
            if (lods.Length == 0 || lods[Mathf.Min(1, lods.Length - 1)].renderers.Length == 0)
            {
                Debug.LogWarning("[Terrain Detail] LOD yang dapat digunakan tidak ditemukan.", source);
                return;
            }

            int lodIndex = Mathf.Min(1, lods.Length - 1);
            HashSet<Renderer> keep = new(lods[lodIndex].renderers);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && !keep.Contains(renderers[i]))
                    Object.DestroyImmediate(renderers[i].gameObject);

            foreach (LODGroup lodGroup in instance.GetComponentsInChildren<LODGroup>(true))
                Object.DestroyImmediate(lodGroup);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(body);
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(animator);

            instance.name = source.name + "_TerrainDetail";
            string path = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + instance.name + ".prefab");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"[Terrain Detail] Dibuat dari {source.name}, memakai LOD{lodIndex}: {path}", saved);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [MenuItem("Assets/Nature Paradise/Create Terrain Detail Copy (LOD1)", true)]
    static bool ValidateConvertSelected()
    {
        GameObject source = Selection.activeObject as GameObject;
        return source != null && PrefabUtility.IsPartOfPrefabAsset(source) &&
               source.GetComponentInChildren<LODGroup>(true) != null;
    }
}
#endif
