using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FeedSiloSaveData { public string id; public int stock; public FeedKind feedKind; }

public sealed class FeedSilo : MonoBehaviour
{
    static readonly List<FeedSilo> Active = new();
    [SerializeField] string siloId;
    [SerializeField, Min(1)] int capacity = 999;
    [SerializeField] AnimalHome[] connectedHomes;
    [Tooltip("Fitur lanjutan. OFF berarti player tetap mengisi setiap box pakan secara manual.")]
    [SerializeField] bool automaticDistributionEnabled;
    [SerializeField] int stock;
    [SerializeField] FeedKind feedKind=FeedKind.Livestock;
    public int Stock => stock;
    public int Store(int amount) { int accepted=Mathf.Clamp(amount,0,Mathf.Max(0,capacity-stock)); stock+=accepted; return accepted; }
    public int Store(int amount,FeedKind kind)=>kind==feedKind?Store(amount):0;
    public void Configure(string id, AnimalHome home) { siloId=id; connectedHomes=new[]{home}; if(home!=null)feedKind=home.RequiredFeedKind; }
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);
    void Update()
    {
        if(!automaticDistributionEnabled || connectedHomes==null) return;
        foreach(var home in connectedHomes)
        {
            if(stock<=0) break;
            if(home==null || !home.Available || !home.HasAutoFeeder) continue;
            // Keep a day of feed in each trough rather than draining the silo into the first home.
            stock-=home.StoreFeed(Mathf.Min(stock,Mathf.Max(0,home.AnimalCount-home.TotalFeed)),feedKind);
        }
    }
    public static List<FeedSiloSaveData> CaptureAll()
    {
        var result=new List<FeedSiloSaveData>();
        foreach(var silo in Active) result.Add(new FeedSiloSaveData{id=silo.siloId,stock=silo.stock,feedKind=silo.feedKind});
        return result;
    }
    public static void RestoreAll(List<FeedSiloSaveData> data)
    {
        foreach(var silo in Active)
        {var saved=data?.Find(s=>s.id==silo.siloId);silo.stock=Mathf.Clamp(saved?.stock??0,0,silo.capacity);if(saved!=null&&saved.feedKind!=FeedKind.None)silo.feedKind=saved.feedKind;}
    }
}
