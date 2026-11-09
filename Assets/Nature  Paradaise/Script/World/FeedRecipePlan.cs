using System;
using System.Collections.Generic;

/// <summary>Plan every required ingredient against copies; callers commit once after all checks pass.</summary>
public static class FeedRecipePlan
{
    public static int Available(IReadOnlyList<ItemStack> slots,FeedIngredient ingredient)
    {
        if(slots==null||ingredient==null)return 0;
        long total=0;foreach(var stack in slots)if(ingredient.Matches(stack))total+=stack.count;
        return (int)Math.Min(int.MaxValue,total);
    }
    public static bool TryPrepare(IReadOnlyList<ItemStack> slots,int capacity,FeedRecipe recipe,int batches,
        out List<ItemStack> prepared,out string reason)
    {
        prepared=null;reason="Data resep belum lengkap";
        if(slots==null||capacity<1||slots.Count>capacity||recipe==null||recipe.TotalInputCount<1||batches<1||batches>99||
            (long)recipe.TotalInputCount*batches>int.MaxValue)return false;
        var result=new List<ItemStack>(capacity);
        for(int i=0;i<capacity;i++)
        {
            var stack=i<slots.Count?slots[i]:null;
            if(stack?.item!=null&&stack.count>stack.item.StackLimit)
            {reason="Stack bahan melebihi batas; rapikan inventory terlebih dahulu";return false;}
            result.Add(stack?.item==null||stack.count<1?null:new ItemStack{item=stack.item,count=stack.count,
                qualityStars=stack.qualityStars,fishSizeCm=stack.fishSizeCm,fishWeightKg=stack.fishWeightKg});
        }
        if(recipe.HasIngredients)
        {
            foreach(var ingredient in recipe.ingredients)
            {
                int remaining=ingredient.amount*batches;
                for(int i=result.Count-1;i>=0&&remaining>0;i--)
                {
                    var stack=result[i];if(!ingredient.Matches(stack))continue;
                    int used=Math.Min(remaining,stack.count);stack.count-=used;remaining-=used;
                    if(stack.count==0)result[i]=null;
                }
                if(remaining>0){reason="Bahan belum cukup: "+ingredient.DisplayName;return false;}
            }
        }
        else
        {
            // Old authored recipes can still be inspected without changing saved production jobs.
            var allowed=recipe.mixedInputs!=null&&recipe.mixedInputs.Length>0?recipe.mixedInputs:new[]{recipe.input};
            int remaining=recipe.inputCount*batches;
            foreach(var item in allowed)
            {
                if(item==null)continue;
                for(int i=result.Count-1;i>=0&&remaining>0;i--)
                {
                    var stack=result[i];if(stack?.item!=item)continue;
                    int used=Math.Min(remaining,stack.count);stack.count-=used;remaining-=used;
                    if(stack.count==0)result[i]=null;
                }
            }
            if(remaining>0){reason="Bahan belum cukup";return false;}
        }
        prepared=result;reason="";return true;
    }
}
