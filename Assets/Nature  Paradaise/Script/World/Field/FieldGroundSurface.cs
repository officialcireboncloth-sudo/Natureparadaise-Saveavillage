using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Terrain-conforming ground geometry; only visuals change, never terrain/save data.</summary>
public static class FieldGroundSurface
{
    public static Terrain TerrainAt(Vector3 position)
    {
        foreach (var terrain in Terrain.activeTerrains)
        {
            Vector3 p = position - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (p.x >= 0 && p.z >= 0 && p.x <= size.x && p.z <= size.z) return terrain;
        }
        return null;
    }

    public static Vector3 GroundPosition(Vector3 position, float lift = 0)
    {
        var terrain = TerrainAt(position);
        if (terrain != null) position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        return position + Vector3.up * lift;
    }

    public static Mesh CreateMesh(Transform target, Vector2 size, bool conform, float lift, int subdivisions)
    {
        int nx = Mathf.Clamp(Mathf.CeilToInt(size.x / .5f), subdivisions, 128);
        int nz = Mathf.Clamp(Mathf.CeilToInt(size.y / .5f), subdivisions, 128);
        var vertices = new Vector3[(nx + 1) * (nz + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[nx * nz * 6];
        for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
        {
            int i = z * (nx + 1) + x;
            uv[i] = new Vector2(x / (float)nx, z / (float)nz);
            Vector3 local = new Vector3((uv[i].x - .5f) * size.x, 0, (uv[i].y - .5f) * size.y);
            Vector3 world = target.TransformPoint(local);
            world = conform ? GroundPosition(world, lift) : world + target.up * lift;
            vertices[i] = target.InverseTransformPoint(world);
        }
        int t = 0;
        for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
        {
            int i = z * (nx + 1) + x;
            triangles[t++] = i; triangles[t++] = i + nx + 1; triangles[t++] = i + 1;
            triangles[t++] = i + 1; triangles[t++] = i + nx + 1; triangles[t++] = i + nx + 2;
        }
        var mesh = new Mesh { name = "TerrainConformingSoil", vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    public static Material CreateMaterial(Material source, bool baseSurface, Vector3 position, FieldSoilVisualProfile profile = null)
    {
        var shader = Shader.Find("Nature Paradise/Soil State");
        if (shader == null) return null;
        var material = new Material(shader) { name = baseSurface ? "FieldDirt_Blend_Runtime" : "HoedSoil_Blend_Runtime", enableInstancing = true, renderQueue = baseSurface ? 3000 : 3001 };
        if (source != null && source.HasProperty("_BaseColor")) material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        if (!baseSurface) material.SetColor("_BaseColor", new Color(.48f, .29f, .13f));
        material.SetFloat("_Furrows", baseSurface ? 0 : .14f);
        if (profile == null) profile = Resources.Load<FieldSoilVisualProfile>("Profiles/Field Soil Visuals");
        if (profile != null) profile.Apply(material, baseSurface);
        return material;
    }

    public static void ApplyEdges(Renderer renderer, Vector2 size, float feather, float radius, Vector2 textureOffset = default)
    {
        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
        block.SetVector("_SurfaceSize", new Vector4(size.x, size.y, 0, 0));
        block.SetVector("_GroundUVOffset", new Vector4(textureOffset.x, textureOffset.y, 0, 0));
        block.SetFloat("_EdgeWidth", feather); block.SetFloat("_CornerRadius", radius);
        renderer.SetPropertyBlock(block);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
}
