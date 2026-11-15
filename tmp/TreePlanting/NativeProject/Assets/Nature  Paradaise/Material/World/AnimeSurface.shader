Shader "Nature Paradise/Anime Surface"
{
    Properties
    {
        [MainTexture] _TextureSample("Base Texture", 2D) = "white" {}
        [MainColor] _Color("Color Tint", Color) = (1,1,1,1)
        _ShadowColor("Shadow Tint", Color) = (0.64,0.72,0.88,1)
        _ShadeThreshold("Toon Threshold", Range(0,1)) = 0.52
        _ShadeSoftness("Toon Edge Softness", Range(0.01,0.5)) = 0.12
        _ShadeStrength("Form Shadow Strength", Range(0,1)) = 0.45
        _CastShadowStrength("Received Shadow Strength", Range(0,1)) = 0.7
        _AmbientInfluence("Environment Color Influence", Range(0,1)) = 0.25
        _Brightness("Surface Brightness", Range(0.2,2)) = 1.1
        _RimColor("Rim Color", Color) = (0.82,0.9,1,1)
        _RimStrength("Soft Rim Strength", Range(0,0.5)) = 0.06
        [Toggle] _SkinEnabled("Character Skin Treatment", Float) = 0
        _SkinMask("Skin Mask (white = skin)", 2D) = "white" {}
        [Toggle] _AutoSkinMask("Detect Warm Skin Colors Within Mask", Float) = 1
        _SkinColor("Clean Skin Color", Color) = (1,0.77,0.61,1)
        _SkinCleanliness("Skin Texture Smoothing", Range(0,1)) = 0.25
        _SkinBrightness("Skin Brightness", Range(1,1.6)) = 1.12
        _SkinShadowStrength("Skin Shadow Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _TextureSample_ST;
            half4 _Color, _ShadowColor, _RimColor, _SkinColor;
            half _ShadeThreshold, _ShadeSoftness, _ShadeStrength, _CastShadowStrength;
            half _AmbientInfluence, _Brightness, _RimStrength, _SkinEnabled;
            half _AutoSkinMask, _SkinCleanliness, _SkinBrightness, _SkinShadowStrength;
        CBUFFER_END
        TEXTURE2D(_TextureSample); SAMPLER(sampler_TextureSample);
        TEXTURE2D(_SkinMask); SAMPLER(sampler_SkinMask);
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fog : TEXCOORD3;
            float4 shadowCoord : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
            o.positionCS = p.positionCS; o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(input.normalOS);
            o.uv = TRANSFORM_TEX(input.uv,_TextureSample);
            o.fog = ComputeFogFactor(p.positionCS.z);
            o.shadowCoord = GetShadowCoord(p);
            return o;
        }
        half WarmSkinMask(half3 color)
        {
            // Conservative fallback for the current single-atlas model. An authored
            // mask can replace this heuristic via Auto Skin Mask = off.
            half warm = smoothstep(0.015h,0.06h,color.r-color.g) * smoothstep(0.015h,0.06h,color.g-color.b);
            return warm * smoothstep(0.25h,0.5h,color.r) * smoothstep(0.12h,0.25h,color.g);
        }
        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 base = SAMPLE_TEXTURE2D(_TextureSample,sampler_TextureSample,input.uv).rgb * _Color.rgb;
            half mask = SAMPLE_TEXTURE2D(_SkinMask,sampler_SkinMask,input.uv).r * _SkinEnabled;
            mask *= lerp(1.0h,WarmSkinMask(base),_AutoSkinMask);
            base = lerp(base,lerp(base,_SkinColor.rgb,_SkinCleanliness) * _SkinBrightness,mask);
            half3 n = normalize(input.normalWS);
            half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
            float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                shadowCoord = input.shadowCoord;
            #endif
            Light sun = GetMainLight(shadowCoord,input.positionWS,half4(1,1,1,1));
            half brightness = max(dot(sun.color,half3(.2126h,.7152h,.0722h)),.001h);
            half3 sunTint = sun.color / max(max(sun.color.r,sun.color.g),max(sun.color.b,.001h));
            half3 ambient = max(SampleSH(n),half3(.001h,.001h,.001h));
            half3 fillTint = ambient / max(max(ambient.r,ambient.g),ambient.b);
            half energy = clamp(.45h + brightness * .4h,.45h,1.25h);
            half lit = smoothstep(_ShadeThreshold-_ShadeSoftness,_ShadeThreshold+_ShadeSoftness,dot(n,sun.direction)*.5h+.5h);
            half formStrength = lerp(_ShadeStrength,_SkinShadowStrength,mask);
            half castStrength = lerp(_CastShadowStrength,_SkinShadowStrength,mask);
            half shade = max((1-lit)*formStrength,(1-sun.shadowAttenuation)*castStrength);
            half3 shadeTint = lerp(half3(1,1,1),_ShadowColor.rgb,shade);
            half shadeValue = 1-shade*.48h;
            half3 lightingTint = lerp(sunTint,fillTint,_AmbientInfluence);
            half3 result = base * shadeTint * shadeValue * lightingTint * energy * _Brightness;
            // Restrained additional lights retain indoor lamps without washing out the ramp.
            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if USE_FORWARD_PLUS
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
                {
                    FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1));
                    result += base * light.color * saturate(dot(n,light.direction)) * light.shadowAttenuation * .35h;
                }
                #endif
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light light = GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1));
                    result += base * light.color * saturate(dot(n,light.direction)) * light.distanceAttenuation * light.shadowAttenuation * .35h;
                LIGHT_LOOP_END
            #endif
            half rim = smoothstep(.65h,.95h,1-saturate(dot(n,view))) * _RimStrength;
            result += base * _RimColor.rgb * rim * energy * lerp(.2h,1.0h,lit*sun.shadowAttenuation);
            return half4(MixFog(result,input.fog),1);
        }
        half4 DepthFrag(Varyings input) : SV_Target { return 0; }
        half4 NormalFrag(Varyings input) : SV_Target { return half4(normalize(input.normalWS),0); }
        ENDHLSL
        Pass
        {
            Name "AnimeForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings o = Vert(input);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction = normalize(_LightPosition-o.positionWS);
                #endif
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z,o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z,o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
