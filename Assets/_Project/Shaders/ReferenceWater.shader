Shader "RunRich/Reference Water"
{
    Properties
    {
        _BaseMap("Water pattern", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _HorizonColor("Horizon", Color) = (0.31,1,1,1)
        _PatternScale("World pattern scale", Float) = 0.04
        _FadeStart("Fade start", Float) = 30
        _FadeEnd("Fade end", Float) = 160
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; half4 _HorizonColor;
            float _PatternScale; float _FadeStart; float _FadeEnd;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS); return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionWS.xz * _PatternScale + _Time.y * float2(0.0008,0.0012);
                half3 pattern = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                float distanceToCamera = distance(_WorldSpaceCameraPos.xz, i.positionWS.xz);
                float fade = smoothstep(_FadeStart, _FadeEnd, distanceToCamera);
                return half4(lerp(pattern, _HorizonColor.rgb, fade),1);
            }
            ENDHLSL
        }
    }
}
