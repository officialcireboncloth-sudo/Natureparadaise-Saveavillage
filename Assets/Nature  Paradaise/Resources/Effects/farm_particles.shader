Shader "Nature Paradise/Farm Particles"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        // Field dirt and hoed stamps render at 3000/3001 without writing depth.
        // Draw particles afterwards so the soil cannot paint over airborne debris.
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.color=v.color*_BaseColor;return o;}
            half4 Frag(Varyings i):SV_Target{return i.color;}
            ENDHLSL
        }
    }
}
