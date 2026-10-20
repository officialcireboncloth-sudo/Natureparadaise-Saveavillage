Shader "Nature Paradise/Construction Reveal"
{
    Properties { _BaseMap("Atlas",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _ClipY("Ground plane",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST; half4 _BaseColor; float _ClipY;
        CBUFFER_END
        struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
        struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normal:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
        V vert(A a) { V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o; }
        half4 frag(V i):SV_Target
        {
            clip(i.positionWS.y-_ClipY);
            Light sun=GetMainLight();half n=smoothstep(-.15,.55,dot(normalize(i.normal),sun.direction));
            half3 light=max(SampleSH(normalize(i.normal)),half3(.12,.12,.14))+sun.color*lerp(.35,.85,n);
            half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*light;
            return half4(MixFog(color,i.fog),1);
        }
        half4 depth(V i):SV_Target {clip(i.positionWS.y-_ClipY);return 0;}
        ENDHLSL
        Pass { Tags {"LightMode"="UniversalForwardOnly"} HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
        ENDHLSL }
        Pass { Tags {"LightMode"="DepthOnly"} ColorMask 0 ZWrite On HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depth
        ENDHLSL }
    }
}
