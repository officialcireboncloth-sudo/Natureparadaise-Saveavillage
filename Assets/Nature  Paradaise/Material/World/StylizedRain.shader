Shader "Nature Paradise/Weather/Stylized Rain"
{
    Properties
    {
        [HDR] _BaseColor("Water Tint", Color) = (0.42, 0.68, 1.0, 1.0)
        _Opacity("Opacity", Range(0, 1)) = 0.82
        _CoreWidth("Bright Core Width", Range(0.01, 0.45)) = 0.12
        _EdgeWidth("Soft Edge Width", Range(0.1, 1)) = 0.72
        _EndSoftness("End Softness", Range(0.01, 0.45)) = 0.15
        _CoreBrightness("Core Brightness", Range(0, 1.5)) = 0.42
        _HeadStrength("Drop Head Strength", Range(0, 0.5)) = 0.14
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "StylizedRainForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Opacity;
                half _CoreWidth;
                half _EdgeWidth;
                half _EndSoftness;
                half _CoreBrightness;
                half _HeadStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half horizontal = abs(input.uv.x * 2.0h - 1.0h);
                half softBody = 1.0h - smoothstep(_CoreWidth, _EdgeWidth, horizontal);
                half brightCore = 1.0h - smoothstep(0.0h, _CoreWidth, horizontal);
                half startFade = smoothstep(0.0h, _EndSoftness, input.uv.y);
                half endFade = 1.0h - smoothstep(1.0h - _EndSoftness, 1.0h, input.uv.y);
                half verticalFade = startFade * endFade;

                // Bulatan kecil pada leading edge membuat streak tetap terbaca sebagai
                // water drop, bukan garis putih padat saat kamera sedang zoom-out.
                half2 dropUv = half2((input.uv.x - 0.5h) * 2.8h, (input.uv.y - 0.13h) * 5.5h);
                half dropHead = 1.0h - smoothstep(0.45h, 1.0h, length(dropUv));
                half shape = saturate(softBody * verticalFade + dropHead * _HeadStrength);

                half4 tint = _BaseColor * input.color;
                half3 color = tint.rgb * (1.0h + brightCore * _CoreBrightness);
                half alpha = shape * tint.a * _Opacity;
                clip(alpha - 0.004h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
