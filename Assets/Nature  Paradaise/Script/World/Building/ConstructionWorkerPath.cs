using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>NavMesh when available, otherwise bounded ground A*. Never teleports across a blocked route.</summary>
public static class ConstructionWorkerPath
{
    const float Step = .9f, Radius = .26f;
    static readonly Collider[] overlaps = new Collider[48];
    static readonly RaycastHit[] hits = new RaycastHit[48];
    static bool Obstacle(Collider c, float groundY)
    {
        if (c == null || c.isTrigger || c is TerrainCollider || c.bounds.max.y < groundY + .18f) return false;
        if (c.GetComponentInParent<PlayerController>() != null || c.GetComponentInParent<AnimalController>() != null) return false;
        // Field base meshes and decorative plants are not walls. Tall solids remain obstacles.
        if (c.GetComponentInParent<FieldArea>() != null && c.bounds.size.y < .25f) return false;
        return true;
    }
    public static bool Ground(Vector3 point, out Vector3 ground)
    {
        ground = point;
        int count = Physics.RaycastNonAlloc(point + Vector3.up * 4f, Vector3.down, hits, 9f, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue; bool found = false;
        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            if (h.normal.y < .7f || h.collider.GetComponentInParent<PlayerController>() != null ||
                h.collider.GetComponentInParent<AnimalController>() != null) continue;
            // Prefer ground at the expected elevation, not a roof or the top of a seller.
            float difference = Mathf.Abs(h.point.y - point.y);
            if (difference < nearest && difference < 1.25f) { nearest = difference; ground = h.point; found = true; }
        }
        if (!found) return false;
        count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * .45f, ground + Vector3.up * 1.45f,
            Radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++) if (Obstacle(overlaps[i], ground.y)) return false;
        return true;
    }
    public static bool Segment(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from; float length = delta.magnitude;
        if (length < .001f) return true;
        int count = Physics.CapsuleCastNonAlloc(from + Vector3.up * .45f, from + Vector3.up * 1.45f,
            Radius, delta / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++) if (Obstacle(hits[i].collider, Mathf.Min(from.y, to.y))) return false;
        int steps = Mathf.CeilToInt(length / .45f); Vector3 previous = from;
        for (int i = 1; i <= steps; i++)
        {
            if (!Ground(Vector3.Lerp(from, to, (float)i / steps), out var next) || Mathf.Abs(next.y - previous.y) > .65f) return false;
            previous = next;
        }
        return true;
    }
    struct Entry { public Vector2Int key; public float score; public int sequence; }
    sealed class EntryComparer : IComparer<Entry>
    {
        public int Compare(Entry a, Entry b) { int score = a.score.CompareTo(b.score); return score != 0 ? score : a.sequence.CompareTo(b.sequence); }
    }
    public static List<Vector3> Find(Vector3 start, Vector3 goal)
    {
        if (!Ground(start, out start) || !Ground(goal, out goal)) return null;
        if (Segment(start, goal)) return new List<Vector3> { goal };
        if (NavMesh.SamplePosition(start, out var ns, 1f, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(goal, out var ng, 1f, NavMesh.AllAreas) &&
            (ns.position - start).sqrMagnitude < 1f && (ng.position - goal).sqrMagnitude < 1f)
        {
            var nav = new NavMeshPath();
            if (NavMesh.CalculatePath(ns.position, ng.position, NavMesh.AllAreas, nav) && nav.status == NavMeshPathStatus.PathComplete)
            {
                var result = new List<Vector3>(nav.corners); result.Add(goal); return result;
            }
        }
        Vector2Int first = Vector2Int.zero;
        var open = new SortedSet<Entry>(new EntryComparer());
        var cost = new Dictionary<Vector2Int, float> { [first] = 0 };
        var positions = new Dictionary<Vector2Int, Vector3> { [first] = start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>(); int sequence = 0;
        open.Add(new Entry { key = first, score = Vector3.Distance(start, goal), sequence = sequence++ });
        float maximumRange = Mathf.Min(220f, Vector3.Distance(start, goal) + 24f);
        for (int examined = 0; open.Count > 0 && examined < 7000; examined++)
        {
            Entry entry = open.Min; open.Remove(entry); var key = entry.key;
            if (!closed.Add(key)) continue;
            Vector3 point = positions[key];
            if ((point - goal).sqrMagnitude < Step * Step * 2 && Segment(point, goal))
            {
                var path = new List<Vector3> { goal, point };
                while (cameFrom.TryGetValue(key, out var previous)) { key = previous; path.Add(positions[key]); }
                path.Reverse(); return path;
            }
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                Vector2Int nextKey = key + new Vector2Int(x, z);
                if (closed.Contains(nextKey) || nextKey.sqrMagnitude * Step * Step > maximumRange * maximumRange) continue;
                Vector3 next = new(start.x + nextKey.x * Step, point.y, start.z + nextKey.y * Step);
                if (!Ground(next, out next) || !Segment(point, next)) continue;
                float nextCost = cost[key] + Vector3.Distance(point, next);
                if (cost.TryGetValue(nextKey, out var oldCost) && oldCost <= nextCost) continue;
                cost[nextKey] = nextCost; positions[nextKey] = next; cameFrom[nextKey] = key;
                open.Add(new Entry { key = nextKey, score = nextCost + Vector3.Distance(next, goal), sequence = sequence++ });
            }
        }
        return null;
    }
}
