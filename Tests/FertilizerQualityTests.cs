using System;
public sealed class CropDataSO { public float TotalGrowthDays = 7; }
class FertilizerQualityTests
{
    static int checks;
    static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
    static CropQualityCare Grow(int days,int missingFertilizer=0,int missingWater=0,int soil=80)
    {
        var care=new CropQualityCare();
        for(int day=1;day<=days;day++)
        {
            if(day!=missingFertilizer)care.ApplyFertilizer(1,1);
            care.RecordDay(day!=missingWater,soil,days,day);care.FinishDay();
        }
        return care;
    }
    static void Main()
    {
        foreach(int duration in new[]{1,3,5,7,14,28,365})
        {
            var perfect=Grow(duration);Check(perfect.Stars==5&&perfect.QualityPercent==100,"Exact perfect crop "+duration);
            var missed=Grow(duration,1);Check(missed.Stars<5,"Missing fertilizer can never yield five stars");
            Check(Math.Abs(missed.QualityPercent-(100d-40d/duration))<1e-10,"Missed fertilizer exact contribution");
            var dry=Grow(duration,0,1);Check(dry.Stars<5,"Missing water can never yield five stars");
        }
        var example=Grow(7,4);Check(Math.Abs(example.QualityPercent-94.28571428571429)<1e-10&&example.Stars==4,"User seven-day example");
        var soilCare=Grow(7,0,0,40);Check(soilCare.QualityPercent==80&&soilCare.Stars==4,"Half fertility gives 20 soil points");
        var durationCare=new CropQualityCare();Check(durationCare.ApplyFertilizer(3,3),"Apply Super");
        Check(!durationCare.ApplyFertilizer(4,7),"No overwrite or double consumption while active");
        for(int d=1;d<=3;d++){Check(durationCare.FertilizerActive,"Super active day "+d);durationCare.RecordDay(true,80,7,d);durationCare.FinishDay();}
        Check(!durationCare.FertilizerActive&&durationCare.boostedDays==3,"Super expires after exactly three daily samples");
        foreach(var effect in new[]{new[]{3,3,7,3},new[]{4,7,7,1},new[]{4,7,9,2}})
        {
            var continuous=new CropQualityCare();int applications=0;
            for(int day=1;day<=effect[2];day++)
            {
                if(!continuous.FertilizerActive)
                {Check(continuous.ApplyFertilizer(effect[0],effect[1]),"Renew expired multi-day effect");applications++;}
                continuous.RecordDay(true,80,effect[2],day);continuous.FinishDay();
            }
            Check(continuous.boostedDays==effect[2]&&continuous.Stars==5&&continuous.QualityPercent==100,
                "Active multi-day effect covers every growth day: level "+effect[0]+", days "+effect[2]);
            Check(applications==effect[3],"Only reapply after expiration: level "+effect[0]+", days "+effect[2]);
        }
        var gap=new CropQualityCare();
        for(int day=1;day<=7;day++)
        {
            if(day==1||day==5)gap.ApplyFertilizer(3,3);
            gap.RecordDay(true,80,7,day);gap.FinishDay();
        }
        Check(gap.boostedDays==6&&gap.Stars==4&&Math.Abs(gap.QualityPercent-94.28571428571429)<1e-10,
            "A day without active Super prevents five stars even after reapplication");
        var activeSave=new CropQualityCare();activeSave.ApplyFertilizer(4,7);
        activeSave.RecordDay(true,80,7,1);activeSave.FinishDay();var restoredActive=activeSave.Copy();
        for(int day=2;day<=7;day++){restoredActive.RecordDay(true,80,7,day);restoredActive.FinishDay();}
        Check(restoredActive.Stars==5&&restoredActive.boostedDays==7&&!restoredActive.FertilizerActive,
            "Restored active Premium retains its remaining coverage and exact expiration");
        var copy=durationCare.Copy();copy.RecordDay(true,80,7,4);Check(durationCare.days==3,"Independent save copy");
        copy.RecordDay(true,80,7,4);Check(copy.days==4,"Duplicate calendar sample ignored");
        var delayed=Grow(7,1);delayed.ApplyFertilizer(1,1);delayed.RecordDay(true,80,7,8);delayed.FinishDay();
        Check(delayed.Stars==4&&delayed.QualityPercent<100,"Extra growth day cannot erase missed care");
        foreach(int hundredths in new[]{0,2000,2100,3999,4000,5999,6000,7999,8000,9999,10000})
        {
            var care=new CropQualityCare{qualityVersion=1,targetGrowthDays=100,days=100};
            long remaining=hundredths*80L;
            care.boostedDays=(int)Math.Min(100,remaining/3200);remaining-=care.boostedDays*3200L;
            care.wateredDays=(int)Math.Min(100,remaining/1600);remaining-=care.wateredDays*1600L;
            care.soilFertilityTotal=remaining/40;
            int expected=hundredths<4000?1:hundredths<6000?2:hundredths<8000?3:hundredths<10000?4:5;
            Check(care.Stars==expected,"Star boundary "+hundredths/100d);
        }
        var legacy=new CropQualityCare{days=7,wateredDays=7,boostedDays=7};
        Check(legacy.RollStars(new CropDataSO(),true,8)<5,"Old saves without soil history cannot invent perfect quality");
        Console.WriteLine("FERTILIZER_QUALITY_PASS checks="+checks);
    }
}
