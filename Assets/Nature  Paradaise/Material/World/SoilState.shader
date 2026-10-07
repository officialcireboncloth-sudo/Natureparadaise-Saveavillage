Shader "Nature Paradise/Soil State"
{
    Properties
    {
        _BaseColor("Dry Soil Color", Color) = (0.302, 0.224, 0.224, 1)
        _WetColorMultiplier("Wet Color Multiplier", Color) = (0.58, 0.62, 0.68, 1)
        _DrySmoothness("Dry Smoothness", Range(0, 1)) = 0.03
        _WetSmoothness("Wet Smoothness", Range(0, 1)) = 0.58
        _WetEdgeWidth("Wet-to-Dry Edge Blend (metres)", Range(0.01, 0.6)) = 0.28
        [PerRendererData] _Wetness("Wetness", Range(0, 1)) = 0
        [PerRendererData] _Fertilized("Fertilized", Range(0, 1)) = 0
        _FertilizerColor("Fertilizer Color", Color) = (0.95, 0.94, 0.86, 0.88)
        _FertilizerTiling("Fertilizer Grain Tiling", Range(4, 40)) = 22
        _FertilizerDensity("Fertilizer Grain Density", Range(0, 1)) = 0.34
        _FertilizerSize("Fertilizer Grain Size", Range(0.03, 0.35)) = 0.13
        _GroundMap("Ground Texture", 2D) = "white" {}
        _FieldGroundMap("Underlying Field Texture", 2D) = "white" {}
        _FieldBaseColor("Underlying Field Tint", Color) = (1,1,1,1)
        _FieldUVScale("Underlying Field UV Scale", Float) = 0.125
        _FieldTextureAmount("Underlying Field Texture Amount", Range(0,1)) = 1
        _TileBlend("Blend Tile Into Field", Float) = 0
        _WetGroundMap("Watered Tile Texture", 2D) = "white" {}
        _WetTextureAmount("Watered Texture Amount", Range(0, 1)) = 0
        _WetGroundUVRotation("Watered Texture Rotation Matrix", Vector) = (1, 0, 0, 1)
        _TextureAmount("Ground Texture Amount", Range(0, 1)) = 0
        _GroundUVTransform("Field Texture Scale / Offset", Vector) = (0.125, 0.125, 0, 0)
        _Furrows("Tilled Soil Detail", Range(0, 0.3)) = 0
        [PerRendererData] _SurfaceSize("Surface Size", Vector) = (1, 1, 0, 0)
        [PerRendererData] _GroundUVOffset("Cell Centre in Field (metres)", Vector) = (0, 0, 0, 0)
        [PerRendererData] _EdgeWidth("Edge Feather (metres)", Float) = 0.06
        [PerRendererData] _CornerRadius("Corner Radius (metres)", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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
                float _WetEdgeWidth;
                half4 _FertilizerColor;
                float _FertilizerTiling;
                half _FertilizerDensity;
                half _FertilizerSize;
                float4 _GroundUVTransform;
                half _TextureAmount;
                half _WetTextureAmount;
                float4 _WetGroundUVRotation;
                half _Furrows;
                half4 _FieldBaseColor;
                float _FieldUVScale;
                half _FieldTextureAmount, _TileBlend;
            CBUFFER_END
            TEXTURE2D(_GroundMap); SAMPLER(sampler_GroundMap);
            TEXTURE2D(_WetGroundMap); SAMPLER(sampler_WetGroundMap);
            TEXTURE2D(_FieldGroundMap); SAMPLER(sampler_FieldGroundMap);

            UNITY_INSTANCING_BUFFER_START(SoilPerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _Wetness)
                UNITY_DEFINE_INSTANCED_PROP(float, _Fertilized)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SurfaceSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GroundUVOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _EdgeWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _CornerRadius)
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

                float2 size = max(UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _SurfaceSize).xy, 0.01);
                float edge = max(UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _EdgeWidth), 0.001);
                float radius = min(UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _CornerRadius), min(size.x, size.y) * 0.45);
                float2 q = abs((input.uv - 0.5) * size) - size * 0.5 + radius;
                float distance = length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
                // Continuous world noise breaks a straight rectangular outline without flicker.
                float noise = sin(input.positionWS.x * 3.3 + sin(input.positionWS.z * 2.1)) * sin(input.positionWS.z * 2.7);
                half coverage = 1 - smoothstep(-edge, 0, distance + noise * edge * 0.18);
                clip(coverage - 0.003);

                // Keep a dry hoed transition beneath the watered centre. This blends
                // wet texture, colour and gloss together rather than fading only colour.
                float wetEdge = min(max(_WetEdgeWidth, edge), min(size.x, size.y) * .4);
                half wetCoverage = 1 - smoothstep(-wetEdge, -edge * .25, distance + noise * wetEdge * .16);
                wetness *= wetCoverage;

                half3 soilColor = _BaseColor.rgb * lerp(half3(1, 1, 1), _WetColorMultiplier.rgb, wetness);
                // Local metres keep texture size independent of field dimensions and
                // follow field rotation. Cell offsets keep neighbouring stamps continuous.
                float2 groundUV = ((input.uv - 0.5) * size + UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _GroundUVOffset).xy) * _GroundUVTransform.xy + _GroundUVTransform.zw;
                half3 ground = SAMPLE_TEXTURE2D(_GroundMap, sampler_GroundMap, groundUV).rgb;
                float2 wetUV = float2(dot(_WetGroundUVRotation.xy, groundUV), dot(_WetGroundUVRotation.zw, groundUV));
                half3 wetGround = SAMPLE_TEXTURE2D(_WetGroundMap, sampler_WetGroundMap, wetUV).rgb;
                ground = lerp(ground, wetGround, wetness * _WetTextureAmount);
                soilColor *= lerp(half3(1, 1, 1), ground, _TextureAmount);
                half furrow = sin(input.uv.y * 48 + sin(input.uv.x * 12) * .45) * .5 + .5;
                soilColor *= 1 - _Furrows * furrow;
                soilColor = lerp(soilColor, _FertilizerColor.rgb, grains * _FertilizerColor.a);

                // Match the actual field's texture coordinates and shading at the boundary.
                // Alpha feather alone left a dark rectangular stamp at gameplay distance.
                if (_TileBlend > .5h)
                {
                    float2 fieldUV = ((input.uv - .5) * size + UNITY_ACCESS_INSTANCED_PROP(SoilPerInstance, _GroundUVOffset).xy) * _FieldUVScale;
                    half3 fieldGround = SAMPLE_TEXTURE2D(_FieldGroundMap, sampler_FieldGroundMap, fieldUV).rgb;
                    half3 fieldColor = _FieldBaseColor.rgb * lerp(half3(1,1,1), fieldGround, _FieldTextureAmount);
                    soilColor = lerp(fieldColor, soilColor, coverage);
                }

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = soilColor;
                surfaceData.alpha = 1;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.04h, 0.04h, 0.04h);
                surfaceData.smoothness = lerp(_DrySmoothness, _WetSmoothness, wetness * lerp(1, coverage, _TileBlend));
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
                // Texture mixing handles the broad transition; alpha only hides the last
                // centimetres of geometry where it meets the identical field underneath.
                color.a = _TileBlend > .5h ? 1 - smoothstep(-min(edge,.04), 0, distance + noise * edge * .18) : coverage;
                return color;
            }
            ENDHLSL
        }

    }
}
