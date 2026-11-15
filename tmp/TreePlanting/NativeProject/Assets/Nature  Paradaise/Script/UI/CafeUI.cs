using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CafeUI : DataDrivenModal
{
    public CafeCatalog catalog;
    public int selectedCategory,selectedProduct,quantity=1;
    public bool dineIn=true;
    List<CafeMenuOffer> products=new();
    Inventory inventory;PlayerStatusSystem status;
    float nextRefresh;
    string feedback="Pilih menu dan cara penyajian.";
    protected override string Prompt=>"E: Kafe & Rumah Makan";
    protected override void BindControls(){Bind("PrevProduct",()=>MoveProduct(-1));Bind("NextProduct",()=>MoveProduct(1));Bind("Minus",()=>SetQuantity(quantity-1));Bind("Plus",()=>SetQuantity(quantity+1));Bind("DineIn",()=>SetDineIn(true));Bind("Takeaway",()=>SetDineIn(false));Bind("Order",Order);Bind("Close",Close);for(int i=0;i<6;i++){int slot=i;Bind("Menu_"+i,()=>SelectProduct(selectedProduct/6*6+slot));}if(catalog!=null)for(int i=0;i<catalog.categories.Count;i++){int index=i;Bind("Category_"+i,()=>SelectCategory(index));}}
    protected override void OnOpen(){selectedCategory=selectedProduct=0;quantity=1;feedback="Pilih menu dan cara penyajian.";Resolve();}
    void Resolve(){catalog??=Resources.Load<CafeCatalog>("Cafe Catalog");player??=FindFirstObjectByType<PlayerController>();if(player!=null){inventory=player.GetComponent<Inventory>();status=player.GetComponent<PlayerStatusSystem>();}}
    protected override void Update()
    {
        bool wasOpen=IsOpen;base.Update();if(!wasOpen||!IsOpen)return;
        if(Input.GetKeyDown(KeyCode.A))MoveProduct(-1);if(Input.GetKeyDown(KeyCode.D))MoveProduct(1);
        if(Input.GetKeyDown(KeyCode.W))SelectCategory(selectedCategory-1);if(Input.GetKeyDown(KeyCode.S))SelectCategory(selectedCategory+1);
        if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.KeypadPlus))SetQuantity(quantity+1);
        if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus))SetQuantity(quantity-1);
        if(Input.GetKeyDown(KeyCode.Return))Order();
        if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}
    }
    public void SelectCategory(int index){if(catalog==null||catalog.categories.Count==0)return;selectedCategory=(index+catalog.categories.Count)%catalog.categories.Count;selectedProduct=0;quantity=1;Refresh();}
    public void SelectProduct(int index){if(index>=products.Count)return;selectedProduct=index;quantity=1;Refresh();}
    public void MoveProduct(int step){if(products.Count>0){selectedProduct=(selectedProduct+step+products.Count)%products.Count;Refresh();}}
    public void SetQuantity(int value){quantity=dineIn?1:Mathf.Clamp(value,1,products.Count>0?products[selectedProduct].maximumQuantity:1);Refresh();}
    public void SetDineIn(bool value){dineIn=value;quantity=1;Refresh();}
    public bool CanOrder(out string reason)
    {
        reason="";Resolve();if(products.Count==0){reason="Belum ada menu pada kategori ini.";return false;}
        var offer=products[selectedProduct];
        if(!offer.enabled||offer.item==null||!offer.item.CanConsume){reason="Menu tidak tersedia / belum siap makan.";return false;}
        if(clock==null)clock=TimeManager.Instance;
        float hour=clock!=null?clock.CurrentTimeHours:12;
        if(hour<catalog.openHour||hour>=catalog.closeHour){reason=$"Kafe buka {catalog.openHour:00}:00–{catalog.closeHour:00}:00.";return false;}
        if(dineIn&&!offer.allowDineIn||!dineIn&&!offer.allowTakeaway){reason="Cara penyajian ini tidak tersedia.";return false;}
        long price=(long)offer.Price*quantity;
        if(price<0||price>int.MaxValue||ScoreManager.Instance==null||ScoreManager.Instance.points<price){reason="Gold tidak cukup.";return false;}
        if(dineIn && (status==null||status.IsFainted||!status.WouldBenefitFromFood(offer.item.healthRestore,offer.item.staminaRestore,offer.item.hungerRestore))){reason="Status sudah penuh atau player tidak dapat makan.";return false;}
        if(!dineIn&&(inventory==null||!inventory.CanAdd(offer.item,quantity,offer.quality))){reason="Tas tidak cukup ruang.";return false;}
        return true;
    }
    public void Order()
    {
        Refresh();if(!CanOrder(out var reason)){feedback=reason;Refresh();return;}
        var offer=products[selectedProduct];int price=offer.Price*quantity;
        if(price>0&&!ScoreManager.Instance.TrySpendPoints(price)){feedback="Gold tidak cukup.";Refresh();return;}
        if(dineIn){status.ApplyFood(offer.item.healthRestore,offer.item.staminaRestore,offer.item.hungerRestore);status.PulseActivity(PlayerMovementState.Eating,.65f);}
        else if(!inventory.Add(offer.item,quantity,offer.quality)){if(price>0)ScoreManager.Instance.AddPoints(price);feedback="Tas penuh. Gold dikembalikan.";Refresh();return;}
        feedback=dineIn?"Makanan disajikan. Selamat menikmati!":$"{offer.item.itemName} × {quantity} masuk ke tas.";Refresh();
    }
    protected override void Refresh()
    {
        if(uiRoot==null)return;Resolve();if(catalog==null)return;
        selectedCategory=Mathf.Clamp(selectedCategory,0,Mathf.Max(0,catalog.categories.Count-1));
        string category=catalog.categories.Count>0?catalog.categories[selectedCategory].id:"";
        products=catalog.menu.Where(p=>p!=null&&p.enabled&&p.item!=null&&p.categoryId==category).ToList();
        selectedProduct=Mathf.Clamp(selectedProduct,0,Mathf.Max(0,products.Count-1));
        var offer=products.Count>0?products[selectedProduct]:null;
        quantity=dineIn?1:Mathf.Clamp(quantity,1,offer?.maximumQuantity??1);
        var categoryRoot=Find<RectTransform>("Categories");
        // Reconcile category controls with Inspector/CSV data without imposing a fixed category count.
        if(categoryRoot.childCount!=catalog.categories.Count)
        {
            foreach(Transform child in categoryRoot)child.gameObject.SetActive(false);
            for(int i=categoryRoot.childCount-1;i>=0;i--){var child=categoryRoot.GetChild(i);child.SetParent(null,false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
            for(int i=0;i<catalog.categories.Count;i++){int index=i;float w=1f/Mathf.Max(1,catalog.categories.Count);var b=Button("Category_"+i,categoryRoot,catalog.categories[i].title,new(i*w+.004f,.04f),new((i+1)*w-.004f,.96f),()=>SelectCategory(index));ImageSlot("CategoryIconSlot_"+i,b.transform,new(.04f,.15f),new(.19f,.85f));}
        }
        for(int i=0;i<catalog.categories.Count;i++){var b=Find<Button>("Category_"+i);if(b!=null){var label=b.GetComponentInChildren<TMP_Text>();label.text=catalog.categories[i].title;label.rectTransform.anchorMin=new(catalog.categories[i].icon!=null?.22f:.06f,.10f);Paint(Find<Image>("CategoryIconSlot_"+i),catalog.categories[i].icon);Selected(b,i==selectedCategory);}}
        Find<TMP_Text>("Title").text=catalog.title;Find<TMP_Text>("Gold").text=$"{(ScoreManager.Instance!=null?ScoreManager.Instance.points:0):N0} G";
        Paint(Find<Image>("BackgroundImageSlot"),catalog.background);Paint(Find<Image>("MerchantPortraitSlot"),catalog.merchantPortrait);Paint(Find<Image>("ShopIconSlot"),catalog.shopIcon);Paint(Find<Image>("GoldIconSlot"),catalog.goldIcon);
        int page=selectedProduct/6;
        for(int i=0;i<6;i++)
        {
            int index=page*6+i;var p=index<products.Count?products[index]:null;var card=Find<Button>("Menu_"+i);card.gameObject.SetActive(products.Count==0||p!=null);card.interactable=p!=null;
            Selected(card,p!=null&&index==selectedProduct);card.GetComponentInChildren<TMP_Text>().text=p!=null?p.item.itemName+"\n"+p.Price.ToString("N0")+" G":"Kosong";
            Paint(Find<Image>("MenuImageSlot_"+i),p!=null?p.illustration!=null?p.illustration:p.item.inventoryIllustration!=null?p.item.inventoryIllustration:p.item.icon:null);
        }
        var item=offer?.item;
        Find<TMP_Text>("ProductName").text=item!=null?item.itemName:"Menu belum tersedia";
        Find<TMP_Text>("Description").text=item!=null?"Stamina +"+item.staminaRestore.ToString("0.#")+"   HP +"+item.healthRestore.ToString("0.#")+"\nKualitas: "+offer.quality:"Daftar akan terisi saat produk makanan ditambahkan ke katalog.";
        Paint(Find<Image>("ProductImageSlot"),offer?.illustration!=null?offer.illustration:item?.inventoryIllustration!=null?item.inventoryIllustration:item?.icon);
        Find<TMP_Text>("Quantity").text=quantity.ToString();Find<TMP_Text>("Total").text=offer!=null?$"{(long)offer.Price*quantity:N0} G":"—";
        var remaining=Find<TMP_Text>("RemainingGold");if(remaining!=null)remaining.text=offer!=null?$"Sisa uang: {System.Math.Max(0,(long)(ScoreManager.Instance!=null?ScoreManager.Instance.points:0)-(long)offer.Price*quantity):N0} G":"";
        Selected(Find<Button>("DineIn"),offer!=null&&dineIn);Selected(Find<Button>("Takeaway"),offer!=null&&!dineIn);
        Find<Button>("DineIn").interactable=offer!=null&&offer.allowDineIn;Find<Button>("Takeaway").interactable=offer!=null&&offer.allowTakeaway;
        Find<Button>("Minus").interactable=offer!=null&&!dineIn&&quantity>1;Find<Button>("Plus").interactable=offer!=null&&!dineIn&&quantity<offer.maximumQuantity;
        bool valid=CanOrder(out var reason);Find<Button>("Order").interactable=valid;
        Find<TMP_Text>("Status").text=valid?feedback:reason;Find<Button>("PrevProduct").interactable=products.Count>1;Find<Button>("NextProduct").interactable=products.Count>1;
    }
    public override void Build()
    {
        var safe=MakeCanvas("Cafe_UI_Editable");ImageSlot("BackgroundImageSlot",uiRoot.transform,Vector2.zero,Vector2.one).transform.SetAsFirstSibling();
        var header=Panel("Header",safe,new(.04f,.88f),new(.96f,.97f));
        Text("Title",header,"KAFE & RUMAH MAKAN",30,new(.08f,.10f),new(.70f,.90f));ImageSlot("ShopIconSlot",header,new(.025f,.15f),new(.065f,.85f));
        Text("Gold",header,"0 G",26,new(.78f,.1f),new(.96f,.9f)).alignment=TextAlignmentOptions.Right;ImageSlot("GoldIconSlot",header,new(.74f,.25f),new(.77f,.75f));
        Panel("Categories",safe,new(.06f,.815f),new(.53f,.87f),new Color(0,0,0,0));
        ImageSlot("MerchantPortraitSlot",safe,new(.70f,.48f),new(.91f,.81f));
        var carousel=Panel("MenuCarousel",safe,new(.10f,.28f),new(.90f,.47f));
        for(int i=0;i<6;i++){int slot=i;float x=.012f+i*.163f;var b=Button("Menu_"+i,carousel,"Kosong",new(x,.06f),new(x+.158f,.94f),()=>SelectProduct(selectedProduct/6*6+slot));
            b.GetComponentInChildren<TMP_Text>().rectTransform.anchorMax=new(.94f,.31f);ImageSlot("MenuImageSlot_"+i,b.transform,new(.12f,.35f),new(.88f,.95f));}
        Button("PrevProduct",safe,"‹",new(.055f,.34f),new(.09f,.405f),()=>MoveProduct(-1));Button("NextProduct",safe,"›",new(.91f,.34f),new(.945f,.405f),()=>MoveProduct(1));
        var detail=Panel("OrderDetails",safe,new(.10f,.10f),new(.90f,.26f));
        ImageSlot("ProductImageSlot",detail,new(.015f,.12f),new(.13f,.92f));Text("ProductName",detail,"",26,new(.15f,.56f),new(.47f,.94f));Text("Description",detail,"",18,new(.15f,.12f),new(.47f,.55f));
        Text("Total",detail,"—",26,new(.48f,.40f),new(.59f,.83f));
        Text("TotalLabel",detail,"Total",16,new(.48f,.12f),new(.59f,.36f));
        Text("QuantityLabel",detail,"Jumlah",16,new(.60f,.42f),new(.715f,.54f)).alignment=TextAlignmentOptions.Center;
        Button("Minus",detail,"−",new(.60f,.10f),new(.635f,.42f),()=>SetQuantity(quantity-1));Text("Quantity",detail,"1",22,new(.638f,.1f),new(.68f,.42f)).alignment=TextAlignmentOptions.Center;
        Button("Plus",detail,"+",new(.68f,.10f),new(.715f,.42f),()=>SetQuantity(quantity+1));
        Button("DineIn",detail,"Makan di sini",new(.60f,.55f),new(.73f,.92f),()=>SetDineIn(true));Button("Takeaway",detail,"Bawa pulang",new(.74f,.55f),new(.865f,.92f),()=>SetDineIn(false));
        Button("Order",detail,"Pesan",new(.88f,.55f),new(.98f,.92f),Order);
        Text("RemainingGold",detail,"",16,new(.74f,.10f),new(.98f,.42f)).alignment=TextAlignmentOptions.Right;
        Text("Status",safe,"",20,new(.10f,.057f),new(.88f,.095f));
        Text("Keyboard",safe,"A / D Pilih menu     W / S Kategori     + / − Jumlah     Enter Pesan     Esc Tutup",18,new(.10f,.015f),new(.79f,.053f));
        Button("Close",safe,"Tutup ×",new(.81f,.015f),new(.90f,.053f),Close);uiRoot.SetActive(false);
    }
}
