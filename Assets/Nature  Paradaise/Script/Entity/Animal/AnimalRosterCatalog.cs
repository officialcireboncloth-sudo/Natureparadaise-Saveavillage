using System;
using System.Collections.Generic;
using UnityEngine;

public enum AnimalPurpose { Product, Meat, Breeding, Riding, Hauling, HerdingCompanion, HouseCompanion }

/// <summary>One source row. Unknown prices remain distinct from a configured zero price.</summary>
[Serializable]
public sealed class AnimalRosterEntry
{
    public string id,displayName,species,benefit;
    public AnimalPurpose purpose;
    public bool hasBuyPrice,hasAdultSellPrice,hasProductPrice;
    public int buyPrice,adultSellPrice,productPrice;
    public ItemSO product;
    public bool hasRuntimeType,isDefault;
    public AnimalType runtimeType;
    public AnimalGrowthProfileSO growthProfile;
    public GameObject animalPrefab;
    public bool CanPurchase=>hasBuyPrice&&hasRuntimeType&&growthProfile!=null;
    public AnimalShopOffer RestoreOffer()=>new AnimalShopOffer
    {
        displayName=displayName,animalType=runtimeType,offerKind=AnimalShopOfferKind.Young,
        price=buyPrice,growthProfile=growthProfile,animalPrefab=animalPrefab,productItem=product
    };
}

[CreateAssetMenu(menuName="Nature Paradise/Animal/Roster Catalog")]
public sealed class AnimalRosterCatalog:ScriptableObject
{
    public AnimalRosterEntry[] entries;
    public static AnimalRosterCatalog Load()=>Resources.Load<AnimalRosterCatalog>("Catalogs/AnimalRosterCatalog");
    public AnimalRosterEntry Find(string id)=>entries==null||string.IsNullOrEmpty(id)?null:Array.Find(entries,e=>e!=null&&e.id==id);
    public AnimalRosterEntry DefaultFor(AnimalType type)=>entries==null?null:Array.Find(entries,e=>e!=null&&e.hasRuntimeType&&e.runtimeType==type&&e.isDefault);
    public void ApplyOffers(List<AnimalShopOffer> offers)
    {
        if(offers==null||entries==null)return;
        foreach(var entry in entries)
        {
            if(entry==null||!entry.isDefault||!entry.CanPurchase)continue;
            var offer=offers.Find(o=>o!=null&&o.animalType==entry.runtimeType&&o.offerKind==AnimalShopOfferKind.Young);
            if(offer==null){offers.Add(entry.RestoreOffer());continue;}
            offer.price=entry.buyPrice;offer.growthProfile=entry.growthProfile;offer.productItem=entry.product;
            offer.displayName="Anak "+entry.displayName;
            if(offer.animalPrefab==null)offer.animalPrefab=entry.animalPrefab;
        }
    }
}
