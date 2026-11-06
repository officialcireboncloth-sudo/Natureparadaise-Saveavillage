Shader "Nature Paradise/UI/Soil Scanner Glass"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha Clip",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;float4 local:TEXCOORD2;};
   sampler2D _MainTex, _ScannerFarmView;
   float4 _ScannerFarmView_TexelSize, _ClipRect;
   float _ScannerGlassReady;
   v2f vert(appdata v)
   {
    v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.vertex);o.uv=v.uv;o.color=v.color;return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float2 uv=i.screen.xy/i.screen.w;
    float2 d=_ScannerFarmView_TexelSize.xy*4;
    half3 scene=tex2D(_ScannerFarmView,uv).rgb*.4;
    scene+=tex2D(_ScannerFarmView,uv+d).rgb*.15;
    scene+=tex2D(_ScannerFarmView,uv-d).rgb*.15;
    scene+=tex2D(_ScannerFarmView,uv+float2(d.x,-d.y)).rgb*.15;
    scene+=tex2D(_ScannerFarmView,uv+float2(-d.x,d.y)).rgb*.15;
    scene=lerp(dot(scene,half3(.2126,.7152,.0722)).xxx,scene,.4);
    half3 glass=i.color.rgb+scene*.095+lerp(-.008,.008,uv.y);
    fixed4 color=fixed4(lerp(i.color.rgb,glass,_ScannerGlassReady),i.color.a*tex2D(_MainTex,i.uv).a);
    #ifdef UNITY_UI_CLIP_RECT
     color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
     clip(color.a-.001);
    #endif
    return color;
   }
   ENDCG
  }
 }
}
