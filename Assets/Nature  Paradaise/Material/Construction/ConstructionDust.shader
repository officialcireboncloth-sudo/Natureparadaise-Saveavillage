Shader "Nature Paradise/Construction Dust"
{
    Properties { _GroundY("Ground height",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _GroundY;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;half fog:TEXCOORD1;float height:TEXCOORD2;};
            V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.positionCS.z);o.height=TransformObjectToWorld(a.positionOS.xyz).y-_GroundY;return o;}
            half4 frag(V i):SV_Target{float2 p=i.uv*2-1;float alpha=pow(saturate(1-dot(p,p)),1.5)*smoothstep(-.05,.25,i.height);return half4(MixFog(i.color.rgb,i.fog),i.color.a*alpha);}
            ENDHLSL
        }
    }
}
