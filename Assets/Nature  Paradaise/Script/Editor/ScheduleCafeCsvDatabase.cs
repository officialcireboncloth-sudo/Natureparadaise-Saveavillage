using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Transactional import of the calendar, weather overrides and actual ItemSO cafe offers.</summary>
public static class ScheduleCafeCsvDatabase
{
    public const string Root="Assets/Nature  Paradaise/Data/Balance/Schedules";
    public const string SchedulePath="Assets/Nature  Paradaise/Resources/Game Schedules.asset";
    public const string CafePath="Assets/Nature  Paradaise/Resources/Cafe Catalog.asset";
    public static string LastReport="";
    static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    static readonly string[] DateColumns={"absolute_day","year","season","day","weekday","note"};
    static readonly string[] WeatherColumns={"absolute_day","weather"};
    static readonly string[] EventColumns={"event_id","year","season","day","type","title","description","location","start_hour","end_hour","image_guid","icon_guid"};
    static readonly string[] CategoryColumns={"category_id","title","icon_guid"};
    static readonly string[] MenuColumns={"offer_id","category_id","item_id","price","quality","maximum_quantity","enabled","allow_dine_in","allow_takeaway","image_guid"};
    static string S(object value)=>Convert.ToString(value,Inv);
    static string Guid(UnityEngine.Object obj)=>obj==null?"":AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
    public static void EnsureAssets()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SchedulePath));
        var schedule=AssetDatabase.LoadAssetAtPath<GameScheduleData>(SchedulePath);
        if(schedule==null)
        {
            schedule=ScriptableObject.CreateInstance<GameScheduleData>();
            for(int absolute=1;absolute<=1120;absolute++){GameScheduleData.Date(absolute,out int year,out int season,out int day);schedule.dates.Add(new(){absoluteDay=absolute,year=year,season=season,day=day,weekday=(absolute-1)%7});schedule.weather.Add(new(){absoluteDay=absolute});}
            AssetDatabase.CreateAsset(schedule,SchedulePath);
        }
        if(AssetDatabase.LoadAssetAtPath<CafeCatalog>(CafePath)==null)
        {
            var cafe=ScriptableObject.CreateInstance<CafeCatalog>();
            cafe.categories.AddRange(new[]{new CafeCategory{id="food",title="Makanan"},new CafeCategory{id="drink",title="Minuman"},new CafeCategory{id="set",title="Paket"}});
            AssetDatabase.CreateAsset(cafe,CafePath);
        }
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Nature Paradise/Data CSV/Schedules and Cafe/Export")]
    public static void Export()
    {
        RequireEdit();EnsureAssets();Directory.CreateDirectory(Root);
        var d=AssetDatabase.LoadAssetAtPath<GameScheduleData>(SchedulePath);var c=AssetDatabase.LoadAssetAtPath<CafeCatalog>(CafePath);
        Write("CalendarDates",DateColumns,d.dates.Select(x=>new[]{S(x.absoluteDay),S(x.year),S(x.season),S(x.day),S(x.weekday),x.note??""}));
        Write("WeatherSchedule",WeatherColumns,d.weather.Select(x=>new[]{S(x.absoluteDay),x.useOverride?x.weather.ToString():"Auto"}));
        Write("FestivalSchedule",EventColumns,d.events.Select(x=>new[]{x.id,S(x.year),S(x.season),S(x.day),x.kind.ToString(),x.title,x.description,x.location,S(x.startHour),S(x.endHour),Guid(x.illustration),Guid(x.icon)}));
        Write("CafeCategories",CategoryColumns,c.categories.Select(x=>new[]{x.id,x.title,Guid(x.icon)}));
        Write("CafeMenu",MenuColumns,c.menu.Select(x=>new[]{x.id,x.categoryId,x.item!=null?x.item.Id:"",S(x.price),S(x.quality),S(x.maximumQuantity),S(x.enabled),S(x.allowDineIn),S(x.allowTakeaway),Guid(x.illustration)}));
        LastReport="Export kalender 10 tahun, cuaca, acara dan kategori/menu cafe selesai.";AssetDatabase.Refresh();Debug.Log("[DATA CSV] "+LastReport);
    }
    static void Write(string name,string[] columns,IEnumerable<string[]> rows)=>BalanceCsvDatabase.Csv.Write(Root+"/"+name+".csv",columns,rows);
    static void RequireEdit(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Keluar Play Mode dahulu.");}
    static List<Dictionary<string,string>> Read(string name,string[] columns)
    {
        string path=Root+"/"+name+".csv";
        var rows=BalanceCsvDatabase.Csv.Read(path,out var header,out var error);
        if(error!=null)throw new InvalidDataException(name+": "+error);
        if(header.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=header.Length||columns.Any(c=>!header.Contains(c)))throw new InvalidDataException(name+": header hilang / duplikat.");
        return rows;
    }
    static int I(Dictionary<string,string> r,string col,int min,int max)
    {if(!int.TryParse(r[col],NumberStyles.Integer,Inv,out int n)||n<min||n>max)throw new InvalidDataException(col+" harus "+min+".."+max+"; nilai: "+r[col]);return n;}
    static float F(Dictionary<string,string> r,string col)
    {if(!float.TryParse(r[col],NumberStyles.Float,Inv,out float n)||float.IsNaN(n)||float.IsInfinity(n)||n<0||n>24)throw new InvalidDataException(col+" harus jam 0..24.");return n;}
    static bool B(Dictionary<string,string> r,string col){if(!bool.TryParse(r[col],out bool b))throw new InvalidDataException(col+" harus TRUE/FALSE.");return b;}
    static string Id(Dictionary<string,string> r,string col,HashSet<string> seen)
    {string id=r[col].Trim();if(string.IsNullOrWhiteSpace(id)||!seen.Add(id))throw new InvalidDataException(col+" kosong / duplikat: "+id);return id;}
    static Sprite Sprite(Dictionary<string,string> r,string col,Sprite previous)
    {
        string guid=r[col].Trim();if(guid.Length==0)return null;
        if(previous!=null&&Guid(previous)==guid)return previous;
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
        if(sprite==null)throw new InvalidDataException(col+": GUID bukan Sprite: "+guid);return sprite;
    }
    static List<T> Parse<T>(string name,string[] columns,Func<Dictionary<string,string>,T> parse)
    {var result=new List<T>();int line=1;foreach(var row in Read(name,columns)){line++;try{result.Add(parse(row));}catch(Exception ex){throw new InvalidDataException(name+":"+line+" "+ex.Message);}}return result;}
    public static bool Prepare(out GameScheduleData next,out CafeCatalog cafe,out string error)
    {
        next=ScriptableObject.CreateInstance<GameScheduleData>();cafe=ScriptableObject.CreateInstance<CafeCatalog>();error=null;
        try
        {
            var old=AssetDatabase.LoadAssetAtPath<GameScheduleData>(SchedulePath);var oldCafe=AssetDatabase.LoadAssetAtPath<CafeCatalog>(CafePath);
            if(old==null||oldCafe==null)throw new InvalidDataException("Jalankan setup / Export dahulu.");
            EditorUtility.CopySerialized(old,next);EditorUtility.CopySerialized(oldCafe,cafe);
            var seen=new HashSet<string>();
            next.dates=Parse("CalendarDates",DateColumns,r=>
            {int abs=I(r,"absolute_day",1,1120);if(!seen.Add(S(abs)))throw new InvalidDataException("absolute_day duplikat: "+abs);GameScheduleData.Date(abs,out int y,out int s,out int d);
                int ry=I(r,"year",1,10),rs=I(r,"season",0,3),rd=I(r,"day",1,28),rw=I(r,"weekday",0,6);
                if(ry!=y||rs!=s||rd!=d||rw!=(abs-1)%7)throw new InvalidDataException("Tanggal tidak cocok dengan kalender game (28 hari/musim). Edit hanya note.");
                return new CalendarDateRecord{absoluteDay=abs,year=y,season=s,day=d,weekday=rw,note=r["note"]};});
            if(next.dates.Count!=1120)throw new InvalidDataException("CalendarDates wajib memuat 1120 tanggal tahun 1–10.");
            seen=new();next.weather=Parse("WeatherSchedule",WeatherColumns,r=>
            {int abs=I(r,"absolute_day",1,1120);if(!seen.Add(S(abs)))throw new InvalidDataException("absolute_day duplikat: "+abs);string value=r["weather"].Trim();bool auto=value.Equals("Auto",StringComparison.OrdinalIgnoreCase);
                WeatherType w=default;if(!auto&&(!Enum.TryParse(value,true,out w)||!Enum.IsDefined(typeof(WeatherType),w)))throw new InvalidDataException("WeatherType tidak dikenal: "+value);
                return new WeatherDateRecord{absoluteDay=abs,useOverride=!auto,weather=w};});
            if(next.weather.Count!=1120)throw new InvalidDataException("WeatherSchedule wajib memuat 1120 tanggal; gunakan Auto untuk generator.");
            seen=new();next.events=Parse("FestivalSchedule",EventColumns,r=>
            {string id=Id(r,"event_id",seen);if(!Enum.TryParse(r["type"],true,out CalendarEventKind kind)||!Enum.IsDefined(typeof(CalendarEventKind),kind))throw new InvalidDataException("type: Festival/Birthday/SeasonDay.");
                if(string.IsNullOrWhiteSpace(r["title"]))throw new InvalidDataException("title wajib diisi.");float start=F(r,"start_hour"),end=F(r,"end_hour");if(start>end)throw new InvalidDataException("start_hour > end_hour.");var prior=old.events.FirstOrDefault(e=>e.id==id);
                return new CalendarEventRecord{id=id,year=I(r,"year",0,10),season=I(r,"season",0,3),day=I(r,"day",1,28),kind=kind,title=r["title"],description=r["description"],location=r["location"],startHour=start,endHour=end,illustration=Sprite(r,"image_guid",prior?.illustration),icon=Sprite(r,"icon_guid",prior?.icon)};});
            seen=new();cafe.categories=Parse("CafeCategories",CategoryColumns,r=>{string id=Id(r,"category_id",seen);if(string.IsNullOrWhiteSpace(r["title"]))throw new InvalidDataException("title wajib diisi.");return new CafeCategory{id=id,title=r["title"],icon=Sprite(r,"icon_guid",oldCafe.categories.FirstOrDefault(e=>e.id==id)?.icon)};});
            var categories=new HashSet<string>(cafe.categories.Select(c=>c.id));
            var items=AssetDatabase.FindAssets("t:ItemSO").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemSO>).Where(i=>i!=null).GroupBy(i=>i.Id).ToDictionary(g=>g.Key,g=>g.ToList());
            seen=new();cafe.menu=Parse("CafeMenu",MenuColumns,r=>
            {string id=Id(r,"offer_id",seen);if(!categories.Contains(r["category_id"]))throw new InvalidDataException("category_id tidak ada di CafeCategories.");
                if(!items.TryGetValue(r["item_id"],out var matches)||matches.Count!=1)throw new InvalidDataException("item_id tidak ada / ambigu: "+r["item_id"]);var item=matches[0];if(!item.CanConsume)throw new InvalidDataException("ItemSO harus makanan siap konsumsi.");
                bool a=B(r,"allow_dine_in"),b=B(r,"allow_takeaway");if(!a&&!b)throw new InvalidDataException("Aktifkan minimal satu cara penyajian.");
                return new CafeMenuOffer{id=id,categoryId=r["category_id"],item=item,price=I(r,"price",-1,int.MaxValue),quality=I(r,"quality",0,5),maximumQuantity=I(r,"maximum_quantity",1,99),enabled=B(r,"enabled"),allowDineIn=a,allowTakeaway=b,illustration=Sprite(r,"image_guid",oldCafe.menu.FirstOrDefault(e=>e.id==id)?.illustration)};});
            return true;
        }
        catch(Exception ex){error=ex.Message;return false;}
    }
    [MenuItem("Nature Paradise/Data CSV/Schedules and Cafe/Validate")]
    public static void ValidateMenu()=>Validate();
    public static bool Validate()
    {
        RequireEdit();bool valid=Prepare(out var next,out var cafe,out var error);
        LastReport=valid?$"VALID: {next.dates.Count} tanggal, {next.weather.Count} cuaca, {next.events.Count} acara, {cafe.categories.Count} kategori, {cafe.menu.Count} menu.":"IMPORT DIBATALKAN: "+error;
        UnityEngine.Object.DestroyImmediate(next);UnityEngine.Object.DestroyImmediate(cafe);if(valid)Debug.Log("[DATA CSV] "+LastReport);else Debug.LogWarning("[DATA CSV] "+LastReport);return valid;
    }
    [MenuItem("Nature Paradise/Data CSV/Schedules and Cafe/Import")]
    public static void Import()
    {
        RequireEdit();bool valid=Prepare(out var next,out var cafe,out var error);
        try
        {
            if(!valid){LastReport="Import dibatalkan: "+error;Debug.LogWarning("[DATA CSV] "+LastReport);return;}
            string backup="BalanceBackups/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            foreach(string path in new[]{SchedulePath,CafePath}){string dest=Path.Combine(backup,path);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(path,dest);File.Copy(path+".meta",dest+".meta");}
            var schedule=AssetDatabase.LoadAssetAtPath<GameScheduleData>(SchedulePath);var target=AssetDatabase.LoadAssetAtPath<CafeCatalog>(CafePath);
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Undo.RecordObjects(new UnityEngine.Object[]{schedule,target},"Import schedule and cafe CSV");EditorUtility.CopySerialized(next,schedule);EditorUtility.CopySerialized(cafe,target);EditorUtility.SetDirty(schedule);EditorUtility.SetDirty(target);AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);}
            catch{Undo.RevertAllDownToGroup(group);AssetDatabase.SaveAssets();throw;}
            LastReport="Import selesai. Backup: "+backup;Debug.Log("[DATA CSV] "+LastReport);
        }
        finally{UnityEngine.Object.DestroyImmediate(next);UnityEngine.Object.DestroyImmediate(cafe);}
    }
}
