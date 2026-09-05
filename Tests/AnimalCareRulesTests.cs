using System;
public enum AnimalHousingKind { None, Barn, Coop }
class AnimalCareRulesTests
{
    static int checks;
    static void Check(bool value,string name) { checks++;if(!value)throw new Exception(name); }
    static void Main()
    {
        foreach(bool bird in new[]{false,true})
            foreach(AnimalHousingKind kind in Enum.GetValues(typeof(AnimalHousingKind)))
                for(int capacity=0;capacity<=8;capacity++)
                    for(int residents=0;residents<=10;residents++)
                        Check(AnimalCareRules.Fits(bird,kind,residents,capacity)==
                            (kind==(bird?AnimalHousingKind.Coop:AnimalHousingKind.Barn)&&residents<capacity),"housing species and capacity");
        for(int hour=0;hour<24;hour++)
        {
            Check(AnimalCareRules.CanTurnOut(true,true,true,hour,true,true)==(hour>=6&&hour<18),"daylight schedule");
            Check(!AnimalCareRules.CanTurnOut(true,true,false,hour,true,true),"rain/snow/storm blocks turnout");
            Check(!AnimalCareRules.CanTurnOut(true,false,true,hour,true,true),"sick cannot turnout");
            Check(!AnimalCareRules.CanTurnOut(false,true,true,hour,true,true),"egg stays inside");
            Check(!AnimalCareRules.CanTurnOut(true,true,true,hour,false,true),"unassigned cannot turnout");
        }
        int stock=2;
        Check(AnimalCareRules.ConsumeFeed(ref stock,true,false)&&stock==1,"one portion consumed");
        Check(!AnimalCareRules.ConsumeFeed(ref stock,true,true)&&stock==1,"already fed does not consume");
        Check(!AnimalCareRules.ConsumeFeed(ref stock,false,false)&&stock==1,"egg does not consume");
        Check(AnimalCareRules.ConsumeFeed(ref stock,true,false)&&stock==0,"second animal consumes final portion");
        Check(!AnimalCareRules.ConsumeFeed(ref stock,true,false)&&stock==0,"empty trough no free feed");
        Console.WriteLine(checks+" animal care rule assertions passed (pure logic, not Play Mode).");
    }
}
