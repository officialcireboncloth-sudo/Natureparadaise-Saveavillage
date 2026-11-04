using System.Collections.Generic;
using UnityEngine;

/// <summary>Temporary per-cart fade, with exact restoration of renderer materials/property blocks.</summary>
public sealed class MarketCartFade
{
    sealed class Binding
    {
        public Renderer renderer;
        public Material[] original, fading;
        public MaterialPropertyBlock[] saved;
    }
    readonly List<Binding> bindings=new();
    readonly List<Material> temporary=new();
    MaterialPropertyBlock block;
    static readonly int Fade=Shader.PropertyToID("_PresentationFade"),ColorId=Shader.PropertyToID("_BaseColor");
    public void Begin(Renderer[] renderers)
    {
        block ??= new MaterialPropertyBlock();
        Restore();
        foreach(var renderer in renderers)
        {
            if(!renderer)continue;
            var binding=new Binding{renderer=renderer,original=renderer.sharedMaterials};
            binding.fading=(Material[])binding.original.Clone();binding.saved=new MaterialPropertyBlock[binding.original.Length];
            for(int i=0;i<binding.original.Length;i++)
            {
                binding.saved[i]=new MaterialPropertyBlock();renderer.GetPropertyBlock(binding.saved[i],i);
                var original=binding.original[i];if(!original||original.HasProperty(Fade))continue;
                // Transparent glass already supports alpha. Opaque keeper skin uses the same
                // textured lit fade as the cart only for this short transition.
                if(original.HasProperty("_Surface")&&original.GetFloat("_Surface")>0)continue;
                var shader=Shader.Find("Nature Paradise/Cozy Atlas");if(!shader)continue;
                var material=new Material(shader){name=original.name+" (temporary cart fade)",hideFlags=HideFlags.HideAndDontSave};
                if(original.HasProperty("_BaseMap")){material.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));material.SetTextureScale("_BaseMap",original.GetTextureScale("_BaseMap"));material.SetTextureOffset("_BaseMap",original.GetTextureOffset("_BaseMap"));}
                material.SetColor(ColorId,original.HasProperty(ColorId)?original.GetColor(ColorId):Color.white);
                material.SetFloat("_UseMask",0);material.SetFloat("_UseNormal",0);material.SetFloat("_Wrap",.12f);material.SetFloat("_Saturation",1);material.SetFloat("_AmbientStrength",1);
                if(original.HasProperty("_Cull"))material.SetFloat("_Cull",original.GetFloat("_Cull"));
                binding.fading[i]=material;temporary.Add(material);
            }
            renderer.sharedMaterials=binding.fading;bindings.Add(binding);
        }
        Set(1);
    }
    public void Set(float alpha)
    {
        alpha=Mathf.Clamp01(alpha);
        foreach(var binding in bindings)
        {
            if(!binding.renderer)continue;
            for(int i=0;i<binding.fading.Length;i++)
            {
                var material=binding.fading[i];if(!material)continue;
                binding.renderer.GetPropertyBlock(block,i);
                if(material.HasProperty(Fade))block.SetFloat(Fade,alpha);
                else if(material.HasProperty(ColorId)){var color=material.GetColor(ColorId);color.a*=alpha;block.SetColor(ColorId,color);}
                binding.renderer.SetPropertyBlock(block,i);
            }
        }
    }
    public void Restore()
    {
        foreach(var binding in bindings)if(binding.renderer)
        {
            binding.renderer.sharedMaterials=binding.original;
            for(int i=0;i<binding.saved.Length;i++)binding.renderer.SetPropertyBlock(binding.saved[i],i);
        }
        bindings.Clear();foreach(var material in temporary)if(material)Object.Destroy(material);temporary.Clear();
    }
}
