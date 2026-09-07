Shader "Nature Paradise/Soil State"
{
    Properties
    {
        _BaseColor("Dry Soil Color", Color) = (0.302, 0.224, 0.224, 1)
        _WetColorMultiplier("Wet Color Multiplier", Color) = (0.58, 0.62, 0.68, 1)
        _DrySmoothness("Dry Smoothness", Range(0, 1)) = 0.03
        _WetSmoothness("Wet Smoothness", Range(0, 1)) = 0.58
        [PerRendererData] _Wetness("Wetness", Range(0, 1)) = 0
        [PerRendererData] _Fertilized("Fertilized", Range(0, 1)) = 0
        _FertilizerColor("Fertilizer Color", Color) = (0.95, 0.94, 0.86, 0.88)
        _FertilizerTiling("Fertilizer Grain Tiling", Range(4, 40)) = 22
        _FertilizerDensity("Fertilizer Grain Density", Range(0, 1)) = 0.34
        _FertilizerSize("Fertilizer Grain Size", Range(0.03, 0.35)) = 0.13
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _WetColorMultiplier;
                half _DrySmoothness;
                half _WetSmoothness;
                half4 _FertilizerColor;
                float _FertilizerTiling;
                half _FertilizerDensity;
                half _FertilizerSize;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(SoilPerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _Wetness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Fertilized)
            UNITY_INSTANCING_BUFFER_END(SoilPerInstance)

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
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half FertilizerGrains(float2 uv)
            {
                float2 grid = uv * _FertilizerTiling;
                float2 cell = floor(grid);
                float2 cellUv = frac(grid) - 0.5;
                float2 offset = (float2(Hash21(cell + 1.7), Hash21(cell + 8.3)) - 0.5) * 0.55;
                float randomValue = Hash21(cell + 19.1);
                float radius = _FertilizerSize * lerp(0.65, 1.25, Hash21(cell + 31.7));
                float grain = 1.0 - smoothstep(radius, radius + 0.035, length(cellUv - offset));
                return (half)(grain * step(1.0 - _FertilizerDensity, randomValue));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half wetness = saturate(UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _Wetness));
                half fertilized = saturate(UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _Fertilized));
                half3 normalWS = normalize(input.normalWS);
                half topMask = smoothstep(0.35h, 0.8h, normalWS.y);
                half grains = FertilizerGrains(input.uv) * fertilized * topMask;

                half3 soilColor = _BaseColor.rgb * lerp(half3(1, 1, 1), _WetColorMultiplier.rgb, wetness);
                soilColor = lerp(soilColor, _FertilizerColor.rgb, grains * _FertilizerColor.a);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = soilColor;
                surfaceData.alpha = 1;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.04h, 0.04h, 0.04h);
                surfaceData.smoothness = lerp(_DrySmoothness, _WetSmoothness, wetness);
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
