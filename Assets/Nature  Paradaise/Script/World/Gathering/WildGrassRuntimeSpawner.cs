using UnityEngine;

/// <summary>Menyediakan beberapa rumput liar test yang dapat disabit dan dimakan ternak.</summary>
public static class WildGrassRuntimeSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SpawnTestingGrass()
    {
        ItemSO grassItem = Resources.Load<ItemSO>("Items/Materials/Grass");
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
                GameObject prefab = field.WildGrassPrefab != null ? field.WildGrassPrefab : Resources.Load<GameObject>("World/Wild Grass");
                if (prefab == null) continue;
                GameObject grass = new GameObject($"WildGrass_{i + 1}", typeof(BoxCollider));
                grass.name = $"WildGrass_{i + 1}";
                grass.transform.SetParent(root, true);
                grass.transform.position = FieldGroundSurface.GroundPosition(field.GridToWorld(x, z), .01f);
                GameObject visual = Object.Instantiate(prefab, grass.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0, i * 137.5f, 0);
                var renderers = visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float diameter = Mathf.Max(.01f, Mathf.Max(bounds.size.x, bounds.size.z));
                    visual.transform.localScale *= field.CellSize * (.50f + i % 3 * .055f) / diameter;
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    visual.transform.position += Vector3.up * (grass.transform.position.y - bounds.min.y);
                    var collider = grass.GetComponent<BoxCollider>();
                    collider.center = grass.transform.InverseTransformPoint(bounds.center + Vector3.up * (grass.transform.position.y - bounds.min.y));
                    collider.size = new Vector3(bounds.size.x, Mathf.Max(.35f, bounds.size.y), bounds.size.z);
                }
                WorldGatherable gatherable = grass.AddComponent<WorldGatherable>();
                gatherable.ConfigureRuntimeGrass($"{field.FieldId}:wild-grass:{x}:{z}", grassItem);
            }
        }
    }
}
