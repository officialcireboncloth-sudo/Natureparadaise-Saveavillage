using UnityEngine;

/// <summary>
/// Menyediakan satu salinan TerrainData untuk seluruh sistem runtime pada Terrain yang sama.
/// Asset TerrainData sumber tidak pernah diubah oleh pohon, rumput, atau save/load saat Play.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Terrain))]
public sealed class TerrainRuntimeDataHost : MonoBehaviour
{
    Terrain targetTerrain;
    TerrainCollider terrainCollider;
    TerrainData sourceData;
    TerrainData sourceColliderData;
    TerrainData runtimeData;
    bool initialized;

    public TerrainData SourceData => sourceData;
    public TerrainData RuntimeData => runtimeData;

    void Awake() => EnsureInitialized();

    public bool EnsureInitialized()
    {
        if (initialized) return runtimeData != null;
        targetTerrain = GetComponent<Terrain>();
        if (targetTerrain == null || targetTerrain.terrainData == null) return false;

        sourceData = targetTerrain.terrainData;
        terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider != null) sourceColliderData = terrainCollider.terrainData;
        runtimeData = Instantiate(sourceData);
        runtimeData.name = sourceData.name + " (Nature Paradise Runtime)";
        targetTerrain.terrainData = runtimeData;
        if (terrainCollider != null) terrainCollider.terrainData = runtimeData;
        initialized = true;
        return true;
    }

    void OnEnable()
    {
        if (!EnsureInitialized()) return;
        targetTerrain.terrainData = runtimeData;
        if (terrainCollider != null) terrainCollider.terrainData = runtimeData;
    }

    void OnDisable()
    {
        if (!initialized) return;
        if (targetTerrain != null && targetTerrain.terrainData == runtimeData)
            targetTerrain.terrainData = sourceData;
        if (terrainCollider != null && terrainCollider.terrainData == runtimeData)
            terrainCollider.terrainData = sourceColliderData;
    }

    void OnDestroy()
    {
        if (runtimeData != null) Destroy(runtimeData);
    }
}
