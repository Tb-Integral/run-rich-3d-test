Shader "RunRich/Reference Sky"
{
    Properties
    {
        _HorizonColor("Horizon", Color) = (0.31,1,1,1)
        _ZenithColor("Zenith", Color) = (0.055,0.56,0.92,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _HorizonColor;
            half4 _ZenithColor;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.direction = v.positionOS.xyz; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float elevation = saturate(normalize(i.direction).y * 6.5);
                return half4(lerp(_HorizonColor.rgb, _ZenithColor.rgb, smoothstep(0,1,elevation)),1);
            }
            ENDHLSL
        }
    }
}
