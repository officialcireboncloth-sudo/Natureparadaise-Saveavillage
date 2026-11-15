using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum ShopKind { Crops, Animal, Minimarket }
[Serializable] public sealed class ShopPriceOverride
{
 public ItemSO item;
 [Tooltip("-1 uses the price from ItemSO. Overrides affect this shop only.")] [Min(-1)] public int price=-1;
 [Min(1)] public int defaultAmount=1;
 [Range(1,99)] public int maximumAmount=99;
 [Range(0,5)] public int qualityStars;
 public Sprite image;
}
[Serializable] public sealed class ShopCatalogTab
{
 public string title;
 public Sprite background;
 public bool sellInventory;
 public List<ItemSO> items=new();
 public List<AnimalShopOffer> animals=new();
}
[CreateAssetMenu(menuName="Nature Paradise/Shop/Catalog")]
public sealed class ShopCatalogSO:ScriptableObject
{
 public ShopKind kind;
 public string displayName;
 public ShopTheme theme;
 public List<ShopCatalogTab> tabs=new();
 [InspectorName("Product Settings — Price / Amount / Quality / Image")] public List<ShopPriceOverride> priceOverrides=new();
 public ShopPriceOverride Settings(ItemSO item)=>priceOverrides.Find(p=>p!=null&&p.item==item);
 public int BuyPrice(ItemSO item)=>Settings(item) is ShopPriceOverride s && s.price>=0?s.price:item.buyPrice;
 public int Quality(ItemSO item)=>Mathf.Clamp(Settings(item)?.qualityStars??0,0,5);
 public int MaximumAmount(ItemSO item)=>Mathf.Clamp(Settings(item)?.maximumAmount??99,1,99);
 public int DefaultAmount(ItemSO item)=>Mathf.Clamp(Settings(item)?.defaultAmount??1,1,MaximumAmount(item));
 public bool Contains(ItemSO item)=>item!=null&&tabs.Any(t=>!t.sellInventory&&t.items.Contains(item));
 public bool Contains(AnimalShopOffer animal)=>animal!=null&&tabs.Any(t=>t.animals.Contains(animal));
 public void Populate(ShopManager manager)
 {
  var items=tabs.Where(t=>!t.sellInventory).SelectMany(t=>t.items).Where(i=>i!=null).Distinct().ToList();
  manager.sellsFarmEquipment=kind==ShopKind.Crops;
  manager.seedItem=items.Find(i=>i.category==ItemCategory.Seed);
  manager.fertilizerItems=items.FindAll(i=>i.IsFertilizer);
  manager.cropBoosterItems=items.FindAll(i=>i.IsCropBooster);
  manager.cropBoosterItem=manager.cropBoosterItems.FirstOrDefault();
  manager.toolItems=items.FindAll(i=>i.category==ItemCategory.Tool);
  manager.sprinklerItems=items.FindAll(i=>i.IsSprinkler);
  manager.treeSeedItems=items.FindAll(i=>i.treeDefinition!=null);
  manager.baitItems=items.FindAll(i=>i.IsFishingBait);
  manager.animalOffers=tabs.SelectMany(t=>t.animals).Where(a=>a!=null).Distinct().ToList();
 }
}

