using System;

/// <summary>Persistent care for one fruit cycle. Expiring fertilizer is independent of the harvest reset.</summary>
[Serializable]
public sealed class FruitTreeCare
{
    public int version;
    public int days,boostedDays,minimumSoil=100;
    public int fertilizerLevel,fertilizerDaysRemaining,lastSampleDay=-1;
    public float mutationRoll=-1;
    public bool mutationRollReady;
    public bool FertilizerActive=>fertilizerLevel>0&&fertilizerDaysRemaining>0;
    public FruitTreeCare Copy()=>(FruitTreeCare)MemberwiseClone();
    public bool ApplyFertilizer(int level,int duration)
    {
        if(FertilizerActive||level<1||duration<1)return false;
        fertilizerLevel=level;fertilizerDaysRemaining=duration;return true;
    }
    public void RecordDay(int soil,int day)
    {
        if(lastSampleDay==day)return;
        version=1;lastSampleDay=day;days++;
        if(FertilizerActive)boostedDays++;
        minimumSoil=Math.Min(minimumSoil,Math.Max(0,Math.Min(100,soil)));
    }
    public void FinishDay()
    {
        fertilizerDaysRemaining=Math.Max(0,fertilizerDaysRemaining-1);
        if(fertilizerDaysRemaining==0)fertilizerLevel=0;
    }
    public int Stars
    {
        get
        {
            int covered=Math.Min(Math.Max(0,boostedDays),Math.Max(0,days));
            if(version<=0||days<=0||covered==0)return 1;
            if(covered==days&&minimumSoil>=80)return 5;
            if((long)covered*100>=80L*days)return 4;
            return (long)covered*2>=days?3:2;
        }
    }
    public FruitTreeCare NextCycle()=>new()
    {fertilizerLevel=fertilizerLevel,fertilizerDaysRemaining=fertilizerDaysRemaining,lastSampleDay=lastSampleDay};
}
