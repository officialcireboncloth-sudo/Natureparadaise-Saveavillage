#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed partial class TerrainLayerVegetation
{
    void UpdatePaintInEditor()
    {
        if (!Application.IsPlaying(gameObject) && profile != null && profile.trees?.terrainLayer != null)
            ProcessPendingPaint();
    }

    void RefreshAfterUndo()
    {
        if (Application.IsPlaying(gameObject) || profile == null || profile.trees?.terrainLayer == null || terrain.terrainData == null)
            return;
        var data = terrain.terrainData;
        needsPaintSync = true;
        QueueRegion(new RectInt(0, 0, data.alphamapWidth, data.alphamapHeight), data.alphamapWidth, data.alphamapHeight);
    }

    void RebuildPaintedTrees(RectInt region)
    {
        if (Application.IsPlaying(gameObject) || profile == null || region.width <= 0 || region.height <= 0)
            return;
        var rule = profile.trees;
        if (rule == null || rule.terrainLayer == null || rule.prefabs == null || rule.prefabs.Length == 0)
            return;
        var data = terrain.terrainData;
        int layer = Array.IndexOf(data.terrainLayers, rule.terrainLayer);
        if (layer < 0 && paintedTrees.Count == 0)
            return;

        int width = data.detailWidth;
        int height = data.detailHeight;
        var obsolete = new HashSet<(GameObject, Vector2Int)>();
        var retained = new List<PaintedTree>(paintedTrees.Count);
        foreach (var tree in paintedTrees)
        {
            var key = (tree.prefab, tree.position);
            var cell = DetailCell(tree.position.x / 1000000f, tree.position.y / 1000000f, width, height);
            if (region.Contains(cell)) obsolete.Add(key);
            else retained.Add(tree);
        }
        int ownedOutsideRegion = retained.Count;

        var prototypes = new List<TreePrototype>(data.treePrototypes);
        var instances = new List<TreeInstance>();
        var occupied = new HashSet<Vector2Int>();
        foreach (var tree in data.treeInstances)
        {
            var key = PositionKey(tree.position.x, tree.position.z);
            if (tree.prototypeIndex >= 0 && tree.prototypeIndex < prototypes.Count &&
                obsolete.Remove((prototypes[tree.prototypeIndex].prefab, key)))
                continue;
            instances.Add(tree);
            occupied.Add(key);
        }
        int previousPrototypeCount = prototypes.Count;
        if (layer >= 0)
        {
            var exclusions = FindObjectsByType<TerrainVegetationExclusion>(FindObjectsSortMode.None);
            var fields = FindObjectsByType<FieldArea>(FindObjectsSortMode.None);
            float spacing = Mathf.Max(2, rule.spacing);
            int alphaWidth = data.alphamapWidth, alphaHeight = data.alphamapHeight;
            int x0 = Mathf.FloorToInt(region.xMin * (float)alphaWidth / width);
            int z0 = Mathf.FloorToInt(region.yMin * (float)alphaHeight / height);
            int x1 = Mathf.Min(alphaWidth, Mathf.CeilToInt(region.xMax * (float)alphaWidth / width));
            int z1 = Mathf.Min(alphaHeight, Mathf.CeilToInt(region.yMax * (float)alphaHeight / height));
            var paint = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
            int firstX = Mathf.Max(0, Mathf.FloorToInt(region.xMin * data.size.x / width / spacing));
            int firstZ = Mathf.Max(0, Mathf.FloorToInt(region.yMin * data.size.z / height / spacing));
            int lastX = Mathf.CeilToInt(region.xMax * data.size.x / width / spacing);
            int lastZ = Mathf.CeilToInt(region.yMax * data.size.z / height / spacing);
            for (int z = firstZ; z < lastZ; z++)
            for (int x = firstX; x < lastX; x++)
            {
                float u = (x + .17f + .66f * Random01(x, z, profile.seed + 41)) * spacing / data.size.x;
                float v = (z + .17f + .66f * Random01(x, z, profile.seed + 43)) * spacing / data.size.z;
                if (u >= 1 || v >= 1 || !region.Contains(DetailCell(u, v, width, height))) continue;
                int px = Mathf.Clamp(Mathf.FloorToInt(u * alphaWidth), x0, x1 - 1) - x0;
                int pz = Mathf.Clamp(Mathf.FloorToInt(v * alphaHeight), z0, z1 - 1) - z0;
                if (paint[pz, px, layer] < rule.minimumPaintWeight || data.GetSteepness(u, v) > rule.maximumSlope ||
                    data.IsHole(Mathf.FloorToInt(u * data.holesResolution), Mathf.FloorToInt(v * data.holesResolution))) continue;
                float elevation = data.GetInterpolatedHeight(u, v);
                var point = terrain.transform.position + new Vector3(u * data.size.x, elevation, v * data.size.z);
                if (elevation < rule.minimumTerrainHeight || Blocked(point, exclusions, fields)) continue;
                var prefab = rule.prefabs[Mathf.FloorToInt(Random01(x, z, profile.seed + 47) * rule.prefabs.Length)];
                var key = PositionKey(u, v);
                if (prefab == null || !occupied.Add(key)) continue;
                int index = prototypes.FindIndex(p => p.prefab == prefab);
                if (index < 0)
                {
                    index = prototypes.Count;
                    prototypes.Add(new TreePrototype { prefab = prefab });
                }
                float scale = Mathf.Lerp(.72f, 1.04f, Random01(x, z, profile.seed + 53));
                instances.Add(new TreeInstance
                {
                    position = new Vector3(u, elevation / data.size.y, v), prototypeIndex = index,
                    rotation = Random01(x, z, profile.seed + 59) * Mathf.PI * 2,
                    widthScale = scale, heightScale = scale, color = Color.white, lightmapColor = Color.white
                });
                retained.Add(new PaintedTree { prefab = prefab, position = key });
            }
        }
        if (retained.Count == ownedOutsideRegion && paintedTrees.Count == ownedOutsideRegion) return;
        if (prototypes.Count != previousPrototypeCount) data.treePrototypes = prototypes.ToArray();
        data.SetTreeInstances(instances.ToArray(), false);
        paintedTrees = retained;
        terrain.Flush();
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    static Vector2Int DetailCell(float u, float v, int w, int h) => new(Mathf.FloorToInt(u * w), Mathf.FloorToInt(v * h));
    static Vector2Int PositionKey(float u, float v) => new(Mathf.RoundToInt(u * 1000000), Mathf.RoundToInt(v * 1000000));
}
#endif
