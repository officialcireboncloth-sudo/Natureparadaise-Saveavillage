using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarUI : DataDrivenModal
{
    public GameScheduleData data;
    public Sprite background,calendarIcon;
    public int viewedYear=1,viewedSeason,selectedDay=1,selectedEvent;
    protected override string Prompt=>"E: Lihat Kalender";
    protected override void BindControls()
    {
        // Older authored calendars have 28 cards. Upgrade only their UI, preserving interaction/data bindings.
        if(Find<Button>("Day_31")==null)
        {
            var old=uiRoot;old.SetActive(false);Build();
            if(Application.isPlaying){Destroy(old);uiRoot.transform.SetParent(null,false);uiRoot.transform.localRotation=Quaternion.identity;uiRoot.transform.localScale=Vector3.one;}
            else DestroyImmediate(old);
        }
        Bind("PreviousSeason",()=>ChangeSeason(-1));Bind("NextSeason",()=>ChangeSeason(1));Bind("NextEvent",NextEvent);Bind("Close",Close);
        for(int i=1;i<=GameScheduleData.MaxDaysPerSeason;i++){int day=i;Bind("Day_"+i,()=>SelectDay(day));}
    }
    protected override void OnOpen(){data??=GameScheduleData.Load();GameScheduleData.Date(TimeManager.Instance!=null?TimeManager.Instance.day:1,out viewedYear,out viewedSeason,out selectedDay);selectedEvent=0;}
    protected override void Update()
    {
        bool wasOpen=IsOpen;base.Update();if(!wasOpen||!IsOpen)return;
        if(Input.GetKeyDown(KeyCode.LeftArrow))ChangeSeason(-1);if(Input.GetKeyDown(KeyCode.RightArrow))ChangeSeason(1);
        int step=Input.GetKeyDown(KeyCode.A)?-1:Input.GetKeyDown(KeyCode.D)?1:Input.GetKeyDown(KeyCode.W)?-7:Input.GetKeyDown(KeyCode.S)?7:0;
        if(step!=0)SelectDay(selectedDay+step);if(Input.GetKeyDown(KeyCode.Return))NextEvent();
    }
    public void ChangeSeason(int step){int season=Mathf.Max(0,(viewedYear-1)*4+viewedSeason+step);viewedYear=season/4+1;viewedSeason=season%4;selectedDay=Mathf.Clamp(selectedDay,1,GameScheduleData.DaysInSeason(viewedSeason));selectedEvent=0;Refresh();}
    public void SelectDay(int day){selectedDay=Mathf.Clamp(day,1,GameScheduleData.DaysInSeason(viewedSeason));selectedEvent=0;Refresh();}
    public void NextEvent(){selectedEvent++;Refresh();}
    protected override void Refresh()
    {
        if(uiRoot==null)return;data??=GameScheduleData.Load();
        int length=GameScheduleData.DaysInSeason(viewedSeason);
        Find<TMP_Text>("Season").text=$"{GameScheduleData.SeasonEnglishName(viewedSeason).ToUpper()} · {GameScheduleData.SeasonName(viewedSeason).ToUpper()}\nBULAN {viewedSeason+1} · TAHUN {viewedYear}";
        Paint(Find<Image>("BackgroundImageSlot"),background);Paint(Find<Image>("CalendarIconSlot"),calendarIcon);
        int today=TimeManager.Instance!=null?TimeManager.Instance.day:1;
        int firstWeekday=(GameScheduleData.AbsoluteDay(viewedYear,viewedSeason,1)-1)%7;
        int rows=Mathf.CeilToInt((firstWeekday+length)/7f);float rowHeight=.405f/rows;
        for(int i=1;i<=GameScheduleData.MaxDaysPerSeason;i++)
        {
            var b=Find<Button>("Day_"+i);b.gameObject.SetActive(i<=length);if(i>length)continue;
            int index=firstWeekday+i-1,col=index%7,row=index/7;float x=.03f+col*.134f,y=.705f-(row+1)*rowHeight;
            var rect=(RectTransform)b.transform;rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(x+.130f,y+rowHeight-.008f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var events=data!=null?data.Events(viewedYear,viewedSeason,i).ToList():new();
            bool current=GameScheduleData.AbsoluteDay(viewedYear,viewedSeason,i)==today;
            Selected(b,i==selectedDay);var label=b.GetComponentInChildren<TMP_Text>();
            label.text=$"{i}"+(current?"  • Hari ini":"")+"\n"+(events.Count>0?events[0].title+(events.Count>1?" +":""):i==length?"Akhir musim":"");label.fontSize=20;label.fontSizeMax=20;label.fontSizeMin=16;
            var icon=b.GetComponentInChildren<Image>(true);var slot=Find<Image>("DayIcon_"+i);Paint(slot,events.FirstOrDefault()?.icon);
        }
        var entries=data!=null?data.Events(viewedYear,viewedSeason,selectedDay).ToList():new();
        var record=data?.dates.FirstOrDefault(d=>d.absoluteDay==GameScheduleData.AbsoluteDay(viewedYear,viewedSeason,selectedDay));
        selectedEvent=entries.Count==0?0:selectedEvent%entries.Count;var e=entries.Count>0?entries[selectedEvent]:null;
        Find<TMP_Text>("DetailTitle").text=e!=null?e.title:selectedDay==length?"Hari terakhir musim":"Tidak ada acara terjadwal";
        Find<TMP_Text>("DetailDate").text=$"{selectedDay} {GameScheduleData.SeasonEnglishName(viewedSeason)} · {GameScheduleData.SeasonName(viewedSeason)} · Tahun {viewedYear}";
        Find<TMP_Text>("DetailBody").text=e!=null?$"{e.location} · {Hour(e.startHour)}–{Hour(e.endHour)}\n\n{e.description}":"Kalender mencatat festival dan hari penting."+(string.IsNullOrWhiteSpace(record?.note)?"":"\n\n"+record.note);
        Paint(Find<Image>("EventImageSlot"),e?.illustration);
        float left=e?.illustration!=null?.29f:.04f;
        foreach(string name in new[]{"DetailBody","DetailTitle","DetailDate"}){var r=Find<TMP_Text>(name).rectTransform;r.anchorMin=new(left,r.anchorMin.y);}
        Find<Button>("PreviousSeason").interactable=viewedYear>1||viewedSeason>0;Find<Button>("NextSeason").interactable=true;
        Find<Button>("NextEvent").gameObject.SetActive(entries.Count>1);
    }
    static string Hour(float h)=>$"{(int)h:00}:{Mathf.RoundToInt((h%1)*60):00}";
    public override void Build()
    {
        var safe=MakeCanvas("Calendar_UI_Editable");ImageSlot("BackgroundImageSlot",uiRoot.transform,Vector2.zero,Vector2.one).transform.SetAsFirstSibling();
        var panel=Panel("CalendarPanel",safe,new(.29f,.08f),new(.97f,.96f));
        Text("Title",panel,"KALENDER",36,new(.06f,.88f),new(.94f,.98f)).alignment=TextAlignmentOptions.Center;
        ImageSlot("CalendarIconSlot",panel,new(.04f,.90f),new(.10f,.96f));
        Button("PreviousSeason",panel,"‹  Bulan sebelumnya",new(.03f,.82f),new(.26f,.88f),()=>ChangeSeason(-1));
        Button("NextSeason",panel,"Bulan berikutnya  ›",new(.74f,.82f),new(.97f,.88f),()=>ChangeSeason(1));
        Text("Season",panel,"",21,new(.27f,.81f),new(.73f,.89f)).alignment=TextAlignmentOptions.Center;
        Text("Legend",panel,"Ulang tahun   ·   Festival   ·   Hari musim",18,new(.04f,.76f),new(.96f,.81f)).alignment=TextAlignmentOptions.Center;
        string[] week={"Sen","Sel","Rab","Kam","Jum","Sab","Min"};
        for(int col=0;col<7;col++)Text("Week_"+col,panel,week[col],20,new(.03f+col*.134f,.71f),new(.16f+col*.134f,.755f)).alignment=TextAlignmentOptions.Center;
        for(int i=0;i<GameScheduleData.MaxDaysPerSeason;i++){int day=i+1;
            var b=Button("Day_"+day,panel,"",new(.03f,.3f),new(.16f,.4f),()=>SelectDay(day));b.GetComponentInChildren<TMP_Text>().alignment=TextAlignmentOptions.TopLeft;
            ImageSlot("DayIcon_"+day,b.transform,new(.70f,.65f),new(.92f,.95f));}
        var detail=Panel("EventDetail",panel,new(.03f,.06f),new(.97f,.25f),GameplayHUDStyle.Card);
        ImageSlot("EventImageSlot",detail,new(.025f,.08f),new(.26f,.92f));
        Text("DetailDate",detail,"",20,new(.04f,.76f),new(.96f,.94f));
        Text("DetailTitle",detail,"",26,new(.29f,.56f),new(.95f,.75f));
        Text("DetailBody",detail,"",20,new(.04f,.10f),new(.95f,.52f));
        Button("NextEvent",detail,"Acara berikutnya",new(.70f,.02f),new(.96f,.19f),NextEvent);
        Text("Keyboard",panel,"← / → Ganti bulan    W A S D Pilih hari    Enter Detail    Esc Tutup",18,new(.04f,.01f),new(.82f,.05f));
        Button("Close",panel,"Tutup ×",new(.84f,.01f),new(.97f,.05f),Close);uiRoot.SetActive(false);
    }
}
