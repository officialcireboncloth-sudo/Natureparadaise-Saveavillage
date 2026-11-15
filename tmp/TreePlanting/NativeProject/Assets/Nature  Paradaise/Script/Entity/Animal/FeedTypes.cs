using UnityEngine;

/// <summary>Stable feed categories. None is reserved for ordinary materials such as raw grass.</summary>
public enum FeedKind { None, Livestock, Poultry, Fish }

/// <summary>Shared feed identity for inventory, production and receiving stations; legacy item IDs remain valid.</summary>
public static class FeedTypes
{
    public const string WrongTypeMessage="Jenis pakan tidak sesuai";
    public static FeedKind Kind(ItemSO item)
    {
        if(item==null)return FeedKind.None;
        if(item.feedKind!=FeedKind.None)return item.feedKind;
        return item.itemId switch {"item.animal_feed"=>FeedKind.Livestock,"item.poultry_feed"=>FeedKind.Poultry,"item.fish_feed"=>FeedKind.Fish,_=>FeedKind.None};
    }
    public static FeedKind ForHousing(AnimalHousingKind housing)=>housing switch
        {AnimalHousingKind.Barn=>FeedKind.Livestock,AnimalHousingKind.Coop=>FeedKind.Poultry,_=>FeedKind.None};
    public static FeedKind ForAnimal(AnimalType type)=>AnimalGrowthProfileSO.IsBird(type)?FeedKind.Poultry:FeedKind.Livestock;
    public static string Label(FeedKind kind)=>kind switch
        {FeedKind.Livestock=>"Pakan Hewan",FeedKind.Poultry=>"Pakan Unggas",FeedKind.Fish=>"Pakan Ikan",_=>"Pakan"};
    public static ItemSO Item(FeedKind kind)=>kind switch
    {
        FeedKind.Livestock=>Resources.Load<ItemSO>("Items/Materials/Animal Feed"),
        FeedKind.Poultry=>Resources.Load<ItemSO>("Items/Materials/Poultry Feed"),
        FeedKind.Fish=>Resources.Load<ItemSO>("Items/Fish/Fish Feed"),_=>null
    };
}
