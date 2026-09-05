using System;
class AnimalHeartTests
{
    static int count;
    static void Check(bool value,string message) {count++;if(!value)throw new Exception(message);}
    static void Main()
    {
        var rules=new AnimalHeartRules();
        var heart=new AnimalHeartState();
        Check(heart.RewardOnce(ref heart.lastPetDay,1,rules.petPoints),"first pet");
        Check(!heart.RewardOnce(ref heart.lastPetDay,1,rules.petPoints)&&heart.points==8,"spam pet blocked");
        Check(heart.RewardOnce(ref heart.lastPetDay,2,rules.petPoints),"next day pet");
        foreach(int points in new[]{0,99,100,299,300,999,1000})
        {heart.points=points;Check(heart.Level==points/100,"heart bands");}
        heart.Add(5000);Check(heart.points==1000,"upper bound");
        heart.Add(-5000);Check(heart.points==0,"lower bound");
        heart.points=700;
        heart.EndDay(rules,1,false,false,true,false,false,false);
        Check(heart.points==700,"one missed feed: grace");
        heart.EndDay(rules,2,false,false,true,false,false,false);
        Check(heart.points==692,"repeated hunger: small loss");
        heart.EndDay(rules,2,false,false,true,true,true,true);
        Check(heart.points==692,"duplicate reset ignored");
        var copy=heart.Copy();copy.Add(10);
        Check(heart.points==692,"snapshot independent");
        var loaded=heart.Copy();
        loaded.lastTreatDay=5;
        var restored=loaded.Copy();
        Check(!restored.RewardOnce(ref restored.lastTreatDay,5,10),"save retains treat day");
        var low=new AnimalHeartState();var high=new AnimalHeartState {points=1000};
        int lowTotal=0,highTotal=0;
        for(int i=0;i<10000;i++)
        {
            double roll=i/10000d;
            int grade=high.RollQuality(100,roll);
            Check(grade>=1&&grade<=5,"quality bounds");
            lowTotal+=low.RollQuality(100,roll);highTotal+=grade;
        }
        Check(highTotal>lowTotal,"heart raises expected quality");
        Check(high.RollQuality(100,0)==1,"max hearts do not guarantee high grade");
        Check(high.RollQuality(0,.99)==1,"bad mood reduces grade");
        var storm=new AnimalHeartState {points=700};
        storm.EndDay(rules,1,false,false,false,true,true,true);
        Check(storm.points==690,"storm small penalty without stacking rain");
        var daily=new AnimalHeartState();daily.grazingDay=1;
        daily.EndDay(rules,1,true,true,true,false,false,false);
        Check(daily.points==rules.healthyDayPoints+rules.grazingPoints,"healthy and grazing care");
        Console.WriteLine(count+" animal heart assertions passed (pure logic, not Play Mode).");
    }
}
