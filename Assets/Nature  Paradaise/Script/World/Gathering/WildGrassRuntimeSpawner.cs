using UnityEngine;

/// <summary>Menyediakan beberapa rumput liar test yang dapat disabit dan dimakan ternak.</summary>
public static class WildGrassRuntimeSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SpawnTestingGrass()
    {
        ItemSO fodder = AnimalCareCatalog.Load()?.fodder;
        foreach (FieldArea field in FieldArea.ActiveAreas)
        {
            if (field == null || field.transform.Find("WildGrass_Test_Runtime") != null) continue;
            Transform root = new GameObject("WildGrass_Test_Runtime").transform;
            root.SetParent(field.transform, false);
            int[,] points =
            {
                { 1, 1 }, { 3, 2 }, { 5, 1 },
                { 2, 5 }, { 6, 4 }, { 8, 7 }
            };
            for (int i = 0; i < points.GetLength(0); i++)
            {
                int x = Mathf.Clamp(points[i, 0], 0, field.Columns - 1);
                int z = Mathf.Clamp(points[i, 1], 0, field.Rows - 1);
                if (!field.CanHoe(x, z)) continue;
                GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                grass.name = $"WildGrass_{i + 1}";
                grass.transform.SetParent(root, true);
                grass.transform.position = field.GridToWorld(x, z) + Vector3.up * 0.42f;
                grass.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                Renderer renderer = grass.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.22f, 0.62f, 0.16f);
                WorldGatherable gatherable = grass.AddComponent<WorldGatherable>();
                gatherable.ConfigureRuntimeGrass($"{field.FieldId}:wild-grass:{x}:{z}", fodder);
            }
        }
    }
}
