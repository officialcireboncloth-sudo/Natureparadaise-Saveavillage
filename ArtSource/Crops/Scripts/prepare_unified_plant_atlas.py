"""Snapshot first, lock the allocation plan, and create one plant-only instanced shader."""
from unified_plant_atlas import *
before_merge();registry=plan(reset_unpublished=not (REVIEW/'Packing.json').exists())
text=(A/'Material/Shared/CozyAtlas.shader').read_text().replace('Shader "Nature Paradise/Cozy Atlas"','Shader "Nature Paradise/Plants Shared Atlas"')
text=text.replace('            half4 _BaseColor;\n','').replace('            half _PresentationFade;\n','')
text=text.replace('        TEXTURE2D(_BaseMap);', '''        // Lifecycle tint and pickup fade are instance data, never cloned materials.
        UNITY_INSTANCING_BUFFER_START(PlantInstances)
            UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
            UNITY_DEFINE_INSTANCED_PROP(half, _PresentationFade)
        UNITY_INSTANCING_BUFFER_END(PlantInstances)
        TEXTURE2D(_BaseMap);''')
text=text.replace('            // Per-cart property block; shared atlas materials remain fully visible by default.','''            UNITY_SETUP_INSTANCE_ID(input);
            half presentationFade = UNITY_ACCESS_INSTANCED_PROP(PlantInstances, _PresentationFade);
            // Defaults remain opaque; fade and tint can vary per instance.''')
text=text.replace('if (_PresentationFade < 1)','if (presentationFade < 1)').replace('clip(_PresentationFade -','clip(presentationFade -').replace('input.uv) * _BaseColor','input.uv) * UNITY_ACCESS_INSTANCED_PROP(PlantInstances, _BaseColor)')
text=text.replace('half3 Normal(Varyings input, half face)','half3 Normal(Varyings input, half face, half relief)').replace('input.uv),_BumpScale)','input.uv),relief)')
text=text.replace('''            half luma = dot(c.rgb,half3(.2126h,.7152h,.0722h));
            half3 albedo = lerp(luma.xxx,c.rgb,_Saturation);
            half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1));''','''            // R: metallic, G: original normal strength, B: original saturation / 2,
            // A: original smoothness. One lookup retains each source group's look.
            half4 surface = SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,input.uv);
            half luma = dot(c.rgb,half3(.2126h,.7152h,.0722h));
            half3 albedo = lerp(luma.xxx,c.rgb,surface.b * 2 * _Saturation);
            half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1),surface.g * _BumpScale);''')
text=text.replace('half4 mask = SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,input.uv);','half4 mask = surface;')
text=text.replace('half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1));','half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1),SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,input.uv).g * _BumpScale);')
assert 'half3 Normal(Varyings input, half face, half relief)' in text and text.count('UNITY_ACCESS_INSTANCED_PROP(PlantInstances, _BaseColor)')==1
SHADER.parent.mkdir(parents=True,exist_ok=True);SHADER.write_text(text);material_metadata();registry['materialGuid']=guid(MATERIAL);write(REGISTRY,registry)
print('UNIFIED_PLAN_READY',len(registry['slots']),'surfaces;',registry['freePercent'],'percent reserved; one material',flush=True)
