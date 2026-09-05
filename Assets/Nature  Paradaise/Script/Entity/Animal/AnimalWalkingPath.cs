using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Gunakan NavMesh jika tersedia; fallback grid ground/collider tidak memerlukan bake.</summary>
public static class AnimalWalkingPath
{
    static readonly Collider[] Obstacles = new Collider[32];
    static readonly RaycastHit[] GroundHits = new RaycastHit[32];
    static int lastPathFrame = -1;
    public static List<Vector3> Find(Vector3 start, Vector3 goal, Transform animal)
    {
        // Sebar pencarian path berat: maksimal satu hewan per frame.
        if (lastPathFrame == Time.frameCount) return null;
        lastPathFrame = Time.frameCount;
        if (NavMesh.SamplePosition(start, out NavMeshHit from, 1f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(goal, out NavMeshHit to, 1f, NavMesh.AllAreas))
        {
            NavMeshPath path = new();
            if (NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                return new List<Vector3>(path.corners);
        }
        const float step = 0.85f;
        Vector2Int end = new(Mathf.RoundToInt((goal.x - start.x) / step), Mathf.RoundToInt((goal.z - start.z) / step));
        List<Vector2Int> open = new() { Vector2Int.zero };
        Dictionary<Vector2Int, int> costs = new() { [Vector2Int.zero] = 0 };
        Dictionary<Vector2Int, Vector2Int> parents = new();
        Dictionary<Vector2Int, Vector3> points = new() { [Vector2Int.zero] = start };
        HashSet<Vector2Int> closed = new();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int budget = 900;
        while (open.Count > 0 && budget-- > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (costs[open[i]] + Distance(open[i], end) < costs[open[best]] + Distance(open[best], end)) best = i;
            Vector2Int current = open[best]; open.RemoveAt(best);
            if (current == end)
            {
                List<Vector3> path = new();
                while (current != Vector2Int.zero) { path.Add(points[current]); current = parents[current]; }
                path.Reverse();
                return path;
            }
            closed.Add(current);
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (closed.Contains(next) || Mathf.Abs(next.x) > 35 || Mathf.Abs(next.y) > 35) continue;
                Vector3 candidate = start + new Vector3(next.x * step, 0, next.y * step);
                candidate.y = points[current].y;
                if (!Ground(candidate, animal, out Vector3 ground)) continue;
                int cost = costs[current] + 1;
                if (costs.TryGetValue(next, out int old) && old <= cost) continue;
                costs[next] = cost; parents[next] = current; points[next] = ground;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        return null;
    }
    static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    public static bool Ground(Vector3 point, Transform animal, out Vector3 ground)
    {
        ground = point;
        // Ray ground tidak boleh menganggap tubuh hewan sendiri sebagai lantai.
        int hits = Physics.RaycastNonAlloc(point + Vector3.up * 1.2f, Vector3.down, GroundHits, 2.5f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == GroundHits.Length) return false;
        RaycastHit hit = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < hits; i++)
        {
            if (GroundHits[i].transform.IsChildOf(animal) || GroundHits[i].distance >= nearest) continue;
            hit = GroundHits[i]; nearest = hit.distance;
        }
        if (float.IsPositiveInfinity(nearest) || hit.normal.y < 0.7f || Mathf.Abs(hit.point.y - point.y) > 0.65f) return false;
        ground = hit.point;
        Collider ownCollider = animal.GetComponent<Collider>();
        float radius = ownCollider != null ? Mathf.Clamp(Mathf.Max(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z), 0.25f, 0.75f) : 0.4f;
        int count = Physics.OverlapSphereNonAlloc(ground + Vector3.up * 0.8f, radius, Obstacles, ~0, QueryTriggerInteraction.Ignore);
        if (count == Obstacles.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider obstacle = Obstacles[i];
            if (obstacle == null || obstacle.transform.IsChildOf(animal) || obstacle.bounds.max.y <= ground.y + 0.12f) continue;
            return false;
        }
        return true;
    }
}
