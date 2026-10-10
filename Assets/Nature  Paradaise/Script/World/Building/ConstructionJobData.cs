using System;
using UnityEngine;

public enum ConstructionWorkerPhase { Travelling, Working, Returning, Finished }

[Serializable]
public sealed class ConstructionJobData
{
    public int version = 1;
    public ConstructionWorkerPhase phase;
    public Vector3 departurePosition, workerPosition, workPosition;
    public Quaternion workerRotation = Quaternion.identity;
    public double lastClockHours;
    public double completedWorkHours;
    public double requiredWorkHours;
    public int workStarts = 8, workEnds = 17;
    public bool pathBlocked;
    public float Progress => requiredWorkHours <= 0 ? 1 : Mathf.Clamp01((float)(completedWorkHours / requiredWorkHours));

    public static double ClockHours => TimeManager.Instance != null
        ? (TimeManager.Instance.day - 1) * 24d + TimeManager.Instance.CurrentTimeHours : 8d;

    // Integrates only the work window. Also handles sleeping, many elapsed days and loading indoors.
    public static double WorkBetween(double from, double to, int start, int end)
    {
        if (to <= from) return 0;
        start = Mathf.Clamp(start, 0, 23); end = Mathf.Clamp(end, start + 1, 24);
        return WorkBefore(to, start, end) - WorkBefore(from, start, end);
    }
    static double WorkBefore(double time, int start, int end)
    {
        double day = Math.Floor(time / 24d), hour = time - day * 24d;
        return day * (end - start) + Math.Max(0, Math.Min(end - start, hour - start));
    }
    public void CatchUp(double now)
    {
        if (phase == ConstructionWorkerPhase.Working)
            completedWorkHours = Math.Min(requiredWorkHours,
                completedWorkHours + WorkBetween(lastClockHours, now, workStarts, workEnds));
        lastClockHours = now;
    }
    public ConstructionJobData Copy() => (ConstructionJobData)MemberwiseClone();
}
