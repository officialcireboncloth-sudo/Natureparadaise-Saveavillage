Shader "Nature Paradise/Weather/Soft Rain"
{
    Properties { _Splash("Surface splash", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Splash;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world); o.color=input.color; o.uv=input.uv;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half2 uv = i.uv * 2 - 1;
                half capsule = (1-smoothstep(.12h, .95h, abs(uv.x))) * (1-smoothstep(.45h, 1.0h, abs(uv.y)));
                half radius=length(uv);
                half ring = smoothstep(.36h,.58h,radius) * (1-smoothstep(.62h,.95h,radius));
                half shape = lerp(capsule,ring,saturate(_Splash));
                half alpha = shape * i.color.a * smoothstep(1.5, 3.0, distance(i.world,_WorldSpaceCameraPos));
                clip(alpha-.003h);
                return half4(i.color.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
