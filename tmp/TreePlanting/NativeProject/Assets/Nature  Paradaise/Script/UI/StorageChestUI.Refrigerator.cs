using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class StorageChestUI
{
    int fridgeFilter;
    TMP_Text fridgeCapacity;
    readonly List<int> fridgeBagIndices=new(), fridgeStoredIndices=new();
    readonly List<Button> fridgeTabs=new();
    public void SetFridgeFilter(int category)
    {
        if(Fridge==null||dragGhost!=null)return;
        fridgeFilter=Mathf.Clamp(category,0,4);quantity=1;
        RefreshFridgeIndices();bagScroll.verticalNormalizedPosition=1;chestScroll.verticalNormalizedPosition=1;dirty=true;
    }
    bool MatchesFridge(ItemSO item)
    {
        if(fridgeFilter==0)return true;
        if(item==null)return false;
        return fridgeFilter switch
        {
            1=>item.refrigeratorCategory is RefrigeratorCategory.Crop or RefrigeratorCategory.Fruit,
            2=>item.refrigeratorCategory==RefrigeratorCategory.Fish,
            3=>item.refrigeratorCategory==RefrigeratorCategory.AnimalProduct,
            4=>item.refrigeratorCategory is RefrigeratorCategory.CookedFood or RefrigeratorCategory.Drink or RefrigeratorCategory.Ingredient || item.foodPreparation==FoodPreparation.ReadyToEat,
            _=>true
        };
    }
    void RefreshFridgeIndices()
    {
        fridgeBagIndices.Clear();fridgeStoredIndices.Clear();
        for(int i=0;i<inventory.Capacity;i++)if(MatchesFridge(inventory.GetSlot(i)?.item))fridgeBagIndices.Add(i);
        for(int i=0;i<StorageCapacity;i++)if(MatchesFridge(Stored(i)?.item))fridgeStoredIndices.Add(i);
        if(!fridgeBagIndices.Contains(bagIndex))bagIndex=fridgeBagIndices.Count>0?fridgeBagIndices[0]:-1;
        if(!fridgeStoredIndices.Contains(chestIndex))chestIndex=fridgeStoredIndices.Count>0?fridgeStoredIndices[0]:-1;
        for(int i=0;i<fridgeTabs.Count;i++){
            var surface=(MainMenuRoundedImage)fridgeTabs[i].targetGraphic;
            surface.borderColor=i==fridgeFilter?new Color(.5f,1,.96f):Color.clear;surface.borderWidth=i==fridgeFilter?1.5f:0;
        }
    }
    void NavigateSlots(int delta)
    {
        if(Fridge==null){Select(storageSelected,(storageSelected?chestIndex:bagIndex)+delta);return;}
        var indices=storageSelected?fridgeStoredIndices:fridgeBagIndices;
        if(indices.Count==0)return;
        int position=Mathf.Max(0,indices.IndexOf(storageSelected?chestIndex:bagIndex));
        Select(storageSelected,indices[Mathf.Clamp(position+delta,0,indices.Count-1)]);
    }
    public void SortFridge(){if(Fridge==null)return;EndDrag();RefrigeratorService.Sort();dirty=true;GameplayInput.ConsumeCurrentFrame();}
    void BuildFridge(RectTransform safe)
    {
        var panel=Rect("Refrigerator Panel",safe,.415f,.035f,.99f,.985f);Surface(panel,new(.11f,.19f,.24f,.93f),theme?.panel);
        Art("Leaf Image Slot",panel,theme?.leafIcon,.035f,.923f,.08f,.982f);
        Text(panel,"KULKAS",42,.10f,.93f,.67f,.995f);
        Text(panel,"Bahan di dalam kulkas otomatis tersedia saat memasak.",21,.10f,.898f,.96f,.94f);
        var capacity=Rect("Capacity Badge",panel,.77f,.941f,.975f,.989f);Surface(capacity,Gray);
        Art("Snowflake Image Slot",capacity,theme?.snowflakeIcon,.03f,.08f,.23f,.92f);
        fridgeCapacity=Text(capacity,"",21,.24f,.05f,.98f,.95f);
        string[] names={"Semua","Sayuran","Protein","Produk Hewan","Masakan"};
        for(int i=0;i<names.Length;i++){int filterIndex=i;float x=.10f+i*.171f;fridgeTabs.Add(ButtonAt(panel,names[i],x,.84f,x+.163f,.888f,()=>SetFridgeFilter(filterIndex)));}
        var bag=Rect("Player Bag",panel,.02f,.285f,.44f,.82f);Surface(bag,new(.15f,.24f,.29f,.6f));
        var contents=Rect("Fridge Contents",panel,.545f,.285f,.98f,.82f);Surface(contents,new(.15f,.24f,.29f,.6f));
        Art("Bag Image Slot",bag,theme?.bagIcon,.025f,.9f,.12f,.98f);Text(bag,"TAS PEMAIN",24,.15f,.90f,.73f,.98f);bagCount=Text(bag,"",20,.75f,.9f,.98f,.98f);
        Art("Fridge Image Slot",contents,theme?.chestIcon,.025f,.9f,.12f,.98f);Text(contents,"ISI KULKAS",24,.15f,.90f,.73f,.98f);chestCount=Text(contents,"",20,.75f,.9f,.98f,.98f);
        bagScroll=Grid(bag,out bagViewport,out bagContent);chestScroll=Grid(contents,out chestViewport,out chestContent);
        storeButton=ButtonAt(panel,"E\nSimpan",.451f,.53f,.532f,.65f,()=>Transfer(false),theme?.storeIcon);
        takeButton=ButtonAt(panel,"R\nAmbil",.451f,.39f,.532f,.51f,()=>Transfer(true),theme?.takeIcon);
        var info=Rect("Item Details",panel,.02f,.055f,.98f,.263f);Surface(info,new(.22f,.29f,.33f,.7f));
        illustration=Art("Item Illustration Slot",info,null,.025f,.25f,.18f,.94f);
        detail=Text(info,"Pilih item",20,.205f,.34f,.75f,.98f);
        Text(info,"Jumlah",19,.79f,.78f,.98f,.99f);
        ButtonAt(info,"−",.79f,.51f,.835f,.75f,()=>ChangeQuantity(-1));amount=Text(info,"1",25,.84f,.51f,.93f,.75f);amount.alignment=TextAlignmentOptions.Midline;
        ButtonAt(info,"+",.935f,.51f,.98f,.75f,()=>ChangeQuantity(1));
        ButtonAt(info,"Q  Simpan Semua Makanan",.205f,.08f,.52f,.30f,StoreAll,theme?.storeAllIcon);
        ButtonAt(info,"F  Ambil Semua",.535f,.08f,.77f,.30f,TakeAll,theme?.takeAllIcon);
        ButtonAt(info,"T  Rapikan",.785f,.08f,.98f,.30f,SortFridge,theme?.sortIcon);
        feedback=Text(panel,"",16,.03f,.031f,.98f,.053f);
        Text(panel,"W A S D  Pilih    Tab  Ganti Panel    E  Simpan    R  Ambil    Shift  Stack    Esc  Tutup",16,.03f,.004f,.98f,.030f);
        bag.gameObject.AddComponent<StorageSlotDragHandler>().Initialize(this,false,-1);
        contents.gameObject.AddComponent<StorageSlotDragHandler>().Initialize(this,true,-1);
    }
}
