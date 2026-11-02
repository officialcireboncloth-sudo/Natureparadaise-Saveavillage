Shader "Nature Paradise/Cozy Atlas"
{
    Properties
    {
        [MainTexture] _BaseMap("Atlas Colour", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Atlas Normal", 2D) = "bump" {}
        _BumpScale("Normal Relief", Range(0,1.5)) = 0.55
        _MetallicGlossMap("Atlas Metal (R), Smoothness (A)", 2D) = "black" {}
        _Smoothness("Smoothness Scale", Range(0,1)) = 1
        _Wrap("Soft Diffuse Wrap", Range(0,0.4)) = 0.12
        _Saturation("Texture Saturation", Range(0,1.2)) = 0.97
        _AmbientStrength("Environment Fill", Range(0,2)) = 1
        _Cutoff("Foliage Cutout", Range(0,1)) = 0.42
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        [Toggle(_NORMALMAP)] _UseNormal("Use Atlas Normal", Float) = 1
        [Toggle(_METALLICSPECGLOSSMAP)] _UseMask("Use Surface Mask", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale, _Smoothness, _Wrap, _Saturation, _AmbientStrength, _Cutoff;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            half fog : TEXCOORD4;
            half3 vertexLight : TEXCOORD5;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs n = GetVertexNormalInputs(input.normalOS,input.tangentOS);
            o.positionCS = p.positionCS; o.positionWS = p.positionWS;
            o.normalWS = n.normalWS;
            o.tangentWS = half4(n.tangentWS,input.tangentOS.w * GetOddNegativeScale());
            o.uv = TRANSFORM_TEX(input.uv,_BaseMap);
            o.fog = ComputeFogFactor(p.positionCS.z);
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                o.vertexLight = VertexLighting(p.positionWS,n.normalWS);
            #endif
            return o;
        }
        half4 Atlas(Varyings input)
        {
            half4 c = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv) * _BaseColor;
            #if defined(_ALPHATEST_ON)
                clip(c.a-_Cutoff);
            #endif
            return c;
        }
        half3 Normal(Varyings input, half face)
        {
            half3 n = normalize(input.normalWS);
            #if defined(_NORMALMAP)
                half3 t = normalize(input.tangentWS.xyz);
                half3 b = cross(n,t) * input.tangentWS.w;
                half3 ts = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale);
                n = normalize(TransformTangentToWorld(ts,half3x3(t,b,n)));
            #endif
            return n * face;
        }
        half3 Direct(Light light, half3 n, half3 view, half3 albedo, half metal, half smoothness)
        {
            half ndl = dot(n,light.direction);
            half diffuse = saturate((ndl+_Wrap)/(1+_Wrap));
            // Broad diffuse preserves painted detail. Only metal receives a strong highlight.
            half3 halfway = SafeNormalize(light.direction+view);
            half power = exp2(3+smoothness*6);
            half spec = pow(saturate(dot(n,halfway)),power) * (power+2)/32;
            half3 f0 = lerp(half3(.025h,.025h,.025h),albedo,metal);
            half3 brdf = albedo*(1-metal*.75h)*diffuse + f0*spec*saturate(ndl);
            return brdf * light.color * light.distanceAttenuation * light.shadowAttenuation;
        }
        half4 Frag(Varyings input, FRONT_FACE_TYPE front : FRONT_FACE_SEMANTIC) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 c = Atlas(input);
            half luma = dot(c.rgb,half3(.2126h,.7152h,.0722h));
            half3 albedo = lerp(luma.xxx,c.rgb,_Saturation);
            half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1));
            half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
            half metal = 0, smoothness = .1h;
            #if defined(_METALLICSPECGLOSSMAP)
                half4 mask = SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,input.uv);
                metal = mask.r; smoothness = mask.a*_Smoothness;
            #endif
            float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                shadowCoord = float4(GetNormalizedScreenSpaceUV(input.positionCS),0,1);
            #endif
            half indirectAO=1, directAO=1;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                indirectAO=ao.indirectAmbientOcclusion; directAO=ao.directAmbientOcclusion;
            #endif
            half3 result = albedo*max(SampleSH(n),0)*_AmbientStrength*(1-metal*.65h)*indirectAO;
            result += Direct(GetMainLight(shadowCoord,input.positionWS,half4(1,1,1,1)),n,view,albedo,metal,smoothness)*directAO;
            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if USE_FORWARD_PLUS
                    for (uint lightIndex=0; lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS); ++lightIndex)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                        result += Direct(GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1)),n,view,albedo,metal,smoothness)*directAO;
                    }
                #endif
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    result += Direct(GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1)),n,view,albedo,metal,smoothness)*directAO;
                LIGHT_LOOP_END
            #elif defined(_ADDITIONAL_LIGHTS_VERTEX)
                result += albedo*input.vertexLight;
            #endif
            return half4(MixFog(result,input.fog),1);
        }
        half4 DepthFrag(Varyings input) : SV_Target { Atlas(input); return 0; }
        half4 NormalFrag(Varyings input, FRONT_FACE_TYPE front : FRONT_FACE_SEMANTIC) : SV_Target
        {
            Atlas(input);
            half3 n = Normal(input,IS_FRONT_VFACE(front,1,-1));
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
            #else
                return half4(n,0);
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "CozyForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
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
                    o.positionCS.z = min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
