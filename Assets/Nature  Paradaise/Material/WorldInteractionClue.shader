Shader "NatureParadise/WorldInteractionClue"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Occlusion independent interaction clue"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionHCS = TransformObjectToHClip(input.positionOS.xyz); output.color = input.color * _Color; return output;
            }
            half4 Frag(Varyings input) : SV_Target { return input.color; }
            ENDHLSL
        }
    }
}
