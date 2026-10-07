using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class SmithToolOffer
{
 public ItemSO item;
 public string displayName;
 [Min(2)] public int maximumLevel=4;
 public Sprite illustration;
 public List<BuildingLevelDefinition> upgrades=new();
 public BuildingLevelDefinition Next(int level)=>upgrades.Find(x=>x.level==level+1);
}
[CreateAssetMenu(menuName="Game/Shop/Blacksmith Upgrade Catalog")]
public sealed class BlacksmithCatalog : ScriptableObject
{
 public Sprite background,merchantPortrait;
 public List<SmithToolOffer> tools=new();
}

/// <summary>Shared transaction used by the UI; callers decide when to persist a successful upgrade.</summary>
public static class BlacksmithUpgradeService
{
 public static bool TryUpgrade(SmithToolOffer offer,PlayerStatusSystem status,Inventory inventory,out string reason)
 {
  if(offer?.item==null||status==null||offer.item.equippedTool==PlayerToolType.None){reason="Alat atau player tidak tersedia";return false;}
  int level=status.GetToolLevel(offer.item.equippedTool);
  if(level>=offer.maximumLevel){reason="Alat sudah mencapai level maksimum";return false;}
  var next=offer.Next(level);
  if(!BuildingCostUtility.CanAfford(next,inventory,out reason))return false;
  if(!BuildingCostUtility.TrySpend(next,inventory)){reason="Transaksi gagal";return false;}
  status.SetToolLevel(offer.item.equippedTool,next.level);reason=null;return true;
 }
}
