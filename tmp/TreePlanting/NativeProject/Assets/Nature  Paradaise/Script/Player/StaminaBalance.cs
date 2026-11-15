using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum StaminaResourceSize { Auto, Small, Large }

/// <summary>One authoritative CSV source; changes take effect on the next Play session.</summary>
[CreateAssetMenu(menuName="Nature Paradise/Balance/Stamina CSV")]
public sealed class StaminaBalance : ScriptableObject
{
    public TextAsset actionsCsv, levelsCsv, berryCsv;
    static StaminaBalance current;
    Dictionary<string,float> costs;
    readonly List<float> levels=new();
    public int TierCount { get { Ensure(); return levels.Count; } }
    public static StaminaBalance Current
    {
        get { if(current==null) current=Resources.Load<StaminaBalance>("Player/Stamina Balance");
            if(current==null) throw new InvalidOperationException("Missing Player/Stamina Balance asset"); return current; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { if(current!=null)current.costs=null; current=null; }
    void OnEnable() { costs=null; }
    public void Reload() { costs=null; Ensure(); }
    public float Cost(string action, float count=1) { Ensure(); return costs[action]*Mathf.Max(0,count); }
    public float Maximum(int tier) { Ensure(); return levels[Mathf.Clamp(tier,0,levels.Count-1)]; }
    void Ensure()
    {
        if(costs!=null) return;
        var next=new Dictionary<string,float>(); var tiers=new List<float>();
        if(actionsCsv==null||levelsCsv==null||berryCsv==null) throw new InvalidOperationException("Stamina CSV slots are missing");
        var rows=Parse(actionsCsv.text);
        foreach(var row in rows.GetRange(1,rows.Count-1))
        {
            if(row.Count!=6) throw new FormatException("StaminaActions schema mismatch");
            float quantity=Number(row[2]),delta=Number(row[4]);
            if(quantity<=0||delta>0||next.ContainsKey(row[0])) throw new FormatException("Invalid stamina action: "+row[0]);
            next.Add(row[0],-delta/quantity);
        }
        string[] required={"run","chop_large_tree","chop_small_tree","break_large_rock","break_small_rock","hoe","plant","water","fertilize","harvest","feed_animal","brush_cow","milk_cow","shear_sheep","cast_fishing_line","catch_fish","fail_fishing","feed_fish","sickle_wild_grass","clean_manure"};
        foreach(var id in required) if(!next.ContainsKey(id)) throw new FormatException("Missing stamina action "+id);
        rows=Parse(levelsCsv.text);
        for(int i=1;i<rows.Count;i++)
        {
            var row=rows[i];float maximum=Number(row[2]);
            if(int.Parse(row[0],CultureInfo.InvariantCulture)!=i-1||maximum<=0||(tiers.Count>0&&maximum<=tiers[tiers.Count-1]))throw new FormatException("Stamina tiers must increase in order");
            tiers.Add(maximum);
        }
        if(tiers.Count!=4)throw new FormatException("Exactly four stamina tiers required");
        var berry=Parse(berryCsv.text);
        if(berry.Count!=2||berry[1][2]!="NextTier"||Number(berry[1][3])!=1||!bool.Parse(berry[1][4])||bool.Parse(berry[1][5])||bool.Parse(berry[1][6]))throw new FormatException("Berry must permanently advance one tier without skill and cannot be consumed at max");
        costs=next; levels.Clear(); levels.AddRange(tiers);
    }
    static float Number(string text) { float value=float.Parse(text,CultureInfo.InvariantCulture); if(float.IsNaN(value)||float.IsInfinity(value))throw new FormatException("Non-finite stamina value");return value; }
    static List<List<string>> Parse(string text)
    {
        var rows=new List<List<string>>();var row=new List<string>();var cell=new System.Text.StringBuilder();bool quoted=false;
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];if(c=='"'){if(quoted&&i+1<text.Length&&text[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
            else if(!quoted&&(c==','||c=='\n')){row.Add(cell.ToString().Trim('﻿','\r'));cell.Clear();if(c=='\n'){if(row.Count>1)rows.Add(row);row=new List<string>();}}
            else cell.Append(c);
        }
        if(quoted)throw new FormatException("Unterminated CSV quote");
        if(cell.Length>0||row.Count>0){row.Add(cell.ToString().Trim('﻿','\r'));if(row.Count>1)rows.Add(row);}
        return rows;
    }
    public static bool Can(Inventory inventory,string action,float count=1)
    {
        var status=inventory!=null?inventory.GetComponent<PlayerStatusSystem>():null;
        return status==null||status.CanSpendStamina(Current.Cost(action,count));
    }
    public static bool Spend(Inventory inventory,string action,float count=1)
    {
        var status=inventory!=null?inventory.GetComponent<PlayerStatusSystem>():null;
        if(status==null||status.TrySpendStamina(Current.Cost(action,count)))return true;
        SaveLoadFeedback.Instance?.ShowMessage("Stamina tidak cukup");return false;
    }
}
