using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class CafeCategory
{
    public string id,title;
    public Sprite icon;
}
[Serializable] public class CafeMenuOffer
{
    public string id,categoryId;
    public ItemSO item;
    [Min(-1),Tooltip("-1 mengikuti buyPrice ItemSO; 0 gratis.")]public int price=-1;
    [Range(0,5)]public int quality;
    [Range(1,99)]public int maximumQuantity=99;
    public bool enabled=true,allowDineIn=true,allowTakeaway=true;
    public Sprite illustration;
    public int Price=>price<0?(item!=null?item.buyPrice:0):price;
}
[CreateAssetMenu(menuName="Nature Paradise/Shop/Cafe Catalog")]
public class CafeCatalog : ScriptableObject
{
    public string title="KAFE & RUMAH MAKAN";
    public List<CafeCategory> categories=new();
    [Tooltip("Kosong sampai menu siap. Semua produk merujuk ItemSO makanan asli.")]public List<CafeMenuOffer> menu=new();
    public Sprite background,merchantPortrait,shopIcon,goldIcon;
    public float openHour=8,closeHour=18;
}
