using System;

/// <summary>One saved roll per harvest; tree and crop quantities deliberately use different rules.</summary>
public static class HarvestRules
{
    public static bool Succeeds(double roll,double probability)=>probability>0&&roll>=0&&roll<Math.Min(1,probability);
    public static int CropAmount(int baseAmount,int stars,double chance,double roll)
        =>checked(Math.Max(1,baseAmount)*(stars==5&&Succeeds(roll,chance)?2:1));
}
